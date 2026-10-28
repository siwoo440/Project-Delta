using System.Collections; // 순차 검증 도구
using NUnit.Framework; // 검증 도구
using ProjectDelta.Core; // 진행 상태 형식
using ProjectDelta.Runtime; // 실제 실행 객체
using UnityEngine; // Unity 실행 도구
using UnityEngine.EventSystems; // 화면 입력 대상
using UnityEngine.InputSystem; // 실제 입력 장치
using UnityEngine.InputSystem.LowLevel; // 입력 상태 전달
using UnityEngine.SceneManagement; // 실제 씬 전환
using UnityEngine.TestTools; // 프레임 기반 검증
using UnityEngine.UI; // 실제 화면 버튼
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class StartupFlowTests // 시작 흐름 통합 검증
    { // 클래스 시작
        private Keyboard keyboard; // 정리가 필요한 검증 장치
        private Mouse mouse; // 정리가 필요한 검증 마우스
        private InputSettings.BackgroundBehavior originalBackgroundBehavior; // 복구할 백그라운드 입력 설정
#if UNITY_EDITOR // 편집기 전용 입력 설정
        private InputSettings.EditorInputBehaviorInPlayMode originalEditorBehavior; // 복구할 편집기 입력 설정
#endif // 편집기 설정 끝
        [SetUp] // 검증 전 준비 표시
        public void RouteTestInputToGame() // 자동 실행의 입력 전달 준비
        { // 검증 시작
            originalBackgroundBehavior = InputSystem.settings.backgroundBehavior; // 기존 입력 설정 보관
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 자동 실행 창 포커스 영향 제외
#if UNITY_EDITOR // 편집기 입력 전달 설정
            originalEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 설정 보관
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 가상 장치 입력을 게임에 전달
#endif // 편집기 입력 설정 끝
        } // 검증 끝
        [UnityTest] // 실행 중 검증 표시
        public IEnumerator BackgroundClick_PreservesKeyboardAndGamepadSelection() // 빈 배경 클릭 후 선택 유지 검증
        { // 검증 시작
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 시작 씬 로드
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            var button = GameObject.Find("NewGameButton"); // 실제 새 게임 버튼 조회
            mouse = InputSystem.AddDevice<Mouse>(); // 검증용 실제 마우스 추가
            var background = new MouseState // 빈 배경 위치 준비
            { // 마우스 상태 시작
                position = new Vector2(2f, 2f) // 버튼 밖 화면 모서리
            }; // 마우스 상태 끝
            InputSystem.QueueStateEvent(mouse, background); // 마우스 위치 전달
            InputSystem.Update(); // 위치 입력 반영
            yield return null; // 화면 입력 처리 대기
            InputSystem.QueueStateEvent(mouse, background.WithButton(MouseButton.Left)); // 배경 클릭 입력 전달
            InputSystem.Update(); // 누른 상태 반영
            yield return null; // 클릭 처리 대기
            InputSystem.QueueStateEvent(mouse, background); // 마우스 버튼 해제
            InputSystem.Update(); // 해제 입력 반영
            yield return null; // 선택 상태 갱신 대기
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(button)); // 키보드와 게임패드 선택 유지 확인
        } // 검증 끝
        [UnityTest] // 실행 중 검증 표시
        public IEnumerator NewGameAndEscape_SwitchScenesAndInputMaps() // 실제 왕복 입력 검증
        { // 검증 시작
            Assert.That(Application.CanStreamedLevelBeLoaded("BootstrapScene"), Is.True, "BootstrapScene must be in Build Settings."); // 시작 씬 등록 확인
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 시작 씬 로드
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            var root = AppRoot.Instance; // 실제 공통 객체 확인
            Assert.That(root.Input.IsUiEnabled, Is.True); // 타이틀 입력 확인
            Assert.That(root.Input.IsExplorationEnabled, Is.False); // 탐험 입력 차단 확인
            var button = GameObject.Find("NewGameButton").GetComponent<Button>(); // 실제 새 게임 버튼 조회
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(button.gameObject)); // 기본 포커스 확인
            button.onClick.Invoke(); // 실제 버튼 요청 실행
            root.StartNewGame(); // 연속 중복 요청 실행
            Assert.That(root.Flow.Phase, Is.EqualTo(GamePhase.Loading)); // 즉시 입력 잠금 확인
            yield return WaitForPhase(GamePhase.Exploration); // 던전 도착 대기
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("DungeonScene")); // 실제 던전 씬 확인
            Assert.That(root.Input.IsUiEnabled, Is.False); // 이전 화면 입력 차단 확인
            Assert.That(root.Input.IsExplorationEnabled, Is.True); // 탐험 입력 활성화 확인
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증용 실제 입력 장치 추가
            InputSystem.Update(); // 새 장치와 동작 연결 반영
            yield return null; // 장치 연결 한 프레임 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // Escape 누름 입력 전달
            InputSystem.Update(); // 입력 처리 실행
            yield return WaitForPhase(GamePhase.Title); // 타이틀 복귀 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 누른 키 해제
            InputSystem.Update(); // 키 해제 반영
            InputSystem.RemoveDevice(keyboard); // 검증 장치 정리
            keyboard = null; // 정리 완료 표시
            Assert.That(root.Input.IsUiEnabled, Is.True); // 화면 입력 복구 확인
            Assert.That(root.Input.IsExplorationEnabled, Is.False); // 탐험 입력 종료 확인
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("NewGameButton")); // 복귀 포커스 확인
        } // 검증 끝
        [UnityTest] // 실행 중 검증 표시
        public IEnumerator MissingScene_PreservesActiveSceneAndReportsFailure() // 씬 누락 복구 검증
        { // 검증 시작
            var originalScene = SceneManager.GetActiveScene(); // 현재 화면 보관
            var loaderObject = new GameObject("MissingSceneTest", typeof(SceneLoader)); // 실제 로더 준비
            var loader = loaderObject.GetComponent<SceneLoader>(); // 실제 로더 조회
            LogAssert.Expect(LogType.Error, "Scene is not registered: Missing_Day01_Scene"); // 예상 오류 기록 지정
            yield return loader.LoadScene("Missing_Day01_Scene"); // 누락된 씬 로드 시도
            Assert.That(loader.Succeeded, Is.False); // 실패 결과 확인
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(originalScene)); // 기존 화면 보존 확인
            Object.Destroy(loaderObject); // 검증 로더 정리
        } // 검증 끝
        [UnityTest] // 실행 중 검증 표시
        public IEnumerator ReenteringBootstrap_KeepsOneRootAndEventSystem() // 초기화 중복 검증
        { // 검증 시작
            Assert.That(Application.CanStreamedLevelBeLoaded("BootstrapScene"), Is.True, "BootstrapScene must be in Build Settings."); // 시작 씬 등록 확인
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 첫 시작 씬 로드
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            var original = AppRoot.Instance; // 최초 공통 객체 보관
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 시작 씬 재진입
            yield return WaitForPhase(GamePhase.Title); // 타이틀 복구 대기
            yield return null; // 중복 객체 삭제 대기
            Assert.That(AppRoot.Instance, Is.SameAs(original)); // 최초 객체 유지 확인
            Assert.That(Object.FindObjectsByType<AppRoot>(FindObjectsSortMode.None).Length, Is.EqualTo(1)); // 공통 객체 단일성 확인
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1)); // 화면 입력 객체 단일성 확인
        } // 검증 끝
        [UnityTearDown] // 실행 후 정리 표시
        public IEnumerator Cleanup() // 검증 객체 정리
        { // 정리 시작
            if (mouse != null && mouse.added) // 남은 검증 마우스 검사
            { // 마우스 정리 시작
                InputSystem.RemoveDevice(mouse); // 검증 마우스 정리
                mouse = null; // 정리 완료 표시
            } // 마우스 정리 끝
            if (keyboard != null && keyboard.added) // 실패 중 남은 입력 장치 검사
            { // 장치 정리 시작
                InputSystem.RemoveDevice(keyboard); // 검증 입력 장치 정리
                keyboard = null; // 정리 완료 표시
            } // 장치 정리 끝
            if (AppRoot.Instance != null) // 남은 공통 객체 검사
            { // 정리 조건 시작
                Object.Destroy(AppRoot.Instance.gameObject); // 공통 객체 삭제
            } // 정리 조건 끝
            yield return null; // 삭제 완료 대기
            InputSystem.settings.backgroundBehavior = originalBackgroundBehavior; // 기존 백그라운드 입력 설정 복구
#if UNITY_EDITOR // 편집기 입력 설정 복구
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorBehavior; // 기존 편집기 입력 전달 복구
#endif // 편집기 복구 끝
        } // 정리 끝
        private static IEnumerator WaitForPhase(GamePhase phase) // 진행 상태 도착 대기
        { // 대기 시작
            var deadline = Time.realtimeSinceStartup + 15f; // 실제 시간 제한
            while (Time.realtimeSinceStartup < deadline) // 제한 시간 안의 반복
            { // 반복 시작
                if (AppRoot.Instance != null && AppRoot.Instance.Flow.Phase == phase && SceneManager.GetActiveScene().name != "BootstrapScene") // 씬과 상태 동시 확인
                { // 도착 조건 시작
                    yield return null; // 화면 초기화 한 프레임 대기
                    yield break; // 대기 종료
                } // 도착 조건 끝
                yield return null; // 다음 프레임 대기
            } // 반복 끝
            Assert.Fail("Timed out waiting for " + phase); // 진행 지연 실패 표시
        } // 대기 끝
    } // 클래스 끝
} // 이름 공간 끝
