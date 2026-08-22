using ProjectDelta.Domain; // 도메인 통로 규칙 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    public sealed class RoomPassageController : MonoBehaviour // 현재 테스트 방 통로 상태 제어
    {
        [SerializeField] private Transform unlockedDoorVisual; // 일반 문 시각 오브젝트
        [SerializeField] private Transform lockedDoorVisual; // 잠긴 문 시각 오브젝트

        private RoomGridLayout layout; // 방 통로 논리 데이터
        private GridPassage unlockedDoorPassage; // 일반 문 통로 상태
        private GridPassage lockedDoorPassage; // 잠긴 문 통로 상태

        private void Awake() // 테스트 방 통로 데이터 구성
        {
            layout = new RoomGridLayout(); // 방 통로 데이터 생성
            unlockedDoorPassage = GridPassage.CreateDoor(false); // 일반 닫힌 문 생성
            lockedDoorPassage = GridPassage.CreateDoor(true); // 잠긴 닫힌 문 생성
            layout.SetPassage(new GridPosition(0, 0), CardinalDirection.North, unlockedDoorPassage); // 중앙 북쪽 일반 문 등록
            layout.SetPassage(new GridPosition(1, 0), CardinalDirection.North, lockedDoorPassage); // 동쪽 북쪽 잠긴 문 등록
            layout.SetPassage(new GridPosition(-1, 0), CardinalDirection.North, GridPassage.CreateWall()); // 서쪽 북쪽 테스트 벽 등록
            RefreshDoorVisuals(); // 초기 문 시각 상태 적용
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
        }
    }
}
