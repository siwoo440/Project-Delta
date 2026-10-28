using System; // 방 변경 알림과 구성 오류
using System.Collections.Generic; // 완료한 조우 기록
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
        private readonly FloorProgress floorProgress = new FloorProgress(); // 현재 층과 전환 잠금
        private readonly HashSet<string> completedEncounters = new HashSet<string>(); // 층별 완료 조우 기록
        private RoomBinding[] templates; // 변경 전 보존한 방 원형
        private DoorBinding[] templateDoors; // 원형의 기존 문 정의
        private GameObject templateHolder; // 숨긴 방 원형 보관 객체
        private GameObject currentFloorHolder; // 생성된 현재 층의 상위 객체
        public int Floor => floorProgress.Floor; // 현재 층 번호
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
                SaveTemplates(); // 변경 전 방 원형 보관
                Seed = GenerationSeedOverride ?? Guid.NewGuid().GetHashCode(); // 검증 시드 또는 새 게임의 난수 시드
                Layout = DungeonGenerator.Generate(Seed); // 시드 기반 1층 구조 생성
                ValidateStairRoom(Layout, rooms); // 방을 변경하기 전 계단 칸 검사
                ValidateEncounterRoom(Layout, rooms); // 방을 변경하기 전 조우 칸 검사
                rooms = GeneratedRoomFactory.Create(Layout, rooms, doors, player); // 생성된 방 구조를 실제 2D 방에 연결
                Discovery = new DungeonDiscovery(Layout, 0, 0); // 새 게임의 시작 방 방문 기록
                AddStairMarker(Layout, rooms); // 계단 방의 실제 표시 추가
                AddEncounterMarker(Layout, rooms); // 상위 개체 방의 시험 대상 표시
            } // 검사 끝
            catch (Exception exception) // 잘못된 방 구조와 생성 오류 처리
            { // 오류 조건 시작
                if (templateHolder != null) // 보관한 원형 존재 검사
                { // 원형 정리 시작
                    Destroy(templateHolder); // 실패한 방 원형 제거
                } // 원형 정리 끝
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
                player.StepArrived += OnStepArrived; // 한 칸 도착 시 조우 검사
            } // 연결 조건 끝
        } // 연결 끝
        private void OnDisable() // 상호작용 입력 해제
        { // 해제 시작
            if (input != null) // 입력 존재 검사
            { // 해제 조건 시작
                input.InteractionRequested -= OnInteraction; // 문 상호작용 구독 해제
                player.StepArrived -= OnStepArrived; // 한 칸 도착 연결 해제
            } // 해제 조건 끝
        } // 해제 끝
        private void OnStepArrived() // 한 칸 도착 시 기존 조우 범위 검사
        { // 검사 시작
            if (!IsReady || input == null || !input.IsExplorationEnabled) // 탐험 상태 검사
            { // 검사 제외 조건 시작
                return; // 탐험 외 조우 차단
            } // 검사 제외 조건 끝
            var room = Layout.GetRoom(state.RoomX, state.RoomY); // 현재 방 역할 조회
            if (room != null && room.Kind == RoomKind.Elite && !completedEncounters.Contains(EncounterKey()) && EncounterRange.Contains(player.X, player.Y, 0, 0)) // 대상 주변 여덟 방향 검사
            { // 자동 조우 조건 시작
                AppRoot.Instance.StartCombat(); // 전투 진입과 대기 이동 취소
            } // 자동 조우 조건 끝
        } // 검사 끝
        public bool CompleteCurrentEncounter() // 승리한 방의 시험 조우 완료
        { // 완료 시작
            if (!IsReady) // 현재 방 준비 상태 검사
            { // 준비 안 됨 조건 시작
                return false; // 잘못된 조우 완료 차단
            } // 준비 안 됨 조건 끝
            var room = Layout.GetRoom(state.RoomX, state.RoomY); // 현재 방 역할 조회
            if (room == null || room.Kind != RoomKind.Elite || !completedEncounters.Add(EncounterKey())) // 상위 개체 방과 중복 완료 검사
            { // 완료 거절 조건 시작
                return false; // 잘못된 방과 중복 완료 차단
            } // 완료 거절 조건 끝
            var root = FindRoom(state.RoomX, state.RoomY); // 실제 현재 방 조회
            var marker = root == null ? null : root.transform.Find("EncounterMarker"); // 실제 조우 대상 표시 조회
            if (marker != null) // 조우 대상 표시 검사
            { // 표시 제거 시작
                marker.gameObject.SetActive(false); // 완료한 대상 즉시 숨김
                Destroy(marker.gameObject); // 완료한 대상 제거
            } // 표시 제거 끝
            return true; // 조우 완료 성공
        } // 완료 끝
        private string EncounterKey() // 층과 방으로 조우 기록 구분
        { // 식별 시작
            return Floor + ":" + state.RoomX + ":" + state.RoomY; // 현재 층의 고유 방 식별자
        } // 식별 끝
        private void OnInteraction() // 실제 문 앞 상호작용 처리
        { // 처리 시작
            if (input == null || !input.IsExplorationEnabled || state == null || player.IsMoving || lastTransitionFrame == Time.frameCount) // 입력 상태와 이동 완료 검사
            { // 전환 차단 조건 시작
                return; // 중복 요청과 이동 중 입력 무시
            } // 전환 차단 조건 끝
            var currentRoom = Layout.GetRoom(state.RoomX, state.RoomY); // 현재 방 역할 조회
            if (currentRoom != null && currentRoom.Kind == RoomKind.Stairs && player.X == 0 && player.Y == 0) // 계단 칸의 상호작용 검사
            { // 계단 상호작용 시작
                TryNextFloor(); // 다음 층 생성과 이동 요청
                return; // 문 상호작용 중복 방지
            } // 계단 상호작용 끝
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
        private void SaveTemplates() // 기존 씬 방을 변경 전 복제
        { // 보관 시작
            templateHolder = new GameObject("FloorTemplates"); // 숨긴 원형 보관 객체
            templateHolder.transform.SetParent(transform, false); // 씬 종료 시 원형 정리
            templateHolder.SetActive(false); // 원형 화면 표시 차단
            templates = new RoomBinding[rooms.Length]; // 원형 방 연결 목록
            templateDoors = doors; // 원본 문 정의 보관
            for (var index = 0; index < rooms.Length; index++) // 기존 방 순회
            { // 원형 복제 시작
                var source = rooms[index]; // 기존 방 연결
                var copy = Instantiate(source.Root, templateHolder.transform); // 변경 전 방 원형 복제
                templates[index] = new RoomBinding(source.X, source.Y, source.IsSecret, copy); // 원형 좌표 연결
            } // 원형 복제 끝
        } // 보관 끝
        private static void ValidateStairRoom(DungeonLayout layout, RoomBinding[] bindings) // 계단 방 원형의 중앙 칸 사전 검사
        { // 검사 시작
            DungeonRoom stair = null; // 생성된 계단 방 정의
            GameObject origin = null; // 기본 시작 방 원형
            GameObject exact = null; // 계단 좌표의 기존 방 원형
            foreach (var room in layout.Rooms) // 생성된 방 정의 순회
            { // 계단 방 조회 시작
                if (room.Kind == RoomKind.Stairs) // 계단 방 역할 검사
                { // 계단 조건 시작
                    stair = room; // 계단 방 정의 보관
                    break; // 계단 방 조회 종료
                } // 계단 조건 끝
            } // 계단 방 조회 끝
            if (stair == null) // 계단 방 존재 검사
            { // 계단 누락 조건 시작
                throw new InvalidOperationException("A stair room binding is required."); // 계단 누락 거절
            } // 계단 누락 조건 끝
            foreach (var binding in bindings) // 원형 방 연결 순회
            { // 원형 조회 시작
                if (binding.X == 0 && binding.Y == 0) // 시작 방 좌표 검사
                { // 시작 방 조건 시작
                    origin = binding.Root; // 복제 원형 보관
                } // 시작 방 조건 끝
                if (binding.X == stair.X && binding.Y == stair.Y) // 기존 계단 좌표 검사
                { // 일치 조건 시작
                    exact = binding.Root; // 해당 좌표의 원형 보관
                } // 일치 조건 끝
            } // 원형 조회 끝
            var root = exact != null ? exact : origin; // 기존 방 또는 시작 방 원형 선택
            var floorObject = root == null ? null : root.transform.Find("Floor"); // 원형 바닥 객체
            var obstacleObject = root == null ? null : root.transform.Find("Obstacles"); // 원형 장애물 객체
            var floor = floorObject == null ? null : floorObject.GetComponent<Tilemap>(); // 원형 바닥 타일맵
            var obstacles = obstacleObject == null ? null : obstacleObject.GetComponent<Tilemap>(); // 원형 장애물 타일맵
            if (floor == null || obstacles == null || !floor.HasTile(Vector3Int.zero) || obstacles.HasTile(Vector3Int.zero)) // 계단 칸 통행 검사
            { // 통행 불가 조건 시작
                throw new InvalidOperationException("The stair cell must be walkable."); // 방 변경 전 충돌 거절
            } // 통행 불가 조건 끝
        } // 검사 끝
        private void AddStairMarker(DungeonLayout layout, RoomBinding[] bindings) // 계단 방의 중앙 칸 표시
        { // 표시 시작
            foreach (var room in layout.Rooms) // 생성된 방 순회
            { // 방 검사 시작
                if (room.Kind != RoomKind.Stairs) // 계단 방 역할 검사
                { // 다른 방 조건 시작
                    continue; // 계단 표시 제외
                } // 다른 방 조건 끝
                foreach (var binding in bindings) // 실제 방 연결 순회
                { // 연결 검사 시작
                    if (binding.X != room.X || binding.Y != room.Y) // 좌표 일치 검사
                    { // 다른 방 조건 시작
                        continue; // 다른 방 제외
                    } // 다른 방 조건 끝
                    var root = binding.Root; // 계단 방 객체
                    var floor = root.transform.Find("Floor").GetComponent<Tilemap>(); // 바닥 타일맵
                    var obstacles = root.transform.Find("Obstacles").GetComponent<Tilemap>(); // 장애물 타일맵
                    if (!floor.HasTile(Vector3Int.zero) || obstacles.HasTile(Vector3Int.zero)) // 계단 칸 통행 검사
                    { // 충돌 조건 시작
                        throw new InvalidOperationException("The stair cell must be walkable."); // 잘못된 계단 위치 거절
                    } // 충돌 조건 끝
                    var marker = new GameObject("StairMarker", typeof(SpriteRenderer)); // 계단 표시 객체
                    marker.transform.SetParent(root.transform, false); // 계단 방 아래 배치
                    var grid = root.GetComponent<Grid>(); // 방 격자 조회
                    var anchor = floor.tileAnchor; // 바닥 칸 중심 기준
                    var size = grid.cellSize; // 격자 칸 크기
                    marker.transform.position = floor.transform.TransformPoint(new Vector3(anchor.x * size.x, anchor.y * size.y, anchor.z * size.z)); // 중앙 칸의 화면 위치
                    marker.transform.localRotation = Quaternion.Euler(0f, 0f, 180f); // 아래층 방향 표시
                    var sprite = marker.GetComponent<SpriteRenderer>(); // 계단 표시 렌더러
                    sprite.sprite = player.transform.Find("DirectionIndicator").GetComponent<SpriteRenderer>().sprite; // 기존 화살표 이미지 사용
                    sprite.color = new Color(0.2f, 0.8f, 1f); // 파란 계단 색상
                    sprite.sortingOrder = 3; // 바닥 위 표시
                    return; // 계단 표시 완료
                } // 연결 검사 끝
            } // 방 검사 끝
            throw new InvalidOperationException("A stair room binding is required."); // 계단 방 누락 거절
        } // 표시 끝
        private static void ValidateEncounterRoom(DungeonLayout layout, RoomBinding[] bindings) // 조우 방 원형의 중앙 칸 사전 검사
        { // 검사 시작
            DungeonRoom encounter = null; // 생성된 조우 방 정의
            GameObject origin = null; // 기본 시작 방 원형
            GameObject exact = null; // 조우 좌표의 기존 방 원형
            foreach (var room in layout.Rooms) // 생성된 방 정의 순회
            { // 조우 방 조회 시작
                if (room.Kind == RoomKind.Elite) // 조우 방 역할 검사
                { // 조우 조건 시작
                    encounter = room; // 조우 방 정의 보관
                    break; // 조우 방 조회 종료
                } // 조우 조건 끝
            } // 조우 방 조회 끝
            if (encounter == null) // 조우 방 존재 검사
            { // 조우 누락 조건 시작
                throw new InvalidOperationException("An encounter room binding is required."); // 조우 누락 거절
            } // 조우 누락 조건 끝
            foreach (var binding in bindings) // 원형 방 연결 순회
            { // 원형 조회 시작
                if (binding.X == 0 && binding.Y == 0) // 시작 방 좌표 검사
                { // 시작 방 조건 시작
                    origin = binding.Root; // 복제 원형 보관
                } // 시작 방 조건 끝
                if (binding.X == encounter.X && binding.Y == encounter.Y) // 기존 조우 좌표 검사
                { // 일치 조건 시작
                    exact = binding.Root; // 해당 좌표의 원형 보관
                } // 일치 조건 끝
            } // 원형 조회 끝
            var root = exact != null ? exact : origin; // 기존 방 또는 시작 방 원형 선택
            var floorObject = root == null ? null : root.transform.Find("Floor"); // 원형 바닥 객체
            var obstacleObject = root == null ? null : root.transform.Find("Obstacles"); // 원형 장애물 객체
            var floor = floorObject == null ? null : floorObject.GetComponent<Tilemap>(); // 원형 바닥 타일맵
            var obstacles = obstacleObject == null ? null : obstacleObject.GetComponent<Tilemap>(); // 원형 장애물 타일맵
            if (floor == null || obstacles == null || !floor.HasTile(Vector3Int.zero) || obstacles.HasTile(Vector3Int.zero)) // 조우 칸 통행 검사
            { // 통행 불가 조건 시작
                throw new InvalidOperationException("The encounter cell must be walkable."); // 방 변경 전 충돌 거절
            } // 통행 불가 조건 끝
        } // 검사 끝
        private void AddEncounterMarker(DungeonLayout layout, RoomBinding[] bindings) // 조우 방의 중앙 칸 표시
        { // 표시 시작
            foreach (var room in layout.Rooms) // 생성된 방 순회
            { // 방 검사 시작
                if (room.Kind != RoomKind.Elite) // 조우 방 역할 검사
                { // 다른 방 조건 시작
                    continue; // 조우 표시 제외
                } // 다른 방 조건 끝
                foreach (var binding in bindings) // 실제 방 연결 순회
                { // 연결 검사 시작
                    if (binding.X != room.X || binding.Y != room.Y) // 좌표 일치 검사
                    { // 다른 방 조건 시작
                        continue; // 다른 방 제외
                    } // 다른 방 조건 끝
                    var root = binding.Root; // 조우 방 객체
                    var floor = root.transform.Find("Floor").GetComponent<Tilemap>(); // 바닥 타일맵
                    var obstacles = root.transform.Find("Obstacles").GetComponent<Tilemap>(); // 장애물 타일맵
                    if (!floor.HasTile(Vector3Int.zero) || obstacles.HasTile(Vector3Int.zero)) // 조우 칸 통행 검사
                    { // 충돌 조건 시작
                        throw new InvalidOperationException("The encounter cell must be walkable."); // 잘못된 조우 위치 거절
                    } // 충돌 조건 끝
                    var marker = new GameObject("EncounterMarker", typeof(SpriteRenderer)); // 조우 표시 객체
                    marker.transform.SetParent(root.transform, false); // 조우 방 아래 배치
                    var grid = root.GetComponent<Grid>(); // 방 격자 조회
                    var anchor = floor.tileAnchor; // 바닥 칸 중심 기준
                    var size = grid.cellSize; // 격자 칸 크기
                    marker.transform.position = floor.transform.TransformPoint(new Vector3(anchor.x * size.x, anchor.y * size.y, anchor.z * size.z)); // 중앙 칸의 화면 위치
                    marker.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); // 아래층 방향 표시
                    var sprite = marker.GetComponent<SpriteRenderer>(); // 조우 표시 렌더러
                    sprite.sprite = player.transform.Find("DirectionIndicator").GetComponent<SpriteRenderer>().sprite; // 기존 화살표 이미지 사용
                    sprite.color = new Color(1f, 0.3f, 0.72f); // 파란 조우 색상
                    sprite.sortingOrder = 3; // 바닥 위 표시
                    return; // 조우 표시 완료
                } // 연결 검사 끝
            } // 방 검사 끝
            throw new InvalidOperationException("An encounter room binding is required."); // 조우 방 누락 거절
        } // 표시 끝
        private void TryNextFloor() // 준비 성공 시에만 다음 층 확정
        { // 전환 시작
            if (Floor >= 2) // 합의된 마지막 층 검사
            { // 추가 층 미정 조건 시작
                return; // 확인되지 않은 층 이동 차단
            } // 추가 층 미정 조건 끝
            if (!floorProgress.TryBegin()) // 전환 잠금 검사
            { // 전환 거절 조건 시작
                return; // 중복 요청 무시
            } // 전환 거절 조건 끝
            lastTransitionFrame = Time.frameCount; // 같은 프레임 재요청 차단
            GameObject stage = null; // 준비 중인 새 층 객체
            try // 다음 층의 방과 시작 위치 준비
            { // 준비 시작
                var nextSeed = unchecked(Seed + floorProgress.Floor * 7919); // 층별 재현 가능한 새 시드
                var nextLayout = DungeonGenerator.Generate(nextSeed); // 임시 1층 규칙으로 새 층 생성
                var nextDiscovery = new DungeonDiscovery(nextLayout, 0, 0); // 새 층의 독립 방문 기록
                stage = new GameObject("Floor_" + (Floor + 1)); // 새 층의 상위 객체
                stage.transform.SetParent(transform, false); // 씬 종료 시 함께 정리
                stage.SetActive(false); // 준비 중 화면 노출 방지
                var originals = new RoomBinding[templates.Length]; // 새 층의 원형 방 연결
                for (var index = 0; index < templates.Length; index++) // 보관된 원형 순회
                { // 복제 시작
                    var template = templates[index]; // 원래 방 원형
                    var copy = Instantiate(template.Root, stage.transform); // 새 층 전용 방 복제
                    originals[index] = new RoomBinding(template.X, template.Y, template.IsSecret, copy); // 원형 좌표 연결
                } // 복제 끝
                ValidateStairRoom(nextLayout, originals); // 방을 변경하기 전 계단 칸 검사
                ValidateEncounterRoom(nextLayout, originals); // 방을 변경하기 전 조우 칸 검사
                var nextRooms = GeneratedRoomFactory.Create(nextLayout, originals, templateDoors, player); // 검증된 새 층 방과 출구 생성
                foreach (var room in nextRooms) // 새 층의 방 순회
                { // 상위 객체 연결 시작
                    room.Root.transform.SetParent(stage.transform, true); // 생성된 방의 일괄 관리
                } // 상위 객체 연결 끝
                AddStairMarker(nextLayout, nextRooms); // 새 층 계단 표시 생성
                AddEncounterMarker(nextLayout, nextRooms); // 새 층 시험 대상 표시 생성
                var start = Array.Find(nextRooms, room => room.X == 0 && room.Y == 0); // 새 층 시작 방 조회
                var floor = start.Root.transform.Find("Floor").GetComponent<Tilemap>(); // 시작 방 바닥
                var obstacles = start.Root.transform.Find("Obstacles").GetComponent<Tilemap>(); // 시작 방 장애물
                if (!player.CanArrive(floor, obstacles, 0, 0)) // 시작 칸 안전 검사
                { // 도착 불가 조건 시작
                    throw new InvalidOperationException("The next floor start is blocked."); // 기존 층 유지
                } // 도착 불가 조건 끝
                stage.SetActive(true); // 새 층 시작 방 활성화
                if (!player.ChangeRoom(floor, obstacles, 0, 0)) // 활성화 후 도착 칸 재검사
                { // 도착 실패 조건 시작
                    throw new InvalidOperationException("The next floor start changed during activation."); // 기존 층 유지
                } // 도착 실패 조건 끝
                foreach (var room in rooms) // 이전 층의 방 순회
                { // 이전 방 정리 시작
                    room.Root.SetActive(false); // 이전 층 즉시 숨김
                    Destroy(room.Root); // 이전 층 방 제거
                } // 이전 방 정리 끝
                if (currentFloorHolder != null) // 이전 층 상위 객체 검사
                { // 상위 객체 정리 시작
                    Destroy(currentFloorHolder); // 이전 층 상위 객체 제거
                } // 상위 객체 정리 끝
                currentFloorHolder = stage; // 새 층 객체 보관
                stage = null; // 성공한 층의 실패 정리 방지
                rooms = nextRooms; // 새 층의 실제 방 연결
                Layout = nextLayout; // 새 층의 방 구조 확정
                Discovery = nextDiscovery; // 새 층의 방문 기록 확정
                state = new RoomTransitionState(0, 0); // 새 층 시작 방 좌표
                floorProgress.Complete(true); // 다음 층 번호 확정
                AppRoot.Instance.UpdatePlayerHealth(100); // 성공한 층 이동의 기본 체력 회복
            } // 준비 끝
            catch (Exception exception) // 새 층 준비 실패 처리
            { // 실패 처리 시작
                if (stage != null) // 준비 중인 층 존재 검사
                { // 실패한 층 정리 시작
                    stage.SetActive(false); // 실패한 방 즉시 숨김
                    Destroy(stage); // 준비 중인 방 제거
                } // 실패한 층 정리 끝
                floorProgress.Complete(false); // 기존 층 번호 유지와 잠금 해제
                Debug.LogWarning("Floor transition failed: " + exception.Message); // 실패 원인 기록
                return; // 실패한 전환의 화면 갱신 제외
            } // 실패 처리 끝
            RoomChanged?.Invoke(); // 확정된 층의 지도와 표시 갱신
        } // 전환 끝
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
