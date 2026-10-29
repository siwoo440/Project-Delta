using System.Collections; // 프레임 기반 대기
using System.Collections.Generic; // 실제 방 경로 탐색
using System.Linq; // 첫 방의 실제 출구 조회
using System.Reflection; // 실제 방 객체 조회
using NUnit.Framework; // PlayMode 검증 도구
using ProjectDelta.Core; // 전투와 저장 데이터
using ProjectDelta.Runtime; // 실제 회차와 화면
using UnityEngine; // 씬 객체와 시간
using UnityEngine.EventSystems; // 안전한 기본 선택
using MoveDirection = ProjectDelta.Core.MoveDirection; // 던전 이동 방향 구분
using UnityEngine.InputSystem; // 시험 키보드
using UnityEngine.InputSystem.LowLevel; // 실제 이동 입력
using UnityEngine.SceneManagement; // 실행 재시작
using UnityEngine.TestTools; // 프레임 기반 검증
using UnityEngine.Tilemaps; // 방 타일 접근
using UnityEngine.UI; // 실제 행동 버튼
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TrialEventBattlePlayModeTests // 실제 이벤트 전투와 저장 검증
    { // 검증 클래스 시작
        private Keyboard keyboard; // 독립 시험 키보드
        private InputSettings.BackgroundBehavior originalBackground; // 기존 입력 포커스 설정
#if UNITY_EDITOR // 편집기 입력 설정 조건
        private InputSettings.EditorInputBehaviorInPlayMode originalEditor; // 기존 편집기 입력 설정
#endif // 편집기 입력 설정 끝
        [SetUp] // 독립 전투 시험 준비
        public void Setup() // 저장과 입력 격리
        { // 준비 시작
            RunStorage.TestDirectoryOverride = System.IO.Path.Combine(Application.dataPath, "..", "Artifacts", "TrialEventBattlePlayMode", System.Guid.NewGuid().ToString("N")); // 고유 시험 저장 경로
            ObservationStorage.TestDirectoryOverride = RunStorage.TestDirectoryOverride; // 독립 영구 저장 경로
            RoomTravelController.RoomContentTestOverride = false; // 다른 방 콘텐츠의 시험 간섭 차단
            RoomTravelController.TutorialEncounterTestOverride = true; // 첫 슬라임 조우 활성화
            RoomTravelController.GenerationSeedOverride = 0; // 결정적인 첫 층
            AppRoot.AutomaticEncounterTestOverride = true; // 실제 자동 전투 진입
            AppRoot.TrialEventBattleRollOverride = (minimum, maximum) => minimum; // 동점 주도권 유지
            originalBackground = InputSystem.settings.backgroundBehavior; // 기존 포커스 설정 보관
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 시험 입력 허용
#if UNITY_EDITOR // 편집기 입력 조건
            originalEditor = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 입력 보관
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 편집기 시험 입력 허용
#endif // 편집기 입력 조건 끝
            keyboard = InputSystem.AddDevice<Keyboard>(); // 독립 이동 키보드
        } // 준비 끝
        [UnityTearDown] // 실제 게임 정리
        public IEnumerator Cleanup() // 시험 설정 복구
        { // 정리 시작
            RoomTravelController.RoomContentTestOverride = null; // 방 콘텐츠 설정 복구
            CombatScreenView.RandomOverride = null; // 전투 난수 설정 복구
            RoomTravelController.TutorialEncounterTestOverride = null; // 조우 설정 복구
            RoomTravelController.GenerationSeedOverride = null; // 던전 씨드 복구
            AppRoot.AutomaticEncounterTestOverride = null; // 자동 전투 설정 복구
            AppRoot.TrialEventBattleRollOverride = null; // 시험 주사위 복구
            if (keyboard != null && keyboard.added) // 시험 키보드 검사
            { // 입력 정리 시작
                InputSystem.RemoveDevice(keyboard); // 독립 키보드 제거
            } // 입력 정리 끝
            if (AppRoot.Instance != null) // 실제 회차 관리자 검사
            { // 게임 정리 시작
                Object.Destroy(AppRoot.Instance.gameObject); // 독립 회차 객체 제거
            } // 게임 정리 끝
            yield return null; // 객체 정리 대기
            RunStorage.TestDirectoryOverride = null; // 저장 경로 복구
            ObservationStorage.TestDirectoryOverride = null; // 영구 저장 경로 복구
            InputSystem.settings.backgroundBehavior = originalBackground; // 기존 포커스 복구
#if UNITY_EDITOR // 편집기 입력 조건
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditor; // 기존 편집기 입력 복구
#endif // 편집기 입력 조건 끝
        } // 정리 끝
        private IEnumerator EnterEventBattle() // 실제 첫 슬라임 전투 진입
        { // 진입 시작
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 시작 씬 로드
            yield return WaitForPhase(GamePhase.Title); // 타이틀 대기
            AppRoot.Instance.StartNewGame(); // 새 회차 시작
            yield return WaitForPhase(GamePhase.Exploration); // 실제 던전 대기
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 격자 플레이어
            var first = travel.Layout.Doors.First(door => door.RoomX == 0 && door.RoomY == 0 && !door.IsSecret); // 첫 일반 방 연결
            yield return UseDoor(travel, player, first); // 첫 슬라임 방 이동
            yield return WaitForObject("TemptBattleButton"); // 자동 전투 버튼 대기
            while (GameObject.Find("EncounterTransitionCanvas") != null) // 조우 연출 완료 검사
            { // 연출 대기 시작
                yield return null; // 다음 프레임 대기
            } // 연출 대기 끝
            Assert.That(travel.CurrentMonsterId, Is.EqualTo("MON_SLIME")); // 실제 첫 슬라임 확인
            yield return null; // 전환 완료 프레임 대기
            GameObject.Find("TemptBattleButton").GetComponent<Button>().onClick.Invoke(); // 실제 이벤트 전투 진입
            yield return WaitForObject("TrialEventBattleAction_3"); // 실제 행동 화면 대기
        } // 진입 끝
        [UnityTest] // 행동 저장과 재실행 및 승리 검증
        public IEnumerator EventBattle_ActionResumesAndVictoryPaysOnce() // 저장된 행동과 경험치 복원
        { // 검증 시작
            yield return EnterEventBattle(); // 실제 슬라임 전투 진입
            var app = AppRoot.Instance; // 실제 회차 관리자
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 실제 화면 캡처 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) // 그래픽 캡처 가능 검사
            { // 캡처 시작
                yield return null; // 전투 화면 갱신 대기
                var image = CaptureScreen("TrialEventBattleCanvas"); // 실제 전투 화면 렌더링
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 실제 화면 이미지 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 끝
            GameObject.Find("TrialEventBattleAction_3").GetComponent<Button>().onClick.Invoke(); // 시험 행동 선택
            yield return null; // 행동과 저장 반영
            var before = app.CurrentTrialEventBattle; // 실제 행동 이후 상태
            Assert.That(before.steps.Length, Is.EqualTo(1)); // 단일 행동 기록
            Assert.That(before.enemies[0].progress, Is.EqualTo(30)); // 실제 시험 진행량
            Assert.That(before.turn, Is.EqualTo(-1)); // 동점의 기존 주도권
            var saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 실제 저장 파일 조회
            Assert.That(saved.trialEventBattle.steps.Length, Is.EqualTo(1)); // 행동 저장 확인
            Object.Destroy(app.gameObject); // 실행 중단 모사
            yield return null; // 기존 게임 정리
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 새 실행 시작
            yield return WaitForPhase(GamePhase.Title); // 타이틀 대기
            app = AppRoot.Instance; // 새 실행 관리자
            Assert.That(app.ContinueRun(), Is.True); // 저장 회차 재개
            yield return WaitForObject("TrialEventBattleAction_3"); // 저장된 전투 화면 복원
            var resumed = app.CurrentTrialEventBattle; // 복원된 실제 상태
            Assert.That(resumed.steps.Length, Is.EqualTo(before.steps.Length)); // 행동 중복 없는 복원
            Assert.That(resumed.stamina, Is.EqualTo(before.stamina)); // 저장 정력 유지
            Assert.That(resumed.turn, Is.EqualTo(before.turn)); // 저장 주도권 유지
            Assert.That(resumed.enemies[0].progress, Is.EqualTo(before.enemies[0].progress)); // 진행 게이지 유지
            for (var index = 0; index < 3; index++) // 남은 시험 행동 순회
            { // 행동 반복 시작
                GameObject.Find("TrialEventBattleAction_3").GetComponent<Button>().onClick.Invoke(); // 완료까지 실제 행동
                yield return null; // 각 행동 저장 반영
            } // 행동 반복 끝
            Assert.That(app.CurrentTrialEventBattle.outcome, Is.EqualTo(1)); // 만족 승리 확인
            Assert.That(app.CurrentTrialEventBattle.experience, Is.EqualTo(48)); // 첫 층 경험치 팔십 퍼센트
            GameObject.Find("TrialEventBattleConfirm").GetComponent<Button>().onClick.Invoke(); // 실제 결과 확정
            yield return WaitForPhase(GamePhase.Exploration); // 탐험 복귀 대기
            Assert.That(app.Experience, Is.EqualTo(48)); // 승리 경험치 단일 지급
            saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 완료 저장 조회
            Assert.That(saved.tutorialCompleted, Is.True); // 첫 조우 완료 저장
            Assert.That(saved.experience, Is.EqualTo(48)); // 지급 결과 저장
            Assert.That(saved.hasTrialEventBattle, Is.False); // 완료 전투 재개 차단
            Assert.That(app.TryFinishEventBattle(out _), Is.False); // 중복 결과 확정 거절
            Assert.That(app.Experience, Is.EqualTo(48)); // 중복 경험치 차단
        } // 검증 끝
        [UnityTest] // 저장 잠금 실패 보존 검증
        public IEnumerator EventBattle_LockedSavePreservesActionState() // 행동 저장 실패의 원본 유지
        { // 검증 시작
            yield return EnterEventBattle(); // 실제 이벤트 전투 진입
            var app = AppRoot.Instance; // 실제 회차 관리자
            var before = app.CurrentTrialEventBattle; // 기존 저장 상태
            using (var gate = new System.IO.FileStream(System.IO.Path.Combine(RunStorage.TestDirectoryOverride, "run.lock"), System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None)) // 실제 저장 잠금 점유
            { // 잠금 시작
                GameObject.Find("TrialEventBattleAction_3").GetComponent<Button>().onClick.Invoke(); // 잠금 중 실제 행동 시도
                yield return null; // 저장 실패 반영
                Assert.That(app.CurrentTrialEventBattle.steps.Length, Is.EqualTo(before.steps.Length)); // 기존 행동 기록 보존
                Assert.That(app.CurrentTrialEventBattle.stamina, Is.EqualTo(before.stamina)); // 기존 정력 보존
                Assert.That(app.CurrentTrialEventBattle.enemies[0].progress, Is.EqualTo(before.enemies[0].progress)); // 기존 진행 보존
                Assert.That(GameObject.Find("TrialEventBattleNotice").GetComponent<Text>().text, Is.Not.Empty); // 저장 실패 안내
            } // 잠금 끝
            GameObject.Find("TrialEventBattleAction_3").GetComponent<Button>().onClick.Invoke(); // 잠금 해제 후 재시도
            yield return null; // 정상 저장 반영
            Assert.That(app.CurrentTrialEventBattle.steps.Length, Is.EqualTo(1)); // 실제 단일 행동 저장
        } // 검증 끝
        [UnityTest] // 시험 중단 확인과 보상 포기 검증
        public IEnumerator EventBattle_RetreatRequiresConfirmationAndForfeitsRewards() // 취소 기본과 이전 방 복귀
        { // 검증 시작
            yield return EnterEventBattle(); // 실제 이벤트 전투 진입
            var app = AppRoot.Instance; // 실제 회차 관리자
            var previous = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 최초 조우 저장 위치
            Assert.That(previous.hasPreviousRoom, Is.True); // 이전 방 존재 확인
            var previousX = previous.previousRoomX; // 실제 이전 방 가로 좌표
            var previousY = previous.previousRoomY; // 실제 이전 방 세로 좌표
            GameObject.Find("TrialEventBattleRetreat").GetComponent<Button>().onClick.Invoke(); // 중단 확인 열기
            yield return null; // 확인 화면 표시 반영
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("TrialEventBattleCancelRetreat")); // 안전한 기본 취소
            GameObject.Find("TrialEventBattleCancelRetreat").GetComponent<Button>().onClick.Invoke(); // 기본 취소 선택
            Assert.That(app.CurrentTrialEventBattle, Is.Not.Null); // 취소 후 전투 유지
            Assert.That(app.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 취소 후 전투 단계 유지
            GameObject.Find("TrialEventBattleRetreat").GetComponent<Button>().onClick.Invoke(); // 중단 확인 재열기
            GameObject.Find("TrialEventBattleConfirmRetreat").GetComponent<Button>().onClick.Invoke(); // 보상 포기 확정
            yield return WaitForPhase(GamePhase.Exploration); // 이전 방 탐험 복귀
            Assert.That(app.Experience, Is.Zero); // 중단한 전투의 경험치 포기
            Assert.That(app.Gold, Is.Zero); // 중단한 전투의 골드 포기
            var run = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 실제 중단 저장 조회
            Assert.That(run.hasTrialEventBattle, Is.False); // 중단한 전투의 재개 차단
            Assert.That(run.tutorialCompleted, Is.False); // 미완료 조우 보존
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 복귀 방 관리자
            Assert.That(travel.RoomX, Is.EqualTo(previousX)); // 실제 이전 방 가로 좌표
            Assert.That(travel.RoomY, Is.EqualTo(previousY)); // 실제 이전 방 세로 좌표
            Assert.That(run.roomX, Is.EqualTo(previousX)); // 저장된 이전 방 가로 좌표
            Assert.That(run.roomY, Is.EqualTo(previousY)); // 저장된 이전 방 세로 좌표
            Object.Destroy(app.gameObject); // 중단 직후 실행 종료 모사
            yield return null; // 게임 객체 정리 대기
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 재실행
            yield return WaitForPhase(GamePhase.Title); // 타이틀 대기
            Assert.That(AppRoot.Instance.ContinueRun(), Is.True); // 중단 저장의 회차 재개
            yield return WaitForPhase(GamePhase.Exploration); // 재개 위치 준비
            travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 복원된 방 관리자
            Assert.That(travel.RoomX, Is.EqualTo(previousX)); // 재개한 실제 이전 방 가로 좌표
            Assert.That(travel.RoomY, Is.EqualTo(previousY)); // 재개한 실제 이전 방 세로 좌표
        } // 검증 끝
        [UnityTest] // 혼합 승리 경험치 검증
        public IEnumerator EventBattle_GroupKillAndSatisfactionAwardMixedExperience() // 일반 처치와 이벤트 승리 합산
        { // 검증 시작
            CombatScreenView.RandomOverride = (minimum, maximum) => minimum; // 확정 명중과 시험 피해
            yield return EnterEventBattle(); // 첫 슬라임 이벤트 전투
            yield return CompleteTrialEventBattle(); // 실제 행동 저장과 첫 조우 승리 대기
            var firstConfirm = GameObject.Find("TrialEventBattleConfirm"); // 실제 첫 전투 결과 버튼
            Assert.That(firstConfirm, Is.Not.Null, BattleDiagnostic()); // 종료 상태와 실제 오류 안내
            firstConfirm.GetComponent<Button>().onClick.Invoke(); // 첫 조우 완료 확정
            yield return WaitForPhase(GamePhase.Exploration); // 탐험 복귀 대기
            var app = AppRoot.Instance; // 실제 회차 관리자
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 격자 플레이어
            var groupRoom = travel.GroupEncounterRoom; // 실제 다수 조우 방
            Assert.That(groupRoom, Is.Not.Null); // 다수 조우 배치 확인
            foreach (var door in FindRoute(travel.Layout, travel.Layout.GetRoom(travel.RoomX, travel.RoomY), groupRoom)) // 실제 문 경로 순회
            { // 방 이동 시작
                yield return UseDoor(travel, player, door); // 실제 연결 문 통과
            } // 방 이동 끝
            yield return WaitForObject("GroupTarget1"); // 실제 다수 전투 화면 대기
            while (GameObject.Find("EncounterTransitionCanvas") != null) // 전투 전환 종료 대기
            { // 대기 시작
                yield return null; // 다음 프레임 대기
            } // 대기 끝
            yield return null; // 입력 차단 프레임 종료
            var firstTarget = GameObject.Find("GroupTarget1").GetComponent<Button>(); // 첫 적 선택
            firstTarget.onClick.Invoke(); // 첫 번째 적 지정
            var attack = GameObject.Find("AdvanceBattleButton").GetComponent<Button>(); // 실제 기본 공격
            for (var index = 0; index < 4 && firstTarget.interactable; index++) // 생존 중 실제 공격 반복
            { // 공격 시작
                attack.onClick.Invoke(); // 지정 대상 공격
                yield return null; // 라운드와 생존 갱신 대기
            } // 공격 끝
            Assert.That(firstTarget.interactable, Is.False); // 첫 번째 적 실제 처치
            Assert.That(GameObject.Find("GroupTarget2").GetComponent<Button>().interactable, Is.True); // 다른 적 생존 확인
            GameObject.Find("TemptBattleButton").GetComponent<Button>().onClick.Invoke(); // 일반 전투에서 이벤트 전투 전환
            yield return WaitForObject("TrialEventBattleAction_3"); // 이벤트 전투 화면 대기
            var battle = app.CurrentTrialEventBattle; // 실제 전환 상태
            Assert.That(battle.enemies.Length, Is.EqualTo(1)); // 남은 적만 참여
            Assert.That(battle.enemies[0].slot, Is.EqualTo(1)); // 기존 두 번째 적 슬롯 유지
            Assert.That(battle.normalExperience, Is.EqualTo(60)); // 생존 적의 기본 경험치
            Assert.That(battle.priorExperience, Is.EqualTo(60)); // 일반 처치 적의 기본 경험치
            Assert.That(ExperienceRules.TryAdvance(app.Level, app.Experience, 108, out var expected), Is.True); // 이전 성장과 혼합 경험치 계산
            yield return CompleteTrialEventBattle(); // 생존 적의 저장된 행동과 승리 대기
            Assert.That(app.CurrentTrialEventBattle.outcome, Is.EqualTo(1)); // 모든 적 제거 승리
            Assert.That(app.CurrentTrialEventBattle.experience, Is.EqualTo(108)); // 일반 백 퍼센트와 만족 팔십 퍼센트
            GameObject.Find("TrialEventBattleConfirm").GetComponent<Button>().onClick.Invoke(); // 실제 승리 확정
            yield return WaitForPhase(GamePhase.Exploration); // 탐험 복귀 대기
            Assert.That(app.Level, Is.EqualTo(expected.Level)); // 혼합 지급 후 레벨
            Assert.That(app.Experience, Is.EqualTo(expected.CurrentExperience)); // 혼합 지급 후 경험치
            var saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 확정 저장 조회
            Assert.That(saved.groupEncounterCompleted, Is.True); // 다수 조우 완료 저장
            Assert.That(saved.level, Is.EqualTo(expected.Level)); // 실제 저장 레벨
            Assert.That(saved.experience, Is.EqualTo(expected.CurrentExperience)); // 실제 저장 경험치
        } // 검증 끝
        [UnityTest] // 실제 관찰 정보와 저장 검증
        public IEnumerator EventBattle_ObserveShowsTargetValuesAndSavesAction() // 체력과 시험 매력 표시 및 행동 기록
        { // 검증 시작
            yield return EnterEventBattle(); // 실제 첫 슬라임 전투 진입
            var app = AppRoot.Instance; // 실제 회차 관리자
            var before = app.CurrentTrialEventBattle; // 관찰 전 참가자 상태
            Assert.That(before, Is.Not.Null, BattleDiagnostic()); // 실제 전투 상태 존재 확인
            Assert.That(before.enemies.Length, Is.EqualTo(1), BattleDiagnostic()); // 첫 슬라임 단일 대상 확인
            var target = before.enemies[0]; // 관찰할 실제 대상 정보
            GameObject.Find("TrialEventBattleAction_0").GetComponent<Button>().onClick.Invoke(); // 실제 관찰 행동 선택
            yield return null; // 행동과 저장 반영
            var current = app.CurrentTrialEventBattle; // 관찰 후 실제 전투 상태
            Assert.That(current.steps.Length, Is.EqualTo(before.steps.Length + 1), BattleDiagnostic()); // 실제 관찰 행동 저장
            Assert.That(current.steps[current.steps.Length - 1].action, Is.Zero, BattleDiagnostic()); // 관찰 행동 번호 확인
            Assert.That(current.steps[current.steps.Length - 1].target, Is.Zero, BattleDiagnostic()); // 실제 대상 기록 확인
            Assert.That(current.enemies[0].health, Is.EqualTo(target.health), BattleDiagnostic()); // 관찰 전후 실제 체력 유지
            Assert.That(current.stamina, Is.EqualTo(before.stamina), BattleDiagnostic()); // 비용 없는 관찰 확인
            var notice = GameObject.Find("TrialEventBattleNotice").GetComponent<Text>().text; // 실제 관찰 결과 안내
            Assert.That(notice, Does.Contain("관찰")); // 실제 관찰 안내 확인
            Assert.That(notice, Does.Contain("체력 " + target.health)); // 대상의 실제 체력 표시
            Assert.That(notice, Does.Contain("시험 매력 " + target.charm)); // 대상의 시험 매력 표시
            var run = new RunStorage(RunStorage.TestDirectoryOverride).Load(out var warning); // 실제 회차 파일 재조회
            Assert.That(warning, Is.Empty); // 정상 저장 조회 확인
            Assert.That(run.hasTrialEventBattle, Is.True); // 저장된 전투 존재
            Assert.That(run.trialEventBattle.steps.Length, Is.EqualTo(current.steps.Length)); // 저장된 행동 수 확인
            Assert.That(run.trialEventBattle.steps[run.trialEventBattle.steps.Length - 1].action, Is.Zero); // 관찰 행동의 디스크 저장
            Assert.That(run.trialEventBattle.enemies[0].health, Is.EqualTo(target.health)); // 실제 대상 체력 저장
            Assert.That(run.trialEventBattle.enemies[0].charm, Is.EqualTo(target.charm)); // 실제 대상 매력 저장
        } // 검증 끝
        [UnityTest] // 중단 확인 중 상대 자동 행동 정지 검증
        public IEnumerator EventBattle_RetreatDialogPausesEnemyAndCancelResumes() // 확인 중 상태 보존과 취소 후 재개
        { // 검증 시작
            var rolls = 0; // 주도권 주사위 호출 수
            AppRoot.TrialEventBattleRollOverride = (minimum, maximum) => ++rolls % 2 == 1 ? 1 : 20; // 상대 주도권 유지
            yield return EnterEventBattle(); // 실제 첫 슬라임 전투 진입
            var app = AppRoot.Instance; // 실제 회차 관리자
            GameObject.Find("TrialEventBattleAction_0").GetComponent<Button>().onClick.Invoke(); // 상대 주도권으로 전환
            var before = app.CurrentTrialEventBattle; // 플레이어 행동 직후 저장 상태
            Assert.That(before, Is.Not.Null, BattleDiagnostic()); // 실제 이벤트 전투 상태 확인
            Assert.That(before.turn, Is.GreaterThanOrEqualTo(0), BattleDiagnostic()); // 상대 주도권 전환 확인
            Assert.That(before.steps.Length, Is.EqualTo(1), BattleDiagnostic()); // 첫 행동만 저장 확인
            GameObject.Find("TrialEventBattleRetreat").GetComponent<Button>().onClick.Invoke(); // 같은 프레임에 중단 확인 열기
            Assert.That(GameObject.Find("TrialEventBattleRetreatDialog"), Is.Not.Null); // 실제 확인창 활성 상태
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("TrialEventBattleCancelRetreat")); // 기본 취소 선택
            for (var index = 0; index < 6; index++) // 여러 프레임 상태 보존 검사
            { // 확인 대기 시작
                yield return null; // 상대 자동 행동의 실행 기회
                var held = app.CurrentTrialEventBattle; // 확인 중 현재 상태
                Assert.That(held, Is.Not.Null, BattleDiagnostic()); // 전투 상태 보존 확인
                Assert.That(held.steps.Length, Is.EqualTo(before.steps.Length), BattleDiagnostic()); // 확인 중 상대 행동 정지
                Assert.That(held.stamina, Is.EqualTo(before.stamina), BattleDiagnostic()); // 확인 중 정력 보존
                Assert.That(held.turn, Is.EqualTo(before.turn), BattleDiagnostic()); // 확인 중 주도권 보존
            } // 확인 대기 끝
            GameObject.Find("TrialEventBattleCancelRetreat").GetComponent<Button>().onClick.Invoke(); // 중단 확인 취소
            var deadline = Time.realtimeSinceStartup + 10f; // 적 자동 행동 재개 대기 제한
            while (app.CurrentTrialEventBattle != null && app.CurrentTrialEventBattle.steps.Length == before.steps.Length && Time.realtimeSinceStartup < deadline) // 실제 다음 행동 대기
            { // 재개 대기 시작
                yield return null; // 다음 프레임의 자동 행동 대기
            } // 재개 대기 끝
            var resumed = app.CurrentTrialEventBattle; // 취소 후 저장 상태
            Assert.That(resumed, Is.Not.Null, BattleDiagnostic()); // 재개된 전투 상태 확인
            Assert.That(resumed.steps.Length, Is.GreaterThan(before.steps.Length), BattleDiagnostic()); // 상대 자동 행동 재개 확인
            Assert.That(resumed.stamina, Is.LessThan(before.stamina), BattleDiagnostic()); // 실제 상대 행동의 정력 변화 확인
        } // 검증 끝
        [UnityTest] // 정력 소진 패배와 회차 종료 검증
        public IEnumerator EventBattle_ZeroStaminaRecordsDefeatAndEndsRun() // 실제 패배 기록과 회차 종료
        { // 검증 시작
            var rolls = 0; // 주도권 주사위 횟수
            AppRoot.TrialEventBattleRollOverride = (minimum, maximum) => ++rolls % 2 == 1 ? 1 : 20; // 상대 주도권 고정
            yield return EnterEventBattle(); // 실제 첫 슬라임 전투 진입
            var app = AppRoot.Instance; // 실제 회차 관리자
            var runId = app.CurrentRunId; // 패배할 회차 식별자
            GameObject.Find("TrialEventBattleAction_0").GetComponent<Button>().onClick.Invoke(); // 상대 주도권으로 전환
            var deadline = Time.realtimeSinceStartup + 30f; // 자동 행동과 저장 대기 제한
            while (app.CurrentTrialEventBattle != null && app.CurrentTrialEventBattle.outcome == 0 && Time.realtimeSinceStartup < deadline) // 실제 정력 소진 대기
            { // 자동 행동 대기 시작
                yield return null; // 적 행동별 저장 대기
            } // 자동 행동 대기 끝
            Assert.That(app.CurrentTrialEventBattle, Is.Not.Null); // 실제 전투 상태 존재
            Assert.That(app.CurrentTrialEventBattle.outcome, Is.EqualTo(2)); // 정력 소진 패배 확인
            Assert.That(app.CurrentTrialEventBattle.stamina, Is.Zero); // 실제 정력 소진
            GameObject.Find("TrialEventBattleConfirm").GetComponent<Button>().onClick.Invoke(); // 실제 패배 기록 확정
            yield return WaitForPhase(GamePhase.Title); // 회차 종료와 타이틀 대기
            var history = new DefeatHistoryStorage(ObservationStorage.ResolveDirectory(), id => id == "MON_SLIME"); // 실제 슬라임 기록 저장소
            var records = history.Load(out var warning); // 저장된 영구 패배 기록
            Assert.That(warning, Is.Empty); // 패배 기록 정상 조회
            Assert.That(records.Any(entry => entry.TransactionId == runId && entry.TargetId == "MON_SLIME" && entry.Reason == BattleDefeatReason.StaminaZero), Is.True); // 회차와 대상 및 정력 소진 원인 보존
            var run = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 종료 후 회차 저장 조회
            Assert.That(run == null || run.runId != runId, Is.True); // 패배 회차 재개 차단
        } // 검증 끝
        private static string BattleDiagnostic() // 실제 전투 상태와 오류 안내
        { // 진단 시작
            var app = AppRoot.Instance; // 현재 회차 관리자
            var battle = app == null ? null : app.CurrentTrialEventBattle; // 현재 저장된 전투
            var noticeObject = GameObject.Find("TrialEventBattleNotice"); // 실제 전투 안내 객체
            var notice = noticeObject == null ? "안내 없음" : noticeObject.GetComponent<Text>().text; // 표시된 오류 또는 결과
            if (battle == null || battle.enemies == null || battle.enemies.Length == 0 || battle.steps == null) // 전투 상태 누락 검사
            { // 누락 시작
                return "전투 상태 없음 · 안내: " + notice; // 누락 원인 안내
            } // 누락 끝
            return "결과=" + battle.outcome + ", 주도권=" + battle.turn + ", 행동=" + battle.steps.Length + ", 정력=" + battle.stamina + ", 진행=" + battle.enemies[0].progress + " · 안내: " + notice; // 실제 저장 상태와 화면 안내
        } // 진단 끝
        private static IEnumerator CompleteTrialEventBattle() // 저장된 승리까지 실제 플레이어 행동
        { // 행동 시작
            var deadline = Time.realtimeSinceStartup + 20f; // 전체 행동 대기 제한
            while (Time.realtimeSinceStartup < deadline) // 실제 승리까지 진행
            { // 행동 검사 시작
                var app = AppRoot.Instance; // 현재 회차 관리자
                var battle = app == null ? null : app.CurrentTrialEventBattle; // 실제 전투 상태
                Assert.That(battle, Is.Not.Null, BattleDiagnostic()); // 전투 상태 존재 확인
                if (battle.outcome != 0) // 실제 전투 종료 검사
                { // 종료 시작
                    Assert.That(battle.outcome, Is.EqualTo(1), BattleDiagnostic()); // 패배가 아닌 실제 승리
                    yield return WaitForObject("TrialEventBattleConfirm"); // 결과 버튼 활성화 대기
                    yield break; // 완료된 행동 종료
                } // 종료 끝
                if (battle.turn != -1) // 상대 주도권 검사
                { // 상대 행동 대기 시작
                    yield return null; // 자동 행동과 저장 대기
                    continue; // 실제 다음 주도권 재검사
                } // 상대 행동 대기 끝
                var actionObject = GameObject.Find("TrialEventBattleAction_3"); // 현재 시험 행동 버튼
                Assert.That(actionObject, Is.Not.Null, BattleDiagnostic()); // 실제 행동 화면 존재 확인
                var action = actionObject.GetComponent<Button>(); // 실제 시험 버튼
                Assert.That(action, Is.Not.Null, BattleDiagnostic()); // 실제 행동 버튼 확인
                Assert.That(action.interactable, Is.True, BattleDiagnostic()); // 플레이어 행동 가능 여부와 안내
                var priorSteps = battle.steps.Length; // 직전 저장된 행동 수
                action.onClick.Invoke(); // 실제 행동 선택
                yield return null; // 실제 행동과 저장 반영
                var after = app.CurrentTrialEventBattle; // 행동 후 저장 상태
                Assert.That(after, Is.Not.Null, BattleDiagnostic()); // 저장 후 전투 상태 확인
                Assert.That(after.steps.Length, Is.GreaterThan(priorSteps), BattleDiagnostic()); // 행동 저장 실패를 즉시 표시
            } // 행동 검사 끝
            Assert.Fail("이벤트 전투 승리 대기 시간 초과 · " + BattleDiagnostic()); // 실제 상태를 포함한 시간 초과
        } // 행동 끝
        private static List<RoomDoor> FindRoute(DungeonLayout layout, DungeonRoom start, DungeonRoom target) // 실제 출구 연결을 이용한 방 경로
        { // 탐색 시작
            var queue = new Queue<DungeonRoom>(); // 방 탐색 대기열
            var previous = new Dictionary<DungeonRoom, RoomDoor>(); // 방별 이전 출구
            queue.Enqueue(start); // 시작 방 예약
            previous.Add(start, null); // 시작 방 방문 기록
            while (queue.Count > 0 && !previous.ContainsKey(target)) // 목적 방까지 탐색
            { // 탐색 반복 시작
                var room = queue.Dequeue(); // 다음 탐색 방
                foreach (var door in layout.Doors) // 실제 출구 순회
                { // 출구 검사 시작
                    if (door.RoomX != room.X || door.RoomY != room.Y || door.IsSecret) // 현재 방과 일반 출구 검사
                    { // 다른 출구 시작
                        continue; // 비밀 통로와 다른 방 제외
                    } // 다른 출구 끝
                    var next = layout.GetRoom(door.TargetRoomX, door.TargetRoomY); // 실제 연결 방
                    if (previous.ContainsKey(next)) // 이미 방문한 방 검사
                    { // 중복 방문 시작
                        continue; // 이전 경로 보존
                    } // 중복 방문 끝
                    previous.Add(next, door); // 이전 출구 보관
                    queue.Enqueue(next); // 다음 방 탐색 예약
                } // 출구 검사 끝
            } // 탐색 반복 끝
            Assert.That(previous.ContainsKey(target), Is.True); // 실제 도달 가능한 이벤트 방 확인
            var route = new List<RoomDoor>(); // 역순 출구 목록
            for (var room = target; room != start; room = layout.GetRoom(previous[room].RoomX, previous[room].RoomY)) // 목적 방부터 시작 방까지 역추적
            { // 경로 저장 시작
                route.Add(previous[room]); // 실제 연결 출구 보관
            } // 경로 저장 끝
            route.Reverse(); // 출발 순서로 경로 정렬
            return route; // 실제 이동 경로 전달
        } // 탐색 끝
        private IEnumerator UseDoor(RoomTravelController travel, GridMoveController player, RoomDoor door) // 실제 이동과 상호작용 키로 출구 통과
        { // 이동 시작
            Assert.That(door, Is.Not.Null); // 실제 출구 확인
            var inputDeadline = Time.realtimeSinceStartup + 10f; // 함정과 전환 입력 대기 제한
            while (!AppRoot.Instance.Input.IsExplorationEnabled && Time.realtimeSinceStartup < inputDeadline) // 경유 방의 함정 처리와 입력 복구 검사
            { // 입력 대기 시작
                yield return null; // 다음 프레임의 입력 복구 대기
            } // 입력 대기 끝
            Assert.That(AppRoot.Instance.Input.IsExplorationEnabled, Is.True); // 문 이동 전 탐험 입력 확인
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 방 씬 객체 조회
            var root = (GameObject)finder.Invoke(travel, new object[] { travel.RoomX, travel.RoomY }); // 현재 방 씬 객체
            var floor = root.transform.Find("Floor").GetComponent<Tilemap>(); // 현재 방 바닥
            var obstacles = root.transform.Find("Obstacles").GetComponent<Tilemap>(); // 현재 방 장애물
            var delta = Offset(door.Direction); // 문 방향의 격자 한 칸
            Assert.That(player.ChangeRoom(floor, obstacles, door.ApproachX - delta.x, door.ApproachY - delta.y), Is.True); // 문 접근 직전 칸 배치
            yield return null; // 위치 변경 프레임 종료
            var key = DirectionKey(door.Direction); // 실제 문 방향 입력
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); // 방향 키 누름
            InputSystem.Update(); // 실제 이동 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
            InputSystem.Update(); // 키 해제 반영
            var deadline = Time.realtimeSinceStartup + 10f; // 이동 완료 대기 제한
            while (player.IsMoving && Time.realtimeSinceStartup < deadline) // 실제 칸 이동 대기
            { // 이동 대기 시작
                yield return null; // 다음 프레임 대기
            } // 이동 대기 끝
            Assert.That(player.IsMoving, Is.False); // 실제 이동 완료 확인
            Assert.That(player.Facing, Is.EqualTo(door.Direction)); // 실제 문 방향 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 상호작용 키 누름
            InputSystem.Update(); // 실제 문 이동 요청
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 상호작용 키 해제
            InputSystem.Update(); // 키 해제 반영
            yield return null; // 방 전환 반영 대기
            Assert.That(travel.RoomX, Is.EqualTo(door.TargetRoomX)); // 도착 방 가로 좌표
            Assert.That(travel.RoomY, Is.EqualTo(door.TargetRoomY)); // 도착 방 세로 좌표
            deadline = Time.realtimeSinceStartup + 10f; // 화면 전환 완료 대기 제한
            while (player.IsRoomTransitionLocked && Time.realtimeSinceStartup < deadline) // 방 전환 연출 대기
            { // 전환 대기 시작
                yield return null; // 다음 프레임 대기
            } // 전환 대기 끝
            Assert.That(player.IsRoomTransitionLocked, Is.False); // 실제 방 전환 완료 확인
        } // 이동 끝
        private static Key DirectionKey(MoveDirection direction) // 출구 방향의 실제 키
        { // 키 조회 시작
            switch (direction) // 방향 선택
            { // 선택 시작
                case MoveDirection.Right: return Key.D; // 오른쪽 이동
                case MoveDirection.Left: return Key.A; // 왼쪽 이동
                case MoveDirection.Up: return Key.W; // 위쪽 이동
                default: return Key.S; // 아래쪽 이동
            } // 선택 끝
        } // 키 조회 끝
        private static Vector3Int Offset(MoveDirection direction) // 출구 방향의 실제 칸 차이
        { // 위치 조회 시작
            switch (direction) // 방향 선택
            { // 선택 시작
                case MoveDirection.Right: return Vector3Int.right; // 오른쪽 칸
                case MoveDirection.Left: return Vector3Int.left; // 왼쪽 칸
                case MoveDirection.Up: return Vector3Int.up; // 위쪽 칸
                default: return Vector3Int.down; // 아래쪽 칸
            } // 선택 끝
        } // 위치 조회 끝
        private static IEnumerator WaitForObject(string name) // 실제 화면 객체 표시 대기
        { // 대기 시작
            var deadline = Time.realtimeSinceStartup + 20f; // 화면 대기 시간 제한
            while (Time.realtimeSinceStartup < deadline && GameObject.Find(name) == null) // 실제 화면 표시 검사
            { // 화면 대기 시작
                yield return null; // 다음 프레임 대기
            } // 화면 대기 끝
            Assert.That(GameObject.Find(name), Is.Not.Null, name + " 화면 표시 시간 초과"); // 실제 화면 확인
        } // 대기 끝
        private static IEnumerator WaitForPhase(GamePhase phase) // 실제 게임 단계 전환 대기
        { // 대기 시작
            var deadline = Time.realtimeSinceStartup + 20f; // 단계 대기 시간 제한
            while (Time.realtimeSinceStartup < deadline && (AppRoot.Instance == null || AppRoot.Instance.Flow.Phase != phase)) // 실제 단계 전환 검사
            { // 단계 대기 시작
                yield return null; // 다음 프레임 대기
            } // 단계 대기 끝
            Assert.That(AppRoot.Instance, Is.Not.Null); // 실제 게임 관리자 확인
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(phase)); // 요청된 단계 확인
            yield return null; // 씬 초기화 프레임 대기
        } // 대기 끝
        private static Texture2D CaptureScreen(string canvasName) // 실제 이벤트 화면의 독립 렌더링
        { // 캡처 시작
            var camera = Camera.main; // 실제 던전 카메라
            var canvas = GameObject.Find(canvasName).GetComponent<Canvas>(); // 실제 이벤트 캔버스
            var dungeonObject = GameObject.Find("DungeonCanvas"); // 기존 탐험 화면 객체
            var dungeon = dungeonObject == null ? null : dungeonObject.GetComponent<Canvas>(); // 선택적인 탐험 캔버스
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); // 화면 밖 캡처 영역
            var oldActive = RenderTexture.active; // 기존 활성 렌더링 대상
            var oldTarget = camera.targetTexture; // 기존 카메라 렌더링 대상
            var oldMode = canvas.renderMode; // 기존 캔버스 렌더링 방식
            var oldCamera = canvas.worldCamera; // 기존 캔버스 카메라
            var oldDistance = canvas.planeDistance; // 기존 캔버스 거리
            var oldSorting = canvas.overrideSorting; // 기존 캔버스 정렬 방식
            var oldOrder = canvas.sortingOrder; // 기존 캔버스 정렬 순서
            var oldPosition = camera.transform.position; // 기존 카메라 위치
            var oldEnabled = dungeon != null && dungeon.enabled; // 기존 탐험 캔버스 표시 상태
            try // 실제 이벤트 화면 렌더링
            { // 렌더링 시작
                target.Create(); // 독립 렌더링 영역 생성
                camera.targetTexture = target; // 카메라의 캡처 영역 연결
                var motion = camera.GetComponent<DungeonCameraMotion>(); // 실제 던전 카메라 보정
                if (motion != null) // 카메라 보정 존재 검사
                { // 화면 비율 보정 시작
                    motion.SendMessage("LateUpdate"); // 캡처 비율의 방 범위 재계산
                } // 화면 비율 보정 끝
                if (dungeon != null && dungeon != canvas) // 별도 탐험 화면 검사
                { // 탐험 화면 숨김 시작
                    dungeon.enabled = false; // 이벤트 뒤 탐험 화면 가림
                } // 탐험 화면 숨김 끝
                canvas.renderMode = RenderMode.ScreenSpaceCamera; // 캡처 카메라 기준 이벤트 화면
                canvas.worldCamera = camera; // 실제 카메라 연결
                canvas.planeDistance = 1f; // 화면과 카메라 거리
                canvas.overrideSorting = true; // 이벤트 화면 우선 표시
                canvas.sortingOrder = 100; // 던전 타일 위 정렬
                Canvas.ForceUpdateCanvases(); // 실제 UI 배치 갱신
                camera.Render(); // 이벤트 선택 화면 렌더링
                RenderTexture.active = target; // 픽셀을 읽을 영역 선택
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false); // 실제 화면 이미지 생성
                image.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0); // 렌더링된 픽셀 읽기
                image.Apply(); // 이미지 픽셀 적용
                return image; // 실제 화면 이미지 반환
            } // 렌더링 끝
            finally // 원래 화면과 메모리 복구
            { // 복구 시작
                canvas.renderMode = oldMode; // 원래 캔버스 방식 복구
                canvas.worldCamera = oldCamera; // 원래 캔버스 카메라 복구
                canvas.planeDistance = oldDistance; // 원래 캔버스 거리 복구
                canvas.overrideSorting = oldSorting; // 원래 정렬 방식 복구
                canvas.sortingOrder = oldOrder; // 원래 정렬 순서 복구
                camera.transform.position = oldPosition; // 원래 카메라 위치 복구
                if (dungeon != null) // 탐험 캔버스 존재 검사
                { // 탐험 화면 복구 시작
                    dungeon.enabled = oldEnabled; // 기존 탐험 화면 표시 복구
                } // 탐험 화면 복구 끝
                camera.targetTexture = oldTarget; // 원래 카메라 대상 복구
                RenderTexture.active = oldActive; // 원래 활성 렌더링 대상 복구
                target.Release(); // 임시 렌더링 메모리 해제
                Object.Destroy(target); // 임시 렌더링 객체 제거
            } // 복구 끝
        } // 캡처 끝
    } // 검증 클래스 끝
} // 이름 공간 끝
