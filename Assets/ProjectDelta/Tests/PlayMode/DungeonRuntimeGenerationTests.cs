using System.Collections; // 프레임 기반 대기
using System.Collections.Generic; // 경로 탐색 목록
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
    public sealed class DoorArrivalBlocker : MonoBehaviour // 도착 시 문 앞 칸 차단 모사
    { // 클래스 시작
        public TileBase Tile; // 차단에 사용할 타일
        public Vector3Int Cell; // 차단할 도착 칸
        private void OnEnable() // 도착 방 활성화 처리
        { // 처리 시작
            transform.Find("Obstacles").GetComponent<Tilemap>().SetTile(Cell, Tile); // 도착 칸의 장애물 추가
        } // 처리 끝
    } // 클래스 끝
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
        private Mouse mouse; // 검증용 마우스
        private Gamepad gamepad; // 검증용 게임패드
        private GameObject testPrototype; // 오류 경로 검증용 방 객체
        private GameObject testController; // 오류 경로 검증용 제어 객체
        private InputSettings.BackgroundBehavior originalBackground; // 기존 백그라운드 입력 설정
#if UNITY_EDITOR // 편집기 입력 설정 보관
        private InputSettings.EditorInputBehaviorInPlayMode originalEditor; // 기존 편집기 입력 설정
