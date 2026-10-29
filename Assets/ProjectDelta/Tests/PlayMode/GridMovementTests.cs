using System.Collections; // 프레임 기반 대기
using NUnit.Framework; // 검증 도구
using ProjectDelta.Core; // 이동 방향과 게임 상태
using ProjectDelta.Runtime; // 실제 실행 객체
using UnityEngine; // 게임 객체와 좌표
using UnityEngine.InputSystem; // 가상 입력 장치
using UnityEngine.InputSystem.LowLevel; // 입력 장치 상태
using UnityEngine.SceneManagement; // 실제 씬 전환
using UnityEngine.TestTools; // 프레임 기반 검증
using UnityEngine.Tilemaps; // 실제 던전 타일
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class GridMovementTests // 실제 던전 이동 검증
    { // 클래스 시작
        private Keyboard keyboard; // 검증용 키보드
        private Gamepad gamepad; // 검증용 게임패드
        private InputSettings.BackgroundBehavior originalBackgroundBehavior; // 복구할 입력 설정
#if UNITY_EDITOR // 편집기 전용 입력 설정
        private InputSettings.EditorInputBehaviorInPlayMode originalEditorBehavior; // 복구할 편집기 입력 설정
#endif // 편집기 설정 끝
        [SetUp] // 검증 전 준비 표시
        public void RouteTestInputToGame() // 자동 검증 입력 준비
        { // 준비 시작
            RoomTravelController.GenerationSeedOverride = 0; // 기존 이동 시험의 재현 가능한 방 구조
            originalBackgroundBehavior = InputSystem.settings.backgroundBehavior; // 기존 백그라운드 설정 보관
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 자동 실행 포커스 영향 제외
#if UNITY_EDITOR // 편집기 입력 전달 설정
            originalEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 설정 보관
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 검증 입력을 게임에 전달
#endif // 편집기 입력 설정 끝
        } // 준비 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator SavedDungeon_ContainsWalkableTilesObstaclesAndPlayer() // 저장된 바닥과 플레이어 검증
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var floorObject = GameObject.Find("DungeonGrid/Floor"); // 실제 바닥 객체 조회
            Assert.That(floorObject, Is.Not.Null); // 바닥 객체 존재 확인
            var floor = floorObject.GetComponent<Tilemap>(); // 실제 바닥 타일맵 조회
            Assert.That(floor.HasTile(new Vector3Int(0, 0, 0)), Is.True, "Starting cell must have a saved floor tile."); // 시작 칸 바닥 확인
            Assert.That(floor.HasTile(new Vector3Int(-6, -4, 0)), Is.True); // 방 경계의 바닥 확인
            Assert.That(floor.HasTile(new Vector3Int(6, 4, 0)), Is.True); // 반대편 경계의 바닥 확인
            var obstacleObject = GameObject.Find("DungeonGrid/Obstacles"); // 실제 장애물 객체 조회
            Assert.That(obstacleObject, Is.Not.Null); // 장애물 객체 존재 확인
            Assert.That(obstacleObject.GetComponent<Tilemap>().HasTile(new Vector3Int(0, 3, 0)), Is.True); // 시험 장애물 저장 확인
            var player = FindPlayer(); // 실제 플레이어 조회
            Assert.That(player.transform.Find("DirectionIndicator"), Is.Not.Null); // 방향 표시 존재 확인
            Assert.That(Vector3.Distance(player.transform.position, floor.GetCellCenterWorld(Vector3Int.zero)), Is.LessThan(0.001f)); // 시작 칸 중심 일치 확인
        } // 검증 끝
