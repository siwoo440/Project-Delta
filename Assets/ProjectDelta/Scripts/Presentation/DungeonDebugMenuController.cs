using ProjectDelta.Application; // ApplicationFlow.Current 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 24일차: DungeonScene을 항상 직접 열어서 테스트하던 것에서 벗어나, 타이틀 → 새 게임 →
    // 던전 → (이 버튼) → 타이틀 순환 경로를 실제로 오갈 수 있게 하는 임시 디버그 버튼.
    // TODO: 정식 일시정지 메뉴(계속하기/설정/타이틀로)가 생기면 이 버튼은 그 메뉴로 흡수한다.
    public sealed class DungeonDebugMenuController : MonoBehaviour // 던전 임시 나가기 버튼 제어
    {
        private GUIStyle buttonStyle; // 버튼 글자 스타일

        private void OnGUI() // 좌측 상단 임시 나가기 버튼 표시
        {
            if (buttonStyle == null) // 버튼 스타일 존재 확인
            {
                buttonStyle = new GUIStyle(GUI.skin.button); // 기본 버튼 스타일 복제
                buttonStyle.fontSize = 14; // 버튼 글자 크기 적용
            }

            if (GUI.Button(new Rect(16f, 16f, 140f, 32f), "타이틀로 (임시)", buttonStyle)) // 좌측 상단 임시 나가기 버튼
            {
                ApplicationFlow.Current?.ReturnToTitle(); // 현재 런 포기 후 타이틀 화면으로 이동
            }
        }
    }
}
