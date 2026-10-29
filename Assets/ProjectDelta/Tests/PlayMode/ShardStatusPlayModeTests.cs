using System.Collections; // 프레임 기반 대기
using NUnit.Framework; // 실제 게임 검증
using ProjectDelta.Core; // 게임 상태와 결말 규칙
using ProjectDelta.Runtime; // 실제 저장소와 게임 화면
using UnityEngine; // 게임 객체와 시간
using UnityEngine.SceneManagement; // 실제 게임 재시작
using UnityEngine.TestTools; // 프레임 기반 검증
using UnityEngine.UI; // 타이틀 표시 검증
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class ShardStatusPlayModeTests // 타이틀 파편 상태 검증
    { // 클래스 시작
        private EndingFlowPlayModeTests endingFlow; // 기존 실제 마왕 승리 검증
        [SetUp] // 독립 저장과 입력 준비
        public void Setup() // 기존 게임 검증 환경 구성
        { // 준비 시작
            endingFlow = new EndingFlowPlayModeTests(); // 독립 결말 검증 생성
            endingFlow.Setup(); // 실제 결말 검증 환경 준비
        } // 준비 끝
        [UnityTearDown] // 저장 경로와 입력 설정 복구
        public IEnumerator Cleanup() // 독립 게임 환경 정리
        { // 정리 시작
            yield return endingFlow.Cleanup(); // 기존 결말 검증 환경 정리
        } // 정리 끝
        [UnityTest] // 실제 결말과 재시작의 파편 표시 검증
        public IEnumerator HeroicEnding_ShowsEarnedShardsAfterReturnAndRestart() // 영웅 귀환의 누적 파편 확인
        { // 검증 시작
            yield return endingFlow.Victory_ChooseHeroicEndingAndCloseRun(); // 실제 마왕 승리와 영웅 귀환 및 회차 종료
            yield return WaitForTitleStatus(); // 타이틀 파편 상태 생성 대기
            AssertEarnedTen(); // 실제 타이틀의 누적 파편과 사용 제한 확인
            var capturePath = System.Environment.GetEnvironmentVariable("PROJECT_DELTA_CAPTURE_PATH"); // 실제 플레이 화면 저장 경로
            if (!string.IsNullOrEmpty(capturePath)) // 실제 화면 캡처 요청 검사
            { // 캡처 시작
                var capture = typeof(EndingFlowPlayModeTests).GetMethod("CaptureScreen", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic); // 기존 별도 화면 렌더링 조회
                Assert.That(capture, Is.Not.Null); // 실제 캡처 기능 존재 확인
                var image = (Texture2D)capture.Invoke(null, new object[] { "TitleCanvas" }); // 실제 타이틀 화면 렌더링
                try // 실제 플레이 화면 파일 기록
                { // 기록 시작
                    System.IO.File.WriteAllBytes(capturePath, image.EncodeToPNG()); // 최신 타이틀 화면 저장
                } // 기록 끝
                finally // 임시 화면 자원 정리
                { // 정리 시작
                    Object.Destroy(image); // 렌더링 이미지 제거
                } // 정리 끝
            } // 캡처 끝
            Object.Destroy(AppRoot.Instance.gameObject); // 실제 앱 공통 객체 제거
            yield return null; // 이전 공통 객체 제거 대기
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 동일 저장의 실제 재시작
            yield return WaitForTitleStatus(); // 재시작한 타이틀 상태 대기
            AssertEarnedTen(); // 재시작 후 동일한 파편과 사용 제한 확인
            var history = new EndingHistoryStorage(RunStorage.TestDirectoryOverride); // 실제 영구 결말 기록 조회
            var entries = history.Load(out var warning); // 재시작 후 영구 기록 읽기
            Assert.That(warning, Is.Empty); // 영구 기록 정상 확인
            Assert.That(entries.Count, Is.EqualTo(1)); // 결말 영수증 중복 없음
            Assert.That(entries[0].EndingId, Is.EqualTo(EndingRules.ReturnHeroic)); // 실제 영웅 귀환 기록
            Assert.That(history.TotalShards, Is.EqualTo(10)); // 중복 없는 실제 누적 획득량
        } // 검증 끝
        [UnityTest] // 손상 기록의 타이틀 표시 검증
        public IEnumerator CorruptHistory_ShowsUnavailableInsteadOfZero() // 확인되지 않은 파편 잔액 표시
        { // 검증 시작
            System.IO.Directory.CreateDirectory(RunStorage.TestDirectoryOverride); // 독립 저장 폴더 준비
            var path = System.IO.Path.Combine(RunStorage.TestDirectoryOverride, "ending-history-v1.json"); // 실제 결말 기록 파일
            System.IO.File.WriteAllText(path, "damaged ending history"); // 손상된 실제 기록 생성
            yield return SceneManager.LoadSceneAsync("BootstrapScene"); // 손상 기록을 가진 실제 앱 시작
            yield return WaitForTitleStatus(); // 실제 타이틀 파편 상태 대기
            var text = GameObject.Find("EarnedShardsStatus").GetComponent<Text>().text; // 실제 파편 상태 문구
            StringAssert.Contains("확인 불가", text); // 손상 기록의 확인 불가 안내
            StringAssert.DoesNotContain("0", text); // 확인되지 않은 획득량의 영 표시 차단
            Assert.That(System.IO.File.ReadAllText(path), Is.EqualTo("damaged ending history")); // 손상 원본 파일 보존 확인
        } // 검증 끝
        private static void AssertEarnedTen() // 실제 누적 획득 문구 검사
        { // 검사 시작
            var status = GameObject.Find("EarnedShardsStatus"); // 실제 타이틀 파편 표시
            Assert.That(status, Is.Not.Null); // 파편 표시 존재 확인
            var text = status.GetComponent<Text>().text; // 표시된 실제 문구
            StringAssert.Contains("10", text); // 누적 획득 파편 확인
            StringAssert.Contains("사용 불가", text); // 내부 시험의 사용 제한 확인
        } // 검사 끝
        private static IEnumerator WaitForTitleStatus() // 실제 타이틀과 파편 표시 대기
        { // 대기 시작
            var deadline = Time.realtimeSinceStartup + 20f; // 화면 생성 대기 제한
            while (Time.realtimeSinceStartup < deadline) // 지정 시간 동안 상태 검사
            { // 반복 시작
                if (AppRoot.Instance != null && AppRoot.Instance.Flow.Phase == GamePhase.Title && GameObject.Find("EarnedShardsStatus") != null) // 타이틀과 실제 표시 검사
                { // 도착 시작
                    yield return null; // 화면 초기화 대기
                    yield break; // 정상 표시 대기 종료
                } // 도착 끝
                yield return null; // 다음 프레임 대기
            } // 반복 끝
            Assert.Fail("타이틀 파편 상태를 찾지 못했습니다."); // 타이틀 표시 누락 안내
        } // 대기 끝
    } // 클래스 끝
} // 이름 공간 끝
