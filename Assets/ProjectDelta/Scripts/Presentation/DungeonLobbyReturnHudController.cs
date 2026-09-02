using ProjectDelta.Application;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 123일차: "던전 클리어 후 돌아오는 버튼" - 5층 마왕(124일차)이 아직 없어서, 지금은
    // 화면 오른쪽 위에 항상 떠 있는 일반적인 "로비로" 나가기 버튼으로 둔다. 124일차에서
    // 마왕 처치 시 ApplicationFlow.ReturnToLobby()를 자동으로 부르게 연결하면 이 버튼은
    // 그대로 "중간에 포기하고 나가기" 용도로 계속 쓸 수 있다.
    public sealed class DungeonLobbyReturnHudController : MonoBehaviour
    {
        private const float ButtonWidth = 110f;
        private const float ButtonHeight = 32f;

        private GUIStyle buttonStyle;

        private void OnGUI()
        {
            if (buttonStyle == null)
            {
                buttonStyle =
                    new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 14
                    };
            }

            Rect buttonRect =
                new Rect(
                    Screen.width - ButtonWidth - 12f,
                    12f,
                    ButtonWidth,
                    ButtonHeight);

            if (GUI.Button(
                    buttonRect,
                    "로비로",
                    buttonStyle))
            {
                ApplicationFlow.Current?.ReturnToLobby();
            }
        }
    }
}