#endif // 편집기 설정 끝
        [SetUp] // 검증 준비 표시
        public void PrepareInput() // 가상 입력을 실제 게임에 전달
        { // 준비 시작
            ObservationStorage.TestDirectoryOverride = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Artifacts", "ObservationPlayModeTests", System.Guid.NewGuid().ToString("N"))); // 개별 시험의 독립 저장 경로
            originalBackground = InputSystem.settings.backgroundBehavior; // 기존 입력 설정 보관
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; // 자동 검증 포커스 영향 제외
#if UNITY_EDITOR // 편집기 입력 설정 시작
            originalEditor = InputSystem.settings.editorInputBehaviorInPlayMode; // 기존 편집기 설정 보관
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView; // 게임 화면에 검증 입력 전달
#endif // 편집기 입력 설정 끝
        } // 준비 끝
        [UnityTest] // 타이틀 미관찰 도감과 실제 입력 검증
        public IEnumerator Journal_Empty_OpensAndClosesWithKeyboardAndGamepad() // 미관찰 정보와 선택 복원 확인
        { // 검증 시작
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 시작 씬 진입
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드 연결
            gamepad = InputSystem.AddDevice<Gamepad>(); // 시험 게임패드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 타이틀 화면 준비 대기
            var journalButton = GameObject.Find("JournalButton"); // 실제 도감 버튼
            Assert.That(journalButton, Is.Not.Null); // 도감 진입 제공 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.DownArrow)); // 도감 버튼으로 이동
            yield return null; // 키 입력 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
            yield return null; // 선택 반영 대기
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(journalButton)); // 도감 버튼의 키보드 선택
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 실제 도감 열기
            yield return null; // 도감 화면 준비 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 선택 해제 반영 대기
            var contents = GameObject.Find("JournalContents").GetComponent<UnityEngine.UI.Text>(); // 도감 내용
            Assert.That(contents.text, Does.Contain("관찰 기록이 없습니다")); // 미관찰 안내
            Assert.That(contents.text, Does.Not.Contain("체력 70")); // 미관찰 정보 비공개
            Assert.That(GameObject.Find("JournalPortrait"), Is.Null); // 미관찰 대상 이미지 비공개
            Assert.That(GameObject.Find("NewGameButton").GetComponent<UnityEngine.UI.Button>().interactable, Is.False); // 도감 열기 중 새 게임 차단
            var close = GameObject.Find("CloseJournalButton"); // 실제 닫기 버튼
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(close)); // 닫기 버튼의 기본 선택
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 키보드 도감 닫기
            yield return null; // 닫기 반영 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 선택 해제 반영 대기
            Assert.That(GameObject.Find("JournalContents"), Is.Null); // 도감 화면 닫기 확인
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(journalButton)); // 도감 버튼 선택 복원
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 게임패드 도감 열기
            yield return null; // 열기 반영 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 버튼 해제
            yield return null; // 해제 반영 대기
            Assert.That(GameObject.Find("JournalContents"), Is.Not.Null); // 게임패드 도감 열기 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 게임패드 도감 닫기
            yield return null; // 닫기 반영 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 버튼 해제
            yield return null; // 해제 반영 대기
            Assert.That(GameObject.Find("JournalContents"), Is.Null); // 게임패드 도감 닫기 확인
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(journalButton)); // 게임패드 선택 복원
        } // 검증 끝
        [UnityTest] // 손상된 저장의 도감 경고와 원본 보호 검증
        public IEnumerator Journal_DamagedSave_ShowsWarningWithoutChangingFile() // 저장 오류 안내와 원본 유지
        { // 검증 시작
            var path = ObservationStorage.TestDirectoryOverride; // 현재 시험 전용 저장 경로
            System.IO.Directory.CreateDirectory(path); // 시험 저장 폴더 준비
            var current = System.IO.Path.Combine(path, "observation-v1.json"); // 시험 저장 파일
            System.IO.File.WriteAllText(current, "damaged test data"); // 손상된 시험 원본
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 시작 씬 진입
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            var journalButton = GameObject.Find("JournalButton"); // 실제 도감 버튼
            Assert.That(journalButton, Is.Not.Null); // 도감 진입 제공 확인
            journalButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 손상 상태의 도감 열기
            yield return null; // 경고 표시 대기
            var warning = GameObject.Find("JournalSaveWarning"); // 도감 저장 오류 안내
            Assert.That(warning, Is.Not.Null); // 저장 오류 안내 제공 확인
            Assert.That(warning.GetComponent<UnityEngine.UI.Text>().text, Does.Contain("보존")); // 원본 보존 안내 확인
            Assert.That(GameObject.Find("JournalContents").GetComponent<UnityEngine.UI.Text>().text, Does.Not.Contain("체력 70")); // 확인되지 않은 정보 비공개
            GameObject.Find("CloseJournalButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 도감 닫기
            Assert.That(System.IO.File.ReadAllText(current), Is.EqualTo("damaged test data")); // 손상된 원본 그대로 보존
        } // 검증 끝
        [UnityTest] // 저장한 관찰 기록의 도감 표시 검증
        public IEnumerator Journal_Observed_ReloadsAndShowsSavedInfo() // 재초기화 후 저장 정보 표시
        { // 검증 시작
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 시작 씬 진입
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            Assert.That(AppRoot.Instance.TryRecordObservation("trial-elite"), Is.True); // 시험 대상의 관찰 기록 저장
            var current = System.IO.Path.Combine(ObservationStorage.TestDirectoryOverride, "observation-v1.json"); // 시험 저장 파일
            var saved = System.IO.File.ReadAllBytes(current); // 도감 열기 전 저장 내용 보관
            Object.Destroy(AppRoot.Instance.gameObject); // 현재 공통 실행 객체 종료
            yield return null; // 객체 종료 반영 대기
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 새 실행과 같은 초기화
            yield return WaitForPhase(GamePhase.Title); // 새 타이틀 도착 대기
            var journalButton = GameObject.Find("JournalButton"); // 실제 도감 버튼
            Assert.That(journalButton, Is.Not.Null); // 도감 진입 제공 확인
            journalButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 저장된 도감 열기
            yield return null; // 도감 화면 배치 대기
            var contents = GameObject.Find("JournalContents").GetComponent<UnityEngine.UI.Text>(); // 실제 도감 내용
            Assert.That(contents.text, Does.Contain("체력 70")); // 저장된 시험 체력 표시
            Assert.That(contents.text, Does.Contain("기본 공격")); // 저장된 공격 성향 표시
            Assert.That(GameObject.Find("JournalPortrait"), Is.Not.Null); // 관찰한 시험 대상 이미지 표시
            Assert.That(System.IO.File.ReadAllBytes(current), Is.EqualTo(saved)); // 도감 열람 시 저장 파일 변경 없음
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                var image = CaptureScreen("TitleCanvas"); // 실제 관찰 도감 화면
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 플레이 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
        } // 검증 끝
        [UnityTest] // 두 배 타일과 생성 이미지 연결 검증
        public IEnumerator DungeonVisuals_DoubleCellSizeAndGeneratedArt() // 격자 크기와 실제 리소스 확인
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 이동 제어
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 현재 방 조회
            var args = new object[] // 시작 방 조회 인수
            { // 인수 목록 시작
                travel.RoomX, travel.RoomY // 현재 방 좌표
            }; // 인수 목록 끝
            var room = (GameObject)finder.Invoke(travel, args); // 실제 시작 방
            var floor = room.transform.Find("Floor").GetComponent<Tilemap>(); // 실제 바닥 타일맵
            var obstacles = room.transform.Find("Obstacles").GetComponent<Tilemap>(); // 실제 장애물 타일맵
            var origin = floor.GetCellCenterWorld(Vector3Int.zero); // 기준 칸의 월드 중심
            var next = floor.GetCellCenterWorld(Vector3Int.right); // 인접 칸의 월드 중심
            Assert.That(Vector3.Distance(origin, next), Is.EqualTo(2f).Within(0.001f)); // 기존 한 칸의 두 배 크기
            Assert.That(Vector3.Distance(player.transform.position, floor.GetCellCenterWorld(new Vector3Int(player.X, player.Y, 0))), Is.LessThan(0.001f)); // 확대 후 플레이어 칸 중심
            Assert.That(player.transform.localScale.x, Is.EqualTo(1.3f).Within(0.001f)); // 확대된 플레이어 가로 크기 확인
            Assert.That(player.transform.localScale.y, Is.EqualTo(1.3f).Within(0.001f)); // 확대된 플레이어 세로 크기 확인
            var borders = room.transform.Find("TileBorders"); // 각 칸의 검은 테두리 타일맵
            Assert.That(borders, Is.Not.Null); // 검은 격자 표시 존재 확인
            var borderMap = borders.GetComponent<Tilemap>(); // 실제 격자 표시 타일맵
            Assert.That(borderMap.HasTile(Vector3Int.zero), Is.True); // 바닥 칸의 격자 표시 확인
            Assert.That(borderMap.GetTile(Vector3Int.zero).name, Is.EqualTo("DungeonTileBorder")); // 전용 테두리 타일 확인
            Assert.That(floor.GetTile(Vector3Int.zero).name, Is.EqualTo("GeneratedDungeonFloor")); // 생성한 바닥 타일 적용
            Assert.That(obstacles.GetTile(new Vector3Int(3, 0, 0)).name, Is.EqualTo("GeneratedDungeonObstacle")); // 생성한 장애물 타일 적용
            var door = room.transform.Cast<Transform>().First(child => child.name == "DoorMarker" && child.gameObject.activeSelf); // 실제 출구 표시
            Assert.That(door.GetComponent<SpriteRenderer>().sprite.texture.name, Is.EqualTo("DungeonDoor")); // 생성한 문 이미지 적용
            Assert.That(Camera.main.GetComponent("DungeonCameraMotion"), Is.Not.Null); // 플레이어 추적 카메라 제공 확인
        } // 검증 끝
        [UnityTest] // 탐험 체력 표시와 변경 반영 검증
        public IEnumerator DungeonHud_TracksPlayerHealth() // 실제 체력의 화면 표시 확인
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var label = GameObject.Find("PlayerHealthStatus"); // 탐험 중 체력 안내
            Assert.That(label, Is.Not.Null); // 체력 안내 제공 확인
            var text = label.GetComponent<UnityEngine.UI.Text>(); // 체력 안내 글자
            Assert.That(text.text, Does.Contain("100 / 100")); // 초기 체력 표시 확인
            AppRoot.Instance.UpdatePlayerHealth(37); // 전투 후 시험 체력 변경
            yield return null; // 화면 갱신 대기
            Assert.That(text.text, Does.Contain("37 / 100")); // 실제 체력 변경 반영 확인
            AppRoot.Instance.UpdatePlayerHealth(12); // 낮은 체력 상태 설정
            yield return null; // 경고 표시 갱신 대기
            Assert.That(text.text, Does.Contain("12 / 100")); // 낮은 체력 수치 표시 확인
            Assert.That(text.color.r, Is.GreaterThan(text.color.g)); // 낮은 체력의 경고 색상 확인
        } // 검증 끝
        [UnityTest] // 던전 안내 가독성과 입력 통과 검증
        public IEnumerator DungeonHud_HasReadableNonBlockingBackgrounds() // 안내 배경과 게임 입력 보존
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var header = GameObject.Find("DungeonHeaderBackground"); // 상단 안내 배경
            var controls = GameObject.Find("DungeonControlsBackground"); // 하단 안내 배경
            Assert.That(header, Is.Not.Null); // 상단 배경 표시 확인
            Assert.That(controls, Is.Not.Null); // 하단 배경 표시 확인
            Assert.That(header.GetComponent<UnityEngine.UI.Image>().raycastTarget, Is.False); // 상단 배경 입력 통과 확인
            Assert.That(controls.GetComponent<UnityEngine.UI.Image>().raycastTarget, Is.False); // 하단 배경 입력 통과 확인
            Assert.That(controls.GetComponent<UnityEngine.UI.Image>().color.a, Is.GreaterThan(0.7f)); // 바닥과 글씨 분리 확인
        } // 검증 끝
        [UnityTest] // 실제 주인공 이미지와 방향 표시 검증
        public IEnumerator DungeonPlayer_UsesAnimeSpriteAndVisibleDirection() // 탐험 주인공과 방향 표시 확인
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            var player = GameObject.Find("PlayerPreview"); // 실제 플레이어 객체
            var renderer = player.GetComponent<SpriteRenderer>(); // 플레이어 이미지 렌더러
            Assert.That(renderer.sprite.texture.name, Is.EqualTo("DungeonPlayer")); // 주인공 이미지 적용 확인
            Assert.That(renderer.color, Is.EqualTo(Color.white)); // 주인공 이미지 본래 색상 확인
            Assert.That(renderer.bounds.size.y, Is.InRange(1.8f, 2.2f)); // 확대된 타일에 맞춘 주인공 크기 확인
            var indicator = player.transform.Find("DirectionIndicator"); // 이동 방향 표시
            Assert.That(indicator, Is.Not.Null); // 방향 표시 보존 확인
            Assert.That(indicator.localScale.x, Is.LessThan(0.5f)); // 캐릭터를 가리지 않는 방향 크기
            Assert.That(indicator.localPosition.y, Is.LessThan(-0.4f)); // 캐릭터 아래의 방향 표시
        } // 검증 끝
        [UnityTest] // 사용자 지정 주인공 이미지 보존 검증
        public IEnumerator DungeonPlayer_CustomSpriteIsPreserved() // 사용자 지정 이미지 덮어쓰기 방지
        { // 검증 시작
            var custom = new GameObject("CustomPlayer", typeof(SpriteRenderer)); // 사용자 지정 플레이어 객체
            var texture = new Texture2D(2, 2); // 사용자 지정 이미지
            texture.name = "CustomPlayerTexture"; // 시험용 사용자 이미지 이름
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f); // 사용자 지정 스프라이트
            var renderer = custom.GetComponent<SpriteRenderer>(); // 사용자 렌더러
            renderer.sprite = sprite; // 사용자 이미지 연결
            DungeonPresentation.ApplyPlayer(custom); // 시험용 이미지 교체 요청
            Assert.That(renderer.sprite, Is.SameAs(sprite)); // 사용자 이미지 보존 확인
            Object.Destroy(custom); // 시험 객체 정리
            Object.Destroy(sprite); // 시험 스프라이트 정리
            Object.Destroy(texture); // 시험 이미지 정리
            yield return null; // 객체 정리 반영 대기
        } // 검증 끝
        [UnityTest] // 방 이동 화면 전환과 입력 잠금 검증
        public IEnumerator DungeonVisuals_RoomTransition_SlidesAndBlocksInput() // 실제 방 이동과 부드러운 카메라 전환
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            yield return EnterDungeon(); // 실제 던전 진입
            keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 입력 준비 대기
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var door = travel.Layout.Doors.First(value => value.RoomX == travel.RoomX && value.RoomY == travel.RoomY); // 시작 방의 실제 출구
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 현재 방 조회
            var args = new object[] // 현재 방 조회 인수
            { // 인수 목록 시작
                travel.RoomX, travel.RoomY // 현재 방 좌표
            }; // 인수 목록 끝
            var room = (GameObject)finder.Invoke(travel, args); // 현재 방 객체
            var floor = room.transform.Find("Floor").GetComponent<Tilemap>(); // 현재 방 바닥
            var obstacles = room.transform.Find("Obstacles").GetComponent<Tilemap>(); // 현재 방 장애물
            var offset = Offset(door.Direction); // 출구 방향의 한 칸
            var before = new Vector3Int(door.ApproachX, door.ApproachY, 0) - offset; // 출구에 접근하기 전 칸
            Assert.That(player.ChangeRoom(floor, obstacles, before.x, before.y), Is.True); // 현재 방의 접근 칸 배치
            yield return null; // 위치 변경 반영 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(DirectionKey(door.Direction))); // 문 방향 이동
            InputSystem.Update(); // 이동 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이동 키 해제
            InputSystem.Update(); // 해제 반영
            while (player.IsMoving) // 문 앞 이동 완료 대기
            { // 이동 대기 시작
                yield return null; // 다음 프레임 대기
            } // 이동 대기 끝
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 실제 문 상호작용
            InputSystem.Update(); // 방 이동 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 상호작용 키 해제
            InputSystem.Update(); // 해제 반영
            Assert.That(travel.RoomX, Is.EqualTo(door.TargetRoomX)); // 실제 도착 방 확인
            var motion = Camera.main.GetComponent("DungeonCameraMotion"); // 실제 카메라 전환 구성
            Assert.That(motion, Is.Not.Null); // 부드러운 방 전환 제공 확인
            var property = motion.GetType().GetProperty("IsTransitioning"); // 전환 진행 상태 조회
            Assert.That(property, Is.Not.Null); // 전환 상태 제공 확인
            Assert.That(property.GetValue(motion), Is.True); // 방 이동 직후 전환 진행 확인
            var initial = Camera.main.transform.position; // 전환 시작 카메라 위치
            var savedX = player.X; // 전환 중 플레이어 가로 칸
            var savedY = player.Y; // 전환 중 플레이어 세로 칸
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); // 전환 중 이동 요청
            InputSystem.Update(); // 이동 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이동 키 해제
            InputSystem.Update(); // 해제 반영
            Assert.That(player.X, Is.EqualTo(savedX)); // 전환 중 가로 이동 차단
            Assert.That(player.Y, Is.EqualTo(savedY)); // 전환 중 세로 이동 차단
            var deadline = Time.realtimeSinceStartup + 10f; // 전환 대기 제한
            while ((bool)property.GetValue(motion) && Time.realtimeSinceStartup < deadline) // 부드러운 전환 완료 대기
            { // 전환 대기 시작
                yield return null; // 다음 프레임 대기
            } // 전환 대기 끝
            Assert.That(property.GetValue(motion), Is.False); // 실제 전환 완료
            Assert.That(Vector3.Distance(initial, Camera.main.transform.position), Is.GreaterThan(0.1f)); // 전환 중 카메라 이동 확인
            var destinationArgs = new object[] // 도착 방 조회 인수
            { // 인수 목록 시작
                travel.RoomX, travel.RoomY // 실제 도착 방 좌표
            }; // 인수 목록 끝
            var destination = (GameObject)finder.Invoke(travel, destinationArgs); // 실제 도착 방 객체
            var destinationFloor = destination.transform.Find("Floor").GetComponent<Tilemap>(); // 도착 방 바닥
            var left = destinationFloor.GetCellCenterWorld(new Vector3Int(-6, 0, 0)); // 왼쪽 끝 타일 중심
            var right = destinationFloor.GetCellCenterWorld(new Vector3Int(6, 0, 0)); // 오른쪽 끝 타일 중심
            var halfWidth = Camera.main.orthographicSize * Camera.main.aspect; // 화면 가로의 절반
            var roomWidth = right.x - left.x + 2f; // 두 배 타일을 포함한 방 가로 크기
            if (halfWidth * 2f >= roomWidth) // 화면이 방보다 넓은 경우
            { // 가운데 정렬 확인 시작
                Assert.That(Camera.main.transform.position.x, Is.EqualTo((left.x + right.x) * 0.5f).Within(0.05f)); // 방보다 넓은 화면의 가운데 정렬
            } // 가운데 정렬 확인 끝
            else // 화면이 방보다 좁은 경우
            { // 화면 범위 확인 시작
                Assert.That(Camera.main.transform.position.x, Is.InRange(left.x - 1f + halfWidth - 0.05f, right.x + 1f - halfWidth + 0.05f)); // 방 바깥의 빈 화면 제한
            } // 화면 범위 확인 끝
            var viewport = Camera.main.WorldToViewportPoint(player.transform.position); // 플레이어의 화면 내 좌표
            Assert.That(viewport.x, Is.InRange(0f, 1f)); // 플레이어의 화면 가로 범위 유지
            Assert.That(viewport.y, Is.InRange(0f, 1f)); // 플레이어의 화면 세로 범위 유지
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                var image = CaptureScreen("DungeonCanvas"); // 실제 확대된 던전 화면
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 플레이 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
        } // 검증 끝
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
                var visible = travel.Discovery.GetVisibility(room.X, room.Y) != RoomVisibility.Hidden && MiniMapWindow.Contains(room.X, room.Y, travel.RoomX, travel.RoomY); // 공개된 방과 미니맵 범위 확인
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
            var inventory = AppRoot.Instance.Inventory; // 현재 인벤토리 보관
            inventory.Add("floor-test", "층 이동 시험", 1, 1); // 층 이동 전 아이템 획득
            var keys = AppRoot.Instance.Keys; // 현재 게임의 열쇠 보관소
            Assert.That(keys.Add(1), Is.True); // 층 이동 전 시험 열쇠 지급
            typeof(AppRoot).GetProperty("PlayerHealth").SetValue(AppRoot.Instance, 42); // 손상된 탐험 체력 모사
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
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(100)); // 성공한 층 이동의 체력 회복
            Assert.That(AppRoot.Instance.Inventory, Is.SameAs(inventory)); // 층 이동 시 기존 인벤토리 유지
            Assert.That(inventory.Slots[0].ItemId, Is.EqualTo("floor-test")); // 층 이동 시 아이템 보존
            Assert.That(AppRoot.Instance.Keys, Is.SameAs(keys)); // 층 이동 시 열쇠 보관소 유지
            Assert.That(keys.Count, Is.EqualTo(1)); // 층 이동 시 열쇠 수량 보존
            Assert.That(FloorActivationProbe.Activations, Is.EqualTo(1)); // 새 층 확정 전 활성화 방지
            Assert.That(travel.Layout, Is.Not.SameAs(oldLayout)); // 새로운 층 구조 확인
            Assert.That(travel.Discovery, Is.Not.SameAs(oldDiscovery)); // 독립된 방문 기록 확인
            Assert.That(travel.RoomX, Is.EqualTo(0)); // 새 층 시작 방 가로 좌표
            Assert.That(travel.RoomY, Is.EqualTo(0)); // 새 층 시작 방 세로 좌표
            Assert.That(player.X, Is.EqualTo(0)); // 새 층 시작 칸 가로 좌표
            Assert.That(player.Y, Is.EqualTo(0)); // 새 층 시작 칸 세로 좌표
            var newRoomArguments = new object[] // 새 층 시작 방 조회 인수
            { // 인수 목록 시작
                0, 0 // 새 층 시작 방 좌표
            }; // 인수 목록 끝
            var newRoot = (GameObject)finder.Invoke(travel, newRoomArguments); // 실제 새 층 시작 방
            var newFloor = newRoot.transform.Find("Floor").GetComponent<Tilemap>(); // 새 층 실제 바닥
            Assert.That(Vector3.Distance(newFloor.GetCellCenterWorld(Vector3Int.zero), newFloor.GetCellCenterWorld(Vector3Int.right)), Is.EqualTo(2f).Within(0.001f)); // 새 층 두 배 타일 유지
            Assert.That(newFloor.GetTile(Vector3Int.zero).name, Is.EqualTo("GeneratedDungeonFloor")); // 새 층의 생성 바닥 이미지
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
            typeof(AppRoot).GetProperty("PlayerHealth").SetValue(AppRoot.Instance, 42); // 손상된 탐험 체력 모사
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
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(42)); // 실패한 층 이동의 체력 보존
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
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(100)); // 재시도 성공 시 체력 회복
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
        [UnityTest] // 자동 조우와 전투 복귀 검증
        public IEnumerator EliteEncounter_OpensCombatAndReturnsToSamePosition() // 실제 이동으로 조우하고 원래 자리 복귀
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전 시드
            var rollOverride = typeof(CombatScreenView).GetProperty("RandomOverride"); // 검증용 전투 난수 연결 조회
            Assert.That(rollOverride, Is.Not.Null); // 결정 가능한 전투 난수 확인
            rollOverride.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => minimum)); // 명중과 최소 피해 편차 고정
            yield return EnterDungeon(); // 실제 던전 진입
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증용 키보드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 입력 준비 대기
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 방 이동 제어 조회
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어 조회
            var layout = travel.Layout; // 현재 층 구조
            var start = layout.GetRoom(travel.RoomX, travel.RoomY); // 출발 방 정의
            var target = layout.Rooms.First(room => room.Kind == RoomKind.Elite); // 상위 개체 방 정의
            var queue = new Queue<DungeonRoom>(); // 연결 방 탐색 대기열
            var previous = new Dictionary<DungeonRoom, RoomDoor>(); // 각 방의 이전 연결
            queue.Enqueue(start); // 시작 방 예약
            previous.Add(start, null); // 시작 방 방문 표시
            while (queue.Count > 0 && !previous.ContainsKey(target)) // 대상 방까지 연결 탐색
            { // 탐색 시작
                var current = queue.Dequeue(); // 탐색할 방 조회
                foreach (var door in layout.Doors) // 모든 출구 순회
                { // 출구 검사 시작
                    if (door.RoomX != current.X || door.RoomY != current.Y) // 현재 방의 출구 검사
                    { // 다른 방 조건 시작
                        continue; // 다른 방 출구 제외
                    } // 다른 방 조건 끝
                    var next = layout.GetRoom(door.TargetRoomX, door.TargetRoomY); // 연결 방 조회
                    if (previous.ContainsKey(next)) // 이미 찾은 방 검사
                    { // 중복 조건 시작
                        continue; // 중복 탐색 제외
                    } // 중복 조건 끝
                    previous.Add(next, door); // 이전 연결 보관
                    queue.Enqueue(next); // 연결 방 탐색 예약
                } // 출구 검사 끝
            } // 탐색 끝
            Assert.That(previous.ContainsKey(target), Is.True); // 상위 개체 방 도달 가능 확인
            var route = new List<RoomDoor>(); // 실제 이동 경로
            var cursor = target; // 도착 방부터 역추적
            while (cursor != start) // 시작 방까지 이전 연결 반복
            { // 경로 복구 시작
                var door = previous[cursor]; // 현재 방으로 들어온 문
                route.Add(door); // 연결 경로 보관
                cursor = layout.GetRoom(door.RoomX, door.RoomY); // 이전 방 이동
            } // 경로 복구 끝
            route.Reverse(); // 출발 방부터 이동 순서 정렬
            foreach (var door in route) // 실제 이동 경로 순회
            { // 방 이동 시작
                yield return UseDoor(travel, player, door); // 실제 키 입력으로 문 통과
            } // 방 이동 끝
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 현재 방 객체 조회
            var arguments = new object[] // 현재 방 좌표 인수
            { // 인수 목록 시작
                travel.RoomX, travel.RoomY // 상위 개체 방 좌표
            }; // 인수 목록 끝
            var root = (GameObject)finder.Invoke(travel, arguments); // 실제 상위 개체 방
            Assert.That(root.transform.Find("EncounterMarker"), Is.Not.Null); // 시험 몬스터 표시 확인
            var floor = root.transform.Find("Floor").GetComponent<Tilemap>(); // 현재 방 바닥
            var obstacles = root.transform.Find("Obstacles").GetComponent<Tilemap>(); // 현재 방 장애물
            Assert.That(player.ChangeRoom(floor, obstacles, 0, 2), Is.True); // 포착 범위 밖 배치
            yield return null; // 위치 변경 프레임 종료
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 범위 밖 탐험 유지
            var savedDiscovery = travel.Discovery; // 현재 층 방문 기록 보관
            var savedFloor = travel.Floor; // 현재 층 번호 보관
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S)); // 대상 방향 한 칸 이동
            InputSystem.Update(); // 방향 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 입력 해제
            InputSystem.Update(); // 해제 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S)); // 이동 중 추가 방향 입력
            InputSystem.Update(); // 대기 이동 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 대기 방향 해제
            InputSystem.Update(); // 해제 반영
            var deadline = Time.realtimeSinceStartup + 10f; // 이동 완료 대기 시간
            while (player.IsMoving && Time.realtimeSinceStartup < deadline) // 이동 완료 대기
            { // 대기 시작
                yield return null; // 다음 프레임 대기
            } // 대기 끝
            yield return null; // 조우 화면 갱신 대기
            Assert.That(player.X, Is.EqualTo(0)); // 도착 가로 칸 확인
            Assert.That(player.Y, Is.EqualTo(1)); // 포착 범위 도착 확인
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Encounter)); // 조우 선택 화면 진입 확인
            Assert.That(AppRoot.Instance.Input.IsExplorationEnabled, Is.False); // 조우 중 탐험 입력 잠금
            Assert.That(AppRoot.Instance.Input.IsUiEnabled, Is.True); // 조우 화면 입력 활성화
            Assert.That(GameObject.Find("BattleCanvas"), Is.Null); // 선택 전 전투 시작 차단
            var encounterCanvas = GameObject.Find("EncounterCanvas"); // 실제 조우 화면 조회
            var encounterButton = GameObject.Find("EncounterFightButton"); // 조우 전투 선택 버튼
            Assert.That(encounterCanvas, Is.Not.Null); // 조우 화면 존재 확인
            Assert.That(encounterButton, Is.Not.Null); // 실제 전투 선택 제공 확인
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(encounterButton)); // 키보드 기본 선택 확인
            yield return null; // 조우 초상화의 화면 크기 반영 대기
            yield return null; // 실제 조우 표시 안정화 대기
            var portrait = GameObject.Find("EncounterPortrait"); // 시험 대상의 초상화 조회
            Assert.That(portrait, Is.Not.Null); // 조우 초상화 존재 확인
            Assert.That(portrait.GetComponent<UnityEngine.UI.RawImage>().enabled, Is.True); // 실제 초상화 표시 확인
            Assert.That(portrait.GetComponent<RectTransform>().sizeDelta.x, Is.GreaterThan(0f)); // 표시 가능한 초상화 크기 확인
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                var image = CaptureScreen("EncounterCanvas"); // 실제 조우 선택 화면 렌더링
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 플레이 화면 저장
                Object.Destroy(image); // 캡처 이미지 정리
            } // 캡처 조건 끝
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 실제 조우 전투 선택 입력
            yield return null; // 조우 선택 처리 대기
            yield return null; // 선택 키를 누른 상태 유지
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 선택 후 전투 진입 확인
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(100)); // 누른 선택 키의 중복 공격 방지
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 조우 선택 키 해제
            yield return null; // 전투 화면 입력 준비 대기
            Assert.That(AppRoot.Instance.Input.IsExplorationEnabled, Is.False); // 전투 중 탐험 입력 잠금 확인
            Assert.That(AppRoot.Instance.Input.IsUiEnabled, Is.True); // 전투 화면 입력 확인
            var battle = GameObject.Find("BattleCanvas"); // 전투 화면 조회
            Assert.That(battle, Is.Not.Null); // 실제 전투 화면 표시 확인
            var button = GameObject.Find("ExitBattleButton"); // 시험 종료 버튼 조회
            var advance = GameObject.Find("AdvanceBattleButton"); // 기본 공격 버튼 조회
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(advance)); // 기본 공격 선택 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); // 전투 중 이동 입력
            InputSystem.Update(); // 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이동 키 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 입력 갱신 대기
            Assert.That(player.Y, Is.EqualTo(1)); // 전투 중 탐험 이동 차단
            var roundTextObject = GameObject.Find("RoundStatus"); // 실제 라운드 안내 조회
            Assert.That(roundTextObject, Is.Not.Null); // 라운드 안내 존재 확인
            var roundText = roundTextObject.GetComponent<UnityEngine.UI.Text>(); // 실제 라운드 글자
            Assert.That(roundText.text, Does.Contain("1라운드")); // 첫 라운드 표시 확인
            Assert.That(roundText.text, Does.Contain("플레이어")); // 첫 행동자 표시 확인
            Assert.That(advance, Is.Not.Null); // 행동 진행 버튼 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.UpArrow)); // 행동 진행 버튼 방향 입력
            yield return null; // 실제 화면 방향 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
            yield return null; // 방향 해제 반영
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(advance)); // 행동 진행 버튼 선택 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 행동 진행 선택 입력
            yield return null; // 실제 화면 선택 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 선택 해제 반영
            Assert.That(roundText.text, Does.Contain("2라운드")); // 적 행동 완료와 다음 라운드 확인
            var enemyHealth = GameObject.Find("EnemyHealth").GetComponent<UnityEngine.UI.Text>(); // 상대 체력 표시 조회
            var playerHealth = GameObject.Find("PlayerHealth").GetComponent<UnityEngine.UI.Text>(); // 플레이어 체력 표시 조회
            Assert.That(enemyHealth.text, Does.Contain("27/70")); // 기존 공식으로 적 체력 감소 확인
            Assert.That(playerHealth.text, Does.Contain("94/100")); // 자동 적 공격 후 체력 감소 확인
            var noticeText = GameObject.Find("Notice").GetComponent<UnityEngine.UI.Text>(); // 실제 공격 결과 안내
            Assert.That(noticeText.text, Does.Contain("플레이어 공격")); // 플레이어 공격 결과 유지 확인
            Assert.That(noticeText.text, Does.Contain("상위 개체 공격")); // 상대 공격 결과 함께 표시
            advance.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 두 번째 기본 공격
            Assert.That(roundText.text, Does.Contain("승리")); // 적 체력 소진과 승리 확인
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(button)); // 승리 후 복귀 버튼 선택 확인
            var submits = 0; // 화면 선택 입력 발생 횟수
            var clicks = 0; // 버튼 선택 처리 횟수
            var module = UnityEngine.EventSystems.EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); // 실제 화면 입력 모듈
            module.submit.action.performed += context => submits++; // 화면 선택 입력 관찰
            button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => clicks++); // 버튼 선택 결과 관찰
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 선택 버튼 확인 입력
            yield return null; // 입력 갱신과 화면 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 종료 화면 반영 대기
            Assert.That(submits, Is.EqualTo(1), "Submit action count"); // 입력 동작 발생 확인
            Assert.That(clicks, Is.EqualTo(1), "Button click count"); // 화면 모듈의 버튼 전달 확인
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 탐험 복귀 확인
            Assert.That(AppRoot.Instance.Input.IsExplorationEnabled, Is.True); // 탐험 입력 복구
            Assert.That(travel.Floor, Is.EqualTo(savedFloor)); // 현재 층 보존
            Assert.That(travel.RoomX, Is.EqualTo(target.X)); // 상위 개체 방 가로 좌표 보존
            Assert.That(travel.RoomY, Is.EqualTo(target.Y)); // 상위 개체 방 세로 좌표 보존
            Assert.That(player.X, Is.EqualTo(0)); // 플레이어 가로 칸 보존
            Assert.That(player.Y, Is.EqualTo(1)); // 플레이어 세로 칸 보존
            Assert.That(travel.Discovery, Is.SameAs(savedDiscovery)); // 기존 방문 기록 보존
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(94)); // 전투 후 현재 체력 보존
            Assert.That(root.transform.Find("EncounterMarker"), Is.Null); // 승리한 조우 대상 제거 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A)); // 기존 포착 범위 내 이동
            InputSystem.Update(); // 실제 이동 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이동 키 해제
            InputSystem.Update(); // 해제 반영
            while (player.IsMoving) // 이동 완료 대기
            { // 대기 시작
                yield return null; // 다음 프레임 대기
            } // 대기 끝
            yield return null; // 조우 여부 반영 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 완료된 조우 재발 방지
        } // 검증 끝
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
        [UnityTest] // 게임패드 전투 종료 검증
        public IEnumerator Combat_CanReturnUsingGamepad() // 게임패드 선택으로 탐험 복귀
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            var rollOverride = typeof(CombatScreenView).GetProperty("RandomOverride"); // 검증용 전투 난수 연결 조회
            Assert.That(rollOverride, Is.Not.Null); // 결정 가능한 전투 난수 확인
            rollOverride.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => minimum)); // 명중과 최소 편차 고정
            yield return EnterDungeon(); // 실제 던전 진입
            gamepad = InputSystem.AddDevice<Gamepad>(); // 검증용 게임패드 연결
            InputSystem.Update(); // 장치 연결 반영
            yield return null; // 장치 준비 대기
            Assert.That(AppRoot.Instance.StartCombat(), Is.True); // 실제 시험 전투 진입
            yield return null; // 화면과 선택 준비 대기
            var advance = GameObject.Find("AdvanceBattleButton"); // 시험 행동 진행 버튼
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(advance)); // 게임패드 기본 공격 선택 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadUp)); // 진행 버튼 방향 입력
            yield return null; // 방향 입력 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 방향 버튼 해제
            yield return null; // 해제 처리 대기
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(advance)); // 진행 버튼 선택 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 게임패드 행동 진행
            yield return null; // 실제 화면 입력 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 행동 처리 대기
            var roundText = GameObject.Find("RoundStatus").GetComponent<UnityEngine.UI.Text>(); // 라운드 안내 조회
            Assert.That(roundText.text, Does.Contain("2라운드")); // 게임패드 공격과 적 행동 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadDown)); // 종료 버튼 방향 입력
            yield return null; // 방향 입력 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 방향 버튼 해제
            yield return null; // 해제 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 게임패드 종료 선택
            yield return null; // 실제 종료 입력 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 종료 처리 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 게임패드 탐험 복귀 확인
            Assert.That(AppRoot.Instance.Input.IsExplorationEnabled, Is.True); // 탐험 입력 복구 확인
        } // 검증 끝
        [UnityTest] // 실제 전투 패배 흐름 검증
        public IEnumerator Combat_PlayerDefeat_ReturnsToTitle() // 체력 소진 후 패배 화면과 타이틀 복귀
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            var rollOverride = typeof(CombatScreenView).GetProperty("RandomOverride"); // 검증용 전투 난수 연결 조회
            Assert.That(rollOverride, Is.Not.Null); // 결정 가능한 전투 난수 확인
            rollOverride.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => minimum)); // 명중과 최소 편차 고정
            yield return EnterDungeon(); // 실제 던전 진입
            var healthProperty = typeof(AppRoot).GetProperty("PlayerHealth"); // 현재 플레이어 체력 조회
            Assert.That(healthProperty, Is.Not.Null); // 탐험 체력 보관 확인
            healthProperty.SetValue(AppRoot.Instance, 1); // 패배가 가능한 현재 체력 지정
            Assert.That(AppRoot.Instance.StartCombat(), Is.True); // 실제 시험 전투 시작
            yield return null; // 전투 화면 준비 대기
            var advance = GameObject.Find("AdvanceBattleButton").GetComponent<UnityEngine.UI.Button>(); // 기본 공격 버튼
            advance.onClick.Invoke(); // 플레이어와 적의 기본 공격 진행
            var roundText = GameObject.Find("RoundStatus").GetComponent<UnityEngine.UI.Text>(); // 실제 결과 표시 조회
            Assert.That(roundText.text, Does.Contain("패배")); // 플레이어 체력 소진에 따른 패배 확인
            Assert.That(AppRoot.Instance.PlayerHealth, Is.Zero); // 패배한 현재 체력 확인
            GameObject.Find("ExitBattleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 패배 결과에서 타이틀 복귀
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            Assert.That(AppRoot.Instance.Input.IsUiEnabled, Is.True); // 타이틀 입력 복구 확인
            AppRoot.Instance.StartNewGame(); // 새 게임 요청
            yield return WaitForPhase(GamePhase.Exploration); // 새 던전 도착 대기
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(100)); // 새 게임의 기본 체력 초기화
        } // 검증 끝
        [UnityTest] // 양측 공격 결과 표시 검증
        public IEnumerator Combat_MissAndEnemyHit_ShowBothResults() // 빗나감과 적중 메시지 동시 보존
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            var rolls = new Queue<int>(new[] // 명중과 편차 난수 순서
            { // 난수 목록 시작
                99, 0, 0, 0 // 플레이어 빗나감과 적의 명중
            }); // 난수 목록 끝
            CombatScreenView.RandomOverride = (minimum, maximum) => rolls.Dequeue(); // 결정 가능한 전투 난수
            yield return EnterDungeon(); // 실제 던전 진입
            Assert.That(AppRoot.Instance.StartCombat(), Is.True); // 실제 시험 전투 시작
            yield return null; // 전투 화면 준비 대기
            GameObject.Find("AdvanceBattleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 한 라운드 기본 공격
            var notice = GameObject.Find("Notice").GetComponent<UnityEngine.UI.Text>(); // 실제 결과 안내 조회
            Assert.That(notice.text, Does.Contain("플레이어 공격 · 빗나감")); // 플레이어 빗나감 결과 보존
            Assert.That(notice.text, Does.Contain("상위 개체 공격")); // 적 공격 결과 함께 표시
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(94)); // 적 공격에 따른 실제 체력 확인
        } // 검증 끝
        [UnityTest] // 같은 프레임의 중복 공격 방지 검증
        public IEnumerator Combat_DuplicateActionInOneFrame_IsIgnored() // 한 프레임의 두 번 공격 차단
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            CombatScreenView.RandomOverride = (minimum, maximum) => minimum; // 명중과 최소 피해 편차 고정
            yield return EnterDungeon(); // 실제 던전 진입
            Assert.That(AppRoot.Instance.StartCombat(), Is.True); // 실제 시험 전투 시작
            yield return null; // 전투 화면 준비 대기
            var attack = GameObject.Find("AdvanceBattleButton").GetComponent<UnityEngine.UI.Button>(); // 실제 공격 버튼
            var health = GameObject.Find("EnemyHealth").GetComponent<UnityEngine.UI.Text>(); // 상대 체력 표시
            attack.onClick.Invoke(); // 첫 공격 요청
            Assert.That(health.text, Does.Contain("27/70")); // 첫 공격의 실제 피해 확인
            attack.onClick.Invoke(); // 같은 프레임의 중복 요청
            Assert.That(health.text, Does.Contain("27/70")); // 중복 공격으로 인한 추가 피해 차단
            yield return null; // 다음 프레임 대기
            attack.onClick.Invoke(); // 다음 프레임의 새 공격 요청
            Assert.That(health.text, Does.Contain("0/70")); // 새 공격과 승리 처리 확인
        } // 검증 끝
        [UnityTest] // 시험 회복약 지급과 탐험 중 사용 검증
        public IEnumerator Inventory_Potion_HealsAndConsumesOnlyWhenEffective() // 실제 인벤토리 체력 회복 확인
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            AppRoot.Instance.UpdatePlayerHealth(80); // 피해를 받은 시험 체력
            AppRoot.Instance.OpenInventory(); // 인벤토리 화면 열기
            yield return null; // 화면 준비 대기
            var give = GameObject.Find("TrialPotionButton"); // 회복약 시험 지급 버튼
            var use = GameObject.Find("UsePotionButton"); // 회복약 사용 버튼
            Assert.That(give, Is.Not.Null); // 회복약 지급 제공 확인
            Assert.That(use, Is.Not.Null); // 회복약 사용 제공 확인
            give.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 첫 회복약 지급
            yield return null; // 중복 입력 방지 프레임 대기
            give.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 두 번째 회복약 지급
            Assert.That(AppRoot.Instance.Inventory.Slots[0].Quantity, Is.EqualTo(2)); // 동일 회복약 중첩 확인
            Assert.That(AppRoot.Instance.Inventory.Slots[0].StackLimit, Is.EqualTo(5)); // 시험 회복약 최대 중첩
            use.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 회복약 사용
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(100)); // 최대 체력까지만 회복
            Assert.That(AppRoot.Instance.Inventory.Slots[0].Quantity, Is.EqualTo(1)); // 실제 회복 후 한 개 소비
            yield return null; // 중복 입력 방지 프레임 대기
            use.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 최대 체력에서 다시 사용
            Assert.That(AppRoot.Instance.Inventory.Slots[0].Quantity, Is.EqualTo(1)); // 효과 없는 회복약 소비 차단
            Assert.That(GameObject.Find("InventoryHealth").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("100")); // 현재 체력 화면 반영
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                var image = CaptureScreen("InventoryCanvas"); // 실제 회복약 사용 화면
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 플레이 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
        } // 검증 끝
        [UnityTest] // 게임패드 회복약 사용과 상태 제한 검증
        public IEnumerator Inventory_Potion_GamepadAndPhaseRestrictions() // 실제 입력과 잘못된 상태 소비 차단
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            AppRoot.Instance.UpdatePlayerHealth(50); // 피해를 받은 시험 체력
            AppRoot.Instance.Inventory.Add("trial-small-potion", "시험 소형 회복약", 1, 5); // 시험 회복약 보관
            Assert.That(AppRoot.Instance.TryUseTrialPotion(out _), Is.False); // 탐험 화면에서 직접 사용 차단
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(50)); // 잘못된 상태의 체력 보존
            Assert.That(AppRoot.Instance.Inventory.Slots[0].Quantity, Is.EqualTo(1)); // 잘못된 상태의 아이템 보존
            gamepad = InputSystem.AddDevice<Gamepad>(); // 시험 게임패드 연결
            InputSystem.Update(); // 입력 장치 반영
            AppRoot.Instance.OpenInventory(); // 인벤토리 화면 열기
            yield return null; // 화면 준비 대기
            var use = GameObject.Find("UsePotionButton"); // 실제 회복약 사용 버튼
            Assert.That(use, Is.Not.Null); // 회복약 사용 제공 확인
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(use); // 회복약 사용 선택
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 실제 게임패드 사용
            yield return null; // 사용 결과 반영 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 해제 반영 대기
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(75)); // 기획 기준 체력 25 회복
            Assert.That(AppRoot.Instance.Inventory.Slots[0].IsEmpty, Is.True); // 마지막 회복약 소비와 슬롯 비움
        } // 검증 끝
        [UnityTest] // 가득 찬 상태의 회복약 교체 검증
        public IEnumerator Inventory_Potion_FullInventoryReplacesConfirmedStack() // 회복약 지급의 기존 획득 선택 재사용
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            for (var index = 0; index < 10; index++) // 기본 슬롯 채우기
            { // 아이템 추가 시작
                AppRoot.Instance.Inventory.Add("old-" + index, "기존 아이템 " + index, 1, 1); // 슬롯별 기존 아이템
            } // 아이템 추가 끝
            AppRoot.Instance.OpenInventory(); // 인벤토리 화면 열기
            yield return null; // 화면 준비 대기
            var give = GameObject.Find("TrialPotionButton"); // 회복약 시험 지급 버튼
            Assert.That(give, Is.Not.Null); // 회복약 지급 제공 확인
            give.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 공간 없는 회복약 지급
            GameObject.Find("ReplaceItemButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 기존 아이템 교체 선택
            GameObject.Find("InventorySlotButton_2").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 기존 슬롯 선택
            Assert.That(GameObject.Find("ReplaceConfirmMessage").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("회복약")); // 실제 새 아이템 표시
            GameObject.Find("ConfirmReplaceButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 교체 최종 확인
            Assert.That(AppRoot.Instance.Inventory.Slots[2].ItemId, Is.EqualTo("trial-small-potion")); // 확인된 회복약 획득
            Assert.That(AppRoot.Instance.Inventory.Slots[2].StackLimit, Is.EqualTo(5)); // 회복약 중첩 한도 보존
            Assert.That(AppRoot.Instance.Inventory.Slots[1].ItemId, Is.EqualTo("old-1")); // 다른 기존 슬롯 보존
        } // 검증 끝
        [UnityTest] // 전투 회복약 사용과 상대 행동 검증
        public IEnumerator CombatPotion_Success_ConsumesTurnAndEnemyActs() // 실제 회복과 한 행동 소모
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            AppRoot.Instance.UpdatePlayerHealth(50); // 피해를 받은 시험 체력
            AppRoot.Instance.Inventory.Add("trial-small-potion", "시험 소형 회복약", 2, 5); // 시험 회복약 두 개 보관
            CombatScreenView.RandomOverride = (minimum, maximum) => minimum; // 재현 가능한 상대 공격 난수
            Assert.That(AppRoot.Instance.StartCombat(), Is.True); // 실제 전투 진입
            yield return null; // 전투 화면 준비 대기
            var potion = GameObject.Find("BattlePotionButton"); // 전투 회복약 사용 버튼
            Assert.That(potion, Is.Not.Null); // 전투 회복약 선택 제공 확인
            keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 입력 준비 대기
            for (var index = 0; index < 3; index++) // 공격 선택에서 회복약까지 이동
            { // 키보드 선택 이동 시작
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); // 오른쪽 선택 입력
                yield return null; // 방향 입력 처리 대기
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
                yield return null; // 선택 변경 대기
            } // 키보드 선택 이동 끝
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(potion)); // 실제 회복약 버튼 선택
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 회복약 사용
            yield return null; // 행동과 상대 공격 반영 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 해제 반영 대기
            Assert.That(AppRoot.Instance.Inventory.Slots[0].Quantity, Is.EqualTo(1)); // 회복약 한 개 소비
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(69)); // 체력 25 회복 후 상대 피해 반영
            Assert.That(GameObject.Find("PlayerHealth").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("69/100")); // 실제 전투 체력 표시
            Assert.That(GameObject.Find("RoundStatus").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("2라운드")); // 한 행동과 상대 차례 소모
            Assert.That(GameObject.Find("Notice").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("회복")); // 실제 회복 결과 표시
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                var image = CaptureScreen("BattleCanvas"); // 실제 전투 회복 결과 화면
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 플레이 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
        } // 검증 끝
        [UnityTest] // 효과 없는 전투 회복약 사용 검증
        public IEnumerator CombatPotion_NoEffect_PreservesItemAndTurn() // 최대 체력에서 행동과 아이템 보존
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            AppRoot.Instance.Inventory.Add("trial-small-potion", "시험 소형 회복약", 1, 5); // 시험 회복약 보관
            Assert.That(AppRoot.Instance.StartCombat(), Is.True); // 실제 전투 진입
            yield return null; // 전투 화면 준비 대기
            var potion = GameObject.Find("BattlePotionButton"); // 전투 회복약 사용 버튼
            Assert.That(potion, Is.Not.Null); // 전투 회복약 선택 제공 확인
            potion.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 최대 체력에서 회복약 요청
            Assert.That(AppRoot.Instance.Inventory.Slots[0].Quantity, Is.EqualTo(1)); // 효과 없는 회복약 소비 차단
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(100)); // 기존 체력 보존
            Assert.That(GameObject.Find("RoundStatus").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("1라운드")); // 플레이어 행동 차례 유지
            Assert.That(GameObject.Find("Notice").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("사용할 수 없습니다")); // 사용 불가 안내
        } // 검증 끝
        [UnityTest] // 전투 중 회복약 부재와 행동 유지 검증
        public IEnumerator CombatPotion_Missing_PreservesPlayerAction() // 회복약이 없을 때 공격 가능 여부 확인
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            AppRoot.Instance.UpdatePlayerHealth(50); // 피해를 받은 시험 체력
            CombatScreenView.RandomOverride = (minimum, maximum) => minimum; // 재현 가능한 시험 공격 난수
            Assert.That(AppRoot.Instance.StartCombat(), Is.True); // 실제 전투 진입
            yield return null; // 전투 화면 준비 대기
            var potion = GameObject.Find("BattlePotionButton").GetComponent<UnityEngine.UI.Button>(); // 전투 회복약 버튼
            potion.onClick.Invoke(); // 보유하지 않은 회복약 사용 요청
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(50)); // 사용 실패 시 체력 보존
            Assert.That(GameObject.Find("RoundStatus").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("1라운드")); // 플레이어 행동 차례 유지
            GameObject.Find("AdvanceBattleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 유지된 플레이어 공격
            Assert.That(GameObject.Find("EnemyHealth").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("27/70")); // 사용 실패 후 공격 가능 확인
        } // 검증 끝
        [UnityTest] // 게임패드 전투 회복약 조작 검증
        public IEnumerator CombatPotion_Gamepad_UsesPotion() // 실제 게임패드 선택과 행동 처리
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            AppRoot.Instance.UpdatePlayerHealth(50); // 피해를 받은 시험 체력
            AppRoot.Instance.Inventory.Add("trial-small-potion", "시험 소형 회복약", 1, 5); // 시험 회복약 보관
            CombatScreenView.RandomOverride = (minimum, maximum) => minimum; // 재현 가능한 상대 공격 난수
            gamepad = InputSystem.AddDevice<Gamepad>(); // 시험 게임패드 연결
            InputSystem.Update(); // 입력 장치 반영
            Assert.That(AppRoot.Instance.StartCombat(), Is.True); // 실제 전투 진입
            yield return null; // 전투 화면 준비 대기
            for (var index = 0; index < 3; index++) // 공격 선택에서 회복약까지 이동
            { // 게임패드 선택 이동 시작
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadRight)); // 오른쪽 선택 입력
                yield return null; // 방향 입력 처리 대기
                InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 방향 버튼 해제
                yield return null; // 선택 변경 대기
            } // 게임패드 선택 이동 끝
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(GameObject.Find("BattlePotionButton"))); // 회복약 버튼 선택 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 실제 회복약 사용
            yield return null; // 행동과 상대 공격 반영 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 해제 반영 대기
            Assert.That(AppRoot.Instance.Inventory.Slots[0].IsEmpty, Is.True); // 마지막 회복약 소비
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(69)); // 회복 후 상대 피해 반영
        } // 검증 끝
        [UnityTest] // 전투 종료 후 회복약 차단 검증
        public IEnumerator CombatPotion_AfterVictory_IsDisabled() // 승리 후 아이템과 체력 보존
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            AppRoot.Instance.UpdatePlayerHealth(50); // 피해를 받은 시험 체력
            AppRoot.Instance.Inventory.Add("trial-small-potion", "시험 소형 회복약", 1, 5); // 시험 회복약 보관
            CombatScreenView.RandomOverride = (minimum, maximum) => minimum; // 재현 가능한 시험 공격 난수
            Assert.That(AppRoot.Instance.StartCombat(), Is.True); // 실제 전투 진입
            yield return null; // 전투 화면 준비 대기
            var attack = GameObject.Find("AdvanceBattleButton").GetComponent<UnityEngine.UI.Button>(); // 실제 공격 버튼
            attack.onClick.Invoke(); // 첫 번째 공격과 상대 행동
            yield return null; // 중복 입력 방지 프레임 대기
            attack.onClick.Invoke(); // 두 번째 공격과 승리
            var potion = GameObject.Find("BattlePotionButton").GetComponent<UnityEngine.UI.Button>(); // 실제 회복약 버튼
            Assert.That(potion.interactable, Is.False); // 승리 후 회복약 버튼 비활성화
            var health = AppRoot.Instance.PlayerHealth; // 승리한 현재 체력
            potion.onClick.Invoke(); // 잘못된 승리 후 회복약 요청
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(health)); // 종료 후 체력 보존
            Assert.That(AppRoot.Instance.Inventory.Slots[0].Quantity, Is.EqualTo(1)); // 종료 후 회복약 수량 보존
        } // 검증 끝
        [UnityTest] // 인벤토리 가득 참의 두고 가기 검증
        public IEnumerator Inventory_FullOffer_LeavePreservesAllItems() // 새 아이템 포기 시 기존 인벤토리 보존
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            for (var index = 0; index < 10; index++) // 기본 슬롯 채우기
            { // 아이템 추가 시작
                AppRoot.Instance.Inventory.Add("old-" + index, "기존 아이템 " + index, 1, 1); // 슬롯별 기존 아이템
            } // 아이템 추가 끝
            AppRoot.Instance.OpenInventory(); // 인벤토리 화면 열기
            yield return null; // 화면 준비 대기
            GameObject.Find("TrialPickupButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 공간 없는 시험 획득
            var leave = GameObject.Find("LeaveItemButton"); // 새 아이템 포기 버튼
            Assert.That(leave, Is.Not.Null); // 가득 참 선택 제공 확인
            Assert.That(GameObject.Find("TrialPickupButton").GetComponent<UnityEngine.UI.Button>().interactable, Is.False); // 선택 중 다른 획득 차단
            leave.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 새 아이템 두고 가기
            yield return null; // 선택 종료 대기
            Assert.That(GameObject.Find("LeaveItemButton"), Is.Null); // 획득 선택 닫기
            for (var index = 0; index < 10; index++) // 기존 슬롯 확인
            { // 슬롯 검사 시작
                Assert.That(AppRoot.Instance.Inventory.Slots[index].ItemId, Is.EqualTo("old-" + index)); // 모든 기존 아이템 보존
            } // 슬롯 검사 끝
        } // 검증 끝
        [UnityTest] // 교체 취소와 확인 검증
        public IEnumerator Inventory_FullOffer_ConfirmAndCancel() // 기존 묶음의 명시적인 교체 확인
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            for (var index = 0; index < 10; index++) // 기본 슬롯 채우기
            { // 아이템 추가 시작
                AppRoot.Instance.Inventory.Add("old-" + index, "기존 아이템 " + index, 1, 1); // 슬롯별 기존 아이템
            } // 아이템 추가 끝
            AppRoot.Instance.OpenInventory(); // 인벤토리 화면 열기
            yield return null; // 화면 준비 대기
            GameObject.Find("TrialPickupButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 공간 없는 시험 획득
            GameObject.Find("ReplaceItemButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 기존 아이템 교체 선택
            GameObject.Find("InventorySlotButton_2").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 교체할 기존 슬롯 선택
            var confirm = GameObject.Find("ConfirmReplaceButton"); // 기존 묶음 교체 확인 버튼
            Assert.That(confirm, Is.Not.Null); // 교체 확인 화면 제공
            Assert.That(GameObject.Find("ReplaceConfirmMessage").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("기존 아이템 2")); // 버릴 대상 안내 확인
            Assert.That(AppRoot.Instance.Inventory.Slots[2].ItemId, Is.EqualTo("old-2")); // 확인 전 기존 아이템 보존
            GameObject.Find("CancelReplaceButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 교체 취소
            yield return null; // 획득 선택 복귀 대기
            Assert.That(GameObject.Find("ReplaceItemButton"), Is.Not.Null); // 획득 선택 복귀 확인
            Assert.That(AppRoot.Instance.Inventory.Slots[2].ItemId, Is.EqualTo("old-2")); // 취소 후 기존 아이템 보존
            GameObject.Find("ReplaceItemButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 교체 다시 선택
            GameObject.Find("InventorySlotButton_2").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 동일한 슬롯 재선택
            yield return null; // 확인 화면 배치 대기
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                var image = CaptureScreen("InventoryCanvas"); // 실제 교체 확인 화면
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 플레이 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
            GameObject.Find("ConfirmReplaceButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 교체 최종 확인
            yield return null; // 교체 결과 반영 대기
            Assert.That(AppRoot.Instance.Inventory.Slots[2].ItemId, Is.EqualTo("ITEM_DAY80_TEST_DROP")); // 새 시험 아이템 획득
            Assert.That(AppRoot.Instance.Inventory.Slots[2].Quantity, Is.EqualTo(1)); // 새 아이템 수량 확인
            Assert.That(AppRoot.Instance.Inventory.Slots[1].ItemId, Is.EqualTo("old-1")); // 다른 슬롯 보존
            Assert.That(AppRoot.Instance.Inventory.Slots[3].ItemId, Is.EqualTo("old-3")); // 다음 슬롯 보존
        } // 검증 끝
        [UnityTest] // 획득 선택 전 이동 표시 초기화 검증
        public IEnumerator Inventory_FullOffer_ClearsPendingMoveIndicator() // 교체 대상 선택 시 오래된 이동 표시 제거
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            for (var index = 0; index < 10; index++) // 기본 슬롯 채우기
            { // 아이템 추가 시작
                AppRoot.Instance.Inventory.Add("old-" + index, "기존 아이템 " + index, 1, 1); // 슬롯별 기존 아이템
            } // 아이템 추가 끝
            AppRoot.Instance.OpenInventory(); // 인벤토리 화면 열기
            yield return null; // 화면 준비 대기
            GameObject.Find("InventorySlotButton_0").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 이동할 출발 슬롯 선택
            Assert.That(GameObject.Find("InventorySlot_0").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("▶")); // 출발 슬롯 표시 확인
            GameObject.Find("TrialPickupButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 가득 찬 상태의 획득 요청
            GameObject.Find("ReplaceItemButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 교체 대상 선택
            Assert.That(GameObject.Find("InventorySlot_0").GetComponent<UnityEngine.UI.Text>().text, Does.Not.Contain("▶")); // 이전 이동 표시 제거 확인
            Assert.That(AppRoot.Instance.Inventory.Slots[0].ItemId, Is.EqualTo("old-0")); // 표시 변경 중 기존 아이템 보존
        } // 검증 끝
        [UnityTest] // 교체 확인 중 기존 아이템 변경 보호 검증
        public IEnumerator Inventory_FullOffer_ChangedTargetIsNotOverwritten() // 확인 후 변경된 슬롯 보존
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            for (var index = 0; index < 10; index++) // 기본 슬롯 채우기
            { // 아이템 추가 시작
                AppRoot.Instance.Inventory.Add("old-" + index, "기존 아이템 " + index, 1, 1); // 슬롯별 기존 아이템
            } // 아이템 추가 끝
            AppRoot.Instance.OpenInventory(); // 인벤토리 화면 열기
            yield return null; // 화면 준비 대기
            GameObject.Find("TrialPickupButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 공간 없는 시험 획득
            GameObject.Find("ReplaceItemButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 교체 대상 선택
            GameObject.Find("InventorySlotButton_2").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 기존 슬롯 선택
            Assert.That(AppRoot.Instance.Inventory.Remove(2, 1, out _), Is.True); // 외부 상태 변경 모사
            Assert.That(AppRoot.Instance.Inventory.Add("different", "변경된 아이템", 1, 1).Added, Is.EqualTo(1)); // 변경된 슬롯 채우기
            GameObject.Find("ConfirmReplaceButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 오래된 확인 요청
            yield return null; // 획득 선택 복귀 대기
            Assert.That(AppRoot.Instance.Inventory.Slots[2].ItemId, Is.EqualTo("different")); // 변경된 아이템 덮어쓰기 차단
            Assert.That(GameObject.Find("FullOfferMessage").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("변경")); // 실제 화면의 취소 이유 안내
        } // 검증 끝
        [UnityTest] // 게임패드의 가득 참 교체 흐름 검증
        public IEnumerator Inventory_FullOffer_GamepadConfirmsReplacement() // 실제 게임패드 선택과 최종 확인
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            gamepad = InputSystem.AddDevice<Gamepad>(); // 시험 게임패드 연결
            InputSystem.Update(); // 입력 장치 반영
            for (var index = 0; index < 10; index++) // 기본 슬롯 채우기
            { // 아이템 추가 시작
                AppRoot.Instance.Inventory.Add("old-" + index, "기존 아이템 " + index, 1, 1); // 슬롯별 기존 아이템
            } // 아이템 추가 끝
            AppRoot.Instance.OpenInventory(); // 인벤토리 화면 열기
            yield return null; // 화면 준비 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 시험 아이템 획득 선택
            yield return null; // 획득 선택 반영 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 해제 반영 대기
            Assert.That(GameObject.Find("ReplaceItemButton"), Is.Not.Null); // 가득 참 선택 제공 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadRight)); // 교체 선택 이동
            yield return null; // 방향 입력 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 방향 버튼 해제
            yield return null; // 선택 변경 대기
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(GameObject.Find("ReplaceItemButton"))); // 교체 버튼 선택 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 교체 대상 선택 시작
            yield return null; // 대상 화면 반영 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 해제 반영 대기
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(GameObject.Find("InventorySlotButton_0"))); // 첫 대상 슬롯 선택 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 첫 슬롯의 교체 확인
            yield return null; // 확인 화면 반영 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 해제 반영 대기
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(GameObject.Find("CancelReplaceButton"))); // 안전한 취소 기본 선택
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadRight)); // 최종 교체 선택 이동
            yield return null; // 방향 입력 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 방향 버튼 해제
            yield return null; // 선택 변경 대기
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(GameObject.Find("ConfirmReplaceButton"))); // 최종 확인 선택
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 교체 최종 확정
            yield return null; // 교체 결과 반영 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 해제 반영 대기
            Assert.That(AppRoot.Instance.Inventory.Slots[0].ItemId, Is.EqualTo("ITEM_DAY80_TEST_DROP")); // 실제 게임패드 교체 확인
            Assert.That(AppRoot.Instance.Inventory.Slots[1].ItemId, Is.EqualTo("old-1")); // 다른 기존 아이템 보존
        } // 검증 끝
        [UnityTest] // 키보드 슬롯 이동과 취소 검증
        public IEnumerator Inventory_SlotMove_KeyboardAndCancel() // 실제 키보드 선택과 수량 보존
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드 연결
            InputSystem.Update(); // 입력 장치 반영
            AppRoot.Instance.Inventory.Add("potion", "물약", 3, 5); // 시험용 아이템 보관
            AppRoot.Instance.OpenInventory(); // 인벤토리 화면 열기
            yield return null; // 화면 준비 대기
            var first = GameObject.Find("InventorySlotButton_0"); // 출발 슬롯 버튼
            var second = GameObject.Find("InventorySlotButton_1"); // 도착 슬롯 버튼
            Assert.That(first, Is.Not.Null); // 슬롯 선택 제공 확인
            Assert.That(second, Is.Not.Null); // 빈 슬롯 선택 제공 확인
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(first); // 출발 슬롯 선택
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 출발 슬롯 확정
            yield return null; // 선택 반영 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 키 해제 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 이동 선택 취소
            yield return null; // 취소 반영 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 취소 키 해제
            yield return null; // 키 해제 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Inventory)); // 이동 취소 후 화면 유지
            Assert.That(AppRoot.Instance.Inventory.Slots[0].Quantity, Is.EqualTo(3)); // 취소한 아이템 보존
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(first); // 출발 슬롯 재선택
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 출발 슬롯 확정
            yield return null; // 선택 반영 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 키 해제 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.DownArrow)); // 아래 도착 슬롯 이동
            yield return null; // 방향 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
            yield return null; // 선택 이동 대기
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(second)); // 실제 도착 슬롯 선택
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 이동 확정
            yield return null; // 이동 반영 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 키 해제 대기
            Assert.That(AppRoot.Instance.Inventory.Slots[0].IsEmpty, Is.True); // 원래 슬롯 비움
            Assert.That(AppRoot.Instance.Inventory.Slots[1].Quantity, Is.EqualTo(3)); // 이동된 수량 보존
        } // 검증 끝
        [UnityTest] // 게임패드 슬롯 교환 검증
        public IEnumerator Inventory_SlotMove_GamepadExchangesStacks() // 실제 게임패드 이동과 교환
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            gamepad = InputSystem.AddDevice<Gamepad>(); // 시험 게임패드 연결
            InputSystem.Update(); // 입력 장치 반영
            AppRoot.Instance.Inventory.Add("potion", "물약", 3, 5); // 첫 아이템
            AppRoot.Instance.Inventory.Add("herb", "약초", 2, 3); // 두 번째 아이템
            AppRoot.Instance.OpenInventory(); // 인벤토리 화면 열기
            yield return null; // 화면 준비 대기
            var first = GameObject.Find("InventorySlotButton_0"); // 출발 슬롯 버튼
            var second = GameObject.Find("InventorySlotButton_1"); // 도착 슬롯 버튼
            Assert.That(first, Is.Not.Null); // 슬롯 선택 제공 확인
            for (var index = 0; index < 5; index++) // 기본 획득 버튼에서 첫 슬롯까지 이동
            { // 게임패드 선택 이동 시작
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadUp)); // 위쪽 선택 입력
                yield return null; // 방향 입력 처리 대기
                InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 방향 버튼 해제
                yield return null; // 선택 변경 반영 대기
            } // 게임패드 선택 이동 끝
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(first)); // 기본 선택에서 첫 슬롯 접근 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 출발 슬롯 확정
            yield return null; // 선택 반영 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 버튼 해제
            yield return null; // 해제 반영 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadDown)); // 아래 도착 슬롯 이동
            yield return null; // 방향 입력 반영
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 방향 버튼 해제
            yield return null; // 선택 이동 대기
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(second)); // 실제 도착 슬롯 선택
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 이동 확정
            yield return null; // 교환 반영 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 해제 반영 대기
            Assert.That(AppRoot.Instance.Inventory.Slots[0].ItemId, Is.EqualTo("herb")); // 첫 슬롯 교환 결과
            Assert.That(AppRoot.Instance.Inventory.Slots[1].ItemId, Is.EqualTo("potion")); // 두 번째 슬롯 교환 결과
            Assert.That(AppRoot.Instance.Inventory.Slots[1].Quantity, Is.EqualTo(3)); // 물약 수량 보존
        } // 검증 끝
        [UnityTest] // 드래그 중 다른 입력의 중복 변경 차단 검증
        public IEnumerator Inventory_SlotMove_DragBlocksOtherChanges() // 드래그 도중 다른 입력 차단
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            AppRoot.Instance.Inventory.Add("potion", "물약", 3, 5); // 시험용 아이템 보관
            AppRoot.Instance.OpenInventory(); // 인벤토리 화면 열기
            yield return null; // 화면 준비 대기
            var first = GameObject.Find("InventorySlotButton_0"); // 출발 슬롯 버튼
            var second = GameObject.Find("InventorySlotButton_1"); // 도착 슬롯 버튼
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current); // 시험 마우스 이벤트
            pointer.pointerDrag = first; // 드래그 출발 슬롯 지정
            UnityEngine.EventSystems.ExecuteEvents.Execute(first, pointer, UnityEngine.EventSystems.ExecuteEvents.beginDragHandler); // 실제 슬롯 드래그 시작
            GameObject.Find("TrialPickupButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 드래그 중 시험 아이템 획득 요청
            second.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 드래그 중 다른 슬롯 선택 요청
            Assert.That(AppRoot.Instance.Inventory.Slots[1].IsEmpty, Is.True); // 드래그 중 아이템 추가 차단
            UnityEngine.EventSystems.ExecuteEvents.Execute(second, pointer, UnityEngine.EventSystems.ExecuteEvents.dropHandler); // 도착 슬롯에 놓기
            UnityEngine.EventSystems.ExecuteEvents.Execute(first, pointer, UnityEngine.EventSystems.ExecuteEvents.endDragHandler); // 드래그 종료
            Assert.That(AppRoot.Instance.Inventory.Slots[0].IsEmpty, Is.True); // 출발 슬롯 비움
            Assert.That(AppRoot.Instance.Inventory.Slots[1].Quantity, Is.EqualTo(3)); // 드래그한 수량 보존
        } // 검증 끝
        [UnityTest] // 가상 마우스의 실제 슬롯 드래그 검증
        public IEnumerator Inventory_SlotMove_ActualMouseDrag() // 실제 입력 장치로 슬롯 이동
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            mouse = InputSystem.AddDevice<Mouse>(); // 시험 마우스 연결
            InputSystem.Update(); // 입력 장치 반영
            AppRoot.Instance.Inventory.Add("potion", "물약", 3, 5); // 시험용 아이템 보관
            AppRoot.Instance.OpenInventory(); // 인벤토리 화면 열기
            yield return null; // 화면 준비 대기
            yield return null; // 화면 위치 반영 대기
            var first = GameObject.Find("InventorySlotButton_0").GetComponent<RectTransform>(); // 출발 슬롯 위치
            var second = GameObject.Find("InventorySlotButton_1").GetComponent<RectTransform>(); // 도착 슬롯 위치
            var start = RectTransformUtility.WorldToScreenPoint(null, first.position); // 출발 슬롯 화면 좌표
            var end = RectTransformUtility.WorldToScreenPoint(null, second.position); // 도착 슬롯 화면 좌표
            var mouseState = new MouseState(); // 시험 마우스 상태
            mouseState.position = start; // 출발 위치 지정
            InputSystem.QueueStateEvent(mouse, mouseState); // 출발 위치로 마우스 이동
            yield return null; // 위치 반영 대기
            InputSystem.QueueStateEvent(mouse, mouseState.WithButton(MouseButton.Left)); // 출발 슬롯 누름
            yield return null; // 누름 처리 대기
            mouseState.position = end; // 도착 위치 지정
            InputSystem.QueueStateEvent(mouse, mouseState.WithButton(MouseButton.Left)); // 도착 슬롯으로 드래그
            yield return null; // 드래그 처리 대기
            yield return null; // 도착 슬롯 반영 대기
            mouseState = new MouseState(); // 버튼이 해제된 마우스 상태
            mouseState.position = end; // 도착 위치 유지
            InputSystem.QueueStateEvent(mouse, mouseState); // 도착 슬롯에서 버튼 해제
            yield return null; // 놓기 처리 대기
            Assert.That(AppRoot.Instance.Inventory.Slots[0].IsEmpty, Is.True); // 실제 입력의 출발 슬롯 비움
            Assert.That(AppRoot.Instance.Inventory.Slots[1].Quantity, Is.EqualTo(3)); // 실제 입력의 수량 보존
        } // 검증 끝
        [UnityTest] // 드래그와 바깥 취소 검증
        public IEnumerator Inventory_SlotMove_DragDropAndOutsideCancel() // 실제 슬롯의 드래그 이벤트 전달
        { // 검증 시작
            yield return EnterDungeon(); // 실제 던전 진입
            AppRoot.Instance.Inventory.Add("potion", "물약", 3, 5); // 시험용 아이템 보관
            AppRoot.Instance.OpenInventory(); // 인벤토리 화면 열기
            yield return null; // 화면 준비 대기
            var first = GameObject.Find("InventorySlotButton_0"); // 출발 슬롯 버튼
            var second = GameObject.Find("InventorySlotButton_1"); // 도착 슬롯 버튼
            Assert.That(first, Is.Not.Null); // 슬롯 드래그 제공 확인
            Assert.That(second, Is.Not.Null); // 도착 슬롯 제공 확인
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current); // 시험 마우스 이벤트
            pointer.pointerDrag = first; // 드래그 출발 슬롯 지정
            UnityEngine.EventSystems.ExecuteEvents.Execute(first, pointer, UnityEngine.EventSystems.ExecuteEvents.beginDragHandler); // 실제 슬롯 드래그 시작
            UnityEngine.EventSystems.ExecuteEvents.Execute(first, pointer, UnityEngine.EventSystems.ExecuteEvents.endDragHandler); // 슬롯 밖 드래그 종료
            Assert.That(AppRoot.Instance.Inventory.Slots[0].Quantity, Is.EqualTo(3)); // 슬롯 밖 취소 시 수량 보존
            UnityEngine.EventSystems.ExecuteEvents.Execute(first, pointer, UnityEngine.EventSystems.ExecuteEvents.beginDragHandler); // 다시 드래그 시작
            UnityEngine.EventSystems.ExecuteEvents.Execute(second, pointer, UnityEngine.EventSystems.ExecuteEvents.dropHandler); // 실제 도착 슬롯에 놓기
            UnityEngine.EventSystems.ExecuteEvents.Execute(first, pointer, UnityEngine.EventSystems.ExecuteEvents.endDragHandler); // 드래그 종료
            Assert.That(AppRoot.Instance.Inventory.Slots[0].IsEmpty, Is.True); // 원래 슬롯 비움
            Assert.That(AppRoot.Instance.Inventory.Slots[1].Quantity, Is.EqualTo(3)); // 이동한 아이템 수량 보존
            yield return null; // 화면 배치 반영 대기
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                var image = CaptureScreen("InventoryCanvas"); // 실제 인벤토리 화면
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 플레이 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
        } // 검증 끝
        [UnityTest] // 키보드 인벤토리 왕복과 새 게임 검증
        public IEnumerator Inventory_OpensAddsClosesAndResets() // 실제 인벤토리 입력과 상태 확인
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            yield return EnterDungeon(); // 실제 던전 진입
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증용 키보드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 입력 준비 대기
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 현재 방 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var savedX = player.X; // 현재 플레이어 가로 위치
            var savedY = player.Y; // 현재 플레이어 세로 위치
            var savedFloor = travel.Floor; // 현재 층 번호
            var savedDiscovery = travel.Discovery; // 현재 방문 기록
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.I)); // 인벤토리 열기 입력
            InputSystem.Update(); // 탐험 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 열기 키 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 실제 화면 준비 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Inventory)); // 인벤토리 화면 상태 확인
            Assert.That(AppRoot.Instance.Input.IsExplorationEnabled, Is.False); // 탐험 입력 잠금 확인
            Assert.That(GameObject.Find("InventoryCanvas"), Is.Not.Null); // 실제 인벤토리 화면 확인
            var property = typeof(AppRoot).GetProperty("Inventory"); // 실제 인벤토리 상태 조회
            Assert.That(property, Is.Not.Null); // 인벤토리 상태 제공 확인
            var inventory = (InventoryState)property.GetValue(AppRoot.Instance); // 현재 게임의 인벤토리
            Assert.That(inventory.Slots.Count, Is.EqualTo(10)); // 기존 기본 슬롯 수 확인
            var pickup = GameObject.Find("TrialPickupButton"); // 시험 아이템 획득 버튼
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(pickup)); // 키보드 기본 선택 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 실제 시험 획득 입력
            yield return null; // 화면 입력 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 획득 결과 반영 대기
            Assert.That(inventory.Slots[0].ItemId, Is.EqualTo("ITEM_DAY80_TEST_DROP")); // 기존 시험 아이템 식별자 확인
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1)); // 실제 획득 수량 확인
            var slot = GameObject.Find("InventorySlot_0").GetComponent<UnityEngine.UI.Text>(); // 화면의 첫 슬롯
            Assert.That(slot.text, Does.Contain("시험 아이템")); // 실제 슬롯 표시 확인
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                var image = CaptureScreen("InventoryCanvas"); // 실제 인벤토리 화면 렌더링
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.I)); // 같은 키로 인벤토리 닫기
            yield return null; // 실제 화면 취소 입력 처리
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 닫기 키 해제
            yield return null; // 탐험 복귀 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 기존 탐험 상태 복구
            Assert.That(player.X, Is.EqualTo(savedX)); // 기존 가로 위치 보존
            Assert.That(player.Y, Is.EqualTo(savedY)); // 기존 세로 위치 보존
            Assert.That(travel.Floor, Is.EqualTo(savedFloor)); // 기존 층 보존
            Assert.That(travel.Discovery, Is.SameAs(savedDiscovery)); // 기존 방문 기록 보존
            Assert.That(property.GetValue(AppRoot.Instance), Is.SameAs(inventory)); // 인벤토리 내용 유지
            Assert.That(AppRoot.Instance.Keys.Add(1), Is.True); // 새 게임 전 시험 열쇠 지급
            AppRoot.Instance.ReturnToTitle(); // 타이틀 복귀
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            AppRoot.Instance.StartNewGame(); // 새 게임 요청
            yield return WaitForPhase(GamePhase.Exploration); // 새 던전 도착 대기
            Assert.That(AppRoot.Instance.Keys.Count, Is.Zero); // 새 게임의 열쇠 초기화 확인
            var fresh = (InventoryState)property.GetValue(AppRoot.Instance); // 새 게임의 인벤토리
            Assert.That(fresh, Is.Not.SameAs(inventory)); // 새 게임의 독립 인벤토리
            Assert.That(fresh.Slots[0].IsEmpty, Is.True); // 새 게임의 빈 슬롯 확인
        } // 검증 끝
        [UnityTest] // 게임패드 인벤토리 왕복 검증
        public IEnumerator Inventory_GamepadToggle_PreservesExploration() // 게임패드 열기와 닫기
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            yield return EnterDungeon(); // 실제 던전 진입
            gamepad = InputSystem.AddDevice<Gamepad>(); // 검증용 게임패드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 입력 준비 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North)); // 인벤토리 열기 입력
            InputSystem.Update(); // 탐험 입력 반영
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 열기 버튼 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 실제 화면 준비 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Inventory)); // 게임패드 인벤토리 열기 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North)); // 같은 버튼으로 닫기
            yield return null; // 화면 취소 입력 처리
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 닫기 버튼 해제
            yield return null; // 탐험 복귀 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 게임패드 탐험 복귀 확인
        } // 검증 끝
        [UnityTest] // 이동 중 인벤토리 입력 잠금 검증
        public IEnumerator Inventory_DuringMovement_PausesAndDiscardsBufferedMove() // 이동 중 열기와 대기 입력 보존 검사
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            yield return EnterDungeon(); // 실제 던전 진입
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증용 키보드 연결
            InputSystem.Update(); // 장치 연결 반영
            yield return null; // 입력 준비 대기
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어 조회
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); // 첫 이동 입력
            InputSystem.Update(); // 첫 이동 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 첫 이동 키 해제
            InputSystem.Update(); // 해제 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); // 이동 중 대기 입력
            InputSystem.Update(); // 대기 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 대기 키 해제
            InputSystem.Update(); // 해제 반영
            Assert.That(player.IsMoving, Is.True); // 첫 칸 이동 중 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.I)); // 이동 중 인벤토리 열기
            InputSystem.Update(); // 인벤토리 열기 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 열기 키 해제
            InputSystem.Update(); // 해제 반영
            var pausedPosition = player.transform.position; // 열린 화면의 현재 표시 위치
            yield return null; // 한 프레임 대기
            yield return null; // 추가 프레임 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Inventory)); // 인벤토리 상태 확인
            Assert.That(player.transform.position, Is.EqualTo(pausedPosition)); // 열린 동안 이동 정지 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); // 인벤토리 닫기 입력
            yield return null; // 화면 취소 입력 처리
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 닫기 키 해제
            yield return null; // 탐험 복귀 대기
            var deadline = Time.realtimeSinceStartup + 10f; // 이동 완료 대기 제한
            while (player.IsMoving && Time.realtimeSinceStartup < deadline) // 진행 중인 첫 이동 대기
            { // 이동 대기 시작
                yield return null; // 다음 프레임 대기
            } // 이동 대기 끝
            Assert.That(player.IsMoving, Is.False); // 이동 완료 확인
            Assert.That(player.Y, Is.EqualTo(1)); // 대기 중인 두 번째 이동 취소 확인
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 탐험 입력 복구 확인
        } // 검증 끝
        [UnityTest] // 인벤토리 단축키 유지 검증
        public IEnumerator Inventory_HeldKeyboardToggle_DoesNotImmediatelyClose() // 누른 키 유지 시 재전환 방지
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            yield return EnterDungeon(); // 실제 던전 진입
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증용 키보드 연결
            InputSystem.Update(); // 장치 연결 반영
            yield return null; // 입력 준비 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.I)); // 인벤토리 열기 키 누름
            InputSystem.Update(); // 열기 요청 반영
            yield return null; // 누른 상태 유지 대기
            yield return null; // 추가 프레임 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Inventory)); // 누른 키의 즉시 닫기 방지
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 열기 키 해제
            yield return null; // 해제 반영 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.I)); // 새 닫기 키 누름
            yield return null; // 화면 취소 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 닫기 키 해제
            yield return null; // 탐험 복귀 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 새 입력의 닫기 확인
        } // 검증 끝
        [UnityTest] // 게임패드 단축키 유지 검증
        public IEnumerator Inventory_HeldGamepadToggle_DoesNotImmediatelyClose() // 누른 버튼 유지 시 재전환 방지
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            yield return EnterDungeon(); // 실제 던전 진입
            gamepad = InputSystem.AddDevice<Gamepad>(); // 검증용 게임패드 연결
            InputSystem.Update(); // 장치 연결 반영
            yield return null; // 입력 준비 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North)); // 인벤토리 열기 버튼 누름
            InputSystem.Update(); // 열기 요청 반영
            yield return null; // 누른 상태 유지 대기
            yield return null; // 추가 프레임 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Inventory)); // 누른 버튼의 즉시 닫기 방지
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 열기 버튼 해제
            yield return null; // 해제 반영 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North)); // 새 닫기 버튼 누름
            yield return null; // 화면 취소 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 닫기 버튼 해제
            yield return null; // 탐험 복귀 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 새 입력의 닫기 확인
        } // 검증 끝
        [UnityTest] // 대상이 없는 시험 조우의 회유 차단 검증
        public IEnumerator EncounterPersuade_WithoutElite_DisablesPersuasion() // 완료할 대상이 없을 때 조우 조작 유지
        { // 검증 시작
            yield return EnterDungeon(); // 실제 시작 방 진입
            Assert.That(AppRoot.Instance.StartEncounter(), Is.True); // 대상 없는 시험 조우 화면 진입
            yield return null; // 조우 화면 준비 대기
            var persuade = GameObject.Find("EncounterPersuadeButton").GetComponent<UnityEngine.UI.Button>(); // 실제 회유 버튼
            Assert.That(persuade.interactable, Is.False); // 회유할 대상이 없을 때 선택 차단
            Assert.That(GameObject.Find("EncounterFightButton").GetComponent<UnityEngine.UI.Button>().interactable, Is.True); // 다른 조우 선택 유지
        } // 검증 끝
        [UnityTest] // 회유 성공의 평화적 조우 완료 검증
        public IEnumerator EncounterPersuade_Success_CompletesWithoutReward() // 실제 회유와 대상 제거 확인
        { // 검증 시작
            var rolls = new Queue<int>(); // 결정 가능한 회유 주사위
            rolls.Enqueue(10); // 플레이어 주사위
            rolls.Enqueue(10); // 대상 주사위
            var property = typeof(EncounterScreenView).GetProperty("PersuadeRollOverride"); // 회유 주사위 연결 조회
            Assert.That(property, Is.Not.Null); // 회유 주사위 제공 확인
            property.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => rolls.Dequeue())); // 동점 성공 지정
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            var persuade = GameObject.Find("EncounterPersuadeButton"); // 실제 회유 선택 버튼
            Assert.That(persuade, Is.Not.Null); // 회유 선택 제공 확인
            var layout = GameObject.Find("EncounterCanvas").GetComponent<CombatActionLayout>(); // 실제 네 행동 배치
            layout.Refresh(900f); // 좁은 화면 너비 적용
            var firstRect = GameObject.Find("EncounterFightButton").GetComponent<RectTransform>(); // 첫 행동 버튼 위치
            var lastRect = persuade.GetComponent<RectTransform>(); // 마지막 행동 버튼 위치
            Assert.That(-firstRect.anchoredPosition.x + firstRect.sizeDelta.x * 0.5f, Is.LessThanOrEqualTo(418.01f)); // 왼쪽 버튼의 화면 안쪽 여백
            Assert.That(lastRect.anchoredPosition.x + lastRect.sizeDelta.x * 0.5f, Is.LessThanOrEqualTo(418.01f)); // 오른쪽 버튼의 화면 안쪽 여백
            layout.Refresh(1920f); // 기준 화면 버튼 배치 복구
            var health = AppRoot.Instance.PlayerHealth; // 기존 플레이어 체력
            var keys = AppRoot.Instance.Keys.Count; // 기존 열쇠 수
            for (var index = 0; index < 3; index++) // 전투 선택에서 회유까지 이동
            { // 키보드 선택 이동 시작
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); // 오른쪽 선택 입력
                yield return null; // 방향 입력 반영 대기
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
                yield return null; // 선택 변경 반영 대기
            } // 키보드 선택 이동 끝
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(persuade)); // 키보드 회유 선택 확인
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                var image = CaptureScreen("EncounterCanvas"); // 실제 조우 선택 화면
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 플레이 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 실제 회유 선택
            yield return null; // 조우 종료 반영 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 입력 해제 반영 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 평화적 탐험 복귀
            Assert.That(GameObject.Find("EncounterMarker"), Is.Null); // 시험 대상 조우 완료
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(health)); // 체력 보존 확인
            Assert.That(AppRoot.Instance.Keys.Count, Is.EqualTo(keys)); // 추가 보상 없는 종료
            Assert.That(rolls.Count, Is.Zero); // 두 주사위만 사용
        } // 검증 끝
        [UnityTest] // 회유 성공 후 조우 완료 오류 검증
        public IEnumerator EncounterPersuade_FailedCompletion_RetriesWithoutReroll() // 성공 판정 보존과 조우 완료 재시도
        { // 검증 시작
            var rolls = new Queue<int>(); // 결정 가능한 회유 주사위
            rolls.Enqueue(10); // 플레이어 주사위
            rolls.Enqueue(10); // 대상 주사위
            var property = typeof(EncounterScreenView).GetProperty("PersuadeRollOverride"); // 회유 주사위 연결 조회
            Assert.That(property, Is.Not.Null); // 회유 주사위 제공 확인
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 현재 방 이동 제어
            var field = typeof(RoomTravelController).GetField("player", BindingFlags.Instance | BindingFlags.NonPublic); // 시험 플레이어 연결 조회
            var player = field.GetValue(travel); // 원래 플레이어 연결 보관
            property.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => // 판정 직후 완료 실패 모사
            { // 시험 주사위 시작
                var value = rolls.Dequeue(); // 지정한 주사위 사용
                if (rolls.Count == 0) // 두 번째 주사위 검사
                { // 완료 실패 조건 시작
                    field.SetValue(travel, null); // 판정 직후 플레이어 연결 해제
                } // 완료 실패 조건 끝
                return value; // 지정한 주사위 전달
            })); // 시험 주사위 끝
            var persuade = GameObject.Find("EncounterPersuadeButton").GetComponent<UnityEngine.UI.Button>(); // 실제 회유 버튼
            persuade.onClick.Invoke(); // 성공한 회유와 완료 실패 요청
            field.SetValue(travel, player); // 검증 중 원래 플레이어 연결 복구
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Encounter)); // 실패 후 조우 화면 유지
            Assert.That(GameObject.Find("EncounterFightButton").GetComponent<UnityEngine.UI.Button>().interactable, Is.False); // 성공 후 전투 선택 차단
            Assert.That(rolls.Count, Is.Zero); // 성공 판정의 주사위 보존
            yield return null; // 중복 선택 방지 프레임 대기
            persuade.onClick.Invoke(); // 성공 판정의 조우 완료 재시도
            yield return null; // 탐험 화면 반영 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 평화적 조우 완료 재시도
            Assert.That(rolls.Count, Is.Zero); // 추가 주사위 없이 완료
        } // 검증 끝
        [UnityTest] // 회유 실패의 일반 전투 검증
        public IEnumerator EncounterPersuade_Failure_StartsCombat() // 실패 시 실제 전투 진입
        { // 검증 시작
            var rolls = new Queue<int>(); // 결정 가능한 회유 주사위
            rolls.Enqueue(1); // 플레이어 주사위
            rolls.Enqueue(20); // 대상 주사위
            var property = typeof(EncounterScreenView).GetProperty("PersuadeRollOverride"); // 회유 주사위 연결 조회
            Assert.That(property, Is.Not.Null); // 회유 주사위 제공 확인
            property.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => rolls.Dequeue())); // 실패 판정 지정
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            var persuade = GameObject.Find("EncounterPersuadeButton"); // 실제 회유 버튼
            Assert.That(persuade, Is.Not.Null); // 회유 선택 제공 확인
            gamepad = InputSystem.AddDevice<Gamepad>(); // 시험 게임패드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 입력 준비 대기
            for (var index = 0; index < 3; index++) // 전투 선택에서 회유까지 이동
            { // 게임패드 선택 이동 시작
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadRight)); // 오른쪽 선택 입력
                yield return null; // 방향 입력 반영 대기
                InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 방향 버튼 해제
                yield return null; // 선택 변경 반영 대기
            } // 게임패드 선택 이동 끝
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(persuade)); // 게임패드 회유 선택 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 실제 회유 선택
            yield return null; // 실제 전투 화면 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 입력 해제 반영 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 실패 후 일반 전투 확인
            Assert.That(rolls.Count, Is.Zero); // 두 주사위만 사용
        } // 검증 끝
        [UnityTest] // 회유 실패 후 전투 생성 오류 검증
        public IEnumerator EncounterPersuade_FailedCombatCreation_DoesNotReroll() // 실패 판정 보존과 전투 재시도
        { // 검증 시작
            var rolls = new Queue<int>(); // 결정 가능한 회유 주사위
            rolls.Enqueue(1); // 플레이어 주사위
            rolls.Enqueue(20); // 대상 주사위
            var property = typeof(EncounterScreenView).GetProperty("PersuadeRollOverride"); // 회유 주사위 연결 조회
            Assert.That(property, Is.Not.Null); // 회유 주사위 제공 확인
            property.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => rolls.Dequeue())); // 실패 판정 지정
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            var persuade = GameObject.Find("EncounterPersuadeButton").GetComponent<UnityEngine.UI.Button>(); // 회유 버튼
            var eventSystem = UnityEngine.EventSystems.EventSystem.current; // 실제 화면 입력 관리자
            eventSystem.gameObject.SetActive(false); // 전투 화면 생성 실패 모사
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("^Combat screen failed:")); // 예상한 전투 화면 오류
            persuade.onClick.Invoke(); // 실패한 회유와 전투 요청
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Encounter)); // 오류 후 조우 복구
            Assert.That(persuade.interactable, Is.False); // 회유 재판정 차단
            Assert.That(rolls.Count, Is.Zero); // 실패한 판정 주사위 보존
            eventSystem.gameObject.SetActive(true); // 화면 입력 관리자 복구
            yield return null; // 기본 전투 선택 복구 대기
            GameObject.Find("EncounterFightButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 전투 생성 재시도
            yield return null; // 전투 화면 반영 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 실패 판정의 전투 재시도
        } // 검증 끝
        [UnityTest] // 조우 관찰 성공과 회차 기록 검증
        public IEnumerator EncounterObserve_Success_PersistsAcrossNewGameAndRestart() // 실제 관찰 선택과 정보 재확인
        { // 검증 시작
            var rolls = new Queue<int>(); // 결정 가능한 관찰 주사위
            rolls.Enqueue(10); // 플레이어 주사위
            rolls.Enqueue(10); // 상대 주사위
            var property = typeof(EncounterScreenView).GetProperty("ObserveRollOverride"); // 검증 주사위 연결 조회
            Assert.That(property, Is.Not.Null); // 관찰 주사위 제공 확인
            property.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => rolls.Dequeue())); // 동점 성공 주사위 지정
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            var observe = GameObject.Find("EncounterObserveButton"); // 실제 관찰 버튼 조회
            Assert.That(observe, Is.Not.Null); // 관찰 선택 제공 확인
            for (var index = 0; index < 2; index++) // 전투 선택에서 관찰 선택까지 이동
            { // 선택 이동 시작
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); // 오른쪽 선택 입력
                yield return null; // 방향 입력 처리 대기
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
                yield return null; // 선택 변경 반영 대기
            } // 선택 이동 끝
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(observe)); // 키보드 관찰 선택 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 실제 관찰 선택
            yield return null; // 관찰 입력 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 관찰 결과 표시 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Encounter)); // 성공한 관찰 후 조우 유지
            var description = GameObject.Find("EncounterDescription").GetComponent<UnityEngine.UI.Text>(); // 실제 관찰 정보 표시
            Assert.That(description.text, Does.Contain("체력 70")); // 1단계 시험 체력 공개
            Assert.That(description.text, Does.Contain("기본 공격")); // 시험 상대 공격 성향 공개
            Assert.That(observe.transform.Find("Label").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("정보 확인")); // 관찰 완료 후 정보 확인 안내
            var journalProperty = typeof(AppRoot).GetProperty("Observations"); // 현재 회차 관찰 기록 조회
            Assert.That(journalProperty, Is.Not.Null); // 회차 관찰 기록 제공 확인
            var journal = journalProperty.GetValue(AppRoot.Instance); // 실제 관찰 기록
            var contains = journal.GetType().GetMethod("Contains"); // 대상 기록 조회 메서드
            var arguments = new object[] // 시험 대상 식별자 인수
            { // 인수 목록 시작
                "trial-elite" // 현재 시험 대상 식별자
            }; // 인수 목록 끝
            Assert.That(contains.Invoke(journal, arguments), Is.True); // 성공한 관찰 기록 확인
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                var image = CaptureScreen("EncounterCanvas"); // 실제 관찰 결과 화면
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 플레이 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
            observe.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 관찰한 정보 다시 확인
            Assert.That(rolls.Count, Is.Zero); // 정보 확인 시 추가 주사위 없음
            Assert.That(description.text, Does.Contain("체력 70")); // 재확인한 시험 체력 표시
            AppRoot.Instance.ReturnToTitle(); // 새 게임을 위한 타이틀 복귀
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            AppRoot.Instance.StartNewGame(); // 새 게임 요청
            yield return WaitForPhase(GamePhase.Exploration); // 새 던전 도착 대기
            var fresh = journalProperty.GetValue(AppRoot.Instance); // 새 회차 관찰 기록
            Assert.That(fresh, Is.SameAs(journal)); // 새 게임에서도 관찰 기록 유지
            Assert.That(contains.Invoke(fresh, arguments), Is.True); // 새 게임의 시험 기록 확인
            Object.Destroy(AppRoot.Instance.gameObject); // 현재 실행의 공통 객체 종료
            yield return null; // 객체 종료 반영 대기
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 새 실행과 같은 시작 씬 진입
            yield return WaitForPhase(GamePhase.Title); // 새 실행의 타이틀 대기
            var reloaded = journalProperty.GetValue(AppRoot.Instance); // 저장 파일에서 다시 읽은 기록
            Assert.That(contains.Invoke(reloaded, arguments), Is.True); // 새 실행에서도 관찰 기록 유지
        } // 검증 끝
        [UnityTest] // 저장 실패 후 관찰 성공 판정 유지 검증
        public IEnumerator EncounterObserve_SaveFailure_RetriesWithoutReroll() // 같은 판정의 저장 재시도 확인
        { // 검증 시작
            var path = ObservationStorage.TestDirectoryOverride; // 현재 시험 전용 저장 경로
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)); // 시험 부모 폴더 준비
            System.IO.File.WriteAllText(path, "test blocker"); // 저장 폴더 생성을 막는 시험 파일
            var rolls = new Queue<int>(); // 결정 가능한 관찰 주사위
            rolls.Enqueue(10); // 플레이어 주사위
            rolls.Enqueue(10); // 상대 주사위
            EncounterScreenView.ObserveRollOverride = (minimum, maximum) => rolls.Dequeue(); // 동점 성공 주사위 지정
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            var observe = GameObject.Find("EncounterObserveButton").GetComponent<UnityEngine.UI.Button>(); // 실제 관찰 버튼
            observe.onClick.Invoke(); // 성공 판정과 저장 실패 모사
            var description = GameObject.Find("EncounterDescription").GetComponent<UnityEngine.UI.Text>(); // 실제 관찰 결과 표시
            Assert.That(description.text, Does.Contain("접근 권한")); // 실제 저장 실패 원인 확인
            Assert.That(AppRoot.Instance.Observations.Contains("trial-elite"), Is.False); // 저장 실패한 정보 공개 차단
            Assert.That(rolls.Count, Is.Zero); // 성공 판정의 주사위 사용 확인
            System.IO.File.Delete(path); // 시험에서 만든 차단 파일만 제거
            AppRoot.Instance.ReturnToTitle(); // 저장 대기 중 타이틀 복귀
            yield return WaitForPhase(GamePhase.Title); // 타이틀 전환 대기
            yield return EnterEliteEncounter(); // 새 조우 화면 진입
            observe = GameObject.Find("EncounterObserveButton").GetComponent<UnityEngine.UI.Button>(); // 새 조우의 관찰 버튼
            description = GameObject.Find("EncounterDescription").GetComponent<UnityEngine.UI.Text>(); // 새 조우의 결과 표시
            observe.onClick.Invoke(); // 성공 판정의 저장 재시도
            Assert.That(AppRoot.Instance.Observations.Contains("trial-elite"), Is.True); // 재시도한 관찰 기록 확인
            Assert.That(description.text, Does.Contain("체력 70")); // 저장 성공 후 정보 공개
            Assert.That(rolls.Count, Is.Zero); // 주사위 재사용 없이 저장 확인
        } // 검증 끝
        [UnityTest] // 저장 충돌과 재시작 안내 검증
        public IEnumerator EncounterObserve_StorageConflict_ExplainsRestartLoss() // 외부 변경의 원본 보호와 안내 확인
        { // 검증 시작
            var rolls = new Queue<int>(); // 결정 가능한 관찰 주사위
            rolls.Enqueue(10); // 플레이어 주사위
            rolls.Enqueue(10); // 상대 주사위
            EncounterScreenView.ObserveRollOverride = (minimum, maximum) => rolls.Dequeue(); // 동점 성공 주사위 지정
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            var path = ObservationStorage.TestDirectoryOverride; // 현재 시험 전용 저장 경로
            System.IO.Directory.CreateDirectory(path); // 시험 저장 폴더 준비
            var current = System.IO.Path.Combine(path, "observation-v1.json"); // 외부 변경을 모사할 시험 파일
            System.IO.File.WriteAllText(current, "external test data"); // 다른 실행의 변경 모사
            var observe = GameObject.Find("EncounterObserveButton").GetComponent<UnityEngine.UI.Button>(); // 실제 관찰 버튼
            observe.onClick.Invoke(); // 성공 판정과 저장 충돌 모사
            var description = GameObject.Find("EncounterDescription").GetComponent<UnityEngine.UI.Text>(); // 실제 관찰 결과 표시
            Assert.That(description.text, Does.Contain("변경")); // 저장 충돌 원인 안내
            Assert.That(description.text, Does.Contain("미저장 판정 초기화")); // 재시작 시 판정 손실 안내
            Assert.That(System.IO.File.ReadAllText(current), Is.EqualTo("external test data")); // 다른 실행의 파일 보존
        } // 검증 끝
        [UnityTest] // 관찰 실패 후 전투 화면 오류 복구 검증
        public IEnumerator EncounterObserve_FailedCombatCreation_DoesNotReroll() // 실패 판정 보존과 전투 재시도
        { // 검증 시작
            var rolls = new Queue<int>(); // 결정 가능한 관찰 주사위
            rolls.Enqueue(1); // 플레이어 최소 주사위
            rolls.Enqueue(20); // 상대 최대 주사위
            var property = typeof(EncounterScreenView).GetProperty("ObserveRollOverride"); // 검증 주사위 연결 조회
            Assert.That(property, Is.Not.Null); // 관찰 주사위 제공 확인
            property.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => rolls.Dequeue())); // 실패 주사위 지정
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            var observe = GameObject.Find("EncounterObserveButton").GetComponent<UnityEngine.UI.Button>(); // 실제 관찰 버튼
            var escape = GameObject.Find("EncounterEscapeButton").GetComponent<UnityEngine.UI.Button>(); // 실제 도망 버튼
            var fight = GameObject.Find("EncounterFightButton"); // 실제 전투 선택 버튼
            var eventSystem = UnityEngine.EventSystems.EventSystem.current; // 실제 화면 입력 관리자
            eventSystem.SetSelectedGameObject(observe.gameObject); // 관찰 버튼의 실제 선택 상태
            eventSystem.gameObject.SetActive(false); // 전투 화면 생성 실패 모사
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("^Combat screen failed:")); // 예상한 전투 화면 오류
            observe.onClick.Invoke(); // 실패한 관찰과 전투 생성 요청
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Encounter)); // 오류 후 조우 화면 복구
            Assert.That(observe.interactable, Is.False); // 관찰 재시도 비활성화
            Assert.That(escape.interactable, Is.False); // 실패 후 다른 조우 행동 차단
            Assert.That(rolls.Count, Is.Zero); // 실패한 판정의 주사위 소비 확인
            observe.onClick.Invoke(); // 잘못된 중복 관찰 요청
            Assert.That(rolls.Count, Is.Zero); // 추가 주사위 소비 방지
            eventSystem.gameObject.SetActive(true); // 화면 입력 관리자 복구
            yield return null; // 입력 관리자 반영 대기
            yield return null; // 전투 버튼 선택 복구 대기
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(fight)); // 전투 재시도 선택 복구
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 실제 전투 재시도 입력
            yield return null; // 전투 선택 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 전투 화면 반영 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 실패 판정의 전투 재시도 성공
        } // 검증 끝
        [UnityTest] // 게임패드 관찰 실패의 전투 진입 검증
        public IEnumerator EncounterObserve_GamepadFailure_StartsCombat() // 실제 관찰 실패와 미공개 정보 확인
        { // 검증 시작
            var rolls = new Queue<int>(); // 결정 가능한 관찰 주사위
            rolls.Enqueue(1); // 플레이어 최소 주사위
            rolls.Enqueue(20); // 상대 최대 주사위
            var property = typeof(EncounterScreenView).GetProperty("ObserveRollOverride"); // 검증 주사위 연결 조회
            Assert.That(property, Is.Not.Null); // 관찰 주사위 제공 확인
            property.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => rolls.Dequeue())); // 실패 주사위 지정
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            gamepad = InputSystem.AddDevice<Gamepad>(); // 시험 게임패드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 입력 준비 대기
            var observe = GameObject.Find("EncounterObserveButton"); // 실제 관찰 버튼 조회
            Assert.That(observe, Is.Not.Null); // 관찰 선택 제공 확인
            for (var index = 0; index < 2; index++) // 전투 선택에서 관찰 선택까지 이동
            { // 선택 이동 시작
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadRight)); // 오른쪽 선택 입력
                yield return null; // 방향 입력 처리 대기
                InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 방향 버튼 해제
                yield return null; // 선택 변경 반영 대기
            } // 선택 이동 끝
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(observe)); // 게임패드 관찰 선택 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 실제 관찰 선택
            yield return null; // 관찰 실패와 전투 처리 대기
            yield return null; // 선택 버튼 유지 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 관찰 실패 후 일반 전투 진입
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(100)); // 누른 선택 버튼의 중복 공격 방지
            var journal = typeof(AppRoot).GetProperty("Observations").GetValue(AppRoot.Instance); // 현재 회차 관찰 기록
            var arguments = new object[] // 시험 대상 식별자 인수
            { // 인수 목록 시작
                "trial-elite" // 현재 시험 대상 식별자
            }; // 인수 목록 끝
            Assert.That(journal.GetType().GetMethod("Contains").Invoke(journal, arguments), Is.False); // 실패한 관찰 정보 미공개
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 전투 입력 준비 대기
        } // 검증 끝
        [UnityTest] // 성공한 조우 도망의 이동 실패 복구 검증
        public IEnumerator EncounterEscape_Success_RetriesBlockedArrivalAndKeepsMonster() // 이전 방 복구와 재조우 확인
        { // 검증 시작
            var rolls = new Queue<int>(); // 결정 가능한 도망 주사위
            rolls.Enqueue(10); // 플레이어 주사위
            rolls.Enqueue(10); // 상대 주사위
            var property = typeof(EncounterScreenView).GetProperty("EscapeRollOverride"); // 검증 주사위 연결 조회
            Assert.That(property, Is.Not.Null); // 조우 도망 주사위 제공 확인
            property.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => rolls.Dequeue())); // 성공 주사위 지정
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var target = travel.Layout.GetRoom(travel.RoomX, travel.RoomY); // 현재 상위 개체 방
            var route = FindTestRoute(travel.Layout, travel.Layout.GetRoom(0, 0), target); // 시작 방부터 실제 경로
            var entry = route[route.Count - 1]; // 조우 방에 들어온 문
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 방 조회
            var arguments = new object[] // 이전 방 좌표 인수
            { // 인수 목록 시작
                entry.RoomX, entry.RoomY // 이전 방 좌표
            }; // 인수 목록 끝
            var previous = (GameObject)finder.Invoke(travel, arguments); // 실제 이전 방
            var targetArguments = new object[] // 조우 방 좌표 인수
            { // 인수 목록 시작
                target.X, target.Y // 현재 조우 방 좌표
            }; // 인수 목록 끝
            var targetRoot = (GameObject)finder.Invoke(travel, targetArguments); // 실제 조우 방
            var floor = targetRoot.transform.Find("Floor").GetComponent<Tilemap>(); // 시험 장애물용 바닥
            var blocker = previous.AddComponent<DoorArrivalBlocker>(); // 이전 방 활성화 시 차단 장치
            blocker.Cell = new Vector3Int(entry.ApproachX, entry.ApproachY, 0); // 이전 방의 도착 칸
            blocker.Tile = floor.GetTile(Vector3Int.zero); // 차단용 시험 타일
            var escape = GameObject.Find("EncounterEscapeButton"); // 조우 도망 버튼
            Assert.That(escape, Is.Not.Null); // 도망 선택 제공 확인
            Assert.That(escape.GetComponent<UnityEngine.UI.Button>().interactable, Is.True); // 이전 방 도망 선택 가능 확인
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                yield return null; // 초상화 표시 갱신 대기
                var image = CaptureScreen("EncounterCanvas"); // 실제 도망 선택 화면
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 플레이 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); // 도망 버튼 방향 입력
            yield return null; // 방향 입력 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
            yield return null; // 선택 변경 반영 대기
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(escape)); // 키보드 도망 선택 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 실제 도망 선택 입력
            yield return null; // 도망 입력 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 실패한 이동 결과 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Encounter)); // 이동 실패 시 조우 유지
            Assert.That(travel.RoomX, Is.EqualTo(target.X)); // 이동 실패 시 조우 방 가로 유지
            Assert.That(travel.RoomY, Is.EqualTo(target.Y)); // 이동 실패 시 조우 방 세로 유지
            Assert.That(rolls.Count, Is.Zero); // 성공한 주사위 한 번만 사용
            var fight = GameObject.Find("EncounterFightButton").GetComponent<UnityEngine.UI.Button>(); // 전투 선택 버튼
            var observe = GameObject.Find("EncounterObserveButton").GetComponent<UnityEngine.UI.Button>(); // 관찰 선택 버튼
            Assert.That(fight.interactable || observe.interactable, Is.False); // 성공한 도망의 다른 선택 차단
            fight.onClick.Invoke(); // 잘못된 전투 선택 요청
            observe.onClick.Invoke(); // 잘못된 관찰 선택 요청
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Encounter)); // 이동 전 조우 화면 보존
            Assert.That(GameObject.Find("EncounterDescription").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("이동 실패")); // 이동 실패 안내
            Object.Destroy(blocker); // 시험 차단 장치 제거
            previous.transform.Find("Obstacles").GetComponent<Tilemap>().SetTile(blocker.Cell, null); // 시험 장애물 제거
            yield return null; // 장치 제거 완료 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 성공한 도망의 이동 재시도
            yield return null; // 재시도 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 탐험 복귀 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 성공한 도망의 탐험 복귀
            Assert.That(travel.RoomX, Is.EqualTo(entry.RoomX)); // 이전 방 가로 좌표
            Assert.That(travel.RoomY, Is.EqualTo(entry.RoomY)); // 이전 방 세로 좌표
            Assert.That(player.X, Is.EqualTo(entry.ApproachX)); // 이전 방의 안전한 가로 도착 칸
            Assert.That(player.Y, Is.EqualTo(entry.ApproachY)); // 이전 방의 안전한 세로 도착 칸
            Assert.That(travel.Discovery.CurrentX, Is.EqualTo(entry.RoomX)); // 지도 현재 방 가로 갱신
            Assert.That(travel.Discovery.CurrentY, Is.EqualTo(entry.RoomY)); // 지도 현재 방 세로 갱신
            Assert.That(targetRoot.transform.Find("EncounterMarker"), Is.Not.Null); // 도망 후 대상 보존
            yield return UseDoor(travel, player, entry); // 보존된 조우 방 재진입
            var targetObstacles = targetRoot.transform.Find("Obstacles").GetComponent<Tilemap>(); // 조우 방 장애물
            Assert.That(player.ChangeRoom(floor, targetObstacles, 0, 2), Is.True); // 조우 범위 밖 재배치
            yield return null; // 위치 변경 반영 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S)); // 대상 방향 재접근
            InputSystem.Update(); // 이동 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이동 키 해제
            InputSystem.Update(); // 해제 반영
            var deadline = Time.realtimeSinceStartup + 10f; // 재조우 대기 제한
            while (player.IsMoving && Time.realtimeSinceStartup < deadline) // 재접근 이동 완료 대기
            { // 대기 시작
                yield return null; // 다음 프레임 대기
            } // 대기 끝
            yield return null; // 조우 화면 갱신 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Encounter)); // 보존된 대상 재조우 확인
        } // 검증 끝
        [UnityTest] // 조우 도망 실패와 전투 진입 검증
        public IEnumerator EncounterEscape_GamepadFailure_EntersCombat() // 게임패드 도망 실패의 전투 전환
        { // 검증 시작
            var rolls = new Queue<int>(); // 결정 가능한 도망 주사위
            rolls.Enqueue(1); // 플레이어 최소 주사위
            rolls.Enqueue(20); // 상대 최대 주사위
            var property = typeof(EncounterScreenView).GetProperty("EscapeRollOverride"); // 검증 주사위 연결 조회
            Assert.That(property, Is.Not.Null); // 조우 도망 주사위 제공 확인
            property.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => rolls.Dequeue())); // 실패 주사위 지정
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            gamepad = InputSystem.AddDevice<Gamepad>(); // 시험 게임패드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 게임패드 준비 대기
            var escape = GameObject.Find("EncounterEscapeButton"); // 실제 조우 도망 버튼
            Assert.That(escape, Is.Not.Null); // 도망 버튼 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadRight)); // 도망 버튼 방향 입력
            yield return null; // 방향 입력 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 방향 버튼 해제
            yield return null; // 선택 변경 반영 대기
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(escape)); // 게임패드 도망 선택 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 실제 도망 선택
            yield return null; // 도망 실패 처리 대기
            yield return null; // 선택 버튼 유지 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 실패 후 일반 전투 진입
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(100)); // 누른 버튼의 중복 공격 방지
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 전투 입력 준비 대기
        } // 검증 끝
        [UnityTest] // 도망 실패 후 전투 화면 오류 복구 검증
        public IEnumerator EncounterEscape_FailedCombatCreation_DoesNotReroll() // 실패 판정 보존과 전투 재시도
        { // 검증 시작
            var rolls = new Queue<int>(); // 결정 가능한 도망 주사위
            rolls.Enqueue(1); // 플레이어 최소 주사위
            rolls.Enqueue(20); // 상대 최대 주사위
            var property = typeof(EncounterScreenView).GetProperty("EscapeRollOverride"); // 검증 주사위 연결 조회
            Assert.That(property, Is.Not.Null); // 도망 주사위 제공 확인
            property.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => rolls.Dequeue())); // 실패 주사위 지정
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            var escape = GameObject.Find("EncounterEscapeButton").GetComponent<UnityEngine.UI.Button>(); // 실제 도망 버튼
            var fight = GameObject.Find("EncounterFightButton"); // 실제 전투 선택 버튼
            var eventSystem = UnityEngine.EventSystems.EventSystem.current; // 실제 화면 입력 관리자
            eventSystem.SetSelectedGameObject(escape.gameObject); // 도망 버튼을 실제 선택 상태로 지정
            eventSystem.gameObject.SetActive(false); // 전투 화면 생성 실패 모사
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("^Combat screen failed:")); // 예상한 전투 화면 오류
            escape.onClick.Invoke(); // 실패한 도망과 전투 생성 요청
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Encounter)); // 오류 후 조우 화면 복구
            Assert.That(escape.interactable, Is.False); // 도망 재시도 비활성화
            Assert.That(rolls.Count, Is.Zero); // 실패한 판정의 주사위 소비 확인
            escape.onClick.Invoke(); // 잘못된 중복 도망 요청
            Assert.That(rolls.Count, Is.Zero); // 추가 주사위 소비 방지
            eventSystem.gameObject.SetActive(true); // 화면 입력 관리자 복구
            yield return null; // 복구한 입력 관리자 반영 대기
            yield return null; // 전투 버튼 기본 선택 복구 대기
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(fight)); // 전투 재시도 선택 복구
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 실제 전투 재시도 입력
            yield return null; // 전투 선택 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 전투 화면 반영 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 실패 판정의 전투 재시도 성공
        } // 검증 끝
        [UnityTest] // 이전 방이 없는 조우 도망 차단 검증
        public IEnumerator EncounterEscape_WithoutPreviousRoom_IsDisabled() // 이전 방이 없는 시험 조우 확인
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            yield return EnterDungeon(); // 실제 던전 진입
            Assert.That(AppRoot.Instance.StartEncounter(), Is.True); // 시작 방에서 시험 조우 진입
            yield return null; // 조우 화면 준비 대기
            var escape = GameObject.Find("EncounterEscapeButton"); // 조우 도망 버튼
            Assert.That(escape, Is.Not.Null); // 도망 버튼 존재 확인
            Assert.That(escape.GetComponent<UnityEngine.UI.Button>().interactable, Is.False); // 이전 방이 없을 때 도망 차단
            Assert.That(GameObject.Find("EncounterDescription").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("이전 방")); // 도망 불가 이유 표시
        } // 검증 끝
        private IEnumerator EnterEliteEncounter() // 실제 입력으로 시험 상위 개체 조우
        { // 조우 준비 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            yield return EnterDungeon(); // 실제 던전 진입
            keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 입력 준비 대기
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var layout = travel.Layout; // 현재 층 구조
            var target = layout.Rooms.First(room => room.Kind == RoomKind.Elite); // 상위 개체 방
            foreach (var door in FindTestRoute(layout, layout.GetRoom(travel.RoomX, travel.RoomY), target)) // 실제 연결 경로
            { // 경로 이동 시작
                yield return UseDoor(travel, player, door); // 실제 입력으로 방 통과
            } // 경로 이동 끝
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 방 조회
            var arguments = new object[] // 현재 방 좌표 인수
            { // 인수 목록 시작
                target.X, target.Y // 상위 개체 방 좌표
            }; // 인수 목록 끝
            var root = (GameObject)finder.Invoke(travel, arguments); // 실제 조우 방
            var floor = root.transform.Find("Floor").GetComponent<Tilemap>(); // 조우 방 바닥
            var obstacles = root.transform.Find("Obstacles").GetComponent<Tilemap>(); // 조우 방 장애물
            Assert.That(player.ChangeRoom(floor, obstacles, 0, 2), Is.True); // 조우 범위 밖 배치
            yield return null; // 위치 변경 반영 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S)); // 대상 방향 이동 입력
            InputSystem.Update(); // 이동 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이동 키 해제
            InputSystem.Update(); // 해제 반영
            var deadline = Time.realtimeSinceStartup + 10f; // 이동 대기 제한
            while (player.IsMoving && Time.realtimeSinceStartup < deadline) // 조우 범위 도착 대기
            { // 대기 시작
                yield return null; // 다음 프레임 대기
            } // 대기 끝
            yield return null; // 조우 화면 생성 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Encounter)); // 실제 조우 진입 확인
        } // 조우 준비 끝
        [UnityTest] // 조우 화면 생성 실패 복구 검증
        public IEnumerator Encounter_ScreenFailure_RestoresExploration() // 입력 관리자 누락과 탐험 복구
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            yield return EnterDungeon(); // 실제 던전 진입
            var eventSystem = UnityEngine.EventSystems.EventSystem.current; // 실제 화면 입력 관리자
            Assert.That(eventSystem, Is.Not.Null); // 입력 관리자 존재 확인
            eventSystem.gameObject.SetActive(false); // 조우 화면의 입력 관리자 누락 모사
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("^Encounter screen failed:")); // 예상한 조우 화면 오류
            Assert.That(AppRoot.Instance.StartEncounter(), Is.False); // 입력 없는 조우 화면 거절
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 조우 실패 시 탐험 복구
            Assert.That(AppRoot.Instance.Input.IsExplorationEnabled, Is.True); // 탐험 입력 복구 확인
            eventSystem.gameObject.SetActive(true); // 화면 입력 관리자 복구
            yield return null; // 입력 복구 대기
            Assert.That(AppRoot.Instance.StartEncounter(), Is.True); // 입력 복구 후 조우 재시도
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Encounter)); // 재시도한 조우 진입 확인
        } // 검증 끝
        [UnityTest] // 전투 화면 생성 실패 복구 검증
        public IEnumerator Encounter_CombatScreenFailure_RestoresChoice() // 실제 화면 오류와 조우 복구
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            yield return EnterDungeon(); // 실제 던전 진입
            Assert.That(AppRoot.Instance.StartEncounter(), Is.True); // 실제 조우 화면 진입
            yield return null; // 조우 화면 준비 대기
            var eventSystem = UnityEngine.EventSystems.EventSystem.current; // 실제 화면 입력 관리자
            Assert.That(eventSystem, Is.Not.Null); // 입력 관리자 존재 확인
            eventSystem.gameObject.SetActive(false); // 전투 화면 생성 실패 상황 모사
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("^Combat screen failed:")); // 예상한 화면 생성 오류
            Assert.That(AppRoot.Instance.StartCombat(), Is.False); // 실패한 전투 화면 진입 거절
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Encounter)); // 기존 조우 상태 복구
            Assert.That(AppRoot.Instance.Input.IsExplorationEnabled, Is.False); // 조우 중 탐험 입력 유지
            Assert.That(GameObject.Find("EncounterCanvas"), Is.Not.Null); // 기존 조우 화면 보존
            Assert.That(GameObject.Find("BattleCanvas"), Is.Null); // 실패한 전투 화면 즉시 숨김
            eventSystem.gameObject.SetActive(true); // 화면 입력 관리자 복구
            yield return null; // 실패한 전투 화면 정리 대기
            Assert.That(AppRoot.Instance.StartCombat(), Is.True); // 조우 화면에서 전투 재시도
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 재시도한 전투 진입 확인
        } // 검증 끝
        [UnityTest] // 게임패드 방어 선택 검증
        public IEnumerator Combat_Defend_GamepadSelectionReducesDamage() // 실제 게임패드 선택과 피해 확인
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            CombatScreenView.RandomOverride = (minimum, maximum) => minimum; // 명중과 최소 피해 편차 고정
            yield return EnterDungeon(); // 실제 던전 진입
            gamepad = InputSystem.AddDevice<Gamepad>(); // 시험 게임패드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 입력 준비 대기
            Assert.That(AppRoot.Instance.StartCombat(), Is.True); // 실제 시험 전투 진입
            yield return null; // 전투 화면 준비 대기
            var defend = GameObject.Find("DefendBattleButton"); // 실제 방어 버튼 조회
            Assert.That(defend, Is.Not.Null); // 방어 버튼 존재 확인
            var layout = GameObject.Find("BattleCanvas").GetComponent<CombatActionLayout>(); // 실제 행동 버튼 배치
            Assert.That(layout, Is.Not.Null); // 화면 너비별 배치 제공 확인
            layout.Refresh(900f); // 좁은 창의 가상 화면 너비 적용
            var attackRect = GameObject.Find("AdvanceBattleButton").GetComponent<RectTransform>(); // 공격 버튼 위치
            var defendRect = defend.GetComponent<RectTransform>(); // 방어 버튼 위치
            Assert.That(-attackRect.anchoredPosition.x + attackRect.sizeDelta.x * 0.5f, Is.LessThanOrEqualTo(418.01f)); // 왼쪽 버튼의 화면 안쪽 여백
            Assert.That(defendRect.anchoredPosition.x + defendRect.sizeDelta.x * 0.5f, Is.LessThanOrEqualTo(418.01f)); // 오른쪽 버튼의 화면 안쪽 여백
            layout.Refresh(1920f); // 기준 화면의 버튼 복구
            for (var index = 0; index < 2; index++) // 공격 버튼에서 방어 버튼까지 이동
            { // 선택 이동 시작
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadRight)); // 오른쪽 선택 입력
                yield return null; // 방향 입력 처리 대기
                InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 방향 버튼 해제
                yield return null; // 선택 변경 반영 대기
            } // 선택 이동 끝
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(defend)); // 게임패드 방어 선택 확인
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 실제 방어 선택
            yield return null; // 방어 입력 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 상대 행동과 다음 차례 대기
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(97)); // 게임패드 방어 피해 감소
            Assert.That(GameObject.Find("Notice").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("방어")); // 방어 결과 표시
        } // 검증 끝
        [UnityTest] // 실제 방어 선택과 턴 처리 검증
        public IEnumerator Combat_Defend_ReducesDamageUntilNextTurn() // 키보드 방어와 다음 공격 확인
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            CombatScreenView.RandomOverride = (minimum, maximum) => minimum; // 명중과 최소 피해 편차 고정
            yield return EnterDungeon(); // 실제 던전 진입
            keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 입력 준비 대기
            Assert.That(AppRoot.Instance.StartCombat(), Is.True); // 실제 시험 전투 진입
            yield return null; // 전투 화면 준비 대기
            var defend = GameObject.Find("DefendBattleButton"); // 실제 방어 버튼 조회
            Assert.That(defend, Is.Not.Null); // 방어 버튼 존재 확인
            for (var index = 0; index < 2; index++) // 공격 버튼에서 방어 버튼까지 이동
            { // 선택 이동 시작
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); // 오른쪽 선택 입력
                yield return null; // 방향 입력 처리 대기
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
                yield return null; // 선택 변경 반영 대기
            } // 선택 이동 끝
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(defend)); // 키보드 방어 버튼 선택 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 실제 방어 선택
            yield return null; // 방어 입력 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 상대 행동과 다음 차례 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 방어 후 전투 유지
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(97)); // 6 피해의 방어 감소 결과
            var notice = GameObject.Find("Notice").GetComponent<UnityEngine.UI.Text>(); // 실제 행동 결과 표시
            Assert.That(notice.text, Does.Contain("방어")); // 방어 행동 결과 안내
            Assert.That(notice.text, Does.Contain("3 피해")); // 감소된 상대 피해 표시
            Assert.That(GameObject.Find("RoundStatus").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("2라운드")); // 방어 행동 후 다음 라운드
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                var image = CaptureScreen("BattleCanvas"); // 실제 방어 결과 전투 화면
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
            var view = GameObject.Find("BattleScreen").GetComponent<CombatScreenView>(); // 실제 전투 화면 제어
            var playerField = typeof(CombatScreenView).GetField("player", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 플레이어 상태 조회
            var fighter = (CombatFighter)playerField.GetValue(view); // 현재 플레이어 전투 상태
            var defending = typeof(CombatFighter).GetProperty("IsDefending"); // 방어 상태 조회
            Assert.That((bool)defending.GetValue(fighter), Is.False); // 다음 플레이어 차례의 방어 해제
            GameObject.Find("AdvanceBattleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 다음 차례 기본 공격
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(91)); // 방어 해제 후 적의 원래 피해
        } // 검증 끝
        [UnityTest] // 강제 개방 실패와 열쇠 사용 검증
        public IEnumerator ForceDoor_KeyboardFailure_AllowsKeyAndBlocksRetry() // 실제 키보드 실패와 게임패드 재시도
        { // 검증 시작
            var seed = Enumerable.Range(0, 100).First(value => new DoorLockState(DungeonGenerator.Generate(value)).TrialDoor != null); // 시험 잠긴 문 시드
            RoomTravelController.GenerationSeedOverride = seed; // 재현 가능한 던전
            var rolls = new Queue<int>(); // 결정 가능한 이벤트 주사위
            rolls.Enqueue(1); // 플레이어 최소 주사위
            rolls.Enqueue(20); // 이벤트 최대 주사위
            var property = typeof(RoomTravelController).GetProperty("ForceRollOverride"); // 검증 주사위 연결 조회
            Assert.That(property, Is.Not.Null); // 강제 개방 주사위 제공 확인
            property.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => rolls.Dequeue())); // 실패 주사위 지정
            yield return EnterDungeon(); // 실제 던전 진입
            keyboard = InputSystem.AddDevice<Keyboard>(); // 시험 키보드 연결
            gamepad = InputSystem.AddDevice<Gamepad>(); // 시험 게임패드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 입력 준비 대기
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var door = travel.TrialLockedDoor; // 현재 층의 시험 잠긴 문
            yield return ApproachLockedDoor(travel, player, door); // 실제 문 앞 접근
            AppRoot.Instance.OpenInventory(); // 탐험 입력이 잠긴 인벤토리 열기
            yield return null; // 인벤토리 화면 준비 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); // 인벤토리 중 강제 개방 입력
            InputSystem.Update(); // 잠긴 탐험 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 강제 개방 키 해제
            InputSystem.Update(); // 해제 반영
            Assert.That(rolls.Count, Is.EqualTo(2)); // 잠긴 입력의 주사위 소비 방지
            Assert.That(travel.IsDoorLocked(door), Is.True); // 잠긴 화면의 문 상태 보존
            AppRoot.Instance.CloseInventory(); // 탐험 화면 복귀
            yield return null; // 탐험 입력 복구 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); // 키보드 강제 개방 입력
            InputSystem.Update(); // 강제 개방 입력 처리
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키보드 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 결과 표시 대기
            Assert.That(travel.IsDoorLocked(door), Is.True); // 실패 후 잠금 유지
            var notice = GameObject.Find("DoorNotice").GetComponent<UnityEngine.UI.Text>(); // 실제 문 결과 안내
            Assert.That(notice.text, Does.Contain("실패")); // 강제 개방 실패 안내
            Assert.That(notice.text, Does.Contain("50")); // 시험 공격력과 난이도 안내
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                var image = CaptureScreen("DungeonCanvas"); // 실제 강제 개방 결과 화면
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 실패 후 열쇠 없는 문 입력
            InputSystem.Update(); // 열쇠 부족 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 문 입력 키 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 문 안내 갱신 대기
            Assert.That(notice.text, Does.Contain("이미 시도")); // 실패 후 재시도 불가 안내
            Assert.That(notice.text, Does.Not.Contain("F / 패드 X")); // 사용할 수 없는 강제 개방 안내 방지
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West)); // 게임패드 재시도 입력
            InputSystem.Update(); // 게임패드 입력 처리
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 게임패드 버튼 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 재시도 안내 대기
            Assert.That(notice.text, Does.Contain("이미")); // 같은 문의 재시도 차단 안내
            Assert.That(rolls.Count, Is.Zero); // 재시도 시 추가 주사위 없음
            Assert.That(AppRoot.Instance.Keys.Add(1), Is.True); // 실패 이후 시험 열쇠 획득
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 열쇠로 실제 문 이동
            InputSystem.Update(); // 문 이동 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 상호작용 키 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 실제 방 이동 대기
            Assert.That(travel.RoomX, Is.EqualTo(door.TargetRoomX)); // 실패 후 열쇠 이동 가로 좌표
            Assert.That(travel.RoomY, Is.EqualTo(door.TargetRoomY)); // 실패 후 열쇠 이동 세로 좌표
            Assert.That(AppRoot.Instance.Keys.Count, Is.Zero); // 기존 열쇠 한 개 소모
        } // 검증 끝
        [UnityTest] // 게임패드 강제 개방 성공 검증
        public IEnumerator ForceDoor_GamepadSuccess_UnlocksWithoutKey() // 실제 게임패드 성공과 양방향 개방
        { // 검증 시작
            var seed = Enumerable.Range(0, 100).First(value => new DoorLockState(DungeonGenerator.Generate(value)).TrialDoor != null); // 시험 잠긴 문 시드
            RoomTravelController.GenerationSeedOverride = seed; // 재현 가능한 던전
            var property = typeof(RoomTravelController).GetProperty("ForceRollOverride"); // 검증 주사위 연결 조회
            Assert.That(property, Is.Not.Null); // 강제 개방 주사위 제공 확인
            property.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => 10)); // 동점 성공 주사위 지정
            yield return EnterDungeon(); // 실제 던전 진입
            keyboard = InputSystem.AddDevice<Keyboard>(); // 접근용 시험 키보드
            gamepad = InputSystem.AddDevice<Gamepad>(); // 강제 개방 시험 게임패드
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 입력 준비 대기
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var door = travel.TrialLockedDoor; // 현재 층의 시험 잠긴 문
            yield return ApproachLockedDoor(travel, player, door); // 실제 문 앞 접근
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West)); // 게임패드 강제 개방 입력
            InputSystem.Update(); // 강제 개방 반영
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 게임패드 버튼 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 결과 표시 대기
            var reverse = travel.Layout.GetConnection(door.TargetRoomX, door.TargetRoomY, door.RoomX, door.RoomY); // 역방향 문
            Assert.That(travel.IsDoorLocked(door) || travel.IsDoorLocked(reverse), Is.False); // 양방향 잠금 해제
            Assert.That(GameObject.Find("DoorNotice").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("성공")); // 성공 결과 안내
            Assert.That(AppRoot.Instance.Keys.Count, Is.Zero); // 열쇠 없는 성공 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 열린 문을 통한 실제 이동
            InputSystem.Update(); // 이동 입력 처리
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 키보드 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 방 이동 결과 대기
            Assert.That(travel.RoomX, Is.EqualTo(door.TargetRoomX)); // 개방 후 도착 가로 좌표
            Assert.That(travel.RoomY, Is.EqualTo(door.TargetRoomY)); // 개방 후 도착 세로 좌표
        } // 검증 끝
        private IEnumerator ApproachLockedDoor(RoomTravelController travel, GridMoveController player, RoomDoor door) // 실제 잠긴 문의 접근 위치 이동
        { // 접근 시작
            var layout = travel.Layout; // 현재 층 구조
            var start = layout.GetRoom(travel.RoomX, travel.RoomY); // 시작 방
            var source = layout.GetRoom(door.RoomX, door.RoomY); // 잠긴 문의 출발 방
            foreach (var routeDoor in FindTestRoute(layout, start, source)) // 실제 연결 경로 순회
            { // 경로 이동 시작
                yield return UseDoor(travel, player, routeDoor); // 실제 입력으로 방 통과
            } // 경로 이동 끝
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 현재 방 객체 조회
            var arguments = new object[] // 출발 방 좌표 인수
            { // 인수 목록 시작
                door.RoomX, door.RoomY // 잠긴 문의 출발 좌표
            }; // 인수 목록 끝
            var root = (GameObject)finder.Invoke(travel, arguments); // 실제 출발 방
            var floor = root.transform.Find("Floor").GetComponent<Tilemap>(); // 출발 방 바닥
            var obstacles = root.transform.Find("Obstacles").GetComponent<Tilemap>(); // 출발 방 장애물
            var before = new Vector3Int(door.ApproachX, door.ApproachY, 0) - Offset(door.Direction); // 문 앞 한 칸 전 위치
            Assert.That(player.ChangeRoom(floor, obstacles, before.x, before.y), Is.True); // 접근 시작 위치 배치
            yield return null; // 위치 변경 프레임 종료
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(DirectionKey(door.Direction))); // 문 방향 이동 입력
            InputSystem.Update(); // 방향 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이동 키 해제
            InputSystem.Update(); // 해제 반영
            var deadline = Time.realtimeSinceStartup + 10f; // 이동 대기 제한
            while (player.IsMoving && Time.realtimeSinceStartup < deadline) // 문 앞 이동 완료 대기
            { // 대기 시작
                yield return null; // 다음 프레임 대기
            } // 대기 끝
            Assert.That(player.IsMoving, Is.False); // 문 앞 도착 확인
        } // 접근 끝
        [UnityTest] // 전투 초상화와 입력 검증
        public IEnumerator Combat_Portraits_AreLoadedWithoutBlockingInput() // 실제 화면의 이미지와 버튼 확인
        { // 검증 시작
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 던전
            yield return EnterDungeon(); // 실제 던전 진입
            Assert.That(AppRoot.Instance.StartCombat(), Is.True); // 전투 화면 진입
            yield return null; // 실제 화면 준비 대기
            var playerPortrait = GameObject.Find("PlayerPortrait"); // 주인공 초상화 조회
            var enemyPortrait = GameObject.Find("EnemyPortrait"); // 상위 개체 초상화 조회
            Assert.That(playerPortrait, Is.Not.Null); // 주인공 초상화 존재 확인
            Assert.That(enemyPortrait, Is.Not.Null); // 상위 개체 초상화 존재 확인
            var playerImage = playerPortrait.GetComponent<UnityEngine.UI.RawImage>(); // 주인공 실제 이미지
            var enemyImage = enemyPortrait.GetComponent<UnityEngine.UI.RawImage>(); // 상대 실제 이미지
            Assert.That(playerImage.texture, Is.Not.Null); // 주인공 리소스 연결 확인
            Assert.That(enemyImage.texture, Is.Not.Null); // 상대 리소스 연결 확인
            Assert.That(playerImage.raycastTarget, Is.False); // 초상화의 입력 가로채기 차단
            Assert.That(enemyImage.raycastTarget, Is.False); // 상대 이미지의 입력 가로채기 차단
            var playerLayout = playerPortrait.GetComponent<CombatPortraitLayout>(); // 주인공의 화면 배치
            var enemyLayout = enemyPortrait.GetComponent<CombatPortraitLayout>(); // 상대의 화면 배치
            Assert.That(playerLayout, Is.Not.Null); // 주인공 크기 조절 제공 확인
            Assert.That(enemyLayout, Is.Not.Null); // 상대 크기 조절 제공 확인
            playerLayout.Refresh(1440f); // 좁은 정사각형 창의 가상 화면 적용
            enemyLayout.Refresh(1440f); // 상대 초상화의 좁은 화면 적용
            var playerRect = playerPortrait.GetComponent<RectTransform>(); // 주인공 표시 위치
            var enemyRect = enemyPortrait.GetComponent<RectTransform>(); // 상대 표시 위치
            Assert.That(-playerRect.anchoredPosition.x + playerRect.sizeDelta.x * 0.5f, Is.LessThanOrEqualTo(704f)); // 왼쪽 화면 안쪽 여백 확인
            Assert.That(enemyRect.anchoredPosition.x + enemyRect.sizeDelta.x * 0.5f, Is.LessThanOrEqualTo(704f)); // 오른쪽 화면 안쪽 여백 확인
            Assert.That(-playerRect.anchoredPosition.x - playerRect.sizeDelta.x * 0.5f, Is.GreaterThanOrEqualTo(420f)); // 주인공 글자 영역 분리
            Assert.That(enemyRect.anchoredPosition.x - enemyRect.sizeDelta.x * 0.5f, Is.GreaterThanOrEqualTo(420f)); // 상대 글자 영역 분리
            playerLayout.Refresh(800f); // 초상화 공간이 없는 좁은 화면 적용
            Assert.That(playerImage.enabled, Is.False); // 공간 부족 시 초상화 숨김
            playerLayout.Refresh(1920f); // 일반 화면의 초상화 복구
            Assert.That(playerImage.enabled, Is.True); // 화면 복귀 시 초상화 표시
            var addPortrait = typeof(CombatScreenView).GetMethod("AddPortrait", BindingFlags.Static | BindingFlags.NonPublic); // 선택적 초상화 생성 조회
            Assert.That(addPortrait, Is.Not.Null); // 초상화 연결 제공 확인
            var missingArguments = new object[] // 없는 이미지의 생성 인수
            { // 인수 목록 시작
                GameObject.Find("BattleCanvas").transform, "MissingPortrait", "BattlePortraits/Missing", Vector2.zero // 존재하지 않는 초상화 정보
            }; // 인수 목록 끝
            Assert.DoesNotThrow(() => addPortrait.Invoke(null, missingArguments)); // 이미지 누락 시 안전한 처리
            Assert.That(GameObject.Find("MissingPortrait"), Is.Null); // 없는 이미지 객체 생성 방지
            Assert.That(GameObject.Find("AdvanceBattleButton").GetComponent<UnityEngine.UI.Button>().interactable, Is.True); // 공격 버튼 사용 가능 확인
            Assert.That(GameObject.Find("FleeBattleButton").GetComponent<UnityEngine.UI.Button>().interactable, Is.True); // 도주 버튼 사용 가능 확인
        } // 검증 끝
        [UnityTest] // 전투 후퇴 실패의 행동 소모 검증
        public IEnumerator Combat_FleeFailure_ConsumesTurnAndShowsResult() // 속도 주사위 실패와 적 행동 확인
        { // 검증 시작
            var rolls = new Queue<int>(); // 결정 가능한 후퇴 주사위
            rolls.Enqueue(1); // 플레이어 후퇴 주사위
            rolls.Enqueue(20); // 상대 추격 주사위
            var property = typeof(CombatScreenView).GetProperty("RetreatRollOverride"); // 후퇴 주사위 연결 조회
            Assert.That(property, Is.Not.Null); // 후퇴 주사위 제공 확인
            property.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => rolls.Dequeue())); // 실패 주사위 지정
            CombatScreenView.RandomOverride = (minimum, maximum) => minimum; // 재현 가능한 상대 공격 난수
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            GameObject.Find("EncounterFightButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 일반 전투 선택
            yield return null; // 전투 화면 준비 대기
            var flee = GameObject.Find("FleeBattleButton"); // 실제 후퇴 버튼
            Assert.That(flee, Is.Not.Null); // 후퇴 선택 제공 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); // 후퇴 버튼 방향 입력
            yield return null; // 방향 입력 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
            yield return null; // 선택 변경 반영 대기
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(flee)); // 키보드 후퇴 버튼 선택
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 실제 후퇴 선택
            yield return null; // 결과 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 결과 반영 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 실패 후 전투 유지
            Assert.That(GameObject.Find("RoundStatus").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("2라운드")); // 후퇴 실패의 행동 소모
            Assert.That(GameObject.Find("Notice").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("후퇴 실패")); // 실제 실패 안내
            Assert.That(AppRoot.Instance.PlayerHealth, Is.EqualTo(94)); // 상대 행동의 실제 피해
            Assert.That(rolls.Count, Is.Zero); // 두 후퇴 주사위 사용 확인
        } // 검증 끝
        [UnityTest] // 실제 전투 후퇴와 재진입 검증
        public IEnumerator EliteEncounter_RetreatSuccess_PreservesEnemyHealth() // 이전 방 복귀와 시험 적 체력 복원
        { // 검증 시작
            var rolls = new Queue<int>(); // 결정 가능한 후퇴 주사위
            rolls.Enqueue(20); // 플레이어 후퇴 주사위
            rolls.Enqueue(1); // 상대 추격 주사위
            var property = typeof(CombatScreenView).GetProperty("RetreatRollOverride"); // 후퇴 주사위 연결 조회
            Assert.That(property, Is.Not.Null); // 후퇴 주사위 제공 확인
            property.SetValue(null, (System.Func<int, int, int>)((minimum, maximum) => rolls.Dequeue())); // 성공 주사위 지정
            CombatScreenView.RandomOverride = (minimum, maximum) => minimum; // 재현 가능한 시험 공격 난수
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var layout = travel.Layout; // 현재 층 구조
            var target = layout.GetRoom(travel.RoomX, travel.RoomY); // 현재 상위 개체 방
            var route = FindTestRoute(layout, layout.GetRoom(0, 0), target); // 시작 방부터 조우 방 경로
            var entry = route[route.Count - 1]; // 직전 방의 진입 출구
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 방 조회
            var args = new object[] // 조우 방 조회 인수
            { // 인수 목록 시작
                target.X, target.Y // 상위 개체 방 좌표
            }; // 인수 목록 끝
            var targetRoot = (GameObject)finder.Invoke(travel, args); // 실제 조우 방
            GameObject.Find("EncounterFightButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 일반 전투 선택
            yield return null; // 전투 화면 준비 대기
            GameObject.Find("AdvanceBattleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 상대에게 피해 적용
            Assert.That(GameObject.Find("EnemyHealth").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("27/70")); // 후퇴 전 상대 체력 확인
            yield return null; // 중복 행동 방지 프레임 대기
            GameObject.Find("FleeBattleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 속도 주사위 후퇴 선택
            yield return null; // 탐험 복귀 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 성공한 후퇴의 탐험 복귀
            Assert.That(travel.RoomX, Is.EqualTo(entry.RoomX)); // 직전 방 가로 위치 복귀
            Assert.That(travel.RoomY, Is.EqualTo(entry.RoomY)); // 직전 방 세로 위치 복귀
            Assert.That(targetRoot.transform.Find("EncounterMarker"), Is.Not.Null); // 미완료 대상 표시 보존
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                var image = CaptureScreen("DungeonCanvas"); // 실제 후퇴 후 탐험 화면
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 플레이 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
            var returnDoor = layout.GetConnection(entry.RoomX, entry.RoomY, target.X, target.Y); // 조우 방으로 다시 가는 출구
            yield return UseDoor(travel, player, returnDoor); // 실제 이전 방에서 재진입
            var floor = targetRoot.transform.Find("Floor").GetComponent<Tilemap>(); // 조우 방 바닥
            var obstacles = targetRoot.transform.Find("Obstacles").GetComponent<Tilemap>(); // 조우 방 장애물
            Assert.That(player.ChangeRoom(floor, obstacles, 0, 2), Is.True); // 대상 범위 밖 배치
            yield return null; // 이동 준비 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S)); // 대상 방향 이동 입력
            InputSystem.Update(); // 이동 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 이동 키 해제
            InputSystem.Update(); // 해제 반영
            var deadline = Time.realtimeSinceStartup + 10f; // 조우 대기 제한
            while (player.IsMoving && Time.realtimeSinceStartup < deadline) // 대상 접근 완료 대기
            { // 이동 대기 시작
                yield return null; // 다음 프레임 대기
            } // 이동 대기 끝
            yield return null; // 조우 화면 준비 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Encounter)); // 재진입한 대상 조우
            GameObject.Find("EncounterFightButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 재진입 전투 선택
            yield return null; // 전투 화면 반영 대기
            Assert.That(GameObject.Find("EnemyHealth").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("27/70")); // 후퇴 전 상대 체력 복원
        } // 검증 끝
        [UnityTest] // 이전 방이 없는 전투 후퇴 차단 검증
        public IEnumerator CombatRetreat_NoPreviousRoom_DoesNotRollOrConsumeTurn() // 직전 방 부재와 행동 보존
        { // 검증 시작
            yield return EnterDungeon(); // 실제 시작 방 진입
            var count = 0; // 후퇴 주사위 호출 횟수
            CombatScreenView.RetreatRollOverride = (minimum, maximum) => // 검증용 후퇴 주사위
            { // 주사위 호출 시작
                count++; // 호출 횟수 증가
                return minimum; // 유효한 최소 주사위
            }; // 주사위 호출 끝
            Assert.That(AppRoot.Instance.StartCombat(), Is.True); // 시작 방의 시험 전투 진입
            yield return null; // 전투 화면 준비 대기
            GameObject.Find("FleeBattleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 직전 방 없는 후퇴 요청
            Assert.That(count, Is.Zero); // 불가능한 이동의 주사위 미사용
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 기존 전투 유지
            Assert.That(GameObject.Find("RoundStatus").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("1라운드")); // 기존 플레이어 행동 보존
            Assert.That(GameObject.Find("Notice").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("이전 방")); // 후퇴 불가 이유 안내
        } // 검증 끝
        [UnityTest] // 성공한 전투 후퇴의 이동 실패 복구 검증
        public IEnumerator CombatRetreat_BlockedArrival_RetriesWithoutReroll() // 성공 판정과 적 체력 보존
        { // 검증 시작
            var rolls = new Queue<int>(); // 결정 가능한 후퇴 주사위
            rolls.Enqueue(20); // 플레이어 후퇴 주사위
            rolls.Enqueue(1); // 상대 추격 주사위
            CombatScreenView.RetreatRollOverride = (minimum, maximum) => rolls.Dequeue(); // 성공 주사위 지정
            yield return EnterEliteEncounter(); // 실제 상위 개체 조우 진입
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 이동 제어
            var target = travel.Layout.GetRoom(travel.RoomX, travel.RoomY); // 현재 상위 개체 방
            var route = FindTestRoute(travel.Layout, travel.Layout.GetRoom(0, 0), target); // 시작 방부터 실제 경로
            var entry = route[route.Count - 1]; // 조우 방 진입 출구
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 방 조회
            var args = new object[] // 이전 방 좌표 인수
            { // 인수 목록 시작
                entry.RoomX, entry.RoomY // 이전 방 좌표
            }; // 인수 목록 끝
            var previous = (GameObject)finder.Invoke(travel, args); // 실제 이전 방
            var targetArgs = new object[] // 조우 방 좌표 인수
            { // 인수 목록 시작
                target.X, target.Y // 상위 개체 방 좌표
            }; // 인수 목록 끝
            var targetRoot = (GameObject)finder.Invoke(travel, targetArgs); // 실제 조우 방
            var floor = targetRoot.transform.Find("Floor").GetComponent<Tilemap>(); // 시험 장애물용 바닥
            var blocker = previous.AddComponent<DoorArrivalBlocker>(); // 이전 방 활성화 시 도착 칸 차단
            blocker.Cell = new Vector3Int(entry.ApproachX, entry.ApproachY, 0); // 이전 방 도착 칸
            blocker.Tile = floor.GetTile(Vector3Int.zero); // 차단용 시험 타일
            GameObject.Find("EncounterFightButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 일반 전투 선택
            yield return null; // 전투 화면 준비 대기
            gamepad = InputSystem.AddDevice<Gamepad>(); // 시험 게임패드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 입력 준비 대기
            var flee = GameObject.Find("FleeBattleButton"); // 실제 후퇴 버튼
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(flee); // 게임패드 후퇴 선택
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 성공한 후퇴 요청
            yield return null; // 실패한 이동 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 해제 반영 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 도착 차단 시 기존 전투 유지
            Assert.That(rolls.Count, Is.Zero); // 성공 판정의 두 주사위 보존
            Assert.That(GameObject.Find("AdvanceBattleButton").GetComponent<UnityEngine.UI.Button>().interactable, Is.False); // 확정 후 공격 차단
            Assert.That(GameObject.Find("ExitBattleButton").GetComponent<UnityEngine.UI.Button>().interactable, Is.False); // 확정 후 시험 종료 차단
            GameObject.Find("AdvanceBattleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); // 잘못된 공격 요청
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Combat)); // 성공 판정과 기존 전투 유지
            Object.Destroy(blocker); // 시험 도착 차단 장치 제거
            previous.transform.Find("Obstacles").GetComponent<Tilemap>().SetTile(blocker.Cell, null); // 시험 장애물 제거
            yield return null; // 제거 반영 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South)); // 같은 후퇴의 이동 재시도
            yield return null; // 재시도 처리 대기
            InputSystem.QueueStateEvent(gamepad, new GamepadState()); // 선택 버튼 해제
            yield return null; // 탐험 복귀 대기
            Assert.That(AppRoot.Instance.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 성공한 후퇴의 탐험 복귀
            Assert.That(travel.RoomX, Is.EqualTo(entry.RoomX)); // 직전 방 가로 좌표
            Assert.That(travel.RoomY, Is.EqualTo(entry.RoomY)); // 직전 방 세로 좌표
            Assert.That(targetRoot.transform.Find("EncounterMarker"), Is.Not.Null); // 후퇴 후 미완료 대상 보존
            Assert.That(rolls.Count, Is.Zero); // 재시도 시 주사위 미사용
        } // 검증 끝
        [UnityTest] // 실제 잠긴 문과 열쇠 이동 검증
        public IEnumerator LockedDoor_RequiresKey_AndRestoresItOnFailedArrival() // 잠금 확인과 열쇠 복구 및 왕복
        { // 검증 시작
            var seed = Enumerable.Range(0, 100).First(value => new DoorLockState(DungeonGenerator.Generate(value)).TrialDoor != null); // 시험 말단 방이 있는 시드
            RoomTravelController.GenerationSeedOverride = seed; // 재현 가능한 시험 던전
            yield return EnterDungeon(); // 실제 던전 진입
            keyboard = InputSystem.AddDevice<Keyboard>(); // 검증용 키보드 연결
            InputSystem.Update(); // 입력 장치 반영
            yield return null; // 입력 준비 대기
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 방 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            var lockedProperty = typeof(RoomTravelController).GetProperty("TrialLockedDoor"); // 실제 시험 잠긴 문 조회
            Assert.That(lockedProperty, Is.Not.Null); // 시험 잠긴 문 제공 확인
            var door = (RoomDoor)lockedProperty.GetValue(travel); // 현재 층의 시험 잠긴 문
            Assert.That(door, Is.Not.Null); // 접근 가능한 시험 문 존재 확인
            var lockMethod = typeof(RoomTravelController).GetMethod("IsDoorLocked"); // 실제 잠금 상태 조회
            Assert.That(lockMethod, Is.Not.Null); // 양방향 잠금 상태 제공 확인
            var keyProperty = typeof(AppRoot).GetProperty("Keys"); // 게임의 열쇠 보관소 조회
            Assert.That(keyProperty, Is.Not.Null); // 열쇠 보관소 제공 확인
            var keys = (DoorKeyRing)keyProperty.GetValue(AppRoot.Instance); // 실제 보유 열쇠
            var layout = travel.Layout; // 현재 층의 방 연결
            var start = layout.GetRoom(travel.RoomX, travel.RoomY); // 현재 출발 방
            var source = layout.GetRoom(door.RoomX, door.RoomY); // 잠긴 문의 출발 방
            foreach (var routeDoor in FindTestRoute(layout, start, source)) // 출발 방까지 실제 경로 순회
            { // 경로 이동 시작
                yield return UseDoor(travel, player, routeDoor); // 실제 키 입력으로 문 통과
            } // 경로 이동 끝
            var finder = typeof(RoomTravelController).GetMethod("FindRoom", BindingFlags.Instance | BindingFlags.NonPublic); // 현재 방 객체 조회
            var sourceArguments = new object[] // 출발 방 좌표 인수
            { // 인수 목록 시작
                door.RoomX, door.RoomY // 잠긴 문의 출발 좌표
            }; // 인수 목록 끝
            var targetArguments = new object[] // 도착 방 좌표 인수
            { // 인수 목록 시작
                door.TargetRoomX, door.TargetRoomY // 잠긴 문의 도착 좌표
            }; // 인수 목록 끝
            var sourceRoot = (GameObject)finder.Invoke(travel, sourceArguments); // 실제 출발 방
            var targetRoot = (GameObject)finder.Invoke(travel, targetArguments); // 실제 도착 방
            var floor = sourceRoot.transform.Find("Floor").GetComponent<Tilemap>(); // 출발 방 바닥
            var obstacles = sourceRoot.transform.Find("Obstacles").GetComponent<Tilemap>(); // 출발 방 장애물
            var before = new Vector3Int(door.ApproachX, door.ApproachY, 0) - Offset(door.Direction); // 문 앞 한 칸 전 위치
            Assert.That(player.ChangeRoom(floor, obstacles, before.x, before.y), Is.True); // 실제 출구 접근 위치 배치
            yield return null; // 위치 변경 프레임 종료
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(DirectionKey(door.Direction))); // 실제 문 방향 이동
            InputSystem.Update(); // 이동 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 방향 키 해제
            InputSystem.Update(); // 해제 반영
            var deadline = Time.realtimeSinceStartup + 10f; // 이동 대기 제한
            while (player.IsMoving && Time.realtimeSinceStartup < deadline) // 문 앞 이동 완료 대기
            { // 이동 대기 시작
                yield return null; // 다음 프레임 대기
            } // 이동 대기 끝
            Assert.That(player.IsMoving, Is.False); // 문 앞 도착 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 열쇠 없이 문 상호작용
            InputSystem.Update(); // 잠긴 문 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 상호작용 키 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 안내 표시 대기
            Assert.That(travel.RoomX, Is.EqualTo(door.RoomX)); // 열쇠 없는 가로 방 유지
            Assert.That(travel.RoomY, Is.EqualTo(door.RoomY)); // 열쇠 없는 세로 방 유지
            var lockArguments = new object[] // 잠금 상태 조회 인수
            { // 인수 목록 시작
                door // 현재 시험 문
            }; // 인수 목록 끝
            Assert.That((bool)lockMethod.Invoke(travel, lockArguments), Is.True); // 열쇠 없는 잠금 유지
            var notice = GameObject.Find("DoorNotice").GetComponent<UnityEngine.UI.Text>(); // 실제 잠금 안내 조회
            Assert.That(notice.text, Does.Contain("열쇠: 0")); // 열쇠 부족 안내 확인
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 최신 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 화면 없는 캡처 검사
            { // 캡처 조건 시작
                var image = CaptureScreen("DungeonCanvas"); // 실제 잠긴 문 화면 렌더링
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 화면 저장
                Object.Destroy(image); // 임시 이미지 정리
            } // 캡처 조건 끝
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.I)); // 시험 열쇠 획득 화면 열기
            InputSystem.Update(); // 인벤토리 열기 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 열기 키 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 인벤토리 표시 대기
            var keyButton = GameObject.Find("TrialKeyButton"); // 시험 열쇠 지급 버튼 조회
            Assert.That(keyButton, Is.Not.Null); // 시험 열쇠 버튼 확인
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(keyButton); // 열쇠 버튼 선택
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); // 실제 시험 열쇠 획득 입력
            yield return null; // 화면 입력 처리 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 선택 키 해제
            yield return null; // 획득 결과 반영 대기
            Assert.That(keys.Count, Is.EqualTo(1)); // 시험 열쇠 한 개 확인
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.I)); // 인벤토리 닫기
            yield return null; // 취소 입력 처리
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 닫기 키 해제
            yield return null; // 탐험 복귀 대기
            Assert.That(notice.text, Is.Empty); // 인벤토리 복귀 시 이전 열쇠 안내 제거
            var blocker = targetRoot.AddComponent<DoorArrivalBlocker>(); // 도착 방 활성화 시 차단 장치
            blocker.Cell = new Vector3Int(door.TargetX, door.TargetY, 0); // 실제 문 도착 칸
            blocker.Tile = floor.GetTile(Vector3Int.zero); // 차단용 시험 타일
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 도착 차단된 문 열기 시도
            InputSystem.Update(); // 문 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 상호작용 키 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 실패한 이동 정리 대기
            Assert.That(travel.RoomX, Is.EqualTo(door.RoomX)); // 실패 시 기존 가로 방 유지
            Assert.That(travel.RoomY, Is.EqualTo(door.RoomY)); // 실패 시 기존 세로 방 유지
            Assert.That(keys.Count, Is.EqualTo(1)); // 실패한 이동의 열쇠 복구
            Assert.That((bool)lockMethod.Invoke(travel, lockArguments), Is.True); // 실패한 이동의 잠금 복구
            Object.Destroy(blocker); // 도착 차단 장치 제거
            targetRoot.transform.Find("Obstacles").GetComponent<Tilemap>().SetTile(blocker.Cell, null); // 시험 장애물 제거
            yield return null; // 장치 제거 완료 대기
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); // 열쇠로 실제 문 열기
            InputSystem.Update(); // 문 입력 반영
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); // 상호작용 키 해제
            InputSystem.Update(); // 해제 반영
            yield return null; // 방 이동 표시 대기
            Assert.That(travel.RoomX, Is.EqualTo(door.TargetRoomX)); // 성공한 도착 가로 좌표
            Assert.That(travel.RoomY, Is.EqualTo(door.TargetRoomY)); // 성공한 도착 세로 좌표
            Assert.That(keys.Count, Is.Zero); // 열쇠 한 개 소모 확인
            var reverse = layout.GetConnection(door.TargetRoomX, door.TargetRoomY, door.RoomX, door.RoomY); // 같은 문의 역방향 연결
            lockArguments[0] = reverse; // 반대 방향 문으로 조회 변경
            Assert.That((bool)lockMethod.Invoke(travel, lockArguments), Is.False); // 역방향 잠금 해제 확인
            yield return UseDoor(travel, player, reverse); // 열쇠 없이 실제 역방향 이동
            Assert.That(keys.Count, Is.Zero); // 열린 문의 중복 소모 방지
            Assert.That(travel.RoomX, Is.EqualTo(door.RoomX)); // 출발 방 복귀 확인
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
        [UnityTearDown] // 검증 후 정리 표시
        public IEnumerator Cleanup() // 검증 시드와 공통 객체 정리
        { // 정리 시작
            RoomTravelController.GenerationSeedOverride = null; // 검증 시드 해제
            typeof(CombatScreenView).GetProperty("RandomOverride")?.SetValue(null, null); // 검증용 전투 난수 해제
            typeof(CombatScreenView).GetProperty("RetreatRollOverride")?.SetValue(null, null); // 검증용 후퇴 주사위 해제
            typeof(RoomTravelController).GetProperty("ForceRollOverride")?.SetValue(null, null); // 검증용 이벤트 주사위 해제
            typeof(EncounterScreenView).GetProperty("EscapeRollOverride")?.SetValue(null, null); // 검증용 도망 주사위 해제
            typeof(EncounterScreenView).GetProperty("ObserveRollOverride")?.SetValue(null, null); // 검증용 관찰 주사위 해제
            typeof(EncounterScreenView).GetProperty("PersuadeRollOverride")?.SetValue(null, null); // 검증용 회유 주사위 해제
            if (testController != null) // 검증용 제어 객체 존재 검사
            { // 제어 객체 정리 시작
                Object.Destroy(testController); // 검증용 제어 객체 제거
            } // 제어 객체 정리 끝
            if (testPrototype != null) // 검증용 방 존재 검사
            { // 검증 방 정리 시작
                Object.Destroy(testPrototype); // 검증용 방 제거
            } // 검증 방 정리 끝
            if (gamepad != null && gamepad.added) // 검증 게임패드 존재 검사
            { // 게임패드 정리 시작
                InputSystem.RemoveDevice(gamepad); // 검증용 게임패드 제거
            } // 게임패드 정리 끝
            if (mouse != null && mouse.added) // 검증 마우스 존재 검사
            { // 마우스 정리 시작
                InputSystem.RemoveDevice(mouse); // 검증용 마우스 제거
            } // 마우스 정리 끝
            if (keyboard != null && keyboard.added) // 검증 키보드 존재 검사
            { // 키보드 정리 시작
                InputSystem.RemoveDevice(keyboard); // 검증용 키보드 제거
            } // 키보드 정리 끝
            if (AppRoot.Instance != null) // 공통 객체 존재 검사
            { // 정리 조건 시작
                Object.Destroy(AppRoot.Instance.gameObject); // 검증용 공통 객체 제거
            } // 정리 조건 끝
            yield return null; // 객체 제거 완료 대기
            ObservationStorage.TestDirectoryOverride = null; // 개별 시험 저장 경로 해제
            InputSystem.settings.backgroundBehavior = originalBackground; // 기존 백그라운드 입력 설정 복구
#if UNITY_EDITOR // 편집기 입력 설정 복구
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditor; // 기존 편집기 설정 복구
#endif // 편집기 입력 설정 끝
        } // 정리 끝
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
        private static string Signature(DungeonLayout layout) // 실제 방 구조 비교
        { // 비교 시작
            return string.Join("|", layout.Rooms.Select(room => room.X + "," + room.Y + ":" + room.Kind).OrderBy(value => value)); // 정렬된 좌표와 역할
        } // 비교 끝
    } // 클래스 끝
} // 이름 공간 끝
