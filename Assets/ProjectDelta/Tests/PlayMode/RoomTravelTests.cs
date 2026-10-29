using System.Collections; // 프레임 기반 대기
using NUnit.Framework; // 검증 도구
using ProjectDelta.Core; // 게임 상태와 방향
using ProjectDelta.Runtime; // 실제 플레이어와 입력
using UnityEngine; // Unity 객체
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 입력 장치 상태
using UnityEngine.SceneManagement; // 실제 씬 전환
using UnityEngine.TestTools; // 프레임 기반 검증
using UnityEngine.Tilemaps; // 도착 바닥 검증
using UnityEngine.UI; // 실제 지도 색상 검증
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class RoomActivationBlocker : MonoBehaviour // 방 활성화 시 도착 칸 차단 모사
    { // 클래스 시작
        public TileBase Tile; // 도착 칸을 막을 타일
        private void OnEnable() // 방 활성화 처리
        { // 처리 시작
            transform.Find("Obstacles").GetComponent<Tilemap>().SetTile(new Vector3Int(-5, 0, 0), Tile); // 도착 칸에 장애물 추가
        } // 처리 끝
    } // 클래스 끝
    public sealed class RoomTravelTests // 실제 방 이동 검증
    { // 클래스 시작
        private Keyboard keyboard; // 검증용 키보드
        private Gamepad gamepad; // 검증용 게임패드
        private InputSettings.BackgroundBehavior originalBackground; // 기존 백그라운드 입력 설정
#if UNITY_EDITOR // 편집기 설정 보관
        private InputSettings.EditorInputBehaviorInPlayMode originalEditor; // 기존 편집기 입력 설정
#endif // 편집기 설정 끝
        [SetUp] // 검증 준비 표시
        public void PrepareInput() // 입력 포커스 설정
        { // 준비 시작
            RoomTravelController.GenerationSeedOverride = 0; // 기존 방 이동 시험의 재현 가능한 방 구조
            originalBackground = InputSystem.settings.backgroundBehavior; // 기존 설정 보관
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 자동 검증 입력 허용
#if UNITY_EDITOR // 편집기 설정 시작
            originalEditor = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 설정 보관
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 검증 장치 입력 허용
#endif // 편집기 설정 끝
        } // 준비 끝
#if !UNITY_EDITOR // Windows 실행 화면 전용 검증
        [UnityTest] // 실제 지도 표시 검증
        public IEnumerator WindowsPlayer_DisplaysCurrentRoomOnMiniMap() // 실행 파일의 미니맵 표시 확인
        { // 검증 시작
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) // 그래픽 장치 검사
            { // 화면 출력 불가 조건 시작
                Assert.Ignore("A graphics device is required for the rendered-screen check."); // 그래픽 없는 환경 구분
            } // 화면 출력 불가 조건 끝
            yield return EnterDungeon(); // 실제 던전 진입
            var current = GameObject.Find("DungeonMiniMap/MapContent/Room_0_0"); // 현재 방의 지도 칸 조회
            Assert.That(current, Is.Not.Null); // 현재 방 지도 객체 확인
            yield return new WaitForEndOfFrame(); // 실제 화면 그리기 대기
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture(); // 게임 자체 화면 캡처
            Assert.That(screenshot, Is.Not.Null); // 화면 캡처 확인
            var point = RectTransformUtility.WorldToScreenPoint(null, current.transform.TransformPoint(new Vector3(13f, 0f, 0f))); // 방향 표시를 피한 현재 방 픽셀 위치
            var color = screenshot.GetPixel(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y)); // 현재 방 픽셀 조회
            Object.Destroy(screenshot); // 캡처 이미지 메모리 정리
            Assert.That(color.g, Is.GreaterThan(0.3f), "Current minimap room must be visible."); // 실제 화면의 현재 방 표시 확인
            Assert.That(color.g, Is.GreaterThan(color.r * 1.15f), "Current room must use the green highlight."); // 현재 위치 강조색 확인
        } // 검증 끝
        [UnityTest] // 실제 표시 검증
        public IEnumerator WindowsPlayer_DisplaysDoorMarker() // 노란 문 표시 확인
        { // 검증 시작
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) // 그래픽 장치 검사
            { // 화면 출력 불가 조건 시작
                Assert.Ignore("A graphics device is required for the rendered-screen check."); // 그래픽 없는 환경 구분
            } // 화면 출력 불가 조건 끝
            yield return EnterDungeon(); // 실제 던전 진입
            yield return new WaitForEndOfFrame(); // 실제 화면 그리기 대기
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture(); // 게임 자체 화면 캡처
            Assert.That(screenshot, Is.Not.Null); // 화면 캡처 확인
            var floor = GameObject.Find("DungeonGrid/Floor").GetComponent<Tilemap>(); // 첫 방의 바닥 조회
            var point = Camera.main.WorldToScreenPoint(floor.GetCellCenterWorld(new Vector3Int(6, 0, 0))); // 문 중심의 화면 좌표
            var color = screenshot.GetPixel(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y)); // 문 표시 픽셀 조회
            Object.Destroy(screenshot); // 검증용 화면 메모리 정리
            Assert.That(color.r, Is.GreaterThan(0.2f)); // 문 표시의 밝기 확인
            Assert.That(color.g, Is.GreaterThan(0.15f)); // 노란 문 표시 확인
        } // 검증 끝
