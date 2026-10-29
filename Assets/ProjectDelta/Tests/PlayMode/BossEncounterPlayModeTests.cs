using System.Collections; // 프레임 기반 대기
using System.Collections.Generic; // 실제 방 경로 탐색
using System.Linq; // 마왕 방과 계단 조회
using System.Reflection; // 실제 방 타일 조회
using NUnit.Framework; // 실제 게임 검증
using ProjectDelta.Core; // 층과 게임 상태 규칙
using ProjectDelta.Runtime; // 실제 게임 화면과 제어
using UnityEngine; // 게임 객체와 시간
using UnityEngine.InputSystem; // 검증용 키보드
using UnityEngine.InputSystem.LowLevel; // 실제 키 입력 상태
using UnityEngine.SceneManagement; // 실제 게임 재시작
using UnityEngine.TestTools; // 프레임 기반 검증
using UnityEngine.Tilemaps; // 실제 방의 바닥과 장애물
using UnityEngine.UI; // 실제 전투 버튼
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class BossEncounterPlayModeTests // 실제 다섯 층과 마왕 전투 검증
    { // 클래스 시작
        private Keyboard keyboard; // 독립 검증 키보드
        private InputSettings.BackgroundBehavior originalBackground; // 원래 입력 포커스 설정
#if UNITY_EDITOR // 편집기 입력 설정 조건
        private InputSettings.EditorInputBehaviorInPlayMode originalEditor; // 원래 편집기 입력 설정
#endif // 편집기 설정 끝
        [SetUp] // 실제 게임 검증 준비
        public void Setup() // 독립 회차와 결정적 입력 설정
        { // 준비 시작
            RunStorage.TestDirectoryOverride = System.IO.Path.Combine(Application.dataPath, "..", "Artifacts", "BossEncounterPlayModeTests", System.Guid.NewGuid().ToString("N")); // 독립 회차 저장 경로
            ObservationStorage.TestDirectoryOverride = RunStorage.TestDirectoryOverride; // 독립 영구 기록 경로
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            RoomTravelController.RoomContentTestOverride = false; // 별도 이벤트가 없는 마왕 경로
            RoomTravelController.TrialEventTestOverride = false; // 독립 마왕 전투 검증
            RoomTravelController.TutorialEncounterTestOverride = false; // 첫 전투 자동 진입 제외
            AppRoot.AutomaticEncounterTestOverride = true; // 실제 자동 전투 전환
            CombatScreenView.RandomOverride = (minimum, maximum) => minimum; // 재현 가능한 전투 판정
            CombatScreenView.TrialEnemySpeedOverride = 0; // 반복 가능한 플레이어 선공
            originalBackground = InputSystem.settings.backgroundBehavior; // 기존 입력 설정 보관
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 검증용 입력 허용
#if UNITY_EDITOR // 편집기 입력 설정 조건
            originalEditor = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 설정 보관
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 게임 화면에 검증 입력 허용
#endif // 편집기 설정 끝
            keyboard = InputSystem.AddDevice<Keyboard>(); // 실제 이동 입력 장치
        } // 준비 끝
        [UnityTearDown] // 실제 게임 검증 정리
        public IEnumerator Cleanup() // 회차와 입력 설정 복구
        { // 정리 시작
            RoomTravelController.GenerationSeedOverride = null; // 던전 시드 복구
            RoomTravelController.RoomContentTestOverride = null; // 콘텐츠 설정 복구
            RoomTravelController.TrialEventTestOverride = null; // 이벤트 설정 복구
            RoomTravelController.TutorialEncounterTestOverride = null; // 첫 조우 설정 복구
            AppRoot.AutomaticEncounterTestOverride = null; // 자동 전투 설정 복구
            CombatScreenView.RandomOverride = null; // 전투 난수 복구
            CombatScreenView.TrialEnemySpeedOverride = null; // 시험 적 속도 복구
            if (keyboard != null && keyboard.added) // 검증용 입력 장치 검사
            { // 입력 정리 시작
                InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
            } // 입력 정리 끝
            if (AppRoot.Instance != null) // 시험 회차 객체 검사
            { // 회차 정리 시작
                Object.Destroy(AppRoot.Instance.gameObject); // 독립 회차 제거
            } // 회차 정리 끝
            yield return null; // 객체 제거 대기
            RunStorage.TestDirectoryOverride = null; // 회차 저장 경로 복구
            ObservationStorage.TestDirectoryOverride = null; // 영구 기록 경로 복구
            InputSystem.settings.backgroundBehavior = originalBackground; // 입력 포커스 복구
#if UNITY_EDITOR // 편집기 입력 설정 조건
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditor; // 기존 편집기 설정 복구
#endif // 편집기 설정 끝
        } // 정리 끝
        [UnityTest] // 실제 마왕 방 선택과 승리 저장 검증
        public IEnumerator Boss_PreparationCancelAndVictoryPersistAcrossRestart() // 실제 마왕 진입과 결과 복원
        { // 검증 시작
            yield return EnterDungeon(); // 실제 첫 층 시작
            var app = AppRoot.Instance; // 현재 회차 관리자
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 층 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            for (var floor = 1; floor < 5; floor++) // 실제 네 개의 계단 이용
            { // 층 진행 시작
                Assert.That(travel.Floor, Is.EqualTo(floor)); // 현재 층 확인
                var stairs = travel.Layout.Rooms.First(room => room.Kind == RoomKind.Stairs); // 실제 계단 방
                var route = FindOpenRoute(travel, stairs); // 잠기지 않은 실제 계단 경로
                foreach (var door in route) // 계단 방까지 실제 이동
                { // 이동 시작
                    yield return UseDoor(travel, player, door); // 실제 방향과 상호작용 입력
                } // 이동 끝
                yield return InteractAtCenter(travel, player); // 실제 계단 상호작용
                yield return null; // 층 전환 처리 대기
                Assert.That(travel.Floor, Is.EqualTo(floor + 1)); // 다음 층 도착 확인
            } // 층 진행 끝
            var bossRoom = travel.Layout.Rooms.Single(room => room.Kind == RoomKind.Boss); // 실제 최종 마왕 방
            var bossRoute = FindOpenRoute(travel, bossRoom); // 마왕 방까지의 실제 경로
            Assert.That(bossRoute.Count, Is.GreaterThan(0)); // 시작 방과 마왕 방 분리
            for (var index = 0; index < bossRoute.Count - 1; index++) // 마왕 방 직전까지 이동
            { // 이동 시작
                yield return UseDoor(travel, player, bossRoute[index]); // 실제 출구 통과
            } // 이동 끝
            var entrance = bossRoute[bossRoute.Count - 1]; // 마왕 방으로 향하는 문
            yield return ApproachDoor(travel, player, entrance); // 문 앞에 실제 방향으로 접근
            var sourceX = travel.RoomX; // 진입 전 실제 방 가로 좌표
            var sourceY = travel.RoomY; // 진입 전 실제 방 세로 좌표
            yield return PressInteraction(); // 마왕 입구 실제 상호작용
            Assert.That(GameObject.Find("BossPreparationScreen"), Is.Not.Null); // 마왕 준비 화면 표시
            Assert.That(travel.RoomX, Is.EqualTo(sourceX)); // 선택 전 기존 방 가로 유지
            Assert.That(travel.RoomY, Is.EqualTo(sourceY)); // 선택 전 기존 방 세로 유지
            GameObject.Find("BossPreparationContinue").GetComponent<Button>().onClick.Invoke(); // 준비 계속 선택
            yield return null; // 준비 화면 종료 대기
            Assert.That(travel.RoomX, Is.EqualTo(sourceX)); // 취소 후 기존 방 가로 유지
            Assert.That(travel.RoomY, Is.EqualTo(sourceY)); // 취소 후 기존 방 세로 유지
            Assert.That(app.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 취소 후 탐험 복귀
            yield return PressInteraction(); // 같은 마왕 입구 재확인
            Assert.That(GameObject.Find("BossPreparationScreen"), Is.Not.Null); // 준비 화면 재표시
            GameObject.Find("BossPreparationEnter").GetComponent<Button>().onClick.Invoke(); // 실제 마왕 방 진입 선택
            yield return WaitForPhase(GamePhase.Combat); // 조우 연출과 전투 진입 대기
            Assert.That(travel.RoomX, Is.EqualTo(bossRoom.X)); // 실제 마왕 방 가로 진입
            Assert.That(travel.RoomY, Is.EqualTo(bossRoom.Y)); // 실제 마왕 방 세로 진입
            Assert.That(GameObject.Find("FleeBattleButton").GetComponent<Button>().interactable, Is.False); // 마왕전 도주 불가
            Assert.That(GameObject.Find("TemptBattleButton").GetComponent<Button>().interactable, Is.False); // 미지원 이벤트 전투 비활성
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 실제 마왕 전투 캡처 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 자동 캡처 요청 검사
            { // 캡처 시작
                var image = CaptureScreen("BattleCanvas"); // 실제 마왕 전투 화면 렌더링
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 실제 플레이 화면 저장
                Object.Destroy(image); // 임시 이미지 제거
            } // 캡처 끝
            var level = app.Level; // 승리 전 레벨
            var experience = app.Experience; // 승리 전 경험치
            var finish = GameObject.Find("ExitBattleButton").GetComponent<Button>(); // 실제 승리 결과 버튼
            using (var gate = new System.IO.FileStream(System.IO.Path.Combine(RunStorage.TestDirectoryOverride, "run.lock"), System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None)) // 승리 전 실제 저장 잠금 점유
            { // 저장 실패 상황 시작
                for (var turn = 0; turn < 30 && GameObject.Find("RoundStatus").GetComponent<Text>().text != "승리"; turn++) // 저장 잠금 중 실제 마왕 공격
                { // 공격 시작
                    GameObject.Find("AdvanceBattleButton").GetComponent<Button>().onClick.Invoke(); // 실제 전투 공격과 자동 결과 저장 시도
                    yield return null; // 다음 행동 프레임 대기
                } // 공격 끝
                Assert.That(GameObject.Find("RoundStatus").GetComponent<Text>().text, Is.EqualTo("승리")); // 저장 실패 전 실제 승리 판정
                Assert.That(app.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 자동 저장 실패 후 전투 결과 유지
                Assert.That(GameObject.Find("BossResultScreen"), Is.Null); // 저장 전 결과 화면 차단
                Assert.That(app.Experience, Is.EqualTo(experience)); // 저장 실패 후 경험치 유지
                Assert.That(new RunStorage(RunStorage.TestDirectoryOverride).Load(out _).bossOutcome, Is.Zero); // 실패 시 기존 회차 결과 유지
            } // 실제 저장 잠금 해제
            yield return null; // 저장 재시도 프레임 대기
            finish.onClick.Invoke(); // 승리 결과 저장 재시도
            yield return null; // 실제 결과 화면 준비
            Assert.That(GameObject.Find("BossResultScreen"), Is.Not.Null); // 저장된 마왕 결과 화면
            Assert.That(app.Level, Is.EqualTo(level)); // 승리 경험치 없는 레벨 유지
            Assert.That(app.Experience, Is.EqualTo(experience)); // 승리 경험치 없음 확인
            var saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out var warning); // 실제 결과 저장 파일 조회
            Assert.That(warning, Is.Empty); // 결과 저장 오류 없음
            Assert.That(saved, Is.Not.Null); // 실제 결과 회차 저장
            Assert.That(saved.floor, Is.EqualTo(5)); // 다섯째 층 결과 저장
            Assert.That(saved.level, Is.EqualTo(level)); // 저장된 레벨 유지
            Assert.That(saved.experience, Is.EqualTo(experience)); // 저장된 경험치 유지
            Object.Destroy(app.gameObject); // 실제 실행 종료
            yield return null; // 공통 객체 정리 대기
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 새 실행 시작
            yield return WaitForPhase(GamePhase.Title); // 타이틀 대기
            app = AppRoot.Instance; // 새 실행 관리자
            Assert.That(app.ContinueRun(), Is.True); // 결과가 있는 회차 재개
            yield return WaitForObject("BossResultScreen"); // 미확인 마왕 결과 복원 대기
            Assert.That(app.Level, Is.EqualTo(level)); // 재실행 후 레벨 유지
            Assert.That(app.Experience, Is.EqualTo(experience)); // 재실행 후 경험치 유지
            Assert.That(GameObject.Find("BossResultTitleButton"), Is.Not.Null); // 결과 화면의 타이틀 선택
        } // 검증 끝
        [UnityTest] // 마왕전 항복 취소와 저장 재시도 검증
        public IEnumerator Boss_SurrenderCancelAndRetryPersistAcrossRestart() // 항복 확인과 저장된 결과 복원
        { // 검증 시작
            yield return EnterBossCombat(false, true); // 준비 화면 ESC와 실제 마왕 전투 진입
            var app = AppRoot.Instance; // 실제 회차 관리자
            GameObject.Find("ExitBattleButton").GetComponent<Button>().onClick.Invoke(); // 실제 항복 확인 열기
            yield return null; // 확인 화면 표시 대기
            Assert.That(GameObject.Find("SurrenderDialog"), Is.Not.Null); // 실제 항복 확인 표시
            GameObject.Find("CancelSurrender").GetComponent<Button>().onClick.Invoke(); // 실제 항복 취소
            yield return null; // 취소 화면 처리 대기
            Assert.That(app.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 취소 후 실제 전투 유지
            Assert.That(GameObject.Find("SurrenderDialog"), Is.Null); // 항복 확인 닫힘
            var before = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 실제 이전 회차 저장
            Assert.That(before.bossOutcome, Is.Zero); // 취소 시 마왕 결과 미확정
            GameObject.Find("ExitBattleButton").GetComponent<Button>().onClick.Invoke(); // 항복 확인 다시 열기
            var confirm = GameObject.Find("ConfirmSurrender").GetComponent<Button>(); // 실제 항복 확정 버튼
            using (var gate = new System.IO.FileStream(System.IO.Path.Combine(RunStorage.TestDirectoryOverride, "run.lock"), System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None)) // 실제 저장 잠금 점유
            { // 저장 실패 상황 시작
                confirm.onClick.Invoke(); // 저장 잠금 중 항복 확정
                yield return null; // 실패 처리 대기
                Assert.That(app.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 실패 후 전투 보존
                Assert.That(GameObject.Find("BossResultScreen"), Is.Null); // 저장 전 결과 화면 차단
                Assert.That(new RunStorage(RunStorage.TestDirectoryOverride).Load(out _).bossOutcome, Is.Zero); // 저장 실패 후 이전 결과 보존
            } // 저장 잠금 해제
            yield return null; // 항복 재시도 프레임 대기
            GameObject.Find("ExitBattleButton").GetComponent<Button>().onClick.Invoke(); // 결과 버튼의 실제 항복 저장 재시도
            yield return WaitForObject("BossResultScreen"); // 저장된 항복 결과 표시
            var saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out var warning); // 실제 항복 결과 조회
            Assert.That(warning, Is.Empty); // 정상 저장 안내 확인
            Assert.That(saved.bossOutcome, Is.EqualTo(3)); // 실제 항복 결과 저장
            Assert.That(GameObject.Find("BossResultHeading").GetComponent<Text>().text, Does.Contain("항복")); // 화면의 항복 결과 확인
            yield return RestartAndCheckBossResult(3, "항복"); // 실제 재실행 후 같은 결과 복원
        } // 검증 끝
        [UnityTest] // 마왕전 체력 소진 패배의 저장 검증
        public IEnumerator Boss_ZeroHealthDefeatPersistsAcrossRestart() // 실제 전투 패배와 결과 복원
        { // 검증 시작
            yield return EnterBossCombat(true, false); // 체력 일의 실제 마왕 전투 진입
            var app = AppRoot.Instance; // 실제 회차 관리자
            var attack = GameObject.Find("AdvanceBattleButton").GetComponent<Button>(); // 실제 전투 공격
            for (var turn = 0; turn < 10 && GameObject.Find("BossResultScreen") == null; turn++) // 실제 체력 소진과 자동 저장까지 진행
            { // 공격 시작
                attack.onClick.Invoke(); // 실제 공격과 마왕의 반격
                yield return null; // 자동 결과 저장과 다음 프레임 대기
            } // 공격 끝
            Assert.That(app.PlayerHealth, Is.Zero); // 실제 플레이어 체력 소진
            Assert.That(GameObject.Find("BossResultScreen"), Is.Not.Null); // 체력 소진 직후 자동 결과 표시
            var saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out var warning); // 실제 패배 결과 조회
            Assert.That(warning, Is.Empty); // 정상 저장 안내 확인
            Assert.That(saved.bossOutcome, Is.EqualTo(2)); // 실제 체력 소진 결과 저장
            Assert.That(GameObject.Find("BossResultHeading").GetComponent<Text>().text, Does.Contain("패배")); // 화면의 패배 결과 확인
            yield return RestartAndCheckBossResult(2, "패배"); // 실제 재실행 후 같은 결과 복원
        } // 검증 끝
        private IEnumerator EnterBossCombat(bool lowHealth, bool checkEscape) // 실제 층 이동과 마왕 전투 진입
        { // 진입 시작
            yield return EnterDungeon(); // 실제 새 회차 시작
            var app = AppRoot.Instance; // 실제 회차 관리자
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 층 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            for (var floor = 1; floor < 5; floor++) // 기존 실제 층 전환 반복
            { // 층 진행 시작
                travel.SendMessage("TryNextFloor"); // 다음 층 생성과 자동 저장
                yield return null; // 층 이동 완료 대기
                Assert.That(travel.Floor, Is.EqualTo(floor + 1)); // 실제 다음 층 도착
            } // 층 진행 끝
            var boss = travel.Layout.Rooms.Single(room => room.Kind == RoomKind.Boss); // 실제 최종 마왕 방
            var route = FindOpenRoute(travel, boss); // 실제 잠기지 않은 진입 경로
            Assert.That(route.Count, Is.GreaterThan(0)); // 출발 방과 최종 방 분리
            for (var index = 0; index < route.Count - 1; index++) // 마왕 방 직전까지 실제 이동
            { // 이동 시작
                yield return UseDoor(travel, player, route[index]); // 실제 문 통과
            } // 이동 끝
            yield return ApproachDoor(travel, player, route[route.Count - 1]); // 실제 마왕 입구 접근
            if (lowHealth) // 체력 소진 시험 검사
            { // 시험 체력 설정 시작
                app.UpdatePlayerHealth(1); // 실제 전투에 전달할 생존 체력
            } // 시험 체력 설정 끝
            yield return PressInteraction(); // 실제 마왕 입구 상호작용
            Assert.That(GameObject.Find("BossPreparationScreen"), Is.Not.Null); // 실제 준비 화면 확인
            if (checkEscape) // 준비 화면 ESC 취소 검사
            { // ESC 검사 시작
                var x = travel.RoomX; // 준비 전 방 가로 좌표
                var y = travel.RoomY; // 준비 전 방 세로 좌표
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 실제 ESC 키 누름
                yield return null; // 화면 취소 입력 처리
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // ESC 키 해제
                yield return null; // 입력 해제 반영
                Assert.That(app.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // ESC 후 탐험 복귀
                Assert.That(travel.RoomX, Is.EqualTo(x)); // ESC 후 기존 방 가로 유지
                Assert.That(travel.RoomY, Is.EqualTo(y)); // ESC 후 기존 방 세로 유지
                yield return PressInteraction(); // 같은 마왕 입구 재확인
                Assert.That(GameObject.Find("BossPreparationScreen"), Is.Not.Null); // 준비 화면 재표시
            } // ESC 검사 끝
            GameObject.Find("BossPreparationEnter").GetComponent<Button>().onClick.Invoke(); // 실제 전투 진입 선택
            yield return WaitForPhase(GamePhase.Combat); // 조우 연출과 실제 전투 대기
        } // 진입 끝
        private static IEnumerator RestartAndCheckBossResult(int outcome, string label) // 실제 앱 재시작 후 결과 확인
        { // 재시작 시작
            Object.Destroy(AppRoot.Instance.gameObject); // 현재 실행 종료
            yield return null; // 공통 객체 제거 대기
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 새 실행 시작
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            Assert.That(AppRoot.Instance.ContinueRun(), Is.True); // 실제 저장 회차 재개
            yield return WaitForObject("BossResultScreen"); // 저장된 결과 화면 복원
            var saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 재실행 후 실제 저장 조회
            Assert.That(saved.bossOutcome, Is.EqualTo(outcome)); // 원래 결과 번호 보존
            Assert.That(GameObject.Find("BossResultHeading").GetComponent<Text>().text, Does.Contain(label)); // 원래 결과 화면 보존
        } // 재시작 끝
        private static List<RoomDoor> FindOpenRoute(RoomTravelController travel, DungeonRoom target) // 잠긴 문을 제외한 실제 방 경로
        { // 탐색 시작
            var start = travel.Layout.GetRoom(travel.RoomX, travel.RoomY); // 현재 출발 방
            var queue = new Queue<DungeonRoom>(); // 방 탐색 대기열
            var previous = new Dictionary<DungeonRoom, RoomDoor>(); // 방문 방의 이전 출구
            queue.Enqueue(start); // 출발 방 예약
            previous.Add(start, null); // 출발 방 방문 표시
            while (queue.Count > 0 && !previous.ContainsKey(target)) // 목적 방까지 탐색
            { // 탐색 시작
                var room = queue.Dequeue(); // 다음 방문 방
                foreach (var door in travel.Layout.Doors) // 실제 출구 순회
                { // 연결 검사 시작
                    if (door.RoomX != room.X || door.RoomY != room.Y || door.IsSecret || travel.IsDoorLocked(door)) // 다른 방과 비밀 및 잠금 문 검사
                    { // 제외 시작
                        continue; // 사용할 수 없는 출구 제외
                    } // 제외 끝
                    var next = travel.Layout.GetRoom(door.TargetRoomX, door.TargetRoomY); // 연결된 실제 방
                    if (previous.ContainsKey(next)) // 이미 방문한 방 검사
                    { // 중복 시작
                        continue; // 중복 방 제외
                    } // 중복 끝
                    previous.Add(next, door); // 실제 이전 출구 보관
                    queue.Enqueue(next); // 연결 방 예약
                } // 연결 검사 끝
            } // 탐색 끝
            Assert.That(previous.ContainsKey(target), Is.True); // 잠기지 않은 목적지 도달 확인
            var route = new List<RoomDoor>(); // 역순 출구 경로
            var cursor = target; // 목적지부터 역추적
            while (cursor != start) // 출발 방까지 순회
            { // 역추적 시작
                var door = previous[cursor]; // 현재 방의 진입 출구
                route.Add(door); // 출구 경로 저장
                cursor = travel.Layout.GetRoom(door.RoomX, door.RoomY); // 이전 방 조회
            } // 역추적 끝
            route.Reverse(); // 출발 방부터 실제 이동 순서
            return route; // 실제 연결 경로 전달
        } // 탐색 끝
        private IEnumerator ApproachDoor(RoomTravelController travel, GridMoveController player, RoomDoor door) // 마왕 문에 실제 입력으로 접근
        { // 접근 시작
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 방 조회
            var root = (GameObject)finder.Invoke(travel, new object[] { travel.RoomX, travel.RoomY }); // 현재 방 객체
            var floor = root.transform.Find("Floor").GetComponent<Tilemap>(); // 실제 바닥
            var obstacles = root.transform.Find("Obstacles").GetComponent<Tilemap>(); // 실제 장애물
            var before = new Vector3Int(door.ApproachX, door.ApproachY, 0) - Offset(door.Direction); // 문 앞 이전 칸
            Assert.That(player.ChangeRoom(floor, obstacles, before.x, before.y), Is.True); // 실제 출구 접근 칸 배치
            yield return null; // 방 위치 갱신 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(DirectionKey(door.Direction))); // 실제 출구 방향 입력
            InputSystem.Update(); // 이동 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이동 입력 해제
            InputSystem.Update(); // 입력 해제 반영
            var deadline = Time.realtimeSinceStartup + 10f; // 실제 이동 제한
            while (player.IsMoving && Time.realtimeSinceStartup < deadline) // 이동 완료 검사
            { // 이동 대기 시작
                yield return null; // 다음 프레임 대기
            } // 이동 대기 끝
            Assert.That(player.IsMoving, Is.False); // 출구 접근 완료
            Assert.That(player.Facing, Is.EqualTo(door.Direction)); // 실제 출구 방향 확인
        } // 접근 끝
        private IEnumerator InteractAtCenter(RoomTravelController travel, GridMoveController player) // 실제 계단 중앙 상호작용
        { // 상호작용 시작
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 현재 방 조회
            var root = (GameObject)finder.Invoke(travel, new object[] { travel.RoomX, travel.RoomY }); // 실제 계단 방
            var floor = root.transform.Find("Floor").GetComponent<Tilemap>(); // 실제 바닥
            var obstacles = root.transform.Find("Obstacles").GetComponent<Tilemap>(); // 실제 장애물
            Assert.That(player.ChangeRoom(floor, obstacles, 0, 0), Is.True); // 계단 중앙에 배치
            yield return null; // 위치 갱신 대기
            yield return PressInteraction(); // 실제 계단 사용
        } // 상호작용 끝
        private IEnumerator PressInteraction() // 실제 상호작용 키 입력
        { // 입력 시작
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 상호작용 키 누름
            InputSystem.Update(); // 실제 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 화면 처리 대기
        } // 입력 끝
        private static IEnumerator WaitForObject(string name) // 결과 화면 생성 대기
        { // 대기 시작
            var deadline = Time.realtimeSinceStartup + 20f; // 화면 생성 제한
            while (GameObject.Find(name) == null && Time.realtimeSinceStartup < deadline) // 실제 화면 검사
            { // 대기 시작
                yield return null; // 다음 프레임 대기
            } // 대기 끝
            Assert.That(GameObject.Find(name), Is.Not.Null); // 실제 결과 화면 확인
        } // 대기 끝
        private IEnumerator UseDoor(RoomTravelController travel, GridMoveController player, RoomDoor door) // 실제 키 입력으로 지정 출구 통과
        { // 출구 이용 시작
            var transitionDeadline = Time.realtimeSinceStartup + 10f; // 이전 화면 전환 대기 제한
            while (player.IsRoomTransitionLocked && Time.realtimeSinceStartup < transitionDeadline) // 이전 전환 완료 대기
            { // 전환 대기 시작
                yield return null; // 다음 프레임 대기
            } // 전환 대기 끝
            Assert.That(player.IsRoomTransitionLocked, Is.False); // 새 문 사용 전 전환 완료 확인
            Assert.That(door, Is.Not.Null); // 실제 연결된 출구 확인
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 현재 방 객체 조회
            var arguments = new object[] // 방 조회 인수
            { // 인수 목록 시작
                travel.RoomX, travel.RoomY // 현재 방 좌표
            }; // 인수 목록 끝
            var root = (GameObject)finder.Invoke(travel, arguments); // 실제 현재 방
            var floor = root.transform.Find("Floor").GetComponent<Tilemap>(); // 현재 바닥 타일맵
            var obstacles = root.transform.Find("Obstacles").GetComponent<Tilemap>(); // 현재 장애물 타일맵
            var offset = Offset(door.Direction); // 출구를 향하는 한 칸
            var before = new Vector3Int(door.ApproachX, door.ApproachY, 0) - offset; // 문 앞에 도달하기 전 칸
            Assert.That(player.ChangeRoom(floor, obstacles, before.x, before.y), Is.True); // 현재 방의 안전한 접근 칸 배치
            yield return null; // 방 변경 프레임 종료 대기
            var key = DirectionKey(door.Direction); // 출구 방향의 실제 키
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); // 방향 키 누름
            InputSystem.Update(); // 이동 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
            InputSystem.Update(); // 입력 해제 반영
            var deadline = Time.realtimeSinceStartup + 10f; // 이동 대기 시간 제한
            while (player.IsMoving && Time.realtimeSinceStartup < deadline) // 이동 완료 대기
            { // 이동 대기 시작
                yield return null; // 다음 프레임 대기
            } // 이동 대기 끝
            Assert.That(player.IsMoving, Is.False); // 실제 이동 완료 확인
            Assert.That(player.Facing, Is.EqualTo(door.Direction)); // 문을 향하는 방향 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 실제 상호작용 키 누름
            InputSystem.Update(); // 문 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 상호작용 키 해제
            InputSystem.Update(); // 입력 해제 반영
            yield return null; // 방 표시 갱신 대기
            Assert.That(travel.RoomX, Is.EqualTo(door.TargetRoomX)); // 실제 도착 방 가로 좌표
            Assert.That(travel.RoomY, Is.EqualTo(door.TargetRoomY)); // 실제 도착 방 세로 좌표
            var completionDeadline = Time.realtimeSinceStartup + 10f; // 현재 화면 전환 대기 제한
            while (player.IsRoomTransitionLocked && Time.realtimeSinceStartup < completionDeadline) // 실제 전환 완료 대기
            { // 전환 완료 대기 시작
                yield return null; // 다음 프레임 대기
            } // 전환 완료 대기 끝
            Assert.That(player.IsRoomTransitionLocked, Is.False); // 문 이동 화면 전환 완료 확인
        } // 출구 이용 끝
        private static Key DirectionKey(MoveDirection direction) // 출구 방향의 키 선택
        { // 키 선택 시작
            switch (direction) // 출구 방향 검사
            { // 방향 선택 시작
                case MoveDirection.Right: // 오른쪽 방향
                    return Key.D; // 오른쪽 이동 키
                case MoveDirection.Left: // 왼쪽 방향
                    return Key.A; // 왼쪽 이동 키
                case MoveDirection.Up: // 위쪽 방향
                    return Key.W; // 위쪽 이동 키
                default: // 아래쪽 방향
                    return Key.S; // 아래쪽 이동 키
            } // 방향 선택 끝
        } // 키 선택 끝
        private static IEnumerator EnterDungeon() // 실제 새 게임 시작
        { // 진입 시작
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 시작 씬 진입
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            AppRoot.Instance.StartNewGame(); // 새 게임 요청
            yield return WaitForPhase(GamePhase.Exploration); // 던전 도착 대기
        } // 진입 끝
        private static IEnumerator WaitForPhase(GamePhase phase) // 실제 게임 상태 대기
        { // 대기 시작
            var deadline = Time.realtimeSinceStartup + 20f; // 최대 대기 시간
            while (Time.realtimeSinceStartup < deadline) // 제한 시간 반복
            { // 반복 시작
                if (AppRoot.Instance != null && AppRoot.Instance.Flow.Phase == phase) // 목표 상태 검사
                { // 도착 조건 시작
                    yield return null; // 씬과 지도 초기화 대기
                    yield break; // 대기 종료
                } // 도착 조건 끝
                yield return null; // 다음 프레임 대기
            } // 반복 끝
            Assert.Fail("Timed out waiting for " + phase); // 전환 지연 표시
        } // 대기 끝
        private static Vector3Int Offset(MoveDirection direction) // 문 방향의 격자 한 칸
        { // 변환 시작
            switch (direction) // 방향 선택
            { // 선택 시작
                case MoveDirection.Right: // 오른쪽 방향
                    return Vector3Int.right; // 오른쪽 한 칸
                case MoveDirection.Left: // 왼쪽 방향
                    return Vector3Int.left; // 왼쪽 한 칸
                case MoveDirection.Up: // 위쪽 방향
                    return Vector3Int.up; // 위쪽 한 칸
                default: // 아래쪽 방향
                    return Vector3Int.down; // 아래쪽 한 칸
            } // 선택 끝
        } // 변환 끝
        private static Texture2D CaptureScreen(string canvasName) // 실제 화면의 별도 렌더링
        { // 캡처 시작
            var camera = Camera.main; // 실제 게임 카메라
            var canvas = GameObject.Find(canvasName).GetComponent<Canvas>(); // 캡처할 실제 화면
            var dungeonObject = GameObject.Find("DungeonCanvas"); // 기존 탐험 화면 객체
            var dungeon = dungeonObject == null ? null : dungeonObject.GetComponent<Canvas>(); // 선택적인 탐험 화면
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); // 화면 밖 렌더링 영역
            var oldActive = RenderTexture.active; // 기존 렌더링 대상
            var oldTarget = camera.targetTexture; // 기존 카메라 대상
            var oldMode = canvas.renderMode; // 기존 화면 렌더링 방식
            var oldCamera = canvas.worldCamera; // 기존 화면 카메라
            var oldDistance = canvas.planeDistance; // 기존 화면 거리
            var oldSorting = canvas.overrideSorting; // 기존 화면 정렬 방식
            var oldOrder = canvas.sortingOrder; // 기존 화면 정렬 순서
            var oldPosition = camera.transform.position; // 캡처 전 카메라 위치
            var oldEnabled = dungeon != null && dungeon.enabled; // 기존 탐험 화면 표시 상태
            try // 실제 화면 렌더링
            { // 렌더링 시작
                target.Create(); // 별도 영역 생성
                camera.targetTexture = target; // 별도 영역 연결
                var motion = camera.GetComponent<DungeonCameraMotion>(); // 던전 카메라 보정 구성
                if (motion != null) // 던전 카메라 존재 검사
                { // 캡처 비율 보정 시작
                    motion.SendMessage("LateUpdate"); // 캡처 비율 기준 방 범위 재계산
                } // 캡처 비율 보정 끝
                if (dungeon != null && dungeon != canvas) // 다른 화면을 캡처하는지 검사
                { // 탐험 화면 가림 시작
                    dungeon.enabled = false; // 다른 화면의 탐험 안내 가림
                } // 탐험 화면 가림 끝
                canvas.renderMode = RenderMode.ScreenSpaceCamera; // 카메라 기준 화면 표시
                canvas.worldCamera = camera; // 실제 게임 카메라 연결
                canvas.planeDistance = 1f; // 전투 화면 거리 지정
                canvas.overrideSorting = true; // 캡처에서 화면 안내 우선 표시
                canvas.sortingOrder = 100; // 던전 타일보다 앞에 안내 표시
                Canvas.ForceUpdateCanvases(); // 화면 구성 갱신
                camera.Render(); // 실제 전투 화면 그리기
                RenderTexture.active = target; // 읽을 화면 선택
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false); // 캡처 이미지 생성
                image.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0); // 화면 픽셀 읽기
                image.Apply(); // 픽셀 반영
                return image; // 실제 전투 화면 전달
            } // 렌더링 끝
            finally // 기존 화면과 리소스 복구
            { // 복구 시작
                canvas.renderMode = oldMode; // 기존 화면 방식 복구
                canvas.worldCamera = oldCamera; // 기존 화면 카메라 복구
                canvas.planeDistance = oldDistance; // 기존 화면 거리 복구
                canvas.overrideSorting = oldSorting; // 기존 화면 정렬 방식 복구
                canvas.sortingOrder = oldOrder; // 기존 화면 정렬 순서 복구
                camera.transform.position = oldPosition; // 캡처 전 카메라 위치 복구
                if (dungeon != null) // 탐험 화면 존재 검사
                { // 탐험 화면 복구 시작
                    dungeon.enabled = oldEnabled; // 탐험 화면 표시 복구
                } // 탐험 화면 복구 끝
                camera.targetTexture = oldTarget; // 기존 카메라 대상 복구
                RenderTexture.active = oldActive; // 기존 활성 대상 복구
                target.Release(); // 임시 렌더링 메모리 해제
                Object.Destroy(target); // 임시 렌더링 객체 제거
            } // 복구 끝
        } // 캡처 끝
    } // 클래스 끝
} // 이름 공간 끝
