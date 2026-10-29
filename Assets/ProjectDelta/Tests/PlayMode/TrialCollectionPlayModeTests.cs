using System.Collections; // 프레임 기반 대기
using NUnit.Framework; // 실제 게임 검증
using ProjectDelta.Core; // 결말과 관찰 규칙
using ProjectDelta.Runtime; // 영구 기록과 화면
using UnityEngine; // 실제 게임 객체
using UnityEngine.EventSystems; // 화면 선택 확인
using UnityEngine.InputSystem; // 검증 입력 장치
using UnityEngine.InputSystem.LowLevel; // 실제 입력 상태
using UnityEngine.SceneManagement; // 게임 재시작
using UnityEngine.TestTools; // 프레임 기반 검증
using UnityEngine.UI; // 화면 버튼과 글자
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TrialCollectionPlayModeTests // 시험 수집 화면 통합 검증
    { // 클래스 시작
        private EndingFlowPlayModeTests environment; // 독립 저장과 입력 환경
        private Keyboard keyboard; // 실제 취소 키 입력
        private Gamepad gamepad; // 실제 패드 취소 입력
        [SetUp] // 독립 게임 환경 준비
        public void Setup() // 저장 경로와 입력 장치 설정
        { // 준비 시작
            environment = new EndingFlowPlayModeTests(); // 기존 독립 환경 생성
            environment.Setup(); // 테스트 전용 저장과 입력 설정
            keyboard = InputSystem.AddDevice<Keyboard>(); // 독립 취소 키보드
            gamepad = InputSystem.AddDevice<Gamepad>(); // 독립 취소 게임패드
        } // 준비 끝
        [UnityTearDown] // 독립 게임 환경 정리
        public IEnumerator Cleanup() // 입력과 저장 경로 복구
        { // 정리 시작
            if (keyboard != null && keyboard.added) // 실제 키보드 연결 검사
            { // 키보드 정리 시작
                InputSystem.RemoveDevice(keyboard); // 검증용 키보드 제거
            } // 키보드 정리 끝
            if (gamepad != null && gamepad.added) // 실제 패드 연결 검사
            { // 패드 정리 시작
                InputSystem.RemoveDevice(gamepad); // 검증용 패드 제거
            } // 패드 정리 끝
            yield return environment.Cleanup(); // 기존 독립 환경 복구
        } // 정리 끝
        [UnityTest] // 새 저장의 시험 결말 비공개 검증
        public IEnumerator FreshSave_HidesSevenEndingNamesAndDetails() // 미해금 결말의 목록 검사
        { // 검증 시작
            yield return StartTitle(); // 실제 새 저장 타이틀 진입
            yield return OpenCollection(); // 실제 시험 수집 화면 열기
            var endings = ReadText("TrialCollectionEndings"); // 실제 시험 결말 목록
            Assert.That(Count(endings, "??? · 미해금"), Is.EqualTo(7)); // 일곱 결말의 잠금 슬롯 표시
            var ids = new[] { EndingRules.ReturnBasic, EndingRules.ReturnHeroic, EndingRules.ReturnEmptyHanded, EndingRules.ReturnWounded, EndingRules.StayBasic, EndingRules.StayConquest, EndingRules.DemonServant }; // 실제 시험 결말 목록
            foreach (var id in ids) // 모든 시험 결말 순회
            { // 비공개 검사 시작
                Assert.That(endings, Does.Not.Contain(EndingRules.DisplayName(id))); // 미해금 결말 이름 비공개
            } // 비공개 검사 끝
            Assert.That(endings, Does.Not.Contain("장비")); // 미해금 상세 조건 비공개
            Assert.That(endings, Does.Not.Contain("체력")); // 미해금 상세 수치 비공개
            Assert.That(ReadText("TrialCollectionProgress"), Does.Contain("첫 관찰 기록: 잠금")); // 실제 관찰 전 시험 기록
            Assert.That(ReadText("TrialCollectionProgress"), Does.Contain("첫 패배 기록: 잠금")); // 실제 패배 전 시험 기록
        } // 검증 끝
        [UnityTest] // 실제 관찰과 패배 기록 검증
        public IEnumerator SavedObservationAndDefeat_ShowOnlyActualRecords() // 실제 기록의 분리 표시
        { // 검증 시작
            var observation = new ObservationStorage(RunStorage.TestDirectoryOverride); // 실제 관찰 저장소
            var journal = observation.Load(out var warning); // 현재 관찰 기록 검증
            Assert.That(warning, Is.Empty); // 정상 관찰 저장 확인
            Assert.That(journal.TryRecord("MON_SLIME"), Is.True); // 실제 슬라임 관찰 기록
            Assert.That(observation.TrySave(journal, out var error), Is.True, error); // 관찰 기록 안전 저장
            var defeats = new DefeatHistoryStorage(RunStorage.TestDirectoryOverride, id => MonsterCatalog.Find(id) != null); // 실제 패배 저장소
            defeats.Load(out warning); // 패배 저장소 최초 조회
            Assert.That(warning, Is.Empty); // 정상 패배 저장 확인
            Assert.That(defeats.TryAppend(System.Guid.NewGuid().ToString("N"), "MON_SLIME", BattleDefeatReason.HealthZero, out _, out error), Is.True, error); // 실제 슬라임 패배 기록 저장
            yield return StartTitle(); // 저장된 기록으로 실제 앱 시작
            yield return OpenCollection(); // 시험 수집 화면 열기
            Assert.That(ReadText("TrialCollectionObservations"), Does.Contain("슬라임")); // 실제 관찰 대상 표시
            Assert.That(ReadText("TrialCollectionObservations"), Does.Not.Contain("고블린")); // 관찰하지 않은 대상 비공개
            Assert.That(ReadText("TrialCollectionDefeats"), Does.Contain("슬라임")); // 실제 패배 대상 표시
            Assert.That(ReadText("TrialCollectionDefeats"), Does.Not.Contain("고블린")); // 기록되지 않은 패배 비공개
            Assert.That(ReadText("TrialCollectionProgress"), Does.Contain("첫 관찰 기록: 달성")); // 관찰 기반 시험 기록
            Assert.That(ReadText("TrialCollectionProgress"), Does.Contain("첫 패배 기록: 달성")); // 패배 기반 시험 기록
        } // 검증 끝
        [UnityTest] // 앱 실행 후 관찰 파일 손상과 삭제 검증
        public IEnumerator ObservationChangedAfterStartup_ShowsUnavailableWithoutStaleRecords() // 실행 중 외부 변경의 안전한 재조회
        { // 검증 시작
            var observation = new ObservationStorage(RunStorage.TestDirectoryOverride); // 실제 관찰 저장소
            var journal = observation.Load(out var warning); // 관찰 저장소 최초 조회
            Assert.That(warning, Is.Empty); // 정상 저장소 확인
            Assert.That(journal.TryRecord("MON_SLIME"), Is.True); // 실제 슬라임 관찰 기록
            Assert.That(observation.TrySave(journal, out var error), Is.True, error); // 관찰 기록 안전 저장
            var path = System.IO.Path.Combine(RunStorage.TestDirectoryOverride, "observation-v1.json"); // 실제 관찰 파일 경로
            var original = System.IO.File.ReadAllBytes(path); // 실제 정상 원본 보관
            yield return StartTitle(); // 정상 관찰 기록으로 앱 시작
            yield return OpenCollection(); // 정상 기록의 수집 화면 열기
            Assert.That(ReadText("TrialCollectionObservations"), Does.Contain("슬라임")); // 실제 관찰 기록 확인
            GameObject.Find("CloseTrialCollectionButton").GetComponent<Button>().onClick.Invoke(); // 수집 화면 닫기
            yield return null; // 닫힌 화면 정리 대기
            System.IO.File.WriteAllText(path, "damaged observation after startup"); // 실행 중 실제 파일 손상
            yield return OpenCollection(); // 손상 파일의 수집 화면 재열기
            Assert.That(ReadText("TrialCollectionObservations"), Does.Contain("관찰 기록 확인 불가")); // 손상 기록 확인 불가 표시
            Assert.That(ReadText("TrialCollectionObservations"), Does.Not.Contain("슬라임")); // 손상 전 오래된 관찰 표시 차단
            Assert.That(ReadText("TrialCollectionProgress"), Does.Contain("첫 관찰 기록: 확인 불가")); // 손상 시 진행 상태 확인 불가
            Assert.That(System.IO.File.ReadAllText(path), Is.EqualTo("damaged observation after startup")); // 손상 원본 파일 보존
            GameObject.Find("CloseTrialCollectionButton").GetComponent<Button>().onClick.Invoke(); // 손상 상태 화면 닫기
            yield return null; // 닫힌 화면 정리 대기
            System.IO.File.WriteAllBytes(path, original); // 정상 원본 파일 복구
            yield return OpenCollection(); // 정상 파일의 수집 화면 재열기
            Assert.That(ReadText("TrialCollectionObservations"), Does.Contain("슬라임")); // 정상 원본의 관찰 재조회 확인
            GameObject.Find("CloseTrialCollectionButton").GetComponent<Button>().onClick.Invoke(); // 정상 화면 닫기
            yield return null; // 닫힌 화면 정리 대기
            System.IO.File.Delete(path); // 실행 중 실제 관찰 파일 삭제
            yield return OpenCollection(); // 삭제 파일의 수집 화면 재열기
            Assert.That(ReadText("TrialCollectionObservations"), Does.Contain("관찰 기록 확인 불가")); // 누락 기록 확인 불가 표시
            Assert.That(ReadText("TrialCollectionObservations"), Does.Not.Contain("슬라임")); // 삭제 전 오래된 관찰 표시 차단
            Assert.That(ReadText("TrialCollectionProgress"), Does.Contain("첫 관찰 기록: 확인 불가")); // 누락 시 진행 상태 확인 불가
            Assert.That(System.IO.File.Exists(path), Is.False); // 삭제된 파일의 자동 재생성 차단
        } // 검증 끝
        [UnityTest] // 앱 실행 후 결말과 패배 파일 삭제 검증
        public IEnumerator EndingAndDefeatDeletedAfterStartup_OnlyMissingSectionIsUnavailable() // 독립 기록 삭제의 안전한 재조회
        { // 검증 시작
            var directory = RunStorage.TestDirectoryOverride; // 실제 시험 저장 폴더
            var endings = new EndingHistoryStorage(directory); // 실제 결말 저장소
            endings.Load(out var warning); // 결말 저장소 최초 조회
            Assert.That(warning, Is.Empty); // 정상 결말 저장소 확인
            Assert.That(endings.TryRecord(System.Guid.NewGuid().ToString("N"), System.Guid.NewGuid().ToString("N"), EndingRules.ReturnHeroic, out _, out _, out var error), Is.True, error); // 실제 영웅 귀환 영수증 저장
            var defeats = new DefeatHistoryStorage(directory, id => MonsterCatalog.Find(id) != null); // 실제 패배 저장소
            defeats.Load(out warning); // 패배 저장소 최초 조회
            Assert.That(warning, Is.Empty); // 정상 패배 저장소 확인
            Assert.That(defeats.TryAppend(System.Guid.NewGuid().ToString("N"), "MON_SLIME", BattleDefeatReason.HealthZero, out _, out error), Is.True, error); // 실제 슬라임 패배 저장
            var endingPath = System.IO.Path.Combine(directory, "ending-history-v1.json"); // 실제 결말 기록 파일
            var defeatPath = System.IO.Path.Combine(directory, "defeat-v1.json"); // 실제 패배 기록 파일
            var originalEnding = System.IO.File.ReadAllBytes(endingPath); // 정상 결말 원본 보관
            var originalDefeat = System.IO.File.ReadAllBytes(defeatPath); // 정상 패배 원본 보관
            yield return StartTitle(); // 두 정상 기록으로 실제 앱 시작
            yield return OpenCollection(); // 정상 기록의 수집 화면 열기
            AssertHeroicOnly(); // 실제 영웅 귀환 표시 확인
            Assert.That(ReadText("TrialCollectionDefeats"), Does.Contain("슬라임")); // 실제 슬라임 패배 표시 확인
            GameObject.Find("CloseTrialCollectionButton").GetComponent<Button>().onClick.Invoke(); // 정상 수집 화면 닫기
            yield return null; // 닫힌 화면 정리 대기
            System.IO.File.Delete(endingPath); // 실행 중 결말 기록 파일 삭제
            yield return OpenCollection(); // 결말 누락 상태의 수집 화면 열기
            Assert.That(ReadText("TrialCollectionEndings"), Does.Contain("결말 기록 확인 불가")); // 누락된 결말 확인 불가 표시
            Assert.That(ReadText("TrialCollectionEndings"), Does.Not.Contain(EndingRules.DisplayName(EndingRules.ReturnHeroic))); // 오래된 영웅 귀환 표시 차단
            Assert.That(ReadText("TrialCollectionEndings"), Does.Not.Contain("??? · 미해금")); // 누락을 미해금으로 오인 차단
            Assert.That(ReadText("TrialCollectionDefeats"), Does.Contain("슬라임")); // 정상 패배 기록 유지
            Assert.That(ReadText("TrialCollectionProgress"), Does.Contain("첫 패배 기록: 달성")); // 정상 패배 진행 상태 유지
            Assert.That(System.IO.File.Exists(endingPath), Is.False); // 삭제된 결말 파일 재생성 차단
            Assert.That(System.IO.File.ReadAllBytes(defeatPath), Is.EqualTo(originalDefeat)); // 정상 패배 파일 원본 유지
            GameObject.Find("CloseTrialCollectionButton").GetComponent<Button>().onClick.Invoke(); // 결말 누락 화면 닫기
            yield return null; // 닫힌 화면 정리 대기
            System.IO.File.WriteAllBytes(endingPath, originalEnding); // 정상 결말 원본 복구
            yield return OpenCollection(); // 정상 결말의 수집 화면 재열기
            AssertHeroicOnly(); // 복구된 결말 기록 재조회 확인
            GameObject.Find("CloseTrialCollectionButton").GetComponent<Button>().onClick.Invoke(); // 정상 수집 화면 닫기
            yield return null; // 닫힌 화면 정리 대기
            System.IO.File.Delete(defeatPath); // 실행 중 패배 기록 파일 삭제
            yield return OpenCollection(); // 패배 누락 상태의 수집 화면 열기
            Assert.That(ReadText("TrialCollectionDefeats"), Does.Contain("패배 기록 확인 불가")); // 누락된 패배 확인 불가 표시
            Assert.That(ReadText("TrialCollectionDefeats"), Does.Not.Contain("슬라임")); // 오래된 패배 표시 차단
            Assert.That(ReadText("TrialCollectionProgress"), Does.Contain("첫 패배 기록: 확인 불가")); // 패배 진행 상태 확인 불가
            AssertHeroicOnly(); // 정상 결말 섹션 유지
            Assert.That(System.IO.File.Exists(defeatPath), Is.False); // 삭제된 패배 파일 재생성 차단
            Assert.That(System.IO.File.ReadAllBytes(endingPath), Is.EqualTo(originalEnding)); // 정상 결말 파일 원본 유지
        } // 검증 끝
        [UnityTest] // 실제 결말 기록과 재시작 표시 검증
        public IEnumerator EndingReceipt_RestartRevealsOnlyRecordedName() // 영구 영수증의 시험 목록 반영
        { // 검증 시작
            var endings = new EndingHistoryStorage(RunStorage.TestDirectoryOverride); // 실제 결말 저장소
            endings.Load(out var warning); // 결말 기록 최초 조회
            Assert.That(warning, Is.Empty); // 정상 저장 상태 확인
            Assert.That(endings.TryRecord(System.Guid.NewGuid().ToString("N"), System.Guid.NewGuid().ToString("N"), EndingRules.ReturnHeroic, out _, out _, out var error), Is.True, error); // 영웅 귀환 실제 영수증 저장
            yield return StartTitle(); // 영구 기록으로 앱 시작
            yield return OpenCollection(); // 수집 화면 열기
            AssertHeroicOnly(); // 실제 기록의 단일 이름 표시 확인
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 실제 수집 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath)) // 실제 화면 캡처 요청 검사
            { // 캡처 시작
                var capture = typeof(EndingFlowPlayModeTests).GetMethod("CaptureScreen", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic); // 기존 별도 화면 렌더링 조회
                Assert.That(capture, Is.Not.Null); // 실제 캡처 기능 존재 확인
                var image = (Texture2D)capture.Invoke(null, new object[] { "TitleCanvas" }); // 결말 영수증이 표시된 실제 타이틀 렌더링
                try // 실제 수집 화면 파일 기록
                { // 기록 시작
                    System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 수집 화면 저장
                } // 기록 끝
                finally // 임시 화면 자원 정리
                { // 정리 시작
                    Object.Destroy(image); // 렌더링 이미지 제거
                } // 정리 끝
            } // 캡처 끝
            Object.Destroy(AppRoot.Instance.gameObject); // 실제 앱 실행 종료
            yield return null; // 공통 객체 제거 대기
            yield return StartTitle(); // 동일 영구 기록으로 실제 재시작
            yield return OpenCollection(); // 재시작한 수집 화면 열기
            AssertHeroicOnly(); // 재시작 후 동일 해금 확인
        } // 검증 끝
        [UnityTest] // 세 독립 파일의 손상 보호 검증
        public IEnumerator CorruptHistories_ShowUnavailableAndPreserveOriginals() // 손상 기록의 미확인 표시
        { // 검증 시작
            var directory = RunStorage.TestDirectoryOverride; // 실제 시험 저장 폴더
            System.IO.Directory.CreateDirectory(directory); // 독립 저장 폴더 생성
            var names = new[] { "ending-history-v1.json", "observation-v1.json", "defeat-v1.json" }; // 독립 영구 기록 파일
            foreach (var name in names) // 실제 기록 파일 순회
            { // 손상 기록 시작
                System.IO.File.WriteAllText(System.IO.Path.Combine(directory, name), "damaged " + name); // 독립 손상 원본 생성
            } // 손상 기록 끝
            yield return StartTitle(); // 손상된 실제 저장으로 앱 시작
            yield return OpenCollection(); // 손상 상태의 수집 화면 열기
            Assert.That(ReadText("TrialCollectionEndings"), Does.Contain("결말 기록 확인 불가")); // 결말 손상 안내
            Assert.That(ReadText("TrialCollectionEndings"), Does.Not.Contain("??? · 미해금")); // 손상을 미해금으로 오인 차단
            Assert.That(ReadText("TrialCollectionObservations"), Does.Contain("관찰 기록 확인 불가")); // 관찰 손상 안내
            Assert.That(ReadText("TrialCollectionDefeats"), Does.Contain("패배 기록 확인 불가")); // 패배 손상 안내
            Assert.That(ReadText("TrialCollectionProgress"), Does.Contain("첫 관찰 기록: 확인 불가")); // 관찰 진행의 불명 상태
            Assert.That(ReadText("TrialCollectionProgress"), Does.Contain("첫 패배 기록: 확인 불가")); // 패배 진행의 불명 상태
            foreach (var name in names) // 손상된 실제 파일 순회
            { // 원본 검사 시작
                Assert.That(System.IO.File.ReadAllText(System.IO.Path.Combine(directory, name)), Is.EqualTo("damaged " + name)); // 각 원본 파일 보존
            } // 원본 검사 끝
        } // 검증 끝
        [UnityTest] // 실제 키보드와 게임패드 취소 검증
        public IEnumerator Panel_LocksTitleAndClosesWithEscapeAndPadCancel() // 패널 입력 차단과 복구
        { // 검증 시작
            yield return StartTitle(); // 실제 타이틀 시작
            yield return OpenCollection(); // 실제 수집 화면 열기
            AssertTitleLocked(); // 타이틀 버튼 입력 차단 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 ESC 누름
            yield return null; // 취소 입력 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // ESC 해제
            yield return null; // 닫기와 선택 복구 대기
            Assert.That(GameObject.Find("TrialCollectionPanel"), Is.Null); // ESC로 패널 닫힘 확인
            Assert.That(GameObject.Find("NewGameButton").GetComponent<Button>().interactable, Is.True); // 타이틀 버튼 복구
            yield return OpenCollection(); // 수집 화면 다시 열기
            AssertTitleLocked(); // 패드 취소 전 타이틀 잠금 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.East)); // 실제 패드 취소 입력
            yield return null; // 취소 입력 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 패드 버튼 해제
            yield return null; // 닫기와 선택 복구 대기
            Assert.That(GameObject.Find("TrialCollectionPanel"), Is.Null); // 패드 취소로 패널 닫힘 확인
            Assert.That(GameObject.Find("NewGameButton").GetComponent<Button>().interactable, Is.True); // 타이틀 버튼 복구
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("TrialCollectionButton")); // 수집 버튼 선택 복구
        } // 검증 끝
        private static void AssertTitleLocked() // 다른 타이틀 동작 잠금 검사
        { // 검사 시작
            Assert.That(GameObject.Find("NewGameButton").GetComponent<Button>().interactable, Is.False); // 새 게임 입력 차단
            Assert.That(GameObject.Find("ContinueButton").GetComponent<Button>().interactable, Is.False); // 이어하기 입력 차단
            Assert.That(GameObject.Find("JournalButton").GetComponent<Button>().interactable, Is.False); // 관찰 기록 입력 차단
            Assert.That(GameObject.Find("TrialCollectionButton").GetComponent<Button>().interactable, Is.False); // 중복 열기 차단
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("CloseTrialCollectionButton")); // 닫기 기본 선택
        } // 검사 끝
        private static void AssertHeroicOnly() // 실제 결말 영수증 표시 확인
        { // 검사 시작
            var text = ReadText("TrialCollectionEndings"); // 실제 결말 목록
            Assert.That(text, Does.Contain(EndingRules.DisplayName(EndingRules.ReturnHeroic))); // 저장된 영웅 귀환 표시
            Assert.That(Count(text, "??? · 미해금"), Is.EqualTo(6)); // 나머지 여섯 결말 비공개
            Assert.That(text, Does.Not.Contain(EndingRules.DisplayName(EndingRules.ReturnBasic))); // 다른 미해금 이름 비공개
        } // 검사 끝
        private static int Count(string text, string value) // 화면의 잠금 슬롯 개수 계산
        { // 계산 시작
            var count = 0; // 발견 슬롯 초기화
            var offset = 0; // 문자열 검색 위치
            while ((offset = text.IndexOf(value, offset, System.StringComparison.Ordinal)) >= 0) // 다음 잠금 표시 검사
            { // 검색 시작
                count++; // 잠금 슬롯 수 증가
                offset += value.Length; // 다음 검색 위치
            } // 검색 끝
            return count; // 실제 잠금 슬롯 개수
        } // 계산 끝
        private static string ReadText(string name) // 실제 수집 화면 글자 조회
        { // 조회 시작
            var target = GameObject.Find(name); // 지정된 실제 화면 요소
            Assert.That(target, Is.Not.Null, name); // 화면 요소 존재 확인
            var text = target.GetComponent<Text>(); // 실제 글자 구성
            Assert.That(text, Is.Not.Null, name); // 실제 글자 구성 확인
            return text.text; // 현재 화면 문구 반환
        } // 조회 끝
        private static IEnumerator StartTitle() // 실제 타이틀 씬 시작
        { // 시작 처리
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 시작 씬 진입
            var deadline = Time.realtimeSinceStartup + 20f; // 타이틀 대기 제한
            while (Time.realtimeSinceStartup < deadline && (AppRoot.Instance == null || AppRoot.Instance.Flow.Phase != GamePhase.Title || GameObject.Find("TrialCollectionButton") == null)) // 실제 타이틀 준비 검사
            { // 대기 시작
                yield return null; // 다음 프레임 대기
            } // 대기 끝
            Assert.That(GameObject.Find("TrialCollectionButton"), Is.Not.Null); // 실제 수집 버튼 확인
            yield return null; // 타이틀 버튼 상태 초기화
        } // 시작 끝
        private static IEnumerator OpenCollection() // 실제 타이틀 수집 버튼 사용
        { // 열기 시작
            var button = GameObject.Find("TrialCollectionButton"); // 실제 수집 버튼
            Assert.That(button, Is.Not.Null); // 실제 버튼 존재 확인
            Assert.That(button.GetComponent<Button>().interactable, Is.True); // 실제 버튼 사용 가능 확인
            button.GetComponent<Button>().onClick.Invoke(); // 실제 수집 화면 열기
            yield return null; // 화면과 입력 상태 갱신 대기
            Assert.That(GameObject.Find("TrialCollectionPanel"), Is.Not.Null); // 실제 수집 패널 확인
        } // 열기 끝
    } // 클래스 끝
} // 이름 공간 끝
