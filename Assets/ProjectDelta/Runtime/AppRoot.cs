using System.Collections; // 순차 화면 전환
using ProjectDelta.Core; // 진행 상태 형식
using UnityEngine; // Unity 실행 도구
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    [RequireComponent(typeof(GameInputRouter), typeof(SceneLoader))] // 필수 실행 구성
    public sealed class AppRoot : MonoBehaviour // 공통 실행 객체
    { // 클래스 시작
        private SceneLoader sceneLoader; // 실제 씬 로더
        private CombatScreenView combatScreen; // 시험 전투 화면
        private EncounterScreenView encounterScreen; // 현재 조우 선택 화면
        private InventoryScreenView inventoryScreen; // 현재 인벤토리 화면
        public InventoryState Inventory // 현재 게임의 아이템 상태
        { // 속성 시작
            get; // 인벤토리 조회
            private set; // 새 게임 인벤토리 변경
        } = new InventoryState(); // 기본 빈 인벤토리 생성
        public DoorKeyRing Keys // 현재 게임의 열쇠 보관소
        { // 속성 시작
            get; // 열쇠 조회
            private set; // 새 게임 열쇠 변경
        } = new DoorKeyRing(); // 빈 열쇠 보관소 생성
        public event System.Action InventoryClosed; // 인벤토리 복귀 알림
        public int PlayerHealth // 탐험 중 플레이어 현재 체력
        { // 속성 시작
            get; // 현재 체력 조회
            private set; // 내부 체력 변경
        } = 100; // 최초 체력 설정
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
            sceneLoader = GetComponent<SceneLoader>(); // 씬 로더 연결
            Input = GetComponent<GameInputRouter>(); // 입력 제어 연결
            Input.Initialize(); // 전용 입력 준비
            Input.BackRequested += ReturnToTitle; // 탐험 복귀 입력 연결
            Input.InventoryRequested += OpenInventory; // 인벤토리 열기 입력 연결
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
                PlayerHealth = 100; // 원본 기본 최대 체력 초기화
                Inventory = new InventoryState(); // 새 게임의 빈 인벤토리 초기화
                Keys = new DoorKeyRing(); // 새 게임의 열쇠 초기화
            } // 새 게임 조건 끝
            RequestTransition(GamePhase.Exploration, "DungeonScene"); // 던전 진입 요청
        } // 메서드 끝
        public void ReturnToTitle() // 타이틀 복귀 요청
        { // 메서드 시작
            RequestTransition(GamePhase.Title, "TitleScene"); // 타이틀 진입 요청
        } // 메서드 끝
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
            } // 성공 조건 끝
            else // 실패 복구 분기
            { // 복구 분기 시작
                Flow.CancelTransition(); // 이전 상태 복구
            } // 복구 분기 끝
            Input.SetPhase(Flow.Phase); // 현재 화면의 입력 복구
        } // 전환 끝
        private void OnDestroy() // 공통 객체 해제
        { // 해제 시작
            if (Instance != this) // 중복 객체 해제 검사
            { // 무시 조건 시작
                return; // 실제 공통 객체 보존
            } // 무시 조건 끝
            if (Input != null) // 입력 객체 존재 검사
            { // 해제 조건 시작
                Input.BackRequested -= ReturnToTitle; // 복귀 입력 연결 해제
                Input.InventoryRequested -= OpenInventory; // 인벤토리 열기 연결 해제
            } // 해제 조건 끝
            Instance = null; // 공통 객체 등록 해제
        } // 해제 끝
    } // 클래스 끝
} // 이름 공간 끝
