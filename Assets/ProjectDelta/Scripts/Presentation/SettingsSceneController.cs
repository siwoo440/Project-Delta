using ProjectDelta.Application; // ApplicationFlow.Current 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 24일차: 설정 화면 자체는 아직 없어서(항목 없음), 타이틀로 돌아가는 경로만 확인하는 임시 화면.
    // TODO: 실제 설정 항목(그래픽/사운드/키 바인딩 등)은 해당 시스템이 생기는 일차에서 채운다.
    public sealed class SettingsSceneController : MonoBehaviour // 설정 화면 임시 버튼 제어
    {
        private GUIStyle labelStyle; // 안내 글자 스타일
        private GUIStyle buttonStyle; // 버튼 글자 스타일

        private void OnGUI() // 설정 임시 UI 표시
        {
            EnsureStyles(); // 스타일 준비

            float centerX = Screen.width / 2f; // 화면 가로 중앙 좌표
            GUI.Label(new Rect(centerX - 200f, Screen.height * 0.35f, 400f, 60f), "설정 (준비 중)", labelStyle); // 안내 문구 표시

            float buttonWidth = 220f; // 버튼 가로 크기
            float buttonHeight = 50f; // 버튼 세로 크기
            Rect backRect = new Rect(centerX - (buttonWidth / 2f), Screen.height * 0.5f, buttonWidth, buttonHeight); // 뒤로가기 버튼 영역

            if (GUI.Button(backRect, "뒤로가기", buttonStyle)) // 뒤로가기 버튼
            {
                ApplicationFlow.Current?.ReturnToTitle(); // 타이틀 화면으로 이동
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
