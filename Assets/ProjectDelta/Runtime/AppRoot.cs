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
        private RunStorage runStorage; // 현재 회차 저장 기능
        private DefeatHistoryStorage defeatStorage; // 독립된 영구 패배 기록
        private RunSaveData savedRun; // 마지막으로 확인한 저장 회차
        private bool savedRunValid; // 마지막 회차의 복원 가능 여부
        public RunSaveData PendingRun { get; private set; } // 씬에 전달할 복원 대기 회차
        public string RunSaveMessage { get; private set; } = string.Empty; // 회차 저장 안내
        public bool CanContinueRun => savedRunValid; // 마지막 검증의 복원 가능 여부
        private CombatScreenView combatScreen; // 시험 전투 화면
        private BattleRewardScreenView rewardScreen; // 저장된 승리 보상 화면
        private EncounterScreenView encounterScreen; // 현재 조우 선택 화면
        private EncounterTransitionView encounterTransition; // 자동 전투 진입 전환 화면
        public static bool? AutomaticEncounterTestOverride { get; set; } // 조우 방식의 독립 검증 설정
        private InventoryScreenView inventoryScreen; // 현재 인벤토리 화면
        private ChestScreenView chestScreen; // 현재 상자 확인 화면
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
            sceneLoader = GetComponent<SceneLoader>(); // 씬 로더 연결
            try // 회차 저장 읽기
            { // 저장 읽기 시작
                runStorage = new RunStorage(RunStorage.ResolveDirectory()); // 독립 회차 저장 경로
                defeatStorage = new DefeatHistoryStorage(RunStorage.ResolveDirectory(), id => id == "trial-elite" || MonsterCatalog.Find(id) != null); // 실제 상대만 허용하는 패배 기록
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
            if (PendingRun != null && PendingRun.pendingReward != null) // 미완료 승리 보상 검사
            { // 보상 복원 준비 시작
                StartCoroutine(OpenRestoredReward()); // 씬 전환 후 저장된 후보 화면 복원
            } // 보상 복원 준비 끝
            PendingRun = null; // 재복원 방지
        } // 소모 끝
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
                    var portrait = monsterId == "MON_SLIME" ? "BattlePortraits/SlimePrototype" : monsterId == "MON_GOBLIN" ? "BattlePortraits/GoblinPrototype" : monsterId == "MON_MIMIC" ? "BattlePortraits/MimicPrototype" : "BattlePortraits/SlimeQueenPrototype"; // 실제 상대의 출현 초상화
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
