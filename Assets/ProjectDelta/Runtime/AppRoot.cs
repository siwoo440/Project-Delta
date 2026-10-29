using System.Collections; // 순차 화면 전환
using ProjectDelta.Core; // 진행 상태 형식
using UnityEngine; // Unity 실행 도구
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    [RequireComponent(typeof(GameInputRouter), typeof(SceneLoader))] // 필수 실행 구성
    public sealed class AppRoot : MonoBehaviour // 공통 실행 객체
    { // 클래스 시작
        public const string TrialPotionId = "trial-small-potion"; // 시험 소형 회복약 식별자
        public const int TrialPotionRecovery = 25; // 기획 기준 시험 체력 회복량
        public const int TrialPotionStackLimit = 5; // 미확정 시험 최대 중첩
        private SceneLoader sceneLoader; // 실제 씬 로더
        private ObservationStorage observationStorage; // 2D 전용 관찰 기록 저장
        private NpcRelationshipStorage relationshipStorage; // 영구 NPC 관계 저장소
        public const string TrialMerchantId = "trial-merchant"; // 시험 상인의 고유 식별자
        public NpcRelationshipState NpcRelationships { get; private set; } = NpcRelationshipState.Empty; // 회차 간 유지하는 관계
        public string RelationshipSaveMessage { get; private set; } = string.Empty; // 관계 저장과 복구 안내
        public bool CanUseTrialFriendliness => relationshipStorage != null && !relationshipStorage.IsBlocked && !NpcRelationships.HasReceipt(CurrentRunId, TrialMerchantId); // 이번 회차의 우호 선택 가능 여부
        private RunStorage runStorage; // 현재 회차 저장 기능
        private DefeatHistoryStorage defeatStorage; // 독립된 영구 패배 기록
        private RunSaveData savedRun; // 마지막으로 확인한 저장 회차
        private bool savedRunValid; // 마지막 회차의 복원 가능 여부
        public RunSaveData PendingRun { get; private set; } // 씬에 전달할 복원 대기 회차
        public string RunSaveMessage { get; private set; } = string.Empty; // 회차 저장 안내
        public bool CanContinueRun => savedRunValid; // 마지막 검증의 복원 가능 여부
        private CombatScreenView combatScreen; // 시험 전투 화면
        private BossPreparationScreenView bossPreparation; // 마왕 방 진입 전 준비 화면
        private BossResultScreenView bossResult; // 저장된 마왕 결과 대기 화면
        private EndingHistoryStorage endingStorage; // 독립 결말과 파편 영구 저장소
        private EndingScreenView endingScreen; // 현재 결말 선택과 결과 화면
        private string pendingBossTransactionId; // 저장 재시도에도 유지할 마왕 결과 식별자
        private TrialEventBattleState trialEventBattle; // 현재 시험 이벤트 전투 상태
        private TrialEventBattleScreenView trialEventBattleScreen; // 별도 이벤트 전투 화면
        private Coroutine trialEventBattleAuto; // 적 주도권의 자동 처리
        public static System.Func<int, int, int> TrialEventBattleRollOverride { get; set; } // 자동 검증용 전투 주사위
        private BattleRewardScreenView rewardScreen; // 저장된 승리 보상 화면
        private EncounterScreenView encounterScreen; // 현재 조우 선택 화면
        private EncounterTransitionView encounterTransition; // 자동 전투 진입 전환 화면
        public static bool? AutomaticEncounterTestOverride { get; set; } // 조우 방식의 독립 검증 설정
        private InventoryScreenView inventoryScreen; // 현재 인벤토리 화면
        private ChestScreenView chestScreen; // 현재 상자 확인 화면
        private TrialNpcScreenView trialNpcScreen; // 현재 시험 상인 대화 화면
        private EconomyScreenView npcShopScreen; // 상인과 연결된 시험 상점 화면
        private TrialEventScreenView trialEventScreen; // 현재 시험 이벤트 화면
        public static System.Func<int, int, int> TrialEventRollOverride { get; set; } // 독립 이벤트 난수 검증
        private readonly System.Random chestRandom = new System.Random(); // 상자와 함정 시험 난수
        public static System.Func<int, int, int> ChestRollOverride { get; set; } // 자동 검증용 상자 난수
        private FullMapScreenView fullMapScreen; // 현재 전체 지도 화면
        private HistoryScreenView historyScreen; // 현재 회차 기록 화면
        private PauseScreenView pauseScreen; // 탐험 일시정지 메뉴
        private InventoryState loadRollbackInventory; // 불러오기 실패 시 이전 인벤토리
        private EquipmentState loadRollbackEquipment; // 불러오기 실패 시 이전 장비
        private RelicState loadRollbackRelics; // 불러오기 실패 시 이전 유물
        private ShopStockData[] loadRollbackShops; // 불러오기 실패 시 이전 상점 목록
        private ShopState loadRollbackShop; // 불러오기 실패 시 이전 열린 상점
        private ShopStockData[] shopStocks = new ShopStockData[0]; // 방문한 층의 고정 상점 목록
        public RelicState Relics { get; private set; } = RelicState.Empty; // 현재 회차의 독립 유물
        public ShopState Shop { get; private set; } // 현재 층에서 선택한 시험 상점
        private DoorKeyRing loadRollbackKeys; // 불러오기 실패 시 이전 열쇠
        private ExplorationHistory loadRollbackHistory; // 불러오기 실패 시 이전 기록
        private int loadRollbackHealth; // 불러오기 실패 시 이전 체력
        private int loadRollbackMana; // 불러오기 실패 시 이전 마나
        private string loadRollbackRunId; // 불러오기 실패 시 이전 회차 식별자
        private int loadRollbackLevel; // 불러오기 실패 시 이전 레벨
        private int loadRollbackExperience; // 불러오기 실패 시 이전 경험치
        private int loadRollbackStatPoints; // 불러오기 실패 시 이전 스탯 포인트
        private int loadRollbackGold; // 불러오기 실패 시 이전 골드
        private SavedRewardReceipt[] loadRollbackRewards; // 불러오기 실패 시 이전 보상 거래
        public string CurrentRunId { get; private set; } = System.Guid.NewGuid().ToString("N"); // 현재 회차의 고유 식별자
        public int Level { get; private set; } = 1; // 현재 회차 레벨
        public int Experience { get; private set; } // 현재 레벨의 진행 경험치
        public int StatPoints { get; private set; } = 5; // 분배하지 않은 스탯 포인트
        public int Gold { get; private set; } // 현재 회차 골드
        public SavedRewardReceipt[] RewardReceipts { get; private set; } = new SavedRewardReceipt[0]; // 완료한 보상 거래 기록
        public ExplorationHistory ExplorationHistory // 현재 회차의 전체 탐험 기록
        { // 속성 시작
            get; // 탐험 기록 조회
            private set; // 새 회차 기록 교체
        } = new ExplorationHistory(); // 초기 빈 탐험 기록
        public InventoryState Inventory // 현재 게임의 아이템 상태
        { // 속성 시작
            get; // 인벤토리 조회
            private set; // 새 게임 인벤토리 변경
        } = new InventoryState(); // 기본 빈 인벤토리 생성
        public EquipmentState Equipment { get; private set; } = EquipmentState.Empty; // 현재 회차의 장착 장비
        public ObservationJournal Observations // 현재 회차의 시험 관찰 기록
        { // 속성 시작
            get; // 관찰 기록 조회
            private set; // 새 게임 기록 변경
        } = new ObservationJournal(); // 빈 관찰 기록 생성
        public DoorKeyRing Keys // 현재 게임의 열쇠 보관소
        { // 속성 시작
            get; // 열쇠 조회
            private set; // 새 게임 열쇠 변경
        } = new DoorKeyRing(); // 빈 열쇠 보관소 생성
        public string ObservationSaveMessage // 관찰 저장과 복구 안내
        { // 속성 시작
            get; // 현재 저장 안내 조회
            private set; // 내부 저장 안내 변경
        } = string.Empty; // 기본 안내 없음
        public string PendingObservationId // 저장 대기 중인 성공 판정 대상
        { // 속성 시작
            get; // 저장 대기 대상 조회
            set; // 조우 화면의 성공 판정 보관
        } = string.Empty; // 기본 대기 대상 없음
        public bool ObservationSaveBlocked => observationStorage == null || observationStorage.IsBlocked; // 복구 또는 재시작이 필요한 저장 상태
        public event System.Action InventoryClosed; // 인벤토리 복귀 알림
        public event System.Action<string> ExplorationEvent; // 실제 탐험 행동 기록 알림
        public int PlayerHealth // 탐험 중 플레이어 현재 체력
        { // 속성 시작
            get; // 현재 체력 조회
            private set; // 내부 체력 변경
        } = 100; // 최초 체력 설정
        public int PlayerMana // 현재 회차의 플레이어 마나
        { // 속성 시작
            get; // 현재 마나 조회
            private set; // 내부 마나 변경
        } = 50; // 기획 기본 마나 설정
        public static AppRoot Instance // 유일한 공통 객체
        { // 속성 시작
            get; // 객체 읽기
            private set; // 내부 객체 등록
        } // 속성 끝
        public GameFlowState Flow // 현재 진행 상태
        { // 속성 시작
            get; // 상태 읽기
        } = new GameFlowState(); // 새 진행 상태 생성
        public GameInputRouter Input // 상태별 입력 제어
        { // 속성 시작
            get; // 입력 제어 읽기
            private set; // 초기 입력 등록
        } // 속성 끝
        private void Awake() // 공통 객체 초기 준비
        { // 초기 준비 시작
            if (Instance != null && Instance != this) // 이미 준비된 객체 검사
            { // 중복 조건 시작
                Instance.ReturnToTitle(); // 시작 씬 재진입 복구
                Destroy(gameObject); // 중복 객체 제거
                return; // 추가 초기화 중단
            } // 중복 조건 끝
            Instance = this; // 단일 공통 객체 등록
            DontDestroyOnLoad(gameObject); // 씬 전환 중 객체 유지
            try // 2D 전용 관찰 기록 읽기
            { // 저장 읽기 시작
                observationStorage = new ObservationStorage(ObservationStorage.ResolveDirectory()); // 독립 저장 폴더 연결
                Observations = observationStorage.Load(out var warning); // 검증된 관찰 기록과 복구 안내
                ObservationSaveMessage = warning; // 사용자에게 표시할 저장 상태
            } // 저장 읽기 끝
            catch (System.Exception) // 저장 경로 준비 실패
            { // 경로 실패 시작
                observationStorage = null; // 잘못된 저장 경로 차단
                ObservationSaveMessage = "관찰 저장 경로를 사용할 수 없습니다. 기존 파일을 확인해 주세요."; // 저장 경로 오류 안내
            } // 경로 실패 끝
            try // 영구 NPC 관계 기록 읽기
            { // 저장 조회 시작
                relationshipStorage = new NpcRelationshipStorage(ObservationStorage.ResolveDirectory()); // 관찰 기록과 분리된 관계 파일
                NpcRelationships = relationshipStorage.Load(out var relationshipWarning); // 검증된 관계와 복구 안내
                RelationshipSaveMessage = relationshipWarning; // 실제 저장 상태 안내
            } // 저장 조회 끝
            catch (System.Exception) // 관계 저장 경로 오류
            { // 경로 오류 시작
                relationshipStorage = null; // 잘못된 저장 경로 차단
                RelationshipSaveMessage = "관계 저장 경로를 사용할 수 없습니다."; // 관계 저장 오류 안내
            } // 경로 오류 끝
            sceneLoader = GetComponent<SceneLoader>(); // 씬 로더 연결
            try // 회차 저장 읽기
            { // 저장 읽기 시작
                runStorage = new RunStorage(RunStorage.ResolveDirectory()); // 독립 회차 저장 경로
                defeatStorage = new DefeatHistoryStorage(RunStorage.ResolveDirectory(), id => id == "trial-elite" || MonsterCatalog.Find(id) != null); // 실제 상대만 허용하는 패배 기록
                endingStorage = new EndingHistoryStorage(RunStorage.ResolveDirectory()); // 회차와 같은 기준 폴더의 결말 기록
                endingStorage.Load(out _); // 결말 파일 최초 검증과 충돌 기준 설정
                savedRun = runStorage.Load(out var runWarning); // 현재 회차 데이터 읽기
                savedRunValid = savedRun != null && savedRun.encounterHealth <= CombatScreenView.TrialEnemyHealth; // 실제 적 체력 범위 검사
                RunSaveMessage = runWarning; // 저장 오류 안내 보관
                ReconcileDefeatedRun(); // 기록 후 중단된 회차 종료 복구
            } // 저장 읽기 끝
            catch (System.Exception) // 저장 경로 오류
            { // 경로 오류 시작
                RunSaveMessage = "회차 저장 경로를 사용할 수 없습니다."; // 저장 경로 오류 안내
            } // 경로 오류 끝
            Input = GetComponent<GameInputRouter>(); // 입력 제어 연결
            Input.Initialize(); // 전용 입력 준비
            Input.BackRequested += OpenPause; // 탐험 일시정지 입력 연결
            Input.InventoryRequested += OpenInventory; // 인벤토리 열기 입력 연결
            Input.FullMapRequested += OpenFullMap; // 전체 지도 열기 입력 연결
            Input.FullMapCloseRequested += CloseFullMap; // 전체 지도 닫기 입력 연결
            Input.HistoryRequested += OpenHistory; // 회차 기록 열기 입력 연결
            Input.HistoryCloseRequested += CloseHistory; // 회차 기록 닫기 입력 연결
            Input.HistoryPageRequested += MoveHistoryPage; // 회차 기록 페이지 입력 연결
            Application.targetFrameRate = 60; // 기본 목표 프레임
        } // 초기 준비 끝
        private void Start() // 최초 실행 흐름
        { // 최초 실행 시작
            if (Instance == this) // 단일 객체 실행 검사
            { // 실행 조건 시작
                ReturnToTitle(); // 타이틀 진입 요청
            } // 실행 조건 끝
        } // 최초 실행 끝
        public void StartNewGame() // 새 게임 요청
        { // 메서드 시작
            if (Flow.Phase == GamePhase.Title) // 실제 새 게임 요청 검사
            { // 새 게임 조건 시작
                PendingRun = null; // 이전 복원 요청 해제
                pendingBossTransactionId = null; // 이전 마왕 결과 식별자 초기화
                CurrentRunId = System.Guid.NewGuid().ToString("N"); // 새 회차의 고유 식별자 생성
                Level = 1; // 새 회차 기본 레벨
                Experience = 0; // 새 회차 경험치 초기화
                StatPoints = 5; // 새 회차 기본 스탯 포인트
                Gold = 0; // 새 회차 시험 골드 초기화
                RewardReceipts = new SavedRewardReceipt[0]; // 새 회차의 빈 보상 거래
                PlayerHealth = 100; // 원본 기본 최대 체력 초기화
                PlayerMana = 50; // 새 회차 기본 마나 초기화
                ExplorationHistory = new ExplorationHistory(); // 새 회차의 탐험 기록 초기화
                Inventory = new InventoryState(); // 새 게임의 빈 인벤토리 초기화
                Equipment = EquipmentState.Empty; // 새 회차 시작 장비 없음
                Relics = RelicState.Empty; // 새 회차 유물 초기화
                shopStocks = new ShopStockData[0]; // 새 회차 상점 가격과 재고 초기화
                Shop = null; // 이전 회차 상점 해제
                Keys = new DoorKeyRing(); // 새 게임의 열쇠 초기화
            } // 새 게임 조건 끝
            RequestTransition(GamePhase.Exploration, "DungeonScene"); // 던전 진입 요청
        } // 메서드 끝
        public bool TrySaveRun(out string error) // 안정된 회차 저장
        { // 저장 시작
            error = string.Empty; // 기본 오류 없음
            var room = GameObject.Find("RoomTravel"); // 현재 던전 이동 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 던전 상태
            var data = travel == null ? null : travel.CaptureRun(); // 안정된 탐험 상태 저장본
            if (runStorage == null || data == null || !RunRestoreRules.IsValid(data)) // 저장 기능과 복원 가능 상태 검사
            { // 저장 거절 조건 시작
                error = "현재 상태를 저장할 수 없습니다. 이동이 끝난 뒤 다시 시도해 주세요."; // 저장 불가 안내
                return false; // 미완료 상태 저장 차단
            } // 저장 거절 조건 끝
            if (!runStorage.TrySave(data, out error)) // 안전한 파일 저장 검사
            { // 저장 오류 시작
                RunSaveMessage = error; // 저장 오류 보관
                return false; // 저장 실패 전달
            } // 저장 오류 끝
            savedRun = data; // 마지막 정상 저장 보관
            savedRunValid = true; // 저장한 회차의 복원 가능 상태
            RunSaveMessage = "회차를 저장했습니다."; // 저장 성공 안내
            return true; // 저장 성공 전달
        } // 저장 끝
        public bool TryPersistFloorTransition(RunSaveData data, out string message) // 다음 층의 검증된 상태 저장
        { // 저장 시작
            message = string.Empty; // 기본 오류 없음
            var savedId = savedRun == null ? null : savedRun.version < 7 ? runStorage?.LegacyRunId : savedRun.runId; // 기존 저장 회차 식별자
            if (runStorage == null || data == null || data.runId != CurrentRunId || savedId != null && savedId != CurrentRunId || !RunRestoreRules.IsValid(data)) // 저장소와 현재 회차 및 복원 가능성 검사
            { // 저장 거절 시작
                message = "다음 층의 회차 상태를 저장할 수 없습니다. 기존 저장을 확인해 주세요."; // 상태와 회차 보호 안내
                return false; // 다른 회차와 잘못된 데이터 보존
            } // 저장 거절 끝
            if (!runStorage.TrySave(data, out message)) // 안전한 파일 저장 검사
            { // 저장 실패 시작
                return false; // 기존 메모리와 저장 상태 유지
            } // 저장 실패 끝
            savedRun = data; // 검증된 층 이동 저장 보관
            savedRunValid = true; // 저장된 회차 재개 허용
            return true; // 안전한 저장 성공
        } // 저장 끝
        public bool ContinueRun() // 타이틀에서 검증된 회차 재개
        { // 재개 시작
            if ((Flow.Phase != GamePhase.Title && Flow.Phase != GamePhase.Pause) || !CanContinueRun) // 타이틀과 메뉴의 회차 검사
            { // 재개 거절 조건 시작
                return false; // 잘못된 재개 차단
            } // 재개 거절 조건 끝
            var restoredInventory = InventoryState.FromSavedSlots(savedRun.slots); // 독립 인벤토리 복원
            var restoredRelics = RelicState.Empty; // 이전 저장의 빈 유물
            if (savedRun.version >= 13 && !RelicState.TryRestore(savedRun.relicIds, out restoredRelics)) // 저장된 유물 검사
            { // 유물 복원 오류 시작
                return false; // 잘못된 유물 차단
            } // 유물 복원 오류 끝
            var restoredShops = savedRun.version < 13 ? new ShopStockData[0] : CopyShopStocks(savedRun.shopStocks); // 이전 저장 또는 검증된 층별 상점 복원
            if (restoredShops == null) // 상점 복원 오류 검사
            { // 상점 복원 오류 시작
                return false; // 손상된 상점 차단
            } // 상점 복원 오류 끝
            var restoredEquipment = EquipmentState.Empty; // 이전 저장의 빈 장비
            if (savedRun.version >= 11 && !EquipmentState.TryRestore(savedRun.weaponId, savedRun.armorId, savedRun.accessoryId, savedRun.bagId, savedRun.version >= 12 ? savedRun.weaponVariant : null, savedRun.version >= 12 ? savedRun.armorVariant : null, savedRun.version >= 12 ? savedRun.accessoryVariant : null, savedRun.version >= 12 ? savedRun.bagVariant : null, out restoredEquipment)) // 저장 장비와 개별 특성 검사
            { // 장비 복원 오류 시작
                return false; // 잘못된 장비 복원 차단
            } // 장비 복원 오류 끝
            var restoredKeys = new DoorKeyRing(); // 독립 열쇠 보관소
            if (savedRun.keys > 0 && !restoredKeys.Add(savedRun.keys)) // 열쇠 수량 복원 검사
            { // 복원 오류 시작
                return false; // 잘못된 수량 차단
            } // 복원 오류 끝
            var restoredHistory = new ExplorationHistory(); // 독립 회차 기록
            foreach (var entry in savedRun.history) // 저장된 사건 순회
            { // 기록 복원 시작
                restoredHistory.Add(entry); // 검증된 사건 복원
            } // 기록 복원 끝
            if (Flow.Phase == GamePhase.Pause) // 기존 회차에서 불러오기 검사
            { // 실패 복구 준비 시작
                loadRollbackInventory = Inventory; // 기존 인벤토리 보관
                loadRollbackEquipment = Equipment; // 기존 장비 보관
                loadRollbackRelics = Relics; // 기존 유물 보관
                loadRollbackShops = shopStocks; // 기존 층별 상점 보관
                loadRollbackShop = Shop; // 기존 열린 상점 보관
                loadRollbackKeys = Keys; // 기존 열쇠 보관
                loadRollbackHistory = ExplorationHistory; // 기존 기록 보관
                loadRollbackHealth = PlayerHealth; // 기존 체력 보관
                loadRollbackMana = PlayerMana; // 기존 마나 보관
                loadRollbackRunId = CurrentRunId; // 기존 회차 식별자 보관
                loadRollbackLevel = Level; // 기존 레벨 보관
                loadRollbackExperience = Experience; // 기존 경험치 보관
                loadRollbackStatPoints = StatPoints; // 기존 포인트 보관
                loadRollbackGold = Gold; // 기존 골드 보관
                loadRollbackRewards = RewardReceipts; // 기존 보상 거래 보관
            } // 실패 복구 준비 끝
            Inventory = restoredInventory; // 복원한 인벤토리 적용
            Equipment = restoredEquipment; // 복원한 장비 적용
            Relics = restoredRelics; // 복원한 유물 적용
            shopStocks = restoredShops; // 복원한 층별 상점 적용
            Shop = null; // 현재 층 상점의 재방문 준비
            Keys = restoredKeys; // 복원한 열쇠 적용
            ExplorationHistory = restoredHistory; // 복원한 회차 기록 적용
            PlayerHealth = savedRun.health; // 저장된 체력 적용
            PlayerMana = savedRun.version < 5 ? 50 : savedRun.mana; // 이전 저장의 기본 마나와 새 저장 복원
            CurrentRunId = savedRun.version < 7 ? runStorage.LegacyRunId : savedRun.runId; // 이전 저장의 안정 식별자와 새 저장 복원
            Level = savedRun.version < 8 ? 1 : savedRun.level; // 이전 저장의 기본 레벨과 현재 레벨
            Experience = savedRun.version < 8 ? 0 : savedRun.experience; // 이전 저장의 기본 경험치와 현재 경험치
            StatPoints = savedRun.version < 8 ? 5 : savedRun.statPoints; // 이전 저장의 기본 포인트와 현재 포인트
            Gold = savedRun.version < 8 ? 0 : savedRun.gold; // 이전 저장의 기본 골드와 현재 골드
            RewardReceipts = savedRun.version < 8 || savedRun.rewardReceipts == null ? new SavedRewardReceipt[0] : savedRun.rewardReceipts; // 이전 저장과 현재 보상 거래 복원
            PendingRun = savedRun; // 던전 씬에 복원 상태 전달
            RequestTransition(GamePhase.Exploration, "DungeonScene"); // 탐험 재개 요청
            return true; // 재개 요청 승인
        } // 재개 끝
        public void FinishRunRestore() // 던전 복원 요청 소모
        { // 소모 시작
            if (PendingRun != null && PendingRun.version >= 19 && PendingRun.bossOutcome != 0) // 저장된 마왕 결과 검사
            { // 마왕 결과 복원 시작
                StartCoroutine(OpenRestoredBossResult()); // 장면 전환 뒤 결과 화면 복원
            } // 마왕 결과 복원 끝
            if (PendingRun != null && PendingRun.pendingReward != null) // 미완료 승리 보상 검사
            { // 보상 복원 준비 시작
                StartCoroutine(OpenRestoredReward()); // 씬 전환 후 저장된 후보 화면 복원
            } // 보상 복원 준비 끝
            if (PendingRun != null && PendingRun.hasTrialEventBattle) // 저장된 이벤트 전투 검사
            { // 이벤트 전투 복원 준비
                StartCoroutine(OpenRestoredEventBattle()); // 장면 전환 후 마지막 행동 상태 복원
            } // 복원 준비 끝
            PendingRun = null; // 재복원 방지
        } // 소모 끝
        private IEnumerator OpenRestoredBossResult() // 저장된 마왕 결과 복원
        { // 복원 시작
            while (Flow.Phase == GamePhase.Loading) // 장면 전환 완료 검사
            { // 대기 시작
                yield return null; // 다음 프레임 대기
            } // 대기 끝
            if (savedRun == null || savedRun.bossOutcome == 0 || Flow.Phase != GamePhase.Exploration) // 결과와 탐험 상태 검사
            { // 오류 시작
                RejectRunRestore(); // 원본 보존과 타이틀 복귀
                yield break; // 잘못된 결과 차단
            } // 오류 끝
            OpenEndingFlow(); // 저장된 결과의 결말 흐름 복원
        } // 복원 끝
        public void OpenBossPreparation(System.Action enter) // 마왕 방 진입 전 준비 확인
        { // 열기 시작
            if (Flow.Phase != GamePhase.Exploration || bossPreparation != null || enter == null) // 탐험과 중복 화면 검사
            { // 거절 시작
                return; // 잘못된 준비 요청 차단
            } // 거절 끝
            Input.SetPhase(GamePhase.Pause); // 탐험 입력 잠금과 준비 화면 입력 활성화
            try // 실제 준비 화면 생성
            { // 생성 시작
                bossPreparation = BossPreparationScreenView.Create(this, () => // 마왕 진입 선택
                { // 진입 시작
                    bossPreparation.Close(); // 준비 화면 닫기
                    bossPreparation = null; // 화면 참조 제거
                    Input.SetPhase(GamePhase.Exploration); // 실제 방 이동 입력 복원
                    enter(); // 확인된 입장 실행
                }, () => // 준비 계속 선택
                { // 취소 시작
                    bossPreparation.Close(); // 준비 화면 닫기
                    bossPreparation = null; // 화면 참조 제거
                    Input.SetPhase(GamePhase.Exploration); // 기존 탐험 입력 복원
                }); // 선택 연결 끝
            } // 생성 끝
            catch (System.Exception exception) // 준비 화면 오류 검사
            { // 오류 시작
                Input.SetPhase(GamePhase.Exploration); // 실패 후 탐험 복구
                Debug.LogError("Boss preparation failed: " + exception.Message); // 실제 오류 기록
            } // 오류 끝
        } // 열기 끝
        public bool TrySaveBossOutcome(int outcome, out string message) // 마왕 결과를 현재 회차에 보존
        { // 저장 시작
            message = string.Empty; // 기본 오류 없음
            var room = GameObject.Find("RoomTravel"); // 현재 던전 조회
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 방 이동 제어
            if (Flow.Phase != GamePhase.Combat || travel == null || !travel.IsBossRoom || outcome < 1 || outcome > 3 || runStorage == null) // 실제 마왕 전투와 결과 검사
            { // 거절 시작
                message = "마왕 전투 결과를 확인할 수 없습니다."; // 잘못된 결과 안내
                return false; // 다른 전투의 결과 저장 차단
            } // 거절 끝
            var savedId = savedRun == null ? null : savedRun.version < 7 ? runStorage.LegacyRunId : savedRun.runId; // 기존 저장 회차 식별자
            if (savedId != null && savedId != CurrentRunId) // 다른 회차의 저장 검사
            { // 보호 시작
                message = "다른 회차의 저장이 있어 마왕 결과를 저장할 수 없습니다."; // 기존 회차 보호 안내
                return false; // 다른 회차 덮어쓰기 차단
            } // 보호 끝
            var data = travel.CaptureBattleRewardRun(); // 현재 전투의 독립 회차 저장본
            if (data == null) // 안정된 위치 검사
            { // 오류 시작
                message = "마왕 전투 위치를 저장할 수 없습니다."; // 저장 불가 안내
                return false; // 잘못된 위치 차단
            } // 오류 끝
            pendingBossTransactionId = pendingBossTransactionId ?? System.Guid.NewGuid().ToString("N"); // 재시도 시 동일 결과 식별자 유지
            data.bossOutcome = outcome; // 확정된 마왕 결과
            data.bossTransactionId = pendingBossTransactionId; // 고유 결과 식별자
            if (!RunRestoreRules.IsValid(data) || !runStorage.TrySave(data, out message)) // 결과 계약과 안전 저장 검사
            { // 저장 실패 시작
                if (string.IsNullOrEmpty(message)) // 구체적인 오류 누락 검사
                { // 기본 안내 시작
                    message = "마왕 결과를 저장할 수 없습니다."; // 재시도 안내
                } // 기본 안내 끝
                return false; // 기존 회차 보존
            } // 저장 실패 끝
            savedRun = data; // 실제 저장된 결과 보관
            savedRunValid = true; // 저장 결과 재개 허용
            EndCombat(); // 저장 성공 후 전투 화면 종료
            OpenEndingFlow(); // 저장된 결과의 결말 흐름 표시
            return true; // 결과 보존 성공
        } // 저장 끝
        private void ShowBossResult(int outcome) // 보존된 마왕 결과 화면 열기
        { // 표시 시작
            Input.SetPhase(GamePhase.Pause); // 탐험 입력 차단과 결과 화면 입력 활성화
            try // 실제 결과 화면 생성
            { // 생성 시작
                bossResult = BossResultScreenView.Create(outcome, () => // 타이틀 이동 선택
                { // 이동 시작
                    bossResult.Close(); // 결과 화면 닫기
                    bossResult = null; // 화면 참조 제거
                    ReturnToTitle(); // 저장된 회차를 유지한 타이틀 복귀
                }); // 선택 연결 끝
            } // 생성 끝
            catch (System.Exception exception) // 결과 화면 생성 오류
            { // 오류 시작
                Debug.LogError("Boss result failed: " + exception.Message); // 실제 오류 기록
                ReturnToTitle(); // 저장 결과를 보존한 타이틀 복귀
            } // 오류 끝
        } // 표시 끝
        private bool TrySaveEndingData(RunSaveData data, out string message) // 결말 후보와 선택의 회차 저장
        { // 저장 시작
            message = string.Empty; // 기본 오류 없음
            if (data == null || data.runId != CurrentRunId || runStorage == null || !RunRestoreRules.IsValid(data)) // 회차 식별과 후보 계약 검사
            { // 거절 시작
                message = "결말 선택 상태를 확인할 수 없습니다."; // 실제 상태 오류 안내
                return false; // 잘못된 회차 저장 차단
            } // 거절 끝
            if (!runStorage.TrySave(data, out message)) // 독립 회차 저장 검사
            { // 오류 시작
                return false; // 이전 회차 상태 보존
            } // 오류 끝
            savedRun = data; // 확인된 결말 상태 보관
            savedRunValid = true; // 저장된 회차 재개 허용
            return true; // 실제 저장 성공
        } // 저장 끝
        private void OpenEndingFlow() // 저장된 마왕 결과의 결말 흐름
        { // 열기 시작
            if (savedRun == null || savedRun.bossOutcome == 0 || savedRun.runId != CurrentRunId) // 실제 마왕 결과와 회차 검사
            { // 오류 시작
                RejectRunRestore(); // 원본 보존과 타이틀 복귀
                return; // 잘못된 결과 차단
            } // 오류 끝
            Input.SetPhase(GamePhase.Pause); // 탐험 잠금과 화면 입력 활성화
            if (endingScreen != null) // 기존 재시도 화면 검사
            { // 화면 정리 시작
                endingScreen.Close(); // 이전 화면 닫기
                endingScreen = null; // 이전 화면 참조 제거
            } // 화면 정리 끝
            if (savedRun.version < 20 || !savedRun.hasEndingCandidates) // 아직 후보가 확정되지 않은 결과 검사
            { // 후보 준비 시작
                var data = JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(savedRun)); // 독립 회차 저장본 복사
                RunStorage.NormalizeEquipment(data); // 복사된 빈 장비 객체 정리
                data.pendingReward = null; // 마왕 결과에 없는 빈 일반 보상 정리
                data.trialEventBattle = null; // 마왕 결과에 없는 빈 이벤트 전투 정리
                data.version = RunSaveData.CurrentVersion; // 최신 결말 저장 형식 적용
                var equipped = (string.IsNullOrEmpty(data.weaponId) ? 0 : 1) + (string.IsNullOrEmpty(data.armorId) ? 0 : 1) + (string.IsNullOrEmpty(data.accessoryId) ? 0 : 1) + (string.IsNullOrEmpty(data.bagId) ? 0 : 1); // 저장된 장착 장비 부위 수
                if (!EndingRules.TryCandidates(data.bossOutcome, data.health, data.maxHealth, equipped, data.relicIds.Length, out var candidates)) // 실제 결과와 조건의 후보 계산
                { // 계산 오류 시작
                    ShowEndingRetry("결말 후보를 확인할 수 없습니다.", OpenEndingFlow); // 원본을 보존한 재시도 안내
                    return; // 잘못된 후보 표시 차단
                } // 계산 오류 끝
                data.hasEndingCandidates = true; // 후보 확정 상태
                data.endingReturnCandidates = new System.Collections.Generic.List<string>(candidates.ReturnCandidates).ToArray(); // 실제 귀환 후보 복사
                data.endingStayCandidates = new System.Collections.Generic.List<string>(candidates.StayCandidates).ToArray(); // 실제 잔류 후보 복사
                data.endingAutomaticCandidate = candidates.AutomaticCandidate; // 패배와 항복의 자동 후보
                data.endingSelectedId = null; // 최초 선택 전 상태
                if (!TrySaveEndingData(data, out var message)) // 후보의 선행 저장 검사
                { // 저장 실패 시작
                    ShowEndingRetry(message, OpenEndingFlow); // 같은 후보 저장 재시도
                    return; // 저장 전 선택 차단
                } // 저장 실패 끝
            } // 후보 준비 끝
            try // 검증된 후보 화면 생성
            { // 생성 시작
                endingScreen = EndingScreenView.Create(savedRun.endingReturnCandidates, savedRun.endingStayCandidates, ChooseEnding, FinishEnding); // 확정된 후보와 실제 처리 연결
                if (!string.IsNullOrEmpty(savedRun.endingSelectedId)) // 이미 선택한 결말 검사
                { // 선택 복원 시작
                    ProcessEnding(); // 같은 선택의 영구 기록 재시도
                } // 선택 복원 끝
                else if (!string.IsNullOrEmpty(savedRun.endingAutomaticCandidate)) // 패배 또는 항복의 자동 결말 검사
                { // 자동 선택 시작
                    ChooseEnding(savedRun.endingAutomaticCandidate); // 자동 후보의 회차 저장
                } // 자동 선택 끝
            } // 생성 끝
            catch (System.Exception exception) // 결말 화면 생성 오류
            { // 오류 시작
                Debug.LogError("Ending screen failed: " + exception.Message); // 실제 오류 기록
                ReturnToTitle(); // 저장된 회차를 보존한 타이틀 복귀
            } // 오류 끝
        } // 열기 끝
        private void ShowEndingRetry(string message, System.Action retry) // 결말 처리 실패의 안전한 재시도 화면
        { // 표시 시작
            if (endingScreen == null) // 아직 결말 화면이 없는지 검사
            { // 화면 생성 시작
                endingScreen = EndingScreenView.Create(new string[0], new string[0], ChooseEnding, FinishEnding); // 선택 없는 재시도 화면 생성
            } // 화면 생성 끝
            endingScreen.ClearResult(); // 이전 결과와 중복 버튼 제거
            endingScreen.ShowRetry(message + " 다시 시도해 주세요.", retry); // 실제 오류와 같은 거래 재시도 표시
        } // 표시 끝
        private void ChooseEnding(string id) // 확인된 후보 하나를 회차에 확정
        { // 선택 시작
            if (savedRun == null || !savedRun.hasEndingCandidates || !string.IsNullOrEmpty(savedRun.endingSelectedId)) // 실제 미선택 후보 검사
            { // 거절 시작
                return; // 중복 선택과 다른 회차 변경 차단
            } // 거절 끝
            var data = JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(savedRun)); // 독립 회차 상태 복사
            RunStorage.NormalizeEquipment(data); // 복사된 빈 장비 객체 정리
            data.pendingReward = null; // 마왕 결과에 없는 빈 일반 보상 정리
            data.trialEventBattle = null; // 마왕 결과에 없는 빈 이벤트 전투 정리
            data.endingSelectedId = id; // 사용자가 고른 단일 결말
            if (!TrySaveEndingData(data, out var message)) // 선택의 선행 저장 검사
            { // 저장 실패 시작
                if (!string.IsNullOrEmpty(savedRun.endingAutomaticCandidate)) // 자동 결말의 재시도 검사
                { // 자동 재시도 시작
                    ShowEndingRetry(message, () => ChooseEnding(id)); // 동일 자동 선택 재시도
                } // 자동 재시도 끝
                else // 직접 선택 분기
                { // 선택 재시도 시작
                    endingScreen.ShowError(message + " 다시 선택해 주세요."); // 선택 화면의 저장 실패 안내
                } // 선택 재시도 끝
                return; // 저장되지 않은 결말의 기록 차단
            } // 저장 실패 끝
            ProcessEnding(); // 저장된 선택의 영구 기록
        } // 선택 끝
        private void ProcessEnding() // 고정된 결말과 최초 보상 처리
        { // 처리 시작
            if (savedRun == null || string.IsNullOrEmpty(savedRun.endingSelectedId) || endingStorage == null || defeatStorage == null) // 선택과 저장소 검사
            { // 오류 시작
                ShowEndingRetry("결말 저장소를 확인할 수 없습니다.", ProcessEnding); // 같은 결과 재시도 안내
                return; // 누락된 기록 처리 차단
            } // 오류 끝
            if (savedRun.bossOutcome >= 1) // 마왕 결과와 패배 기록의 선행 충돌 검사
            { // 충돌 검사 시작
                var expectedReason = savedRun.bossOutcome == 2 ? BattleDefeatReason.HealthZero : BattleDefeatReason.Surrender; // 저장된 마왕 패배 원인
                var defeats = defeatStorage.Load(out var defeatWarning); // 최신 패배 기록과 무결성 조회
                if (defeatStorage.IsBlocked) // 손상되거나 복구 대기인 기록 검사
                { // 차단 시작
                    ShowEndingRetry(defeatWarning, ProcessEnding); // 기존 기록 보존과 재시도 안내
                    return; // 확인되지 않은 영구 기록 차단
                } // 차단 끝
                foreach (var defeat in defeats) // 기존 패배 거래 순회
                { // 거래 검사 시작
                    if (defeat.TransactionId == CurrentRunId && (savedRun.bossOutcome == 1 || defeat.TargetId != "MON_DEMON_LORD" || defeat.Reason != expectedReason)) // 승리와 패배 충돌 또는 서로 다른 패배 검사
                    { // 충돌 시작
                        ShowEndingRetry("같은 회차에 다른 패배 기록이 있습니다.", ProcessEnding); // 충돌 기록 보호 안내
                        return; // 충돌한 결말 보상 차단
                    } // 충돌 끝
                } // 거래 검사 끝
            } // 충돌 검사 끝
            if (!endingStorage.TryRecord(CurrentRunId, savedRun.bossTransactionId, savedRun.endingSelectedId, out _, out _, out var error)) // 고유 거래의 영구 결말과 최초 보상 저장
            { // 기록 실패 시작
                ShowEndingRetry(error, ProcessEnding); // 동일 거래 재시도
                return; // 영구 기록 전 회차 종료 차단
            } // 기록 실패 끝
            if (savedRun.bossOutcome >= 2) // 실제 마왕 패배 또는 항복 검사
            { // 패배 기록 시작
                var reason = savedRun.bossOutcome == 2 ? BattleDefeatReason.HealthZero : BattleDefeatReason.Surrender; // 저장된 마왕 결과의 실제 원인
                if (!defeatStorage.TryAppend(CurrentRunId, "MON_DEMON_LORD", reason, out _, out error)) // 같은 회차의 마왕 패배 기록 검사
                { // 기록 실패 시작
                    ShowEndingRetry(error, ProcessEnding); // 결말 거래를 중복 없이 재시도
                    return; // 패배 기록 전 회차 종료 차단
                } // 기록 실패 끝
            } // 패배 기록 끝
            if (!endingStorage.TryGetRun(CurrentRunId, out var receipt)) // 저장된 결말 거래 재조회
            { // 조회 실패 시작
                ShowEndingRetry("저장된 결말을 확인할 수 없습니다.", ProcessEnding); // 검증 실패 재시도
                return; // 확인되지 않은 결과 표시 차단
            } // 조회 실패 끝
            endingScreen.ClearResult(); // 이전 재시도 표시 정리
            endingScreen.ShowResult(savedRun.endingSelectedId, receipt.GrantedShards, endingStorage.TotalShards); // 실제 영구 기록과 보상 표시
        } // 처리 끝
        private void FinishEnding() // 영구 기록 확인 후 현재 회차 종료
        { // 종료 시작
            if (savedRun == null || endingStorage == null || defeatStorage == null || runStorage == null || savedRun.runId != CurrentRunId) // 현재 회차와 저장소 검사
            { // 오류 시작
                endingScreen.ShowFinalError("결말 기록을 확인할 수 없습니다."); // 기존 회차 보존 안내
                return; // 확인되지 않은 회차 종료 차단
            } // 오류 끝
            try // 영구 기록 잠금과 회차 종료 검사
            { // 잠금 시작
                var directory = RunStorage.ResolveDirectory(); // 실제 영구 기록 폴더
                System.IO.Directory.CreateDirectory(directory); // 잠금 파일 폴더 준비
                using (var endingGate = new System.IO.FileStream(System.IO.Path.Combine(directory, "ending-history.lock"), System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None)) // 결말 기록 독점 잠금
                using (var defeatGate = new System.IO.FileStream(System.IO.Path.Combine(directory, "defeat.lock"), System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None)) // 패배 기록 독점 잠금
                { // 기록 재검증 시작
                    endingStorage.Load(out var endingWarning); // 최신 결말 기록 재조회
                    if (endingStorage.IsBlocked || !endingStorage.TryGetRun(CurrentRunId, out var receipt) || !string.Equals(receipt.TransactionId, savedRun.bossTransactionId, System.StringComparison.OrdinalIgnoreCase) || receipt.EndingId != savedRun.endingSelectedId) // 손상과 동일 결말 영수증 검사
                    { // 오류 시작
                        endingScreen.ShowFinalError(string.IsNullOrEmpty(endingWarning) ? "결말 기록을 확인할 수 없습니다." : endingWarning); // 실제 기록 오류 안내
                        return; // 영구 기록 불일치의 회차 종료 차단
                    } // 오류 끝
                    var defeats = defeatStorage.Load(out var defeatWarning); // 최신 패배 기록 재조회
                    if (defeatStorage.IsBlocked) // 손상 또는 복구 대기 검사
                    { // 오류 시작
                        endingScreen.ShowFinalError(defeatWarning); // 실제 패배 기록 오류 안내
                        return; // 확인되지 않은 회차 종료 차단
                    } // 오류 끝
                    var matchingDefeat = false; // 현재 회차 패배 기록 확인 상태
                    foreach (var defeat in defeats) // 검증된 패배 기록 순회
                    { // 패배 검사 시작
                        if (defeat.TransactionId != CurrentRunId) // 다른 회차 기록 검사
                        { // 다른 회차 시작
                            continue; // 다음 기록 확인
                        } // 다른 회차 끝
                        var reason = savedRun.bossOutcome == 2 ? BattleDefeatReason.HealthZero : BattleDefeatReason.Surrender; // 현재 마왕 결과의 패배 원인
                        if (savedRun.bossOutcome == 1 || defeat.TargetId != "MON_DEMON_LORD" || defeat.Reason != reason) // 승리와 패배 충돌 및 기록 불일치 검사
                        { // 오류 시작
                            endingScreen.ShowFinalError("현재 회차의 패배 기록이 결말과 다릅니다."); // 충돌 안내
                            return; // 충돌한 회차 종료 차단
                        } // 오류 끝
                        matchingDefeat = true; // 일치한 마왕 패배 기록 확인
                    } // 패배 검사 끝
                    if (savedRun.bossOutcome >= 2 && !matchingDefeat) // 패배 또는 항복의 영구 기록 누락 검사
                    { // 오류 시작
                        endingScreen.ShowFinalError("마왕 패배 기록을 확인할 수 없습니다."); // 누락된 패배 기록 안내
                        return; // 패배 기록 없는 회차 종료 차단
                    } // 오류 끝
                    if (!runStorage.TryMarkEnded(CurrentRunId, out var error)) // 두 영구 기록 잠금 중 회차 종료 검사
                    { // 종료 실패 시작
                        endingScreen.ShowFinalError(error + " 다시 선택해 주세요."); // 같은 종료 재시도 안내
                        return; // 저장된 결과 보존
                    } // 종료 실패 끝
                } // 기록 재검증 끝
            } // 잠금 끝
            catch (System.Exception) // 파일 접근과 잠금 실패
            { // 오류 시작
                endingScreen.ShowFinalError("결말 기록을 확인할 수 없습니다. 다시 시도해 주세요."); // 안전한 재시도 안내
                return; // 확인되지 않은 회차 보존
            } // 오류 끝
            savedRun = runStorage.Load(out var warning); // 종료 후 다른 회차 보존 여부 확인
            savedRunValid = savedRun != null && RunRestoreRules.IsValid(savedRun); // 실제 이어하기 가능 여부
            RunSaveMessage = warning; // 종료 저장 안내
            endingScreen.Close(); // 결과 화면 닫기
            endingScreen = null; // 결과 화면 참조 해제
            ReturnToTitle(); // 확인한 결말의 타이틀 복귀
        } // 종료 끝
        private IEnumerator OpenRestoredEventBattle() // 저장된 이벤트 전투 화면 복원
        { // 복원 시작
            while (Flow.Phase == GamePhase.Loading) // 던전 장면 전환 검사
            { // 전환 대기 시작
                yield return null; // 탐험 상태 대기
            } // 전환 대기 끝
            if (Flow.Phase != GamePhase.Exploration || savedRun == null || !savedRun.hasTrialEventBattle || !TrialEventBattleState.TryRestore(savedRun.trialEventBattle, out var restored) || !Flow.TryStartCombat()) // 저장된 전투와 탐험 상태 검사
            { // 복원 오류 시작
                RejectRunRestore(); // 저장 원본 보존과 타이틀 복귀
                yield break; // 잘못된 전투 복원 차단
            } // 복원 오류 끝
            trialEventBattle = restored; // 검증된 전투 복원
            Input.SetPhase(GamePhase.Combat); // 탐험 입력 잠금
            if (!ShowTrialEventBattle()) // 실제 전투 화면 생성 검사
            { // 화면 오류 시작
                Flow.TryEndCombat(); // 실패한 전투 상태를 탐험으로 복구
                Input.SetPhase(GamePhase.Exploration); // 탐험 상태의 복구 입력 적용
                RejectRunRestore(); // 저장된 전투를 보존한 타이틀 복귀
                yield break; // 불완전한 화면 차단
            } // 화면 오류 끝
            QueueEventBattleAuto(); // 저장된 적 주도권 자동 재개
        } // 복원 끝
        private IEnumerator OpenRestoredReward() // 저장된 승리 보상 화면 복원
        { // 복원 시작
            while (Flow.Phase == GamePhase.Loading) // 씬 전환 완료 검사
            { // 전환 대기 시작
                yield return null; // 다음 프레임 대기
            } // 전환 대기 끝
            if (Flow.Phase == GamePhase.Exploration && savedRun != null && savedRun.pendingReward != null) // 미완료 보상과 탐험 상태 검사
            { // 화면 복원 시작
                if (!OpenRewardScreen()) // 저장된 후보 화면 복원 검사
                { // 복원 실패 보호 시작
                    RejectRunRestore(); // 저장된 후보 보존과 타이틀 복귀
                } // 복원 실패 보호 끝
            } // 화면 복원 끝
        } // 복원 끝
        public bool TryStageVictoryReward(SavedBattleReward reward, bool replaceOtherRun, out string error) // 승리 보상 후보의 사전 저장
        { // 준비 시작
            error = string.Empty; // 기본 오류 없음
            var room = GameObject.Find("RoomTravel"); // 현재 방 이동 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 조우 상태
            if (Flow.Phase != GamePhase.Combat || runStorage == null || travel == null || !travel.CanCompleteCurrentEncounter() || reward == null) // 실제 승리 조우와 저장 상태 검사
            { // 준비 오류 시작
                error = "보상을 준비할 수 없습니다."; // 준비 오류 안내
                return false; // 잘못된 조우 보상 차단
            } // 준비 오류 끝
            if (savedRun != null && savedRun.pendingReward != null && savedRun.pendingReward.transactionId == reward.transactionId) // 같은 보상 후보의 재시도 검사
            { // 중복 준비 시작
                return true; // 이미 검증된 후보 재사용
            } // 중복 준비 끝
            var savedId = savedRun == null ? null : savedRun.version < 7 ? runStorage.LegacyRunId : savedRun.runId; // 기존 저장 회차 식별자
            if (savedId != null && savedId != CurrentRunId && !replaceOtherRun) // 다른 회차 저장 덮어쓰기 확인
            { // 기존 저장 보호 시작
                error = "다른 회차의 저장이 있습니다. 기존 저장 교체를 선택해야 보상을 저장할 수 있습니다."; // 명시적 교체 안내
                return false; // 기존 회차 저장 보존
            } // 기존 저장 보호 끝
            var data = travel.CaptureBattleRewardRun(); // 전투 위치의 독립 저장본
            if (data == null) // 안정된 전투 위치 검사
            { // 저장본 오류 시작
                error = "현재 전투 위치를 저장할 수 없습니다."; // 저장본 오류 안내
                return false; // 후보 표시 차단
            } // 저장본 오류 끝
            data.pendingReward = reward; // 확인 전 고정된 보상 후보
            data.hasPendingReward = true; // 저장된 미완료 후보 표시
            if (!RunRestoreRules.IsValid(data) || !runStorage.TrySave(data, out error)) // 후보와 현재 회차의 원자적 저장 검사
            { // 저장 오류 시작
                if (string.IsNullOrEmpty(error)) // 검증 실패 검사
                { // 오류 안내 시작
                    error = "보상 후보를 검증할 수 없습니다."; // 후보 오류 안내
                } // 오류 안내 끝
                return false; // 저장 실패 시 후보 화면 차단
            } // 저장 오류 끝
            savedRun = data; // 검증된 후보와 회차 보관
            savedRunValid = true; // 중단 후 후보 재개 허용
            return true; // 후보 사전 저장 완료
        } // 준비 끝
        public bool OpenRewardScreen() // 저장된 승리 보상 표시
        { // 표시 시작
            if (savedRun == null || savedRun.pendingReward == null || rewardScreen != null) // 미완료 후보 검사
            { // 표시 거절 시작
                return false; // 중복 화면 차단
            } // 표시 거절 끝
            var resumed = Flow.Phase == GamePhase.Exploration; // 저장 재개 중 보상 표시 여부
            if (resumed && !Flow.TryStartCombat()) // 복원된 보상 화면의 입력 잠금
            { // 잠금 오류 시작
                return false; // 잘못된 상태 차단
            } // 잠금 오류 끝
            try // 실제 보상 화면 생성
            { // 생성 시작
                rewardScreen = BattleRewardScreenView.Create(this, savedRun.pendingReward); // 저장된 후보 표시
                if (combatScreen != null) // 기존 승리 화면 검사
                { // 승리 화면 정리 시작
                    combatScreen.gameObject.SetActive(false); // 이전 화면 즉시 숨김
                    Destroy(combatScreen.gameObject); // 이전 화면 정리
                    combatScreen = null; // 이전 화면 참조 해제
                } // 승리 화면 정리 끝
                Input.SetPhase(GamePhase.Combat); // 보상 확인 중 탐험 입력 잠금
                return true; // 보상 화면 표시 성공
            } // 생성 끝
            catch (System.Exception exception) // 화면 생성 오류
            { // 오류 복구 시작
                if (resumed) // 복원 중 생성 실패 검사
                { // 복원 상태 복구 시작
                    Flow.TryEndCombat(); // 탐험 상태 복구
                    Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
                } // 복원 상태 복구 끝
                Debug.LogError("Reward screen failed: " + exception.Message); // 실제 화면 오류 기록
                return false; // 저장된 후보 보존
            } // 오류 복구 끝
        } // 표시 끝
        public bool TryResolveVictoryReward(bool takePotion, out string error) // 보상 획득 또는 아이템 포기의 원자적 확정
        { // 일반 확정 시작
            return ResolveVictoryReward(takePotion, -1, null, null, 0, 0, null, out error); // 기존 인벤토리를 보존하는 일반 확정
        } // 일반 확정 끝
        public bool TryResolveVictoryRewardReplacing(int index, string expectedId, string expectedName, int expectedQuantity, int expectedLimit, string expectedEquipmentSnapshot, out string error) // 확인한 보유 묶음의 보상 교체
        { // 교체 확정 시작
            if (index < 0) // 잘못된 교체 슬롯 검사
            { // 슬롯 오류 시작
                error = "교체할 아이템을 확인할 수 없습니다."; // 슬롯 오류 안내
                return false; // 일반 획득으로 잘못 전환되는 상황 차단
            } // 슬롯 오류 끝
            return ResolveVictoryReward(true, index, expectedId, expectedName, expectedQuantity, expectedLimit, expectedEquipmentSnapshot, out error); // 확인한 슬롯을 포함한 원자적 확정
        } // 교체 확정 끝
        private bool ResolveVictoryReward(bool takePotion, int replaceIndex, string expectedId, string expectedName, int expectedQuantity, int expectedLimit, string expectedEquipmentSnapshot, out string error) // 보상과 조우 완료의 공통 저장
        { // 확정 시작
            error = string.Empty; // 기본 오류 없음
            var room = GameObject.Find("RoomTravel"); // 현재 방 이동 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 조우 상태
            if (Flow.Phase != GamePhase.Combat || runStorage == null || savedRun == null || savedRun.pendingReward == null || travel == null || !travel.CanCompleteCurrentEncounter()) // 저장된 후보와 실제 조우 검사
            { // 확정 거절 시작
                error = "보상을 확정할 수 없습니다."; // 확정 오류 안내
                return false; // 잘못된 거래 차단
            } // 확정 거절 끝
            var reward = savedRun.pendingReward; // 사전 저장된 후보
            var item = reward.potionCount > 0 ? TrialConsumableRules.Find(reward.itemId) : null; // 저장된 보상 소비품 정의
            var equipmentReward = reward.equipment; // 저장된 개별 시험 장비 후보
            if (reward.potionCount > 0 && item == null || equipmentReward != null && !TrialEquipmentTraits.IsValid(equipmentReward)) // 알 수 없는 보상 종류 검사
            { // 보상 종류 오류 시작
                error = "보상 아이템을 확인할 수 없습니다."; // 보상 종류 오류 안내
                return false; // 확인되지 않은 보상 지급 차단
            } // 보상 종류 오류 끝
            var inventory = InventoryState.FromSavedSlots(savedRun.slots); // 저장된 소지품의 독립 복사
            if (inventory == null || !ExperienceRules.TryAdvance(savedRun.level, savedRun.experience, reward.experience, out var growth) || (long)savedRun.gold + reward.gold > int.MaxValue) // 소지품과 성장 및 재화 범위 검사
            { // 계산 오류 시작
                error = "보상 상태를 계산할 수 없습니다."; // 계산 오류 안내
                return false; // 원본과 후보 보존
            } // 계산 오류 끝
            if (replaceIndex >= 0) // 기존 묶음을 교체하는지 검사
            { // 교체 검사 시작
                if (!takePotion || reward.potionCount <= 0 && equipmentReward == null || replaceIndex >= inventory.Slots.Count || replaceIndex >= Inventory.Slots.Count) // 후보와 교체 범위 검사
                { // 교체 오류 시작
                    error = "교체할 아이템을 확인할 수 없습니다."; // 교체 오류 안내
                    return false; // 기존 소지품과 후보 보존
                } // 교체 오류 끝
                var live = Inventory.Slots[replaceIndex]; // 현재 화면의 실제 보유 묶음
                var saved = inventory.Slots[replaceIndex]; // 후보 저장 시점의 보유 묶음
                var liveEquipment = live.Equipment == null ? string.Empty : JsonUtility.ToJson(live.Equipment); // 현재 개별 장비의 독립 내용
                var savedEquipment = saved.Equipment == null ? string.Empty : JsonUtility.ToJson(saved.Equipment); // 저장된 개별 장비의 독립 내용
                if (live.IsEmpty || live.ItemId != expectedId || live.Name != expectedName || live.Quantity != expectedQuantity || live.StackLimit != expectedLimit || saved.ItemId != expectedId || saved.Name != expectedName || saved.Quantity != expectedQuantity || saved.StackLimit != expectedLimit || liveEquipment != expectedEquipmentSnapshot || savedEquipment != expectedEquipmentSnapshot) // 확인 시점과 저장 시점의 아이템 비교
                { // 변경 충돌 시작
                    error = "아이템이 변경되어 교체할 수 없습니다."; // 기존 데이터 변경 안내
                    return false; // 다른 묶음의 삭제 차단
                } // 변경 충돌 끝
                if (!(equipmentReward == null ? inventory.TryReplace(replaceIndex, item.Id, item.Name, reward.potionCount, item.StackLimit) : inventory.TryReplaceWithEquipment(replaceIndex, equipmentReward))) // 독립 인벤토리의 전체 교체 검사
                { // 교체 실패 시작
                    error = "보상 아이템을 교체할 수 없습니다."; // 교체 실패 안내
                    return false; // 기존 소지품 보존
                } // 교체 실패 끝
            } // 교체 검사 끝
            else if (takePotion && reward.potionCount > 0 && inventory.Add(item.Id, item.Name, reward.potionCount, item.StackLimit).Remaining > 0) // 선택된 소비품 전체 수용 검사
            { // 공간 부족 시작
                error = "인벤토리 공간이 부족합니다. 교체하거나 아이템을 포기할 수 있습니다."; // 명시적 선택 안내
                return false; // 기존 인벤토리와 후보 보존
            } // 공간 부족 끝
            if (replaceIndex < 0 && takePotion && equipmentReward != null && !inventory.TryAddEquipment(equipmentReward)) // 선택한 개별 장비의 수용 검사
            { // 장비 공간 부족 시작
                error = "인벤토리 공간이 부족합니다. 교체하거나 아이템을 포기할 수 있습니다."; // 명시적 선택 안내
                return false; // 기존 인벤토리와 후보 보존
            } // 장비 공간 부족 끝
            var data = JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(savedRun)); // 저장 후보의 독립 복사
            RunStorage.NormalizeEquipment(data); // 직렬화된 빈 장비 객체 정리
            data.level = growth.Level; // 적용한 레벨
            data.experience = growth.CurrentExperience; // 적용한 현재 레벨 경험치
            data.statPoints += growth.GainedStatPoints; // 도달 레벨의 스탯 포인트
            data.gold += reward.gold; // 시험 골드 지급
            data.slots = inventory.SaveSlots(); // 획득 또는 포기한 아이템 상태
            data.pendingReward = null; // 완료된 후보 화면 재진입 차단
            data.hasPendingReward = false; // 보상 처리 완료 표시
            var receipts = new System.Collections.Generic.List<SavedRewardReceipt>(data.rewardReceipts ?? new SavedRewardReceipt[0]); // 기존 보상 거래의 독립 목록
            receipts.Add(new SavedRewardReceipt { transactionId = reward.transactionId, tookPotion = takePotion && reward.potionCount > 0, tookEquipment = takePotion && equipmentReward != null }); // 실제 획득 또는 포기 기록
            data.rewardReceipts = receipts.ToArray(); // 완료한 거래 목록 저장
            if (!travel.PrepareCompletedEncounter(data) || !RunRestoreRules.IsValid(data) || !runStorage.TrySave(data, out error)) // 보상과 조우 완료의 단일 저장 검사
            { // 저장 오류 시작
                if (string.IsNullOrEmpty(error)) // 검증 오류 검사
                { // 안내 시작
                    error = "보상 완료 상태를 저장할 수 없습니다."; // 저장 오류 안내
                } // 안내 끝
                return false; // 저장된 후보와 기존 상태 보존
            } // 저장 오류 끝
            savedRun = data; // 검증된 완료 저장 보관
            Level = data.level; // 확정한 레벨 반영
            Experience = data.experience; // 확정한 경험치 반영
            StatPoints = data.statPoints; // 확정한 스탯 포인트 반영
            Gold = data.gold; // 확정한 골드 반영
            RewardReceipts = data.rewardReceipts; // 확정한 보상 거래 반영
            Inventory = inventory; // 확정한 인벤토리 반영
            travel.CompleteCurrentEncounter(); // 검증된 저장 뒤 실제 조우 완료
            EndCombat(); // 보상 확정 후 탐험 복귀
            return true; // 보상 지급 완료
        } // 확정 끝
        public void RejectRunRestore() // 잘못된 재개 상태 보호
        { // 복원 거절 시작
            PendingRun = null; // 잘못된 복원 요청 해제
            runStorage?.BlockWrites(); // 복원 실패 파일의 덮어쓰기 차단
            savedRun = null; // 같은 오류의 재시도 차단
            savedRunValid = false; // 잘못된 회차 재개 비활성화
            RunSaveMessage = "저장된 위치를 복원할 수 없습니다. 기존 저장 파일을 확인해 주세요."; // 원본 보존 안내
            StartCoroutine(ReturnAfterFailedRestore()); // 씬 전환 종료 후 타이틀 복귀
        } // 복원 거절 끝
        private IEnumerator ReturnAfterFailedRestore() // 복원 실패의 안전한 화면 복귀
        { // 대기 시작
            while (Flow.Phase == GamePhase.Loading) // 기존 씬 전환 완료 검사
            { // 전환 대기 시작
                yield return null; // 다음 프레임 대기
            } // 전환 대기 끝
            if (Flow.Phase == GamePhase.Exploration) // 탐험 전환 완료 검사
            { // 복귀 조건 시작
                ReturnToTitle(); // 잘못된 던전에서 타이틀 복귀
            } // 복귀 조건 끝
        } // 대기 끝
        public void ReturnToTitle() // 타이틀 복귀 요청
        { // 메서드 시작
            RequestTransition(GamePhase.Title, "TitleScene"); // 타이틀 진입 요청
        } // 메서드 끝
        private void ReconcileDefeatedRun() // 패배 기록과 저장 회차의 중단 복구
        { // 복구 시작
            var records = defeatStorage.Load(out var warning); // 검증된 패배 기록 조회
            if (defeatStorage.IsBlocked) // 손상된 패배 기록 검사
            { // 보호 시작
                savedRunValid = false; // 종료 여부가 불명확한 회차 재개 차단
                RunSaveMessage = warning; // 기록 복구 안내
                return; // 기존 파일 보존
            } // 보호 끝
            if (savedRun == null) // 저장 회차 존재 검사
            { // 회차 없음 시작
                return; // 비교할 회차 없음
            } // 회차 없음 끝
            if (savedRun.version >= 19 && savedRun.bossOutcome != 0) // 확정된 마왕 결과의 결말 대기 검사
            { // 결말 대기 시작
                return; // 결과 확인 전 회차 종료 차단
            } // 결말 대기 끝
            var runId = savedRun.version < 7 ? runStorage.LegacyRunId : savedRun.runId; // 저장 회차의 안정적인 식별자
            foreach (var record in records) // 검증된 패배 기록 순회
            { // 기록 비교 시작
                if (record.TransactionId != runId) // 다른 회차 기록 검사
                { // 다른 회차 시작
                    continue; // 다음 기록 검사
                } // 다른 회차 끝
                savedRunValid = false; // 패배한 회차의 재개 차단
                if (!runStorage.TryMarkEnded(runId, out var error)) // 중단된 종료 표시 재시도
                { // 종료 오류 시작
                    RunSaveMessage = error; // 원본 보존과 재시도 안내
                    return; // 저장 회차 보존
                } // 종료 오류 끝
                savedRun = null; // 종료된 회차의 복원 상태 해제
                RunSaveMessage = "패배한 회차의 종료를 복구했습니다."; // 중단 복구 안내
                return; // 중복 종료 차단
            } // 기록 비교 끝
        } // 복구 끝
        public bool TryRecordDefeat(string targetId, BattleDefeatReason reason, out string error) // 영구 패배 기록과 해당 회차 종료
        { // 기록 시작
            error = string.Empty; // 기본 오류 없음
            if (defeatStorage == null || runStorage == null || !defeatStorage.TryAppend(CurrentRunId, targetId, reason, out _, out error)) // 패배 기록의 영구 저장 검사
            { // 기록 실패 시작
                if (string.IsNullOrEmpty(error)) // 저장소 누락 검사
                { // 누락 안내 시작
                    error = "패배 기록 저장소를 사용할 수 없습니다."; // 저장소 오류 안내
                } // 누락 안내 끝
                return false; // 회차 저장 보존
            } // 기록 실패 끝
            if (!runStorage.TryMarkEnded(CurrentRunId, out error)) // 영구 기록 후 해당 회차 종료 검사
            { // 종료 실패 시작
                savedRunValid = false; // 종료 대기 회차의 재개 차단
                return false; // 기존 회차 보존과 재시도 허용
            } // 종료 실패 끝
            savedRun = runStorage.Load(out var warning); // 다른 회차가 있으면 보존한 상태 확인
            savedRunValid = savedRun != null && RunRestoreRules.IsValid(savedRun); // 보존된 다른 회차 재개 상태
            RunSaveMessage = warning; // 실제 저장 상태 안내
            return true; // 기록과 종료 완료
        } // 기록 끝
        public void OpenPause() // 탐험 중 일시정지 메뉴 열기
        { // 열기 시작
            var room = GameObject.Find("RoomTravel"); // 현재 던전 조회
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 방 제어 조회
            if (travel == null || travel.CaptureRun() == null || !Flow.TryOpenPause()) // 안정된 탐험 상태 검사
            { // 진입 거절 시작
                return; // 이동 중 메뉴 진입 차단
            } // 진입 거절 끝
            Input.SetPhase(GamePhase.Pause); // 탐험 입력 잠금과 메뉴 입력 활성화
            try // 메뉴 화면 생성
            { // 생성 시작
                pauseScreen = PauseScreenView.Create(this, travel.transform); // 현재 회차 메뉴 연결
            } // 생성 끝
            catch (System.Exception exception) // 화면 생성 실패 검사
            { // 실패 복구 시작
                Flow.TryClosePause(); // 탐험 상태 복구
                Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
                Debug.LogError("Pause screen failed: " + exception.Message); // 실제 오류 기록
            } // 실패 복구 끝
        } // 열기 끝
        public void ClosePause() // 메뉴를 닫고 탐험 복귀
        { // 닫기 시작
            if (!Flow.TryClosePause()) // 실제 메뉴 상태 검사
            { // 닫기 거절 시작
                return; // 잘못된 닫기 차단
            } // 닫기 거절 끝
            if (pauseScreen != null) // 메뉴 화면 존재 검사
            { // 화면 정리 시작
                pauseScreen.gameObject.SetActive(false); // 메뉴 즉시 숨김
                Destroy(pauseScreen.gameObject); // 기존 메뉴 정리
                pauseScreen = null; // 메뉴 참조 해제
            } // 화면 정리 끝
            Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
        } // 닫기 끝
        public bool ReloadRunFromPause(out string error) // 메뉴의 저장 회차 불러오기
        { // 불러오기 시작
            error = string.Empty; // 기본 오류 없음
            if (Flow.Phase != GamePhase.Pause || runStorage == null) // 메뉴와 저장소 검사
            { // 불러오기 거절 시작
                error = "저장된 회차를 불러올 수 없습니다."; // 불러오기 불가 안내
                return false; // 잘못된 요청 차단
            } // 불러오기 거절 끝
            var data = runStorage.Load(out var warning); // 최신 저장 파일과 보호 상태 확인
            if (data == null || !RunRestoreRules.IsValid(data)) // 저장 회차 유효성 검사
            { // 저장 오류 시작
                error = string.IsNullOrEmpty(warning) ? "저장된 회차가 없습니다." : warning; // 실제 불러오기 오류 안내
                return false; // 현재 회차 보존
            } // 저장 오류 끝
            savedRun = data; // 확인한 최신 회차 보관
            savedRunValid = true; // 회차 재개 허용
            RunSaveMessage = warning; // 저장 복구 안내 보관
            ReconcileDefeatedRun(); // 패배 기록과 재개 가능 상태 확인
            if (!savedRunValid) // 종료되었거나 검증되지 않은 회차 검사
            { // 불러오기 차단 시작
                error = RunSaveMessage; // 실제 저장 상태 안내
                return false; // 패배 회차의 재개 차단
            } // 불러오기 차단 끝
            return ContinueRun(); // 검증된 회차 재개
        } // 불러오기 끝
        public void OpenHistory() // 탐험 중 회차 기록 화면 열기
        { // 열기 시작
            var room = GameObject.Find("RoomTravel"); // 실제 던전 이동 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 던전 상태
            if (travel == null || !travel.IsReady || !Flow.TryOpenHistory()) // 던전 준비와 탐험 상태 검사
            { // 진입 거절 조건 시작
                return; // 준비되지 않은 기록 화면 차단
            } // 진입 거절 조건 끝
            Input.SetPhase(GamePhase.History); // 탐험 입력 잠금과 기록 입력 활성화
            try // 실제 기록 화면 준비
            { // 화면 준비 시작
                historyScreen = HistoryScreenView.Create(this, travel.transform); // 현재 회차 기록 화면 생성
            } // 화면 준비 끝
            catch (System.Exception exception) // 기록 화면 생성 실패 복구
            { // 실패 복구 시작
                Flow.TryCloseHistory(); // 기존 탐험 상태 복구
                Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
                Debug.LogError("History screen failed: " + exception.Message); // 실제 오류 기록
            } // 실패 복구 끝
        } // 열기 끝
        public void CloseHistory() // 회차 기록 닫고 탐험 복귀
        { // 닫기 시작
            if (!Flow.TryCloseHistory()) // 실제 기록 화면 상태 검사
            { // 닫기 거절 조건 시작
                return; // 중복 또는 잘못된 닫기 차단
            } // 닫기 거절 조건 끝
            if (historyScreen != null) // 기록 화면 존재 검사
            { // 화면 정리 시작
                historyScreen.gameObject.SetActive(false); // 화면 즉시 숨김
                Destroy(historyScreen.gameObject); // 기록 화면 객체 정리
                historyScreen = null; // 기록 화면 참조 해제
            } // 화면 정리 끝
            Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
        } // 닫기 끝
        private void MoveHistoryPage(int direction) // 현재 기록 화면의 페이지 이동
        { // 이동 시작
            if (Flow.Phase == GamePhase.History && historyScreen != null) // 기록 화면 활성 상태 검사
            { // 이동 조건 시작
                historyScreen.MovePage(direction); // 요청한 기록 페이지 표시
            } // 이동 조건 끝
        } // 이동 끝
        public void OpenFullMap() // 탐험 중 전체 지도 열기
        { // 열기 시작
            var room = GameObject.Find("RoomTravel"); // 실제 던전 이동 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 방과 발견 기록
            if (travel == null || !travel.IsReady || !Flow.TryOpenFullMap()) // 던전 준비와 탐험 상태 검사
            { // 진입 거절 조건 시작
                return; // 준비되지 않은 지도 진입 차단
            } // 진입 거절 조건 끝
            Input.SetPhase(GamePhase.FullMap); // 탐험 입력 잠금과 지도 닫기 입력
            try // 전체 지도 화면 준비
            { // 화면 준비 시작
                fullMapScreen = FullMapScreenView.Create(travel); // 발견된 방의 전체 지도 생성
            } // 화면 준비 끝
            catch (System.Exception exception) // 지도 화면 생성 실패 복구
            { // 실패 복구 시작
                Flow.TryCloseFullMap(); // 기존 탐험 상태 복구
                Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
                Debug.LogError("Full map screen failed: " + exception.Message); // 실제 오류 기록
            } // 실패 복구 끝
        } // 열기 끝
        public void CloseFullMap() // 전체 지도 닫고 탐험 복귀
        { // 닫기 시작
            if (!Flow.TryCloseFullMap()) // 실제 지도 상태 검사
            { // 닫기 거절 조건 시작
                return; // 중복 또는 잘못된 닫기 차단
            } // 닫기 거절 조건 끝
            if (fullMapScreen != null) // 지도 화면 존재 검사
            { // 화면 정리 시작
                fullMapScreen.gameObject.SetActive(false); // 화면 즉시 숨김
                Destroy(fullMapScreen.gameObject); // 지도 객체 정리
                fullMapScreen = null; // 지도 참조 해제
            } // 화면 정리 끝
            Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
        } // 닫기 끝
        public void OpenInventory() // 탐험 중 인벤토리 화면 열기
        { // 열기 시작
            if (!Flow.TryOpenInventory()) // 인벤토리 진입 상태 검사
            { // 진입 거절 조건 시작
                return; // 중복 또는 탐험 외 요청 차단
            } // 진입 거절 조건 끝
            Input.SetPhase(GamePhase.Inventory); // 탐험 입력 잠금과 화면 입력 활성화
            try // 실제 인벤토리 화면 준비
            { // 화면 준비 시작
                inventoryScreen = InventoryScreenView.Create(this); // 인벤토리 화면 생성
            } // 화면 준비 끝
            catch (System.Exception exception) // 화면 생성 실패 복구
            { // 실패 복구 시작
                Flow.TryCloseInventory(); // 기존 탐험 상태 복구
                Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
                Debug.LogError("Inventory screen failed: " + exception.Message); // 실제 오류 기록
            } // 실패 복구 끝
        } // 열기 끝
        public void CloseInventory() // 인벤토리 화면 닫고 탐험 복귀
        { // 닫기 시작
            if (!Flow.TryCloseInventory()) // 인벤토리 상태 검사
            { // 닫기 거절 조건 시작
                return; // 중복 또는 잘못된 닫기 차단
            } // 닫기 거절 조건 끝
            if (inventoryScreen != null) // 인벤토리 화면 존재 검사
            { // 화면 정리 시작
                inventoryScreen.gameObject.SetActive(false); // 기존 화면 즉시 숨김
                Destroy(inventoryScreen.gameObject); // 인벤토리 화면 정리
                inventoryScreen = null; // 화면 참조 해제
            } // 화면 정리 끝
            Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
            InventoryClosed?.Invoke(); // 탐험 화면 복귀 알림
        } // 닫기 끝
        public bool TryRecordObservation(string id) // 관찰 결과의 안전한 영구 저장
        { // 저장 시작
            if (Observations.Contains(id)) // 이미 기록한 대상 검사
            { // 기존 기록 조건 시작
                return true; // 중복 저장 없이 기존 정보 유지
            } // 기존 기록 조건 끝
            if (observationStorage == null) // 저장 기능 준비 검사
            { // 저장 불가 조건 시작
                ObservationSaveMessage = "관찰 저장 경로를 사용할 수 없습니다. 기존 파일을 확인해 주세요."; // 저장 경로 오류 안내
                return false; // 임시 관찰 결과 보존
            } // 저장 불가 조건 끝
            var candidate = new ObservationJournal(); // 기존 기록의 독립 복사
            foreach (var existing in Observations.Snapshot()) // 저장된 대상 순회
            { // 기존 기록 복사 시작
                candidate.TryRecord(existing); // 검증된 기존 기록 보존
            } // 기존 기록 복사 끝
            if (!candidate.TryRecord(id)) // 새로운 대상 식별자 검사
            { // 잘못된 대상 조건 시작
                ObservationSaveMessage = "관찰 대상을 기록할 수 없습니다."; // 잘못된 기록 안내
                return false; // 잘못된 관찰 저장 차단
            } // 잘못된 대상 조건 끝
            if (!observationStorage.TrySave(candidate, out var error)) // 안전한 파일 저장 검사
            { // 저장 실패 조건 시작
                ObservationSaveMessage = error; // 실제 저장 실패 안내 보관
                return false; // 성공 판정은 화면에서 재시도 대기
            } // 저장 실패 조건 끝
            Observations = candidate; // 성공적으로 저장된 기록 확정
            ObservationSaveMessage = string.Empty; // 이전 저장 오류 안내 해제
            return true; // 실제 영구 기록 성공
        } // 저장 끝
        public void RecordExplorationEvent(string message) // 현재 회차의 실제 탐험 사건 기록
        { // 기록 시작
            if (ExplorationHistory.Add(message)) // 유효한 사건 기록 검사
            { // 기록 알림 시작
                ExplorationEvent?.Invoke(message); // 화면에 실제 기록 추가 알림
            } // 기록 알림 끝
        } // 기록 끝
        public void RecordPotionAcquisition() // 실제 회복약 획득의 탐험 기록 전달
        { // 기록 시작
            if (Flow.Phase == GamePhase.Inventory) // 탐험 인벤토리 상태 검사
            { // 기록 조건 시작
                RecordExplorationEvent("시험 소형 회복약 1개 획득"); // 확인된 획득 결과 보관
            } // 기록 조건 끝
        } // 기록 끝
        public bool TryUseTrialPotion(out string message) // 탐험 인벤토리의 시험 회복약 사용
        { // 사용 시작
            message = "사용할 소형 회복약이 없습니다"; // 회복약 부재 안내
            if (Flow.Phase != GamePhase.Inventory) // 실제 인벤토리 상태 검사
            { // 잘못된 상태 조건 시작
                message = "지금은 회복약을 사용할 수 없습니다"; // 사용 불가 상태 안내
                return false; // 탐험 인벤토리 외 사용 차단
            } // 잘못된 상태 조건 끝
            var index = -1; // 사용 가능한 첫 회복약 슬롯
            for (var slot = 0; slot < Inventory.Slots.Count; slot++) // 현재 슬롯 순회
            { // 슬롯 검사 시작
                if (Inventory.Slots[slot].ItemId == TrialPotionId && Inventory.Slots[slot].Quantity > 0) // 실제 회복약 묶음 검사
                { // 회복약 조건 시작
                    index = slot; // 첫 회복약 슬롯 보관
                    break; // 추가 슬롯 검사 종료
                } // 회복약 조건 끝
            } // 슬롯 검사 끝
            if (index < 0) // 회복약 부재 검사
            { // 부재 조건 시작
                return false; // 아이템 수량과 체력 보존
            } // 부재 조건 끝
            if (!PotionRecoveryRules.TryRecover(PlayerHealth, 100, TrialPotionRecovery, out var recovered)) // 실제 회복 가능 여부 검사
            { // 회복 불가 조건 시작
                message = "회복할 체력이 없습니다"; // 효과 없는 사용 안내
                return false; // 효과 없는 회복약 소비 차단
            } // 회복 불가 조건 끝
            if (!Inventory.Remove(index, 1, out var removed) || removed != 1) // 실제 회복약 한 개 소비 검사
            { // 소비 실패 조건 시작
                message = "회복약을 사용할 수 없습니다"; // 소비 실패 안내
                return false; // 소비 없이 체력 변경 차단
            } // 소비 실패 조건 끝
            var amount = recovered - PlayerHealth; // 실제 회복한 체력
            UpdatePlayerHealth(recovered); // 최대 체력 안에서 회복 적용
            message = "체력 " + amount + " 회복 · 소형 회복약 1개 사용"; // 실제 회복과 소비 안내
            RecordExplorationEvent(message); // 실제 회복 성공의 탐험 기록 보관
            return true; // 체력 회복과 아이템 소비 완료
        } // 사용 끝
        public bool TryUseInventoryConsumable(int index, out string message) // 선택한 시험 소비품의 탐험 사용
        { // 사용 시작
            message = "사용할 수 없는 아이템입니다"; // 기본 사용 불가 안내
            if (Flow.Phase != GamePhase.Inventory || index < 0 || index >= Inventory.Slots.Count) // 탐험 인벤토리와 슬롯 범위 검사
            { // 사용 거절 시작
                return false; // 잘못된 상태와 슬롯 차단
            } // 사용 거절 끝
            var item = TrialConsumableRules.Find(Inventory.Slots[index].ItemId); // 선택한 실제 소비품 정의
            if (item == null) // 지원하는 소비품 검사
            { // 미지원 시작
                return false; // 알 수 없는 아이템 보존
            } // 미지원 끝
            var current = item.Resource == TrialConsumableResource.Health ? PlayerHealth : PlayerMana; // 선택한 자원의 현재 값
            var maximum = item.Resource == TrialConsumableResource.Health ? 100 : 50; // 현재 시험 최대 자원
            if (!TrialConsumableRules.TryUse(Inventory, index, current, maximum, out var recovered)) // 실제 회복과 소비 검사
            { // 회복 실패 시작
                message = "회복할 자원이 없습니다"; // 최대치 또는 사용 불가 안내
                return false; // 아이템과 자원 보존
            } // 회복 실패 끝
            if (item.Resource == TrialConsumableResource.Health) // 체력 회복 아이템 검사
            { // 체력 회복 시작
                UpdatePlayerHealth(recovered); // 확인된 체력 반영
            } // 체력 회복 끝
            else // 마나 회복 분기
            { // 마나 회복 시작
                UpdatePlayerMana(recovered); // 확인된 마나 반영
            } // 마나 회복 끝
            message = item.Name + " 사용 · " + (item.Resource == TrialConsumableResource.Health ? "체력 " : "마나 ") + (recovered - current) + " 회복"; // 실제 사용 결과 안내
            RecordExplorationEvent(message); // 성공한 아이템 사용 기록
            return true; // 확인된 소비와 회복 성공
        } // 사용 끝
        public bool EnsureTrialShop(out string message) // 현재 층의 시험 상점 준비와 저장
        { // 준비 시작
            message = string.Empty; // 기본 안내 초기화
            var room = GameObject.Find("RoomTravel"); // 현재 던전 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 층과 이동 상태
            if (Flow.Phase != GamePhase.Inventory || travel == null || !travel.IsReady) // 실제 인벤토리와 안정된 던전 검사
            { // 준비 오류 시작
                message = "현재 상점을 열 수 없습니다."; // 잘못된 위치 안내
                return false; // 잘못된 상점 진입 차단
            } // 준비 오류 끝
            foreach (var saved in shopStocks) // 이전에 방문한 층별 상점 순회
            { // 상점 조회 시작
                if (saved.floor == travel.Floor) // 현재 층의 기존 상점 검사
                { // 기존 상점 시작
                    if (!ShopState.TryRestore(saved, out var restored)) // 기존 가격과 재고 복원 검사
                    { // 복원 오류 시작
                        message = "저장된 상점을 확인할 수 없습니다."; // 손상된 상점 안내
                        return false; // 다른 가격 재생성 차단
                    } // 복원 오류 끝
                    Shop = restored; // 저장된 현재 층 상점 연결
                    return true; // 기존 상품과 재고 재사용
                } // 기존 상점 끝
            } // 상점 조회 끝
            var generated = TrialShopCatalog.Create(travel.Floor, new System.Random()); // 기획 범위 내 최초 시험 가격 생성
            if (!ShopState.TryRestore(generated, out var shop)) // 실제 상품과 가격 검증
            { // 생성 오류 시작
                message = "시험 상점을 준비할 수 없습니다."; // 생성 오류 안내
                return false; // 잘못된 상점 차단
            } // 생성 오류 끝
            return TryPersistEconomy(Inventory, Relics, shop, Gold, "시험 상점 준비", out message); // 상품 가격의 사전 저장
        } // 준비 끝
        public bool TryBuyShopItem(int offerIndex, out string message) // 확인한 상점 상품의 원자적 구매
        { // 구매 시작
            message = string.Empty; // 기본 안내 초기화
            if (Flow.Phase != GamePhase.Inventory || Shop == null) // 실제 상점 진입 상태 검사
            { // 상태 오류 시작
                message = "상점이 열려 있지 않습니다."; // 잘못된 화면 안내
                return false; // 잘못된 구매 차단
            } // 상태 오류 끝
            if (!Shop.TryBuy(offerIndex, 1, Gold, Inventory, out var result, out message)) // 재고와 잔액 및 수용 검사
            { // 구매 거절 시작
                return false; // 기존 골드와 상품 및 아이템 보존
            } // 구매 거절 끝
            return TryPersistEconomy(result.Inventory, Relics, result.Shop, result.Gold, "상품 구매", out message); // 안전한 저장 후 거래 확정
        } // 구매 끝
        public bool TrySellShopItem(int offerIndex, int inventoryIndex, out string message) // 확인한 가격과 보유품의 원자적 판매
        { // 판매 시작
            message = string.Empty; // 기본 안내 초기화
            if (Flow.Phase != GamePhase.Inventory || Shop == null) // 실제 상점 진입 상태 검사
            { // 상태 오류 시작
                message = "상점이 열려 있지 않습니다."; // 잘못된 화면 안내
                return false; // 잘못된 판매 차단
            } // 상태 오류 끝
            if (!Shop.TrySell(offerIndex, inventoryIndex, 1, Gold, Inventory, out var result, out message)) // 같은 상품 가격과 실제 보유품 검사
            { // 판매 거절 시작
                return false; // 기존 골드와 상품 및 아이템 보존
            } // 판매 거절 끝
            return TryPersistEconomy(result.Inventory, Relics, result.Shop, result.Gold, "아이템 판매", out message); // 안전한 저장 후 거래 확정
        } // 판매 끝
        public bool TryAcquireRelic(string id, string replaceId, out string message) // 시험 유물 획득 또는 명시적 교체
        { // 획득 시작
            message = string.Empty; // 기본 안내 초기화
            if (Flow.Phase != GamePhase.Inventory || Shop == null) // 실제 시험 경제 화면 상태 검사
            { // 상태 오류 시작
                message = "시험 유물을 받을 수 없습니다."; // 잘못된 위치 안내
                return false; // 다른 화면의 획득 차단
            } // 상태 오류 끝
            var valid = string.IsNullOrEmpty(replaceId) ? Relics.TryAcquire(id, out var next) : Relics.TryReplace(replaceId, id, out next); // 확인한 기존 유물 또는 빈 슬롯 적용
            if (!valid) // 유물 한도와 중복 및 실제 대상 검사
            { // 획득 거절 시작
                message = "유물이 중복되었거나 보유 한도에 도달했습니다."; // 실제 획득 불가 안내
                return false; // 기존 유물과 효과 보존
            } // 획득 거절 끝
            return TryPersistEconomy(Inventory, next, Shop, Gold, "시험 유물 획득", out message); // 안전한 저장 후 유물 효과 적용
        } // 획득 끝
        private bool TryPersistEconomy(InventoryState inventory, RelicState relics, ShopState shop, int gold, string success, out string message) // 골드와 유물 및 상점 거래의 단일 저장
        { // 저장 시작
            message = string.Empty; // 기본 안내 초기화
            var savedId = savedRun == null ? null : savedRun.version < 7 ? runStorage?.LegacyRunId : savedRun.runId; // 기존 회차 식별자
            if (runStorage == null || savedId != null && savedId != CurrentRunId || inventory == null || relics == null || shop == null || gold < 0) // 저장 기능과 기존 회차 및 거래 결과 검사
            { // 저장 거절 시작
                message = "기존 회차 저장 또는 거래 상태를 확인해 주세요."; // 기존 저장 보호 안내
                return false; // 다른 회차 덮어쓰기와 잘못된 거래 차단
            } // 저장 거절 끝
            var room = GameObject.Find("RoomTravel"); // 실제 방 이동 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 탐험 상태
            var data = travel == null ? null : travel.CaptureInventoryRun(); // 안정된 인벤토리 상태의 저장본
            var changed = shop.Snapshot(); // 거래 후 층별 상품과 재고
            if (data == null || changed.floor != data.floor) // 실제 현재 층과 상점 일치 검사
            { // 층 오류 시작
                message = "현재 층의 상점을 저장할 수 없습니다."; // 층 불일치 안내
                return false; // 다른 층 거래 차단
            } // 층 오류 끝
            var shops = new System.Collections.Generic.List<ShopStockData>(CopyShopStocks(shopStocks)); // 이전 상점의 독립 복사
            var found = shops.FindIndex(value => value.floor == changed.floor); // 현재 층 상점의 저장 위치
            if (found < 0) // 처음 방문한 층 검사
            { // 새 상점 시작
                shops.Add(changed); // 고정된 새 상품과 재고 추가
            } // 새 상점 끝
            else // 기존 층 상점 분기
            { // 기존 상점 시작
                shops[found] = changed; // 거래 후 상품과 재고 반영
            } // 기존 상점 끝
            data.gold = gold; // 거래 후 골드 잔액
            data.slots = inventory.SaveSlots(); // 거래 후 독립 인벤토리
            data.relicIds = relics.SaveIds(); // 변경할 유물 목록
            data.shopStocks = shops.ToArray(); // 모든 방문 층의 상품과 재고
            if (!RunRestoreRules.IsValid(data) || !runStorage.TrySave(data, out message)) // 안전한 회차 저장 검사
            { // 저장 실패 시작
                if (string.IsNullOrEmpty(message)) // 상세 오류 누락 검사
                { // 오류 안내 시작
                    message = "거래를 저장할 수 없습니다."; // 기본 저장 실패 안내
                } // 오류 안내 끝
                return false; // 원본 골드와 아이템 및 재고 보존
            } // 저장 실패 끝
            Inventory = inventory; // 검증된 소지품 적용
            Relics = relics; // 저장된 유물 효과 적용
            Gold = gold; // 저장된 골드 반영
            Shop = shop; // 저장된 현재 상점 반영
            shopStocks = data.shopStocks; // 저장된 층별 상점 목록 반영
            savedRun = data; // 마지막 정상 저장 갱신
            savedRunValid = true; // 저장된 회차 재개 허용
            message = success + " · 저장 완료"; // 실제 거래와 저장 결과 안내
            return true; // 안전한 거래 확정 완료
        } // 저장 끝
        public string TrialMerchantRelationshipText // 시험 상인의 호감도와 기획 단계
        { // 속성 시작
            get // 관계 조회
            { // 조회 시작
                var value = NpcRelationships.GetAffection(TrialMerchantId); // 현재 상인의 영구 호감도
                var stage = NpcRelationshipRules.Stage(value); // 기획된 현재 관계 단계
                var label = stage == NpcRelationshipStage.Neutral ? "중립" : stage == NpcRelationshipStage.Interest ? "관심" : stage == NpcRelationshipStage.Trust ? "신뢰" : stage == NpcRelationshipStage.Special ? "특별" : "엔딩 가능"; // 실제 기획 단계 이름
                return "호감도 " + value + "/100 · " + label; // 수치와 단계 안내
            } // 조회 끝
        } // 속성 끝
        public bool TryGrantTrialFriendliness(out string message) // 회차당 한 번의 시험 우호 선택
        { // 적용 시작
            message = string.Empty; // 기본 안내 초기화
            if (trialNpcScreen == null || npcShopScreen != null || Flow.Phase != GamePhase.Inventory || relationshipStorage == null) // 실제 상인 화면과 저장소 검사
            { // 오류 시작
                message = "관계 기록을 사용할 수 없습니다."; // 저장 상태 안내
                return false; // 다른 화면에서 우호 적용 차단
            } // 오류 끝
            if (NpcRelationships.HasReceipt(CurrentRunId, TrialMerchantId)) // 현재 회차의 적용 이력 검사
            { // 중복 시작
                message = "이번 회차에 이미 우호 선택을 했습니다."; // 중복 선택 안내
                return false; // 같은 회차의 중복 반영 차단
            } // 중복 끝
            if (!NpcRelationships.TryGrantFriendliness(CurrentRunId, TrialMerchantId, out var next)) // 독립 관계와 적용 이력 생성
            { // 오류 시작
                message = "우호 선택을 확인할 수 없습니다."; // 잘못된 선택 안내
                return false; // 기존 관계 보존
            } // 오류 끝
            var previous = NpcRelationships.GetAffection(TrialMerchantId); // 변경 전 실제 호감도
            if (!relationshipStorage.TrySave(next, out message)) // 영구 관계와 적용 이력의 단일 저장
            { // 저장 오류 시작
                RelationshipSaveMessage = message; // 실제 저장 실패 안내 보관
                return false; // 저장 실패 시 실제 관계 보존
            } // 저장 오류 끝
            NpcRelationships = next; // 저장 성공 후 영구 관계 적용
            RelationshipSaveMessage = string.Empty; // 이전 저장 오류 해제
            var gained = next.GetAffection(TrialMerchantId) - previous; // 상한을 반영한 실제 증가량
            message = "시험 우호 선택 · 호감도 +" + gained + " · 이번 회차 완료"; // 실제 변경량과 제한 안내
            return true; // 안전한 관계 변경 완료
        } // 적용 끝
        public bool OpenTrialNpc() // 현재 방의 시험 상인 대화 시작
        { // 진입 시작
            var room = GameObject.Find("RoomTravel"); // 현재 던전 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 방 제어
            var target = travel == null ? null : travel.TrialNpcRoom; // 현재 층 시험 상인 방
            if (trialNpcScreen != null || target == null || travel.RoomX != target.X || travel.RoomY != target.Y || !Flow.TryOpenInventory()) // 현재 상인과 탐험 상태 검사
            { // 오류 시작
                return false; // 다른 방과 중복 화면 차단
            } // 오류 끝
            Input.SetPhase(GamePhase.Inventory); // 탐험 입력 잠금
            try // 대화 화면 생성 보호
            { // 생성 시작
                trialNpcScreen = TrialNpcScreenView.Create(this); // 실제 상인 대화 표시
                return true; // 대화 진입 완료
            } // 생성 끝
            catch (System.Exception exception) // 화면 생성 실패 검사
            { // 복구 시작
                Flow.TryCloseInventory(); // 기존 탐험 상태 복구
                Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
                Debug.LogError("Trial merchant screen failed: " + exception.Message); // 실제 화면 오류 기록
                return false; // 불완전한 대화 화면 차단
            } // 복구 끝
        } // 진입 끝
        public bool TryOpenNpcShop(out string message) // 상인의 실제 시험 상점 열기
        { // 상점 진입 시작
            message = string.Empty; // 기본 안내 초기화
            if (trialNpcScreen == null || npcShopScreen != null || Flow.Phase != GamePhase.Inventory) // 실제 대화와 중복 화면 검사
            { // 오류 시작
                message = "상인 상점을 열 수 없습니다."; // 진입 오류 안내
                return false; // 다른 화면과 중복 진입 차단
            } // 오류 끝
            if (!EnsureTrialShop(out message)) // 현재 층 상품과 가격 저장 검사
            { // 저장 오류 시작
                return false; // 미저장 상점 차단
            } // 저장 오류 끝
            try // 상점 화면 생성 보호
            { // 생성 시작
                npcShopScreen = EconomyScreenView.Create(this, CloseNpcShop, false); // 기존 거래 규칙과 무료 시험 유물 차단
                trialNpcScreen.gameObject.SetActive(false); // 상점 아래 대화의 키보드 입력 차단
                return true; // 실제 상점 진입 성공
            } // 생성 끝
            catch (System.Exception exception) // 화면 생성 실패 검사
            { // 오류 시작
                Debug.LogError("Merchant shop failed: " + exception.Message); // 실제 화면 오류 기록
                message = "상점 화면을 열 수 없습니다."; // 화면 오류 안내
                return false; // 저장된 상품과 대화 보존
            } // 오류 끝
        } // 상점 진입 끝
        private void CloseNpcShop() // 상점 종료 후 같은 상인 대화 복귀
        { // 복귀 시작
            npcShopScreen = null; // 종료된 상점 참조 해제
            if (trialNpcScreen != null) // 대화 화면 존재 검사
            { // 선택 복구 시작
                trialNpcScreen.gameObject.SetActive(true); // 상점 종료 후 대화 화면 복구
                trialNpcScreen.SelectDefault(); // 안전한 떠나기 기본 선택
            } // 선택 복구 끝
        } // 복귀 끝
        public void CloseTrialNpc() // 상인 대화를 닫고 탐험 복귀
        { // 종료 시작
            if (trialNpcScreen == null || npcShopScreen != null || !Flow.TryCloseInventory()) // 실제 대화와 상점 종료 검사
            { // 종료 차단 시작
                return; // 상점 중 대화 종료 차단
            } // 종료 차단 끝
            trialNpcScreen.gameObject.SetActive(false); // 이전 대화 입력 차단
            Destroy(trialNpcScreen.gameObject); // 상인 대화 화면 정리
            trialNpcScreen = null; // 화면 참조 해제
            Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
        } // 종료 끝
        public TrialEventBattleSaveData CurrentTrialEventBattle => trialEventBattle == null ? null : trialEventBattle.Snapshot(); // 현재 전투의 독립 저장 상태
        public bool TryStartEventBattle(CombatScreenView source, out string message) // 일반 전투의 실제 참가자를 별도 전투로 전환
        { // 전환 시작
            message = string.Empty; // 기본 안내 초기화
            if (Flow.Phase != GamePhase.Combat || source == null || source != combatScreen || trialEventBattleScreen != null) // 실제 일반 전투와 중복 화면 검사
            { // 오류 시작
                message = "이벤트 전투를 시작할 수 없습니다."; // 전환 오류 안내
                return false; // 다른 화면의 전투 차단
            } // 오류 끝
            if (!source.TryCaptureEventBattle(out var initial, out message)) // 생존 적과 실제 전투 상태 검사
            { // 오류 시작
                return false; // 잘못된 참가자 차단
            } // 오류 끝
            if (!CommitTrialEventBattle(initial, out message)) // 전환 전 참가자의 안전한 저장
            { // 저장 오류 시작
                return false; // 원래 일반 전투 보존
            } // 저장 오류 끝
            source.gameObject.SetActive(false); // 기존 전투의 입력 차단
            if (!ShowTrialEventBattle()) // 실제 화면 생성 검사
            { // 화면 오류 시작
                message = "이벤트 전투 화면을 열지 못해 저장된 회차를 보존합니다."; // 복원 안내
                ReturnToTitle(); // 저장된 전투를 보존한 타이틀 복귀
                return false; // 중복 일반 행동 차단
            } // 화면 오류 끝
            return true; // 안전한 이벤트 전투 진입
        } // 전환 끝
        private bool ShowTrialEventBattle() // 저장된 이벤트 전투 화면 생성
        { // 표시 시작
            try // 화면 생성 보호
            { // 생성 시작
                trialEventBattleScreen = TrialEventBattleScreenView.Create(this); // 실제 저장 전투 표시
                return true; // 화면 생성 성공
            } // 생성 끝
            catch (System.Exception exception) // 화면 생성 실패 검사
            { // 오류 시작
                Debug.LogError("Event battle screen failed: " + exception.Message); // 실제 화면 오류 기록
                return false; // 저장된 전투 보존
            } // 오류 끝
        } // 표시 끝
        private bool CommitTrialEventBattle(TrialEventBattleState next, out string message) // 각 행동의 이벤트 전투 상태 저장
        { // 저장 시작
            message = string.Empty; // 기본 안내 초기화
            var room = GameObject.Find("RoomTravel"); // 현재 전투 방 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 실제 조우 제어
            var savedId = savedRun == null ? null : savedRun.version < 7 ? runStorage?.LegacyRunId : savedRun.runId; // 기존 회차 식별자
            if (Flow.Phase != GamePhase.Combat || runStorage == null || travel == null || next == null || savedId != null && savedId != CurrentRunId) // 실제 전투와 기존 저장 검사
            { // 오류 시작
                message = "기존 저장 또는 전투 상태를 확인해 주세요."; // 다른 회차 보호 안내
                return false; // 기존 회차와 전투 상태 보존
            } // 오류 끝
            var data = travel.CaptureBattleRewardRun(); // 안정된 전투 위치의 독립 저장본
            if (data == null) // 실제 위치 저장 검사
            { // 오류 시작
                message = "현재 전투를 저장할 수 없습니다."; // 전투 위치 오류 안내
                return false; // 실제 상태 보존
            } // 오류 끝
            data.hasTrialEventBattle = true; // 별도 전투 복원 표시
            data.trialEventBattle = next.Snapshot(); // 완료한 행동과 참가자 기록
            if (!RunRestoreRules.IsValid(data) || !runStorage.TrySave(data, out message)) // 참가자와 행동의 재생 검증 및 저장
            { // 오류 시작
                if (string.IsNullOrEmpty(message)) // 상세 오류 누락 검사
                { // 오류 안내 시작
                    message = "이벤트 전투 상태를 저장할 수 없습니다."; // 기본 저장 오류
                } // 오류 안내 끝
                return false; // 저장 실패 시 이전 상태 보존
            } // 오류 끝
            trialEventBattle = next; // 저장 후 실제 전투 상태 적용
            savedRun = data; // 마지막 정상 저장 갱신
            savedRunValid = true; // 저장된 전투 재개 허용
            return true; // 실제 행동 저장 성공
        } // 저장 끝
        private int EventBattleRoll() // 시험 전투의 주도권 주사위
        { // 주사위 시작
            return TrialEventBattleRollOverride == null ? chestRandom.Next(1, 21) : TrialEventBattleRollOverride(1, 21); // 실제 난수 또는 검증값
        } // 주사위 끝
        public bool TryEventBattleAction(int targetIndex, int action, out string message) // 플레이어 행동과 전투 기록 저장
        { // 행동 시작
            message = string.Empty; // 기본 안내 초기화
            if (trialEventBattleScreen == null || trialEventBattle == null || trialEventBattle.Snapshot().turn != -1 || !trialEventBattle.TryAction(targetIndex, action, EventBattleRoll(), EventBattleRoll(), out var next)) // 플레이어 주도권과 대상 및 행동 검사
            { // 오류 시작
                message = "현재 행동을 선택할 수 없습니다. 시험 전투를 중단할 수 있습니다."; // 행동 한도와 상태 안내
                return false; // 잘못된 행동 차단
            } // 오류 끝
            if (!CommitTrialEventBattle(next, out message)) // 완료 행동의 안전한 저장
            { // 오류 시작
                return false; // 저장 전 상태 보존
            } // 오류 끝
            var observed = next.Snapshot().enemies[targetIndex]; // 저장된 실제 대상 상태
            message = action == 0 ? "관찰 · 상대 " + (observed.slot + 1) + " · 체력 " + observed.health + " · 시험 매력 " + observed.charm : "시험 행동 " + action + " 적용"; // 관찰 정보 또는 실제 행동 결과
            QueueEventBattleAuto(); // 다음 적 주도권의 자동 처리
            return true; // 저장된 행동 완료
        } // 행동 끝
        public bool TryAdvanceEventBattle(out string message) // 현재 적의 한 행동과 기록 저장
        { // 행동 시작
            message = string.Empty; // 기본 안내 초기화
            var current = CurrentTrialEventBattle; // 현재 전투 상태
            if (trialEventBattleScreen == null || trialEventBattle == null || current == null || current.outcome != 0 || current.turn < 0 || !trialEventBattle.TryAction(current.turn, 1, EventBattleRoll(), EventBattleRoll(), out var next)) // 실제 적 주도권과 시험 행동 검사
            { // 오류 시작
                message = "상대 행동을 진행할 수 없습니다. 시험 전투를 중단할 수 있습니다."; // 상태 또는 기록 한도 안내
                return false; // 잘못된 적 행동 차단
            } // 오류 끝
            if (!CommitTrialEventBattle(next, out message)) // 적 행동의 안전한 저장
            { // 오류 시작
                return false; // 이전 행동 상태 보존
            } // 오류 끝
            message = "상대의 시험 행동 완료"; // 저장된 적 행동 안내
            QueueEventBattleAuto(); // 이어지는 적 주도권 자동 처리
            return true; // 실제 적 행동 완료
        } // 행동 끝
        private void QueueEventBattleAuto() // 적 주도권의 중복 없는 자동 처리 요청
        { // 요청 시작
            var current = CurrentTrialEventBattle; // 현재 저장 상태
            if (trialEventBattleAuto == null && trialEventBattleScreen != null && current != null && current.outcome == 0 && current.turn >= 0) // 실제 적 차례와 중복 코루틴 검사
            { // 자동 처리 시작
                trialEventBattleAuto = StartCoroutine(AdvanceEventBattleAutomatically()); // 한 행동씩 저장하는 자동 처리
            } // 자동 처리 끝
        } // 요청 끝
        private IEnumerator AdvanceEventBattleAutomatically() // 적 행동별 저장과 화면 갱신
        { // 처리 시작
            yield return null; // 화면 표시와 입력 잠금 대기
            while (trialEventBattleScreen != null && CurrentTrialEventBattle != null && CurrentTrialEventBattle.outcome == 0 && CurrentTrialEventBattle.turn >= 0) // 남은 적 주도권 검사
            { // 적 행동 시작
                if (trialEventBattleScreen.IsRetreatConfirmationOpen) // 중단 확인 화면 검사
                { // 자동 행동 대기 시작
                    yield return null; // 사용자 확인 중 전투 상태 보존
                    continue; // 확인 종료 후 같은 적 차례 재검사
                } // 자동 행동 대기 끝
                if (!TryAdvanceEventBattle(out var message)) // 실제 적 행동과 저장 검사
                { // 오류 시작
                    trialEventBattleScreen.ShowAdvanceFailure(message); // 재시도 가능한 저장 오류 표시
                    break; // 무한 저장 재시도 차단
                } // 오류 끝
                trialEventBattleScreen.Refresh(); // 저장된 행동 상태 표시
                yield return null; // 다음 행동 전 화면 갱신
            } // 적 행동 끝
            trialEventBattleAuto = null; // 종료된 자동 처리 참조 해제
        } // 처리 끝
        public bool TryFinishEventBattle(out string message) // 실제 승패 결과의 안전한 확정
        { // 완료 시작
            message = string.Empty; // 기본 안내 초기화
            var room = GameObject.Find("RoomTravel"); // 현재 조우 방 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 실제 조우 제어
            var battle = CurrentTrialEventBattle; // 저장된 이벤트 전투 결과
            if (Flow.Phase != GamePhase.Combat || trialEventBattleScreen == null || travel == null || battle == null || battle.outcome == 0) // 실제 종료 상태 검사
            { // 오류 시작
                message = "전투 결과를 확인할 수 없습니다."; // 결과 오류 안내
                return false; // 미완료 전투 차단
            } // 오류 끝
            if (battle.outcome == 2) // 정력 소진 패배 검사
            { // 패배 시작
                if (!TryRecordDefeat(travel.CurrentMonsterId, BattleDefeatReason.StaminaZero, out message)) // 실제 상대와 패배 원인 기록
                { // 저장 오류 시작
                    return false; // 패배 기록 전 현재 전투 보존
                } // 저장 오류 끝
                CloseEventBattleScreen(); // 기록된 패배 화면과 자동 처리 정리
                ReturnToTitle(); // 기록과 회차 종료 후 타이틀 복귀
                return true; // 패배 결과 확정
            } // 패배 끝
            if (!ExperienceRules.TryAdvance(Level, Experience, battle.experience, out var growth)) // 실제 승리 경험치 성장 검사
            { // 오류 시작
                message = "전투 경험치를 계산할 수 없습니다."; // 성장 오류 안내
                return false; // 잘못된 보상 차단
            } // 오류 끝
            var data = travel.CaptureBattleRewardRun(); // 완료할 조우의 독립 저장본
            if (data == null || !travel.PrepareCompletedEncounter(data)) // 실제 조우 완료 검사
            { // 오류 시작
                message = "전투 완료 위치를 확인할 수 없습니다."; // 조우 오류 안내
                return false; // 중복 조우 완료 차단
            } // 오류 끝
            data.hasTrialEventBattle = false; // 별도 전투 복원 종료
            data.trialEventBattle = null; // 완료한 행동 기록 제거
            data.level = growth.Level; // 성장 후 레벨
            data.experience = growth.CurrentExperience; // 성장 후 경험치
            data.statPoints += growth.GainedStatPoints; // 추가 스탯 포인트
            if (!RunRestoreRules.IsValid(data) || !runStorage.TrySave(data, out message)) // 완료와 경험치의 단일 저장
            { // 오류 시작
                if (string.IsNullOrEmpty(message)) // 상세 오류 누락 검사
                { // 안내 시작
                    message = "전투 결과를 저장할 수 없습니다."; // 기본 저장 오류
                } // 안내 끝
                return false; // 지급과 조우 완료 보존
            } // 오류 끝
            savedRun = data; // 완료된 회차 저장
            Level = data.level; // 저장된 레벨 적용
            Experience = data.experience; // 저장된 경험치 적용
            StatPoints = data.statPoints; // 저장된 스탯 포인트 적용
            travel.CompleteCurrentEncounter(); // 저장된 실제 조우 완료 적용
            CloseEventBattleScreen(); // 별도 전투 화면 정리
            EndCombat(); // 실제 탐험 복귀
            return true; // 안전한 승리 확정
        } // 완료 끝
        private void CloseEventBattleScreen() // 저장된 이벤트 전투 화면 정리
        { // 정리 시작
            if (trialEventBattleAuto != null) // 진행 중인 적 행동 검사
            { // 자동 처리 정리 시작
                StopCoroutine(trialEventBattleAuto); // 남은 적 행동 중단
                trialEventBattleAuto = null; // 코루틴 참조 해제
            } // 자동 처리 정리 끝
            if (trialEventBattleScreen != null) // 실제 이벤트 전투 화면 검사
            { // 화면 제거 시작
                trialEventBattleScreen.gameObject.SetActive(false); // 화면 입력 차단
                Destroy(trialEventBattleScreen.gameObject); // 완료 화면 제거
                trialEventBattleScreen = null; // 화면 참조 해제
            } // 화면 제거 끝
            trialEventBattle = null; // 완료된 전투 참조 해제
        } // 정리 끝
        public bool TryRetreatEventBattle(out string message) // 보상 포기의 시험 전투 중단
        { // 중단 시작
            message = string.Empty; // 기본 안내 초기화
            var room = GameObject.Find("RoomTravel"); // 현재 조우 방 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 실제 방 제어
            var battle = CurrentTrialEventBattle; // 저장된 전투와 참가자
            if (Flow.Phase != GamePhase.Combat || trialEventBattleScreen == null || travel == null || battle == null || battle.outcome != 0 || !travel.CanReturnToPreviousRoom()) // 진행 전투와 안전한 이전 방 검사
            { // 오류 시작
                message = "이전 방으로 중단할 수 없습니다."; // 안전한 복귀 안내
                return false; // 잘못된 전투 중단 차단
            } // 오류 끝
            var data = travel.CaptureBattleRewardRun(); // 현재 위치의 독립 저장본
            if (data == null) // 안정된 저장 위치 검사
            { // 오류 시작
                message = "전투 중단을 저장할 수 없습니다."; // 위치 오류 안내
                return false; // 현재 전투 보존
            } // 오류 끝
            var health = new int[travel.IsGroupEncounter ? 2 : 1]; // 원래 조우의 적별 잔여 체력
            foreach (var enemy in battle.enemies) // 이벤트에 참여한 상대 순회
            { // 적 상태 복사 시작
                if (enemy.slot >= 0 && enemy.slot < health.Length && enemy.status == 0) // 아직 참여 중인 적 검사
                { // 생존 복사 시작
                    health[enemy.slot] = enemy.health; // 해당 슬롯의 실제 잔여 체력
                } // 생존 복사 끝
            } // 적 상태 복사 끝
            if (travel.IsGroupEncounter) // 실제 다수 조우 검사
            { // 다수 저장 시작
                data.groupEnemyHealth = health; // 제거된 적을 제외한 개별 체력
            } // 다수 저장 끝
            else if (travel.TutorialEncounter != null && travel.RoomX == travel.TutorialEncounter.RoomX && travel.RoomY == travel.TutorialEncounter.RoomY && !travel.TutorialCompleted) // 첫 조우 방 검사
            { // 첫 조우 저장 시작
                data.tutorialHealth = health[0]; // 살아 있는 첫 상대 체력
            } // 첫 조우 저장 끝
            else // 상위 개체 조우 분기
            { // 상위 저장 시작
                data.encounterHealth = health[0]; // 살아 있는 상위 상대 체력
            } // 상위 저장 끝
            var entry = travel.Layout.GetConnection(data.previousRoomX, data.previousRoomY, data.roomX, data.roomY); // 이전 방에서 현재 방으로 들어온 출구
            if (entry == null) // 실제 이전 방 연결 검사
            { // 연결 오류 시작
                message = "이전 방의 연결을 확인할 수 없습니다."; // 중단 위치 오류 안내
                return false; // 잘못된 복귀 저장 차단
            } // 연결 오류 끝
            var originX = data.roomX; // 현재 방 가로 좌표 보관
            var originY = data.roomY; // 현재 방 세로 좌표 보관
            data.roomX = data.previousRoomX; // 저장할 복귀 방 가로 좌표
            data.roomY = data.previousRoomY; // 저장할 복귀 방 세로 좌표
            data.playerX = entry.ApproachX; // 이전 방의 실제 복귀 가로 칸
            data.playerY = entry.ApproachY; // 이전 방의 실제 복귀 세로 칸
            data.previousRoomX = originX; // 복귀 전 방의 가로 좌표
            data.previousRoomY = originY; // 복귀 전 방의 세로 좌표
            data.hasPreviousRoom = true; // 실제 역방향 연결 유지
            data.hasTrialEventBattle = false; // 중단한 별도 전투 해제
            data.trialEventBattle = null; // 미완료 시험 전투 기록 제거
            if (!RunRestoreRules.IsValid(data) || !runStorage.TrySave(data, out message)) // 중단과 상대 체력의 단일 저장
            { // 오류 시작
                if (string.IsNullOrEmpty(message)) // 상세 오류 누락 검사
                { // 오류 안내 시작
                    message = "전투 중단을 저장할 수 없습니다."; // 기본 저장 오류
                } // 오류 안내 끝
                return false; // 저장 실패 시 전투 유지
            } // 오류 끝
            savedRun = data; // 안전한 중단 저장 갱신
            if (travel.IsGroupEncounter) // 다수 상대의 실제 체력 반영
            { // 다수 적용 시작
                travel.StoreCurrentGroupHealth(health); // 저장된 개별 체력 적용
            } // 다수 적용 끝
            else // 단일 상대 체력 반영
            { // 단일 적용 시작
                travel.StoreCurrentEncounterHealth(health[0]); // 저장된 잔여 체력 적용
            } // 단일 적용 끝
            var returned = travel.TryReturnToPreviousRoom(); // 실제 이전 방 복귀
            CloseEventBattleScreen(); // 중단한 전투 화면 정리
            EndCombat(); // 탐험 상태 복귀
            message = returned ? "시험 전투 중단 · 보상 포기" : "전투를 중단했습니다. 현재 방 상태를 확인하세요."; // 실제 복귀 결과 안내
            return true; // 저장된 중단 처리 완료
        } // 중단 끝
        public TrialEventSaveData CurrentEvent // 현재 방의 독립 이벤트 상태
        { // 속성 시작
            get // 현재 상태 조회
            { // 조회 시작
                var room = GameObject.Find("RoomTravel"); // 현재 던전 객체
                var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 방 제어
                var record = travel == null || !travel.IsTrialEventPending ? null : travel.CurrentTrialEventRecord; // 현재 방의 미완료 이벤트
                return record != null && TrialEventState.TryRestore(record.eventData, out var state) ? state.Snapshot() : null; // 독립 저장 상태 전달
            } // 조회 끝
        } // 속성 끝
        public string EventDisabledReason // 시험 행동의 실제 조건 안내
        { // 속성 시작
            get // 조건 조회
            { // 조회 시작
                return TrialEventRules.CanChoose(0, Gold, out var reason) ? string.Empty : reason + " · 현재 골드 " + Gold; // 비용과 현재 골드 안내
            } // 조회 끝
        } // 속성 끝
        public float EventProbability => TrialEventRules.SuccessCount(50) / 4f; // 장비를 제외한 실제 사백 조합 확률
        public bool OpenTrialEvent(out string message) // 현재 이벤트의 저장과 자동 화면 열기
        { // 열기 시작
            message = string.Empty; // 기본 안내 초기화
            var room = GameObject.Find("RoomTravel"); // 현재 던전 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 방 제어
            if (trialEventScreen != null || travel == null || !travel.IsTrialEventPending || !TrialEventState.TryRestore(CurrentEvent, out var state) || !Flow.TryOpenInventory()) // 현재 이벤트와 탐험 상태 검사
            { // 오류 시작
                message = "이벤트를 열 수 없습니다."; // 화면 오류 안내
                return false; // 중복 화면 차단
            } // 오류 끝
            Input.SetPhase(GamePhase.Inventory); // 탐험 입력 잠금
            if (!TryCommitTrialEvent(travel, state, Gold, out message)) // 선택 순서와 현재 위치의 사전 저장
            { // 저장 오류 시작
                Flow.TryCloseInventory(); // 탐험 상태 복구
                Input.SetPhase(GamePhase.Exploration); // 재시도 입력 복구
                return false; // 저장 전 화면 차단
            } // 저장 오류 끝
            try // 화면 생성 보호
            { // 생성 시작
                trialEventScreen = TrialEventScreenView.Create(this); // 저장된 선택 또는 결과 표시
                return true; // 화면 진입 완료
            } // 생성 끝
            catch (System.Exception exception) // 화면 생성 실패 검사
            { // 복구 시작
                Flow.TryCloseInventory(); // 탐험 상태 복구
                Input.SetPhase(GamePhase.Exploration); // 재시도 입력 복구
                Debug.LogError("Trial event screen failed: " + exception.Message); // 실제 오류 기록
                message = "이벤트 화면을 열 수 없습니다."; // 화면 오류 안내
                return false; // 저장된 상태 보존
            } // 복구 끝
        } // 열기 끝
        private bool TryCommitTrialEvent(RoomTravelController travel, TrialEventState next, int gold, out string message) // 이벤트와 골드의 단일 저장
        { // 저장 시작
            message = string.Empty; // 기본 안내 초기화
            var savedId = savedRun == null ? null : savedRun.version < 7 ? runStorage?.LegacyRunId : savedRun.runId; // 기존 회차 식별자
            if (Flow.Phase != GamePhase.Inventory || runStorage == null || travel == null || next == null || gold < 0 || savedId != null && savedId != CurrentRunId) // 화면과 회차 및 골드 검사
            { // 오류 시작
                message = "기존 저장 또는 이벤트 상태를 확인해 주세요."; // 안전한 저장 안내
                return false; // 다른 회차와 잘못된 상태 보호
            } // 오류 끝
            var data = travel.CaptureInventoryRun(); // 현재 위치의 독립 회차
            var events = travel.TrialEventSnapshotsWith(next); // 변경된 이벤트의 독립 목록
            if (data == null || events == null) // 안정된 위치와 기록 검사
            { // 오류 시작
                message = "현재 이벤트를 저장할 수 없습니다."; // 위치 오류 안내
                return false; // 원본 상태 보존
            } // 오류 끝
            data.trialEvents = events; // 선택 또는 결과의 고정 기록
            data.gold = gold; // 비용과 보상을 반영한 잔액
            if (!RunRestoreRules.IsValid(data) || !runStorage.TrySave(data, out message)) // 회차 검증과 실제 저장
            { // 오류 시작
                if (string.IsNullOrEmpty(message)) // 상세 오류 누락 검사
                { // 안내 시작
                    message = "이벤트를 저장할 수 없습니다."; // 기본 저장 오류
                } // 안내 끝
                return false; // 저장 실패 시 실제 상태 보존
            } // 오류 끝
            travel.ApplyTrialEvent(next); // 저장된 이벤트 단계 적용
            Gold = gold; // 저장된 재화 적용
            savedRun = data; // 마지막 정상 저장 갱신
            savedRunValid = true; // 재개 허용
            message = "이벤트 저장 완료"; // 실제 저장 결과 안내
            return true; // 원자적 저장 완료
        } // 저장 끝
        public bool TryChooseTrialEvent(int choice, out string message) // 고정 선택과 판정 결과 저장
        { // 선택 시작
            message = string.Empty; // 기본 안내 초기화
            var room = GameObject.Find("RoomTravel"); // 현재 던전 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 방 제어
            var saved = CurrentEvent; // 현재 이벤트 저장 상태
            if (trialEventScreen == null || travel == null || !TrialEventState.TryRestore(saved, out var state) || choice < 0 || choice > 1) // 화면과 실제 선택 검사
            { // 오류 시작
                message = "이벤트 선택을 확인할 수 없습니다."; // 선택 오류 안내
                return false; // 잘못된 선택 차단
            } // 오류 끝
            var index = System.Array.IndexOf(saved.choices, choice); // 저장된 표시 순서의 선택 위치
            var attack = choice == 0 ? 50 : 0; // 장비를 제외한 시험 이벤트 공격력
            var playerRoll = choice == 0 ? TrialEventRoll(1, 21) : 0; // 행동 시 플레이어 주사위
            var eventRoll = choice == 0 ? TrialEventRoll(1, 21) : 0; // 행동 시 이벤트 주사위
            if (!state.TryChoose(index, Gold, attack, playerRoll, eventRoll, System.Guid.NewGuid().ToString("N"), out var next, out message)) // 조건과 판정 결과 검사
            { // 오류 시작
                return false; // 선택과 결과 보존
            } // 오류 끝
            var result = next.Snapshot(); // 고정된 비용과 결과
            var balance = (long)Gold - result.cost + result.rewardGold; // 정수 범위를 보호한 재화 결과
            if (balance < 0 || balance > int.MaxValue) // 재화 범위 검사
            { // 오류 시작
                message = "이벤트 재화를 계산할 수 없습니다."; // 계산 오류 안내
                return false; // 잘못된 지급 차단
            } // 오류 끝
            return TryCommitTrialEvent(travel, next, (int)balance, out message); // 결과 표시 전 비용과 보상 저장
        } // 선택 끝
        private int TrialEventRoll(int minimum, int maximum) // 이벤트 판정 전용 난수
        { // 난수 시작
            return TrialEventRollOverride == null ? chestRandom.Next(minimum, maximum) : TrialEventRollOverride(minimum, maximum); // 실제 난수 또는 검증값
        } // 난수 끝
        public bool ConfirmTrialEvent(out string message) // 저장된 이벤트 결과 확인
        { // 확인 시작
            message = string.Empty; // 기본 안내 초기화
            var room = GameObject.Find("RoomTravel"); // 현재 던전 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 방 제어
            if (trialEventScreen == null || travel == null || !TrialEventState.TryRestore(CurrentEvent, out var state) || !state.TryComplete(out var next)) // 실제 결과와 완료 전이 검사
            { // 오류 시작
                message = "이벤트 결과를 확인할 수 없습니다."; // 확인 오류 안내
                return false; // 중복 완료 차단
            } // 오류 끝
            if (!TryCommitTrialEvent(travel, next, Gold, out message)) // 완료 기록의 안전한 저장
            { // 오류 시작
                return false; // 저장 실패 시 결과 화면 보존
            } // 오류 끝
            Flow.TryCloseInventory(); // 탐험 상태 복구
            trialEventScreen.gameObject.SetActive(false); // 이전 화면 입력 차단
            Destroy(trialEventScreen.gameObject); // 완료한 이벤트 화면 제거
            trialEventScreen = null; // 화면 참조 해제
            Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
            return true; // 확인 완료
        } // 확인 끝
        public ChestSaveData CurrentChest // 현재 방의 독립 상자 상태
        { // 속성 시작
            get // 현재 상자 조회
            { // 조회 시작
                var room = GameObject.Find("RoomTravel"); // 현재 던전 객체
                var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 실제 방 이동 제어
                return travel != null && travel.CurrentContent != null && travel.CurrentContent.TryGetChest(travel.RoomX, travel.RoomY, out var chest) ? chest.Snapshot() : null; // 현재 상자 또는 빈 상태
            } // 조회 끝
        } // 속성 끝
        public bool OpenChest() // 현재 방의 실제 상자 확인 화면 열기
        { // 열기 시작
            var chest = CurrentChest; // 현재 방의 독립 상자 상태
            if (chest == null || chest.completed || !Flow.TryOpenInventory()) // 상자와 탐험 화면 검사
            { // 진입 오류 시작
                return false; // 없는 상자와 중복 화면 차단
            } // 진입 오류 끝
            Input.SetPhase(GamePhase.Inventory); // 탐험 입력 잠금과 상자 UI 입력 활성화
            try // 상자 화면 생성
            { // 화면 생성 시작
                chestScreen = ChestScreenView.Create(this); // 실제 상자와 보상 화면 연결
                return true; // 상자 화면 진입 완료
            } // 화면 생성 끝
            catch (System.Exception exception) // 화면 준비 실패 검사
            { // 오류 복구 시작
                Flow.TryCloseInventory(); // 이전 탐험 상태 복구
                Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
                Debug.LogError("Chest screen failed: " + exception.Message); // 실제 화면 오류 기록
                return false; // 불완전한 상자 화면 차단
            } // 오류 복구 끝
        } // 열기 끝
        public void CloseChest() // 상자 화면을 닫고 탐험 복귀
        { // 닫기 시작
            if (chestScreen == null || !Flow.TryCloseInventory()) // 실제 상자 화면과 상태 검사
            { // 닫기 거절 시작
                return; // 다른 인벤토리 화면 차단
            } // 닫기 거절 끝
            chestScreen.gameObject.SetActive(false); // 이전 화면의 입력 차단
            Destroy(chestScreen.gameObject); // 실제 상자 화면 정리
            chestScreen = null; // 이전 화면 참조 해제
            Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
        } // 닫기 끝
        private int ChestRoll(int minimum, int maximum) // 상자와 함정의 시험 주사위 조회
        { // 주사위 시작
            return ChestRollOverride == null ? chestRandom.Next(minimum, maximum) : ChestRollOverride(minimum, maximum); // 실제 난수 또는 검증 주사위
        } // 주사위 끝
        public bool TryUnlockChestWithKey(out string message) // 열쇠와 상자 잠금 해제의 단일 저장
        { // 열쇠 사용 시작
            message = string.Empty; // 기본 안내 초기화
            var room = GameObject.Find("RoomTravel"); // 현재 던전 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 실제 방 제어
            if (Flow.Phase != GamePhase.Inventory || chestScreen == null || travel == null || !ChestState.TryRestore(CurrentChest, out var chest) || !chest.TryUnlockWithKey(Keys.Count, out var opened, out var remaining) || !travel.CurrentContent.TrySetChest(travel.RoomX, travel.RoomY, opened, out var next)) // 열쇠와 실제 상자 상태 검사
            { // 사용 오류 시작
                message = "열쇠를 사용할 수 없습니다."; // 잘못된 열쇠 사용 안내
                return false; // 기존 열쇠와 상자 보존
            } // 사용 오류 끝
            return TryCommitRoomContent(travel, next, PlayerHealth, remaining, Inventory, null, "열쇠로 잠금 해제", out message); // 안전한 저장 후 열쇠와 상자 적용
        } // 열쇠 사용 끝
        public bool TryForceChest(out string message) // 이벤트 공격력 기반의 단일 강제 개방
        { // 강제 시작
            message = string.Empty; // 기본 안내 초기화
            var room = GameObject.Find("RoomTravel"); // 현재 던전 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 실제 방 제어
            if (Flow.Phase != GamePhase.Inventory || chestScreen == null || travel == null || !ChestState.TryRestore(CurrentChest, out var chest) || !chest.TryForce(50, 50, ChestRoll(1, 21), ChestRoll(1, 21), out var opened, out var success) || !travel.CurrentContent.TrySetChest(travel.RoomX, travel.RoomY, opened, out var next)) // 시험 공격과 난이도 및 단일 시도 검사
            { // 판정 오류 시작
                message = "강제 개방을 시도할 수 없습니다."; // 재시도 불가 안내
                return false; // 기존 상자 보존
            } // 판정 오류 끝
            return TryCommitRoomContent(travel, next, PlayerHealth, Keys.Count, Inventory, null, success ? "강제 개방 성공" : "강제 개방 실패 · 열쇠 사용 가능", out message); // 실제 시도 결과를 안전하게 저장
        } // 강제 끝
        public bool TryRevealChest(out string message) // 상자의 미믹과 보상 후보 사전 확정
        { // 판정 시작
            message = string.Empty; // 기본 안내 초기화
            var room = GameObject.Find("RoomTravel"); // 현재 던전 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 실제 방 제어
            var saved = CurrentChest; // 현재 상자의 독립 저장본
            if (Flow.Phase != GamePhase.Inventory || chestScreen == null || travel == null || !ChestState.TryRestore(saved, out var chest)) // 실제 상자 화면과 상태 검사
            { // 상태 오류 시작
                message = "상자를 확인할 수 없습니다."; // 상자 오류 안내
                return false; // 잘못된 후보 생성 차단
            } // 상태 오류 끝
            var grade = (ChestGrade)saved.grade; // 실제 상자 등급
            var roll = ChestRoll(0, 100); // 등급별 미믹 주사위
            var minimum = grade == ChestGrade.Common ? 1 : grade == ChestGrade.Advanced ? 2 : 3; // 기획의 등급별 최소 보상 수
            var baseCount = ChestRoll(minimum, minimum + 2); // 기획 범위의 기본 보상 수
            if (roll < 0 || roll >= 100 || !ChestRules.TryRewardCount(grade, baseCount, saved.wasLocked, roll < ChestRules.MimicChance(grade), out var count)) // 미믹 난수와 기획 수량 검사
            { // 판정 오류 시작
                message = "상자 보상을 결정할 수 없습니다."; // 시험 판정 오류 안내
                return false; // 잘못된 후보와 재화 차단
            } // 판정 오류 끝
            var rewards = new ChestRewardCandidateData[count]; // 실제 고정 아이템 후보 목록
            var kinds = new[] { "trial-small-potion", "trial-large-potion", "trial-mana-potion" }; // 정식 분포 미정의 시험 소비품
            for (var index = 0; index < count; index++) // 확정 보상 수 순회
            { // 후보 생성 시작
                var choice = ChestRoll(0, kinds.Length); // 시험 소비품 종류 추첨
                if (choice < 0 || choice >= kinds.Length) // 시험 난수 범위 검사
                { // 난수 오류 시작
                    message = "상자 보상을 결정할 수 없습니다."; // 잘못된 난수 안내
                    return false; // 후보 사전 저장 차단
                } // 난수 오류 끝
                rewards[index] = new ChestRewardCandidateData { candidateId = System.Guid.NewGuid().ToString("N"), itemId = kinds[choice], quantity = 1 }; // 개별 고정 시험 보상
            } // 후보 생성 끝
            if (!chest.TryReveal(roll, baseCount, System.Guid.NewGuid().ToString("N"), rewards, out var revealed) || !travel.CurrentContent.TrySetChest(travel.RoomX, travel.RoomY, revealed, out var next)) // 고정 후보와 상자 전이 검사
            { // 전이 오류 시작
                message = "상자를 열 수 없습니다."; // 상자 상태 오류 안내
                return false; // 미확정 상자 보존
            } // 전이 오류 끝
            if (!TryCommitRoomContent(travel, next, PlayerHealth, Keys.Count, Inventory, null, "상자 판정 저장", out message)) // 후보의 화면 표시 전 저장 검사
            { // 저장 오류 시작
                return false; // 후보 노출과 재추첨 충돌 차단
            } // 저장 오류 끝
            if (revealed.Snapshot().mimicState == 2) // 확정된 미믹 검사
            { // 미믹 조우 시작
                CloseChest(); // 기존 상자 화면 종료
                if (!StartEncounter()) // 실제 전투 전 조우 연출 요청
                { // 조우 오류 시작
                    message = "미믹 조우를 시작하지 못했습니다. 상자를 다시 확인하세요."; // 저장된 후보의 재시도 안내
                    return false; // 확정한 미믹 상태 보존
                } // 조우 오류 끝
            } // 미믹 조우 끝
            return true; // 확정된 상자 또는 미믹 처리 완료
        } // 판정 끝
        private bool TryCommitRoomContent(RoomTravelController travel, FloorRoomContent next, int health, int keys, InventoryState inventory, ExperienceProgress growth, string success, out string message) // 방 콘텐츠와 회차의 원자적 저장
        { // 저장 시작
            message = string.Empty; // 기본 안내 초기화
            var savedId = savedRun == null ? null : savedRun.version < 7 ? runStorage?.LegacyRunId : savedRun.runId; // 기존 회차 식별자
            if (travel == null || next == null || runStorage == null || savedId != null && savedId != CurrentRunId || health <= 0 || keys < 0 || inventory == null) // 회차와 변경 상태 검사
            { // 오류 시작
                message = "기존 저장 또는 방 상태를 확인해 주세요."; // 안전한 저장 안내
                return false; // 다른 회차와 잘못된 상태 보호
            } // 오류 끝
            var data = Flow.Phase == GamePhase.Inventory ? travel.CaptureInventoryRun() : Flow.Phase == GamePhase.Combat ? travel.CaptureBattleRewardRun() : travel.CaptureRun(); // 현재 단계의 독립 저장본
            var contents = travel.RoomContentSnapshotsWith(next); // 변경된 층을 포함한 독립 기록
            if (data == null || contents == null) // 안정된 위치와 층 기록 검사
            { // 오류 시작
                message = "현재 방을 저장할 수 없습니다."; // 위치 저장 실패 안내
                return false; // 원본 상태 보존
            } // 오류 끝
            data.roomContents = contents; // 변경된 층별 기록
            data.health = health; // 변경된 체력
            data.keys = keys; // 변경된 열쇠
            data.slots = inventory.SaveSlots(); // 변경된 인벤토리
            if (growth != null) // 경험치 지급 검사
            { // 성장 시작
                data.level = growth.Level; // 성장 후 레벨
                data.experience = growth.CurrentExperience; // 성장 후 경험치
                data.statPoints += growth.GainedStatPoints; // 추가 스탯 포인트
            } // 성장 끝
            if (!RunRestoreRules.IsValid(data) || !runStorage.TrySave(data, out message)) // 전체 저장 검증과 실제 기록
            { // 저장 실패 시작
                if (string.IsNullOrEmpty(message)) // 상세 오류 누락 검사
                { // 안내 시작
                    message = "방 상태를 저장할 수 없습니다."; // 기본 저장 오류
                } // 안내 끝
                return false; // 저장 실패 시 실제 상태 보존
            } // 저장 실패 끝
            travel.ApplyRoomContent(next); // 저장된 방 기록 적용
            PlayerHealth = health; // 저장된 체력 적용
            Keys = new DoorKeyRing(); // 저장된 열쇠 묶음 생성
            if (keys > 0) // 보유 열쇠 검사
            { // 열쇠 복원 시작
                Keys.Add(keys); // 저장된 열쇠 수량 복원
            } // 열쇠 복원 끝
            Inventory = inventory; // 저장된 소지품 적용
            Level = data.level; // 저장된 레벨 적용
            Experience = data.experience; // 저장된 경험치 적용
            StatPoints = data.statPoints; // 저장된 스탯 포인트 적용
            savedRun = data; // 마지막 정상 저장 갱신
            savedRunValid = true; // 저장 회차 재개 허용
            message = success + " · 저장 완료"; // 실제 저장 결과 안내
            return true; // 안전한 확정 완료
        } // 저장 끝
        public bool TryClaimChest(int[] selectedIndices, int replaceIndex, out string message) // 선택한 상자 보상의 단일 확정
        { // 확정 시작
            message = string.Empty; // 기본 안내 초기화
            var room = GameObject.Find("RoomTravel"); // 현재 던전 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 방 제어
            var saved = CurrentChest; // 저장된 상자 상태
            if (Flow.Phase != GamePhase.Inventory || chestScreen == null || travel == null || selectedIndices == null || !ChestState.TryRestore(saved, out var chest) || saved.completed || saved.mimicState == 0 || saved.mimicState == 2 && !saved.mimicDefeated || replaceIndex >= 0 && selectedIndices.Length != 1) // 화면과 후보의 지급 가능 여부
            { // 오류 시작
                message = "상자 보상을 확정할 수 없습니다."; // 보상 오류 안내
                return false; // 잘못된 지급 차단
            } // 오류 끝
            var inventory = InventoryState.FromSavedSlots(Inventory.SaveSlots()); // 실제 소지품의 독립 복사
            var seen = new System.Collections.Generic.HashSet<int>(); // 중복 후보 검사
            if (inventory == null || saved.rewards == null) // 유효한 인벤토리와 후보 검사
            { // 오류 시작
                message = "상자 보상을 확인할 수 없습니다."; // 데이터 오류 안내
                return false; // 지급 차단
            } // 오류 끝
            foreach (var index in selectedIndices) // 선택된 후보 순회
            { // 후보 처리 시작
                if (index < 0 || index >= saved.rewards.Length || !seen.Add(index)) // 후보 범위와 중복 검사
                { // 오류 시작
                    message = "선택한 보상을 확인할 수 없습니다."; // 후보 오류 안내
                    return false; // 중복 지급 차단
                } // 오류 끝
                var reward = saved.rewards[index]; // 고정된 개별 후보
                var item = TrialConsumableRules.Find(reward.itemId); // 지원하는 소비품 정의
                var added = reward.equipment != null ? replaceIndex >= 0 ? inventory.TryReplaceWithEquipment(replaceIndex, reward.equipment) : inventory.TryAddEquipment(reward.equipment) : item != null && (replaceIndex >= 0 ? inventory.TryReplace(replaceIndex, item.Id, item.Name, reward.quantity, item.StackLimit) : inventory.Add(item.Id, item.Name, reward.quantity, item.StackLimit).Remaining == 0); // 선택 후보의 전체 수용
                if (!added) // 공간과 아이템 검사
                { // 오류 시작
                    message = "가방 공간 또는 보상을 확인해 주세요."; // 원본 보존 안내
                    return false; // 부분 지급 차단
                } // 오류 끝
            } // 후보 처리 끝
            if (!chest.TryComplete(out var completed) || !travel.CurrentContent.TrySetChest(travel.RoomX, travel.RoomY, completed, out var next) || !ExperienceRules.TryAdvance(Level, Experience, saved.experience, out var growth)) // 상자 완료와 경험치 검증
            { // 오류 시작
                message = "상자 경험치를 계산할 수 없습니다."; // 성장 오류 안내
                return false; // 상자와 소지품 보존
            } // 오류 끝
            if (!TryCommitRoomContent(travel, next, PlayerHealth, Keys.Count, inventory, growth, "상자 보상 확정", out message)) // 지급과 완료의 단일 저장
            { // 오류 시작
                return false; // 저장 전 실제 지급 차단
            } // 오류 끝
            CloseChest(); // 확정된 화면 종료
            return true; // 실제 보상 완료
        } // 확정 끝
        public bool TryPersistMimicHealth(FloorRoomContent next, out string message) // 도망 전 미믹 체력 저장
        { // 저장 시작
            var room = GameObject.Find("RoomTravel"); // 현재 던전 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 방 제어
            return TryCommitRoomContent(travel, next, PlayerHealth, Keys.Count, Inventory, null, "미믹 체력 보존", out message); // 도망 전 전투 상태 저장
        } // 저장 끝
        public bool TryFinishMimic(out string message) // 미믹 승리와 상자 복구
        { // 승리 시작
            message = string.Empty; // 기본 안내 초기화
            var room = GameObject.Find("RoomTravel"); // 현재 던전 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 방 제어
            if (travel == null || !travel.IsCurrentMimic || !ChestState.TryRestore(CurrentChest, out var chest) || !ExperienceRules.TryCalculateVictory(travel.Floor, 1, 0, out var experience) || !chest.TryWinMimic(experience, out var won) || !travel.CurrentContent.TrySetChest(travel.RoomX, travel.RoomY, won, out var next)) // 미믹과 승리 경험치 검사
            { // 오류 시작
                message = "미믹 승리를 확인할 수 없습니다."; // 승리 오류 안내
                return false; // 잘못된 승리 차단
            } // 오류 끝
            if (!TryCommitRoomContent(travel, next, PlayerHealth, Keys.Count, Inventory, null, "미믹 승리", out message)) // 승리와 후보의 사전 저장
            { // 오류 시작
                return false; // 저장 실패 시 승리 화면 유지
            } // 오류 끝
            EndCombat(); // 전투 화면 종료
            if (!OpenChest()) // 저장된 상자 보상 화면 검사
            { // 화면 오류 시작
                message = "상자를 다시 확인해 보상을 받으세요."; // 저장된 보상의 재시도 안내
            } // 화면 오류 끝
            return true; // 저장된 승리 처리 완료
        } // 승리 끝
        public bool TryApplyTrap(RoomTravelController travel, out string message) // 최초 진입 함정의 안전한 확정
        { // 판정 시작
            message = string.Empty; // 기본 안내 초기화
            var content = travel == null || travel.CurrentContent == null ? null : travel.CurrentContent.Snapshot(); // 현재 층의 독립 기록
            if (Flow.Phase != GamePhase.Exploration || content == null || content.trapCompleted || content.trapX != travel.RoomX || content.trapY != travel.RoomY || !TrialTrapState.Untriggered.TryTrigger(40, ChestRoll(0, 100), ChestRoll(10, 19), out _, out var result)) // 함정 좌표와 시험 판정 검사
            { // 오류 시작
                message = "함정을 확인할 수 없습니다."; // 함정 오류 안내
                return false; // 잘못된 피해 차단
            } // 오류 끝
            if (PlayerHealth <= result.Damage) // 치명 피해 검사
            { // 종료 시작
                if (runStorage == null || !runStorage.TryMarkEnded(CurrentRunId, out message)) // 실제 회차 종료 저장
                { // 오류 시작
                    return false; // 저장 실패 시 회차 보존
                } // 오류 끝
                savedRun = runStorage.Load(out var warning); // 다른 회차의 저장 보존
                savedRunValid = savedRun != null && RunRestoreRules.IsValid(savedRun); // 남은 회차 재개 검사
                RunSaveMessage = warning; // 실제 저장 안내
                message = "함정으로 회차 종료"; // 실제 결과 안내
                ReturnToTitle(); // 종료 후 타이틀 복귀
                return true; // 회차 종료 완료
            } // 종료 끝
            if (!travel.CurrentContent.TryCompleteTrap(out var next)) // 미완료 함정의 완료 검사
            { // 오류 시작
                message = "함정을 저장할 수 없습니다."; // 함정 오류 안내
                return false; // 피해 미적용
            } // 오류 끝
            return TryCommitRoomContent(travel, next, PlayerHealth - result.Damage, Keys.Count, Inventory, null, result.Avoided ? "함정 회피" : "함정 피해 " + result.Damage, out message); // 피해와 함정 완료의 단일 저장
        } // 판정 끝
        public ShopStockData[] ShopStocksSnapshot() // 전체 방문 상점의 독립 저장본
        { // 저장본 생성 시작
            return CopyShopStocks(shopStocks); // 원본을 보호하는 전체 상점 복사
        } // 저장본 생성 끝
        private static ShopStockData[] CopyShopStocks(ShopStockData[] source) // 검증된 층별 상점의 심층 복사
        { // 복사 시작
            if (source == null) // 누락된 상점 목록 검사
            { // 누락 오류 시작
                return null; // 복원 불가능한 목록 차단
            } // 누락 오류 끝
            var result = new ShopStockData[source.Length]; // 독립 상점 목록 생성
            for (var index = 0; index < source.Length; index++) // 저장된 상점 순회
            { // 상점 복사 시작
                if (!ShopState.TryRestore(source[index], out var shop)) // 가격과 재고 복원 검사
                { // 복원 오류 시작
                    return null; // 손상된 상점 목록 차단
                } // 복원 오류 끝
                result[index] = shop.Snapshot(); // 검증된 상품과 재고 복사
            } // 상점 복사 끝
            return result; // 안전한 상점 목록 전달
        } // 복사 끝
        public bool TryEquipInventoryItem(int index, out string message) // 보유 장비의 원자적 장착
        { // 장착 시작
            message = "장착할 장비를 선택하세요."; // 기본 선택 안내
            if (Flow.Phase != GamePhase.Inventory || index < 0 || index >= Inventory.Slots.Count) // 실제 화면과 선택 범위 검사
            { // 선택 오류 시작
                return false; // 잘못된 장착 차단
            } // 선택 오류 끝
            var item = Inventory.Slots[index]; // 선택한 보유 묶음
            var definition = EquipmentCatalog.Find(item.ItemId); // 확인된 장비 정의
            if (definition == null || item.Quantity != 1 || item.StackLimit != 1 || !Equipment.TryEquip(definition.Slot, definition.Id, item.Equipment, out var next)) // 실제 단일 장비와 부위 검사
            { // 장비 오류 시작
                return false; // 등록되지 않은 장비 차단
            } // 장비 오류 끝
            var inventory = InventoryState.FromSavedSlots(Inventory.SaveSlots()); // 실패 시 원본을 보존하는 독립 인벤토리
            var previous = Equipment.Get(definition.Slot); // 기존 장착 장비
            var previousVariant = Equipment.GetVariant(definition.Slot); // 기존 장비의 개별 특성
            if (inventory == null || !inventory.Remove(index, 1, out _) || (previous != null && !(previousVariant == null ? inventory.Add(previous.Id, previous.Name, 1, 1).Remaining == 0 : inventory.TryAddEquipment(previousVariant))) || !next.TryCalculate(50, 40, 10, 0, 150, 10, out var stats) || !inventory.TryResize(stats.InventoryCapacity)) // 교체품 보관과 새 가방 용량 검사
            { // 공간 부족 시작
                message = "장비를 보관할 공간이 부족합니다."; // 변경 없이 공간 부족 안내
                return false; // 기존 장비와 아이템 보존
            } // 공간 부족 끝
            return TryPersistEquipment(inventory, next, definition.Name + " 장착", out message); // 저장 성공 후에만 장착 반영
        } // 장착 끝
        public bool TryPurifyInventoryItem(int index, out string message) // 보유한 시험 저주 장비 정화
        { // 정화 시작
            message = "정화할 시험 저주 장비를 선택하세요."; // 기본 선택 안내
            if (Flow.Phase != GamePhase.Inventory || index < 0 || index >= Inventory.Slots.Count) // 실제 화면과 슬롯 범위 검사
            { // 선택 오류 시작
                return false; // 잘못된 정화 차단
            } // 선택 오류 끝
            var item = Inventory.Slots[index].Equipment; // 선택한 개별 장비의 독립 복사
            if (!TrialEquipmentTraits.IsValid(item) || !EquipmentVariantRules.TryGetPurification(item, TrialEquipmentTraits.Policies(), out var plan) || plan.kind != EquipmentVariantRules.Purify || plan.resultId != item.definitionId || plan.resultQuantity != 1 || plan.resultEffects != null && plan.resultEffects.Length > 0) // 명시된 시험 정화 결과 검사
            { // 정화 불가 시작
                return false; // 미정 결과와 다른 정화 방법 차단
            } // 정화 불가 끝
            item.cursed = false; // 시험 저주 제거
            item.curseEffects = new EquipmentOptionData[0]; // 공개된 저주 효과 제거
            item.purification = null; // 사용한 시험 정화 계획 제거
            var inventory = InventoryState.FromSavedSlots(Inventory.SaveSlots()); // 원본을 보존하는 독립 인벤토리
            if (!TrialEquipmentTraits.IsValid(item) || inventory == null || !inventory.TryReplaceEquipment(index, item)) // 정화 결과와 동일 인스턴스 검사
            { // 정화 오류 시작
                message = "시험 정화 결과를 확인할 수 없습니다."; // 안전한 실패 안내
                return false; // 기존 저주 장비 보존
            } // 정화 오류 끝
            return TryPersistEquipment(inventory, Equipment, "시험 저주 정화", out message); // 저장 성공 후 정화 결과 적용
        } // 정화 끝
        public bool TryUnequipItem(EquipmentSlot slot, out string message) // 장착 장비의 원자적 해제
        { // 해제 시작
            message = "해제할 장비가 없습니다."; // 기본 해제 안내
            var previous = Equipment.Get(slot); // 기존 장착 장비
            var previousVariant = Equipment.GetVariant(slot); // 기존 장비의 개별 특성
            if (Flow.Phase != GamePhase.Inventory || previous == null || !Equipment.TryUnequip(slot, out var next)) // 실제 화면과 장착 상태 검사
            { // 해제 오류 시작
                return false; // 잘못된 해제 차단
            } // 해제 오류 끝
            var inventory = InventoryState.FromSavedSlots(Inventory.SaveSlots()); // 실패 시 원본을 보존하는 독립 인벤토리
            if (inventory == null || !(previousVariant == null ? inventory.Add(previous.Id, previous.Name, 1, 1).Remaining == 0 : inventory.TryAddEquipment(previousVariant)) || !next.TryCalculate(50, 40, 10, 0, 150, 10, out var stats) || !inventory.TryResize(stats.InventoryCapacity)) // 장비 보관과 축소 용량 검사
            { // 공간 부족 시작
                message = "장비를 보관할 공간이 부족합니다."; // 변경 없이 공간 부족 안내
                return false; // 기존 장비와 아이템 보존
            } // 공간 부족 끝
            return TryPersistEquipment(inventory, next, previous.Name + " 해제", out message); // 저장 성공 후에만 해제 반영
        } // 해제 끝
        private bool TryPersistEquipment(InventoryState inventory, EquipmentState equipment, string success, out string message) // 장비와 소지품의 단일 저장
        { // 저장 시작
            message = string.Empty; // 기본 안내 초기화
            var savedId = savedRun == null ? null : savedRun.version < 7 ? runStorage?.LegacyRunId : savedRun.runId; // 다른 회차 저장 식별자
            if (runStorage == null || (savedId != null && savedId != CurrentRunId)) // 저장 기능과 다른 회차 보호 검사
            { // 저장 거절 시작
                message = "다른 회차 저장을 확인한 뒤 장비를 변경하세요."; // 기존 회차 보호 안내
                return false; // 다른 회차 덮어쓰기 차단
            } // 저장 거절 끝
            var room = GameObject.Find("RoomTravel"); // 실제 방 이동 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 안정된 던전 상태
            var data = travel == null ? null : travel.CaptureInventoryRun(); // 현재 인벤토리의 독립 저장본
            if (data == null) // 저장 가능한 위치 검사
            { // 위치 오류 시작
                message = "현재 장비를 저장할 수 없습니다."; // 저장 불가 안내
                return false; // 이동 중 장착 차단
            } // 위치 오류 끝
            data.slots = inventory.SaveSlots(); // 변경할 독립 인벤토리 저장
            data.weaponId = equipment.Get(EquipmentSlot.Weapon)?.Id; // 변경할 무기 식별자
            data.armorId = equipment.Get(EquipmentSlot.Armor)?.Id; // 변경할 방어구 식별자
            data.accessoryId = equipment.Get(EquipmentSlot.Accessory)?.Id; // 변경할 장신구 식별자
            data.bagId = equipment.Get(EquipmentSlot.Bag)?.Id; // 변경할 가방 식별자
            data.weaponVariant = equipment.GetVariant(EquipmentSlot.Weapon); // 변경할 무기의 개별 특성
            data.armorVariant = equipment.GetVariant(EquipmentSlot.Armor); // 변경할 방어구의 개별 특성
            data.accessoryVariant = equipment.GetVariant(EquipmentSlot.Accessory); // 변경할 장신구의 개별 특성
            data.bagVariant = equipment.GetVariant(EquipmentSlot.Bag); // 변경할 가방의 개별 특성
            if (!RunRestoreRules.IsValid(data) || !runStorage.TrySave(data, out message)) // 복원 가능성과 안전한 저장 검사
            { // 저장 실패 시작
                if (string.IsNullOrEmpty(message)) // 세부 오류 누락 검사
                { // 기본 오류 시작
                    message = "장비 변경을 저장할 수 없습니다."; // 사용자 저장 오류 안내
                } // 기본 오류 끝
                return false; // 저장 실패 시 기존 장비와 아이템 보존
            } // 저장 실패 끝
            Inventory = inventory; // 검증된 인벤토리 적용
            Equipment = equipment; // 저장된 장비 적용
            savedRun = data; // 마지막 정상 저장 반영
            savedRunValid = true; // 저장 회차 재개 허용
            message = success + " · 저장 완료"; // 실제 장착과 저장 안내
            return true; // 안전한 장비 변경 완료
        } // 저장 끝
        public void UpdatePlayerMana(int mana) // 전투 후 현재 마나 반영
        { // 갱신 시작
            PlayerMana = Mathf.Clamp(mana, 0, 50); // 기획 기본 마나 범위 유지
        } // 갱신 끝
        public void UpdatePlayerHealth(int health) // 전투 후 탐험 체력 반영
        { // 반영 시작
            PlayerHealth = Mathf.Clamp(health, 0, 100); // 원본 기본 최대 체력 범위 유지
        } // 반영 끝
        public bool StartEncounter() // 탐험 중 조우 선택 화면 진입
        { // 진입 시작
            if (!Flow.TryStartEncounter()) // 조우 진입 상태 검사
            { // 진입 거절 조건 시작
                return false; // 중복 조우 차단
            } // 진입 거절 조건 끝
            try // 실제 조우 화면 준비
            { // 화면 준비 시작
                if (AutomaticEncounterTestOverride ?? true) // 실제 자동 전투 진입 검사
                { // 자동 전환 시작
                    Input.SetPhase(GamePhase.Loading); // 연출 중 이동과 화면 입력 잠금
                    var room = GameObject.Find("RoomTravel"); // 현재 조우 방 조회
                    var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 실제 조우 대상
                    var monsterId = travel == null ? string.Empty : travel.CurrentMonsterId; // 실제 상대 식별자
                    var name = travel == null ? "상위 개체" : travel.CurrentMonsterName; // 실제 상대 이름
                    var portrait = monsterId == "MON_DEMON_LORD" ? "BattlePortraits/DemonLordPrototype" : monsterId == "MON_SLIME" ? "BattlePortraits/SlimePrototype" : monsterId == "MON_GOBLIN" ? "BattlePortraits/GoblinPrototype" : monsterId == "MON_MIMIC" ? "BattlePortraits/MimicPrototype" : "BattlePortraits/SlimeQueenPrototype"; // 실제 상대의 출현 초상화
                    encounterTransition = EncounterTransitionView.Create(name, portrait); // 출현과 화면 전환 생성
                    StartCoroutine(TransitionIntoCombat(encounterTransition)); // 조우 연출 후 자동 전투 시작
                    return true; // 자동 전환 요청 성공
                } // 자동 전환 끝
                Input.SetPhase(GamePhase.Encounter); // 시험용 기존 조우 선택 입력 활성화
                encounterScreen = EncounterScreenView.Create(this); // 시험 조우 선택 화면 생성
                return true; // 실제 화면 진입 성공
            } // 화면 준비 끝
            catch (System.Exception exception) // 조우 화면 생성 실패 복구
            { // 실패 복구 시작
                Flow.TryCancelEncounter(); // 기존 탐험 상태 복구
                Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
                Debug.LogError("Encounter screen failed: " + exception.Message); // 실제 오류 기록
                return false; // 잘못된 조우 진입 거절
            } // 실패 복구 끝
        } // 진입 끝
        private IEnumerator TransitionIntoCombat(EncounterTransitionView transition) // 조우 연출과 자동 전투 진입
        { // 전환 시작
            try // 전환 화면과 입력 정리 보장
            { // 연출 시작
                yield return transition.Cover(); // 상대 출현과 화면 가림 대기
                if (transition == null || !transition.IsCovered || Flow.Phase != GamePhase.Encounter) // 실제 조우와 전환 상태 검사
                { // 중단 시작
                    yield break; // 중단된 조우의 전투 생성 차단
                } // 중단 끝
                if (!StartCombat()) // 완전히 가려진 상태의 전투 생성 검사
                { // 생성 실패 시작
                    Flow.TryCancelEncounter(); // 실패한 조우의 탐험 복구
                    Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
                    yield break; // 미완성 전투 표시 차단
                } // 생성 실패 끝
                Input.SetPhase(GamePhase.Loading); // 전투 화면이 드러날 때까지 입력 잠금
                yield return transition.Reveal(); // 전투 화면의 부드러운 노출
                if (Flow.Phase == GamePhase.Combat) // 실제 전투 상태 검사
                { // 입력 복구 시작
                    Input.SetPhase(GamePhase.Combat); // 연출 완료 후 전투 입력 활성화
                } // 입력 복구 끝
            } // 연출 끝
            finally // 중단과 실패를 포함한 화면 정리
            { // 정리 시작
                if (transition != null) // 전환 화면 존재 검사
                { // 화면 정리 시작
                    transition.Close(); // 입력 차단 즉시 해제와 화면 제거
                } // 화면 정리 끝
                if (encounterTransition == transition) // 현재 연출 참조 검사
                { // 참조 정리 시작
                    encounterTransition = null; // 완료된 연출 참조 해제
                } // 참조 정리 끝
            } // 정리 끝
        } // 전환 끝
        public bool EndEncounterAfterEscape() // 성공한 조우 도망 후 탐험 복귀
        { // 복귀 시작
            if (!Flow.TryCancelEncounter()) // 현재 조우 상태 검사
            { // 복귀 거절 조건 시작
                return false; // 잘못된 조우 종료 차단
            } // 복귀 거절 조건 끝
            if (encounterScreen != null) // 조우 화면 존재 검사
            { // 화면 정리 시작
                encounterScreen.gameObject.SetActive(false); // 조우 화면 즉시 숨김
                Destroy(encounterScreen.gameObject); // 종료한 조우 화면 제거
                encounterScreen = null; // 조우 화면 참조 해제
            } // 화면 정리 끝
            Input.SetPhase(GamePhase.Exploration); // 기존 탐험 입력 복구
            return true; // 조우 도망 복귀 성공
        } // 복귀 끝
        public bool StartCombat() // 조우 선택 또는 시험 전투 화면 진입
        { // 진입 시작
            var fromEncounter = Flow.Phase == GamePhase.Encounter; // 조우 화면에서의 전투 선택 검사
            if (!Flow.TryStartCombat()) // 전투 진입 상태 검사
            { // 진입 거절 조건 시작
                return false; // 중복 조우 차단
            } // 진입 거절 조건 끝
            Input.SetPhase(GamePhase.Combat); // 탐험 입력 잠금과 화면 입력 활성화
            try // 실제 전투 화면 준비
            { // 화면 준비 시작
                combatScreen = CombatScreenView.Create(this); // 시험 전투 화면 생성
                if (fromEncounter && encounterScreen != null) // 전투 선택 후 기존 조우 화면 검사
                { // 조우 화면 정리 시작
                    encounterScreen.gameObject.SetActive(false); // 조우 화면 즉시 숨김
                    Destroy(encounterScreen.gameObject); // 전환 완료된 조우 화면 제거
                    encounterScreen = null; // 기존 조우 화면 참조 해제
                } // 조우 화면 정리 끝
                return true; // 실제 화면 진입 성공
            } // 화면 준비 끝
            catch (System.Exception exception) // 화면 생성 실패 복구
            { // 실패 복구 시작
                if (fromEncounter) // 조우 화면에서 시작한 전투 검사
                { // 조우 복구 시작
                    Flow.TryRevertCombatToEncounter(); // 기존 조우 상태 복구
                } // 조우 복구 끝
                else // 시험 전투의 탐험 복구
                { // 탐험 복구 시작
                    Flow.TryEndCombat(); // 기존 탐험 상태 복구
                } // 탐험 복구 끝
                Input.SetPhase(Flow.Phase); // 이전 화면의 입력 복구
                if (fromEncounter && encounterScreen != null) // 복구할 조우 화면 검사
                { // 조우 선택 복구 시작
                    encounterScreen.SelectDefault(); // 전투 선택 버튼 복구
                } // 조우 선택 복구 끝
                Debug.LogError("Combat screen failed: " + exception.Message); // 실제 오류 기록
                return false; // 잘못된 전투 진입 거절
            } // 실패 복구 끝
        } // 진입 끝
        public void EndCombat() // 시험 전투 종료와 탐험 복귀
        { // 복귀 시작
            if (!Flow.TryEndCombat()) // 전투 상태 검사
            { // 복귀 거절 조건 시작
                return; // 중복 종료 차단
            } // 복귀 거절 조건 끝
            if (combatScreen != null) // 전투 화면 존재 검사
            { // 화면 정리 시작
                combatScreen.gameObject.SetActive(false); // 기존 전투 화면 즉시 숨김
                Destroy(combatScreen.gameObject); // 시험 전투 화면 정리
                combatScreen = null; // 화면 참조 해제
            } // 화면 정리 끝
            if (rewardScreen != null) // 보상 화면 존재 검사
            { // 보상 화면 정리 시작
                rewardScreen.gameObject.SetActive(false); // 보상 화면 즉시 숨김
                Destroy(rewardScreen.gameObject); // 완료된 보상 화면 정리
                rewardScreen = null; // 보상 화면 참조 해제
            } // 보상 화면 정리 끝
            Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
        } // 복귀 끝
        private void RequestTransition(GamePhase destination, string sceneName) // 전환 승인과 입력 잠금
        { // 요청 시작
            if (!Flow.TryBeginTransition(destination)) // 중복 요청 검사
            { // 거절 조건 시작
                return; // 승인되지 않은 요청 종료
            } // 거절 조건 끝
            Input.SetPhase(GamePhase.Loading); // 이전 입력 즉시 잠금
            StartCoroutine(Transition(sceneName)); // 비동기 전환 시작
        } // 요청 끝
        private IEnumerator Transition(string sceneName) // 실제 전환 순서
        { // 전환 시작
            yield return sceneLoader.LoadScene(sceneName); // 씬 준비 완료 대기
            if (sceneLoader.Succeeded) // 실제 로드 성공 검사
            { // 성공 조건 시작
                Flow.CompleteTransition(); // 도착 상태 확정
                ClearLoadRollback(); // 불러오기 복구 대상 해제
            } // 성공 조건 끝
            else // 실패 복구 분기
            { // 복구 분기 시작
                Flow.CancelTransition(); // 이전 상태 복구
                if (loadRollbackInventory != null) // 메뉴 불러오기 실패 검사
                { // 기존 회차 복구 시작
                    Inventory = loadRollbackInventory; // 이전 인벤토리 복원
                    Equipment = loadRollbackEquipment; // 이전 장비 복원
                    Relics = loadRollbackRelics; // 이전 유물 복원
                    shopStocks = loadRollbackShops; // 이전 상점 목록 복원
                    Shop = loadRollbackShop; // 이전 상점 복원
                    Keys = loadRollbackKeys; // 이전 열쇠 복원
                    ExplorationHistory = loadRollbackHistory; // 이전 회차 기록 복원
                    PlayerHealth = loadRollbackHealth; // 이전 체력 복원
                    PlayerMana = loadRollbackMana; // 이전 마나 복원
                    CurrentRunId = loadRollbackRunId; // 이전 회차 식별자 복원
                    Level = loadRollbackLevel; // 이전 레벨 복원
                    Experience = loadRollbackExperience; // 이전 경험치 복원
                    StatPoints = loadRollbackStatPoints; // 이전 스탯 포인트 복원
                    Gold = loadRollbackGold; // 이전 골드 복원
                    RewardReceipts = loadRollbackRewards; // 이전 보상 거래 복원
                    PendingRun = null; // 실패한 회차 복원 요청 해제
                    ClearLoadRollback(); // 복구 대상 해제
                } // 기존 회차 복구 끝
            } // 복구 분기 끝
            Input.SetPhase(Flow.Phase); // 현재 화면의 입력 복구
        } // 전환 끝
        private void ClearLoadRollback() // 불러오기 전 상태 보관 해제
        { // 해제 시작
            loadRollbackInventory = null; // 이전 인벤토리 참조 해제
            loadRollbackEquipment = null; // 이전 장비 참조 해제
            loadRollbackRelics = null; // 이전 유물 참조 해제
            loadRollbackShops = null; // 이전 상점 목록 참조 해제
            loadRollbackShop = null; // 이전 상점 참조 해제
            loadRollbackKeys = null; // 이전 열쇠 참조 해제
            loadRollbackHistory = null; // 이전 기록 참조 해제
            loadRollbackRunId = null; // 이전 회차 식별자 참조 해제
            loadRollbackRewards = null; // 이전 보상 거래 참조 해제
        } // 해제 끝
        private void OnDestroy() // 공통 객체 해제
        { // 해제 시작
            if (Instance != this) // 중복 객체 해제 검사
            { // 무시 조건 시작
                return; // 실제 공통 객체 보존
            } // 무시 조건 끝
            if (encounterTransition != null) // 미완료 조우 연출 검사
            { // 연출 정리 시작
                encounterTransition.Close(); // 전환 화면과 입력 차단 해제
                encounterTransition = null; // 연출 참조 해제
            } // 연출 정리 끝
            if (Input != null) // 입력 객체 존재 검사
            { // 해제 조건 시작
                Input.BackRequested -= OpenPause; // 일시정지 입력 연결 해제
                Input.InventoryRequested -= OpenInventory; // 인벤토리 열기 연결 해제
                Input.FullMapRequested -= OpenFullMap; // 전체 지도 열기 연결 해제
                Input.FullMapCloseRequested -= CloseFullMap; // 전체 지도 닫기 연결 해제
                Input.HistoryRequested -= OpenHistory; // 회차 기록 열기 연결 해제
                Input.HistoryCloseRequested -= CloseHistory; // 회차 기록 닫기 연결 해제
                Input.HistoryPageRequested -= MoveHistoryPage; // 회차 기록 페이지 연결 해제
            } // 해제 조건 끝
            Instance = null; // 공통 객체 등록 해제
        } // 해제 끝
    } // 클래스 끝
} // 이름 공간 끝
