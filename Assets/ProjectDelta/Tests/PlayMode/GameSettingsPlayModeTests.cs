using System.Collections; // 프레임 기반 대기
using NUnit.Framework; // 실제 게임 검증
using ProjectDelta.Core; // 게임 진행 상태
using ProjectDelta.Runtime; // 실제 설정과 화면
using UnityEngine; // 실제 화면과 오디오
using UnityEngine.EventSystems; // 화면 선택 확인
using UnityEngine.InputSystem; // 검증 입력 장치
using UnityEngine.InputSystem.LowLevel; // 실제 입력 상태
using UnityEngine.SceneManagement; // 게임 재시작
using UnityEngine.TestTools; // 프레임 기반 검증
using UnityEngine.UI; // 실제 버튼과 글자
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class GameSettingsPlayModeTests // 설정 화면과 저장 통합 검증
    { // 클래스 시작
        private EndingFlowPlayModeTests environment; // 독립 저장과 입력 환경
        private Keyboard keyboard; // 실제 취소 키 입력
        private Gamepad gamepad; // 실제 패드 취소 입력
        private float oldVolume; // 기존 오디오 크기
        [SetUp] // 독립 검증 준비
        public void Setup() // 저장 경로와 입력 설정
        { // 준비 시작
            oldVolume = AudioListener.volume; // 기존 오디오 상태 보관
            environment = new EndingFlowPlayModeTests(); // 기존 독립 환경 생성
            environment.Setup(); // 시험 저장과 입력 설정
            keyboard = InputSystem.AddDevice<Keyboard>(); // 실제 취소 키보드
            gamepad = InputSystem.AddDevice<Gamepad>(); // 실제 취소 게임패드
        } // 준비 끝
        [UnityTearDown] // 독립 검증 정리
        public IEnumerator Cleanup() // 입력과 오디오 복구
        { // 정리 시작
            if (keyboard != null && keyboard.added) // 키보드 연결 검사
            { // 키보드 정리 시작
                InputSystem.RemoveDevice(keyboard); // 독립 키보드 제거
            } // 키보드 정리 끝
            if (gamepad != null && gamepad.added) // 패드 연결 검사
            { // 패드 정리 시작
                InputSystem.RemoveDevice(gamepad); // 독립 패드 제거
            } // 패드 정리 끝
            yield return environment.Cleanup(); // 실제 앱과 경로 복구
            AudioListener.volume = oldVolume; // 이전 오디오 크기 복구
        } // 정리 끝
        [UnityTest] // 타이틀 설정 저장과 재시작 검증
        public IEnumerator TitleSettings_AppliesVolumeAndRestoresAfterRestart() // 실제 음량 적용과 저장 확인
        { // 검증 시작
            yield return StartTitle(); // 실제 타이틀 시작
            yield return OpenSettings("TitleSettingsButton"); // 타이틀 설정 열기
            Assert.That(GameObject.Find("NewGameButton").GetComponent<Button>().interactable, Is.False); // 뒤쪽 새 게임 잠금
            Assert.That(GameObject.Find("JournalButton").GetComponent<Button>().interactable, Is.False); // 뒤쪽 관찰 기록 잠금
            Assert.That(GameObject.Find("TrialCollectionButton").GetComponent<Button>().interactable, Is.False); // 뒤쪽 수집 기록 잠금
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("CloseSettingsButton")); // 안전한 닫기 기본 선택
            Click("SettingsVolumeDown"); // 실제 전체 음량 감소
            yield return null; // 실제 설정 적용 대기
            Assert.That(AudioListener.volume, Is.EqualTo(0.9f).Within(0.001f)); // 전체 오디오 실제 적용
            Assert.That(ReadText("SettingsVolumeValue"), Does.Contain("90")); // 실제 저장된 음량 표시
            Click("SettingsFlashOff"); // 실제 섬광 끄기 선택
            yield return null; // 실제 설정 저장 대기
            Assert.That(ReadText("SettingsFlashValue"), Does.Contain("끔")); // 실제 섬광 설정 표시
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 실제 설정 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath)) // 실제 화면 캡처 요청 검사
            { // 캡처 시작
                var capture = typeof(EndingFlowPlayModeTests).GetMethod("CaptureScreen", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic); // 기존 별도 화면 렌더링 조회
                Assert.That(capture, Is.Not.Null); // 실제 캡처 기능 존재 확인
                var image = (Texture2D)capture.Invoke(null, new object[] { "TitleCanvas" }); // 설정 화면이 표시된 실제 타이틀 렌더링
                try // 실제 설정 화면 파일 기록
                { // 기록 시작
                    System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 설정 화면 저장
                } // 기록 끝
                finally // 임시 화면 자원 정리
                { // 정리 시작
                    Object.Destroy(image); // 렌더링 이미지 제거
                } // 정리 끝
            } // 캡처 끝
            Object.Destroy(AppRoot.Instance.gameObject); // 실제 앱 공통 객체 종료
            yield return null; // 공통 객체 제거 대기
            yield return StartTitle(); // 같은 저장으로 실제 앱 재시작
            Assert.That(AudioListener.volume, Is.EqualTo(0.9f).Within(0.001f)); // 재시작 직후 실제 음량 복원
            yield return OpenSettings("TitleSettingsButton"); // 재시작 후 설정 열기
            Assert.That(ReadText("SettingsVolumeValue"), Does.Contain("90")); // 재시작한 음량 표시
            Assert.That(ReadText("SettingsFlashValue"), Does.Contain("끔")); // 재시작한 섬광 표시
        } // 검증 끝
        [UnityTest] // 타이틀과 일시정지의 입력 차단 검증
        public IEnumerator Settings_BackReturnsToOriginAndLocksUnderlyingInput() // ESC와 패드 취소의 이전 화면 복귀
        { // 검증 시작
            yield return StartTitle(); // 실제 타이틀 시작
            yield return OpenSettings("TitleSettingsButton"); // 타이틀 설정 열기
            Assert.That(GameObject.Find("NewGameButton").GetComponent<Button>().interactable, Is.False); // 타이틀 뒤쪽 입력 차단
            yield return PressEscape(); // 실제 ESC 취소
            Assert.That(GameObject.Find("SettingsPanel"), Is.Null); // 타이틀 설정 닫힘
            Assert.That(GameObject.Find("NewGameButton").GetComponent<Button>().interactable, Is.True); // 타이틀 입력 복구
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("TitleSettingsButton")); // 타이틀 설정 선택 복구
            AppRoot.Instance.StartNewGame(); // 실제 새 회차 시작
            yield return WaitPhase(GamePhase.Exploration); // 실제 탐험 대기
            yield return PressEscape(); // 실제 일시정지 열기
            Assert.That(GameObject.Find("PauseSettingsButton"), Is.Not.Null); // 일시정지 설정 버튼 확인
            yield return OpenSettings("PauseSettingsButton"); // 일시정지 설정 열기
            Assert.That(AppRoot.Instance.Input.IsExplorationEnabled, Is.False); // 탐험 이동 입력 차단
            Assert.That(GameObject.Find("PauseResumeButton").GetComponent<Button>().interactable, Is.False); // 뒤쪽 계속하기 입력 차단
            Assert.That(GameObject.Find("PauseSaveButton").GetComponent<Button>().interactable, Is.False); // 뒤쪽 저장 입력 차단
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.East)); // 실제 패드 취소 입력
            yield return null; // 패드 취소 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 실제 패드 입력 해제
            yield return null; // 이전 화면 복귀 대기
            Assert.That(GameObject.Find("SettingsPanel"), Is.Null); // 패드 취소로 설정 닫힘
            Assert.That(GameObject.Find("PauseSettingsButton"), Is.Not.Null); // 실제 일시정지 메뉴 복귀
            Assert.That(GameObject.Find("PauseResumeButton").GetComponent<Button>().interactable, Is.True); // 일시정지 입력 복구
            Assert.That(AppRoot.Instance.Input.IsExplorationEnabled, Is.False); // 일시정지 중 탐험 입력 유지 차단
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("PauseSettingsButton")); // 일시정지 설정 선택 복구
        } // 검증 끝
        [UnityTest] // 섬광 설정의 실제 조우 연출 검증
        public IEnumerator FlashOff_SuppressesWhiteFlashDuringEncounter() // 섬광 제거와 전투 전환 확인
        { // 검증 시작
            yield return StartTitle(); // 실제 타이틀 시작
            yield return OpenSettings("TitleSettingsButton"); // 실제 설정 화면 열기
            Click("SettingsFlashOff"); // 실제 섬광 끄기 저장
            yield return null; // 설정 적용 대기
            Click("CloseSettingsButton"); // 실제 설정 닫기
            yield return null; // 타이틀 복귀 대기
            AppRoot.Instance.StartNewGame(); // 실제 새 회차 시작
            yield return WaitPhase(GamePhase.Exploration); // 실제 탐험 대기
            Assert.That(AppRoot.Instance.StartEncounter(), Is.True); // 실제 자동 조우 전환 시작
            var deadline = Time.realtimeSinceStartup + 10f; // 전환 완료 시간 제한
            var observedOverlay = false; // 실제 전환막 관찰 여부
            while (Time.realtimeSinceStartup < deadline && AppRoot.Instance.Flow.Phase != GamePhase.Combat) // 조우 전환 동안 프레임 검사
            { // 전환 검사 시작
                var overlay = GameObject.Find("EncounterTransitionOverlay"); // 실제 조우 전환막
                if (overlay != null) // 실제 전환막 존재 검사
                { // 섬광 검사 시작
                    observedOverlay = true; // 실제 연출 관찰 확인
                    var color = overlay.GetComponent<Image>().color; // 전환막 실제 색상
                    Assert.That(color.r > 0.9f && color.g > 0.9f && color.b > 0.9f && color.a > 0.01f, Is.False); // 흰 섬광 표시 차단
                } // 섬광 검사 끝
                yield return null; // 다음 실제 화면 프레임
            } // 전환 검사 끝
            Assert.That(observedOverlay, Is.True); // 실제 전환막 검사 확인
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 섬광 없이 실제 전투 진입
        } // 검증 끝
        [UnityTest] // 감소 설정의 실제 조우 섬광 검증
        public IEnumerator FlashReduced_LimitsWhiteFlashAndEntersCombat() // 감소 강도와 전투 전환 확인
        { // 검증 시작
            yield return StartTitle(); // 실제 타이틀 시작
            yield return OpenSettings("TitleSettingsButton"); // 실제 설정 화면 열기
            Click("SettingsFlashReduced"); // 실제 섬광 감소 저장
            yield return null; // 설정 적용 대기
            Assert.That(ReadText("SettingsFlashValue"), Does.Contain("감소")); // 실제 감소 설정 확인
            Click("CloseSettingsButton"); // 실제 설정 닫기
            yield return null; // 타이틀 복귀 대기
            AppRoot.Instance.StartNewGame(); // 실제 새 회차 시작
            yield return WaitPhase(GamePhase.Exploration); // 실제 탐험 대기
            Assert.That(AppRoot.Instance.StartEncounter(), Is.True); // 실제 자동 조우 전환 시작
            var deadline = Time.realtimeSinceStartup + 10f; // 전환 완료 시간 제한
            var observedFlash = false; // 실제 흰 섬광 관찰 여부
            while (Time.realtimeSinceStartup < deadline && AppRoot.Instance.Flow.Phase != GamePhase.Combat) // 조우 전환 동안 프레임 검사
            { // 전환 검사 시작
                var overlay = GameObject.Find("EncounterTransitionOverlay"); // 실제 조우 전환막
                if (overlay != null) // 실제 전환막 존재 검사
                { // 섬광 검사 시작
                    var color = overlay.GetComponent<Image>().color; // 실제 전환막 색상
                    if (color.r > 0.9f && color.g > 0.9f && color.b > 0.9f && color.a > 0.001f) // 실제 흰 섬광 표시 검사
                    { // 강도 검사 시작
                        observedFlash = true; // 실제 흰 섬광 관찰 확인
                        Assert.That(color.a, Is.LessThanOrEqualTo(0.35f + 0.001f)); // 감소 섬광 최대 강도 확인
                    } // 강도 검사 끝
                } // 섬광 검사 끝
                yield return null; // 다음 실제 화면 프레임
            } // 전환 검사 끝
            Assert.That(observedFlash, Is.True); // 실제 감소 섬광 관찰 확인
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 감소 섬광 후 실제 전투 진입
        } // 검증 끝
        [UnityTest] // 잠긴 설정 파일의 변경 실패 검증
        public IEnumerator LockedSettings_RejectsChangeAndPreservesAppliedState() // 실제 잠금의 안전한 저장 거절
        { // 검증 시작
            yield return StartTitle(); // 실제 타이틀 시작
            yield return OpenSettings("TitleSettingsButton"); // 설정 화면 열기
            Click("SettingsVolumeDown"); // 정상 설정 파일 생성
            yield return null; // 정상 저장 대기
            var directory = RunStorage.TestDirectoryOverride; // 실제 시험 저장 폴더
            var path = System.IO.Path.Combine(directory, "settings-v1.json"); // 실제 설정 저장 파일
            var original = System.IO.File.ReadAllBytes(path); // 정상 설정 원본 보관
            var before = AudioListener.volume; // 현재 적용된 실제 음량
            using (var gate = new System.IO.FileStream(System.IO.Path.Combine(directory, "settings.lock"), System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None)) // 실제 저장 잠금 점유
            { // 잠금 시작
                Click("SettingsVolumeDown"); // 잠금 중 실제 설정 변경 시도
                yield return null; // 저장 실패 표시 대기
                Assert.That(AudioListener.volume, Is.EqualTo(before).Within(0.001f)); // 실패 시 실제 음량 보존
                Assert.That(ReadText("SettingsVolumeValue"), Does.Contain("90")); // 실패 시 설정값 보존
                Assert.That(ReadText("SettingsNotice"), Does.Contain("저장하지 못")); // 실제 저장 실패 안내
                Assert.That(System.IO.File.ReadAllBytes(path), Is.EqualTo(original)); // 정상 설정 원본 보존
            } // 잠금 끝
        } // 검증 끝
        [UnityTest] // 손상 설정 파일의 변경 실패 검증
        public IEnumerator CorruptSettings_RejectsChangesAndPreservesOriginal() // 실제 손상 파일 보호 확인
        { // 검증 시작
            var directory = RunStorage.TestDirectoryOverride; // 실제 시험 저장 폴더
            System.IO.Directory.CreateDirectory(directory); // 독립 저장 폴더 준비
            var path = System.IO.Path.Combine(directory, "settings-v1.json"); // 실제 설정 파일 경로
            System.IO.File.WriteAllText(path, "damaged settings"); // 실제 손상 설정 원본
            yield return StartTitle(); // 손상된 설정으로 실제 앱 시작
            yield return OpenSettings("TitleSettingsButton"); // 실제 설정 화면 열기
            var initialNotice = ReadText("SettingsNotice"); // 변경 전 실제 손상 안내
            Assert.That(initialNotice, Does.Contain("보존")); // 손상 원본 보호 안내
            Assert.That(initialNotice, Does.Contain("기본 또는 복구")); // 표시값의 대체 설정 안내
            var before = AudioListener.volume; // 기존 적용 음량
            var displayed = ReadText("SettingsVolumeValue"); // 기존 표시 설정값
            Click("SettingsVolumeDown"); // 손상 파일의 실제 변경 시도
            yield return null; // 저장 거절 표시 대기
            Assert.That(AudioListener.volume, Is.EqualTo(before).Within(0.001f)); // 손상 시 실제 음량 보존
            Assert.That(ReadText("SettingsVolumeValue"), Is.EqualTo(displayed)); // 손상 시 표시값 보존
            Assert.That(ReadText("SettingsNotice"), Is.Not.Empty); // 실제 손상 안내
            Assert.That(System.IO.File.ReadAllText(path), Is.EqualTo("damaged settings")); // 손상 원본 보존
        } // 검증 끝
        private static void Click(string name) // 실제 화면 버튼 실행
        { // 실행 시작
            var target = GameObject.Find(name); // 지정 화면 버튼
            Assert.That(target, Is.Not.Null, name); // 실제 버튼 존재 확인
            target.GetComponent<Button>().onClick.Invoke(); // 실제 버튼 동작 실행
        } // 실행 끝
        private static string ReadText(string name) // 실제 설정 문구 조회
        { // 조회 시작
            var target = GameObject.Find(name); // 지정 화면 글자
            Assert.That(target, Is.Not.Null, name); // 실제 글자 존재 확인
            return target.GetComponent<Text>().text; // 실제 표시 문구 반환
        } // 조회 끝
        private static IEnumerator OpenSettings(string buttonName) // 실제 설정 버튼 사용
        { // 열기 시작
            Click(buttonName); // 현재 화면의 설정 버튼 실행
            yield return null; // 실제 설정 화면 구성 대기
            Assert.That(GameObject.Find("SettingsPanel"), Is.Not.Null); // 실제 설정 화면 확인
        } // 열기 끝
        private IEnumerator PressEscape() // 실제 키보드 취소 입력
        { // 입력 시작
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // ESC 누름
            yield return null; // 실제 키 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // ESC 해제
            yield return null; // 실제 해제와 화면 전환 대기
        } // 입력 끝
        private static IEnumerator StartTitle() // 실제 타이틀 시작
        { // 시작 처리
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 시작 씬 진입
            yield return WaitPhase(GamePhase.Title); // 타이틀 상태 대기
            var deadline = Time.realtimeSinceStartup + 20f; // 버튼 생성 대기 제한
            while (GameObject.Find("TitleSettingsButton") == null && Time.realtimeSinceStartup < deadline) // 실제 타이틀 버튼 검사
            { // 대기 시작
                yield return null; // 다음 프레임 대기
            } // 대기 끝
            Assert.That(GameObject.Find("TitleSettingsButton"), Is.Not.Null); // 실제 설정 버튼 확인
            yield return null; // 타이틀 상태 갱신
        } // 시작 끝
        private static IEnumerator WaitPhase(GamePhase phase) // 실제 게임 상태 대기
        { // 대기 시작
            var deadline = Time.realtimeSinceStartup + 20f; // 화면 전환 시간 제한
            while ((AppRoot.Instance == null || AppRoot.Instance.Flow.Phase != phase) && Time.realtimeSinceStartup < deadline) // 실제 상태 검사
            { // 대기 시작
                yield return null; // 다음 프레임 대기
            } // 대기 끝
            Assert.That(AppRoot.Instance, Is.Not.Null); // 공통 실행 객체 확인
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(phase)); // 실제 목표 상태 확인
            yield return null; // 화면 구성 대기
        } // 대기 끝
    } // 클래스 끝
} // 이름 공간 끝
