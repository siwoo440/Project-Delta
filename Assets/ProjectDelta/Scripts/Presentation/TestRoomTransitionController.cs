using ProjectDelta.Domain; // 도메인 방 연결 규칙 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    public sealed class TestRoomTransitionController : MonoBehaviour // 15일차 두 테스트 방 연결 제어
    {
        [SerializeField] private RoomPassageController roomA; // 첫 번째 테스트 방 통로 컨트롤러
        [SerializeField] private RoomPassageController roomB; // 두 번째 테스트 방 통로 컨트롤러
        [SerializeField] private GridFloorGuideController roomAGuide; // 첫 번째 방 이동 가능 칸 선
        [SerializeField] private GridFloorGuideController roomBGuide; // 두 번째 방 이동 가능 칸 선

        private RoomConnection connection; // 양방향 테스트 방 연결 데이터
        private bool initialized; // 연결 초기화 여부

        private void Start() // 런타임 연결 초기화
        {
            EnsureInitialized(); // 공유 문과 방 연결 구성
            SetCurrentRoomVisual(roomA != null ? roomA.RoomId : null); // 첫 번째 방 가이드 표시
        }

        public bool TryTransition(string currentRoomId, GridPosition currentPosition, CardinalDirection moveDirection, out RoomPassageController destinationRoom, out GridPosition destinationEntryPosition) // 방 경계 이동 대상 조회
        {
            EnsureInitialized(); // 방 연결 데이터 준비
            destinationRoom = null; // 목적 방 초기화
            destinationEntryPosition = GridPosition.Zero; // 목적 입구 초기화

            if (connection == null) // 연결 데이터 존재 확인
            {
                return false; // 방 이동 불가 반환
            }

            if (!connection.TryGetDestination(currentRoomId, currentPosition, moveDirection, out string destinationRoomId, out destinationEntryPosition)) // 방·경계 칸·방향 연결 조건 확인
            {
                return false; // 연결 조건 불일치 반환
            }

            destinationRoom = GetRoomById(destinationRoomId); // 목적 방 컨트롤러 조회

            if (destinationRoom == null) // 목적 방 존재 여부 확인
            {
                return false; // 연결 방 없음 반환
            }

            SetCurrentRoomVisual(destinationRoomId); // 목적 방 이동 가능 칸 선 표시
            return true; // 방 경계 이동 허용
        }

        private void EnsureInitialized() // 방 연결과 공유 문 상태 구성
        {
            if (initialized) // 이미 초기화되었는지 확인
            {
                return; // 중복 초기화 중단
            }

            if (roomA == null || roomB == null) // 두 테스트 방 연결 여부 확인
            {
                return; // 연결 초기화 대기
            }

            GridPassage sharedDoor = roomA.BoundaryDoorPassage; // 첫 번째 방 경계 문 상태 조회

            if (sharedDoor == null) // 경계 문 생성 여부 확인
            {
                return; // 방 Awake 이후 다시 시도
            }

            roomB.SetBoundaryDoorPassage(sharedDoor); // 두 방이 같은 경계 문 상태 공유
            RoomConnectionEnd endA = new RoomConnectionEnd(roomA.RoomId, roomA.BoundaryPosition, roomA.BoundaryDirection); // A방 연결 끝 생성
            RoomConnectionEnd endB = new RoomConnectionEnd(roomB.RoomId, roomB.BoundaryPosition, roomB.BoundaryDirection); // B방 연결 끝 생성
            connection = new RoomConnection(endA, endB); // 양방향 방 연결 데이터 생성
            initialized = true; // 연결 초기화 완료 표시
        }

        private RoomPassageController GetRoomById(string roomId) // 방 식별자로 컨트롤러 조회
        {
            if (roomA != null && roomA.RoomId == roomId) // A방 식별자 확인
            {
                return roomA; // A방 반환
            }

            if (roomB != null && roomB.RoomId == roomId) // B방 식별자 확인
            {
                return roomB; // B방 반환
            }

            return null; // 연결 방 없음 반환
        }

        private void SetCurrentRoomVisual(string roomId) // 현재 방 바닥 이동 가능 칸 선 전환
        {
            if (roomAGuide != null) // A방 가이드 존재 확인
            {
                roomAGuide.SetGuideVisible(roomA != null && roomA.RoomId == roomId); // A방일 때 선 표시
            }

            if (roomBGuide != null) // B방 가이드 존재 확인
            {
                roomBGuide.SetGuideVisible(roomB != null && roomB.RoomId == roomId); // B방일 때 선 표시
            }
        }
    }
}
