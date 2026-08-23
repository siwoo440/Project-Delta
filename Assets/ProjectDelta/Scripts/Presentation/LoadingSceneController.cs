using ProjectDelta.Application; // ApplicationFlow.Current 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 24일차: 실제 로딩 진행률 표시는 아직 없다. 지금은 "새 게임" → 로딩 화면 → 던전"이라는
    // 씬 전환 경로 자체를 눈으로 확인하기 위한 자리표시자라, 버튼을 눌러야 다음 씬으로 넘어간다.
    // TODO: 실제 로딩(Addressables 다운로드 진행률 등)이 필요해지는 일차에 자동 진행으로 바꾼다.
    public sealed class LoadingSceneController : MonoBehaviour // 로딩 화면 임시 진행 제어
    {
        private GUIStyle labelStyle; // 안내 글자 스타일
        private GUIStyle buttonStyle; // 버튼 글자 스타일

        private void OnGUI() // 로딩 임시 UI 표시
        {
            EnsureStyles(); // 스타일 준비

            float centerX = Screen.width / 2f; // 화면 가로 중앙 좌표
            GUI.Label(new Rect(centerX - 200f, Screen.height * 0.4f, 400f, 60f), "로딩 중...", labelStyle); // 로딩 안내 표시

            float buttonWidth = 220f; // 버튼 가로 크기
            float buttonHeight = 50f; // 버튼 세로 크기
            Rect continueRect = new Rect(centerX - (buttonWidth / 2f), Screen.height * 0.55f, buttonWidth, buttonHeight); // 계속하기 버튼 영역

            if (GUI.Button(continueRect, "계속 (임시)", buttonStyle)) // 계속하기 버튼 (자동 진행 전까지의 임시 확인 수단)
            {
                ApplicationFlow.Current?.ProceedFromLoadingScreen(); // 예정된 목적지 씬으로 이동
            }
        }

        private void EnsureStyles() // GUI 스타일 최초 1회 생성
        {
            if (labelStyle == null) // 안내 스타일 존재 확인
            {
                labelStyle = new GUIStyle(GUI.skin.label); // 기본 라벨 스타일 복제
                labelStyle.alignment = TextAnchor.MiddleCenter; // 가운데 정렬 적용
                labelStyle.fontSize = 28; // 안내 글자 크기 적용
                labelStyle.normal.textColor = Color.white; // 흰색 적용
            }

            if (buttonStyle == null) // 버튼 스타일 존재 확인
            {
                buttonStyle = new GUIStyle(GUI.skin.button); // 기본 버튼 스타일 복제
                buttonStyle.fontSize = 20; // 버튼 글자 크기 적용
            }
        }
    }
}
