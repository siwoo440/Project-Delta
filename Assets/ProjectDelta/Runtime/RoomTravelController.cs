using System; // 방 변경 알림과 구성 오류
using System.Collections; // 방 전환 완료 대기
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
        private DoorLockState doorLocks; // 현재 층의 시험 문 잠금
        private RoomDoor entryDoor; // 현재 방으로 진입한 양방향 연결
        private readonly System.Random forceRandom = new System.Random(); // 시험 강제 개방 주사위
        private const int TrialForceAttack = 50; // 기존 기본 공격력의 시험 적용
        private const int TrialForceDifficulty = 50; // 아직 미정인 문의 시험 난이도
        private int lastTransitionFrame = -1; // 같은 프레임 중복 전환 차단
        private int generationVersion = 3; // 현재 회차의 던전 생성 규칙
        private readonly List<FloorRoomContent> roomContents = new List<FloorRoomContent>(); // 방문한 층의 상자와 함정 상태
        private FloorRoomContent currentContent; // 현재 층의 시험 방 콘텐츠
        public static bool? RoomContentTestOverride { get; set; } // 자동 검증용 시험 콘텐츠 활성 설정
        public bool RoomContentEnabled { get; private set; } // 현재 회차의 상자와 함정 활성 여부
        public FloorRoomContent CurrentContent => currentContent; // 현재 층 콘텐츠 조회
        public static bool? TrialEventTestOverride { get; set; } // 자동 검증의 이벤트 활성 설정
        public bool TrialEventsEnabled { get; private set; } // 현재 회차의 이벤트 활성 여부
        private readonly List<SavedFloorTrialEvent> trialEvents = new List<SavedFloorTrialEvent>(); // 층별 시험 이벤트 기록
        public DungeonRoom TrialEventRoom => CurrentTrialEventRecord == null ? null : Layout.GetRoom(CurrentTrialEventRecord.x, CurrentTrialEventRecord.y); // 현재 층의 시험 이벤트 방
        public SavedFloorTrialEvent CurrentTrialEventRecord => trialEvents.Find(value => value.floor == Floor); // 현재 층 이벤트 기록
        public bool IsTrialEventPending => IsReady && TrialEventRoom != null && RoomX == TrialEventRoom.X && RoomY == TrialEventRoom.Y && CurrentTrialEventRecord.eventData.phase < 2 && !HasCompletedTrialEvent(); // 현재 방의 미완료 시험 이벤트
        public bool IsTrapPending => IsReady && currentContent != null && currentContent.TrapRoom.X == RoomX && currentContent.TrapRoom.Y == RoomY && !currentContent.Snapshot().trapCompleted; // 현재 방의 미처리 함정 여부
        private bool tutorialCompleted; // 첫 슬라임 조우 완료 여부
        private int tutorialHealth; // 도망한 슬라임의 남은 체력
        private EncounterPlacement tutorialEncounter; // 첫 일반 방의 고정 조우 데이터
        private DungeonRoom groupEncounterRoom; // 시험 다수 조우가 배치된 일반 방
        private bool groupEncounterEnabled; // 현재 회차의 시험 다수 조우 활성화
        private bool groupGoblin; // 현재 회차 다수 조우의 고블린 종족 여부
        private bool groupEncounterCompleted; // 시험 다수 조우 완료 상태
        private int[] groupEnemyHealth; // 후퇴한 시험 적의 개별 체력
        public static bool? TutorialEncounterTestOverride { get; set; } // 독립 테스트의 첫 조우 활성화 설정
        public bool TutorialCompleted => tutorialCompleted; // 첫 조우 완료 상태
        private bool TutorialEnabled => TutorialEncounterTestOverride ?? true; // 실제 게임의 첫 조우 활성 상태
        public EncounterPlacement TutorialEncounter => tutorialEncounter; // 첫 일반 방의 실제 조우 데이터
        public string CurrentMonsterId => IsMimicEncounter ? "MON_MIMIC" : IsGroupRoom() ? (groupGoblin ? "MON_GOBLIN" : "MON_SLIME") : IsTutorialRoom() && !tutorialCompleted ? "MON_SLIME" : "trial-elite"; // 미믹을 우선한 시험 조우 식별자
        public string CurrentMonsterName => IsMimicEncounter ? "미믹" : IsGroupRoom() ? (groupGoblin ? "고블린 2개체" : "슬라임 2개체") : IsTutorialRoom() && !tutorialCompleted ? tutorialEncounter.Monsters[0].Name : "상위 개체"; // 미믹을 우선한 시험 대상 이름
        public bool IsMimicEncounter // 현재 상자의 미완료 미믹 여부
        { // 속성 시작
            get // 실제 상자 상태 조회
            { // 조회 시작
                if (!IsReady || currentContent == null || !currentContent.TryGetChest(RoomX, RoomY, out var chest)) // 현재 방과 상자 검사
                { // 상자 없음 시작
                    return false; // 미믹 조우 아님
                } // 상자 없음 끝
                var saved = chest.Snapshot(); // 실제 상자와 미믹 상태
                return saved.mimicState == 2 && !saved.mimicDefeated && !saved.completed; // 살아 있는 미믹 조우
            } // 조회 끝
        } // 속성 끝
        public bool IsCurrentMimic => IsMimicEncounter; // 미믹 승리 처리를 위한 현재 조우 여부
        public bool IsGroupEncounter => IsGroupRoom() && !groupEncounterCompleted; // 현재 미완료 다수 조우 여부
        public DungeonRoom GroupEncounterRoom => groupEncounterRoom; // 시험 다수 조우 방 조회
        private readonly FloorProgress floorProgress = new FloorProgress(); // 현재 층과 전환 잠금
        private readonly HashSet<string> completedEncounters = new HashSet<string>(); // 층별 완료 조우 기록
        private readonly Dictionary<string, int> encounterHealth = new Dictionary<string, int>(); // 층과 방별 후퇴한 시험 적의 체력
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
        public static Func<int, int, int> ForceRollOverride // 자동 검증용 이벤트 주사위 지정
        { // 속성 시작
            get; // 지정 주사위 조회
            set; // 검증 주사위 설정
        } // 속성 끝
        public event Action RoomChanged; // 성공한 방 이동 알림
        public event Action DiscoveryChanged; // 비밀 통로 발견과 개방 알림
        public event Action<string> DoorNoticeChanged; // 문 상태 안내 변경 알림
        public RoomDoor TrialLockedDoor => doorLocks?.TrialDoor; // 현재 층의 시험 잠금 문
        public bool IsDoorLocked(RoomDoor door) // 문 연결의 잠금 여부
        { // 조회 시작
            return doorLocks != null && doorLocks.IsLocked(door); // 양방향 잠금 조회
        } // 조회 끝
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
        public RunSaveData CaptureRun() // 확정된 탐험 상태의 저장본
        { // 탐험 저장 시작
            return CaptureRun(false); // 일반 탐험과 메뉴 상태 검사
        } // 탐험 저장 끝
        public RunSaveData CaptureBattleRewardRun() // 승리 보상 준비를 위한 저장본
        { // 보상 저장 시작
            return CaptureRun(true); // 실제 전투 중 안정된 위치 허용
        } // 보상 저장 끝
        public RunSaveData CaptureInventoryRun() // 장비 변경의 안정된 저장본
        { // 인벤토리 저장 시작
            return CaptureRun(false, true); // 실제 인벤토리 상태 허용
        } // 인벤토리 저장 끝
        private RunSaveData CaptureRun(bool allowCombat, bool allowInventory = false) // 확정된 현재 위치의 저장본
        { // 저장본 생성 시작
            if (!IsReady || player.IsMoving || player.IsRoomTransitionLocked || floorProgress.IsTransitioning || AppRoot.Instance == null || (AppRoot.Instance.Flow.Phase != GamePhase.Exploration && AppRoot.Instance.Flow.Phase != GamePhase.Pause && (!allowCombat || AppRoot.Instance.Flow.Phase != GamePhase.Combat) && (!allowInventory || AppRoot.Instance.Flow.Phase != GamePhase.Inventory))) // 안정된 탐험과 승리 전투 상태 검사
            { // 저장 불가 조건 시작
                return null; // 이동이나 다른 화면의 저장 차단
            } // 저장 불가 조건 끝
            var lockData = doorLocks.Save(); // 확정된 시험 문 상태
            if (lockData == null) // 예약 중인 문 검사
            { // 예약 조건 시작
                return null; // 미확정 문 저장 차단
            } // 예약 조건 끝
            var elite = new List<DungeonRoom>(Layout.Rooms).Find(room => room.Kind == RoomKind.Elite); // 현재 층 시험 조우 방
            var eliteKey = elite == null ? string.Empty : Floor + ":" + elite.X + ":" + elite.Y; // 현재 층 조우 식별자
            encounterHealth.TryGetValue(eliteKey, out var enemyHealth); // 후퇴한 시험 적 체력
            if (completedEncounters.Contains(eliteKey)) // 완료한 조우의 이전 체력 검사
            { // 완료 조건 시작
                enemyHealth = 0; // 완료한 대상의 이전 체력 제외
            } // 완료 조건 끝
            return new RunSaveData // 현재 회차의 독립 데이터
            { // 저장 데이터 시작
                version = RunSaveData.CurrentVersion, // 회차 형식 버전
                runId = AppRoot.Instance.CurrentRunId, // 현재 회차의 고유 식별자
                level = AppRoot.Instance.Level, // 현재 회차 레벨
                experience = AppRoot.Instance.Experience, // 현재 레벨의 진행 경험치
                statPoints = AppRoot.Instance.StatPoints, // 분배하지 않은 스탯 포인트
                gold = AppRoot.Instance.Gold, // 현재 회차 골드
                rewardReceipts = (SavedRewardReceipt[])AppRoot.Instance.RewardReceipts.Clone(), // 완료한 보상 거래 복사
                seed = Seed, // 최초 던전 생성 시드
                generationVersion = generationVersion, // 현재 회차의 생성 규칙
                tutorialCompleted = tutorialCompleted, // 첫 슬라임 조우 완료 상태
                tutorialHealth = tutorialHealth, // 도망 후 남은 첫 슬라임 체력
                groupEncounterEnabled = groupEncounterEnabled, // 시험 다수 조우 활성 상태
                groupGoblin = groupGoblin, // 현재 다수 조우의 고블린 종족 여부
                groupEncounterCompleted = groupEncounterCompleted, // 시험 다수 조우 완료 상태
                groupEnemyHealth = Floor == 1 && groupEnemyHealth != null ? (int[])groupEnemyHealth.Clone() : new int[0], // 현재 층의 후퇴한 다수 적 체력
                secretPassages = Discovery.SaveSecrets(), // 발견한 비밀 연결과 개방 상태
                layoutFingerprint = DungeonLayoutFingerprint.Compute(Layout), // 저장 당시 현재 층의 구조 지문
                floor = Floor, // 현재 층 번호
                roomX = state.RoomX, // 현재 방 가로 좌표
                roomY = state.RoomY, // 현재 방 세로 좌표
                playerX = player.X, // 플레이어 가로 칸
                playerY = player.Y, // 플레이어 세로 칸
                facing = (int)player.Facing, // 현재 바라보는 방향
                health = AppRoot.Instance.PlayerHealth, // 플레이어 현재 체력
                maxHealth = 100, // 시험 최대 체력
                mana = AppRoot.Instance.PlayerMana, // 현재 회차 마나
                maxMana = 50, // 기획 기본 최대 마나
                keys = AppRoot.Instance.Keys.Count, // 시험 열쇠 수량
                slots = AppRoot.Instance.Inventory.SaveSlots(), // 아이템 칸과 수량
                weaponId = AppRoot.Instance.Equipment.Get(EquipmentSlot.Weapon)?.Id, // 장착한 무기 식별자
                armorId = AppRoot.Instance.Equipment.Get(EquipmentSlot.Armor)?.Id, // 장착한 방어구 식별자
                accessoryId = AppRoot.Instance.Equipment.Get(EquipmentSlot.Accessory)?.Id, // 장착한 장신구 식별자
                bagId = AppRoot.Instance.Equipment.Get(EquipmentSlot.Bag)?.Id, // 장착한 가방 식별자
                weaponVariant = AppRoot.Instance.Equipment.GetVariant(EquipmentSlot.Weapon), // 장착한 무기의 개별 특성
                armorVariant = AppRoot.Instance.Equipment.GetVariant(EquipmentSlot.Armor), // 장착한 방어구의 개별 특성
                accessoryVariant = AppRoot.Instance.Equipment.GetVariant(EquipmentSlot.Accessory), // 장착한 장신구의 개별 특성
                bagVariant = AppRoot.Instance.Equipment.GetVariant(EquipmentSlot.Bag), // 장착한 가방의 개별 특성
                relicIds = AppRoot.Instance.Relics.SaveIds(), // 현재 회차의 유물 목록
                shopStocks = AppRoot.Instance.ShopStocksSnapshot(), // 방문한 층의 상품과 재고
                roomContentEnabled = RoomContentEnabled, // 현재 회차의 시험 방 콘텐츠 여부
                roomContents = RoomContentSnapshots(), // 방문한 층의 상자와 함정 기록
                trialEventsEnabled = TrialEventsEnabled, // 현재 회차의 시험 이벤트 활성 여부
                trialEvents = TrialEventSnapshots(), // 층별 고정 이벤트와 결과
                visitedRooms = Discovery.SaveVisited(), // 현재 층의 방문 기록
                doorLock = lockData, // 현재 층의 잠금 기록
                hasPreviousRoom = HasPreviousRoom, // 이전 방의 연결 여부
                previousRoomX = HasPreviousRoom ? entryDoor.RoomX : 0, // 이전 방 가로 좌표
                previousRoomY = HasPreviousRoom ? entryDoor.RoomY : 0, // 이전 방 세로 좌표
                encounterCompleted = completedEncounters.Contains(eliteKey), // 현재 층 조우 완료 여부
                encounterHealth = enemyHealth, // 후퇴한 상대 체력
                history = new List<string>(AppRoot.Instance.ExplorationHistory.Entries).ToArray() // 회차 탐험 기록 복사
            }; // 저장 데이터 끝
        } // 저장본 생성 끝
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
        public bool HasPreviousRoom // 현재 방에 들어오기 전 방 존재 여부
        { // 속성 시작
            get // 이전 연결 조회
            { // 조회 시작
                return state != null && entryDoor != null && entryDoor.TargetRoomX == state.RoomX && entryDoor.TargetRoomY == state.RoomY && FindRoom(entryDoor.RoomX, entryDoor.RoomY) != null; // 현재 방의 유효한 이전 연결
            } // 조회 끝
        } // 속성 끝
        public bool CanReturnToPreviousRoom() // 이전 방과 도착 칸의 현재 안전성 검사
        { // 검사 시작
            if (!IsReady || player.IsMoving || !HasPreviousRoom || Layout.GetConnection(state.RoomX, state.RoomY, entryDoor.RoomX, entryDoor.RoomY) == null) // 현재 위치와 역방향 연결 검사
            { // 복귀 불가 조건 시작
                return false; // 없는 이전 방으로 이동 차단
            } // 복귀 불가 조건 끝
            var destination = FindRoom(entryDoor.RoomX, entryDoor.RoomY); // 이전 방 씬 객체
            var floorObject = destination.transform.Find("Floor"); // 이전 방 바닥 조회
            var obstacleObject = destination.transform.Find("Obstacles"); // 이전 방 장애물 조회
            if (floorObject == null || obstacleObject == null) // 필수 타일맵 검사
            { // 누락 조건 시작
                return false; // 도착 칸 검사 불가
            } // 누락 조건 끝
            return player.CanArrive(floorObject.GetComponent<Tilemap>(), obstacleObject.GetComponent<Tilemap>(), entryDoor.ApproachX, entryDoor.ApproachY); // 이전 출구 앞 칸의 안전성
        } // 검사 끝
        public bool TryReturnToPreviousRoom() // 성공한 조우 도망의 안전한 방 복귀
        { // 복귀 시작
            if (AppRoot.Instance == null || (AppRoot.Instance.Flow.Phase != GamePhase.Encounter && AppRoot.Instance.Flow.Phase != GamePhase.Combat) || !CanReturnToPreviousRoom()) // 조우 상태와 안전한 이전 방 검사
            { // 복귀 거절 조건 시작
                return false; // 잘못된 방 이동 차단
            } // 복귀 거절 조건 끝
            var previous = FindRoom(entryDoor.RoomX, entryDoor.RoomY); // 실제 이전 방
            var current = FindRoom(state.RoomX, state.RoomY); // 현재 조우 방
            if (current == null) // 현재 방 존재 검사
            { // 방 누락 조건 시작
                return false; // 잘못된 현재 방 이동 차단
            } // 방 누락 조건 끝
            var reverse = Layout.GetConnection(state.RoomX, state.RoomY, entryDoor.RoomX, entryDoor.RoomY); // 돌아갈 역방향 출구
            var floor = previous.transform.Find("Floor").GetComponent<Tilemap>(); // 이전 방 바닥
            var obstacles = previous.transform.Find("Obstacles").GetComponent<Tilemap>(); // 이전 방 장애물
            var wasActive = previous.activeSelf; // 이전 방의 기존 활성 상태
            previous.SetActive(true); // 도착 방 활성화
            if (!player.ChangeRoom(floor, obstacles, entryDoor.ApproachX, entryDoor.ApproachY)) // 활성화 후 도착 칸 재검사
            { // 도착 실패 조건 시작
                previous.SetActive(wasActive); // 이전 방의 활성 상태 복구
                return false; // 현재 방과 플레이어 상태 보존
            } // 도착 실패 조건 끝
            current.SetActive(false); // 출발한 조우 방 숨김
            state = new RoomTransitionState(entryDoor.RoomX, entryDoor.RoomY); // 이전 방 좌표 확정
            Discovery.TryVisit(state.RoomX, state.RoomY); // 실제 복귀한 방의 지도 갱신
            entryDoor = reverse; // 복귀한 방의 이전 연결 보관
            lastTransitionFrame = Time.frameCount; // 같은 프레임의 중복 이동 차단
            DoorNoticeChanged?.Invoke(string.Empty); // 이전 문의 안내 제거
            BeginRoomTransition(reverse.Direction); // 이전 방으로 부드러운 화면 이동 시작
            RoomChanged?.Invoke(); // 실제 방 복귀의 화면 갱신
            return true; // 안전한 이전 방 복귀 성공
        } // 복귀 끝
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
                var saved = AppRoot.Instance.PendingRun; // 복원 대기 중인 회차
                Seed = saved == null ? GenerationSeedOverride ?? Guid.NewGuid().GetHashCode() : saved.seed; // 새 게임 또는 저장된 시드
                generationVersion = saved == null ? 3 : saved.version < 3 ? 1 : saved.generationVersion; // 새 회차 또는 저장된 생성 규칙
                tutorialCompleted = saved != null && (saved.version < 4 || saved.tutorialCompleted); // 이전 회차와 저장된 첫 조우 완료
                tutorialHealth = saved == null || saved.version < 4 ? 0 : saved.tutorialHealth; // 저장된 첫 조우 상대 체력
                groupEncounterEnabled = saved == null ? TutorialEnabled : saved.version >= 6 && saved.groupEncounterEnabled; // 새 회차 또는 저장된 다수 조우 활성 상태
                groupGoblin = groupEncounterEnabled && (saved == null ? GroupEncounterSpeciesRules.ForNewRun() : GroupEncounterSpeciesRules.FromSave(saved.version, saved.groupGoblin)); // 새 회차 고블린과 기존 슬라임 조우 구분
                groupEncounterCompleted = saved != null && saved.version >= 6 && saved.groupEncounterCompleted; // 저장된 다수 조우 완료 상태
                groupEnemyHealth = saved != null && saved.version >= 6 && saved.groupEnemyHealth != null && saved.groupEnemyHealth.Length > 0 ? (int[])saved.groupEnemyHealth.Clone() : null; // 저장된 개별 적 체력 복원
                var floorSeed = saved != null && saved.floor == 2 ? unchecked(Seed + 7919) : Seed; // 저장 층의 생성 시드
                Layout = DungeonGenerator.GenerateVersioned(floorSeed, generationVersion); // 저장된 생성 규칙의 현재 층 구조
                doorLocks = saved == null ? new DoorLockState(Layout) : DoorLockState.FromSaved(Layout, saved.doorLock); // 새 문 또는 저장된 문 상태
                ValidateStairRoom(Layout, rooms); // 방을 변경하기 전 계단 칸 검사
                ValidateEncounterRoom(Layout, rooms); // 방을 변경하기 전 조우 칸 검사
                rooms = GeneratedRoomFactory.Create(Layout, rooms, doors, player); // 생성된 방 구조를 실제 2D 방에 연결
                DungeonPresentation.Apply(rooms); // 생성 이미지와 두 배 타일 표시 적용
                Discovery = saved == null ? new DungeonDiscovery(Layout, 0, 0) : DungeonDiscovery.FromSaved(Layout, saved.visitedRooms, saved.roomX, saved.roomY, saved.secretPassages); // 새 기록 또는 저장한 방문 기록
                if (saved != null && (doorLocks == null || Discovery == null || !floorProgress.TryRestore(saved.floor))) // 저장된 던전 상태 검사
                { // 복원 오류 조건 시작
                    throw new InvalidOperationException("Invalid saved dungeon state."); // 잘못된 저장 복원 거절
                } // 복원 오류 조건 끝
                if (generationVersion == 3 && Floor == 1) // 첫 일반 조우가 있는 새 생성 규칙 검사
                { // 조우 데이터 준비 시작
                    foreach (var door in Layout.Doors) // 시작 방 연결 순회
                    { // 첫 연결 검사 시작
                        if (door.RoomX == 0 && door.RoomY == 0 && !door.IsSecret) // 첫 일반 연결 검사
                        { // 첫 연결 조건 시작
                            tutorialEncounter = EncounterPlacement.Create(Layout, 1, door.TargetRoomX, door.TargetRoomY, new[] { "MON_SLIME" }, true); // 검증된 첫 슬라임 배치
                            break; // 첫 조우 배치 완료
                        } // 첫 연결 조건 끝
                    } // 첫 연결 검사 끝
                    if (tutorialEncounter == null) // 첫 조우 데이터 유효성 검사
                    { // 배치 오류 시작
                        throw new InvalidOperationException("Invalid first encounter placement."); // 잘못된 첫 조우 차단
                    } // 배치 오류 끝
                    if (groupEncounterEnabled) // 시험 다수 조우 활성화 검사
                    { // 시험 방 선택 시작
                        groupEncounterRoom = TrialGroupRoomSelector.Select(Layout, tutorialEncounter.RoomX, tutorialEncounter.RoomY); // 첫 조우와 분리된 시험 일반 방
                        if (groupEncounterRoom == null) // 시험 방 선택 실패 검사
                        { // 선택 오류 시작
                            throw new InvalidOperationException("Invalid trial group encounter room."); // 잘못된 시험 배치 차단
                        } // 선택 오류 끝
                    } // 시험 방 선택 끝
                } // 조우 데이터 준비 끝
                RoomContentEnabled = saved != null && saved.version >= 14 ? saved.roomContentEnabled : RoomContentTestOverride ?? true; // 새 회차 또는 저장된 콘텐츠 활성 여부
                if (RoomContentEnabled && !InitializeRoomContents(saved)) // 층별 배치와 저장 상태 검사
                { // 콘텐츠 오류 시작
                    throw new InvalidOperationException("Invalid room content state."); // 잘못된 상자와 함정 상태 차단
                } // 콘텐츠 오류 끝
                TrialEventsEnabled = RoomContentEnabled && (saved != null && saved.version >= 15 ? saved.trialEventsEnabled : TrialEventTestOverride ?? true); // 새 회차 또는 저장된 이벤트 활성 여부
                if (TrialEventsEnabled && !InitializeTrialEvents(saved)) // 층별 시험 이벤트 준비 검사
                { // 이벤트 오류 시작
                    throw new InvalidOperationException("Invalid trial event placement."); // 기존 콘텐츠와 충돌 차단
                } // 이벤트 오류 끝
                AddContentMarkers(rooms, currentContent); // 실제 상자 방의 시험 표시 추가
                AddStairMarker(Layout, rooms); // 계단 방의 실제 표시 추가
                AddEncounterMarker(Layout, rooms); // 상위 개체 방의 시험 대상 표시
                UpdateLockMarkers(Layout, rooms, doorLocks); // 잠긴 문의 양방향 표시
                UpdateSecretMarkers(Layout, rooms, Discovery); // 발견된 비밀벽의 표시 복원
            } // 검사 끝
            catch (Exception exception) // 잘못된 방 구조와 생성 오류 처리
            { // 오류 조건 시작
                if (AppRoot.Instance.PendingRun != null) // 복원 중 실패 여부 검사
                { // 복원 실패 시작
                    AppRoot.Instance.RejectRunRestore(); // 저장 원본을 보존하고 타이틀 복귀
                } // 복원 실패 끝
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
            var pending = AppRoot.Instance.PendingRun; // 저장된 회차 복원 요청
            state = pending == null ? new RoomTransitionState(0, 0) : new RoomTransitionState(pending.roomX, pending.roomY); // 새 시작 방 또는 저장된 현재 방
            if (pending != null) // 복원할 회차 검사
            { // 복원 시작
                entryDoor = pending.hasPreviousRoom ? Layout.GetConnection(pending.previousRoomX, pending.previousRoomY, pending.roomX, pending.roomY) : null; // 저장된 이전 방 연결
                var elite = new List<DungeonRoom>(Layout.Rooms).Find(room => room.Kind == RoomKind.Elite); // 현재 층 조우 방
                var eliteKey = elite == null ? string.Empty : Floor + ":" + elite.X + ":" + elite.Y; // 현재 층 조우 식별자
                if (pending.encounterCompleted) // 완료한 조우 검사
                { // 완료 조건 시작
                    completedEncounters.Add(eliteKey); // 완료 상태 복원
                    var eliteRoot = FindRoom(elite.X, elite.Y); // 조우 방의 실제 객체
                    var marker = eliteRoot == null ? null : eliteRoot.transform.Find("EncounterMarker"); // 조우 대상 표시
                    if (marker != null) // 표시 존재 검사
                    { // 표시 정리 시작
                        marker.gameObject.SetActive(false); // 완료한 대상 숨김
                    } // 표시 정리 끝
                } // 완료 조건 끝
                if (pending.encounterHealth > 0) // 후퇴한 적의 체력 검사
                { // 체력 복원 시작
                    encounterHealth[eliteKey] = pending.encounterHealth; // 저장된 상대 체력 복원
                } // 체력 복원 끝
            } // 복원 끝
            input = AppRoot.Instance.Input; // 공통 탐험 입력 조회
        } // 초기화 끝
        private void Start() // 확대된 시작 방과 추적 카메라 연결
        { // 시작 준비 시작
            if (!IsReady) // 던전 생성 준비 검사
            { // 준비 안 됨 조건 시작
                return; // 잘못된 던전 화면 변경 차단
            } // 준비 안 됨 조건 끝
            var saved = AppRoot.Instance.PendingRun; // 복원 대기 중인 회차
            var root = FindRoom(state.RoomX, state.RoomY); // 현재 시작 방 객체
            var floor = root == null ? null : root.transform.Find("Floor").GetComponent<Tilemap>(); // 현재 방 바닥
            var obstacles = root == null ? null : root.transform.Find("Obstacles").GetComponent<Tilemap>(); // 현재 방 장애물
            var x = saved == null ? player.X : saved.playerX; // 새 게임 또는 저장된 가로 칸
            var y = saved == null ? player.Y : saved.playerY; // 새 게임 또는 저장된 세로 칸
            if (saved != null && !player.CanArrive(floor, obstacles, x, y)) // 저장된 도착 칸 사전 검사
            { // 도착 오류 시작
                Debug.LogError("Saved player position is invalid."); // 잘못된 저장 위치 기록
                AppRoot.Instance.RejectRunRestore(); // 저장 원본을 보존하고 타이틀 복귀
                enabled = false; // 유효하지 않은 복원 중단
                return; // 저장된 위치로 잘못된 이동 방지
            } // 도착 오류 끝
            if (saved != null) // 저장된 방 활성화 검사
            { // 방 활성화 시작
                foreach (var room in rooms) // 생성된 방 순회
                { // 활성 상태 갱신 시작
                    room.Root.SetActive(room.X == state.RoomX && room.Y == state.RoomY); // 저장된 현재 방만 표시
                } // 활성 상태 갱신 끝
            } // 방 활성화 끝
            if (!player.ChangeRoom(floor, obstacles, x, y)) // 확대된 현재 칸에 플레이어 배치
            { // 배치 실패 조건 시작
                if (saved != null) // 복원 중 배치 실패 검사
                { // 복원 실패 시작
                    AppRoot.Instance.RejectRunRestore(); // 저장 원본을 보존하고 타이틀 복귀
                } // 복원 실패 끝
                Debug.LogWarning("Dungeon player could not be aligned to the expanded grid."); // 배치 실패 안내
                return; // 잘못된 추적 카메라 연결 차단
            } // 배치 실패 조건 끝
            if (saved != null) // 저장된 방향 복원 검사
            { // 방향 복원 시작
                player.RestoreFacing((MoveDirection)saved.facing); // 저장된 플레이어 방향 적용
                AppRoot.Instance.FinishRunRestore(); // 성공한 복원 요청 해제
            } // 방향 복원 끝
            player.transform.localScale = Vector3.Scale(player.transform.localScale, new Vector3(2f, 2f, 1f)); // 확대된 타일에 맞춘 플레이어 크기
            DungeonPresentation.ApplyPlayer(player.gameObject); // 시험 주인공을 SD 이미지로 교체
            var camera = Camera.main; // 실제 던전 카메라 조회
            if (camera != null) // 카메라 존재 검사
            { // 카메라 연결 시작
                var motion = camera.GetComponent<DungeonCameraMotion>(); // 기존 추적 구성 조회
                if (motion == null) // 추적 구성 부재 검사
                { // 추적 구성 생성 시작
                    motion = camera.gameObject.AddComponent<DungeonCameraMotion>(); // 부드러운 추적 구성 추가
                } // 추적 구성 생성 끝
                motion.Initialize(player); // 실제 플레이어 추적 시작
            } // 카메라 연결 끝
            if (saved != null && IsTrialEventPending) // 복원된 이벤트 방의 미완료 상태 검사
            { // 복원 이벤트 시작
                StartCoroutine(OpenTrialEventAfterTransition(Floor, RoomX, RoomY)); // 저장된 선택 또는 결과 화면 복원
            } // 복원 이벤트 끝
            if (saved != null && currentContent != null && currentContent.TrapRoom.X == state.RoomX && currentContent.TrapRoom.Y == state.RoomY && !currentContent.Snapshot().trapCompleted) // 복원된 함정 방의 미처리 상태 검사
            { // 복원 함정 처리 시작
                StartCoroutine(ApplyTrapAfterTransition(Floor, state.RoomX, state.RoomY)); // 복원 후 최초 미처리 함정 판정
            } // 복원 함정 처리 끝
        } // 시작 준비 끝
        private void OnEnable() // 상호작용 입력 연결
        { // 연결 시작
            if (input != null) // 입력 준비 검사
            { // 연결 조건 시작
                input.InteractionRequested += OnInteraction; // 문 상호작용 구독
                input.ForceDoorRequested += OnForceDoor; // 강제 개방 입력 구독
                player.StepArrived += OnStepArrived; // 한 칸 도착 시 조우 검사
            } // 연결 조건 끝
        } // 연결 끝
        private void OnDisable() // 상호작용 입력 해제
        { // 해제 시작
            if (input != null) // 입력 존재 검사
            { // 해제 조건 시작
                input.InteractionRequested -= OnInteraction; // 문 상호작용 구독 해제
                input.ForceDoorRequested -= OnForceDoor; // 강제 개방 입력 해제
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
            if (TutorialEnabled && IsTutorialRoom() && !tutorialCompleted) // 미완료 첫 슬라임 조우 검사
            { // 첫 조우 시작
                AppRoot.Instance.StartEncounter(); // 슬라임 조우 선택 화면 진입
                return; // 다른 조우와 단서 중복 차단
            } // 첫 조우 끝
            if (IsGroupEncounter && tutorialCompleted) // 첫 조우 이후 시험 다수 조우 검사
            { // 다수 조우 시작
                AppRoot.Instance.StartEncounter(); // 실제 시험 다수 조우 화면 진입
                return; // 다른 조우와 중복 차단
            } // 다수 조우 끝
            var wall = Layout.FindDoor(state.RoomX, state.RoomY, player.X, player.Y, player.Facing); // 현재 바라보는 벽 조회
            if (wall != null && wall.IsSecret && !Discovery.IsSecretDiscovered(wall)) // 미발견 비밀벽의 단서 검사
            { // 단서 조건 시작
                DoorNoticeChanged?.Invoke("벽에서 희미한 흔적 발견 · E / 패드 A: 조사"); // 비밀벽 조사 단서 안내
            } // 단서 조건 끝
            if (room != null && room.Kind == RoomKind.Elite && !completedEncounters.Contains(EncounterKey()) && EncounterRange.Contains(player.X, player.Y, 0, 0)) // 대상 주변 여덟 방향 검사
            { // 자동 조우 조건 시작
                AppRoot.Instance.StartEncounter(); // 조우 선택 화면과 대기 이동 취소
            } // 자동 조우 조건 끝
        } // 검사 끝
        public SavedFloorRoomContent[] RoomContentSnapshots() // 방문한 층 콘텐츠의 독립 저장본
        { // 저장본 생성 시작
            var result = new SavedFloorRoomContent[roomContents.Count]; // 층별 독립 저장 목록
            for (var index = 0; index < result.Length; index++) // 현재 콘텐츠 순회
            { // 복사 시작
                result[index] = roomContents[index].Snapshot(); // 후보와 상태를 포함한 깊은 복사
            } // 복사 끝
            return result; // 실제 층별 상태 전달
        } // 저장본 생성 끝
        public SavedFloorRoomContent[] RoomContentSnapshotsWith(FloorRoomContent next) // 한 층을 바꾼 독립 저장본
        { // 교체 시작
            if (next == null || currentContent == null || next.Floor != Floor) // 현재 층의 유효한 변경 검사
            { // 변경 오류 시작
                return null; // 다른 층의 상태 변경 차단
            } // 변경 오류 끝
            var result = RoomContentSnapshots(); // 기존 층의 독립 복사
            for (var index = 0; index < result.Length; index++) // 저장 층 순회
            { // 대상 검사 시작
                if (result[index].floor == next.Floor) // 현재 층 일치 검사
                { // 교체 대상 시작
                    result[index] = next.Snapshot(); // 변경할 층의 독립 상태
                    return result; // 나머지 층을 보존한 목록 전달
                } // 교체 대상 끝
            } // 대상 검사 끝
            return null; // 없는 층의 변경 차단
        } // 교체 끝
        public bool ApplyRoomContent(FloorRoomContent next) // 저장된 현재 층 상태 적용
        { // 적용 시작
            if (next == null || currentContent == null || next.Floor != Floor) // 현재 층의 상태 검사
            { // 적용 오류 시작
                return false; // 다른 층의 적용 차단
            } // 적용 오류 끝
            for (var index = 0; index < roomContents.Count; index++) // 현재 층 위치 검색
            { // 위치 검사 시작
                if (roomContents[index].Floor == next.Floor) // 동일 층 검사
                { // 적용 시작
                    roomContents[index] = next; // 저장된 불변 상태 적용
                    currentContent = next; // 현재 층 참조 갱신
                    RefreshContentMarkers(); // 완료된 상자 표시 갱신
                    return true; // 실제 적용 완료
                } // 적용 끝
            } // 위치 검사 끝
            return false; // 저장되지 않은 층 차단
        } // 적용 끝
        private bool InitializeRoomContents(RunSaveData saved) // 저장된 층별 콘텐츠 또는 새 배치 준비
        { // 준비 시작
            roomContents.Clear(); // 기존 콘텐츠 상태 초기화
            if (saved != null && saved.version >= 14 && saved.roomContents != null) // 저장된 콘텐츠 목록 검사
            { // 복원 시작
                foreach (var floor in saved.roomContents) // 이전에 생성한 층 순회
                { // 층 복원 시작
                    var seed = floor.floor == 1 ? Seed : unchecked(Seed + 7919); // 층별 재현 씨드
                    var layout = DungeonGenerator.GenerateVersioned(seed, generationVersion); // 저장된 층의 던전 구조
                    var excluded = TrialRoomContentPlacement.Excluded(layout, floor.floor, generationVersion, groupEncounterEnabled); // 기존 조우 방 제외
                    if (excluded == null || !FloorRoomContent.TryRestore(floor, layout, seed, excluded, out var restored)) // 배치와 상자 상태 검사
                    { // 복원 오류 시작
                        return false; // 손상된 층 상태 차단
                    } // 복원 오류 끝
                    roomContents.Add(restored); // 검증된 층 상태 보관
                    if (floor.floor == Floor) // 현재 층 검사
                    { // 현재 층 시작
                        currentContent = restored; // 현재 방 콘텐츠 연결
                    } // 현재 층 끝
                } // 층 복원 끝
            } // 복원 끝
            if (currentContent == null) // 새 회차 또는 이전 저장의 현재 층 검사
            { // 새 콘텐츠 생성 시작
                var floorSeed = Floor == 1 ? Seed : unchecked(Seed + 7919); // 실제 현재 층 씨드
                var excluded = TrialRoomContentPlacement.Excluded(Layout, Floor, generationVersion, groupEncounterEnabled); // 기존 조우 방 제외
                if (excluded == null || !FloorRoomContent.TryCreate(Floor, Layout, floorSeed, excluded, out var created)) // 결정적 시험 콘텐츠 생성
                { // 생성 오류 시작
                    return false; // 콘텐츠 부족과 조우 중복 차단
                } // 생성 오류 끝
                currentContent = created; // 새 현재 층 상태
                roomContents.Add(created); // 새 층 보관
            } // 새 콘텐츠 생성 끝
            return true; // 모든 층 배치 준비 완료
        } // 준비 끝
        private static SavedFloorTrialEvent CopyTrialEvent(SavedFloorTrialEvent record) // 이벤트 기록의 독립 복사
        { // 복사 시작
            return record != null && TrialEventState.TryRestore(record.eventData, out var restored) ? new SavedFloorTrialEvent { floor = record.floor, x = record.x, y = record.y, eventData = restored.Snapshot() } : null; // 좌표와 검증된 독립 상태
        } // 복사 끝
        public SavedFloorTrialEvent[] TrialEventSnapshots() // 현재 회차의 독립 이벤트 기록
        { // 저장본 시작
            var result = new SavedFloorTrialEvent[trialEvents.Count]; // 독립 기록 배열
            for (var index = 0; index < result.Length; index++) // 층별 기록 순회
            { // 복사 시작
                result[index] = CopyTrialEvent(trialEvents[index]); // 검증된 기록 복사
            } // 복사 끝
            return result; // 독립 이벤트 목록 전달
        } // 저장본 끝
        public SavedFloorTrialEvent[] TrialEventSnapshotsWith(TrialEventState next) // 현재 층의 변경 상태를 포함한 독립 기록
        { // 저장본 시작
            var records = TrialEventSnapshots(); // 현재 기록 복사
            for (var index = 0; index < records.Length; index++) // 층별 기록 검사
            { // 현재 층 검색 시작
                if (records[index].floor == Floor && next != null) // 현재 층과 새 이벤트 검사
                { // 변경 시작
                    records[index].eventData = next.Snapshot(); // 변경된 이벤트 기록
                    return records; // 독립 저장본 전달
                } // 변경 끝
            } // 현재 층 검색 끝
            return null; // 현재 층 누락 차단
        } // 저장본 끝
        public bool ApplyTrialEvent(TrialEventState next) // 저장 성공 후 현재 이벤트 적용
        { // 적용 시작
            var record = CurrentTrialEventRecord; // 현재 층 이벤트
            if (record == null || next == null || next.Snapshot().eventId != record.eventData.eventId) // 대상과 식별자 검사
            { // 오류 시작
                return false; // 다른 이벤트 적용 차단
            } // 오류 끝
            record.eventData = next.Snapshot(); // 저장된 독립 상태 적용
            return true; // 실제 상태 적용 완료
        } // 적용 끝
        private bool HasCompletedTrialEvent() // 같은 회차의 동일 이벤트 완료 여부
        { // 검사 시작
            return trialEvents.Exists(value => value.eventData.phase == 2); // 모든 층의 완료 상태 검사
        } // 검사 끝
        private bool TryCreateTrialEvent(int floor, DungeonLayout layout, int seed, FloorRoomContent content, out SavedFloorTrialEvent record) // 기존 콘텐츠와 겹치지 않는 시험 이벤트 생성
        { // 생성 시작
            record = null; // 기본 생성 실패
            var excluded = TrialRoomContentPlacement.Excluded(layout, floor, generationVersion, groupEncounterEnabled); // 기존 조우 방 제외
            if (excluded == null || content == null || !TrialEventPlacement.TrySelect(layout, seed, content.Snapshot(), excluded, out var room) || !TrialEventState.TryCreate("trial-event-01", Seed & 1, out var initial)) // 결정적 방과 회차 선택 순서 검사
            { // 오류 시작
                return false; // 공간 부족과 잘못된 이벤트 차단
            } // 오류 끝
            record = new SavedFloorTrialEvent { floor = floor, x = room.X, y = room.Y, eventData = initial.Snapshot() }; // 실제 좌표와 고정 순서
            return true; // 안전한 이벤트 생성
        } // 생성 끝
        private bool InitializeTrialEvents(RunSaveData saved) // 저장과 이전 회차의 이벤트 복원
        { // 복원 시작
            if (saved != null && saved.version >= 15) // 이벤트 저장 버전 검사
            { // 저장 복원 시작
                foreach (var record in saved.trialEvents) // 저장된 이벤트 순회
                { // 복사 시작
                    var copy = CopyTrialEvent(record); // 검증된 독립 기록
                    if (copy == null) // 손상된 이벤트 검사
                    { // 오류 시작
                        return false; // 손상 상태 복원 차단
                    } // 오류 끝
                    trialEvents.Add(copy); // 복원된 층 기록 보관
                } // 복사 끝
            } // 저장 복원 끝
            if (CurrentTrialEventRecord == null) // 이전 버전 또는 새 회차 검사
            { // 생성 시작
                var seed = Floor == 1 ? Seed : unchecked(Seed + 7919); // 현재 층의 생성 씨드
                if (!TryCreateTrialEvent(Floor, Layout, seed, currentContent, out var record)) // 현재 층의 이벤트 방 검사
                { // 오류 시작
                    return false; // 다른 콘텐츠 침범 차단
                } // 오류 끝
                trialEvents.Add(record); // 현재 층 시험 이벤트 보관
            } // 생성 끝
            return true; // 이벤트 준비 완료
        } // 복원 끝
        public int CurrentEncounterHealth() // 현재 방의 후퇴한 시험 적 체력 조회
        { // 조회 시작
            if (!IsReady) // 현재 던전 준비 상태 검사
            { // 준비 안 됨 조건 시작
                return CombatScreenView.TrialEnemyHealth; // 시험 적의 기본 최대 체력
            } // 준비 안 됨 조건 끝
            if (IsMimicEncounter && currentContent.TryGetChest(RoomX, RoomY, out var mimic)) // 미완료 미믹과 저장된 상자 검사
            { // 미믹 체력 조회 시작
                var mimicHealth = mimic.Snapshot().mimicHealth; // 후퇴 후 보관된 미믹 체력
                return mimicHealth > 0 ? mimicHealth : CombatScreenView.TrialEnemyHealth; // 실제 저장 체력 또는 시험 최대 체력
            } // 미믹 체력 조회 끝
            if (IsTutorialRoom() && !tutorialCompleted) // 첫 슬라임 조우 검사
            { // 첫 조우 조건 시작
                return tutorialHealth > 0 ? tutorialHealth : CombatScreenView.TrialEnemyHealth; // 저장된 체력 또는 시험 최대 체력
            } // 첫 조우 조건 끝
            return encounterHealth.TryGetValue(EncounterKey(), out var health) ? health : CombatScreenView.TrialEnemyHealth; // 보관한 체력 또는 기본 체력
        } // 조회 끝
        public int[] CurrentGroupEnemyHealth() // 시험 다수 적의 저장 체력 조회
        { // 조회 시작
            return groupEnemyHealth == null ? new[] { 70, 70 } : (int[])groupEnemyHealth.Clone(); // 초기 체력 또는 독립 저장 체력 전달
        } // 조회 끝
        public bool StoreCurrentGroupHealth(int[] health) // 후퇴한 시험 다수 적 체력 보관
        { // 보관 시작
            if (!IsGroupEncounter || health == null || health.Length != 2) // 실제 다수 조우와 적 수 검사
            { // 잘못된 조우 시작
                return false; // 다른 조우의 체력 변경 차단
            } // 잘못된 조우 끝
            var alive = false; // 생존한 적 존재 여부
            foreach (var value in health) // 실제 적 체력 순회
            { // 체력 검사 시작
                if (value < 0 || value > CombatScreenView.TrialEnemyHealth) // 개별 체력 범위 검사
                { // 범위 오류 시작
                    return false; // 잘못된 체력 보관 차단
                } // 범위 오류 끝
                alive |= value > 0; // 생존한 적 상태 누적
            } // 체력 검사 끝
            if (!alive) // 미완료 조우의 전멸 검사
            { // 전멸 차단 시작
                return false; // 완료할 조우의 후퇴 상태 차단
            } // 전멸 차단 끝
            groupEnemyHealth = (int[])health.Clone(); // 후퇴한 개별 적 체력 보관
            return true; // 체력 보관 성공
        } // 보관 끝
        public bool StoreCurrentEncounterHealth(int health) // 후퇴 전 현재 방의 시험 적 체력 보관
        { // 보관 시작
            if (!CanCompleteCurrentEncounter() || health <= 0 || health > CombatScreenView.TrialEnemyHealth) // 미완료 조우와 생존 체력 검사
            { // 잘못된 보관 조건 시작
                return false; // 다른 방과 잘못된 적 체력 저장 차단
            } // 잘못된 보관 조건 끝
            if (IsMimicEncounter) // 상자 미믹의 후퇴 검사
            { // 미믹 체력 보관 시작
                return currentContent.TryGetChest(RoomX, RoomY, out var chest) && chest.TryStoreMimicHealth(health, out var stored) && currentContent.TrySetChest(RoomX, RoomY, stored, out var next) && AppRoot.Instance != null && AppRoot.Instance.TryPersistMimicHealth(next, out _); // 미믹 체력과 상자 상태의 원자적 저장
            } // 미믹 체력 보관 끝
            if (IsTutorialRoom() && !tutorialCompleted) // 첫 슬라임 조우 검사
            { // 첫 조우 체력 보관 시작
                tutorialHealth = health; // 도망 후 남은 체력 보관
                return true; // 첫 슬라임 체력 보관 성공
            } // 첫 조우 체력 보관 끝
            encounterHealth[EncounterKey()] = health; // 같은 층과 방의 현재 적 체력 보관
            return true; // 현재 조우의 체력 보관 완료
        } // 보관 끝
        public bool CanCompleteCurrentEncounter() // 현재 방에서 시험 조우 완료 가능 여부
        { // 검사 시작
            if (!IsReady) // 방 준비 상태 검사
            { // 준비 안 됨 조건 시작
                return false; // 완료할 수 없는 조우 차단
            } // 준비 안 됨 조건 끝
            var room = Layout.GetRoom(state.RoomX, state.RoomY); // 현재 방 역할 조회
            return room != null && (IsMimicEncounter || IsGroupEncounter || (IsTutorialRoom() && !tutorialCompleted) || (room.Kind == RoomKind.Elite && !completedEncounters.Contains(EncounterKey()))); // 미믹과 미완료 다수·첫 조우 또는 상위 방 검사
        } // 검사 끝
        public bool CompleteCurrentEncounter() // 현재 방의 시험 조우 완료
        { // 완료 시작
            if (!IsReady) // 현재 방 준비 상태 검사
            { // 준비 안 됨 조건 시작
                return false; // 잘못된 조우 완료 차단
            } // 준비 안 됨 조건 끝
            var room = Layout.GetRoom(state.RoomX, state.RoomY); // 현재 방 역할 조회
            if (IsGroupEncounter) // 미완료 시험 다수 조우 검사
            { // 다수 조우 완료 시작
                groupEncounterCompleted = true; // 시험 다수 조우 완료 보관
                groupEnemyHealth = null; // 완료한 적의 개별 체력 제거
                return true; // 다수 조우 완료
            } // 다수 조우 완료 끝
            if (room != null && IsTutorialRoom() && !tutorialCompleted) // 미완료 첫 슬라임 검사
            { // 첫 조우 완료 시작
                tutorialCompleted = true; // 첫 조우 완료 기록
                tutorialHealth = 0; // 완료한 상대의 체력 기록 제거
                return true; // 첫 슬라임 조우 완료
            } // 첫 조우 완료 끝
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
        public bool PrepareCompletedEncounter(RunSaveData data) // 현재 조우 완료를 독립 저장본에 반영
        { // 저장본 수정 시작
            if (data == null || !CanCompleteCurrentEncounter() || data.floor != Floor || data.roomX != RoomX || data.roomY != RoomY) // 현재 조우와 저장 위치 검사
            { // 완료 거절 시작
                return false; // 잘못된 조우 상태 차단
            } // 완료 거절 끝
            if (IsMimicEncounter) // 상자 미믹의 별도 승리 처리 검사
            { // 미믹 완료 차단 시작
                return false; // 일반 조우 보상으로 상자 상태 손상 방지
            } // 미믹 완료 차단 끝
            if (IsGroupEncounter) // 시험 다수 조우 검사
            { // 다수 완료 시작
                data.groupEncounterCompleted = true; // 다수 조우 완료 기록
                data.groupEnemyHealth = new int[0]; // 완료한 적의 이전 체력 제거
            } // 다수 완료 끝
            else if (IsTutorialRoom() && !tutorialCompleted) // 첫 조우 검사
            { // 첫 완료 시작
                data.tutorialCompleted = true; // 첫 조우 완료 기록
                data.tutorialHealth = 0; // 완료한 적의 이전 체력 제거
            } // 첫 완료 끝
            else // 현재 층 상위 개체 분기
            { // 상위 완료 시작
                data.encounterCompleted = true; // 상위 조우 완료 기록
                data.encounterHealth = 0; // 완료한 적의 이전 체력 제거
            } // 상위 완료 끝
            return true; // 독립 저장본 준비 완료
        } // 저장본 수정 끝
        private string EncounterKey() // 층과 방으로 조우 기록 구분
        { // 식별 시작
            return Floor + ":" + state.RoomX + ":" + state.RoomY; // 현재 층의 고유 방 식별자
        } // 식별 끝
        private bool IsTutorialRoom() // 현재 방의 첫 슬라임 조우 여부
        { // 검사 시작
            if (!TutorialEnabled || tutorialEncounter == null || Floor != 1 || state == null) // 첫 조우 활성 상태와 배치 및 층 검사
            { // 제외 조건 시작
                return false; // 다른 층과 이전 회차 제외
            } // 제외 조건 끝
            return state.RoomX == tutorialEncounter.RoomX && state.RoomY == tutorialEncounter.RoomY; // 현재 방과 고정 조우 위치 비교
        } // 검사 끝
        private bool IsGroupRoom() // 현재 방의 시험 다수 조우 여부
        { // 검사 시작
            return groupEncounterEnabled && groupEncounterRoom != null && Floor == 1 && state != null && state.RoomX == groupEncounterRoom.X && state.RoomY == groupEncounterRoom.Y; // 활성 시험 조우 방 확인
        } // 검사 끝
        private void OnForceDoor() // 정면 잠긴 문의 공격력 판정
        { // 강제 개방 시작
            if (input == null || !input.IsExplorationEnabled || state == null || player.IsMoving || player.IsRoomTransitionLocked || lastTransitionFrame == Time.frameCount) // 탐험 상태와 중복 입력 검사
            { // 입력 차단 조건 시작
                return; // 이동 중과 중복 요청 무시
            } // 입력 차단 조건 끝
            var door = Layout.FindDoor(state.RoomX, state.RoomY, player.X, player.Y, player.Facing); // 정면의 문 조회
            if (door == null || !doorLocks.IsLocked(door)) // 정면 잠긴 문 검사
            { // 잠긴 문 없음 조건 시작
                return; // 다른 위치와 열린 문 제외
            } // 잠긴 문 없음 조건 끝
            if (!doorLocks.CanForce(door)) // 이전 강제 개방 시도 검사
            { // 재시도 차단 조건 시작
                DoorNoticeChanged?.Invoke("이미 시도한 문 · 열쇠 사용 가능"); // 실패 후 열쇠 사용 안내
                return; // 두 번째 주사위 판정 차단
            } // 재시도 차단 조건 끝
            var playerRoll = ForceRollOverride == null ? forceRandom.Next(1, 21) : ForceRollOverride(1, 21); // 플레이어 이벤트 주사위
            var eventRoll = ForceRollOverride == null ? forceRandom.Next(1, 21) : ForceRollOverride(1, 21); // 잠긴 문의 이벤트 주사위
            if (!doorLocks.TryForce(door, TrialForceAttack, 0, TrialForceDifficulty, playerRoll, eventRoll, out var success)) // 강제 개방 판정 검사
            { // 판정 거절 조건 시작
                return; // 잘못된 주사위의 문 상태 보존
            } // 판정 거절 조건 끝
            lastTransitionFrame = Time.frameCount; // 같은 프레임의 중복 행동 차단
            if (success) // 강제 개방 성공 검사
            { // 성공 조건 시작
                UpdateLockMarkers(Layout, rooms, doorLocks); // 양방향 문 표시 갱신
            } // 성공 조건 끝
            var result = success ? "성공" : "실패"; // 실제 판정 결과 문구
            DoorNoticeChanged?.Invoke("강제 개방 " + result + " · 공격 50 / 난이도 50 (시험값)"); // 시험 수치와 결과 안내
        } // 강제 개방 끝
        private void OnInteraction() // 실제 문 앞 상호작용 처리
        { // 처리 시작
            if (input == null || !input.IsExplorationEnabled || state == null || player.IsMoving || player.IsRoomTransitionLocked || lastTransitionFrame == Time.frameCount) // 입력 상태와 이동 완료 검사
            { // 전환 차단 조건 시작
                return; // 중복 요청과 이동 중 입력 무시
            } // 전환 차단 조건 끝
            if (IsTrialEventPending) // 이전 저장에 실패한 이벤트 검사
            { // 이벤트 재시도 시작
                StartCoroutine(OpenTrialEventAfterTransition(Floor, RoomX, RoomY)); // 현재 방의 이벤트 화면 재시도
                return; // 이벤트 처리 전 상호작용 차단
            } // 이벤트 재시도 끝
            if (IsTrapPending) // 이전 저장에 실패한 함정 검사
            { // 함정 재시도 시작
                StartCoroutine(ApplyTrapAfterTransition(Floor, state.RoomX, state.RoomY)); // 같은 방에서 함정 저장 재시도
                return; // 함정 처리 전 상호작용 차단
            } // 함정 재시도 끝
            var currentRoom = Layout.GetRoom(state.RoomX, state.RoomY); // 현재 방 역할 조회
            if (currentRoom != null && currentRoom.Kind == RoomKind.Stairs && player.X == 0 && player.Y == 0) // 계단 칸의 상호작용 검사
            { // 계단 상호작용 시작
                TryNextFloor(); // 다음 층 생성과 이동 요청
                return; // 문 상호작용 중복 방지
            } // 계단 상호작용 끝
            if (currentContent != null && Math.Abs((long)player.X) + Math.Abs((long)player.Y) <= 1 && currentContent.TryGetChest(state.RoomX, state.RoomY, out var chest) && !chest.Snapshot().completed) // 중앙 상자와 인접 상호작용 검사
            { // 상자 상호작용 시작
                if (IsMimicEncounter) // 확정되었지만 처치하지 않은 미믹 검사
                { // 미믹 재조우 시작
                    AppRoot.Instance.StartEncounter(); // 저장된 미믹과 다시 조우
                } // 미믹 재조우 끝
                else // 일반 상자 또는 미믹 승리 후 분기
                { // 상자 확인 시작
                    AppRoot.Instance.OpenChest(); // 실제 상자 상태와 보상 화면 열기
                } // 상자 확인 끝
                return; // 같은 입력의 문 이동 차단
            } // 상자 상호작용 끝
            var door = Layout.FindDoor(state.RoomX, state.RoomY, player.X, player.Y, player.Facing); // 현재 접근 위치의 출구 조회
            if (door == null) // 정면 문 존재 검사
            { // 문 없음 조건 시작
                return; // 일치하는 출구 없음
            } // 문 없음 조건 끝
            if (door.IsSecret && !Discovery.IsSecretDiscovered(door)) // 미발견 비밀벽 검사
            { // 발견 시작
                if (Discovery.TryDiscoverSecret(door)) // 실제 비밀벽 발견 검사
                { // 발견 성공 시작
                    UpdateSecretMarkers(Layout, rooms, Discovery); // 발견한 벽 표시 갱신
                    DoorNoticeChanged?.Invoke("비밀벽을 발견했습니다 · E / 패드 A: 개방"); // 발견과 개방 안내
                    DiscoveryChanged?.Invoke(); // 발견 지도 갱신
                } // 발견 성공 끝
                return; // 발견과 방 이동 분리
            } // 발견 끝
            if (door.IsSecret && !Discovery.IsSecretOpen(door)) // 발견했지만 닫힌 비밀벽 검사
            { // 개방 시작
                if (Discovery.TryOpenSecret(door)) // 실제 비밀벽 개방 검사
                { // 개방 성공 시작
                    DoorNoticeChanged?.Invoke("비밀 통로를 열었습니다 · E / 패드 A: 이동"); // 개방과 이동 안내
                    DiscoveryChanged?.Invoke(); // 지도 갱신
                } // 개방 성공 끝
                return; // 개방과 방 이동 분리
            } // 개방 끝
            var requiresKey = doorLocks.IsLocked(door); // 현재 문의 잠금 검사
            if (requiresKey && AppRoot.Instance.Keys.Count <= 0) // 열쇠 부족 검사
            { // 열쇠 부족 조건 시작
                var hint = doorLocks.CanForce(door) ? " · F / 패드 X: 강제 개방" : " · 강제 개방 이미 시도"; // 강제 개방 가능 상태 안내
                DoorNoticeChanged?.Invoke("잠긴 문 · 열쇠: " + AppRoot.Instance.Keys.Count + hint); // 현재 열쇠와 가능 행동 안내
                return; // 잠긴 문 이동 차단
            } // 열쇠 부족 조건 끝
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
            if (requiresKey && !doorLocks.TryUnlock(door, AppRoot.Instance.Keys)) // 열쇠 예약과 잠금 해제 검사
            { // 해제 실패 조건 시작
                state = new RoomTransitionState(door.RoomX, door.RoomY); // 이전 방 좌표 복구
                return; // 실패한 이동 중단
            } // 해제 실패 조건 끝
            destination.SetActive(true); // 도착 방 표시
            if (!player.ChangeRoom(floor, obstacles, door.TargetX, door.TargetY)) // 방 활성화 후 도착 칸 재검사
            { // 도착 실패 조건 시작
                if (requiresKey && !doorLocks.Rollback(door, AppRoot.Instance.Keys)) // 예약 열쇠와 잠금 복구 검사
                { // 복구 실패 조건 시작
                    Debug.LogError("Failed to restore the reserved door key."); // 예기치 않은 상태 오류 기록
                } // 복구 실패 조건 끝
                state = new RoomTransitionState(door.RoomX, door.RoomY); // 이전 방 좌표 복구
                destination.SetActive(false); // 실패한 목적 방 표시 취소
                lastTransitionFrame = Time.frameCount; // 같은 프레임의 재시도 차단
                return; // 기존 플레이어와 방 유지
            } // 도착 실패 조건 끝
            if (requiresKey) // 잠긴 문 이동 성공 검사
            { // 잠금 해제 확정 시작
                if (!doorLocks.Commit(door, AppRoot.Instance.Keys)) // 예약 열쇠 확정 검사
                { // 확정 실패 조건 시작
                    Debug.LogError("Failed to commit the reserved door key."); // 예기치 않은 상태 오류 기록
                } // 확정 실패 조건 끝
                UpdateLockMarkers(Layout, rooms, doorLocks); // 양방향 문 표시 갱신
                DoorNoticeChanged?.Invoke("문을 열었습니다 · 열쇠: " + AppRoot.Instance.Keys.Count); // 열쇠 사용 안내
            } // 잠금 해제 확정 끝
            else // 일반 문 이동 분기
            { // 일반 이동 안내 시작
                DoorNoticeChanged?.Invoke(string.Empty); // 이전 문 안내 제거
            } // 일반 이동 안내 끝
            previous.SetActive(false); // 이전 방 표시 종료
            entryDoor = door; // 실제로 들어온 방 연결 기록
            Discovery.TryVisit(door.TargetRoomX, door.TargetRoomY); // 실제 도착한 방만 방문 기록
            lastTransitionFrame = Time.frameCount; // 같은 프레임의 재전환 차단
            BeginRoomTransition(door.Direction); // 도착 방으로 부드러운 화면 이동 시작
            RoomChanged?.Invoke(); // 실제 방 이동 성공 알림
            if (currentContent != null && currentContent.TrapRoom.X == state.RoomX && currentContent.TrapRoom.Y == state.RoomY && !currentContent.Snapshot().trapCompleted) // 처음 진입한 미완료 함정 방 검사
            { // 함정 판정 준비 시작
                StartCoroutine(ApplyTrapAfterTransition(Floor, state.RoomX, state.RoomY)); // 방 전환 종료 후 즉시 함정 처리
            } // 함정 판정 준비 끝
            if (IsTrialEventPending) // 처음 진입한 시험 이벤트 방 검사
            { // 이벤트 시작 준비
                StartCoroutine(OpenTrialEventAfterTransition(Floor, RoomX, RoomY)); // 방 전환 후 자동 화면 진입
            } // 이벤트 준비 끝
            if (TutorialEnabled && IsTutorialRoom() && !tutorialCompleted) // 첫 연결 방의 슬라임 검사
            { // 첫 조우 시작
                AppRoot.Instance.StartEncounter(); // 실제 일반 조우 선택 진입
            } // 첫 조우 끝
            else if (IsGroupEncounter && tutorialCompleted) // 첫 조우 이후 시험 다수 방 검사
            { // 다수 조우 시작
                AppRoot.Instance.StartEncounter(); // 실제 다수 조우 선택 진입
            } // 다수 조우 끝
        } // 처리 끝
        private IEnumerator OpenTrialEventAfterTransition(int expectedFloor, int x, int y) // 전환 후 시험 이벤트 자동 진입
        { // 대기 시작
            if (input != null) // 탐험 입력 검사
            { // 입력 잠금 시작
                input.SetPhase(GamePhase.Loading); // 이벤트 준비 전 이동 차단
            } // 입력 잠금 끝
            yield return null; // 같은 프레임 전환 대기
            while (AppRoot.Instance != null && AppRoot.Instance.Flow.Phase == GamePhase.Loading) // 저장 재개 전환 검사
            { // 복원 대기 시작
                yield return null; // 탐험 상태 대기
            } // 복원 대기 끝
            while (player != null && (player.IsRoomTransitionLocked || player.IsMoving)) // 방 이동 완료 검사
            { // 이동 대기 시작
                yield return null; // 다음 프레임 대기
            } // 이동 대기 끝
            if (!IsReady || Floor != expectedFloor || RoomX != x || RoomY != y || !IsTrialEventPending || AppRoot.Instance == null) // 현재 방과 미완료 상태 검사
            { // 대상 변경 시작
                yield break; // 다른 이벤트와 중복 표시 차단
            } // 대상 변경 끝
            if (!AppRoot.Instance.OpenTrialEvent(out var message)) // 이벤트의 저장과 화면 진입 검사
            { // 저장 오류 시작
                DoorNoticeChanged?.Invoke(message + " · E: 다시 시도 · ESC: 메뉴"); // 재시도와 메뉴 안내
                if (input != null && AppRoot.Instance.Flow.Phase == GamePhase.Exploration) // 탐험 입력 복구 검사
                { // 복구 시작
                    input.SetPhase(GamePhase.Exploration); // 오류 후 재시도 입력 허용
                } // 복구 끝
            } // 저장 오류 끝
        } // 대기 끝
        private IEnumerator ApplyTrapAfterTransition(int expectedFloor, int x, int y) // 전환 완료 후 실제 함정 저장과 발동
        { // 대기 시작
            if (input != null) // 함정 판정 전 입력 검사
            { // 입력 잠금 시작
                input.SetPhase(GamePhase.Loading); // 판정 전 이동 입력 잠금
            } // 입력 잠금 끝
            yield return null; // 같은 프레임의 문 전환 잠금 대기
            while (AppRoot.Instance != null && AppRoot.Instance.Flow.Phase == GamePhase.Loading) // 복원 장면 전환 검사
            { // 복원 대기 시작
                yield return null; // 탐험 상태 전환 대기
            } // 복원 대기 끝
            while (player != null && (player.IsRoomTransitionLocked || player.IsMoving)) // 화면 전환과 플레이어 이동 검사
            { // 전환 대기 시작
                yield return null; // 다음 프레임의 전환 완료 검사
            } // 전환 대기 끝
            if (!IsReady || Floor != expectedFloor || state.RoomX != x || state.RoomY != y || currentContent == null || currentContent.TrapRoom.X != x || currentContent.TrapRoom.Y != y || currentContent.Snapshot().trapCompleted || AppRoot.Instance == null) // 현재 방과 미완료 함정 검사
            { // 대상 변경 시작
                yield break; // 다른 방과 이미 완료한 함정 차단
            } // 대상 변경 끝
            if (!AppRoot.Instance.TryApplyTrap(this, out var message)) // 실제 판정과 원자적 저장 검사
            { // 저장 실패 시작
                DoorNoticeChanged?.Invoke(message + " · E: 다시 시도 · ESC: 메뉴"); // 재시도와 메뉴 안내
                if (input != null && AppRoot.Instance.Flow.Phase == GamePhase.Exploration) // 탐험 입력 복구 검사
                { // 입력 복구 시작
                    input.SetPhase(GamePhase.Exploration); // 실패 후 기존 탐험 입력 복구
                } // 입력 복구 끝
                yield break; // 저장되지 않은 결과 적용 차단
            } // 저장 실패 끝
            DoorNoticeChanged?.Invoke(message); // 실제 함정 처리 결과 안내
            if (input != null && AppRoot.Instance.Flow.Phase == GamePhase.Exploration) // 정상 탐험 상태 검사
            { // 입력 복구 시작
                input.SetPhase(GamePhase.Exploration); // 함정 완료 후 이동 입력 복구
            } // 입력 복구 끝
        } // 대기 끝
        private static void BeginRoomTransition(MoveDirection direction) // 도착 방의 부드러운 화면 전환
        { // 전환 시작
            var camera = Camera.main; // 실제 던전 카메라 조회
            var motion = camera == null ? null : camera.GetComponent<DungeonCameraMotion>(); // 현재 카메라 전환 구성
            if (motion != null) // 전환 구성 존재 검사
            { // 화면 이동 시작
                motion.BeginTransition(direction); // 출구 방향으로 화면 이동
            } // 화면 이동 끝
        } // 전환 끝
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
        private static void AddContentMarkers(RoomBinding[] bindings, FloorRoomContent content) // 상자 방에 실제 시험 상자 표시
        { // 표시 시작
            if (content == null) // 비활성 콘텐츠 검사
            { // 비활성 시작
                return; // 상자 표시 없음
            } // 비활성 끝
            foreach (var entry in content.Snapshot().chests) // 현재 층 상자 순회
            { // 상자 표시 시작
                foreach (var binding in bindings) // 실제 방 연결 순회
                { // 좌표 검사 시작
                    if (binding.X != entry.x || binding.Y != entry.y) // 다른 방 검사
                    { // 다른 방 시작
                        continue; // 다음 방 검사
                    } // 다른 방 끝
                    var floor = binding.Root.transform.Find("Floor").GetComponent<Tilemap>(); // 실제 방 바닥
                    var marker = new GameObject("ChestMarker"); // 상자 표시 묶음
                    marker.transform.SetParent(binding.Root.transform, false); // 상자 방에 표시 연결
                    marker.transform.position = floor.GetCellCenterWorld(Vector3Int.zero); // 중앙 칸에 상자 표시
                    AddChestPart(marker.transform, "ChestBody", new Vector3(0f, -0.15f, 0f), new Vector3(1.1f, 0.65f, 1f), new Color(0.40f, 0.19f, 0.07f), 3); // 나무 상자 몸체
                    AddChestPart(marker.transform, "ChestLid", new Vector3(0f, 0.25f, 0f), new Vector3(1.2f, 0.22f, 1f), new Color(0.78f, 0.54f, 0.14f), 4); // 금색 테두리의 상자 뚜껑
                    AddChestPart(marker.transform, "ChestLatch", new Vector3(0f, 0.03f, 0f), new Vector3(0.18f, 0.25f, 1f), new Color(1f, 0.79f, 0.25f), 5); // 상자 중앙 잠금 장식
                    marker.SetActive(!entry.chest.completed); // 완료된 상자 숨김
                    break; // 해당 방 표시 완료
                } // 좌표 검사 끝
            } // 상자 표시 끝
        } // 표시 끝
        private static void AddChestPart(Transform parent, string name, Vector3 offset, Vector3 size, Color color, int order) // 상자 표시의 단색 구성 요소
        { // 생성 시작
            var part = new GameObject(name, typeof(SpriteRenderer)); // 상자 모양 조각
            part.transform.SetParent(parent, false); // 상자 묶음에 연결
            part.transform.localPosition = offset; // 상자 내부 위치
            part.transform.localScale = size; // 실제 타일에 맞는 크기
            var renderer = part.GetComponent<SpriteRenderer>(); // 화면 표시 구성
            renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f); // 기본 단색 사각형 이미지
            renderer.color = color; // 나무와 금속의 시험 색상
            renderer.sortingOrder = order; // 방 바닥 위 표시 순서
        } // 생성 끝
        private void RefreshContentMarkers() // 완료한 상자 표시의 실제 상태 반영
        { // 갱신 시작
            if (currentContent == null || rooms == null) // 현재 콘텐츠와 방 검사
            { // 누락 시작
                return; // 갱신할 상자 없음
            } // 누락 끝
            foreach (var entry in currentContent.Snapshot().chests) // 현재 층 상자 순회
            { // 표시 갱신 시작
                var root = FindRoom(entry.x, entry.y); // 실제 상자 방 객체
                var marker = root == null ? null : root.transform.Find("ChestMarker"); // 상자 표시 조회
                if (marker != null) // 실제 표시 검사
                { // 표시 적용 시작
                    marker.gameObject.SetActive(!entry.chest.completed); // 완료한 상자 숨김
                } // 표시 적용 끝
            } // 표시 갱신 끝
        } // 갱신 끝
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
        private static void UpdateSecretMarkers(DungeonLayout layout, RoomBinding[] bindings, DungeonDiscovery discovery) // 발견된 비밀벽 표시 갱신
        { // 표시 갱신 시작
            foreach (var binding in bindings) // 현재 층의 실제 방 순회
            { // 방 순회 시작
                foreach (var marker in binding.Root.GetComponentsInChildren<SecretDoorMarker>(true)) // 비밀벽 표시 순회
                { // 표시 순회 시작
                    var door = layout.GetConnection(marker.RoomX, marker.RoomY, marker.TargetX, marker.TargetY); // 표시의 실제 비밀 연결
                    marker.gameObject.SetActive(discovery.IsSecretDiscovered(door)); // 발견한 연결만 표시
                } // 표시 순회 끝
            } // 방 순회 끝
        } // 표시 갱신 끝
        private static void UpdateLockMarkers(DungeonLayout layout, RoomBinding[] bindings, DoorLockState locks) // 시험 문의 양방향 표시 갱신
        { // 표시 갱신 시작
            var first = locks.TrialDoor; // 시험 문 조회
            if (first == null) // 시험 문 존재 검사
            { // 시험 문 없음 조건 시작
                return; // 표시할 문 없음
            } // 시험 문 없음 조건 끝
            var reverse = layout.GetConnection(first.TargetRoomX, first.TargetRoomY, first.RoomX, first.RoomY); // 반대 방향 문 조회
            UpdateLockMarker(bindings, first, locks.IsLocked(first)); // 정방향 표시 갱신
            UpdateLockMarker(bindings, reverse, locks.IsLocked(reverse)); // 역방향 표시 갱신
        } // 표시 갱신 끝
        private static void UpdateLockMarker(RoomBinding[] bindings, RoomDoor door, bool locked) // 한쪽 문 표시 색상 갱신
        { // 표시 갱신 시작
            if (door == null) // 문 존재 검사
            { // 문 누락 조건 시작
                return; // 누락된 문 제외
            } // 문 누락 조건 끝
            var binding = Array.Find(bindings, room => room.X == door.RoomX && room.Y == door.RoomY); // 출발 방 연결 조회
            if (binding == null || binding.Root == null) // 실제 방 존재 검사
            { // 방 누락 조건 시작
                return; // 누락된 방 제외
            } // 방 누락 조건 끝
            var floor = binding.Root.transform.Find("Floor").GetComponent<Tilemap>(); // 방 바닥 조회
            var grid = binding.Root.GetComponent<Grid>(); // 방 격자 조회
            var offset = Vector3Int.zero; // 문의 방향별 칸 차이
            switch (door.Direction) // 문 방향 선택
            { // 방향 선택 시작
                case MoveDirection.Up: // 위쪽 문
                    offset = Vector3Int.up; // 위쪽 칸 차이
                    break; // 방향 선택 종료
                case MoveDirection.Down: // 아래쪽 문
                    offset = Vector3Int.down; // 아래쪽 칸 차이
                    break; // 방향 선택 종료
                case MoveDirection.Left: // 왼쪽 문
                    offset = Vector3Int.left; // 왼쪽 칸 차이
                    break; // 방향 선택 종료
                case MoveDirection.Right: // 오른쪽 문
                    offset = Vector3Int.right; // 오른쪽 칸 차이
                    break; // 방향 선택 종료
            } // 방향 선택 끝
            var cell = new Vector3Int(door.ApproachX, door.ApproachY, 0) + offset; // 문 표시 칸
            var size = grid.cellSize; // 격자 칸 크기
            var gap = grid.cellGap; // 격자 칸 간격
            var anchor = floor.tileAnchor; // 바닥 타일 중심
            var local = new Vector3((cell.x + anchor.x) * size.x + cell.x * gap.x, (cell.y + anchor.y) * size.y + cell.y * gap.y, (cell.z + anchor.z) * size.z + cell.z * gap.z); // 문 칸의 지역 좌표
            var position = floor.transform.TransformPoint(local); // 문 칸의 실제 위치
            foreach (Transform child in binding.Root.transform) // 방의 표시 객체 순회
            { // 문 표시 검사 시작
                if (child.name == "DoorMarker" && child.gameObject.activeSelf && Vector3.Distance(child.position, position) < 0.01f) // 해당 문 표시 검사
                { // 일치 조건 시작
                    var renderer = child.GetComponent<SpriteRenderer>(); // 실제 문 표시 렌더러
                    var generated = renderer.sprite != null && renderer.sprite.texture != null && renderer.sprite.texture.name == "DungeonDoor"; // 생성 문 이미지 검사
                    renderer.color = locked ? new Color(0.68f, 0.42f, 1f) : (generated ? Color.white : new Color(1f, 0.88f, 0.2f)); // 잠금과 문 이미지에 따른 색상
                } // 일치 조건 끝
            } // 문 표시 검사 끝
        } // 표시 갱신 끝
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
                var nextLayout = DungeonGenerator.GenerateVersioned(nextSeed, generationVersion); // 현재 회차 규칙으로 새 층 생성
                var nextLocks = new DoorLockState(nextLayout); // 새 층의 독립 문 잠금
                var nextDiscovery = new DungeonDiscovery(nextLayout, 0, 0); // 새 층의 독립 방문 기록
                FloorRoomContent nextContent = null; // 다음 층의 시험 콘텐츠
                if (RoomContentEnabled) // 방 콘텐츠 활성 검사
                { // 다음 층 콘텐츠 생성 시작
                    var excluded = TrialRoomContentPlacement.Excluded(nextLayout, Floor + 1, generationVersion, groupEncounterEnabled); // 다음 층의 기존 조우 제외
                    if (excluded == null || !FloorRoomContent.TryCreate(Floor + 1, nextLayout, nextSeed, excluded, out nextContent)) // 결정적 상자와 함정 준비 검사
                    { // 생성 오류 시작
                        throw new InvalidOperationException("Invalid next floor content."); // 기존 층을 유지한 전환 차단
                    } // 생성 오류 끝
                } // 다음 층 콘텐츠 생성 끝
                SavedFloorTrialEvent nextEvent = null; // 다음 층의 시험 이벤트 기록
                if (TrialEventsEnabled && !TryCreateTrialEvent(Floor + 1, nextLayout, nextSeed, nextContent, out nextEvent)) // 다음 층 이벤트 방 검사
                { // 오류 시작
                    throw new InvalidOperationException("Invalid next floor event."); // 공간 부족 시 기존 층 보존
                } // 오류 끝
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
                DungeonPresentation.Apply(nextRooms); // 새 층에도 생성 이미지와 두 배 타일 적용
                AddContentMarkers(nextRooms, nextContent); // 다음 층 상자 표시 생성
                AddStairMarker(nextLayout, nextRooms); // 새 층 계단 표시 생성
                AddEncounterMarker(nextLayout, nextRooms); // 새 층 시험 대상 표시 생성
                UpdateLockMarkers(nextLayout, nextRooms, nextLocks); // 새 층 잠긴 문 표시
                UpdateSecretMarkers(nextLayout, nextRooms, nextDiscovery); // 새 층의 미발견 비밀벽 숨김
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
                doorLocks = nextLocks; // 새 층의 문 잠금 확정
                Discovery = nextDiscovery; // 새 층의 방문 기록 확정
                state = new RoomTransitionState(0, 0); // 새 층 시작 방 좌표
                entryDoor = null; // 이전 층의 방 연결 해제
                floorProgress.Complete(true); // 다음 층 번호 확정
                if (nextContent != null) // 실제 방 콘텐츠 활성 검사
                { // 콘텐츠 확정 시작
                    currentContent = nextContent; // 다음 층 콘텐츠 연결
                    roomContents.Add(nextContent); // 이전 층 콘텐츠를 보존한 새 층 추가
                } // 콘텐츠 확정 끝
                if (nextEvent != null) // 다음 층 이벤트 검사
                { // 이벤트 확정 시작
                    trialEvents.Add(nextEvent); // 이전 층 기록을 보존한 새 이벤트
                } // 이벤트 확정 끝
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
