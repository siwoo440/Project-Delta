using System.Collections; // 프레임 대기
using System.Collections.Generic; // 실제 방 경로
using System.Linq; // 층별 저장 조회
using System.Reflection; // 방과 검증 재화 조회
using NUnit.Framework; // 실제 게임 검증
using ProjectDelta.Core; // 던전과 상점 데이터
using ProjectDelta.Runtime; // 실제 실행 흐름
using UnityEngine; // 게임 객체와 시간
using UnityEngine.InputSystem; // 검증용 키보드
using UnityEngine.InputSystem.LowLevel; // 검증용 키 입력
using UnityEngine.SceneManagement; // 실제 게임 재시작
using UnityEngine.TestTools; // 프레임 기반 검증
using UnityEngine.Tilemaps; // 실제 방 타일
using UnityEngine.UI; // 실제 상인 버튼
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TrialNpcPlayModeTests // 실제 상인과 층별 상점 검증
    { // 클래스 시작
        private Keyboard keyboard; // 독립 검증 키보드
        private InputSettings.BackgroundBehavior originalBackground; // 기존 입력 설정
#if UNITY_EDITOR // 편집기 입력 설정 조건
        private InputSettings.EditorInputBehaviorInPlayMode originalEditor; // 기존 편집기 설정
#endif // 편집기 설정 끝
        [SetUp] // 실제 상인 검증 준비
        public void Setup() // 독립 저장과 결정적 던전 설정
        { // 준비 시작
            RunStorage.TestDirectoryOverride = System.IO.Path.Combine(Application.dataPath, "..", "Artifacts", "TrialNpcPlayModeTests", System.Guid.NewGuid().ToString("N")); // 독립 회차 저장
            ObservationStorage.TestDirectoryOverride = RunStorage.TestDirectoryOverride; // 독립 관찰 저장
            RoomTravelController.RoomContentTestOverride = true; // 실제 방 콘텐츠 활성화
            RoomTravelController.TrialEventTestOverride = true; // 실제 이벤트 활성화
            RoomTravelController.TutorialEncounterTestOverride = false; // 독립 이동의 첫 전투 제외
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 층 생성
            AppRoot.AutomaticEncounterTestOverride = false; // 검증 전투 연출 제외
            AppRoot.ChestRollOverride = (minimum, maximum) => minimum; // 경유 함정 판정 고정
            originalBackground = InputSystem.settings.backgroundBehavior; // 기존 입력 설정 보관
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 검증 입력 허용
#if UNITY_EDITOR // 편집기 입력 설정 조건
            originalEditor = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 설정 보관
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 실제 게임 입력 허용
#endif // 편집기 입력 설정 끝
            keyboard = InputSystem.AddDevice<Keyboard>(); // 가상 키보드 준비
        } // 준비 끝
        [UnityTearDown] // 검증 후 정리
        public IEnumerator Cleanup() // 게임 객체와 시험 설정 복구
        { // 정리 시작
            RoomTravelController.RoomContentTestOverride = null; // 방 콘텐츠 설정 복구
            RoomTravelController.TrialEventTestOverride = null; // 이벤트 설정 복구
            RoomTravelController.TutorialEncounterTestOverride = null; // 첫 조우 설정 복구
            RoomTravelController.GenerationSeedOverride = null; // 생성 시드 복구
            AppRoot.AutomaticEncounterTestOverride = null; // 전투 연출 설정 복구
            AppRoot.ChestRollOverride = null; // 함정 난수 복구
            if (keyboard != null && keyboard.added) // 검증 키보드 검사
            { // 입력 정리 시작
                InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            } // 입력 정리 끝
            if (AppRoot.Instance != null) // 실제 회차 객체 검사
            { // 회차 정리 시작
                Object.Destroy(AppRoot.Instance.gameObject); // 시험 회차 제거
            } // 회차 정리 끝
            yield return null; // 객체 제거 대기
            RunStorage.TestDirectoryOverride = null; // 회차 저장 경로 복구
            ObservationStorage.TestDirectoryOverride = null; // 관찰 저장 경로 복구
            InputSystem.settings.backgroundBehavior = originalBackground; // 입력 설정 복구
#if UNITY_EDITOR // 편집기 입력 설정 조건
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditor; // 기존 편집기 설정 복구
#endif // 편집기 입력 설정 끝
        } // 정리 끝
        [UnityTest] // 실제 상인 재방문과 저장 재개 검증
        public IEnumerator Merchant_RevisitAndRestartPreserveFloorStock() // 실제 거래 후 같은 층 상점 복원
        { // 검증 시작
            yield return EnterDungeon(); // 실제 새 회차 진입
            var app = AppRoot.Instance; // 실제 회차 관리자
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 층 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var npcRoom = travel.TrialNpcRoom; // 현재 층의 실제 상인 방
            Assert.That(npcRoom, Is.Not.Null); // 상인 방 생성 확인
            var contents = travel.CurrentContent.Snapshot(); // 상자와 함정 위치
            Assert.That(contents.chests.Any(item => item.x == npcRoom.X && item.y == npcRoom.Y), Is.False); // 상자 방과 분리
            Assert.That(contents.trapX == npcRoom.X && contents.trapY == npcRoom.Y, Is.False); // 함정 방과 분리
            var eventRoom = travel.TrialEventRoom; // 현재 층 이벤트 방
            Assert.That(eventRoom, Is.Not.Null); // 실제 이벤트 방 확인
            Assert.That(eventRoom.X == npcRoom.X && eventRoom.Y == npcRoom.Y, Is.False); // 이벤트 방과 분리
            var route = FindTestRoute(travel.Layout, travel.Layout.GetRoom(travel.RoomX, travel.RoomY), npcRoom); // 실제 상인 방 경로
            Assert.That(route.Count, Is.GreaterThan(0)); // 시작 방과 상인 방 분리
            foreach (var door in route) // 상인 방까지 실제 이동
            { // 이동 시작
                yield return UseDoorAndResolveEvent(travel, player, door); // 실제 이동과 경유 이벤트 처리
            } // 이동 끝
            yield return OpenNpc(travel, player); // 인접 상호작용으로 상인 열기
            Assert.That(GameObject.Find("TrialNpcTalk"), Is.Not.Null); // 실제 대화 버튼
            Assert.That(GameObject.Find("TrialNpcShop"), Is.Not.Null); // 실제 상점 버튼
            Assert.That(GameObject.Find("TrialNpcLeave"), Is.Not.Null); // 실제 떠나기 버튼
            var dialogue = GameObject.Find("TrialNpcDialogue").GetComponent<Text>(); // 실제 상인 대사
            var greeting = dialogue.text; // 처음 표시한 대사
            GameObject.Find("TrialNpcTalk").GetComponent<Button>().onClick.Invoke(); // 실제 대화 선택
            Assert.That(dialogue.text, Is.Not.EqualTo(greeting)); // 대화 내용 변경 확인
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 실제 NPC 화면 캡처 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 자동 캡처 요청 검사
            { // 캡처 시작
                var image = CaptureScreen("TrialNpcCanvas"); // 실제 NPC 화면 렌더링
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 화면 저장
                Object.Destroy(image); // 캡처 이미지 제거
            } // 캡처 끝
            typeof(AppRoot).GetProperty("Gold").GetSetMethod(true).Invoke(app, new object[] { 100 }); // 거래 검증용 골드
            GameObject.Find("TrialNpcShop").GetComponent<Button>().onClick.Invoke(); // 실제 상인 상점 열기
            Assert.That(GameObject.Find("EconomyCanvas"), Is.Not.Null); // 상인 상점 화면 확인
            Assert.That(GameObject.Find("TrialNpcScreen"), Is.Null); // 상점 뒤의 대화 입력 차단
            Assert.That(GameObject.Find("TrialRelic_0"), Is.Null); // 상인의 무료 시험 유물 지급 차단
            var offer = app.Shop.Snapshot().offers[0]; // 최초 확정된 상품
            GameObject.Find("ShopOffer_0").GetComponent<Button>().onClick.Invoke(); // 첫 상품 선택
            GameObject.Find("ShopBuyButton").GetComponent<Button>().onClick.Invoke(); // 실제 구매
            Assert.That(app.Shop.Snapshot().offers[0].remaining, Is.EqualTo(offer.remaining - 1)); // 실제 재고 차감
            var expectedGold = 100 - offer.price; // 거래 후 예상 잔액
            Assert.That(app.Gold, Is.EqualTo(expectedGold)); // 실제 골드 차감
            GameObject.Find("CloseEconomyButton").GetComponent<Button>().onClick.Invoke(); // 상점 종료
            yield return null; // 상인 화면 복귀 대기
            GameObject.Find("TrialNpcLeave").GetComponent<Button>().onClick.Invoke(); // 실제 상인 대화 종료
            yield return null; // 탐험 복귀 대기
            var entry = route[route.Count - 1]; // 상인 방 진입 문
            var exit = travel.Layout.GetConnection(npcRoom.X, npcRoom.Y, entry.RoomX, entry.RoomY); // 직전 방 복귀 문
            yield return UseDoorAndResolveEvent(travel, player, exit); // 상인 방 이탈
            yield return UseDoorAndResolveEvent(travel, player, entry); // 같은 상인 방 재방문
            yield return OpenNpc(travel, player); // 같은 상인 상호작용
            GameObject.Find("TrialNpcShop").GetComponent<Button>().onClick.Invoke(); // 같은 층 상점 재방문
            Assert.That(app.Shop.Snapshot().offers[0].price, Is.EqualTo(offer.price)); // 최초 가격 유지
            Assert.That(app.Shop.Snapshot().offers[0].remaining, Is.EqualTo(offer.remaining - 1)); // 거래 후 재고 유지
            GameObject.Find("CloseEconomyButton").GetComponent<Button>().onClick.Invoke(); // 상점 종료
            yield return null; // 상인 화면 복귀 대기
            GameObject.Find("TrialNpcLeave").GetComponent<Button>().onClick.Invoke(); // 실제 상인 떠나기
            yield return null; // 저장 가능한 상태 대기
            Assert.That(app.TrySaveRun(out var message), Is.True, message); // 현재 층 상점과 위치 저장
            Object.Destroy(app.gameObject); // 현재 실행 종료
            yield return null; // 공통 객체 정리 대기
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 새 실행 시작
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            app = AppRoot.Instance; // 새 실행 관리자
            Assert.That(app.ContinueRun(), Is.True); // 회차 저장 재개
            yield return WaitForPhase(GamePhase.Exploration); // 탐험 복원 대기
            travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 복원된 층 제어
            player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 복원된 플레이어
            Assert.That(app.Gold, Is.EqualTo(expectedGold)); // 실제 잔액 복원
            yield return OpenNpc(travel, player); // 복원된 같은 상인 상호작용
            GameObject.Find("TrialNpcShop").GetComponent<Button>().onClick.Invoke(); // 복원 상점 열기
            Assert.That(app.Shop.Snapshot().offers[0].price, Is.EqualTo(offer.price)); // 재실행 후 가격 보존
            Assert.That(app.Shop.Snapshot().offers[0].remaining, Is.EqualTo(offer.remaining - 1)); // 재실행 후 재고 보존
            GameObject.Find("CloseEconomyButton").GetComponent<Button>().onClick.Invoke(); // 상점 종료
            yield return null; // 상인 화면 복귀 대기
            GameObject.Find("TrialNpcLeave").GetComponent<Button>().onClick.Invoke(); // 실제 상인 떠나기
            yield return null; // 층 전환 준비 대기
            travel.SendMessage("TryNextFloor"); // 기존 실제 다음 층 전환
            yield return null; // 다음 층 준비 대기
            Assert.That(travel.Floor, Is.EqualTo(2)); // 두 번째 층 도착
            Assert.That(travel.CanReturnToPreviousRoom(), Is.False); // 이전 층의 상인 방 직접 복귀 차단
            var secondNpc = travel.TrialNpcRoom; // 새 층 상인 방
            Assert.That(secondNpc, Is.Not.Null); // 새 층 상인 생성
            var secondRoute = FindTestRoute(travel.Layout, travel.Layout.GetRoom(travel.RoomX, travel.RoomY), secondNpc); // 새 층 상인 경로
            foreach (var door in secondRoute) // 새 층 실제 이동
            { // 이동 시작
                yield return UseDoorAndResolveEvent(travel, player, door); // 출구 이동과 이벤트 처리
            } // 이동 끝
            yield return OpenNpc(travel, player); // 두 번째 층 상인 열기
            var secondNotice = GameObject.Find("TrialNpcNotice").GetComponent<Text>(); // 상점 오류 안내 보관
            GameObject.Find("TrialNpcShop").GetComponent<Button>().onClick.Invoke(); // 두 번째 층 상점 열기
            Assert.That(GameObject.Find("EconomyCanvas"), Is.Not.Null, secondNotice.text); // 두 번째 층 상점 열기 결과
            Assert.That(app.Shop.Snapshot().floor, Is.EqualTo(2)); // 두 번째 층 상점 분리
            var saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 실제 층별 상점 저장 읽기
            Assert.That(saved.shopStocks.Single(item => item.floor == 1).offers[0].price, Is.EqualTo(offer.price)); // 이전 층의 가격 보존
            Assert.That(saved.shopStocks.Single(item => item.floor == 1).offers[0].remaining, Is.EqualTo(offer.remaining - 1)); // 이전 층의 재고 보존
            Assert.That(saved.shopStocks.Single(item => item.floor == 2).floor, Is.EqualTo(2)); // 새 층의 별도 상점 보관
        } // 검증 끝
        private IEnumerator UseDoorAndResolveEvent(RoomTravelController travel, GridMoveController player, RoomDoor door) // 실제 이동 중 이벤트 처리
        { // 이동 시작
            yield return UseDoor(travel, player, door); // 실제 출구 이동
            yield return null; // 방 콘텐츠 표시 대기
            yield return null; // 이벤트 준비 대기
            if (GameObject.Find("TrialEventCanvas") != null) // 경유 이벤트 화면 검사
            { // 이벤트 처리 시작
                var choices = AppRoot.Instance.CurrentEvent.choices; // 저장된 이벤트 선택 순서
                var leave = System.Array.IndexOf(choices, 1); // 실제 떠나기 위치
                GameObject.Find("TrialEventChoice_" + leave).GetComponent<Button>().onClick.Invoke(); // 이벤트 떠나기 선택
                GameObject.Find("TrialEventConfirm").GetComponent<Button>().onClick.Invoke(); // 결과 확인
                yield return null; // 탐험 복귀 대기
            } // 이벤트 처리 끝
        } // 이동 끝
        private IEnumerator OpenNpc(RoomTravelController travel, GridMoveController player) // 상인 방 중앙 인접 상호작용
        { // 상호작용 시작
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 방 조회
            var root = (GameObject)finder.Invoke(travel, new object[] { travel.RoomX, travel.RoomY }); // 현재 상인 방
            var floor = root.transform.Find("Floor").GetComponent<Tilemap>(); // 상인 방 바닥
            var obstacles = root.transform.Find("Obstacles").GetComponent<Tilemap>(); // 상인 방 장애물
            Assert.That(player.ChangeRoom(floor, obstacles, 0, 1), Is.True); // 상인 인접 위치 배치
            yield return null; // 위치 갱신 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 실제 상호작용 키 누름
            InputSystem.Update(); // 상호작용 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 상호작용 키 해제
            InputSystem.Update(); // 해제 입력 반영
            yield return null; // 상인 화면 생성 대기
            Assert.That(GameObject.Find("TrialNpcShop"), Is.Not.Null); // 실제 상인 화면 확인
        } // 상호작용 끝
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
