using System; // 입력 요청 알림
using System.Collections.Generic; // 장치별 방향 보관
using ProjectDelta.Core; // 진행 상태 형식
using UnityEngine; // Unity 실행 도구
using UnityEngine.EventSystems; // 화면 입력 전달
using UnityEngine.InputSystem; // 입력 동작 구성
using UnityEngine.InputSystem.UI; // 화면 입력 모듈
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class GameInputRouter : MonoBehaviour // 상태별 입력 제어
    { // 클래스 시작
        private InputActionAsset actions; // 전용 입력 모음
        private InputActionMap uiMap; // 타이틀 화면 입력
        private InputActionMap explorationMap; // 던전 탐험 입력
        private InputAction moveUp; // 위쪽 버튼 입력
        private InputAction moveDown; // 아래쪽 버튼 입력
        private InputAction moveLeft; // 왼쪽 버튼 입력
        private InputAction moveRight; // 오른쪽 버튼 입력
        private InputAction moveStick; // 게임패드 스틱 입력
        private InputSystemUIInputModule uiModule; // 화면 입력 모듈
        private InputActionReference[] references; // 화면 입력 연결 목록
        private Vector2Int pendingMove; // 현재 입력 갱신의 방향 합계
        private readonly HashSet<int> suppressedSticks = new HashSet<int>(); // 이전 화면에서 기울인 스틱
        private readonly Dictionary<int, Vector2Int> lastStickDirections = new Dictionary<int, Vector2Int>(); // 장치별 마지막 스틱 방향
        public event Action BackRequested; // 타이틀 복귀 요청 알림
        public event Action<Vector2Int> MoveRequested; // 한 칸 이동 요청 알림
        public event Action InteractionRequested; // 정면 상호작용 요청 알림
        public event Action ForceDoorRequested; // 잠긴 문 강제 개방 요청 알림
        public event Action InventoryRequested; // 인벤토리 열기 요청 알림
        public event Action ExplorationLocked; // 탐험 입력 잠금 알림
        public bool IsUiEnabled => uiMap != null && uiMap.enabled; // 실제 화면 입력 상태
        public bool IsExplorationEnabled => explorationMap != null && explorationMap.enabled; // 실제 탐험 입력 상태
        public void Initialize() // 입력 모음 준비
        { // 준비 시작
            actions = ScriptableObject.CreateInstance<InputActionAsset>(); // 실행 전용 입력 생성
            actions.name = "ProjectDeltaInput"; // 입력 모음 이름
            uiMap = new InputActionMap("UI"); // 화면 입력 묶음 생성
            explorationMap = new InputActionMap("Exploration"); // 탐험 입력 묶음 생성
            actions.AddActionMap(uiMap); // 화면 입력 묶음 등록
            actions.AddActionMap(explorationMap); // 탐험 입력 묶음 등록
            var navigate = AddDirectionAction(uiMap, "Navigate", InputActionType.PassThrough); // 화면 방향 입력 생성
            var submit = uiMap.AddAction("Submit", InputActionType.Button); // 화면 선택 입력 생성
            submit.AddBinding("<Keyboard>/enter"); // Enter 선택 연결
            submit.AddBinding("<Keyboard>/space"); // Space 선택 연결
            submit.AddBinding("<Gamepad>/buttonSouth"); // 게임패드 선택 연결
            var cancel = uiMap.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape"); // 화면 취소 입력 생성
            cancel.AddBinding("<Gamepad>/buttonEast"); // 게임패드 취소 연결
            cancel.AddBinding("<Keyboard>/i"); // 인벤토리 단축키 닫기 연결
            cancel.AddBinding("<Gamepad>/buttonNorth"); // 게임패드 인벤토리 닫기 연결
            var point = uiMap.AddAction("Point", InputActionType.PassThrough, "<Mouse>/position"); // 마우스 위치 입력
            var click = uiMap.AddAction("Click", InputActionType.PassThrough, "<Mouse>/leftButton"); // 마우스 선택 입력
            var scroll = uiMap.AddAction("Scroll", InputActionType.PassThrough, "<Mouse>/scroll"); // 마우스 스크롤 입력
            moveUp = AddMoveButton("MoveUp", "<Keyboard>/w", "<Keyboard>/upArrow", "<Gamepad>/dpad/up"); // 위쪽 이동 버튼
            moveDown = AddMoveButton("MoveDown", "<Keyboard>/s", "<Keyboard>/downArrow", "<Gamepad>/dpad/down"); // 아래쪽 이동 버튼
            moveLeft = AddMoveButton("MoveLeft", "<Keyboard>/a", "<Keyboard>/leftArrow", "<Gamepad>/dpad/left"); // 왼쪽 이동 버튼
            moveRight = AddMoveButton("MoveRight", "<Keyboard>/d", "<Keyboard>/rightArrow", "<Gamepad>/dpad/right"); // 오른쪽 이동 버튼
            moveStick = explorationMap.AddAction("MoveStick", InputActionType.Value, "<Gamepad>/leftStick"); // 게임패드 스틱 방향 입력
            moveStick.performed += OnMoveStick; // 스틱 방향 변경 감지
            moveStick.canceled += OnMoveStickCanceled; // 스틱 중립 상태 감지
            InputSystem.onAfterUpdate += OnAfterInputUpdate; // 한 입력 갱신의 방향 확정
            var back = explorationMap.AddAction("Back", InputActionType.Button, "<Keyboard>/escape"); // 던전 복귀 입력 생성
            back.AddBinding("<Gamepad>/buttonEast"); // 게임패드 복귀 연결
            back.performed += OnBack; // 복귀 요청 감지 연결
            var interact = explorationMap.AddAction("Interact", InputActionType.Button, "<Keyboard>/e"); // 탐험 상호작용 입력
            interact.AddBinding("<Gamepad>/buttonSouth"); // 게임패드 상호작용 연결
            interact.performed += OnInteract; // 상호작용 요청 감지
            var forceDoor = explorationMap.AddAction("ForceDoor", InputActionType.Button, "<Keyboard>/f"); // 강제 개방 입력 생성
            forceDoor.AddBinding("<Gamepad>/buttonWest"); // 게임패드 강제 개방 연결
            forceDoor.performed += OnForceDoor; // 강제 개방 요청 감지
            var inventory = explorationMap.AddAction("Inventory", InputActionType.Button, "<Keyboard>/i"); // 인벤토리 열기 입력
            inventory.AddBinding("<Gamepad>/buttonNorth"); // 게임패드 인벤토리 열기 연결
            inventory.performed += OnInventory; // 인벤토리 열기 감지
            var eventObject = new GameObject("EventSystem"); // 단일 화면 입력 객체
            eventObject.SetActive(false); // 연결 전 입력 활성화 방지
            eventObject.transform.SetParent(transform, false); // 공통 객체 아래 배치
            eventObject.AddComponent<EventSystem>(); // 화면 선택 관리자 추가
            uiModule = eventObject.AddComponent<InputSystemUIInputModule>(); // 새 입력 시스템 연결
            uiModule.deselectOnBackgroundClick = false; // 빈 배경 클릭 후 기본 선택 유지
            uiModule.enabled = false; // 준비 중 화면 입력 잠금
            references = new[] // 화면 입력 참조 목록
            { // 참조 목록 시작
                InputActionReference.Create(navigate), // 방향 입력 연결
                InputActionReference.Create(submit), // 선택 입력 연결
                InputActionReference.Create(cancel), // 취소 입력 연결
                InputActionReference.Create(point), // 위치 입력 연결
                InputActionReference.Create(click), // 클릭 입력 연결
                InputActionReference.Create(scroll) // 스크롤 입력 연결
            }; // 참조 목록 끝
            uiModule.actionsAsset = actions; // 전용 입력 모음 지정
            uiModule.move = references[0]; // 화면 방향 전달
            uiModule.submit = references[1]; // 화면 선택 전달
            uiModule.cancel = references[2]; // 화면 취소 전달
            uiModule.point = references[3]; // 마우스 위치 전달
            uiModule.leftClick = references[4]; // 마우스 선택 전달
            uiModule.scrollWheel = references[5]; // 스크롤 전달
            eventObject.SetActive(true); // 준비된 화면 입력 객체 활성화
            SetPhase(GamePhase.Initializing); // 초기 입력 잠금 적용
        } // 준비 끝
        public void SetPhase(GamePhase phase) // 상태별 입력 전환
        { // 전환 시작
            var wasExploring = IsExplorationEnabled; // 이전 탐험 입력 상태 보관
            uiModule.enabled = false; // 기존 화면 입력 종료
            actions.Disable(); // 이전 입력 묶음 초기화
            pendingMove = Vector2Int.zero; // 이전 화면의 입력 대기 초기화
            suppressedSticks.Clear(); // 이전 스틱 차단 상태 초기화
            lastStickDirections.Clear(); // 장치별 스틱 방향 초기화
            if (wasExploring && phase != GamePhase.Exploration) // 탐험 입력 종료 검사
            { // 입력 잠금 조건 시작
                ExplorationLocked?.Invoke(); // 대기 중인 이동 취소 요청
            } // 입력 잠금 조건 끝
            if (phase == GamePhase.Title || phase == GamePhase.Encounter || phase == GamePhase.Combat || phase == GamePhase.Inventory) // 화면 입력 상태 검사
            { // 화면 입력 조건 시작
                uiModule.enabled = true; // 화면 입력 전달 활성화
                uiMap.Enable(); // 타이틀 입력만 활성화
            } // 화면 입력 조건 끝
            else if (phase == GamePhase.Exploration) // 탐험 상태 검사
            { // 탐험 입력 조건 시작
                foreach (var pad in Gamepad.all) // 현재 연결된 게임패드 순회
                { // 게임패드 검사 시작
                    if (ToCardinalDirection(pad.leftStick.ReadValue()) != Vector2Int.zero) // 이전 화면에서 기울인 스틱 검사
                    { // 기울인 스틱 조건 시작
                        suppressedSticks.Add(pad.deviceId); // 해당 스틱만 중립까지 차단
                    } // 기울인 스틱 조건 끝
                } // 게임패드 검사 끝
                explorationMap.Enable(); // 탐험 입력만 활성화
            } // 탐험 입력 조건 끝
        } // 전환 끝
        private static InputAction AddDirectionAction(InputActionMap map, string name, InputActionType type) // 공통 방향 입력 생성
        { // 생성 시작
            var action = map.AddAction(name, type); // 방향 동작 추가
            action.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d"); // WASD 방향 연결
            action.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow"); // 화살표 방향 연결
            action.AddBinding("<Gamepad>/leftStick"); // 게임패드 스틱 연결
            action.AddBinding("<Gamepad>/dpad"); // 게임패드 방향 버튼 연결
            return action; // 구성된 방향 동작 전달
        } // 생성 끝
        private void OnBack(InputAction.CallbackContext context) // 복귀 입력 전달
        { // 전달 시작
            BackRequested?.Invoke(); // 화면 흐름에 복귀 요청
        } // 전달 끝
        private void OnInventory(InputAction.CallbackContext context) // 탐험 인벤토리 열기 전달
        { // 전달 시작
            if (IsExplorationEnabled) // 탐험 입력 활성 상태 검사
            { // 탐험 조건 시작
                InventoryRequested?.Invoke(); // 인벤토리 열기 요청
            } // 탐험 조건 끝
        } // 전달 끝
        private void OnInteract(InputAction.CallbackContext context) // 탐험 상호작용 전달
        { // 전달 시작
            if (IsExplorationEnabled) // 탐험 입력 활성 상태 검사
            { // 탐험 조건 시작
                InteractionRequested?.Invoke(); // 정면 상호작용 요청
            } // 탐험 조건 끝
        } // 전달 끝
        private void OnForceDoor(InputAction.CallbackContext context) // 탐험 강제 개방 입력 전달
        { // 전달 시작
            if (IsExplorationEnabled) // 탐험 입력 활성 상태 검사
            { // 탐험 조건 시작
                ForceDoorRequested?.Invoke(); // 잠긴 문 강제 개방 요청
            } // 탐험 조건 끝
        } // 전달 끝
        private InputAction AddMoveButton(string name, string letter, string arrow, string pad) // 개별 방향 버튼 생성
        { // 생성 시작
            var action = explorationMap.AddAction(name, InputActionType.Button); // 독립 방향 동작 생성
            action.AddBinding(letter); // 문자 키 연결
            action.AddBinding(arrow); // 화살표 키 연결
            action.AddBinding(pad); // 게임패드 방향 버튼 연결
            action.performed += OnMoveButton; // 새로 누른 방향 감지
            return action; // 구성한 방향 동작 전달
        } // 생성 끝
        private void OnMoveButton(InputAction.CallbackContext context) // 새로 누른 방향 버튼 수집
        { // 수집 시작
            switch (context.action.name) // 입력된 방향 버튼 검사
            { // 방향 선택 시작
                case "MoveUp": // 위쪽 버튼 입력
                    pendingMove += Vector2Int.up; // 위쪽 입력 누적
                    break; // 방향 선택 종료
                case "MoveDown": // 아래쪽 버튼 입력
                    pendingMove += Vector2Int.down; // 아래쪽 입력 누적
                    break; // 방향 선택 종료
                case "MoveLeft": // 왼쪽 버튼 입력
                    pendingMove += Vector2Int.left; // 왼쪽 입력 누적
                    break; // 방향 선택 종료
                case "MoveRight": // 오른쪽 버튼 입력
                    pendingMove += Vector2Int.right; // 오른쪽 입력 누적
                    break; // 방향 선택 종료
            } // 방향 선택 끝
        } // 수집 끝
        private void OnMoveStick(InputAction.CallbackContext context) // 게임패드 스틱 방향 수집
        { // 수집 시작
            var deviceId = context.control.device.deviceId; // 입력한 게임패드 식별자
            var direction = ToCardinalDirection(context.ReadValue<Vector2>()); // 스틱의 네 방향 선택
            if (direction == Vector2Int.zero) // 스틱 중립 상태 검사
            { // 중립 조건 시작
                suppressedSticks.Remove(deviceId); // 중립 복귀한 스틱 차단 해제
                lastStickDirections.Remove(deviceId); // 이전 스틱 방향 초기화
                return; // 이동 명령 없음
            } // 중립 조건 끝
            if (suppressedSticks.Contains(deviceId)) // 이전 화면에서 누른 스틱 검사
            { // 입력 차단 조건 시작
                return; // 해당 스틱의 중립 복귀 대기
            } // 입력 차단 조건 끝
            if (lastStickDirections.TryGetValue(deviceId, out var previous) && previous == direction) // 같은 방향의 세기 변화 검사
            { // 반복 방향 조건 시작
                return; // 스틱 흔들림의 중복 이동 방지
            } // 반복 방향 조건 끝
            lastStickDirections[deviceId] = direction; // 게임패드의 마지막 방향 보관
            pendingMove += direction; // 현재 입력 갱신의 스틱 방향 수집
        } // 수집 끝
        private void OnMoveStickCanceled(InputAction.CallbackContext context) // 스틱 입력 중립 처리
        { // 해제 시작
            var deviceId = context.control.device.deviceId; // 입력을 해제한 게임패드
            suppressedSticks.Remove(deviceId); // 해당 스틱 차단 해제
            lastStickDirections.Remove(deviceId); // 해당 장치 방향 초기화
        } // 해제 끝
        private void OnAfterInputUpdate() // 한 입력 갱신의 이동 방향 확정
        { // 검사 시작
            if (!IsExplorationEnabled || pendingMove == Vector2Int.zero) // 이동 입력 가능 여부 검사
            { // 검사 제외 조건 시작
                pendingMove = Vector2Int.zero; // 잠긴 화면의 대기 입력 제거
                return; // 전달할 이동 요청 없음
            } // 검사 제외 조건 끝
            var direction = ToCardinalDirection(new Vector2(pendingMove.x, pendingMove.y)); // 대각 입력의 한 방향 선택
            pendingMove = Vector2Int.zero; // 다음 입력 갱신 준비
            if (direction != Vector2Int.zero) // 유효한 방향 검사
            { // 방향 전달 조건 시작
                MoveRequested?.Invoke(direction); // 던전에 한 칸 이동 요청
            } // 방향 전달 조건 끝
        } // 검사 끝
        private static Vector2Int ToCardinalDirection(Vector2 value) // 입력 벡터의 한 방향 선택
        { // 방향 선택 시작
            var horizontal = Mathf.Abs(value.x); // 가로 입력 크기
            var vertical = Mathf.Abs(value.y); // 세로 입력 크기
            if (Mathf.Max(horizontal, vertical) < 0.5f) // 작은 스틱 입력 검사
            { // 중립 조건 시작
                return Vector2Int.zero; // 방향 입력 없음
            } // 중립 조건 끝
            if (horizontal >= vertical) // 같은 크기의 가로 우선 기준
            { // 가로 선택 시작
                return value.x > 0f ? Vector2Int.right : Vector2Int.left; // 좌우 한 방향 선택
            } // 가로 선택 끝
            return value.y > 0f ? Vector2Int.up : Vector2Int.down; // 상하 한 방향 선택
        } // 방향 선택 끝
        private void OnDestroy() // 실행 전용 입력 정리
        { // 정리 시작
            if (actions == null) // 준비되지 않은 중복 객체 검사
            { // 무시 조건 시작
                return; // 정리 대상 없음
            } // 무시 조건 끝
            if (uiModule != null) // 화면 모듈 존재 검사
            { // 해제 조건 시작
                uiModule.enabled = false; // 참조 해제 전 입력 종료
            } // 해제 조건 끝
            actions.Disable(); // 모든 입력 종료
            InputSystem.onAfterUpdate -= OnAfterInputUpdate; // 첫 입력 검사 연결 해제
            moveUp.performed -= OnMoveButton; // 위쪽 입력 연결 해제
            moveDown.performed -= OnMoveButton; // 아래쪽 입력 연결 해제
            moveLeft.performed -= OnMoveButton; // 왼쪽 입력 연결 해제
            moveRight.performed -= OnMoveButton; // 오른쪽 입력 연결 해제
            moveStick.performed -= OnMoveStick; // 스틱 입력 연결 해제
            moveStick.canceled -= OnMoveStickCanceled; // 스틱 중립 연결 해제
            explorationMap.FindAction("Back").performed -= OnBack; // 복귀 감지 연결 해제
            explorationMap.FindAction("Interact").performed -= OnInteract; // 상호작용 감지 연결 해제
            explorationMap.FindAction("ForceDoor").performed -= OnForceDoor; // 강제 개방 감지 연결 해제
            explorationMap.FindAction("Inventory").performed -= OnInventory; // 인벤토리 열기 연결 해제
            foreach (var reference in references) // 생성한 참조 순회
            { // 참조 정리 시작
                Destroy(reference); // 실행 전용 참조 삭제
            } // 참조 정리 끝
            Destroy(actions); // 실행 전용 입력 모음 삭제
        } // 정리 끝
    } // 클래스 끝
} // 이름 공간 끝
