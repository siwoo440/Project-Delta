using ProjectDelta.Application; // ApplicationFlow.Current 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 24일차: 새 게임 흐름을 눈으로 확인하기 위한 임시 타이틀 화면. Canvas/Button 없이
    // 기존 방/미니맵 프롬프트와 같은 OnGUI 방식으로 만들어서 최소한으로 유지한다.
    // TODO: 실제 타이틀 UI(아트/애니메이션)는 이후 별도 일차에서 정식으로 만든다.
    public sealed class TitleSceneController : MonoBehaviour // 타이틀 화면 임시 버튼 제어
    {
        private GUIStyle titleStyle; // 제목 글자 스타일
        private GUIStyle buttonStyle; // 버튼 글자 스타일

        private void OnGUI() // 타이틀 임시 UI 표시
        {
            EnsureStyles(); // 스타일 준비

            float centerX = Screen.width / 2f; // 화면 가로 중앙 좌표
            GUI.Label(new Rect(centerX - 200f, Screen.height * 0.25f, 400f, 60f), "Project Delta", titleStyle); // 제목 표시

            float buttonWidth = 220f; // 버튼 가로 크기
            float buttonHeight = 50f; // 버튼 세로 크기
            float spacing = buttonHeight + 16f; // 버튼 사이 간격 포함 세로 이동량
            float buttonX = centerX - (buttonWidth / 2f); // 버튼 가로 중앙 정렬 좌표
            float y = Screen.height * 0.4f; // 첫 버튼 세로 시작 좌표

            bool hasSavedRun = ApplicationFlow.Current != null && ApplicationFlow.Current.HasSavedRun(); // 26일차: 저장된 런 존재 여부 확인

            if (hasSavedRun) // 저장된 런이 있을 때만 이어하기 버튼 표시
            {
                if (GUI.Button(new Rect(buttonX, y, buttonWidth, buttonHeight), "이어하기", buttonStyle)) // 이어하기 버튼
                {
                    ApplicationFlow.Current?.ContinueGame(); // 저장된 런 복원 후 로딩 화면을 거쳐 던전으로 이동
                }

                y += spacing; // 다음 버튼 위치로 이동
            }

            if (GUI.Button(new Rect(buttonX, y, buttonWidth, buttonHeight), "새 게임", buttonStyle)) // 새 게임 버튼
            {
                ApplicationFlow.Current?.StartNewGame(); // 새 런 시작 후 로딩 화면을 거쳐 던전으로 이동
            }

            y += spacing; // 다음 버튼 위치로 이동

            if (GUI.Button(new Rect(buttonX, y, buttonWidth, buttonHeight), "설정", buttonStyle)) // 설정 버튼
            {
                ApplicationFlow.Current?.OpenSettings(); // 설정 화면으로 이동
            }

            y += spacing; // 다음 버튼 위치로 이동

            if (GUI.Button(new Rect(buttonX, y, buttonWidth, buttonHeight), "종료", buttonStyle)) // 종료 버튼
            {
                QuitGame(); // 게임 종료 처리
            }
        }

        private void EnsureStyles() // GUI 스타일 최초 1회 생성
        {
            if (titleStyle == null) // 제목 스타일 존재 확인
            {
                titleStyle = new GUIStyle(GUI.skin.label); // 기본 라벨 스타일 복제
                titleStyle.alignment = TextAnchor.MiddleCenter; // 가운데 정렬 적용
                titleStyle.fontSize = 36; // 제목 글자 크기 적용
                titleStyle.fontStyle = FontStyle.Bold; // 굵게 적용
                titleStyle.normal.textColor = Color.white; // 흰색 적용
            }

            if (buttonStyle == null) // 버튼 스타일 존재 확인
            {
                buttonStyle = new GUIStyle(GUI.skin.button); // 기본 버튼 스타일 복제
                buttonStyle.fontSize = 20; // 버튼 글자 크기 적용
            }
        }

        private static void QuitGame() // 에디터/빌드 환경에 맞는 종료 처리
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // 에디터 플레이 모드 종료
#else
            Application.Quit(); // 빌드 실행 파일 종료
#endif
        }
    }
}
