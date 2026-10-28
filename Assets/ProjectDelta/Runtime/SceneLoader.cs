using System; // 로드 예외 처리
using System.Collections; // 비동기 순서 도구
using UnityEngine; // Unity 실행 도구
using UnityEngine.SceneManagement; // 실제 씬 로드
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class SceneLoader : MonoBehaviour // 씬 로드 담당
    { // 클래스 시작
        public bool Succeeded // 마지막 로드 성공 여부
        { // 속성 시작
            get; // 성공 상태 읽기
            private set; // 내부 결과 변경
        } // 속성 끝
        public IEnumerator LoadScene(string sceneName) // 지정 씬 비동기 로드
        { // 로드 시작
            Succeeded = false; // 이전 성공 결과 초기화
            if (!Application.CanStreamedLevelBeLoaded(sceneName)) // 빌드 씬 등록 검사
            { // 누락 조건 시작
                Debug.LogError("Scene is not registered: " + sceneName); // 누락 씬 오류 기록
                yield break; // 이전 화면 유지
            } // 누락 조건 끝
            AsyncOperation operation; // 비동기 로드 상태
            try // 즉시 로드 오류 검사
            { // 예외 검사 시작
                operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); // 단일 씬 전환 요청
            } // 예외 검사 끝
            catch (Exception exception) // 로드 요청 실패 처리
            { // 실패 처리 시작
                Debug.LogException(exception); // 실제 실패 원인 기록
                yield break; // 복구 가능한 상태 전달
            } // 실패 처리 끝
            if (operation == null) // 로드 작업 생성 검사
            { // 누락 조건 시작
                Debug.LogError("Scene load did not start: " + sceneName); // 시작 실패 기록
                yield break; // 화면 전환 중단
            } // 누락 조건 끝
            yield return operation; // 실제 씬 활성화 대기
            Succeeded = SceneManager.GetActiveScene().name == sceneName; // 도착 씬 확인
        } // 로드 끝
    } // 클래스 끝
} // 이름 공간 끝
