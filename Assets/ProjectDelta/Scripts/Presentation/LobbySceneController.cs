using ProjectDelta.Application;
using ProjectDelta.Data;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 123일차: 타이틀(새 게임)과 던전 사이의 로비 화면 - TitleSceneController(24일차)와
    // 완전히 같은 임시 OnGUI 패턴을 따른다. "던전 입장" 버튼이 실제로 새 런을 시작한다
    // (ApplicationFlow.StartNewGame - 이 메서드 자체는 그대로 두고 호출 지점만 옮겼다).
    // TODO: 실제 로비 UI(아트/애니메이션, 영구 강화 상점 등)는 이후 별도 일차에서 정식으로 만든다.
    public sealed class LobbySceneController : MonoBehaviour
    {
        private GUIStyle titleStyle;
        private GUIStyle buttonStyle;

        // 125일차: 영구 성장 재화(기억의 조각) 표시용 스타일.
        private GUIStyle shardStyle;

        // 125일차: 로비에 머무는 동안 값이 바뀌지 않으므로(전투는 던전에서만 일어난다)
        // 씬 진입 시 한 번만 읽는다 - 매 프레임 저장 파일을 다시 읽지 않는다.
        private int memoryShards;

        private void OnEnable()
        {
            ProfileData profile =
                ApplicationFlow.Current?.ReadOrCreateProfile();

            memoryShards =
                profile != null
                    ? profile.PermanentGrowth.MemoryShards
                    : 0;
        }

        private void OnGUI()
        {
            EnsureStyles();

            float centerX =
                Screen.width / 2f;

            GUI.Label(
                new Rect(
                    centerX - 200f,
                    Screen.height * 0.25f,
                    400f,
                    60f),
                "로비",
                titleStyle);

            GUI.Label(
                new Rect(
                    centerX - 200f,
                    Screen.height * 0.25f + 56f,
                    400f,
                    28f),
                $"기억의 조각 {memoryShards}",
                shardStyle);

            float buttonWidth = 220f;
            float buttonHeight = 50f;
            float spacing = buttonHeight + 16f;
            float buttonX = centerX - (buttonWidth / 2f);
            float y = Screen.height * 0.45f;

            if (GUI.Button(
                    new Rect(buttonX, y, buttonWidth, buttonHeight),
                    "던전 입장",
                    buttonStyle))
            {
                ApplicationFlow.Current?.StartNewGame();
            }

            y += spacing;

            if (GUI.Button(
                    new Rect(buttonX, y, buttonWidth, buttonHeight),
                    "타이틀로",
                    buttonStyle))
            {
                ApplicationFlow.Current?.EnterTitle();
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle == null)
            {
                titleStyle =
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 36,
                        fontStyle = FontStyle.Bold
                    };

                titleStyle.normal.textColor =
                    Color.white;
            }

            if (buttonStyle == null)
            {
                buttonStyle =
                    new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 20
                    };
            }

            if (shardStyle == null)
            {
                shardStyle =
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 18
                    };

                shardStyle.normal.textColor =
                    new Color(0.86f, 0.72f, 0.3f);
            }
        }
    }
}
