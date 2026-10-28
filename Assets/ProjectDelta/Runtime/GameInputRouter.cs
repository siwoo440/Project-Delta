using System; // 입력 요청 알림
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
        private InputSystemUIInputModule uiModule; // 화면 입력 모듈
        private InputActionReference[] references; // 화면 입력 연결 목록
        public event Action BackRequested; // 타이틀 복귀 요청 알림
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
            var point = uiMap.AddAction("Point", InputActionType.PassThrough, "<Mouse>/position"); // 마우스 위치 입력
            var click = uiMap.AddAction("Click", InputActionType.PassThrough, "<Mouse>/leftButton"); // 마우스 선택 입력
            var scroll = uiMap.AddAction("Scroll", InputActionType.PassThrough, "<Mouse>/scroll"); // 마우스 스크롤 입력
            AddDirectionAction(explorationMap, "Move", InputActionType.Value); // 다음 일차의 이동 입력 기반
            var back = explorationMap.AddAction("Back", InputActionType.Button, "<Keyboard>/escape"); // 던전 복귀 입력 생성
            back.AddBinding("<Gamepad>/buttonEast"); // 게임패드 복귀 연결
            back.performed += OnBack; // 복귀 요청 감지 연결
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
            uiModule.enabled = false; // 기존 화면 입력 종료
            actions.Disable(); // 이전 입력 묶음 초기화
            if (phase == GamePhase.Title) // 타이틀 상태 검사
            { // 화면 입력 조건 시작
                uiModule.enabled = true; // 화면 입력 전달 활성화
                uiMap.Enable(); // 타이틀 입력만 활성화
            } // 화면 입력 조건 끝
            else if (phase == GamePhase.Exploration) // 탐험 상태 검사
            { // 탐험 입력 조건 시작
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
            explorationMap.FindAction("Back").performed -= OnBack; // 복귀 감지 연결 해제
            foreach (var reference in references) // 생성한 참조 순회
            { // 참조 정리 시작
                Destroy(reference); // 실행 전용 참조 삭제
            } // 참조 정리 끝
            Destroy(actions); // 실행 전용 입력 모음 삭제
        } // 정리 끝
    } // 클래스 끝
} // 이름 공간 끝
