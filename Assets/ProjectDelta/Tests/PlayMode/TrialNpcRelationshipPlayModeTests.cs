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
    public sealed class TrialNpcRelationshipPlayModeTests // 실제 상인 관계와 영구 저장 검증
    { // 클래스 시작
        private Keyboard keyboard; // 독립 검증 키보드
        private InputSettings.BackgroundBehavior originalBackground; // 기존 입력 설정
#if UNITY_EDITOR // 편집기 입력 설정 조건
        private InputSettings.EditorInputBehaviorInPlayMode originalEditor; // 기존 편집기 설정
#endif // 편집기 설정 끝
        [SetUp] // 실제 상인 검증 준비
        public void Setup() // 독립 저장과 결정적 던전 설정
        { // 준비 시작
            RunStorage.TestDirectoryOverride = System.IO.Path.Combine(Application.dataPath, "..", "Artifacts", "TrialNpcRelationshipPlayModeTests", System.Guid.NewGuid().ToString("N")); // 독립 회차 저장
            ObservationStorage.TestDirectoryOverride = RunStorage.TestDirectoryOverride; // 독립 관찰 저장
            RoomTravelController.RoomContentTestOverride = true; // 실제 방 콘텐츠 활성화
            RoomTravelController.TrialNpcTestOverride = true; // 실제 시험 상인 활성화
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
            RoomTravelController.TrialNpcTestOverride = null; // 시험 상인 설정 복구
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
        [UnityTest] // 우호 선택의 영구 저장과 회차별 중복 검증
        public IEnumerator Affinity_PersistsAcrossRestartAndNewRun() // 실제 대화와 저장 재개
        { // 검증 시작
            yield return EnterDungeon(); // 실제 새 회차 진입
            var app = AppRoot.Instance; // 실제 회차 관리자
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 층 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            yield return GoToNpc(travel, player); // 실제 상인 방 이동과 대화
            var affinity = GameObject.Find("TrialNpcAffinity").GetComponent<Text>(); // 실제 관계 표시
            Assert.That(affinity.text, Does.StartWith("호감도 0/100")); // 초기 상인 관계 확인
            var choice = GameObject.Find("TrialNpcFriendlyChoice").GetComponent<Button>(); // 실제 우호 선택
            Assert.That(choice.interactable, Is.True); // 최초 우호 선택 활성 확인
            choice.onClick.Invoke(); // 첫 우호 선택
            Assert.That(affinity.text, Does.StartWith("호감도 5/100")); // 시험 호감도 증가 확인
            choice.onClick.Invoke(); // 같은 회차의 중복 선택 시도
            Assert.That(affinity.text, Does.StartWith("호감도 5/100")); // 같은 회차 중복 증가 차단
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 실제 화면 캡처 경로
            if (!string.IsNullOrEmpty(capturePath) && Application.isBatchMode) // 자동 캡처 요청 검사
            { // 캡처 시작
                yield return null; // 대화 갱신 프레임 대기
                var image = CaptureScreen("TrialNpcCanvas"); // 실제 관계 화면 렌더링
                System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 실제 화면 저장
                Object.Destroy(image); // 캡처 이미지 제거
            } // 캡처 끝
            GameObject.Find("TrialNpcLeave").GetComponent<Button>().onClick.Invoke(); // 실제 대화 종료
            yield return null; // 탐험 복귀 대기
            Assert.That(app.TrySaveRun(out var message), Is.True, message); // 실제 현재 회차 저장
            Object.Destroy(app.gameObject); // 현재 앱 종료
            yield return null; // 앱 객체 제거 대기
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 새 실행 시작
            yield return WaitForPhase(GamePhase.Title); // 타이틀 진입 대기
            app = AppRoot.Instance; // 새 실행 관리자
            Assert.That(app.ContinueRun(), Is.True); // 저장된 회차 재개
            yield return WaitForPhase(GamePhase.Exploration); // 실제 회차 복원 대기
            travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 복원된 층 이동 제어
            player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 복원된 플레이어
            yield return OpenNpc(travel, player); // 저장된 상인 대화 재진입
            affinity = GameObject.Find("TrialNpcAffinity").GetComponent<Text>(); // 복원된 관계 표시
            Assert.That(affinity.text, Does.StartWith("호감도 5/100")); // 앱 재시작 후 관계 보존
            choice = GameObject.Find("TrialNpcFriendlyChoice").GetComponent<Button>(); // 재개한 회차의 우호 선택
            choice.onClick.Invoke(); // 재개한 같은 회차의 중복 시도
            Assert.That(affinity.text, Does.StartWith("호감도 5/100")); // 재시작 후 중복 증가 차단
            GameObject.Find("TrialNpcLeave").GetComponent<Button>().onClick.Invoke(); // 상인 대화 종료
            yield return null; // 탐험 복귀 대기
            app.ReturnToTitle(); // 새로운 회차를 위한 타이틀 복귀
            yield return WaitForPhase(GamePhase.Title); // 실제 타이틀 전환 대기
            app.StartNewGame(); // 새로운 회차 시작
            yield return WaitForPhase(GamePhase.Exploration); // 실제 던전 도착 대기
            travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 새 회차 층 제어
            player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 새 회차 플레이어
            yield return GoToNpc(travel, player); // 새 회차의 실제 상인 대화
            affinity = GameObject.Find("TrialNpcAffinity").GetComponent<Text>(); // 새 회차 관계 표시
            Assert.That(affinity.text, Does.StartWith("호감도 5/100")); // 새 회차에서 영구 관계 유지
            choice = GameObject.Find("TrialNpcFriendlyChoice").GetComponent<Button>(); // 새 회차의 우호 선택
            Assert.That(choice.interactable, Is.True); // 새 회차의 우호 선택 재활성
            choice.onClick.Invoke(); // 새 회차의 우호 선택
            Assert.That(affinity.text, Does.StartWith("호감도 10/100")); // 새 회차의 별도 관계 변화
        } // 검증 끝
        [UnityTest] // 영구 관계 저장 충돌의 원본 보존 검증
        public IEnumerator Affinity_ProfileLockPreservesValueAndAllowsRetry() // 실제 저장 잠금과 재시도
        { // 검증 시작
            yield return EnterDungeon(); // 실제 새 회차 진입
            var app = AppRoot.Instance; // 현재 회차 관리자
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 층 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            yield return GoToNpc(travel, player); // 실제 상인 대화
            GameObject.Find("TrialNpcFriendlyChoice").GetComponent<Button>().onClick.Invoke(); // 첫 회차의 우호 선택
            Assert.That(GameObject.Find("TrialNpcAffinity").GetComponent<Text>().text, Does.StartWith("호감도 5/100")); // 실제 영구 관계 생성
            var profilePath = System.IO.Path.Combine(ObservationStorage.TestDirectoryOverride, "npc-relationship-v1.json"); // 영구 관계 파일 경로
            var lockPath = System.IO.Path.Combine(ObservationStorage.TestDirectoryOverride, "npc-relationship.lock"); // 영구 관계 잠금 경로
            Assert.That(System.IO.File.Exists(profilePath), Is.True); // 실제 영구 관계 파일 확인
            var before = System.IO.File.ReadAllBytes(profilePath); // 저장 충돌 전 원본 바이트
            GameObject.Find("TrialNpcLeave").GetComponent<Button>().onClick.Invoke(); // 상인 대화 종료
            yield return null; // 탐험 복귀 대기
            app.ReturnToTitle(); // 새 회차 시작을 위한 타이틀 복귀
            yield return WaitForPhase(GamePhase.Title); // 실제 타이틀 전환 대기
            app.StartNewGame(); // 별도 회차 시작
            yield return WaitForPhase(GamePhase.Exploration); // 새 회차 던전 대기
            travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 새 회차 층 이동 제어
            player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 새 회차 플레이어
            yield return GoToNpc(travel, player); // 새 회차의 실제 상인 대화
            var affinity = GameObject.Find("TrialNpcAffinity").GetComponent<Text>(); // 저장 충돌 전 관계 표시
            Assert.That(affinity.text, Does.StartWith("호감도 5/100")); // 기존 영구 관계 확인
            var choice = GameObject.Find("TrialNpcFriendlyChoice").GetComponent<Button>(); // 새 회차의 우호 선택
            using (var gate = new System.IO.FileStream(lockPath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None)) // 실제 프로필 저장 잠금 점유
            { // 저장 충돌 시작
                choice.onClick.Invoke(); // 잠금 중 실제 우호 선택
                Assert.That(affinity.text, Does.StartWith("호감도 5/100")); // 실패한 저장의 기존 관계 보존
                Assert.That(System.IO.File.ReadAllBytes(profilePath), Is.EqualTo(before)); // 실패 시 영구 파일 원본 보존
            } // 저장 충돌 끝
            choice.onClick.Invoke(); // 잠금 해제 후 같은 선택 재시도
            Assert.That(affinity.text, Does.StartWith("호감도 10/100")); // 실제 저장 성공 후 관계 적용
        } // 검증 끝
        [UnityTest] // 손상 관계 파일과 사용자 경고 보존 검증
        public IEnumerator Affinity_CorruptProfileWarningSurvivesDialogueAndShop() // 대화와 상점 복귀 후 경고 확인
        { // 검증 시작
            var directory = ObservationStorage.TestDirectoryOverride; // 독립 관계 저장 폴더
            System.IO.Directory.CreateDirectory(directory); // 손상 파일을 둘 독립 폴더 준비
            var profilePath = System.IO.Path.Combine(directory, "npc-relationship-v1.json"); // 실제 관계 파일 경로
            var damaged = "{ damaged relationship data"; // 읽을 수 없는 시험 원본
            System.IO.File.WriteAllText(profilePath, damaged); // 앱 시작 전 손상 원본 기록
            var original = System.IO.File.ReadAllBytes(profilePath); // 원본의 실제 바이트 보관
            yield return EnterDungeon(); // 손상 파일을 읽는 실제 앱 시작
            var app = AppRoot.Instance; // 실제 회차 관리자
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 현재 층 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어
            yield return GoToNpc(travel, player); // 실제 상인 방 이동과 대화
            var notice = GameObject.Find("TrialNpcNotice").GetComponent<Text>(); // 관계 저장 경고 표시
            var warning = notice.text; // 상인 화면 최초 경고
            Assert.That(warning, Does.Contain("관계 저장")); // 손상된 관계 저장 경고 확인
            Assert.That(warning, Is.EqualTo(app.RelationshipSaveMessage)); // 실제 저장소 경고 표시 확인
            var choice = GameObject.Find("TrialNpcFriendlyChoice").GetComponent<Button>(); // 우호 선택 버튼
            Assert.That(choice.interactable, Is.False); // 손상 원본 보호의 우호 선택 비활성
            GameObject.Find("TrialNpcTalk").GetComponent<Button>().onClick.Invoke(); // 실제 상인 대화 선택
            Assert.That(notice.text, Is.EqualTo(warning)); // 대화 후 기존 관계 경고 유지
            Assert.That(choice.interactable, Is.False); // 대화 후 우호 선택 차단 유지
            Assert.That(System.IO.File.ReadAllBytes(profilePath), Is.EqualTo(original)); // 대화 후 손상 원본 보존
            GameObject.Find("TrialNpcShop").GetComponent<Button>().onClick.Invoke(); // 실제 상인 상점 진입
            Assert.That(GameObject.Find("EconomyCanvas"), Is.Not.Null); // 관계 손상과 독립적인 상점 진입
            GameObject.Find("CloseEconomyButton").GetComponent<Button>().onClick.Invoke(); // 상점 닫기와 대화 복귀
            yield return null; // 상인 대화 화면 복구 대기
            notice = GameObject.Find("TrialNpcNotice").GetComponent<Text>(); // 복구된 상인 경고 표시
            choice = GameObject.Find("TrialNpcFriendlyChoice").GetComponent<Button>(); // 복구된 우호 선택
            Assert.That(notice.text, Is.EqualTo(warning)); // 상점 복귀 후 최초 경고 유지
            Assert.That(choice.interactable, Is.False); // 상점 복귀 후 우호 선택 차단 유지
            Assert.That(System.IO.File.ReadAllBytes(profilePath), Is.EqualTo(original)); // 상점 복귀 후 손상 원본 보존
        } // 검증 끝
        private IEnumerator GoToNpc(RoomTravelController travel, GridMoveController player) // 실제 상인 방까지 이동과 상호작용
        { // 이동 시작
            var npc = travel.TrialNpcRoom; // 현재 층의 시험 상인 방
            Assert.That(npc, Is.Not.Null); // 배치된 상인 확인
            var route = FindTestRoute(travel.Layout, travel.Layout.GetRoom(travel.RoomX, travel.RoomY), npc); // 현재 위치에서 상인 방 경로
            foreach (var door in route) // 실제 방 출구 순회
            { // 출구 이동 시작
                yield return UseDoorAndResolveEvent(travel, player, door); // 실제 이동과 경유 이벤트 처리
            } // 출구 이동 끝
            yield return OpenNpc(travel, player); // 실제 인접 상호작용
        } // 이동 끝
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
