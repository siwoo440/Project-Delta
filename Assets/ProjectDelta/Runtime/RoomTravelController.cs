using System; // 방 변경 알림과 구성 오류
using ProjectDelta.Core; // 순수 방 연결 규칙
using UnityEngine; // 실제 방 객체
using UnityEngine.Tilemaps; // 방의 바닥과 장애물
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class RoomTravelController : MonoBehaviour // 시험 방 사이의 이동 제어
    { // 클래스 시작
        [SerializeField] private RoomBinding[] rooms; // 방 좌표별 실제 씬 객체
        [SerializeField] private DoorBinding[] doors; // 씬에 저장된 양방향 출구
        [SerializeField] private GameObject firstRoom; // 3일차 씬의 기존 첫 방 참조
        [SerializeField] private GameObject secondRoom; // 3일차 씬의 기존 두 번째 방 참조
        [SerializeField] private GridMoveController player; // 실제 격자 플레이어
        private GameInputRouter input; // 탐험 상호작용 입력
        private RoomTransitionState state; // 층 지도상의 현재 방
        private int lastTransitionFrame = -1; // 같은 프레임 중복 전환 차단
        public static int? GenerationSeedOverride // 자동 검증용 생성 시드 지정
        { // 속성 시작
            get; // 지정 시드 조회
            set; // 검증 시드 설정
        } // 속성 끝
        public event Action RoomChanged; // 성공한 방 이동 알림
        public DungeonLayout Layout // 화면과 공유하는 방 구조
        { // 속성 시작
            get; // 방 구조 조회
            private set; // 초기 구조 설정
        } // 속성 끝
        public DungeonDiscovery Discovery // 현재 게임의 발견 기록
        { // 속성 시작
            get; // 발견 기록 조회
            private set; // 새 게임 기록 설정
        } // 속성 끝
        public int Seed // 이번 던전의 생성 시드
        { // 속성 시작
            get; // 시드 조회
            private set; // 실행 시드 설정
        } // 속성 끝
        public int RoomX => state.RoomX; // 현재 방 가로 좌표
        public int RoomY => state.RoomY; // 현재 방 세로 좌표
        public MoveDirection Facing => player.Facing; // 현재 플레이어의 이동 방향
        public bool IsReady => state != null && Layout != null && Discovery != null && player != null; // 생성과 화면 연결 준비 상태
        public void Configure(GameObject first, GameObject second, GridMoveController controller) // 씬 제작 시 방과 플레이어 연결
        { // 연결 시작
            var roomList = new[] // 기존 두 방의 씬 연결
            { // 방 목록 시작
                new RoomBinding(0, 0, false, first), // 첫 번째 방
                new RoomBinding(1, 0, false, second) // 두 번째 방
            }; // 방 목록 끝
            var doorList = new[] // 기존 두 방의 양방향 문
            { // 출구 목록 시작
                new DoorBinding(0, 0, 5, 0, MoveDirection.Right, 1, 0, -5, 0), // 첫 방 오른쪽 문
                new DoorBinding(1, 0, -5, 0, MoveDirection.Left, 0, 0, 5, 0) // 두 번째 방 왼쪽 문
            }; // 출구 목록 끝
            Configure(roomList, doorList, controller); // 방 데이터와 플레이어 연결
            firstRoom = first; // 기존 첫 방 참조 유지
            secondRoom = second; // 기존 두 번째 방 참조 유지
        } // 연결 끝
        public void Configure(RoomBinding[] roomList, DoorBinding[] doorList, GridMoveController controller) // 임의 시험 방과 출구 연결
        { // 연결 시작
            rooms = roomList; // 방 좌표와 씬 객체 보관
            doors = doorList; // 문 위치와 방 연결 보관
            player = controller; // 플레이어 연결
        } // 연결 끝
        public bool HasConfiguredRoom(int x, int y, GameObject root) // 저장된 방 연결 확인
        { // 확인 시작
            if (rooms == null) // 방 연결 존재 검사
            { // 누락 조건 시작
                return false; // 저장된 연결 없음
            } // 누락 조건 끝
            foreach (var room in rooms) // 저장된 방 순회
            { // 방 순회 시작
                if (room != null && room.X == x && room.Y == y && room.Root == root) // 좌표와 객체 일치 검사
                { // 일치 조건 시작
                    return true; // 기존 연결 확인
                } // 일치 조건 끝
            } // 방 순회 끝
            return false; // 일치하는 연결 없음
        } // 확인 끝
        public bool HasExactConfiguration(RoomBinding[] expectedRooms, DoorBinding[] expectedDoors, GridMoveController expectedPlayer) // 사용자 연결 덮어쓰기 전 원본 검사
        { // 검사 시작
            if (expectedRooms == null || expectedDoors == null || player != expectedPlayer) // 예상 데이터와 플레이어 검사
            { // 불일치 조건 시작
                return false; // 기존 구성 보존
            } // 불일치 조건 끝
            if (rooms == null || rooms.Length == 0) // 3일차 기존 씬 형식 검사
            { // 기존 형식 조건 시작
                return expectedRooms.Length == 2 && expectedDoors.Length == 2 && (doors == null || doors.Length == 0) && firstRoom == expectedRooms[0].Root && secondRoom == expectedRooms[1].Root; // 기존 두 방 참조 일치 확인
            } // 기존 형식 조건 끝
            if (doors == null || rooms.Length != expectedRooms.Length || doors.Length != expectedDoors.Length) // 방과 출구 수 검사
            { // 수 불일치 조건 시작
                return false; // 기존 구성 보존
            } // 수 불일치 조건 끝
            for (var index = 0; index < rooms.Length; index++) // 저장된 방 순회
            { // 방 검사 시작
                var actual = rooms[index]; // 현재 저장된 방
                var expected = expectedRooms[index]; // 예상하는 방
                if (actual == null || expected == null || actual.X != expected.X || actual.Y != expected.Y || actual.IsSecret != expected.IsSecret || actual.Root != expected.Root) // 방 위치와 객체 검사
                { // 방 불일치 조건 시작
                    return false; // 사용자 방 연결 보존
                } // 방 불일치 조건 끝
            } // 방 검사 끝
            for (var index = 0; index < doors.Length; index++) // 저장된 출구 순회
            { // 출구 검사 시작
                var actual = doors[index]; // 현재 저장된 출구
                var expected = expectedDoors[index]; // 예상하는 출구
                if (actual == null || expected == null || actual.RoomX != expected.RoomX || actual.RoomY != expected.RoomY || actual.ApproachX != expected.ApproachX || actual.ApproachY != expected.ApproachY || actual.Direction != expected.Direction || actual.TargetRoomX != expected.TargetRoomX || actual.TargetRoomY != expected.TargetRoomY || actual.TargetX != expected.TargetX || actual.TargetY != expected.TargetY || actual.IsSecret != expected.IsSecret) // 모든 출구 연결 정보 검사
                { // 출구 불일치 조건 시작
                    return false; // 사용자 출구 설정 보존
                } // 출구 불일치 조건 끝
            } // 출구 검사 끝
            return true; // 시험 구성과 완전 일치
        } // 검사 끝
        private void Awake() // 방 이동 초기화
        { // 초기화 시작
            if ((rooms == null || rooms.Length == 0) && firstRoom != null && secondRoom != null && player != null) // 기존 3일차 씬 형식 검사
            { // 기존 형식 조건 시작
                Configure(firstRoom, secondRoom, player); // 기존 두 방을 새 데이터 형식으로 연결
            } // 기존 형식 조건 끝
            if (rooms == null || rooms.Length == 0 || doors == null || player == null || AppRoot.Instance == null) // 필수 연결 검사
            { // 설정 오류 조건 시작
                Debug.LogError("Room travel requires room bindings, doors, the player, and AppRoot."); // 누락된 연결 기록
                enabled = false; // 잘못된 이동 중단
                return; // 추가 초기화 종료
            } // 설정 오류 조건 끝
            var definitions = new DungeonRoom[rooms.Length]; // 순수 방 정의 배열
            for (var index = 0; index < rooms.Length; index++) // 실제 방 연결 순회
            { // 방 순회 시작
                if (rooms[index] == null || rooms[index].Root == null) // 씬 방 객체 검사
                { // 누락 조건 시작
                    Debug.LogError("Every room binding requires a scene object."); // 누락된 방 연결 기록
                    enabled = false; // 잘못된 이동 중단
                    return; // 추가 초기화 종료
                } // 누락 조건 끝
                for (var previous = 0; previous < index; previous++) // 앞서 등록한 방 순회
                { // 중복 검사 시작
                    if (rooms[previous].Root == rooms[index].Root) // 같은 씬 객체 중복 검사
                    { // 중복 조건 시작
                        Debug.LogError("Room scene objects must be unique."); // 중복 씬 방 기록
                        enabled = false; // 잘못된 이동 중단
                        return; // 추가 초기화 종료
                    } // 중복 조건 끝
                } // 중복 검사 끝
                definitions[index] = new DungeonRoom(rooms[index].X, rooms[index].Y, rooms[index].IsSecret); // 순수 방 정보 생성
            } // 방 순회 끝
            var exits = new RoomDoor[doors.Length]; // 순수 출구 정의 배열
            for (var index = 0; index < doors.Length; index++) // 저장된 출구 순회
            { // 출구 순회 시작
                if (doors[index] == null) // 출구 정보 누락 검사
                { // 누락 조건 시작
                    Debug.LogError("Every door binding requires a definition."); // 누락된 출구 기록
                    enabled = false; // 잘못된 이동 중단
                    return; // 추가 초기화 종료
                } // 누락 조건 끝
                exits[index] = doors[index].ToDoor(); // 순수 방 출구 생성
            } // 출구 순회 끝
            try // 방 연결 유효성 검사
            { // 검사 시작
                _ = new DungeonLayout(definitions, exits); // 기존 시험 방 연결 검증
                Seed = GenerationSeedOverride ?? Guid.NewGuid().GetHashCode(); // 검증 시드 또는 새 게임의 난수 시드
                Layout = DungeonGenerator.Generate(Seed); // 시드 기반 1층 구조 생성
                rooms = GeneratedRoomFactory.Create(Layout, rooms, doors, player); // 생성된 방 구조를 실제 2D 방에 연결
                Discovery = new DungeonDiscovery(Layout, 0, 0); // 새 게임의 시작 방 방문 기록
            } // 검사 끝
            catch (Exception exception) // 잘못된 방 구조와 생성 오류 처리
            { // 오류 조건 시작
                Layout = null; // 실패한 방 구조 제거
                Discovery = null; // 실패한 방문 기록 제거
                Debug.LogError("Invalid dungeon layout: " + exception.Message); // 구성 오류 기록
                enabled = false; // 잘못된 이동 중단
                return; // 추가 초기화 종료
            } // 오류 조건 끝
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
            var door = Layout.FindDoor(state.RoomX, state.RoomY, player.X, player.Y, player.Facing); // 현재 접근 위치의 출구 조회
            if (door == null) // 정면 문 존재 검사
            { // 문 없음 조건 시작
                return; // 일치하는 출구 없음
            } // 문 없음 조건 끝
            var previous = FindRoom(state.RoomX, state.RoomY); // 현재 방의 씬 객체 조회
            var destination = FindRoom(door.TargetRoomX, door.TargetRoomY); // 도착 방의 씬 객체 조회
            if (previous == null || destination == null) // 실제 방 객체 검사
            { // 방 누락 조건 시작
                return; // 기존 방 유지
            } // 방 누락 조건 끝
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
            previous.SetActive(false); // 이전 방 표시 종료
            Discovery.TryVisit(door.TargetRoomX, door.TargetRoomY); // 실제 도착한 방만 방문 기록
            lastTransitionFrame = Time.frameCount; // 같은 프레임의 재전환 차단
            RoomChanged?.Invoke(); // 실제 방 이동 성공 알림
        } // 처리 끝
        private GameObject FindRoom(int x, int y) // 방 좌표의 실제 씬 객체 조회
        { // 조회 시작
            foreach (var room in rooms) // 저장된 방 연결 순회
            { // 방 순회 시작
                if (room.X == x && room.Y == y) // 방 좌표 일치 검사
                { // 일치 조건 시작
                    return room.Root; // 실제 방 객체 전달
                } // 일치 조건 끝
            } // 방 순회 끝
            return null; // 좌표에 맞는 방 없음
        } // 조회 끝
    } // 클래스 끝
} // 이름 공간 끝