#endif // Windows 화면 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator DoorInteraction_TravelsBothWaysWithKeyboardAndGamepad() // 키보드와 게임패드 왕복 검증
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            Assert.That(GameObject.Find("RoomTravel"), Is.Not.Null, "Dungeon must contain room travel controller."); // 방 이동 구성 확인
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어 조회
            yield return ApproachEastDoor(player); // 첫 방의 문 앞 이동
            Assert.That(player.X, Is.EqualTo(5)); // 문 앞 가로 칸 확인
            Assert.That(player.Facing, Is.EqualTo(MoveDirection.Right)); // 문을 바라보는 방향 확인
            yield return TapKey(Key.E); // 키보드 상호작용
            Assert.That(GameObject.Find("SecondDungeonGrid"), Is.Not.Null); // 두 번째 방 표시 확인
            Assert.That(GameObject.Find("DungeonGrid"), Is.Null); // 첫 번째 방 표시 종료 확인
            Assert.That(player.X, Is.EqualTo(-5)); // 반대쪽 문 안쪽 가로 칸
            Assert.That(player.Y, Is.EqualTo(0)); // 반대쪽 문 안쪽 세로 칸
            var floor = GameObject.Find("SecondDungeonGrid/Floor").GetComponent<Tilemap>(); // 도착 방 바닥 조회
            Assert.That(floor.HasTile(new Vector3Int(player.X, player.Y, 0)), Is.True); // 도착 칸 바닥 확인
            yield return Move(player, Key.D); // 두 번째 방에서 오른쪽 이동
            yield return Move(player, Key.A); // 왼쪽 문을 바라보며 복귀
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 게임패드 상호작용 입력
            InputSystem.Update(); // 버튼 입력 반영
            yield return null; // 방 전환 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 버튼 입력 해제
            InputSystem.Update(); // 입력 해제 반영
            yield return null; // 화면 갱신 대기
            Assert.That(GameObject.Find("DungeonGrid"), Is.Not.Null); // 첫 번째 방 복귀 확인
            Assert.That(player.X, Is.EqualTo(5)); // 원래 문 안쪽 좌표 확인
            Assert.That(player.Y, Is.EqualTo(0)); // 원래 문 세로 좌표 확인
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator InteractionAwayFromDoorOrWhileMoving_DoesNotChangeRooms() // 잘못된 상호작용 차단 검증
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            Assert.That(GameObject.Find("RoomTravel"), Is.Not.Null, "Dungeon must contain room travel controller."); // 방 이동 구성 확인
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어 조회
            yield return TapKey(Key.E); // 문에서 떨어진 위치의 상호작용
            Assert.That(GameObject.Find("DungeonGrid"), Is.Not.Null); // 기존 방 유지 확인
            yield return ApproachEastDoor(player); // 문 앞까지 이동
            yield return Move(player, Key.A); // 문 반대 방향으로 이동
            yield return Move(player, Key.D); // 다시 문 앞에 도착
            SetKeyboard(Key.A); // 문에서 벗어나는 이동 시작
            SetKeyboard(); // 방향 입력 해제
            SetKeyboard(Key.E); // 이동 중 상호작용 시도
            SetKeyboard(); // 상호작용 해제
            yield return WaitForStopped(player); // 이동 종료 대기
            Assert.That(GameObject.Find("DungeonGrid"), Is.Not.Null); // 이동 중 방 전환 차단
            Assert.That(player.X, Is.EqualTo(4)); // 실제 이동 칸 유지
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator WrongFacingAndBlockedArrival_KeepOriginalRoom() // 방향과 도착 장애물 검사
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어 조회
            yield return ApproachEastDoor(player); // 오른쪽 문 접근
            yield return Move(player, Key.W); // 문 위쪽으로 이동
            yield return Move(player, Key.S); // 문 앞에 아래 방향으로 복귀
            yield return TapKey(Key.E); // 다른 방향의 문 상호작용
            Assert.That(GameObject.Find("DungeonGrid"), Is.Not.Null); // 방향이 다를 때 원래 방 유지
            yield return Move(player, Key.A); // 문에서 한 칸 왼쪽 이동
            yield return Move(player, Key.D); // 문을 바라보며 접근
            GameObject second = null; // 비활성 두 번째 방 보관
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) // 실제 씬 루트 순회
            { // 루트 검사 시작
                if (root.name == "SecondDungeonGrid") // 두 번째 방 이름 검사
                { // 두 번째 방 조건 시작
                    second = root; // 비활성 방 객체 보관
                } // 두 번째 방 조건 끝
            } // 루트 검사 끝
            Assert.That(second, Is.Not.Null); // 두 번째 방 존재 확인
            var obstacles = second.transform.Find("Obstacles").GetComponent<Tilemap>(); // 도착 방 장애물 조회
            var tile = obstacles.GetTile(new Vector3Int(3, 0, 0)); // 기존 시험 장애물 타일
            obstacles.SetTile(new Vector3Int(-5, 0, 0), tile); // 도착 칸을 장애물로 막기
            yield return TapKey(Key.E); // 막힌 방으로 이동 시도
            Assert.That(GameObject.Find("DungeonGrid"), Is.Not.Null); // 도착 칸이 막히면 원래 방 유지
            Assert.That(player.X, Is.EqualTo(5)); // 원래 칸 유지
            Assert.That(player.Y, Is.EqualTo(0)); // 원래 세로 칸 유지
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator SimultaneousDoorAndMove_DoesNotCarryMovementIntoNextRoom() // 방 전환 프레임의 이동 차단
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어 조회
            yield return ApproachEastDoor(player); // 오른쪽 문 접근
            SetKeyboard(Key.E, Key.D); // 문과 방향을 동시에 누름
            SetKeyboard(); // 두 입력 해제
            yield return null; // 방 전환 프레임 종료 대기
            Assert.That(GameObject.Find("SecondDungeonGrid"), Is.Not.Null); // 두 번째 방 진입 확인
            Assert.That(player.X, Is.EqualTo(-5)); // 새 방에 추가 이동이 전달되지 않음
            Assert.That(player.IsMoving, Is.False); // 대기 이동 없음 확인
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator ArrivalBlockedOnActivation_RollsBackRoomTransition() // 방 활성화 시 도착 칸 변경 검증
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어 조회
            yield return ApproachEastDoor(player); // 오른쪽 문 접근
            GameObject second = null; // 비활성 목적 방 보관
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) // 씬 루트 순회
            { // 루트 검사 시작
                if (root.name == "SecondDungeonGrid") // 목적 방 이름 검사
                { // 목적 방 조건 시작
                    second = root; // 비활성 방 보관
                } // 목적 방 조건 끝
            } // 루트 검사 끝
            Assert.That(second, Is.Not.Null); // 목적 방 존재 확인
            var obstacles = second.transform.Find("Obstacles").GetComponent<Tilemap>(); // 목적 방 장애물 조회
            var blocker = second.AddComponent<RoomActivationBlocker>(); // 방 활성화 시 변경 구성
            blocker.Tile = obstacles.GetTile(new Vector3Int(3, 0, 0)); // 기존 장애물 타일 전달
            yield return TapKey(Key.E); // 실제 방 이동 시도
            Assert.That(GameObject.Find("DungeonGrid"), Is.Not.Null); // 실패 시 원래 방 유지 확인
            Assert.That(GameObject.Find("SecondDungeonGrid"), Is.Null); // 실패한 목적 방 비활성 확인
            Assert.That(player.X, Is.EqualTo(5)); // 플레이어의 원래 칸 유지
            Assert.That(GameObject.Find("RoomTravel").GetComponent<RoomTravelController>().RoomX, Is.EqualTo(0)); // 논리 방 좌표 복구 확인
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator MiniMap_RevealsVisitsAndResetsOnNewGame() // 방문·연결 표시와 새 게임 초기화
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어 조회
            var start = GameObject.Find("DungeonMiniMap/MapContent/Room_0_0"); // 시작 방 지도 칸
            var neighbor = GameObject.Find("DungeonMiniMap/MapContent/Room_1_0"); // 연결된 두 번째 방 지도 칸
            Assert.That(start, Is.Not.Null, "Starting room must appear on the minimap."); // 시작 방 표시 확인
            Assert.That(start.transform.Find("CurrentDirection"), Is.Not.Null); // 현재 위치의 방향 표시 확인
            Assert.That(neighbor, Is.Not.Null, "The connected room must appear as an outline."); // 연결 방 표시 확인
            Assert.That(GameObject.Find("DungeonMiniMap/MapContent/Room_1_1"), Is.Null); // 아직 확인하지 않은 세 번째 방 가림
            var connectedColor = neighbor.GetComponent<Image>().color; // 미방문 방 색상 보관
            yield return ApproachEastDoor(player); // 첫 방의 오른쪽 문 접근
            Assert.That(start.transform.Find("CurrentDirection").GetComponent<Text>().text, Is.EqualTo("▶")); // 실제 이동 방향과 지도 방향 일치
            yield return TapKey(Key.E); // 두 번째 방 진입
            Assert.That(GameObject.Find("DungeonMiniMap/MapContent/Room_1_1"), Is.Not.Null); // 새로 확인한 위쪽 방 표시
            Assert.That(GameObject.Find("DungeonMiniMap/MapContent/Room_1_0").GetComponent<Image>().color, Is.Not.EqualTo(connectedColor)); // 방문 방의 표시 변경
            yield return Move(player, Key.W); // 두 번째 방의 위쪽 이동
            yield return Move(player, Key.W); // 장애물 위쪽 경로 진입
            for (var index = 0; index < 7; index++) // 위쪽 문 아래 가로 이동
            { // 반복 시작
                yield return Move(player, Key.D); // 오른쪽 한 칸 이동
            } // 반복 끝
            yield return Move(player, Key.W); // 위쪽 문 앞 접근
            Assert.That(player.X, Is.EqualTo(2)); // 문 앞 가로 좌표 확인
            Assert.That(player.Y, Is.EqualTo(3)); // 문 앞 세로 좌표 확인
            yield return TapKey(Key.E); // 세 번째 방 진입
            Assert.That(GameObject.Find("ThirdDungeonGrid"), Is.Not.Null); // 세 번째 방 표시 확인
            Assert.That(GameObject.Find("RoomTravel").GetComponent<RoomTravelController>().RoomY, Is.EqualTo(1)); // 현재 방 세로 좌표 확인
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 검증 도구에서 전달한 캡처 경로
            if (!string.IsNullOrEmpty(capturePath)) // 스크린샷 저장 요청 검사
            { // 화면 저장 조건 시작
                Texture2D screenshot; // 실제 플레이 화면 이미지
                if (Application.isBatchMode) // 화면 조작 없는 자동 실행 검사
                { // 백그라운드 캡처 시작
                    screenshot = CaptureOffscreen(); // 카메라와 게임 UI의 별도 렌더링
                } // 백그라운드 캡처 끝
                else // 일반 실행 화면 캡처 분기
                { // 일반 화면 조건 시작
                    yield return new WaitForEndOfFrame(); // 실제 화면 그리기 완료 대기
                    screenshot = ScreenCapture.CaptureScreenshotAsTexture(); // 게임 자체 플레이 화면 캡처
                } // 일반 화면 조건 끝
                Assert.That(screenshot, Is.Not.Null); // 화면 캡처 성공 확인
                System.IO.File.WriteAllBytes(capturePath, screenshot.EncodeToPNG()); // 최신 플레이 화면 파일 저장
                Object.Destroy(screenshot); // 캡처 이미지 메모리 정리
            } // 화면 저장 조건 끝
            yield return Move(player, Key.W); // 세 번째 방 안쪽 이동
            yield return Move(player, Key.S); // 아래쪽 문을 바라보며 복귀
            yield return TapKey(Key.E); // 두 번째 방 복귀
            Assert.That(GameObject.Find("SecondDungeonGrid"), Is.Not.Null); // 위쪽 방에서 역방향 복귀 확인
            AppRoot.Instance.ReturnToTitle(); // 타이틀 복귀
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            AppRoot.Instance.StartNewGame(); // 새 게임 시작
            yield return WaitForPhase(GamePhase.Exploration); // 새 던전 진입 대기
            yield return null; // 새 지도 화면 준비 대기
            Assert.That(GameObject.Find("DungeonMiniMap/MapContent/Room_1_1"), Is.Null); // 새 게임의 세 번째 방 가림
            Assert.That(GameObject.Find("DungeonMiniMap/MapContent/Room_1_0").GetComponent<Image>().color, Is.EqualTo(connectedColor)); // 이전 방문 기록 초기화
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator FailedRoomArrival_DoesNotRevealNextRoom() // 실패한 방 전환의 지도 보존
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어 조회
            yield return ApproachEastDoor(player); // 첫 방의 오른쪽 문 접근
            GameObject second = null; // 비활성 목적 방 보관
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) // 씬 루트 순회
            { // 루트 순회 시작
                if (root.name == "SecondDungeonGrid") // 두 번째 방 검사
                { // 방 일치 조건 시작
                    second = root; // 목적 방 보관
                } // 방 일치 조건 끝
            } // 루트 순회 끝
            Assert.That(second, Is.Not.Null); // 목적 방 존재 확인
            var obstacles = second.transform.Find("Obstacles").GetComponent<Tilemap>(); // 목적 방 장애물 조회
            var blocker = second.AddComponent<RoomActivationBlocker>(); // 방 활성화 중 도착 칸 변경
            blocker.Tile = obstacles.GetTile(new Vector3Int(3, 0, 0)); // 기존 장애물 타일 전달
            var connected = GameObject.Find("DungeonMiniMap/MapContent/Room_1_0").GetComponent<Image>().color; // 이동 전 표시 색상
            yield return TapKey(Key.E); // 실패할 방 이동 시도
            Assert.That(GameObject.Find("DungeonMiniMap/MapContent/Room_1_1"), Is.Null); // 도착하지 않은 방의 연결 가림
            Assert.That(GameObject.Find("DungeonMiniMap/MapContent/Room_1_0").GetComponent<Image>().color, Is.EqualTo(connected)); // 방문 상태 미변경
            Assert.That(GameObject.Find("RoomTravel").GetComponent<RoomTravelController>().RoomX, Is.EqualTo(0)); // 현재 방 좌표 유지
        } // 검증 끝
        [UnityTearDown] // 검증 정리 표시
        public IEnumerator Cleanup() // 가상 장치와 설정 복구
        { // 정리 시작
            RoomTravelController.GenerationSeedOverride = null; // 검증용 생성 시드 해제
            if (keyboard != null && keyboard.added) // 가상 키보드 검사
            { // 키보드 정리 시작
                InputSystem.RemoveDevice(keyboard); // 가상 키보드 제거
            } // 키보드 정리 끝
            if (gamepad != null && gamepad.added) // 가상 게임패드 검사
            { // 게임패드 정리 시작
                InputSystem.RemoveDevice(gamepad); // 가상 게임패드 제거
            } // 게임패드 정리 끝
            if (AppRoot.Instance != null) // 공통 객체 검사
            { // 공통 객체 정리 시작
                Object.Destroy(AppRoot.Instance.gameObject); // 검증용 공통 객체 제거
            } // 공통 객체 정리 끝
            yield return null; // 객체 제거 대기
            InputSystem.settings.backgroundBehavior = originalBackground; // 기존 입력 설정 복구
#if UNITY_EDITOR // 편집기 설정 복구
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditor; // 기존 편집기 설정 복구
#endif // 편집기 설정 끝
        } // 정리 끝
        private IEnumerator EnterDungeon() // 실제 게임 시작
        { // 진입 시작
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 시작 씬 로드
            yield return WaitForPhase(GamePhase.Title); // 타이틀 진입 대기
            AppRoot.Instance.StartNewGame(); // 새 게임 시작
            yield return WaitForPhase(GamePhase.Exploration); // 탐험 진입 대기
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증용 키보드 추가
            gamepad = InputSystem.AddDevice<Gamepad>(); // 검증용 게임패드 추가
            InputSystem.Update(); // 입력 장치 연결
            yield return null; // 입력 준비 대기
        } // 진입 끝
        private static IEnumerator WaitForPhase(GamePhase phase) // 게임 상태 대기
        { // 대기 시작
            var deadline = Time.realtimeSinceStartup + 15f; // 최대 대기 시간
            while (Time.realtimeSinceStartup < deadline) // 제한 시간 반복
            { // 반복 시작
                if (AppRoot.Instance != null && AppRoot.Instance.Flow.Phase == phase) // 목표 상태 검사
                { // 도착 조건 시작
                    yield return null; // 씬 초기화 대기
                    yield break; // 대기 종료
                } // 도착 조건 끝
                yield return null; // 다음 프레임 대기
            } // 반복 끝
            Assert.Fail("Timed out waiting for " + phase); // 상태 전환 실패 표시
        } // 대기 끝
        private IEnumerator ApproachEastDoor(GridMoveController player) // 장애물을 피해 오른쪽 문 접근
        { // 접근 시작
            yield return Move(player, Key.W); // 위쪽 이동
            for (var index = 0; index < 4; index++) // 장애물 위를 가로지르는 이동
            { // 반복 시작
                yield return Move(player, Key.D); // 오른쪽 이동
            } // 반복 끝
            yield return Move(player, Key.S); // 문 앞줄 진입
            yield return Move(player, Key.D); // 문 앞에서 오른쪽 바라보기
        } // 접근 끝
        private IEnumerator Move(GridMoveController player, Key key) // 한 칸 이동과 완료 대기
        { // 이동 시작
            var transitionDeadline = Time.realtimeSinceStartup + 10f; // 화면 전환 대기 제한
            while (player.IsRoomTransitionLocked && Time.realtimeSinceStartup < transitionDeadline) // 이전 방 전환 완료 대기
            { // 전환 대기 시작
                yield return null; // 다음 프레임 대기
            } // 전환 대기 끝
            Assert.That(player.IsRoomTransitionLocked, Is.False); // 실제 이동 전 전환 완료 확인
            yield return TapKey(key); // 방향 키 입력
            yield return WaitForStopped(player); // 이동 종료 대기
        } // 이동 끝
        private IEnumerator TapKey(Key key) // 키 누름과 해제
        { // 입력 시작
            SetKeyboard(key); // 키 누름
            yield return null; // 입력 반영 대기
            SetKeyboard(); // 키 해제
            yield return null; // 해제 반영 대기
        } // 입력 끝
        private void SetKeyboard(params Key[] keys) // 가상 키보드 입력 설정
        { // 설정 시작
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); // 키 상태 전달
            InputSystem.Update(); // 즉시 입력 반영
        } // 설정 끝
        private static IEnumerator WaitForStopped(GridMoveController player) // 이동 종료 대기
        { // 대기 시작
            yield return null; // 이동 시작 대기
            var deadline = Time.realtimeSinceStartup + 10f; // 최대 이동 대기 시간
            while (player.IsMoving && Time.realtimeSinceStartup < deadline) // 이동 중 반복
            { // 반복 시작
                yield return null; // 다음 프레임 대기
            } // 반복 끝
            Assert.That(player.IsMoving, Is.False); // 이동 종료 확인
        } // 대기 끝
        private static Texture2D CaptureOffscreen() // 사용자 화면을 건드리지 않는 플레이 화면 캡처
        { // 캡처 시작
            var camera = Camera.main; // 실제 던전 카메라 조회
            var canvas = GameObject.Find("DungeonCanvas").GetComponent<Canvas>(); // 실제 던전 안내와 미니맵 조회
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); // 별도 화면 렌더링 영역
            var previousActive = RenderTexture.active; // 기존 렌더링 대상 보관
            var previousTarget = camera.targetTexture; // 기존 카메라 대상 보관
            var previousMode = canvas.renderMode; // 기존 UI 렌더링 방식 보관
            var previousCamera = canvas.worldCamera; // 기존 UI 카메라 보관
            var previousDistance = canvas.planeDistance; // 기존 UI 거리 보관
            try // 실제 화면의 카메라와 UI 렌더링
            { // 렌더링 시작
                target.Create(); // 별도 렌더링 영역 생성
                camera.targetTexture = target; // 실제 카메라를 별도 영역에 연결
                canvas.renderMode = RenderMode.ScreenSpaceCamera; // 화면 UI의 카메라 렌더링
                canvas.worldCamera = camera; // 실제 던전 카메라 연결
                canvas.planeDistance = 1f; // 게임 화면 앞쪽 UI 배치
                Canvas.ForceUpdateCanvases(); // 실제 화면 배치 반영
                camera.Render(); // 게임 화면을 별도 영역에 렌더링
                RenderTexture.active = target; // 읽을 렌더링 영역 선택
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false); // 캡처 이미지 생성
                image.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0); // 렌더링된 화면 픽셀 읽기
                image.Apply(); // 캡처 픽셀 반영
                return image; // 실제 플레이 이미지 전달
            } // 렌더링 끝
            finally // 기존 화면과 리소스 복구
            { // 복구 시작
                canvas.renderMode = previousMode; // 기존 UI 렌더링 방식 복구
                canvas.worldCamera = previousCamera; // 기존 UI 카메라 복구
                canvas.planeDistance = previousDistance; // 기존 UI 거리 복구
                camera.targetTexture = previousTarget; // 기존 카메라 대상 복구
                RenderTexture.active = previousActive; // 기존 활성 렌더링 대상 복구
                target.Release(); // 임시 렌더링 메모리 해제
                Object.Destroy(target); // 임시 렌더링 객체 제거
            } // 복구 끝
        } // 캡처 끝
    } // 클래스 끝
} // 이름 공간 끝