#if !UNITY_EDITOR // 실제 실행 파일의 렌더링 검증
        [UnityTest] // Windows 실행 화면 검증 표시
        public IEnumerator WindowsPlayer_RendersFloorObstacleAndDirectionArrow() // 실제 격자 화면 표시 검증
        { // 검증 시작
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) // 그래픽 장치 존재 검사
            { // 그래픽 장치 제외 조건 시작
                Assert.Ignore("A graphics device is required for the rendered-screen check."); // 화면 출력 불가 환경 구분
            } // 그래픽 장치 제외 조건 끝
            yield return EnterDungeon(); // 실제 던전 진입
            yield return new WaitForEndOfFrame(); // 실제 화면 그리기 완료 대기
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture(); // 게임 자체의 화면 픽셀 캡처
            Assert.That(screenshot, Is.Not.Null); // 화면 캡처 성공 확인
            var floor = GameObject.Find("DungeonGrid/Floor").GetComponent<Tilemap>(); // 실제 바닥 타일맵 조회
            var floorPoint = Camera.main.WorldToScreenPoint(floor.GetCellCenterWorld(new Vector3Int(-4, -2, 0))); // 일반 바닥 칸의 화면 위치
            var wallPoint = Camera.main.WorldToScreenPoint(floor.GetCellCenterWorld(new Vector3Int(3, 0, 0))); // 장애물 칸의 화면 위치
            var playerPoint = Camera.main.WorldToScreenPoint(FindPlayer().transform.position); // 플레이어 중심의 화면 위치
            var backgroundPoint = Camera.main.WorldToScreenPoint(new Vector3(8f, -2f, 0f)); // 바닥 바깥 배경의 화면 위치
            var floorColor = screenshot.GetPixel(Mathf.RoundToInt(floorPoint.x), Mathf.RoundToInt(floorPoint.y)); // 일반 바닥 픽셀 읽기
            var wallColor = screenshot.GetPixel(Mathf.RoundToInt(wallPoint.x), Mathf.RoundToInt(wallPoint.y)); // 장애물 픽셀 읽기
            var playerColor = screenshot.GetPixel(Mathf.RoundToInt(playerPoint.x), Mathf.RoundToInt(playerPoint.y)); // 방향 화살표 픽셀 읽기
            var backgroundColor = screenshot.GetPixel(Mathf.RoundToInt(backgroundPoint.x), Mathf.RoundToInt(backgroundPoint.y)); // 배경 픽셀 읽기
            Object.Destroy(screenshot); // 검증용 캡처 메모리 정리
            Assert.That(floorColor.grayscale, Is.GreaterThan(backgroundColor.grayscale + 0.02f), "Floor must be visible against the background."); // 바닥의 실제 화면 표시 확인
            Assert.That(wallColor.r, Is.GreaterThan(wallColor.g * 1.3f), "Obstacle must remain visually distinct."); // 붉은 장애물 구분 확인
            Assert.That(wallColor.r, Is.GreaterThan(backgroundColor.r + 0.10f), "Obstacle must remain bright enough to see."); // 배경 대비 장애물 밝기 확인
            Assert.That(playerColor.grayscale, Is.GreaterThan(floorColor.grayscale + 0.05f), "The player direction arrow must be visible."); // 흰 방향 표시 확인
        } // 검증 끝
