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
    public sealed class DungeonRuntimeGenerationTests // 생성된 실제 던전 검증
    { // 클래스 시작
        private Keyboard keyboard; // 검증용 키보드
        private GameObject testPrototype; // 오류 경로 검증용 방 객체
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
        [UnityTearDown] // 검증 후 정리 표시
        public IEnumerator Cleanup() // 검증 시드와 공통 객체 정리
        { // 정리 시작
            RoomTravelController.GenerationSeedOverride = null; // 검증 시드 해제
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
