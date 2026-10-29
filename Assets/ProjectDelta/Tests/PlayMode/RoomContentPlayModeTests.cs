using System.Collections; // 프레임 대기
using System.Collections.Generic; // 경로 탐색
using System.Linq; // 저장 방 조회
using System.Reflection; // 실제 방 조회
using NUnit.Framework; // 통합 검증
using ProjectDelta.Core; // 방 콘텐츠 규칙
using ProjectDelta.Runtime; // 실제 게임 흐름
using UnityEngine; // 게임 객체와 화면
using UnityEngine.InputSystem; // 검증용 키보드
using UnityEngine.InputSystem.LowLevel; // 가상 키 입력
using UnityEngine.SceneManagement; // 실제 시작 씬
using UnityEngine.TestTools; // 프레임 기반 검증
using UnityEngine.Tilemaps; // 실제 방 타일맵
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class RoomContentPlayModeTests // 실제 상자와 함정 통합 검증
    { // 클래스 시작
        private Keyboard keyboard; // 검증용 키보드
        private InputSettings.BackgroundBehavior originalBackground; // 기존 백그라운드 입력 설정
#if UNITY_EDITOR // 편집기 설정 조건
        private InputSettings.EditorInputBehaviorInPlayMode originalEditor; // 기존 편집기 입력 설정
#endif // 편집기 설정 끝
        [SetUp] // 시험 준비
        public void Setup() // 독립 저장과 실제 콘텐츠 활성화
        { // 준비 시작
            RoomTravelController.RoomContentTestOverride = true; // 실제 시험 방 콘텐츠 사용
            RoomTravelController.TutorialEncounterTestOverride = false; // 독립 콘텐츠 검증
            AppRoot.AutomaticEncounterTestOverride = false; // 시험 조우 화면 사용
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            RoomTravelController.ForceRollOverride = (minimum, maximum) => maximum - 1; // 잠긴 문의 시험 주사위
            RunStorage.TestDirectoryOverride = System.IO.Path.Combine(Application.dataPath, "..", "Artifacts", "RoomContentPlayModeTests", System.Guid.NewGuid().ToString("N")); // 독립 회차 저장
            ObservationStorage.TestDirectoryOverride = RunStorage.TestDirectoryOverride; // 독립 관찰 저장
            originalBackground = InputSystem.settings.backgroundBehavior; // 기존 입력 설정 보관
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 자동 검증 입력 허용
#if UNITY_EDITOR // 편집기 설정 조건
            originalEditor = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 설정 보관
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 실제 게임 입력
#endif // 편집기 설정 끝
            keyboard = InputSystem.AddDevice<Keyboard>(); // 실제 이동 입력 장치
        } // 준비 끝
        [UnityTearDown] // 시험 정리
        public IEnumerator Cleanup() // 검증 설정과 객체 복구
        { // 정리 시작
            RoomTravelController.RoomContentTestOverride = null; // 콘텐츠 설정 해제
            RoomTravelController.TutorialEncounterTestOverride = null; // 조우 설정 해제
            RoomTravelController.GenerationSeedOverride = null; // 던전 시드 해제
            RoomTravelController.ForceRollOverride = null; // 문 주사위 해제
            AppRoot.AutomaticEncounterTestOverride = null; // 조우 방식 복구
            AppRoot.ChestRollOverride = null; // 상자 주사위 해제
            CombatScreenView.RandomOverride = null; // 전투 난수 해제
            CombatScreenView.RetreatRollOverride = null; // 후퇴 난수 해제
            if (keyboard != null && keyboard.added) // 입력 장치 검사
            { // 장치 정리 시작
                InputSystem.RemoveDevice(keyboard); // 시험 키보드 제거
            } // 장치 정리 끝
            if (AppRoot.Instance != null) // 공통 객체 검사
            { // 객체 정리 시작
                Object.Destroy(AppRoot.Instance.gameObject); // 시험 회차 제거
            } // 객체 정리 끝
            yield return null; // 객체 제거 대기
            RunStorage.TestDirectoryOverride = null; // 회차 저장 경로 해제
            ObservationStorage.TestDirectoryOverride = null; // 관찰 저장 경로 해제
            InputSystem.settings.backgroundBehavior = originalBackground; // 기존 입력 설정 복구
#if UNITY_EDITOR // 편집기 설정 조건
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditor; // 기존 편집기 설정 복구
#endif // 편집기 설정 끝
        } // 정리 끝
        [UnityTest] // 일반 상자와 저장된 보상 검증
        public IEnumerator NormalChest_SavesCandidatesAndClaimsOnce() // 실제 상자 화면과 지급 확인
        { // 검증 시작
            AppRoot.ChestRollOverride = (minimum, maximum) => maximum == 100 ? 99 : minimum; // 일반 상자와 최소 후보 고정
            yield return EnterDungeon(); // 실제 던전 진입
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 층 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            Assert.That(travel.RoomContentEnabled, Is.True); // 실제 콘텐츠 활성 확인
            var entry = travel.CurrentContent.Snapshot().chests.First(item => !item.chest.wasLocked); // 열쇠 없는 실제 상자
            foreach (var door in FindTestRoute(travel.Layout, travel.Layout.GetRoom(travel.RoomX, travel.RoomY), travel.Layout.GetRoom(entry.x, entry.y))) // 상자 방 경로
            { // 이동 시작
                yield return UseDoor(travel, player, door); // 실제 출구 이동
            } // 이동 끝
            Assert.That(AppRoot.Instance.OpenChest(), Is.True); // 실제 상자 화면 열기
            Assert.That(AppRoot.Instance.Input.IsExplorationEnabled, Is.False); // 화면 중 이동 잠금
            Assert.That(AppRoot.Instance.TryRevealChest(out var message), Is.True, message); // 실제 보상 확정 저장
            var current = AppRoot.Instance.CurrentChest; // 확정된 상자
            Assert.That(current.mimicState, Is.EqualTo(1)); // 일반 상자 판정
            var saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 실제 저장 읽기
            var stored = saved.roomContents.Single(item => item.floor == travel.Floor).chests.Single(item => item.chest.chestId == current.chestId).chest; // 저장된 상자 조회
            Assert.That(stored.transactionId, Is.EqualTo(current.transactionId)); // 화면 표시 전 거래 저장
            Assert.That(stored.rewards[0].candidateId, Is.EqualTo(current.rewards[0].candidateId)); // 실제 후보 유지
            var path = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 실제 화면 캡처 경로
            if (!string.IsNullOrEmpty(path) && Application.isBatchMode) // 자동 캡처 요청 검사
            { // 캡처 시작
                GameObject.Find("ChestScreen").GetComponent<ChestScreenView>().Refresh(); // 보상 내용 화면 갱신
                yield return null; // 화면 갱신 대기
                var image = CaptureScreen("ChestCanvas"); // 실제 상자 화면 렌더링
                System.IO.File.WriteAllBytes(path, image.EncodeToPNG()); // 실제 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 끝
            Assert.That(AppRoot.Instance.TryClaimChest(new[] { 0 }, -1, out message), Is.True, message); // 선택 보상 원자적 획득
            Assert.That(AppRoot.Instance.CurrentChest.completed, Is.True); // 상자 완료 상태 확인
            Assert.That(AppRoot.Instance.OpenChest(), Is.False); // 완료 상자 재지급 차단
            saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 지급 후 저장 읽기
            Assert.That(saved.roomContents.Single(item => item.floor == travel.Floor).chests.Single(item => item.chest.chestId == current.chestId).chest.completed, Is.True); // 완료 상태 실제 저장
        } // 검증 끝
        [UnityTest] // 미믹 확정과 체력 저장 검증
        public IEnumerator Mimic_SavesHealthAndKeepsFixedCandidates() // 조우와 저장된 잔여 체력 확인
        { // 검증 시작
            AppRoot.ChestRollOverride = (minimum, maximum) => minimum; // 미믹과 최소 후보 고정
            yield return EnterDungeon(); // 실제 던전 진입
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 층 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var entry = travel.CurrentContent.Snapshot().chests.First(item => !item.chest.wasLocked); // 잠금 없는 실제 상자
            foreach (var door in FindTestRoute(travel.Layout, travel.Layout.GetRoom(travel.RoomX, travel.RoomY), travel.Layout.GetRoom(entry.x, entry.y))) // 실제 상자 경로
            { // 이동 시작
                yield return UseDoor(travel, player, door); // 실제 출구 이동
            } // 이동 끝
            Assert.That(AppRoot.Instance.OpenChest(), Is.True); // 실제 상자 화면 열기
            Assert.That(AppRoot.Instance.TryRevealChest(out var message), Is.True, message); // 미믹과 고정 후보 저장
            Assert.That(travel.IsCurrentMimic, Is.True); // 확정된 미믹 조우 확인
            var original = AppRoot.Instance.CurrentChest; // 최초 확정된 거래
            GameObject.Find("EncounterFightButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 실제 전투 진입
            yield return null; // 전투 화면 준비
            CombatScreenView.RandomOverride = (minimum, maximum) => minimum; // 시험 공격 난수 고정
            GameObject.Find("AdvanceBattleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 실제 미믹에게 피해 적용
            yield return null; // 전투 행동 종료 대기
            var rolls = new Queue<int>(new[] { 20, 1 }); // 후퇴 성공 주사위
            CombatScreenView.RetreatRollOverride = (minimum, maximum) => rolls.Dequeue(); // 후퇴 주사위 연결
            GameObject.Find("FleeBattleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 실제 후퇴 선택
            yield return null; // 방 복귀 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 후퇴 후 탐험 복귀
            var saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 실제 회차 저장 읽기
            var stored = saved.roomContents.Single(item => item.floor == travel.Floor).chests.Single(item => item.chest.chestId == original.chestId).chest; // 저장된 미믹 상태
            Assert.That(stored.mimicHealth, Is.GreaterThan(0).And.LessThan(RunRestoreRules.TrialEnemyMaxHealth)); // 실제 피해 후 저장 체력 확인
            Assert.That(stored.transactionId, Is.EqualTo(original.transactionId)); // 재추첨 없는 거래 보존
            Assert.That(stored.rewards[0].candidateId, Is.EqualTo(original.rewards[0].candidateId)); // 후보 식별자 보존
            Assert.That(ChestState.TryRestore(stored, out var restored), Is.True); // 저장된 상자 재구성
            var invalid = restored.Snapshot(); // 독립 상자 저장본
            invalid.mimicHealth = RunRestoreRules.TrialEnemyMaxHealth + 1; // 최대 체력 초과 상태
            Assert.That(ChestState.TryRestore(invalid, out _), Is.False); // 손상된 체력 복원 차단
            Assert.That(restored.Snapshot().mimicHealth, Is.EqualTo(stored.mimicHealth)); // 재구성된 체력 확인
            var returnDoor = travel.Layout.GetConnection(travel.RoomX, travel.RoomY, entry.x, entry.y); // 미믹 방 재진입 출구
            yield return UseDoor(travel, player, returnDoor); // 실제 미믹 방 재진입
            Assert.That(travel.CurrentEncounterHealth(), Is.EqualTo(stored.mimicHealth)); // 재조우용 체력 복원
            Assert.That(AppRoot.Instance.StartEncounter(), Is.True); // 저장된 미믹 다시 조우
            GameObject.Find("EncounterFightButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 실제 전투 재진입
            yield return null; // 전투 화면 준비
            Assert.That(GameObject.Find("EnemyHealth").GetComponent<UnityEngine.UI.Text>().text, Does.Contain(stored.mimicHealth + "/")); // 재조우 화면의 저장 체력
            for (var turn = 0; turn < 10 && GameObject.Find("RoundStatus").GetComponent<UnityEngine.UI.Text>().text != "승리"; turn++) // 승리까지 실제 공격
            { // 공격 반복 시작
                GameObject.Find("AdvanceBattleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 실제 미믹 공격
                yield return null; // 다음 행동 프레임 대기
            } // 공격 반복 끝
            Assert.That(GameObject.Find("RoundStatus").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("승리")); // 실제 미믹 승리 확인
            GameObject.Find("ExitBattleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 승리 보상 화면 진입
            yield return null; // 실제 상자 화면 준비
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Inventory)); // 미믹 보상 화면 확인
            Assert.That(AppRoot.Instance.CurrentChest.mimicDefeated, Is.True); // 미믹 처치 기록
            Assert.That(AppRoot.Instance.CurrentChest.mimicHealth, Is.Zero); // 처치 후 체력 초기화
            Assert.That(AppRoot.Instance.CurrentChest.transactionId, Is.EqualTo(original.transactionId)); // 처음 확정한 보상 유지
            Assert.That(GameObject.Find("ChestReward_0"), Is.Not.Null); // 실제 보상 선택 버튼
            GameObject.Find("ChestReward_0").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 첫 보상 선택
            GameObject.Find("ChestClaim").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 실제 보상 획득
            Assert.That(AppRoot.Instance.CurrentChest.completed, Is.True); // 미믹 보상 지급 완료
            Assert.That(AppRoot.Instance.OpenChest(), Is.False); // 완료 상자의 중복 개방 차단
            var completed = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 지급 완료 저장 읽기
            Assert.That(completed.roomContents.Single(item => item.floor == travel.Floor).chests.Single(item => item.chest.chestId == original.chestId).chest.completed, Is.True); // 실제 저장의 중복 지급 차단
        } // 검증 끝
        [UnityTest] // 보상 교체 중 다른 행동 차단 검증
        public IEnumerator ChestReplacement_BlocksClaimSkipAndCloseUntilCancelled() // 슬롯 선택과 확인 중 원본 보존
        { // 검증 시작
            AppRoot.ChestRollOverride = (minimum, maximum) => maximum == 100 ? 99 : minimum; // 일반 상자 후보 고정
            yield return EnterDungeon(); // 실제 던전 진입
            var app = AppRoot.Instance; // 실제 회차 관리자
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 층 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var entry = travel.CurrentContent.Snapshot().chests.First(item => !item.chest.wasLocked); // 잠금 없는 상자
            foreach (var door in FindTestRoute(travel.Layout, travel.Layout.GetRoom(travel.RoomX, travel.RoomY), travel.Layout.GetRoom(entry.x, entry.y))) // 실제 상자 방 경로
            { // 이동 시작
                yield return UseDoor(travel, player, door); // 실제 출구 이동
            } // 이동 끝
            var definition = TrialConsumableRules.Find("trial-small-potion"); // 등록된 시험 소비품
            Assert.That(app.Inventory.Add(definition.Id, definition.Name, 1, definition.StackLimit).Remaining, Is.Zero); // 교체할 보유 아이템 준비
            var index = -1; // 선택할 기존 슬롯 번호
            for (var slot = 0; slot < app.Inventory.Slots.Count; slot++) // 실제 보유 슬롯 순회
            { // 슬롯 검색 시작
                if (app.Inventory.Slots[slot].ItemId == definition.Id) // 준비한 아이템 검사
                { // 일치 시작
                    index = slot; // 실제 보유 슬롯 보관
                    break; // 슬롯 검색 종료
                } // 일치 끝
            } // 슬롯 검색 끝
            Assert.That(index, Is.GreaterThanOrEqualTo(0)); // 실제 보유 슬롯 확인
            var beforeQuantity = app.Inventory.Slots[index].Quantity; // 확인 전 기존 수량
            Assert.That(app.OpenChest(), Is.True); // 실제 상자 화면 열기
            Assert.That(app.TryRevealChest(out var message), Is.True, message); // 실제 보상 후보 저장
            GameObject.Find("ChestScreen").GetComponent<ChestScreenView>().Refresh(); // 확정된 후보 버튼 표시
            var claim = GameObject.Find("ChestClaim").GetComponent<UnityEngine.UI.Button>(); // 기본 획득 버튼
            var skip = GameObject.Find("ChestSkip").GetComponent<UnityEngine.UI.Button>(); // 기본 포기 버튼
            var close = GameObject.Find("ChestClose").GetComponent<UnityEngine.UI.Button>(); // 기본 닫기 버튼
            GameObject.Find("ChestReward_0").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 단일 보상 선택
            GameObject.Find("ChestReplace").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 보유 슬롯 선택 진입
            Assert.That(GameObject.Find("ChestReplacementSelection"), Is.Not.Null); // 선택 화면 표시
            claim.onClick.Invoke(); // 선택 중 기본 획득 시도
            skip.onClick.Invoke(); // 선택 중 기본 포기 시도
            close.onClick.Invoke(); // 선택 중 기본 닫기 시도
            Assert.That(app.Flow.Phase, Is.EqualTo(GamePhase.Inventory)); // 상자 화면 유지
            Assert.That(app.CurrentChest.completed, Is.False); // 보상 미확정 유지
            Assert.That(app.Inventory.Slots[index].Quantity, Is.EqualTo(beforeQuantity)); // 기존 묶음 수량 유지
            GameObject.Find("ChestSlot_" + index).GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 보유 슬롯 선택
            Assert.That(GameObject.Find("ChestReplacementConfirmation"), Is.Not.Null); // 최종 확인 표시
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("CancelChestConfirmation")); // 안전한 기본 취소 선택
            claim.onClick.Invoke(); // 확인 중 기본 획득 시도
            skip.onClick.Invoke(); // 확인 중 기본 포기 시도
            close.onClick.Invoke(); // 확인 중 기본 닫기 시도
            Assert.That(app.Flow.Phase, Is.EqualTo(GamePhase.Inventory)); // 확인 화면 유지
            Assert.That(app.CurrentChest.completed, Is.False); // 확인 중 보상 미확정
            Assert.That(app.Inventory.Slots[index].Quantity, Is.EqualTo(beforeQuantity)); // 확인 중 기존 묶음 유지
            GameObject.Find("CancelChestConfirmation").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 최종 교체 취소
            Assert.That(GameObject.Find("ChestReplacementSelection"), Is.Not.Null); // 슬롯 선택 복귀
            GameObject.Find("CancelChestReplacement").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 슬롯 선택 취소
            Assert.That(app.CurrentChest.completed, Is.False); // 취소 후 보상 미확정
            Assert.That(app.Inventory.Slots[index].Quantity, Is.EqualTo(beforeQuantity)); // 취소 후 원래 묶음 보존
        } // 검증 끝
        [UnityTest] // 함정의 최초 진입과 중복 방지 검증
        public IEnumerator Trap_TriggersOnlyOnceAndPersists() // 실제 이동 후 함정 완료 확인
        { // 검증 시작
            AppRoot.ChestRollOverride = (minimum, maximum) => minimum; // 시험 함정 판정 고정
            yield return EnterDungeon(); // 실제 던전 진입
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 층 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var target = travel.CurrentContent.TrapRoom; // 실제 함정 방
            foreach (var door in FindTestRoute(travel.Layout, travel.Layout.GetRoom(travel.RoomX, travel.RoomY), travel.Layout.GetRoom(target.X, target.Y))) // 함정 방 경로
            { // 이동 시작
                yield return UseDoor(travel, player, door); // 실제 출구 이동
            } // 이동 끝
            yield return null; // 전환 후 함정 처리 대기
            Assert.That(travel.CurrentContent.Snapshot().trapCompleted, Is.True); // 최초 진입 함정 완료
            var health = AppRoot.Instance.PlayerHealth; // 최초 처리 후 체력
            Assert.That(AppRoot.Instance.TryApplyTrap(travel, out _), Is.False); // 함정 재처리 차단
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(health)); // 중복 피해 차단
            var saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 실제 저장 읽기
            Assert.That(saved.roomContents.Single(item => item.floor == travel.Floor).trapCompleted, Is.True); // 함정 완료 상태 저장
        } // 검증 끝
        [UnityTest] // 함정 저장 실패 후 재시도 검증
        public IEnumerator Trap_SaveFailureBlocksMovementAndAllowsPauseAndRetry() // 저장 실패 시 상태 보존과 입력 확인
        { // 검증 시작
            AppRoot.ChestRollOverride = (minimum, maximum) => minimum; // 함정 판정 고정
            yield return EnterDungeon(); // 실제 던전 진입
            var app = AppRoot.Instance; // 현재 회차 관리자
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 층 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var target = travel.CurrentContent.TrapRoom; // 실제 함정 방
            var route = FindTestRoute(travel.Layout, travel.Layout.GetRoom(travel.RoomX, travel.RoomY), travel.Layout.GetRoom(target.X, target.Y)); // 함정 방 경로
            Assert.That(route.Count, Is.GreaterThan(0)); // 시작 방과 분리된 함정 확인
            for (var index = 0; index < route.Count - 1; index++) // 함정 방 직전까지 이동
            { // 이동 시작
                yield return UseDoor(travel, player, route[index]); // 실제 문 통과
            } // 이동 끝
            System.IO.Directory.CreateDirectory(RunStorage.TestDirectoryOverride); // 독립 저장 폴더 준비
            using (var gate = new System.IO.FileStream(System.IO.Path.Combine(RunStorage.TestDirectoryOverride, "run.lock"), System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None)) // 실제 저장 잠금 점유
            { // 실패 상황 시작
                yield return UseDoor(travel, player, route[route.Count - 1]); // 함정 방 진입
                yield return null; // 전환 후 함정 처리 대기
                Assert.That(travel.IsTrapPending, Is.True); // 저장 실패 후 미처리 상태
                Assert.That(travel.CurrentContent.Snapshot().trapCompleted, Is.False); // 실패한 함정 완료 차단
                var x = player.X; // 이동 전 가로 위치
                var y = player.Y; // 이동 전 세로 위치
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); // 이동 키 누름
                InputSystem.Update(); // 실제 이동 입력 반영
                yield return null; // 이동 처리 대기
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이동 키 해제
                InputSystem.Update(); // 해제 반영
                yield return null; // 이동 완료 대기
                Assert.That(player.X, Is.EqualTo(x)); // 미처리 함정의 가로 이동 차단
                Assert.That(player.Y, Is.EqualTo(y)); // 미처리 함정의 세로 이동 차단
                Assert.That(player.IsMoving, Is.False); // 미처리 함정의 이동 시작 차단
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 메뉴 키 누름
                yield return null; // 메뉴 입력 처리
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 메뉴 키 해제
                yield return null; // 메뉴 준비 대기
                Assert.That(app.Flow.Phase, Is.EqualTo(GamePhase.Pause)); // 저장 실패 중 ESC 메뉴 허용
                app.ClosePause(); // 실제 메뉴 닫기
                yield return null; // 탐험 입력 복구 대기
            } // 저장 잠금 해제
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 함정 재시도 입력
            InputSystem.Update(); // 상호작용 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 상호작용 해제
            InputSystem.Update(); // 입력 해제 반영
            yield return null; // 재시도 시작 대기
            yield return null; // 재시도 저장 완료 대기
            Assert.That(travel.IsTrapPending, Is.False); // 함정 재시도 완료
            Assert.That(travel.CurrentContent.Snapshot().trapCompleted, Is.True); // 완료 상태 반영
            var saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 실제 저장 읽기
            Assert.That(saved.roomContents.Single(item => item.floor == travel.Floor).trapCompleted, Is.True); // 재시도 결과 저장 확인
        } // 검증 끝
        private static System.Collections.Generic.List<RoomDoor> FindTestRoute(DungeonLayout layout, DungeonRoom start, DungeonRoom target) // 시험 목적 방까지 연결 경로 조회
        { // 경로 조회 시작
            var queue = new Queue<DungeonRoom>(); // 연결 방 탐색 대기열
            var previous = new Dictionary<DungeonRoom, RoomDoor>(); // 방별 이전 출구 목록
            queue.Enqueue(start); // 시작 방 예약
            previous.Add(start, null); // 시작 방 방문 표시
            while (queue.Count > 0 && !previous.ContainsKey(target)) // 목적 방까지 탐색
            { // 탐색 시작
                var current = queue.Dequeue(); // 다음 방 조회
                foreach (var door in layout.Doors) // 현재 구조의 출구 순회
                { // 출구 검사 시작
                    if (door.RoomX != current.X || door.RoomY != current.Y) // 현재 방 출구 검사
                    { // 다른 방 조건 시작
                        continue; // 다른 방 출구 제외
                    } // 다른 방 조건 끝
                    var next = layout.GetRoom(door.TargetRoomX, door.TargetRoomY); // 연결된 방 조회
                    if (previous.ContainsKey(next)) // 이미 방문한 방 검사
                    { // 중복 조건 시작
                        continue; // 중복 탐색 제외
                    } // 중복 조건 끝
                    previous.Add(next, door); // 이전 출구 보관
                    queue.Enqueue(next); // 다음 방 탐색 예약
                } // 출구 검사 끝
            } // 탐색 끝
            Assert.That(previous.ContainsKey(target), Is.True); // 시험 목적 방의 도달 가능 확인
            var route = new List<RoomDoor>(); // 목적 방까지 실제 출구 목록
            var cursor = target; // 목적 방부터 역추적
            while (cursor != start) // 시작 방까지 반복
            { // 역추적 시작
                var door = previous[cursor]; // 현재 방으로 들어온 출구
                route.Add(door); // 출구 경로 보관
                cursor = layout.GetRoom(door.RoomX, door.RoomY); // 이전 방 조회
            } // 역추적 끝
            route.Reverse(); // 출발 방부터 이동 순서 정렬
            return route; // 실제 연결 경로 전달
        } // 경로 조회 끝
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
