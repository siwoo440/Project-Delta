using System.Collections; // 프레임 기반 대기
using NUnit.Framework; // 실제 게임 검증 도구
using ProjectDelta.Core; // 회차와 층 규칙
using ProjectDelta.Runtime; // 실제 층 이동과 저장
using UnityEngine; // 게임 객체와 시간
using UnityEngine.SceneManagement; // 실제 새 실행 시작
using UnityEngine.TestTools; // 프레임 기반 검증
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class FloorTransitionSavePlayModeTests // 층 이동의 자동 저장과 실패 복구 검증
    { // 클래스 시작
        [SetUp] // 독립 검증 준비
        public void Setup() // 실제 콘텐츠와 별도 회차 저장 설정
        { // 준비 시작
            RunStorage.TestDirectoryOverride = System.IO.Path.Combine(Application.dataPath, "..", "Artifacts", "FloorTransitionSaveTests", System.Guid.NewGuid().ToString("N")); // 독립 회차 저장 경로
            ObservationStorage.TestDirectoryOverride = RunStorage.TestDirectoryOverride; // 독립 영구 기록 경로
            RoomTravelController.GenerationSeedOverride = 0; // 재현 가능한 실제 던전
            RoomTravelController.RoomContentTestOverride = true; // 실제 상자와 함정 활성화
            RoomTravelController.TrialEventTestOverride = true; // 실제 시험 이벤트 활성화
            RoomTravelController.TutorialEncounterTestOverride = false; // 시험 이동의 첫 조우 제외
            AppRoot.AutomaticEncounterTestOverride = false; // 별도 조우 연출 제외
        } // 준비 끝
        [UnityTearDown] // 독립 검증 정리
        public IEnumerator Cleanup() // 회차와 저장 설정 복구
        { // 정리 시작
            RoomTravelController.GenerationSeedOverride = null; // 던전 시드 복구
            RoomTravelController.RoomContentTestOverride = null; // 방 콘텐츠 설정 복구
            RoomTravelController.TrialEventTestOverride = null; // 이벤트 설정 복구
            RoomTravelController.TutorialEncounterTestOverride = null; // 조우 설정 복구
            AppRoot.AutomaticEncounterTestOverride = null; // 전투 설정 복구
            if (AppRoot.Instance != null) // 실제 회차 객체 검사
            { // 객체 정리 시작
                Object.Destroy(AppRoot.Instance.gameObject); // 독립 회차 제거
            } // 객체 정리 끝
            yield return null; // 객체 제거 완료 대기
            RunStorage.TestDirectoryOverride = null; // 회차 경로 복구
            ObservationStorage.TestDirectoryOverride = null; // 영구 기록 경로 복구
        } // 정리 끝
        [UnityTest] // 다음 층 자동 저장과 실제 재실행 검증
        public IEnumerator Transition_SavesSecondFloorAndRestartsThere() // 자동 저장 후 두 번째 층 재개
        { // 검증 시작
            yield return EnterDungeon(); // 실제 새 회차 진입
            var app = AppRoot.Instance; // 실제 회차 관리자
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 층 이동 제어
            app.UpdatePlayerHealth(51); // 이동 전의 감소한 체력
            Assert.That(app.TrySaveRun(out var message), Is.True, message); // 이전 층의 기준 저장
            travel.SendMessage("TryNextFloor"); // 실제 다음 층 전환 요청
            yield return null; // 실제 층 이동 처리 대기
            Assert.That(travel.Floor, Is.EqualTo(2)); // 실제 두 번째 층 도착
            var saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out var warning); // 독립 저장 파일 읽기
            Assert.That(warning, Is.Empty); // 정상 자동 저장 확인
            Assert.That(saved, Is.Not.Null); // 실제 회차 파일 존재
            Assert.That(saved.floor, Is.EqualTo(2)); // 새 층의 자동 저장
            Assert.That(saved.roomX, Is.Zero); // 새 시작 방 가로 저장
            Assert.That(saved.roomY, Is.Zero); // 새 시작 방 세로 저장
            Assert.That(saved.health, Is.EqualTo(app.PlayerHealth)); // 저장된 체력과 실제 체력 일치
            Assert.That(RunRestoreRules.IsValid(saved), Is.True); // 저장된 새 층의 복원 가능성
            Object.Destroy(app.gameObject); // 현재 실행 종료
            yield return null; // 공통 객체 제거 대기
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 새 실행 시작
            yield return WaitForPhase(GamePhase.Title); // 타이틀 도착 대기
            app = AppRoot.Instance; // 새 실행 관리자
            Assert.That(app.ContinueRun(), Is.True); // 자동 저장 회차 재개
            yield return WaitForPhase(GamePhase.Exploration); // 실제 탐험 복원 대기
            travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 복원된 층 이동 제어
            Assert.That(travel.Floor, Is.EqualTo(2)); // 실제 두 번째 층 재개
            Assert.That(travel.RoomX, Is.Zero); // 복원된 시작 방 가로 좌표
            Assert.That(travel.RoomY, Is.Zero); // 복원된 시작 방 세로 좌표
            Assert.That(app.PlayerHealth, Is.EqualTo(saved.health)); // 재시작 후 체력 보존
        } // 검증 끝
        [UnityTest] // 층 저장 실패와 재시도 검증
        public IEnumerator Transition_SaveLockPreservesFloorPositionAndHealth() // 저장 실패 시 기존 탐험 유지
        { // 검증 시작
            yield return EnterDungeon(); // 실제 새 회차 진입
            var app = AppRoot.Instance; // 실제 회차 관리자
            var travel = GameObject.Find("RoomTravel").GetComponent<RoomTravelController>(); // 실제 층 이동 제어
            var player = GameObject.Find("PlayerPreview").GetComponent<GridMoveController>(); // 실제 플레이어 위치
            app.UpdatePlayerHealth(51); // 이동 실패 전 체력
            Assert.That(app.TrySaveRun(out var message), Is.True, message); // 이전 층의 기준 저장
            var oldFloor = travel.Floor; // 기존 층 번호
            var oldRoomX = travel.RoomX; // 기존 방 가로 좌표
            var oldRoomY = travel.RoomY; // 기존 방 세로 좌표
            var oldPlayerX = player.X; // 기존 플레이어 가로 칸
            var oldPlayerY = player.Y; // 기존 플레이어 세로 칸
            using (var gate = new System.IO.FileStream(System.IO.Path.Combine(RunStorage.TestDirectoryOverride, "run.lock"), System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None)) // 실제 저장 잠금 점유
            { // 저장 실패 상황 시작
                travel.SendMessage("TryNextFloor"); // 저장 잠금 중 다음 층 요청
                yield return null; // 실제 실패 처리 대기
                Assert.That(travel.Floor, Is.EqualTo(oldFloor)); // 실패 후 기존 층 유지
                Assert.That(travel.RoomX, Is.EqualTo(oldRoomX)); // 실패 후 기존 방 가로 유지
                Assert.That(travel.RoomY, Is.EqualTo(oldRoomY)); // 실패 후 기존 방 세로 유지
                Assert.That(player.X, Is.EqualTo(oldPlayerX)); // 실패 후 기존 칸 가로 유지
                Assert.That(player.Y, Is.EqualTo(oldPlayerY)); // 실패 후 기존 칸 세로 유지
                Assert.That(app.PlayerHealth, Is.EqualTo(51)); // 실패 후 기존 체력 유지
                Assert.That(app.Flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 실패 후 탐험 상태 유지
                var saved = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 실제 저장 파일 재조회
                Assert.That(saved.floor, Is.EqualTo(oldFloor)); // 실패 시 이전 층 저장 유지
                Assert.That(saved.health, Is.EqualTo(51)); // 실패 시 이전 체력 저장 유지
            } // 저장 잠금 해제
            yield return null; // 재시도 프레임 대기
            travel.SendMessage("TryNextFloor"); // 잠금 해제 후 재시도
            yield return null; // 실제 재시도 완료 대기
            Assert.That(travel.Floor, Is.EqualTo(2)); // 재시도 성공의 새 층
            var retried = new RunStorage(RunStorage.TestDirectoryOverride).Load(out _); // 재시도 후 저장 조회
            Assert.That(retried.floor, Is.EqualTo(2)); // 실제 새 층 저장 성공
            Assert.That(retried.health, Is.EqualTo(app.PlayerHealth)); // 저장된 새 층 체력 일치
        } // 검증 끝
        private static IEnumerator EnterDungeon() // 실제 새 회차와 던전 진입
        { // 진입 시작
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 실제 시작 씬 열기
            yield return WaitForPhase(GamePhase.Title); // 타이틀 상태 대기
            AppRoot.Instance.StartNewGame(); // 실제 새 게임 시작
            yield return WaitForPhase(GamePhase.Exploration); // 실제 탐험 준비 대기
        } // 진입 끝
        private static IEnumerator WaitForPhase(GamePhase phase) // 실제 게임 전환 완료 대기
        { // 대기 시작
            var deadline = Time.realtimeSinceStartup + 20f; // 상태 전환 제한 시간
            while (Time.realtimeSinceStartup < deadline) // 제한 시간 안의 상태 검사
            { // 검사 시작
                if (AppRoot.Instance != null && AppRoot.Instance.Flow.Phase == phase) // 실제 목표 상태 검사
                { // 도착 시작
                    yield return null; // 씬 준비 완료 대기
                    yield break; // 정상 대기 종료
                } // 도착 끝
                yield return null; // 다음 프레임 대기
            } // 검사 끝
            Assert.Fail("Timed out waiting for " + phase); // 전환 지연 표시
        } // 대기 끝
    } // 클래스 끝
} // 이름 공간 끝
