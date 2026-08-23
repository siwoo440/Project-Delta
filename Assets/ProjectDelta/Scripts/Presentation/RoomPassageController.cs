using ProjectDelta.Data; // 방 정의 데이터 사용 (18일차)
using ProjectDelta.Domain; // 도메인 통로 규칙 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    public enum TestRoomLayoutKind // 15일차 테스트 방 종류
    {
        Primary, // 첫 번째 테스트 방
        Secondary // 두 번째 테스트 방
    }

    public sealed class RoomPassageController : MonoBehaviour // 현재 테스트 방 통로 상태 제어
    {
        [SerializeField] private string roomId = "TestRoom_A"; // 현재 테스트 방 식별자
        [SerializeField] private RoomDefinition roomDefinition; // 방 통로 배치를 담은 정의 데이터 (18일차)
        [SerializeField] private TestRoomLayoutKind layoutKind = TestRoomLayoutKind.Primary; // 테스트 방 통로 구성 종류
        [SerializeField] private Transform unlockedDoorVisual; // 일반 문 시각 오브젝트
        [SerializeField] private Transform lockedDoorVisual; // 잠긴 문 시각 오브젝트
        [SerializeField] private Transform boundaryDoorVisual; // 방 연결 경계 문 시각 오브젝트

        private RoomInstance roomInstance; // 방 런타임 인스턴스 (통로 데이터 + 방문 상태, 20일차)
        private RoomGridLayout layout; // 방 통로 논리 데이터
        private GridPassage unlockedDoorPassage; // 일반 문 통로 상태
        private GridPassage lockedDoorPassage; // 잠긴 문 통로 상태
        private GridPassage boundaryDoorPassage; // 방 연결 경계 문 상태

        public string RoomId => roomId; // 현재 방 식별자 공개
        public TestRoomLayoutKind LayoutKind => layoutKind; // 테스트 방 종류 공개
        public GridPosition BoundaryPosition => layoutKind == TestRoomLayoutKind.Primary ? new GridPosition(0, 2) : new GridPosition(0, -2); // 방 연결 경계 칸 공개
        public CardinalDirection BoundaryDirection => layoutKind == TestRoomLayoutKind.Primary ? CardinalDirection.North : CardinalDirection.South; // 방 연결 출구 방향 공개
        public GridPassage BoundaryDoorPassage => boundaryDoorPassage; // 방 연결 문 상태 공개
        public RoomInstance CurrentInstance => roomInstance; // 방문 상태 확인용 방 인스턴스 공개 (20일차)

        private void Awake() // 방 정의로부터 통로 데이터 구성 (18일차: 하드코딩 대신 RoomDefinition 사용)
        {
            if (roomDefinition == null) // 방 정의 미지정 확인
            {
                Debug.LogError($"[Project Delta] {roomId}에 RoomDefinition이 지정되지 않았습니다. 모든 방향이 벽 없이 열린 상태로 동작합니다.", this); // 정의 누락 경고 출력
            }

            roomInstance = RoomInstance.Create(roomId, roomDefinition != null ? roomDefinition.Id : roomId, roomDefinition != null ? roomDefinition.Passages : null); // 정의 데이터로 방 인스턴스 생성
            layout = roomInstance.Layout; // 생성된 통로 데이터 연결

            if (RunContext.Current != null) // 실제 런 진행 여부 확인 (21일차: 테스트 씬은 등록하지 않음)
            {
                RunContext.Current.Dungeon.Register(roomInstance); // 현재 런의 방 레지스트리에 등록

                if (DungeonSaveMapper.TryGetRoomState(roomId, out RoomRunState savedState)) // 26일차: 이어하기로 복원할 상태가 있는지 확인
                {
                    roomInstance.ApplySavedState(savedState.Visited, savedState.Completed, savedState.ChestOpened); // 저장된 방 상태 복원
                }
            }

            if (layoutKind == TestRoomLayoutKind.Primary) // 첫 번째 테스트 방 확인
            {
                unlockedDoorPassage = layout.GetPassage(new GridPosition(0, 0), CardinalDirection.North); // 일반 문 통로 참조 조회
                lockedDoorPassage = layout.GetPassage(new GridPosition(1, 0), CardinalDirection.North); // 잠긴 문 통로 참조 조회
            }

            boundaryDoorPassage = layout.GetPassage(BoundaryPosition, BoundaryDirection); // 방 연결 경계 문 통로 참조 조회

            RefreshDoorVisuals(); // 초기 문 시각 상태 적용
        }

        private void Update() // 공유 문 시각 상태 갱신
        {
            RefreshDoorVisuals(); // 양쪽 방의 공유 경계 문 상태 반영
        }

        public bool CanPass(GridPosition position, CardinalDirection direction) // 현재 칸 방향 통과 가능 여부 검사
        {
            return layout != null && layout.CanPass(position, direction); // 통로 데이터 판정 반환
        }

        public bool TryGetDoor(GridPosition position, CardinalDirection direction, out GridPassage doorPassage) // 정면 문 조회
        {
            doorPassage = layout != null ? layout.GetPassage(position, direction) : null; // 현재 방향 통로 조회

            if (doorPassage == null || doorPassage.Type != PassageType.Door) // 문 통로 여부 확인
            {
                doorPassage = null; // 문 결과 초기화
                return false; // 문 없음 반환
            }

            return true; // 문 존재 반환
        }

        public DoorOpenResult TryOpenDoor(GridPosition position, CardinalDirection direction, PlayerRunState playerState) // 정면 문 열기 시도
        {
            if (!TryGetDoor(position, direction, out GridPassage doorPassage)) // 정면 문 존재 확인
            {
                return DoorOpenResult.NotDoor; // 문 아님 반환
            }

            DoorOpenResult result = doorPassage.TryOpenDoor(playerState); // 도메인 문 열기 규칙 실행

            if (result == DoorOpenResult.Opened) // 문 열기 성공 확인
            {
                RefreshDoorVisuals(); // 열린 문 시각 상태 갱신
            }

            return result; // 문 열기 결과 반환
        }

        public void SetBoundaryDoorPassage(GridPassage sharedPassage) // 연결된 두 방의 경계 문 상태 공유
        {
            if (sharedPassage == null || layout == null) // 공유 통로와 방 데이터 확인
            {
                return; // 공유 설정 중단
            }

            boundaryDoorPassage = sharedPassage; // 공유 경계 문 상태 저장
            layout.SetPassage(BoundaryPosition, BoundaryDirection, boundaryDoorPassage); // 현재 방 경계에 공유 문 등록
            RefreshDoorVisuals(); // 공유 상태 즉시 시각 반영
        }

        private void RefreshDoorVisuals() // 문 시각 상태 동기화
        {
            if (unlockedDoorVisual != null) // 일반 문 시각 오브젝트 확인
            {
                unlockedDoorVisual.gameObject.SetActive(unlockedDoorPassage == null || !unlockedDoorPassage.IsOpen); // 일반 문 열림 시 숨김
            }

            if (lockedDoorVisual != null) // 잠긴 문 시각 오브젝트 확인
            {
                lockedDoorVisual.gameObject.SetActive(lockedDoorPassage == null || !lockedDoorPassage.IsOpen); // 잠긴 문 열림 시 숨김
            }

            if (boundaryDoorVisual != null) // 방 연결 문 시각 오브젝트 확인
            {
                boundaryDoorVisual.gameObject.SetActive(boundaryDoorPassage == null || !boundaryDoorPassage.IsOpen); // 공유 경계 문 열림 시 숨김
            }
        }
    }
}
