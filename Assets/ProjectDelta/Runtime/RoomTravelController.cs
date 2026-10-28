using ProjectDelta.Core; // 순수 방 연결 규칙
using UnityEngine; // 실제 방 객체
using UnityEngine.Tilemaps; // 방의 바닥과 장애물
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class RoomTravelController : MonoBehaviour // 시험 방 사이의 이동 제어
    { // 클래스 시작
        [SerializeField] private GameObject firstRoom; // 첫 번째 시험 방
        [SerializeField] private GameObject secondRoom; // 두 번째 시험 방
        [SerializeField] private GridMoveController player; // 실제 격자 플레이어
        private GameInputRouter input; // 탐험 상호작용 입력
        private RoomTransitionState state; // 층 지도상의 현재 방
        private int lastTransitionFrame = -1; // 같은 프레임 중복 전환 차단
        private readonly RoomDoor eastDoor = new RoomDoor(0, 0, 5, 0, MoveDirection.Right, 1, 0, -5, 0); // 첫 방 오른쪽 문
        private readonly RoomDoor westDoor = new RoomDoor(1, 0, -5, 0, MoveDirection.Left, 0, 0, 5, 0); // 두 번째 방 왼쪽 문
        public int RoomX => state.RoomX; // 현재 방 가로 좌표
        public int RoomY => state.RoomY; // 현재 방 세로 좌표
        public void Configure(GameObject first, GameObject second, GridMoveController controller) // 씬 제작 시 방과 플레이어 연결
        { // 연결 시작
            firstRoom = first; // 첫 번째 방 연결
            secondRoom = second; // 두 번째 방 연결
            player = controller; // 플레이어 연결
        } // 연결 끝
        private void Awake() // 방 이동 초기화
        { // 초기화 시작
            if (firstRoom == null || secondRoom == null || player == null || AppRoot.Instance == null) // 필수 연결 검사
            { // 설정 오류 조건 시작
                Debug.LogError("Room travel requires two rooms, the player, and AppRoot."); // 누락된 연결 기록
                enabled = false; // 잘못된 이동 중단
                return; // 추가 초기화 종료
            } // 설정 오류 조건 끝
            state = new RoomTransitionState(0, 0); // 시작 방 좌표 초기화
            input = AppRoot.Instance.Input; // 공통 탐험 입력 조회
        } // 초기화 끝
        private void OnEnable() // 상호작용 입력 연결
        { // 연결 시작
            if (input != null) // 입력 준비 검사
            { // 연결 조건 시작
                input.InteractionRequested += OnInteraction; // 문 상호작용 구독
            } // 연결 조건 끝
        } // 연결 끝
        private void OnDisable() // 상호작용 입력 해제
        { // 해제 시작
            if (input != null) // 입력 존재 검사
            { // 해제 조건 시작
                input.InteractionRequested -= OnInteraction; // 문 상호작용 구독 해제
            } // 해제 조건 끝
        } // 해제 끝
        private void OnInteraction() // 실제 문 앞 상호작용 처리
        { // 처리 시작
            if (input == null || !input.IsExplorationEnabled || state == null || player.IsMoving || lastTransitionFrame == Time.frameCount) // 입력 상태와 이동 완료 검사
            { // 전환 차단 조건 시작
                return; // 중복 요청과 이동 중 입력 무시
            } // 전환 차단 조건 끝
            var leavingFirst = state.RoomX == 0 && state.RoomY == 0; // 현재 첫 번째 방 검사
            var door = leavingFirst ? eastDoor : westDoor; // 현재 방의 연결 문 선택
            var destination = leavingFirst ? secondRoom : firstRoom; // 도착 방 객체 선택
            var floorObject = destination.transform.Find("Floor"); // 도착 방 바닥 조회
            var obstacleObject = destination.transform.Find("Obstacles"); // 도착 방 장애물 조회
            if (floorObject == null || obstacleObject == null) // 필수 타일맵 검사
            { // 타일맵 누락 조건 시작
                return; // 기존 방 유지
            } // 타일맵 누락 조건 끝
            var floor = floorObject.GetComponent<Tilemap>(); // 도착 바닥 타일맵
            var obstacles = obstacleObject.GetComponent<Tilemap>(); // 도착 장애물 타일맵
            if (!state.TryTransition(door, player.X, player.Y, player.Facing, (roomX, roomY, x, y) => player.CanArrive(floor, obstacles, x, y))) // 문 접근과 도착 칸 검사
            { // 전환 거절 조건 시작
                return; // 기존 방과 위치 유지
            } // 전환 거절 조건 끝
            destination.SetActive(true); // 도착 방 표시
            if (!player.ChangeRoom(floor, obstacles, door.TargetX, door.TargetY)) // 방 활성화 후 도착 칸 재검사
            { // 도착 실패 조건 시작
                state = new RoomTransitionState(door.RoomX, door.RoomY); // 이전 방 좌표 복구
                destination.SetActive(false); // 실패한 목적 방 표시 취소
                lastTransitionFrame = Time.frameCount; // 같은 프레임의 재시도 차단
                return; // 기존 플레이어와 방 유지
            } // 도착 실패 조건 끝
            (leavingFirst ? firstRoom : secondRoom).SetActive(false); // 이전 방 표시 종료
            lastTransitionFrame = Time.frameCount; // 같은 프레임의 재전환 차단
        } // 처리 끝
    } // 클래스 끝
} // 이름 공간 끝
