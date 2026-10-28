using System.Collections; // 프레임 기반 대기
using System.Linq; // 방 구조 비교
using System.Reflection; // 실제 방 연결 조회
using NUnit.Framework; // 검증 도구
using ProjectDelta.Core; // 순수 던전 구조
using ProjectDelta.Runtime; // 실제 던전 실행 구성
using UnityEngine; // 씬 객체와 좌표
using UnityEngine.InputSystem; // 가상 키보드 입력
using UnityEngine.InputSystem.LowLevel; // 키보드 상태 설정
using UnityEngine.SceneManagement; // 실제 씬 전환
using UnityEngine.TestTools; // 프레임 기반 검증
using UnityEngine.Tilemaps; // 실제 출구와 도착 칸 검사
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class FloorActivationProbe : MonoBehaviour // 방 활성화 시점 검증
    { // 클래스 시작
        public static int Activations; // 방 활성화 횟수
        private void OnEnable() // 실제 활성화 알림
        { // 알림 시작
            Activations++; // 활성화 횟수 증가
        } // 알림 끝
    } // 클래스 끝
    public sealed class DungeonRuntimeGenerationTests // 생성된 실제 던전 검증
    { // 클래스 시작
        private Keyboard keyboard; // 검증용 키보드
        private GameObject testPrototype; // 오류 경로 검증용 방 객체
        private GameObject testController; // 오류 경로 검증용 제어 객체
        private InputSettings.BackgroundBehavior originalBackground; // 기존 백그라운드 입력 설정
#if UNITY_EDITOR // 편집기 입력 설정 보관
        private InputSettings.EditorInputBehaviorInPlayMode originalEditor; // 기존 편집기 입력 설정
#endif // 편집기 설정 끝
        [SetUp] // 검증 준비 표시
        public void PrepareInput() // 가상 입력을 실제 게임에 전달
        { // 준비 시작
            originalBackground = InputSystem.settings.backgroundBehavior; // 기존 입력 설정 보관
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 자동 검증 포커스 영향 제외
#if UNITY_EDITOR // 편집기 입력 설정 시작
            originalEditor = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 설정 보관
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 게임 화면에 검증 입력 전달
#endif // 편집기 입력 설정 끝
        } // 준비 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator GeneratedRooms_HaveUsableDoorsAndCorrectMiniMap() // 생성된 모든 방과 문 연결 검증
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 42; // 재현 가능한 생성 시드
            yield return EnterDungeon(); // 실제 던전 진입
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 이동 조회
            var layout = travel.Layout; // 실제 이동과 지도가 공유하는 구조
            Assert.That(layout.Rooms.Count, Is.InRange(11, 13)); // 실제 생성된 전체 방 수
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 방 객체 조회 메서드
            Assert.That(finder, Is.Not.Null); // 실제 방 조회 기능 확인
            foreach (var door in layout.Doors) // 생성된 모든 양방향 문 순회
            { // 문 검사 시작
                var sourceArguments = new object[] // 출발 방 조회 인수
                { // 인수 목록 시작
                    door.RoomX, door.RoomY // 출발 방 좌표
                }; // 인수 목록 끝
                var targetArguments = new object[] // 도착 방 조회 인수
                { // 인수 목록 시작
                    door.TargetRoomX, door.TargetRoomY // 도착 방 좌표
                }; // 인수 목록 끝
                var source = (GameObject)finder.Invoke(travel, sourceArguments); // 출발 방 객체 조회
                var target = (GameObject)finder.Invoke(travel, targetArguments); // 도착 방 객체 조회
                Assert.That(source, Is.Not.Null); // 출발 방 인스턴스 존재
                Assert.That(target, Is.Not.Null); // 도착 방 인스턴스 존재
                var floor = source.transform.Find("Floor").GetComponent<Tilemap>(); // 출발 방의 바닥
                var obstacles = source.transform.Find("Obstacles").GetComponent<Tilemap>(); // 출발 방의 장애물
                var approach = new Vector3Int(door.ApproachX, door.ApproachY, 0); // 실제 문 앞 칸
                var markerCell = approach + Offset(door.Direction); // 문 표시가 놓일 칸
                Assert.That(floor.HasTile(approach) && !obstacles.HasTile(approach), Is.True); // 출발 문 앞 칸 통행 확인
                Assert.That(floor.HasTile(markerCell) && obstacles.HasTile(markerCell), Is.True); // 실제 문 칸 차단 확인
                var markerFound = false; // 출구 표시 존재 여부
                foreach (Transform child in source.transform) // 출발 방 객체 순회
                { // 표시 검사 시작
                    var expectedPosition = floor.transform.TransformPoint(new Vector3(markerCell.x + 0.5f, markerCell.y + 0.5f, 0f)); // 시험 격자의 실제 문 중심
                    if (child.name == "DoorMarker" && child.gameObject.activeSelf && Vector3.Distance(child.position, expectedPosition) < 0.01f) // 문 칸의 활성 표시 검사
                    { // 표시 일치 조건 시작
                        markerFound = true; // 실제 문 표시 확인
                    } // 표시 일치 조건 끝
                } // 표시 검사 끝
                Assert.That(markerFound, Is.True, "Generated exit must have a visible marker."); // 실제 문 표시 누락 방지
                var targetFloor = target.transform.Find("Floor").GetComponent<Tilemap>(); // 도착 방의 바닥
                var targetObstacles = target.transform.Find("Obstacles").GetComponent<Tilemap>(); // 도착 방의 장애물
                var arrival = new Vector3Int(door.TargetX, door.TargetY, 0); // 생성된 도착 칸
                Assert.That(targetFloor.HasTile(arrival) && !targetObstacles.HasTile(arrival), Is.True); // 안전한 실제 도착 칸 확인
            } // 문 검사 끝
            foreach (var room in layout.Rooms) // 생성된 방의 지도 표시 검사
            { // 지도 검사 시작
                var visible = travel.Discovery.GetVisibility(room.X, room.Y) != RoomVisibility.Hidden; // 플레이어에게 공개된 방 여부
                var image = GameObject.Find("DungeonMiniMap/MapContent/Room_" + room.X + "_" + room.Y); // 실제 미니맵 방 칸
                Assert.That(image != null, Is.EqualTo(visible)); // 생성된 구조와 지도 공개 상태 일치
            } // 지도 검사 끝
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator NewGames_UseSeededLayoutsAndResetVisits() // 실제 새 게임 시드와 방문 기록 검증
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 1; // 첫 번째 검증 시드
            yield return EnterDungeon(); // 첫 번째 던전 진입
            var first = Signature(GameObject.Find("RoomTravel").GetComponent<RoomTravelController>().Layout); // 첫 구조 보관
            AppRoot.Instance.ReturnToTitle(); // 타이틀 복귀
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            RoomTravelController.GenerationSeedOverride = 2; // 다른 검증 시드
            AppRoot.Instance.StartNewGame(); // 두 번째 던전 시작
            yield return WaitForPhase(GamePhase.Exploration); // 두 번째 던전 진입 대기
            var secondTravel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 두 번째 던전 조회
            var second = Signature(secondTravel.Layout); // 두 번째 구조 보관
            Assert.That(second, Is.Not.EqualTo(first)); // 다른 시드의 구조 차이
            Assert.That(secondTravel.Discovery.GetVisibility(0, 0), Is.EqualTo(RoomVisibility.Visited)); // 새 시작 방 방문 처리
            Assert.That(secondTravel.Layout.Rooms.Count(room => secondTravel.Discovery.GetVisibility(room.X, room.Y) == RoomVisibility.Visited), Is.EqualTo(1)); // 이전 방문 기록 초기화
            AppRoot.Instance.ReturnToTitle(); // 타이틀 재복귀
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            RoomTravelController.GenerationSeedOverride = 1; // 첫 번째 검증 시드 재사용
            AppRoot.Instance.StartNewGame(); // 같은 시드의 던전 시작
            yield return WaitForPhase(GamePhase.Exploration); // 재생성 던전 진입 대기
            Assert.That(Signature(GameObject.Find("RoomTravel").GetComponent<RoomTravelController>().Layout), Is.EqualTo(first)); // 실제 던전 구조 재현 확인
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator MiniMap_DoesNotRevealHiddenLayoutThroughScale() // 숨겨진 방이 지도 크기에 미치는 영향 차단
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 첫 번째 검증 시드
            yield return EnterDungeon(); // 첫 번째 던전 진입
            var first = GameObject.Find("DungeonMiniMap/MapContent/Room_0_0").GetComponent<RectTransform>(); // 첫 지도 시작 방
            var firstPosition = first.anchoredPosition; // 공개된 시작 방 표시 위치
            var firstSize = first.sizeDelta; // 공개된 시작 방 표시 크기
            Assert.That(GameObject.Find("DungeonMiniMap/MapContent/Room_1_0"), Is.Not.Null); // 동일한 첫 출구 확인
            AppRoot.Instance.ReturnToTitle(); // 타이틀 복귀
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            RoomTravelController.GenerationSeedOverride = 4; // 숨겨진 경로가 다른 시드
            AppRoot.Instance.StartNewGame(); // 두 번째 던전 시작
            yield return WaitForPhase(GamePhase.Exploration); // 두 번째 던전 진입 대기
            var second = GameObject.Find("DungeonMiniMap/MapContent/Room_0_0").GetComponent<RectTransform>(); // 두 번째 지도의 시작 방
            Assert.That(GameObject.Find("DungeonMiniMap/MapContent/Room_1_0"), Is.Not.Null); // 동일한 공개 출구 확인
            Assert.That(second.anchoredPosition, Is.EqualTo(firstPosition)); // 비공개 배치가 지도 위치를 바꾸지 않음
            Assert.That(second.sizeDelta, Is.EqualTo(firstSize)); // 비공개 배치가 지도 크기를 바꾸지 않음
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator GeneratedRoom_CanBeEnteredAndExitedUsingRealInput() // 새로 복제된 방의 실제 왕복 이동
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 최초 주 경로
            yield return EnterDungeon(); // 실제 던전 진입
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증용 실제 키보드 추가
            InputSystem.Update(); // 입력 장치 연결 반영
            yield return null; // 입력 준비 대기
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 이동 플레이어
            yield return UseDoor(travel, player, travel.Layout.GetConnection(0, 0, 1, 0)); // 첫 번째 방 연결 통과
            yield return UseDoor(travel, player, travel.Layout.GetConnection(1, 0, 1, 1)); // 두 번째 방의 위쪽 연결 통과
            var generatedDoor = travel.Layout.Doors.First(door => door.RoomX == 1 && door.RoomY == 1 && !(door.TargetRoomX == 1 && door.TargetRoomY == 0)); // 세 번째 방의 새로운 출구
            var targetX = generatedDoor.TargetRoomX; // 생성된 목적 방 가로 좌표
            var targetY = generatedDoor.TargetRoomY; // 생성된 목적 방 세로 좌표
            yield return UseDoor(travel, player, generatedDoor); // 실제 입력으로 생성된 방 진입
            Assert.That(travel.RoomX, Is.EqualTo(targetX)); // 생성된 방 가로 좌표 확인
            Assert.That(travel.RoomY, Is.EqualTo(targetY)); // 생성된 방 세로 좌표 확인
            Assert.That(travel.Discovery.GetVisibility(targetX, targetY), Is.EqualTo(RoomVisibility.Visited)); // 실제 도착한 방 방문 처리
            yield return UseDoor(travel, player, travel.Layout.GetConnection(targetX, targetY, 1, 1)); // 실제 입력으로 이전 방 복귀
            Assert.That(travel.RoomX, Is.EqualTo(1)); // 이전 방 가로 좌표 복구
            Assert.That(travel.RoomY, Is.EqualTo(1)); // 이전 방 세로 좌표 복구
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator InvalidPrototype_LeavesNoTemporaryRoomsOrChangedSource() // 생성 실패 시 원본과 임시 객체 보존
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 시작 방 출구
            yield return EnterDungeon(); // 실제 던전 진입
            var source = GameObject.Find("DungeonGrid"); // 기존 시작 방 조회
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어 조회
            testPrototype = Object.Instantiate(source); // 검증용 방 복제
            testPrototype.name = "ConflictPrototype"; // 검증용 방 이름
            var floor = testPrototype.transform.Find("Floor").GetComponent<Tilemap>(); // 검증 방 바닥 조회
            var obstacles = testPrototype.transform.Find("Obstacles").GetComponent<Tilemap>(); // 검증 방 장애물 조회
            var cell = new Vector3Int(6, 0, 0); // 생성될 오른쪽 문 칸
            var customTile = floor.GetTile(Vector3Int.zero); // 사용자 타일 대체 객체
            obstacles.SetTile(cell, customTile); // 문 칸의 사용자 타일 모사
            var marker = testPrototype.transform.Find("DoorMarker").gameObject; // 기존 문 표시 보관
            var rooms = new[] // 검증용 시작 방 연결
            { // 방 목록 시작
                new RoomBinding(0, 0, false, testPrototype) // 별도 검증용 방
            }; // 방 목록 끝
            var doors = new[] // 검증용 기존 출구
            { // 출구 목록 시작
                new DoorBinding(0, 0, 5, 0, MoveDirection.Right, 1, 0, -5, 0) // 사용자 타일과 충돌할 출구
            }; // 출구 목록 끝
            Assert.Throws<System.ArgumentException>(() => GeneratedRoomFactory.Create(DungeonGenerator.Generate(0), rooms, doors, player)); // 충돌된 방 템플릿 거절
            yield return null; // 임시 객체 정리 대기
            Assert.That(obstacles.GetTile(cell), Is.SameAs(customTile)); // 사용자 타일 보존
            Assert.That(marker.activeSelf, Is.True); // 기존 문 표시 보존
            Assert.That(SceneManager.GetActiveScene().GetRootGameObjects().Any(root => root.name == "RoomTemplateRuntime"), Is.False); // 실패한 임시 방 정리
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator InvalidTravelState_DoesNotCrashDungeonScreen() // 생성 실패 후 화면 오류 방지
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 시작 구조
            yield return EnterDungeon(); // 실제 던전 진입
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 이동 객체
            travel.enabled = false; // 잘못된 이동 입력 차단
            typeof(RoomTravelController).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(travel, null); // 실패한 방 상태 모사
            typeof(RoomTravelController).GetField("<Discovery>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(travel, null); // 실패한 발견 상태 모사
            var extraScreen = new GameObject("FailureScreen", typeof(DungeonScreenView)); // 오류 상태의 던전 화면 생성
            yield return null; // 실제 화면 초기화 대기
            Assert.That(extraScreen, Is.Not.Null); // 추가 예외 없는 화면 유지
            Object.Destroy(extraScreen); // 검증용 화면 정리
        } // 검증 끝
        [UnityTest] // 계단을 통한 실제 층 이동 검증
        public IEnumerator Stairs_AdvancesFloor_AndResetsDiscovery() // 실제 계단 입력과 새 층 확인
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 1층 시드
            yield return EnterDungeon(); // 실제 던전 진입
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증용 키보드 연결
            InputSystem.Update(); // 장치 연결 반영
            yield return null; // 입력 준비 대기
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 방 이동 제어 조회
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어 조회
            var floorProperty = typeof(RoomTravelController).GetProperty("Floor"); // 층 번호 표시 조회
            Assert.That(floorProperty, Is.Not.Null); // 층 번호 제공 확인
            Assert.That(floorProperty.GetValue(travel), Is.EqualTo(1)); // 첫 층 확인
            var oldLayout = travel.Layout; // 기존 층 구조 보관
            var oldDiscovery = travel.Discovery; // 기존 방문 기록 보관
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 계단이 아닌 방 상호작용
            InputSystem.Update(); // 상호작용 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            InputSystem.Update(); // 해제 반영
            Assert.That(floorProperty.GetValue(travel), Is.EqualTo(1)); // 일반 방의 층 이동 차단
            for (var index = 1; index <= 6; index++) // 시작부터 계단 방까지 주 경로 순회
            { // 경로 이동 시작
                var target = oldLayout.Rooms[index]; // 다음 주 경로 방
                var door = oldLayout.GetConnection(travel.RoomX, travel.RoomY, target.X, target.Y); // 다음 방 연결
                Assert.That(door, Is.Not.Null); // 주 경로 연결 확인
                yield return UseDoor(travel, player, door); // 실제 키 입력으로 방 이동
            } // 경로 이동 끝
            Assert.That(oldLayout.GetRoom(travel.RoomX, travel.RoomY).Kind, Is.EqualTo(RoomKind.Stairs)); // 계단 방 도착 확인
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 현재 방 조회
            var roomArguments = new object[] // 현재 방 좌표 인수
            { // 인수 목록 시작
                travel.RoomX, travel.RoomY // 현재 방 좌표
            }; // 인수 목록 끝
            var root = (GameObject)finder.Invoke(travel, roomArguments); // 계단 방 객체
            Assert.That(root.transform.Find("StairMarker"), Is.Not.Null); // 계단 표시 존재 확인
            var floor = root.transform.Find("Floor").GetComponent<Tilemap>(); // 계단 방 바닥
            var obstacles = root.transform.Find("Obstacles").GetComponent<Tilemap>(); // 계단 방 장애물
            Assert.That(player.ChangeRoom(floor, obstacles, 0, 1), Is.True); // 계단이 아닌 칸 배치
            yield return null; // 방 이동 프레임 종료
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 계단 외 위치의 상호작용
            InputSystem.Update(); // 상호작용 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            InputSystem.Update(); // 해제 반영
            Assert.That(floorProperty.GetValue(travel), Is.EqualTo(1)); // 계단 외 위치에서 이동 차단
            Assert.That(player.ChangeRoom(floor, obstacles, 0, 0), Is.True); // 계단 칸 배치
            var savedField = typeof(RoomTravelController).GetField("templates", BindingFlags.Instance | BindingFlags.NonPublic); // 새 층 원형 조회
            var savedRooms = (RoomBinding[])savedField.GetValue(travel); // 보관된 방 원형
            var savedStart = savedRooms.First(room => room.X == 0 && room.Y == 0).Root; // 시작 방 원형 조회
            FloorActivationProbe.Activations = 0; // 방 활성화 횟수 초기화
            savedStart.AddComponent<FloorActivationProbe>(); // 활성화 시점 관찰 연결
            yield return null; // 방 이동 프레임 종료
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 실제 계단 상호작용 입력
            InputSystem.Update(); // 상호작용 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            InputSystem.Update(); // 해제 반영
            travel.SendMessage("OnInteraction"); // 같은 프레임 중복 요청
            yield return null; // 화면 갱신 대기
            Assert.That(floorProperty.GetValue(travel), Is.EqualTo(2)); // 다음 층과 중복 차단 확인
            Assert.That(FloorActivationProbe.Activations, Is.EqualTo(1)); // 새 층 확정 전 활성화 방지
            Assert.That(travel.Layout, Is.Not.SameAs(oldLayout)); // 새로운 층 구조 확인
            Assert.That(travel.Discovery, Is.Not.SameAs(oldDiscovery)); // 독립된 방문 기록 확인
            Assert.That(travel.RoomX, Is.EqualTo(0)); // 새 층 시작 방 가로 좌표
            Assert.That(travel.RoomY, Is.EqualTo(0)); // 새 층 시작 방 세로 좌표
            Assert.That(player.X, Is.EqualTo(0)); // 새 층 시작 칸 가로 좌표
            Assert.That(player.Y, Is.EqualTo(0)); // 새 층 시작 칸 세로 좌표
            Assert.That(travel.Layout.Rooms.Count(room => travel.Discovery.GetVisibility(room.X, room.Y) == RoomVisibility.Visited), Is.EqualTo(1)); // 새 층 시작 방만 방문 확인
            var status = GameObject.Find("DungeonCanvas/Status").GetComponent<UnityEngine.UI.Text>(); // 실제 상태 안내 조회
            Assert.That(status.text, Does.Contain("2층")); // 현재 층 표시 확인
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 화면 캡처 경로 조회
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 검증 캡처 검사
            { // 캡처 조건 시작
                var capture = typeof(RoomTravelTests).GetMethod("CaptureOffscreen", BindingFlags.Static | BindingFlags.NonPublic); // 실제 카메라 캡처 도구 조회
                var image = (Texture2D)capture.Invoke(null, null); // 실제 새 층 화면 렌더링
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 플레이 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
            travel.SendMessage("TryNextFloor"); // 미정인 추가 층 전환 요청
            Assert.That(travel.Floor, Is.EqualTo(2)); // 3층 전환 차단 확인
        } // 검증 끝
        [UnityTest] // 새 층 준비 실패와 재시도 검증
        public IEnumerator Stairs_BlockedStart_PreservesFloorAndCanRetry() // 시작 칸 차단 시 이전 층 보존
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 방 구조
            yield return EnterDungeon(); // 실제 던전 진입
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증용 키보드 연결
            InputSystem.Update(); // 장치 연결 반영
            yield return null; // 입력 준비 대기
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var oldLayout = travel.Layout; // 기존 층 구조 보관
            var oldDiscovery = travel.Discovery; // 기존 방문 기록 보관
            for (var index = 1; index <= 6; index++) // 계단 방까지 주 경로 순회
            { // 경로 이동 시작
                var target = oldLayout.Rooms[index]; // 다음 경로 방
                yield return UseDoor(travel, player, oldLayout.GetConnection(travel.RoomX, travel.RoomY, target.X, target.Y)); // 실제 입력으로 방 이동
            } // 경로 이동 끝
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 방 조회
            var roomArguments = new object[] // 현재 방 좌표 인수
            { // 인수 목록 시작
                travel.RoomX, travel.RoomY // 현재 방 좌표
            }; // 인수 목록 끝
            var root = (GameObject)finder.Invoke(travel, roomArguments); // 현재 계단 방
            var floor = root.transform.Find("Floor").GetComponent<Tilemap>(); // 계단 방 바닥
            var obstacles = root.transform.Find("Obstacles").GetComponent<Tilemap>(); // 계단 방 장애물
            Assert.That(player.ChangeRoom(floor, obstacles, 0, 0), Is.True); // 계단 칸 배치
            var templatesField = typeof(RoomTravelController).GetField("templates", BindingFlags.Instance | BindingFlags.NonPublic); // 새 층 원형 조회
            var templates = (RoomBinding[])templatesField.GetValue(travel); // 보관된 방 원형
            var start = templates.First(room => room.X == 0 && room.Y == 0).Root; // 새 층의 시작 방 원형
            var startObstacles = start.transform.Find("Obstacles").GetComponent<Tilemap>(); // 원형 장애물
            startObstacles.SetTile(Vector3Int.zero, floor.GetTile(Vector3Int.zero)); // 새 시작 칸 차단 모사
            yield return null; // 방 전환 프레임 종료
            LogAssert.Expect(LogType.Warning, "Floor transition failed: The stair cell must be walkable."); // 예상 실패 기록
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 계단 상호작용
            InputSystem.Update(); // 실패 요청 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 실패한 방 정리 대기
            Assert.That(travel.Floor, Is.EqualTo(1)); // 기존 층 번호 유지
            Assert.That(travel.Layout, Is.SameAs(oldLayout)); // 기존 층 구조 유지
            Assert.That(travel.Discovery, Is.SameAs(oldDiscovery)); // 기존 방문 기록 유지
            Assert.That(root.activeInHierarchy, Is.True); // 기존 계단 방 표시 유지
            Assert.That(player.X, Is.EqualTo(0)); // 기존 가로 칸 유지
            Assert.That(player.Y, Is.EqualTo(0)); // 기존 세로 칸 유지
            startObstacles.SetTile(Vector3Int.zero, null); // 차단 원인 제거
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 계단 재시도
            InputSystem.Update(); // 재시도 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 새 층 표시 대기
            Assert.That(travel.Floor, Is.EqualTo(2)); // 실패 후 정상 재시도 확인
        } // 검증 끝
        [UnityTest] // 최초 생성 실패 복구 검증
        public IEnumerator BlockedStairTemplate_DoesNotChangeOriginalRooms() // 계단 칸 충돌 시 기존 방 보존
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 계단 배치
            yield return EnterDungeon(); // 실제 던전 진입
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 기존 방 제어 조회
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 기존 플레이어 조회
            var savedField = typeof(RoomTravelController).GetField("templates", BindingFlags.Instance | BindingFlags.NonPublic); // 원래 방 원형 조회
            var savedRooms = (RoomBinding[])savedField.GetValue(travel); // 기존 방 원형 목록
            var doorsField = typeof(RoomTravelController).GetField("templateDoors", BindingFlags.Instance | BindingFlags.NonPublic); // 원래 문 연결 조회
            var savedDoors = (DoorBinding[])doorsField.GetValue(travel); // 원래 문 연결 목록
            testPrototype = new GameObject("BlockedTemplates"); // 별도 시험 원형 상위 객체
            testPrototype.SetActive(false); // 시험 방 표시 방지
            var originals = new RoomBinding[savedRooms.Length]; // 시험용 원본 방 목록
            for (var index = 0; index < savedRooms.Length; index++) // 보관된 원형 순회
            { // 시험 원형 준비 시작
                var saved = savedRooms[index]; // 기존 방 원형
                var copy = Object.Instantiate(saved.Root, testPrototype.transform); // 독립 시험 원형 복제
                var floor = copy.transform.Find("Floor").GetComponent<Tilemap>(); // 시험 바닥 타일맵
                var obstacles = copy.transform.Find("Obstacles").GetComponent<Tilemap>(); // 시험 장애물 타일맵
                obstacles.SetTile(Vector3Int.zero, floor.GetTile(Vector3Int.zero)); // 각 방의 계단 칸 충돌 모사
                originals[index] = new RoomBinding(saved.X, saved.Y, saved.IsSecret, copy); // 시험 방 좌표 연결
            } // 시험 원형 준비 끝
            var start = originals.First(room => room.X == 0 && room.Y == 0).Root; // 시험 시작 방
            var startObstacles = start.transform.Find("Obstacles").GetComponent<Tilemap>(); // 기존 출구 타일맵
            var existingTile = startObstacles.GetTile(new Vector3Int(6, 0, 0)); // 기존 출구 타일 보관
            var existingMarker = start.transform.Find("DoorMarker").gameObject; // 기존 문 표시 보관
            var generatedBefore = SceneManager.GetActiveScene().GetRootGameObjects().Count(root => root.name.StartsWith("GeneratedRoom_")); // 기존 생성 방 수
            testController = new GameObject("BlockedController"); // 독립 방 제어 객체
            testController.SetActive(false); // 구성 전 초기화 방지
            var controller = testController.AddComponent<RoomTravelController>(); // 시험용 방 제어 추가
            controller.Configure(originals, savedDoors, player); // 기존 형식의 방과 문 연결
            LogAssert.Expect(LogType.Error, "Invalid dungeon layout: The stair cell must be walkable."); // 충돌 감지 기록
            testController.SetActive(true); // 충돌한 방 구조 초기화
            yield return null; // 실패한 임시 객체 정리 대기
            Assert.That(controller.IsReady, Is.False); // 충돌한 방 구조 사용 방지
            Assert.That(startObstacles.GetTile(new Vector3Int(6, 0, 0)), Is.SameAs(existingTile)); // 기존 문 타일 보존
            Assert.That(existingMarker.activeSelf, Is.True); // 기존 문 표시 보존
            Assert.That(SceneManager.GetActiveScene().GetRootGameObjects().Count(root => root.name.StartsWith("GeneratedRoom_")), Is.EqualTo(generatedBefore)); // 실패한 생성 방 누출 방지
        } // 검증 끝
        [UnityTearDown] // 검증 후 정리 표시
        public IEnumerator Cleanup() // 검증 시드와 공통 객체 정리
        { // 정리 시작
            RoomTravelController.GenerationSeedOverride = null; // 검증 시드 해제
            if (testController != null) // 검증용 제어 객체 존재 검사
            { // 제어 객체 정리 시작
                Object.Destroy(testController); // 검증용 제어 객체 제거
            } // 제어 객체 정리 끝
            if (testPrototype != null) // 검증용 방 존재 검사
            { // 검증 방 정리 시작
                Object.Destroy(testPrototype); // 검증용 방 제거
            } // 검증 방 정리 끝
            if (keyboard != null && keyboard.added) // 검증 키보드 존재 검사
            { // 키보드 정리 시작
                InputSystem.RemoveDevice(keyboard); // 검증용 키보드 제거
            } // 키보드 정리 끝
            if (AppRoot.Instance != null) // 공통 객체 존재 검사
            { // 정리 조건 시작
                Object.Destroy(AppRoot.Instance.gameObject); // 검증용 공통 객체 제거
            } // 정리 조건 끝
            yield return null; // 객체 제거 완료 대기
            InputSystem.settings.backgroundBehavior = originalBackground; // 기존 백그라운드 입력 설정 복구
#if UNITY_EDITOR // 편집기 입력 설정 복구
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditor; // 기존 편집기 설정 복구
#endif // 편집기 입력 설정 끝
        } // 정리 끝
        private IEnumerator UseDoor(RoomTravelController travel, GridMoveController player, RoomDoor door) // 실제 키 입력으로 지정 출구 통과
        { // 출구 이용 시작
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
        private static string Signature(DungeonLayout layout) // 실제 방 구조 비교
        { // 비교 시작
            return string.Join("|", layout.Rooms.Select(room => room.X + "," + room.Y + ":" + room.Kind).OrderBy(value => value)); // 정렬된 좌표와 역할
        } // 비교 끝
    } // 클래스 끝
} // 이름 공간 끝
