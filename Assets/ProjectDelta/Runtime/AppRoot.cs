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
        public const int TrialPotionStackLimit = 5; // 기획 기준 시험 최대 중첩
        private SceneLoader sceneLoader; // 실제 씬 로더
        private ObservationStorage observationStorage; // 2D 전용 관찰 기록 저장
        private RunStorage runStorage; // 현재 회차 저장 기능
        private RunSaveData savedRun; // 마지막으로 확인한 저장 회차
        private bool savedRunValid; // 마지막 회차의 복원 가능 여부
        public RunSaveData PendingRun { get; private set; } // 씬에 전달할 복원 대기 회차
        public string RunSaveMessage { get; private set; } = string.Empty; // 회차 저장 안내
        public bool CanContinueRun => savedRunValid; // 마지막 검증의 복원 가능 여부
        private CombatScreenView combatScreen; // 시험 전투 화면
        private EncounterScreenView encounterScreen; // 현재 조우 선택 화면
        private InventoryScreenView inventoryScreen; // 현재 인벤토리 화면
        private FullMapScreenView fullMapScreen; // 현재 전체 지도 화면
        private HistoryScreenView historyScreen; // 현재 회차 기록 화면
        private PauseScreenView pauseScreen; // 탐험 일시정지 메뉴
        private InventoryState loadRollbackInventory; // 불러오기 실패 시 이전 인벤토리
        private DoorKeyRing loadRollbackKeys; // 불러오기 실패 시 이전 열쇠
        private ExplorationHistory loadRollbackHistory; // 불러오기 실패 시 이전 기록
        private int loadRollbackHealth; // 불러오기 실패 시 이전 체력
        private int loadRollbackMana; // 불러오기 실패 시 이전 마나
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
                savedRun = runStorage.Load(out var runWarning); // 현재 회차 데이터 읽기
                savedRunValid = savedRun != null && savedRun.encounterHealth <= CombatScreenView.TrialEnemyHealth; // 실제 적 체력 범위 검사
                RunSaveMessage = runWarning; // 저장 오류 안내 보관
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
                PlayerHealth = 100; // 원본 기본 최대 체력 초기화
                PlayerMana = 50; // 새 회차 기본 마나 초기화
                ExplorationHistory = new ExplorationHistory(); // 새 회차의 탐험 기록 초기화
                Inventory = new InventoryState(); // 새 게임의 빈 인벤토리 초기화
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
                loadRollbackKeys = Keys; // 기존 열쇠 보관
                loadRollbackHistory = ExplorationHistory; // 기존 기록 보관
                loadRollbackHealth = PlayerHealth; // 기존 체력 보관
                loadRollbackMana = PlayerMana; // 기존 마나 보관
            } // 실패 복구 준비 끝
            Inventory = restoredInventory; // 복원한 인벤토리 적용
            Keys = restoredKeys; // 복원한 열쇠 적용
            ExplorationHistory = restoredHistory; // 복원한 회차 기록 적용
            PlayerHealth = savedRun.health; // 저장된 체력 적용
            PlayerMana = savedRun.version < 5 ? 50 : savedRun.mana; // 이전 저장의 기본 마나와 새 저장 복원
            PendingRun = savedRun; // 던전 씬에 복원 상태 전달
            RequestTransition(GamePhase.Exploration, "DungeonScene"); // 탐험 재개 요청
            return true; // 재개 요청 승인
        } // 재개 끝
        public void FinishRunRestore() // 던전 복원 요청 소모
        { // 소모 시작
            PendingRun = null; // 재복원 방지
        } // 소모 끝
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
            Input.SetPhase(GamePhase.Encounter); // 탐험 입력 잠금과 조우 입력 활성화
            try // 실제 조우 화면 준비
            { // 화면 준비 시작
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
                    Keys = loadRollbackKeys; // 이전 열쇠 복원
                    ExplorationHistory = loadRollbackHistory; // 이전 회차 기록 복원
                    PlayerHealth = loadRollbackHealth; // 이전 체력 복원
                    PlayerMana = loadRollbackMana; // 이전 마나 복원
                    PendingRun = null; // 실패한 회차 복원 요청 해제
                    ClearLoadRollback(); // 복구 대상 해제
                } // 기존 회차 복구 끝
            } // 복구 분기 끝
            Input.SetPhase(Flow.Phase); // 현재 화면의 입력 복구
        } // 전환 끝
        private void ClearLoadRollback() // 불러오기 전 상태 보관 해제
        { // 해제 시작
            loadRollbackInventory = null; // 이전 인벤토리 참조 해제
            loadRollbackKeys = null; // 이전 열쇠 참조 해제
            loadRollbackHistory = null; // 이전 기록 참조 해제
        } // 해제 끝
        private void OnDestroy() // 공통 객체 해제
        { // 해제 시작
            if (Instance != this) // 중복 객체 해제 검사
            { // 무시 조건 시작
                return; // 실제 공통 객체 보존
            } // 무시 조건 끝
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