#endif // Windows 화면 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator KeyboardAndGamepad_MoveOneCellAndShowDirection() // 키보드와 게임패드 방향 검증
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = FindPlayer(); // 실제 플레이어 조회
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증 키보드 추가
            gamepad = InputSystem.AddDevice<Gamepad>(); // 검증 게임패드 추가
            InputSystem.Update(); // 새 장치 입력 연결
            yield return null; // 입력 안정화 대기
            yield return TapKey(Key.W); // 위쪽 한 칸 입력
            yield return WaitForStopped(player); // 첫 이동 완료 대기
            Assert.That(player.X, Is.EqualTo(0)); // 위쪽 이동의 가로 위치
            Assert.That(player.Y, Is.EqualTo(1)); // 위쪽 이동의 세로 위치
            Assert.That(player.Facing, Is.EqualTo(MoveDirection.Up)); // 위쪽 표시 방향
            AssertDirection(player, 0f); // 위쪽 화살표 확인
            Assert.That(player.GetComponent<SpriteRenderer>().sprite.texture.name, Is.EqualTo("DungeonPlayerUp")); // 위쪽 주인공 이미지 확인
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 플레이 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 요청 검사
            { // 캡처 조건 시작
                var capture = typeof(DungeonRuntimeGenerationTests).GetMethod("CaptureScreen", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic); // 실제 던전 화면 캡처 도구
                var image = (Texture2D)capture.Invoke(null, new object[] { "DungeonCanvas" }); // 위쪽 주인공의 실제 화면
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 플레이 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadRight)); // 게임패드 오른쪽 입력
            InputSystem.Update(); // 게임패드 입력 반영
            yield return null; // 게임패드 입력 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 게임패드 방향 해제
            InputSystem.Update(); // 방향 해제 반영
            yield return WaitForStopped(player); // 두 번째 이동 완료 대기
            Assert.That(player.X, Is.EqualTo(1)); // 오른쪽 이동의 가로 위치
            Assert.That(player.Y, Is.EqualTo(1)); // 오른쪽 이동의 세로 위치
            Assert.That(player.Facing, Is.EqualTo(MoveDirection.Right)); // 오른쪽 표시 방향
            AssertDirection(player, -90f); // 오른쪽 화살표 확인
            Assert.That(player.GetComponent<SpriteRenderer>().sprite.texture.name, Is.EqualTo("DungeonPlayerRight")); // 오른쪽 주인공 이미지 확인
            var floor = GameObject.Find("DungeonGrid/Floor").GetComponent<Tilemap>(); // 바닥 중심 변환 조회
            Assert.That(Vector3.Distance(player.transform.position, floor.GetCellCenterWorld(new Vector3Int(1, 1, 0))), Is.LessThan(0.001f)); // 실제 화면과 칸 좌표 일치 확인
            yield return TapKey(Key.S); // 아래쪽 한 칸 입력
            yield return WaitForStopped(player); // 아래쪽 이동 완료 대기
            Assert.That(player.X, Is.EqualTo(1)); // 아래쪽 이동의 가로 위치
            Assert.That(player.Y, Is.EqualTo(0)); // 아래쪽 이동의 세로 위치
            Assert.That(player.Facing, Is.EqualTo(MoveDirection.Down)); // 아래쪽 표시 방향
            AssertDirection(player, 180f); // 아래쪽 화살표 확인
            Assert.That(player.GetComponent<SpriteRenderer>().sprite.texture.name, Is.EqualTo("DungeonPlayer")); // 아래쪽 주인공 이미지 확인
            yield return TapKey(Key.LeftArrow); // 화살표 키의 왼쪽 입력
            yield return WaitForStopped(player); // 화살표 키 이동 완료 대기
            Assert.That(player.X, Is.EqualTo(0)); // 왼쪽 화살표 키 이동 확인
            Assert.That(player.Facing, Is.EqualTo(MoveDirection.Left)); // 왼쪽 화살표 키 방향 확인
            Assert.That(player.GetComponent<SpriteRenderer>().sprite.texture.name, Is.EqualTo("DungeonPlayerLeft")); // 왼쪽 주인공 이미지 확인
            yield return TapKey(Key.DownArrow); // 화살표 키의 아래쪽 입력
            yield return WaitForStopped(player); // 아래쪽 화살표 키 이동 완료 대기
            Assert.That(player.Y, Is.EqualTo(-1)); // 아래쪽 화살표 키 이동 확인
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator KeyboardInput_StopsAtObstacleAndRoomBoundary() // 장애물과 방 경계 검증
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = FindPlayer(); // 실제 플레이어 조회
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증 키보드 추가
            InputSystem.Update(); // 새 장치 연결 반영
            yield return null; // 입력 안정화 대기
            yield return TapKey(Key.W); // 위쪽 첫 이동 입력
            yield return WaitForStopped(player); // 첫 이동 완료 대기
            yield return TapKey(Key.W); // 위쪽 두 번째 이동 입력
            yield return WaitForStopped(player); // 두 번째 이동 완료 대기
            yield return TapKey(Key.W); // 장애물 방향 이동 입력
            yield return WaitForStopped(player); // 차단 처리 대기
            Assert.That(player.X, Is.EqualTo(0)); // 장애물 앞 가로 위치
            Assert.That(player.Y, Is.EqualTo(2)); // 장애물 앞 세로 위치
            yield return TapKey(Key.S); // 아래쪽 이동 입력
            yield return WaitForStopped(player); // 아래쪽 이동 완료 대기
            yield return TapKey(Key.S); // 시작 줄 복귀 입력
            yield return WaitForStopped(player); // 시작 줄 복귀 완료 대기
            for (var index = 0; index < 7; index++) // 방 경계를 넘는 이동 시도
            { // 반복 시작
                yield return TapKey(Key.A); // 왼쪽 이동 입력
                yield return WaitForStopped(player); // 이동 또는 차단 완료 대기
            } // 반복 끝
            Assert.That(player.X, Is.EqualTo(-6)); // 방 밖 가로 이동 차단
            Assert.That(player.Y, Is.EqualTo(0)); // 방 경계 세로 위치 보존
            Assert.That(player.Facing, Is.EqualTo(MoveDirection.Left)); // 마지막 성공 방향 표시
            AssertDirection(player, 90f); // 왼쪽 화살표 확인
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator DiagonalKeyboardInput_MovesOnlyOneHorizontalCell() // 대각 입력 차단 검증
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = FindPlayer(); // 실제 플레이어 조회
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증 키보드 추가
            InputSystem.Update(); // 새 장치 연결 반영
            yield return null; // 입력 안정화 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D)); // 동시에 누른 대각 입력
            InputSystem.Update(); // 대각 입력 반영
            yield return null; // 입력 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 입력 해제
            InputSystem.Update(); // 입력 해제 반영
            yield return WaitForStopped(player); // 이동 완료 대기
            Assert.That(player.X, Is.EqualTo(1)); // 동일 축의 가로 우선 적용
            Assert.That(player.Y, Is.EqualTo(0)); // 대각선 세로 이동 차단
            Assert.That(player.Facing, Is.EqualTo(MoveDirection.Right)); // 가로 방향 표시
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator InputDuringMovement_UsesOnlyFirstBufferedDirection() // 실제 입력 대기 제한 검증
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = FindPlayer(); // 실제 플레이어 조회
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증 키보드 추가
            InputSystem.Update(); // 새 장치 연결 반영
            yield return null; // 입력 안정화 대기
            SetKeyboard(Key.W); // 위쪽 이동 시작
            SetKeyboard(); // 위쪽 키 해제
            SetKeyboard(Key.D); // 오른쪽 입력 대기
            SetKeyboard(); // 오른쪽 키 해제
            SetKeyboard(Key.A); // 초과 대기 입력 시도
            SetKeyboard(); // 왼쪽 키 해제
            Assert.That(player.IsMoving, Is.True); // 첫 이동 진행 상태 확인
            yield return WaitForStopped(player); // 모든 유효 이동 완료 대기
            Assert.That(player.X, Is.EqualTo(1)); // 첫 대기 입력의 오른쪽 이동
            Assert.That(player.Y, Is.EqualTo(1)); // 최초 위쪽 이동 보존
            Assert.That(player.Facing, Is.EqualTo(MoveDirection.Right)); // 초과 입력 무시 확인
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator FirstInputAfterUnlock_IsNotDiscarded() // 입력 활성화 직후 새 키 검증
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = FindPlayer(); // 실제 플레이어 조회
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증 키보드 추가
            InputSystem.Update(); // 새 장치 연결 반영
            yield return null; // 입력 안정화 대기
            AppRoot.Instance.Input.SetPhase(GamePhase.Title); // 탐험 입력 잠금
            AppRoot.Instance.Input.SetPhase(GamePhase.Exploration); // 탐험 입력 재활성화
            SetKeyboard(Key.W); // 첫 입력 갱신 전 새 키 누름
            SetKeyboard(); // 이동 키 해제
            yield return WaitForStopped(player); // 이동 완료 대기
            Assert.That(player.Y, Is.EqualTo(1)); // 새로 누른 첫 입력의 이동 확인
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator NewDirectionWhileOldKeyIsHeld_IsAccepted() // 이전 화면 키와 새 방향의 독립 처리 검증
        { // 검증 시작
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증 키보드 추가
            InputSystem.Update(); // 새 장치 연결 반영
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 시작 씬 진입
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            SetKeyboard(Key.W); // 이전 화면에서 위쪽 키 누름
            AppRoot.Instance.StartNewGame(); // 키를 누른 채 새 게임 시작
            yield return WaitForPhase(GamePhase.Exploration); // 던전 도착 대기
            var player = FindPlayer(); // 실제 플레이어 조회
            SetKeyboard(Key.W, Key.D); // 기존 키 유지 중 새 오른쪽 키 입력
            yield return WaitForStopped(player); // 새 방향 이동 완료 대기
            Assert.That(player.X, Is.EqualTo(1)); // 새 오른쪽 입력 허용 확인
            Assert.That(player.Y, Is.EqualTo(0)); // 이전 위쪽 키의 이동 차단 확인
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator QuickPressAndReleaseInOneUpdate_MovesOneCell() // 한 입력 갱신의 빠른 탭 검증
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = FindPlayer(); // 실제 플레이어 조회
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증 키보드 추가
            InputSystem.Update(); // 새 장치 연결 반영
            yield return null; // 입력 안정화 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); // 위쪽 키 누름 대기열
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 같은 갱신의 키 해제 대기열
            InputSystem.Update(); // 누름과 해제 동시 처리
            yield return WaitForStopped(player); // 이동 완료 대기
            Assert.That(player.Y, Is.EqualTo(1)); // 빠른 입력의 한 칸 이동 확인
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator HeldStickFromTitle_DoesNotBlockNewDpadInput() // 이전 스틱과 새 방향 버튼 분리 검증
        { // 검증 시작
            gamepad = InputSystem.AddDevice<Gamepad>(); // 검증 게임패드 추가
            InputSystem.Update(); // 새 장치 연결 반영
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 시작 씬 진입
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            var heldStick = new GamepadState // 이전 화면의 스틱 상태
            { // 게임패드 상태 시작
                leftStick = Vector2.up // 위쪽 스틱 유지
            }; // 게임패드 상태 끝
            InputSystem.QueueStateEvent(gamepad, heldStick); // 타이틀 스틱 입력
            InputSystem.Update(); // 이전 화면 입력 반영
            AppRoot.Instance.StartNewGame(); // 스틱을 기울인 채 새 게임 시작
            yield return WaitForPhase(GamePhase.Exploration); // 던전 도착 대기
            var player = FindPlayer(); // 실제 플레이어 조회
            InputSystem.QueueStateEvent(gamepad, heldStick.WithButton(GamepadButton.DpadRight)); // 새 오른쪽 버튼 입력
            InputSystem.Update(); // 게임패드 입력 반영
            yield return WaitForStopped(player); // 새 방향 이동 완료 대기
            Assert.That(player.X, Is.EqualTo(1)); // 새 방향 버튼 허용 확인
            Assert.That(player.Y, Is.EqualTo(0)); // 이전 화면 스틱 입력 차단 확인
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator FreshStickAfterUnlock_MovesWithoutWaitingForNeutral() // 새 스틱의 첫 입력 검증
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = FindPlayer(); // 실제 플레이어 조회
            gamepad = InputSystem.AddDevice<Gamepad>(); // 검증 게임패드 추가
            InputSystem.Update(); // 새 장치 연결 반영
            yield return null; // 입력 안정화 대기
            AppRoot.Instance.Input.SetPhase(GamePhase.Title); // 탐험 입력 잠금
            AppRoot.Instance.Input.SetPhase(GamePhase.Exploration); // 탐험 입력 재활성화
            var newStick = new GamepadState // 새 스틱 입력 상태
            { // 게임패드 상태 시작
                leftStick = Vector2.up // 새 위쪽 스틱 입력
            }; // 게임패드 상태 끝
            InputSystem.QueueStateEvent(gamepad, newStick); // 활성화 직후 스틱 입력
            InputSystem.Update(); // 첫 입력 처리
            yield return WaitForStopped(player); // 이동 완료 대기
            Assert.That(player.Y, Is.EqualTo(1)); // 새 스틱 입력의 이동 확인
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator InputLock_PausesMovementAndClearsBufferedInput() // 입력 잠금과 대기 취소 검증
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = FindPlayer(); // 실제 플레이어 조회
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증 키보드 추가
            InputSystem.Update(); // 새 장치 연결 반영
            yield return null; // 입력 안정화 대기
            SetKeyboard(Key.W); // 위쪽 이동 시작
            SetKeyboard(); // 위쪽 키 해제
            SetKeyboard(Key.D); // 오른쪽 입력 대기
            SetKeyboard(); // 오른쪽 키 해제
            var lockedPosition = player.transform.position; // 잠금 직전 표시 위치
            AppRoot.Instance.Input.SetPhase(GamePhase.Title); // 화면 입력으로 전환
            var deadline = Time.realtimeSinceStartup + 0.3f; // 잠금 확인 시간
            while (Time.realtimeSinceStartup < deadline) // 잠금 유지 시간 반복
            { // 반복 시작
                yield return null; // 한 프레임 대기
            } // 반복 끝
            Assert.That(Vector3.Distance(player.transform.position, lockedPosition), Is.LessThan(0.001f)); // 잠금 중 표시 정지 확인
            AppRoot.Instance.Input.SetPhase(GamePhase.Exploration); // 탐험 입력 복구
            yield return WaitForStopped(player); // 중단한 첫 이동 완료 대기
            Assert.That(player.X, Is.EqualTo(0)); // 잠금 전 대기 이동 취소 확인
            Assert.That(player.Y, Is.EqualTo(1)); // 진행 중 이동 결과 보존 확인
        } // 검증 끝
        [UnityTest] // 실제 실행 검증 표시
        public IEnumerator HeldKeyAtSceneTransition_DoesNotMoveUntilReleased() // 화면 전환 중 누른 입력 차단 검증
        { // 검증 시작
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증 키보드 추가
            InputSystem.Update(); // 새 장치 연결 반영
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 시작 씬 진입
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            SetKeyboard(Key.W); // 타이틀에서 이동 키 누름
            AppRoot.Instance.StartNewGame(); // 키를 누른 채 새 게임 시작
            yield return WaitForPhase(GamePhase.Exploration); // 던전 도착 대기
            var player = FindPlayer(); // 실제 플레이어 조회
            var deadline = Time.realtimeSinceStartup + 0.25f; // 입력 누수 확인 시간
            while (Time.realtimeSinceStartup < deadline) // 누른 키 유지 시간 반복
            { // 반복 시작
                yield return null; // 한 프레임 대기
            } // 반복 끝
            Assert.That(player.X, Is.EqualTo(0)); // 이전 화면 입력의 가로 누수 방지
            Assert.That(player.Y, Is.EqualTo(0)); // 이전 화면 입력의 세로 누수 방지
            SetKeyboard(); // 이전 화면에서 누른 키 해제
            yield return null; // 입력 해제 반영 대기
            yield return TapKey(Key.W); // 새 이동 입력
            yield return WaitForStopped(player); // 새 이동 완료 대기
            Assert.That(player.Y, Is.EqualTo(1)); // 키 해제 후 정상 이동 확인
        } // 검증 끝
        [UnityTearDown] // 검증 후 정리 표시
        public IEnumerator Cleanup() // 검증 장치와 공통 객체 정리
        { // 정리 시작
            RoomTravelController.GenerationSeedOverride = null; // 검증용 생성 시드 해제
            if (keyboard != null && keyboard.added) // 검증 키보드 존재 확인
            { // 키보드 정리 시작
                InputSystem.RemoveDevice(keyboard); // 검증 키보드 제거
                keyboard = null; // 키보드 정리 표시
            } // 키보드 정리 끝
            if (gamepad != null && gamepad.added) // 검증 게임패드 존재 확인
            { // 게임패드 정리 시작
                InputSystem.RemoveDevice(gamepad); // 검증 게임패드 제거
                gamepad = null; // 게임패드 정리 표시
            } // 게임패드 정리 끝
            if (AppRoot.Instance != null) // 공통 객체 존재 확인
            { // 객체 정리 시작
                Object.Destroy(AppRoot.Instance.gameObject); // 공통 객체 삭제
            } // 객체 정리 끝
            yield return null; // 객체 삭제 대기
            InputSystem.settings.backgroundBehavior = originalBackgroundBehavior; // 기존 백그라운드 입력 설정 복구
#if UNITY_EDITOR // 편집기 입력 설정 복구
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorBehavior; // 기존 편집기 입력 설정 복구
#endif // 편집기 설정 복구 끝
        } // 정리 끝
        private static IEnumerator EnterDungeon() // 실제 던전 진입 도우미
        { // 진입 시작
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 시작 씬 진입
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            AppRoot.Instance.StartNewGame(); // 새 게임 요청
            yield return WaitForPhase(GamePhase.Exploration); // 던전 도착 대기
            yield return null; // 탐험 입력 준비 대기
        } // 진입 끝
        private static IEnumerator WaitForPhase(GamePhase phase) // 지정 게임 상태 대기
        { // 대기 시작
            var deadline = Time.realtimeSinceStartup + 15f; // 실제 시간 제한
            while (Time.realtimeSinceStartup < deadline) // 제한 시간 안의 반복
            { // 반복 시작
                if (AppRoot.Instance != null && AppRoot.Instance.Flow.Phase == phase) // 실제 도착 상태 검사
                { // 도착 조건 시작
                    yield return null; // 화면 초기화 대기
                    yield break; // 대기 종료
                } // 도착 조건 끝
                yield return null; // 다음 프레임 대기
            } // 반복 끝
            Assert.Fail("Timed out waiting for " + phase); // 상태 전환 지연 표시
        } // 대기 끝
        private GridMoveController FindPlayer() // 실제 이동 객체 조회
        { // 조회 시작
            var playerObject = GameObject.Find("PlayerPreview"); // 플레이어 객체 조회
            Assert.That(playerObject, Is.Not.Null); // 플레이어 존재 확인
            var player = playerObject.GetComponent<GridMoveController>(); // 이동 제어기 조회
            Assert.That(player, Is.Not.Null, "Player must have the grid movement component."); // 이동 제어기 존재 확인
            return player; // 검증 대상 전달
        } // 조회 끝
        private IEnumerator TapKey(Key key) // 한 번의 키 입력 전달
        { // 입력 시작
            SetKeyboard(key); // 지정 키 누름
            yield return null; // 입력 처리 대기
            SetKeyboard(); // 모든 키 해제
            yield return null; // 해제 처리 대기
        } // 입력 끝
        private void SetKeyboard(params Key[] keys) // 실제 입력 장치 상태 설정
        { // 설정 시작
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); // 지정 키 상태 전달
            InputSystem.Update(); // 즉시 입력 처리
        } // 설정 끝
        private static IEnumerator WaitForStopped(GridMoveController player) // 격자 이동 완료 대기
        { // 대기 시작
            yield return null; // 입력에 따른 이동 시작 대기
            var deadline = Time.realtimeSinceStartup + 10f; // 이동 완료 시간 제한
            while (player.IsMoving && Time.realtimeSinceStartup < deadline) // 이동 중 반복
            { // 반복 시작
                yield return null; // 다음 프레임 대기
            } // 반복 끝
            Assert.That(player.IsMoving, Is.False, "Grid movement did not finish."); // 이동 종료 확인
        } // 대기 끝
        private static void AssertDirection(GridMoveController player, float expectedAngle) // 실제 화살표 방향 확인
        { // 확인 시작
            var indicator = player.transform.Find("DirectionIndicator"); // 방향 화살표 조회
            Assert.That(indicator, Is.Not.Null); // 방향 화살표 존재 확인
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(indicator.localEulerAngles.z, expectedAngle)), Is.LessThan(0.1f)); // 표시 회전 각도 확인
        } // 확인 끝
    } // 클래스 끝
} // 이름 공간 끝
