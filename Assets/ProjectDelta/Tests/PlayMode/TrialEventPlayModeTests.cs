using System.Collections; // 프레임 기반 대기
using System.Collections.Generic; // 방 경로 탐색
using System.Linq; // 저장된 이벤트 조회
using System.Reflection; // 실제 방 객체 조회
using NUnit.Framework; // PlayMode 검증 도구
using ProjectDelta.Core; // 방과 이벤트 저장 데이터
using ProjectDelta.Runtime; // 실제 게임과 입력
using UnityEngine; // 씬 객체와 시간
using UnityEngine.InputSystem; // 시험 키보드
using UnityEngine.InputSystem.LowLevel; // 가상 키 입력 상태
using UnityEngine.SceneManagement; // 실제 씬 재시작
using UnityEngine.TestTools; // 프레임 기반 테스트
using UnityEngine.Tilemaps; // 실제 방의 타일
using UnityEngine.UI; // 이벤트 버튼과 설명
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TrialEventPlayModeTests // 시험 이벤트의 실제 이동과 저장 재개 검증
    { // 검증 클래스 시작
        private Keyboard keyboard; // 독립 시험 키보드
        private InputSettings.BackgroundBehavior originalBackground; // 원래 입력 설정
#if UNITY_EDITOR // 편집기 입력 설정 조건
        private InputSettings.EditorInputBehaviorInPlayMode originalEditor; // 원래 편집기 입력 설정
#endif // 편집기 입력 설정 끝
        [SetUp] // 실제 이벤트 시험 준비
        public void Setup() // 독립 저장과 재현 가능한 던전 준비
        { // 준비 시작
            RunStorage.TestDirectoryOverride = System.IO.Path.Combine(Application.dataPath, "..", "Artifacts", "TrialEventPlayMode", System.Guid.NewGuid().ToString("N")); // 독립 회차 저장 경로
            ObservationStorage.TestDirectoryOverride = RunStorage.TestDirectoryOverride; // 독립 관찰 저장 경로
            RoomTravelController.RoomContentTestOverride = true; // 실제 상자와 함정 콘텐츠 활성화
            RoomTravelController.TutorialEncounterTestOverride = false; // 실제 방 이동의 첫 조우 제외
            RoomTravelController.GenerationSeedOverride = 0; // 결정적인 시험 층
            AppRoot.AutomaticEncounterTestOverride = false; // 시험 전투 연출 제외
            AppRoot.ChestRollOverride = (minimum, maximum) => minimum; // 경유 함정의 회피 판정 고정
            originalBackground = InputSystem.settings.backgroundBehavior; // 원래 입력 포커스 설정
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 자동 검증 입력 허용
#if UNITY_EDITOR // 편집기 입력 조건
            originalEditor = InputSystem.settings.editorInputBehaviorInPlayMode; // 원래 편집기 입력 설정
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 편집기 시험 입력 허용
#endif // 편집기 입력 조건 끝
            keyboard = InputSystem.AddDevice<Keyboard>(); // 방 이동을 위한 시험 키보드
        } // 준비 끝
        [UnityTearDown] // 실제 게임 정리
        public IEnumerator Cleanup() // 시험 입력과 저장 경로 복구
        { // 정리 시작
            RoomTravelController.RoomContentTestOverride = null; // 방 콘텐츠 설정 복구
            RoomTravelController.TutorialEncounterTestOverride = null; // 첫 조우 설정 복구
            RoomTravelController.GenerationSeedOverride = null; // 던전 씨드 설정 복구
            AppRoot.AutomaticEncounterTestOverride = null; // 조우 방식 설정 복구
            AppRoot.ChestRollOverride = null; // 시험 난수 복구
            AppRoot.TrialEventRollOverride = null; // 이벤트 판정 난수 복구
            if (keyboard != null && keyboard.added) // 시험 입력 장치 검사
            { // 입력 정리 시작
                InputSystem.RemoveDevice(keyboard); // 독립 키보드 제거
            } // 입력 정리 끝
            if (AppRoot.Instance != null) // 공통 게임 객체 검사
            { // 게임 정리 시작
                Object.Destroy(AppRoot.Instance.gameObject); // 독립 회차 객체 제거
            } // 게임 정리 끝
            yield return null; // 객체 정리 대기
            RunStorage.TestDirectoryOverride = null; // 회차 저장 경로 복구
            ObservationStorage.TestDirectoryOverride = null; // 관찰 저장 경로 복구
            InputSystem.settings.backgroundBehavior = originalBackground; // 입력 포커스 설정 복구
#if UNITY_EDITOR // 편집기 입력 조건
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditor; // 편집기 입력 설정 복구
#endif // 편집기 입력 조건 끝
        } // 정리 끝
        [UnityTest] // 실제 방 이동과 저장 재개 검증
        public IEnumerator EventRoom_SavesLeaveResultAndNeverRepeats() // 조건 비활성화와 결과 복원 확인
        { // 검증 시작
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 시작 씬 열기
            yield return WaitForPhase(GamePhase.Title); // 시작 화면 준비 대기
            AppRoot.Instance.StartNewGame(); // 실제 새 회차 시작
            yield return WaitForPhase(GamePhase.Exploration); // 던전 생성 대기
            var app = AppRoot.Instance; // 실제 회차 관리자
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 이동 관리자
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 격자 플레이어
            var target = travel.TrialEventRoom; // 실제 시험 이벤트 방
            Assert.That(target, Is.Not.Null); // 이벤트 방 배치 확인
            var route = FindRoute(travel.Layout, travel.Layout.GetRoom(travel.RoomX, travel.RoomY), target); // 실제 문 연결 경로
            Assert.That(route.Count, Is.GreaterThan(0)); // 시작 방과 다른 이벤트 방 확인
            foreach (var door in route) // 목적 방까지 문 순회
            { // 실제 이동 시작
                yield return UseDoor(travel, player, door); // 실제 키 입력으로 방 이동
            } // 실제 이동 끝
            yield return WaitForObject("TrialEventScreen"); // 자동 이벤트 화면 대기
            Assert.That(app.Input.IsExplorationEnabled, Is.False); // 선택 중 탐험 입력 잠금
            var eventData = app.CurrentEvent; // 실제 화면의 이벤트 상태
            Assert.That(eventData, Is.Not.Null); // 이벤트 상태 확인
            Assert.That(eventData.phase, Is.Zero); // 선택 단계 확인
            var inspect = System.Array.IndexOf(eventData.choices, 0); // 저장된 순서의 조사 위치
            var leave = System.Array.IndexOf(eventData.choices, 1); // 저장된 순서의 떠나기 위치
            Assert.That(inspect, Is.InRange(0, 1)); // 조사 선택 위치 확인
            Assert.That(leave, Is.InRange(0, 1)); // 떠나기 선택 위치 확인
            var inspectButton = GameObject.Find("TrialEventChoice_" + inspect).GetComponent<Button>(); // 실제 조사 선택 버튼
            Assert.That(app.Gold, Is.Zero); // 재화 부족 시험의 시작 골드 확인
            Assert.That(inspectButton.interactable, Is.False); // 조건 부족 조사 비활성 확인
            Assert.That(GameObject.Find("TrialEventRequirement").GetComponent<Text>().text, Does.Contain("골드")); // 비활성 이유 표시 확인
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 실제 화면 캡처 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) // 자동 캡처와 그래픽 장치 검사
            { // 화면 캡처 시작
                yield return null; // 실제 이벤트 화면 갱신 대기
                var image = CaptureScreen("TrialEventCanvas"); // 실제 이벤트 선택 화면의 독립 렌더링
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 요청 경로에 실제 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 화면 캡처 끝
            GameObject.Find("TrialEventChoice_" + leave).GetComponent<Button>().onClick.Invoke(); // 실제 떠나기 선택
            yield return null; // 선택과 결과 저장 반영
            Assert.That(app.CurrentEvent.phase, Is.EqualTo(1)); // 결과 확인 단계
            Assert.That(app.CurrentEvent.result, Is.EqualTo(TrialEventRules.Left)); // 실제 떠나기 결과
            var transaction = app.CurrentEvent.transactionId; // 확정된 결과 거래 식별자
            var run = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 실제 저장 파일 읽기
            var saved = run.trialEvents.Single(item => item.floor == travel.Floor && item.x == target.X && item.y == target.Y).eventData; // 해당 이벤트 방 저장본
            Assert.That(saved.transactionId, Is.EqualTo(transaction)); // 결과 화면 표시 전 거래 저장
            Assert.That(saved.phase, Is.EqualTo(1)); // 확인 전 결과 저장 확인
            Object.Destroy(app.gameObject); // 결과 화면에서 강제 종료 모사
            yield return null; // 기존 회차 객체 정리 대기
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 재시작 씬 로드
            yield return WaitForPhase(GamePhase.Title); // 타이틀 재준비 대기
            app = AppRoot.Instance; // 새 실행의 실제 관리자
            Assert.That(app.ContinueRun(), Is.True); // 실제 저장 회차 재개
            yield return WaitForObject("TrialEventConfirm"); // 저장된 결과 화면 재표시 대기
            Assert.That(app.CurrentEvent.phase, Is.EqualTo(1)); // 기존 결과 단계 유지
            Assert.That(app.CurrentEvent.transactionId, Is.EqualTo(transaction)); // 재선택과 재추첨 없는 동일 결과
            Assert.That(app.CurrentEvent.result, Is.EqualTo(TrialEventRules.Left)); // 저장된 떠나기 결과 유지
            GameObject.Find("TrialEventConfirm").GetComponent<Button>().onClick.Invoke(); // 실제 결과 확인
            yield return WaitForPhase(GamePhase.Exploration); // 탐험으로 복귀
            travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 재개된 실제 방 관리자
            player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 재개된 실제 플레이어
            run = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 완료 후 저장 읽기
            saved = run.trialEvents.Single(item => item.floor == travel.Floor && item.x == target.X && item.y == target.Y).eventData; // 완료된 이벤트 저장본
            Assert.That(saved.phase, Is.EqualTo(2)); // 이벤트 완료 상태 저장
            Assert.That(saved.transactionId, Is.EqualTo(transaction)); // 완료 전후 동일 거래
            var back = travel.Layout.GetConnection(travel.RoomX, travel.RoomY, route[route.Count - 1].RoomX, route[route.Count - 1].RoomY); // 이벤트 방에서 이전 방으로 연결
            yield return UseDoor(travel, player, back); // 실제 이벤트 방 이탈
            var returnDoor = travel.Layout.GetConnection(travel.RoomX, travel.RoomY, target.X, target.Y); // 완료된 이벤트 방 재진입 출구
            yield return UseDoor(travel, player, returnDoor); // 실제 이벤트 방 재방문
            yield return null; // 이벤트 재발생 여부 대기
            Assert.That(GameObject.Find("TrialEventScreen"), Is.Null); // 같은 회차의 이벤트 재발생 차단
            Assert.That(app.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 완료 방의 탐험 유지
        } // 검증 끝
        [UnityTest] // 시험 성공 행동과 결과 재개 검증
        public IEnumerator EventRoom_SuccessPersistsGoldAndResumesResult() // 선택 비용과 결과 중단 복원 확인
        { // 검증 시작
            AppRoot.TrialEventRollOverride = (minimum, maximum) => minimum; // 동점 성공을 위한 시험 판정 고정
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 시작 씬 열기
            yield return WaitForPhase(GamePhase.Title); // 시작 화면 준비 대기
            AppRoot.Instance.StartNewGame(); // 실제 새 회차 시작
            yield return WaitForPhase(GamePhase.Exploration); // 던전 생성 대기
            var app = AppRoot.Instance; // 실제 회차 관리자
            typeof(AppRoot).GetProperty("Gold").SetValue(app, 30); // 시험 선택에 필요한 초기 골드 준비
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 이동 관리자
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 격자 플레이어
            var target = travel.TrialEventRoom; // 실제 시험 이벤트 방
            Assert.That(target, Is.Not.Null); // 이벤트 방 배치 확인
            foreach (var door in FindRoute(travel.Layout, travel.Layout.GetRoom(travel.RoomX, travel.RoomY), target)) // 실제 이벤트 방 경로 순회
            { // 실제 이동 시작
                yield return UseDoor(travel, player, door); // 실제 문 입력으로 방 이동
            } // 실제 이동 끝
            yield return WaitForObject("TrialEventScreen"); // 자동 이벤트 화면 대기
            var index = System.Array.IndexOf(app.CurrentEvent.choices, 0); // 저장된 순서의 조사 행동
            var button = GameObject.Find("TrialEventChoice_" + index).GetComponent<Button>(); // 실제 조사 버튼
            Assert.That(button.interactable, Is.True); // 충분한 골드의 선택 활성화
            button.onClick.Invoke(); // 실제 조사 행동 선택
            yield return null; // 선택과 저장 반영 대기
            var selected = app.CurrentEvent; // 확정된 실제 이벤트 결과
            Assert.That(selected.phase, Is.EqualTo(1)); // 결과 확인 단계
            Assert.That(selected.result, Is.EqualTo(TrialEventRules.Success)); // 동점 판정 성공
            Assert.That(selected.cost, Is.EqualTo(TrialEventRules.TrialCost)); // 실제 차감 비용
            Assert.That(selected.rewardGold, Is.EqualTo(TrialEventRules.TrialSuccessRewardGold)); // 실제 성공 보상
            var expectedGold = 30 - selected.cost + selected.rewardGold; // 선택 비용과 보상을 반영한 잔액
            Assert.That(app.Gold, Is.EqualTo(expectedGold)); // 실제 지급 잔액 확인
            var transaction = selected.transactionId; // 확정된 거래 식별자
            var run = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 결과 저장 파일 조회
            Assert.That(run.gold, Is.EqualTo(expectedGold)); // 결과 연출 전 잔액 저장 확인
            var originalRoomX = run.roomX; // 실제 이벤트 방 좌표 보관
            run.roomX++; // 결과 확인 전 다른 방으로 변경
            Assert.That(RunRestoreRules.IsValid(run), Is.False); // 미확인 결과의 다른 방 복원 차단
            run.roomX = originalRoomX; // 실제 저장 위치 복구
            Assert.That(run.trialEvents.Single(item => item.floor == travel.Floor && item.x == target.X && item.y == target.Y).eventData.transactionId, Is.EqualTo(transaction)); // 저장된 동일 거래 확인
            AppRoot.TrialEventRollOverride = (minimum, maximum) => throw new System.InvalidOperationException("A stored result must not be rerolled."); // 재개 시 판정 재추첨 차단 검증
            Object.Destroy(app.gameObject); // 결과 화면에서 실행 중단 모사
            yield return null; // 기존 게임 객체 정리
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 시작 씬 재실행
            yield return WaitForPhase(GamePhase.Title); // 타이틀 준비 대기
            app = AppRoot.Instance; // 재시작한 실제 관리자
            Assert.That(app.ContinueRun(), Is.True); // 저장된 회차 재개
            yield return WaitForObject("TrialEventConfirm"); // 저장된 결과 확인 화면 대기
            Assert.That(app.Gold, Is.EqualTo(expectedGold)); // 재개 후 중복 지급 없는 잔액
            Assert.That(app.CurrentEvent.transactionId, Is.EqualTo(transaction)); // 동일한 결과 거래 유지
            Assert.That(app.CurrentEvent.result, Is.EqualTo(TrialEventRules.Success)); // 저장된 성공 결과 유지
            GameObject.Find("TrialEventConfirm").GetComponent<Button>().onClick.Invoke(); // 실제 결과 확인
            yield return WaitForPhase(GamePhase.Exploration); // 실제 탐험 복귀
            Assert.That(app.Gold, Is.EqualTo(expectedGold)); // 결과 확인의 중복 지급 차단
        } // 검증 끝
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
