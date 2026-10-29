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
    public sealed class EndingFlowPlayModeTests // 실제 다섯 층과 마왕 전투 검증
    { // 클래스 시작
        private Keyboard keyboard; // 독립 검증 키보드
        private InputSettings.BackgroundBehavior originalBackground; // 원래 입력 포커스 설정
#if UNITY_EDITOR // 편집기 입력 설정 조건
        private InputSettings.EditorInputBehaviorInPlayMode originalEditor; // 원래 편집기 입력 설정
#endif // 편집기 설정 끝
        [SetUp] // 실제 게임 검증 준비
        public void Setup() // 독립 회차와 결정적 입력 설정
        { // 준비 시작
            RunStorage.TestDirectoryOverride = System.IO.Path.Combine(Application.dataPath, "..", "Artifacts", "EndingFlowPlayModeTests", System.Guid.NewGuid().ToString("N")); // 독립 회차 저장 경로
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
        [UnityTest] // 마왕 승리 후 직접 결말 선택과 기록 검증
        public IEnumerator Victory_ChooseHeroicEndingAndCloseRun() // 실제 귀환 후보와 영구 기록 확인
        { // 검증 시작
            yield return StartBoss(false); // 실제 다섯 층과 마왕 전투 진입
            yield return WinBoss(); // 실제 마왕 일반 전투 승리
            yield return WaitForObject("EndingScreen"); // 저장된 결말 선택 화면 대기
            GameObject.Find("EndingBranchReturn").GetComponent<Button>().onClick.Invoke(); // 실제 현실 귀환 방향 선택
            Assert.That(GameObject.Find("EndingCandidate_" + EndingRules.ReturnBasic), Is.Not.Null); // 기본 귀환도 동시에 표시
            var heroic = GameObject.Find("EndingCandidate_" + EndingRules.ReturnHeroic); // 실제 영웅 귀환 후보
            Assert.That(heroic, Is.Not.Null); // 일반 승리의 영웅 후보 확인
            var path = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 결말 화면 캡처 경로
            if (!string.IsNullOrEmpty(path) && Application.isBatchMode) // 자동 캡처 요청 검사
            { // 캡처 시작
                var image = CaptureScreen("EndingCanvas"); // 실제 결말 선택 화면 렌더링
                System.IO.File.WriteAllBytes(path, image.EncodeToPNG()); // 최신 플레이 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 끝
            heroic.GetComponent<Button>().onClick.Invoke(); // 실제 영웅 귀환 선택
            yield return WaitForObject("EndingFinishButton"); // 영구 기록과 보상 완료 대기
            var storage = new EndingHistoryStorage(RunStorage.TestDirectoryOverride); // 실제 영구 결말 저장소
            var entries = storage.Load(out var warning); // 실제 영구 결말 읽기
            Assert.That(warning, Is.Empty); // 영구 저장 오류 없음
            Assert.That(entries.Count, Is.EqualTo(1)); // 단일 결말 거래 기록
            Assert.That(entries[0].EndingId, Is.EqualTo(EndingRules.ReturnHeroic)); // 실제 선택 결말
            Assert.That(entries[0].GrantedShards, Is.EqualTo(10)); // 최초 영웅 결말 파편
            Assert.That(storage.TotalShards, Is.EqualTo(10)); // 중복 없는 영구 파편 합계
            GameObject.Find("EndingFinishButton").GetComponent<Button>().onClick.Invoke(); // 실제 결말 확인과 회차 종료
            yield return WaitForPhase(GamePhase.Title); // 실제 타이틀 복귀
            Assert.That(new RunStorage(RunStorage.TestDirectoryOverride).Load(out _), Is.Null); // 결말 확인 후 회차 종료 표시
            Assert.That(AppRoot.Instance.CanContinueRun, Is.False); // 종료된 회차 재개 차단
        } // 검증 끝
        [UnityTest] // 선택된 결말의 영구 저장 실패와 재시작 검증
        public IEnumerator SelectedEnding_ProfileLockAndRestartRetrySameTransaction() // 확정된 단일 선택과 중복 방지
        { // 검증 시작
            yield return StartBoss(false); // 실제 다섯 층과 마왕 전투 진입
            yield return WinBoss(); // 실제 마왕 일반 전투 승리
            yield return WaitForObject("EndingScreen"); // 실제 결말 선택 화면
            GameObject.Find("EndingBranchReturn").GetComponent<Button>().onClick.Invoke(); // 현실 귀환 방향 선택
            using (var gate = new System.IO.FileStream(System.IO.Path.Combine(RunStorage.TestDirectoryOverride, "ending-history.lock"), System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None)) // 영구 결말 기록 잠금
            { // 저장 실패 상황 시작
                GameObject.Find("EndingCandidate_" + EndingRules.ReturnHeroic).GetComponent<Button>().onClick.Invoke(); // 실제 영웅 귀환 선택
                yield return WaitForObject("EndingRetryButton"); // 영구 저장 실패의 재시도 표시
                var selected = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 선택 후 실제 회차 저장
                Assert.That(selected.endingSelectedId, Is.EqualTo(EndingRules.ReturnHeroic)); // 영구 저장 전에 선택 고정
                var transaction = selected.bossTransactionId; // 저장된 고유 마왕 거래
                Assert.That(GameObject.Find("EndingBranchReturn"), Is.Null); // 다른 결말 재선택 차단
                Object.Destroy(AppRoot.Instance.gameObject); // 선택 후 실제 실행 종료
                yield return null; // 공통 객체 제거 대기
                yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 새 실행 시작
                yield return WaitForPhase(GamePhase.Title); // 타이틀 대기
                Assert.That(AppRoot.Instance.ContinueRun(), Is.True); // 확정된 결말 회차 재개
                yield return WaitForObject("EndingRetryButton"); // 잠금 중 같은 선택의 재시도 표시
                var restored = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 재시작한 실제 회차
                Assert.That(restored.endingSelectedId, Is.EqualTo(EndingRules.ReturnHeroic)); // 재선택 없는 기존 결말 유지
                Assert.That(restored.bossTransactionId, Is.EqualTo(transaction)); // 같은 고유 거래 유지
                Assert.That(GameObject.Find("EndingBranchReturn"), Is.Null); // 복원 후 다른 결말 차단
            } // 실제 영구 저장 잠금 해제
            GameObject.Find("EndingRetryButton").GetComponent<Button>().onClick.Invoke(); // 저장된 결말 영구 기록 재시도
            yield return WaitForObject("EndingFinishButton"); // 실제 기록 완료 대기
            var history = new EndingHistoryStorage(RunStorage.TestDirectoryOverride); // 독립 영구 결말 조회
            var entries = history.Load(out var warning); // 실제 저장 기록
            Assert.That(warning, Is.Empty); // 정상 영구 저장 확인
            Assert.That(entries.Count, Is.EqualTo(1)); // 재시도 후 한 건의 거래
            Assert.That(entries[0].EndingId, Is.EqualTo(EndingRules.ReturnHeroic)); // 기존 선택만 기록
            Assert.That(history.TotalShards, Is.EqualTo(10)); // 중복 지급 없는 최초 파편
        } // 검증 끝
        [UnityTest] // 영구 결말 파일 변경과 종료 잠금 검증
        public IEnumerator EndingFinish_ProtectsRunWhenHistoryIsCorruptMissingOrLocked() // 실제 기록 손상과 삭제 및 잠금
        { // 검증 시작
            yield return StartBoss(false); // 실제 다섯 층과 마왕 전투 진입
            yield return WinBoss(); // 실제 마왕 일반 전투 승리
            yield return WaitForObject("EndingScreen"); // 실제 결말 선택 화면
            GameObject.Find("EndingBranchReturn").GetComponent<Button>().onClick.Invoke(); // 현실 귀환 방향 선택
            GameObject.Find("EndingCandidate_" + EndingRules.ReturnHeroic).GetComponent<Button>().onClick.Invoke(); // 실제 영웅 귀환 선택
            yield return WaitForObject("EndingFinishButton"); // 실제 영구 기록과 결말 결과 대기
            var run = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 선택된 실제 회차
            var runId = run.runId; // 보존할 고유 회차
            var transaction = run.bossTransactionId; // 보존할 결말 거래
            var path = System.IO.Path.Combine(RunStorage.TestDirectoryOverride, "ending-history-v1.json"); // 실제 영구 결말 파일
            var original = System.IO.File.ReadAllBytes(path); // 검증된 기록의 독립 백업
            var finish = GameObject.Find("EndingFinishButton").GetComponent<Button>(); // 실제 결말 확인 버튼
            System.IO.File.WriteAllText(path, "damaged ending history"); // 외부 파일 손상 재현
            finish.onClick.Invoke(); // 손상된 기록으로 회차 종료 시도
            yield return null; // 실제 오류 표시 대기
            AssertEndingStillOpen(runId, transaction); // 손상 시 선택과 회차 보존
            System.IO.File.WriteAllBytes(path, original); // 검증된 원래 기록 복구
            System.IO.File.Delete(path); // 외부 파일 삭제 재현
            finish.onClick.Invoke(); // 누락된 기록으로 회차 종료 시도
            yield return null; // 실제 오류 표시 대기
            AssertEndingStillOpen(runId, transaction); // 삭제 시 선택과 회차 보존
            System.IO.File.WriteAllBytes(path, original); // 검증된 원래 기록 복구
            using (var gate = new System.IO.FileStream(System.IO.Path.Combine(RunStorage.TestDirectoryOverride, "ending-history.lock"), System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None)) // 실제 영구 기록 잠금 점유
            { // 잠금 상황 시작
                finish.onClick.Invoke(); // 잠금 중 회차 종료 시도
                yield return null; // 잠금 오류 표시 대기
                AssertEndingStillOpen(runId, transaction); // 잠금 중 선택과 회차 보존
            } // 영구 기록 잠금 해제
            finish.onClick.Invoke(); // 정상 파일과 잠금 해제 후 재시도
            yield return WaitForPhase(GamePhase.Title); // 실제 회차 종료와 타이틀 복귀
            Assert.That(new RunStorage(RunStorage.TestDirectoryOverride).Load(out _), Is.Null); // 정상 재시도 후 회차 종료
        } // 검증 끝
        private static void AssertEndingStillOpen(string runId, string transaction) // 오류 시 원본 회차와 화면 확인
        { // 확인 시작
            Assert.That(GameObject.Find("EndingFinishButton"), Is.Not.Null); // 결말 확인 화면 유지
            Assert.That(GameObject.Find("EndingResultNotice").GetComponent<Text>().text, Is.Not.Empty); // 실제 오류 안내 표시
            var saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out var warning); // 실제 회차 저장 조회
            Assert.That(warning, Is.Empty); // 회차 파일의 정상 상태 확인
            Assert.That(saved, Is.Not.Null); // 회차 종료 차단 확인
            Assert.That(saved.runId, Is.EqualTo(runId)); // 동일한 회차 식별자 보존
            Assert.That(saved.bossTransactionId, Is.EqualTo(transaction)); // 동일한 결말 거래 보존
            Assert.That(saved.endingSelectedId, Is.EqualTo(EndingRules.ReturnHeroic)); // 기존 확정 결말 보존
        } // 확인 끝
        [UnityTest] // 마왕 항복의 결말과 패배 기록 검증
        public IEnumerator Surrender_RecordsServantEndingAndDefeat() // 실제 항복의 두 기록 확인
        { // 검증 시작
            yield return VerifyBossDefeat(true); // 확인된 항복의 실제 결과 검증
        } // 검증 끝
        [UnityTest] // 마왕 체력 패배의 결말과 패배 기록 검증
        public IEnumerator Defeat_RecordsServantEndingAndDefeat() // 실제 체력 소진의 두 기록 확인
        { // 검증 시작
            yield return VerifyBossDefeat(false); // 실제 체력 소진 결과 검증
        } // 검증 끝
        private IEnumerator VerifyBossDefeat(bool surrender) // 마왕 패배 원인별 영구 거래 확인
        { // 확인 시작
            yield return StartBoss(!surrender); // 실제 다섯 층과 마왕 전투 진입
            if (surrender) // 확인된 항복 검사
            { // 항복 시작
                GameObject.Find("ExitBattleButton").GetComponent<Button>().onClick.Invoke(); // 실제 항복 확인 열기
                GameObject.Find("ConfirmSurrender").GetComponent<Button>().onClick.Invoke(); // 항복 명시적 확정
            } // 항복 끝
            else // 체력 소진 패배
            { // 체력 소진 시작
                for (var turn = 0; turn < 10 && GameObject.Find("EndingFinishButton") == null; turn++) // 실제 패배와 자동 결과 저장 대기
                { // 전투 행동 시작
                    GameObject.Find("AdvanceBattleButton").GetComponent<Button>().onClick.Invoke(); // 실제 공격과 마왕의 반격
                    yield return null; // 자동 결과 저장 대기
                } // 전투 행동 끝
            } // 체력 소진 끝
            yield return WaitForObject("EndingFinishButton"); // 마왕의 종 영구 기록 완료 대기
            var ending = new EndingHistoryStorage(RunStorage.TestDirectoryOverride); // 실제 영구 결말 저장소
            var entries = ending.Load(out var warning); // 영구 결말 기록 조회
            Assert.That(warning, Is.Empty); // 정상 결말 기록 확인
            Assert.That(entries.Count, Is.EqualTo(1)); // 같은 회차의 단일 결말
            Assert.That(entries[0].EndingId, Is.EqualTo(EndingRules.DemonServant)); // 항복과 패배의 같은 결말
            Assert.That(ending.TotalShards, Is.EqualTo(5)); // 마왕의 종 최초 파편
            var defeat = new DefeatHistoryStorage(RunStorage.TestDirectoryOverride, id => id == "MON_DEMON_LORD"); // 실제 마왕 패배 기록
            var defeats = defeat.Load(out warning); // 저장된 패배 기록 조회
            Assert.That(warning, Is.Empty); // 정상 패배 기록 확인
            Assert.That(defeats.Count, Is.EqualTo(1)); // 실제 단일 패배 기록
            Assert.That(defeats[0].TargetId, Is.EqualTo("MON_DEMON_LORD")); // 실제 마왕 식별자
            Assert.That(defeats[0].Reason, Is.EqualTo(surrender ? BattleDefeatReason.Surrender : BattleDefeatReason.HealthZero)); // 실제 패배 원인 구분
            Assert.That(defeats[0].TransactionId, Is.EqualTo(entries[0].RunId)); // 같은 회차의 결말과 패배 기록
        } // 확인 끝
        private IEnumerator StartBoss(bool lowHealth) // 실제 계단과 마왕 방 진입
        { // 진입 시작
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
            if (lowHealth) // 체력 소진 시험 검사
            { // 체력 설정 시작
                app.UpdatePlayerHealth(1); // 마왕 반격에 소진할 생존 체력
            } // 체력 설정 끝
            yield return PressInteraction(); // 실제 마왕 입구 상호작용
            Assert.That(GameObject.Find("BossPreparationEnter"), Is.Not.Null); // 실제 준비 화면 표시
            GameObject.Find("BossPreparationEnter").GetComponent<Button>().onClick.Invoke(); // 실제 마왕 방 전투 진입
            yield return WaitForPhase(GamePhase.Combat); // 실제 전투 화면 대기
        } // 진입 끝
        private static IEnumerator WinBoss() // 실제 마왕 일반 전투 승리
        { // 승리 시작
            for (var turn = 0; turn < 30 && GameObject.Find("EndingScreen") == null; turn++) // 실제 공격과 결과 자동 저장
            { // 공격 시작
                var button = GameObject.Find("AdvanceBattleButton"); // 실제 공격 버튼
                Assert.That(button, Is.Not.Null); // 전투 화면 존재 확인
                button.GetComponent<Button>().onClick.Invoke(); // 실제 마왕 공격
                yield return null; // 전투와 자동 저장 대기
            } // 공격 끝
            Assert.That(GameObject.Find("EndingScreen"), Is.Not.Null); // 실제 승리 결말 화면 확인
        } // 승리 끝
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
            Assert.That(GameObject.Find(name), Is.Not.Null, "대기 중인 화면: " + name); // 대기 화면 이름 확인
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
