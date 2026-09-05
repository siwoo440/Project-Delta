using ProjectDelta.Application; // ApplicationFlow.Current 사용
using ProjectDelta.Data; // SettingsData 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // Canvas UI 기능 사용

// 137일차: 키 리매핑에 쓰는 액션 표시 이름 목록.
using RebindableAction = System.ValueTuple<string, string>;

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 24일차: 항목 없는 임시 화면 → 136~137일차에 UI 배율·자막·키 리매핑을 OnGUI로 채움 →
    // 139일차에 기획서 8.2절 "화면별 정식 UI" 전환의 일환으로 런타임 Canvas 방식으로
    // 옮겼다. 판정/저장 로직(ApplicationFlow 호출)은 그대로 두고 그리는 방식만 바꿨다.
    public sealed class SettingsSceneController : MonoBehaviour // 설정 화면 버튼 제어
    {
        // 137일차: 기획서 8.1절 "키보드/마우스/게임패드 리매핑" - 탐험 맵의 이동·상호작용
        // 5개 액션만 우선 다룬다(장치를 가리지 않고 다음 입력을 그대로 새 바인딩으로 받음).
        private static readonly RebindableAction[] RebindableActions =
        {
            ("MoveForward", "전진"),
            ("MoveBackward", "후진"),
            ("MoveLeft", "좌측 이동"),
            ("MoveRight", "우측 이동"),
            ("Interact", "상호작용")
        };

        private static readonly Color SelectedOptionColor =
            new Color(0.30f, 0.45f, 0.65f, 1f);

        private static readonly Color NormalOptionColor =
            new Color(0.2f, 0.2f, 0.26f, 1f);

        private SettingsData settings; // 현재 설정 값

        private readonly Image[] uiScaleOptionImages = new Image[3]; // 소/보통/대 버튼 배경(선택 강조용)
        private Text subtitleToggleText; // 자막 토글 버튼 라벨
        private Text keyRemapToggleText; // 키 설정 토글 버튼 라벨
        private GameObject keyRemapPanel; // 키 설정 패널 루트
        private readonly Text[] rebindButtonTexts = new Text[RebindableActions.Length]; // 재설정 버튼 라벨들

        private string rebindingActionName; // 현재 입력을 기다리는 중인 액션(없으면 null)

        private void Awake()
        {
            RuntimeUiFactory.EnsureEventSystem();

            settings = // 저장된 설정 읽기(없으면 기본값)
                ApplicationFlow.Current?.ReadOrCreateSettings()
                ?? new SettingsData();

            Transform canvasTransform =
                RuntimeUiFactory.BuildScreenCanvas(
                    transform,
                    "SettingsCanvas",
                    "설정");

            BuildUiScaleRow(
                canvasTransform);

            BuildSubtitleToggle(
                canvasTransform);

            BuildKeyRemapToggle(
                canvasTransform);

            BuildBackButton(
                canvasTransform);

            BuildKeyRemapPanel(
                canvasTransform);

            RefreshUiScaleHighlight();
            RefreshKeyRemapLabels();
        }

        private void BuildUiScaleRow(
            Transform parent)
        {
            RectTransform labelRect =
                RuntimeUiFactory.CreateUiObject(
                    "UiScaleLabel",
                    parent);

            labelRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            labelRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            labelRect.pivot =
                new Vector2(0.5f, 0.5f);

            labelRect.anchoredPosition =
                new Vector2(0f, 200f);

            labelRect.sizeDelta =
                new Vector2(400f, 28f);

            Text labelComponent =
                labelRect.gameObject.AddComponent<Text>();

            RuntimeUiFactory.ConfigureText(
                labelComponent,
                "UI 배율",
                20,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);

            (string label, float value)[] options =
            {
                ("소", UiScaleSettings.Small),
                ("보통", UiScaleSettings.Normal),
                ("대", UiScaleSettings.Large)
            };

            float[] xOffsets =
            {
                -116f, 0f, 116f
            };

            for (int i = 0; i < options.Length; i++)
            {
                float capturedValue =
                    options[i].value;

                Button button =
                    RuntimeUiFactory.CreateCenteredButton(
                        parent,
                        $"UiScaleOption_{options[i].label}",
                        new Vector2(xOffsets[i], 150f),
                        new Vector2(100f, 44f),
                        options[i].label,
                        16,
                        () => SelectUiScale(capturedValue),
                        out _);

                uiScaleOptionImages[i] =
                    button.GetComponent<Image>();
            }
        }

        private void BuildSubtitleToggle(
            Transform parent)
        {
            RuntimeUiFactory.CreateCenteredButton(
                parent,
                "SubtitleToggleButton",
                new Vector2(0f, 70f),
                new Vector2(240f, 50f),
                string.Empty,
                18,
                ToggleSubtitles,
                out subtitleToggleText);
        }

        private void BuildKeyRemapToggle(
            Transform parent)
        {
            RuntimeUiFactory.CreateCenteredButton(
                parent,
                "KeyRemapToggleButton",
                new Vector2(0f, 0f),
                new Vector2(240f, 50f),
                string.Empty,
                18,
                ToggleKeyRemapPanel,
                out keyRemapToggleText);
        }

        private void BuildBackButton(
            Transform parent)
        {
            RuntimeUiFactory.CreateCenteredButton(
                parent,
                "BackButton",
                new Vector2(0f, -70f),
                new Vector2(240f, 50f),
                "뒤로가기",
                18,
                () => ApplicationFlow.Current?.ReturnToTitle(),
                out _);
        }

        // 137일차: 액션 5개를 나열해 각각 현재 바인딩과 재설정 버튼을 보여준다.
        private void BuildKeyRemapPanel(
            Transform parent)
        {
            RectTransform panelRect =
                RuntimeUiFactory.CreateUiObject(
                    "KeyRemapPanel",
                    parent);

            keyRemapPanel =
                panelRect.gameObject;

            panelRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            panelRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            panelRect.pivot =
                new Vector2(0.5f, 1f);

            panelRect.anchoredPosition =
                new Vector2(0f, -110f);

            panelRect.sizeDelta =
                new Vector2(420f, RebindableActions.Length * 48f);

            Image panelImage =
                panelRect.gameObject.AddComponent<Image>();

            panelImage.color =
                new Color(0.1f, 0.1f, 0.14f, 0.85f);

            for (int i = 0; i < RebindableActions.Length; i++)
            {
                (string actionName, string displayName) =
                    RebindableActions[i];

                float rowY =
                    -(i * 48f) - 4f;

                RectTransform labelRect =
                    RuntimeUiFactory.CreateUiObject(
                        $"Label_{actionName}",
                        panelRect);

                labelRect.anchorMin =
                    new Vector2(0f, 1f);

                labelRect.anchorMax =
                    new Vector2(0f, 1f);

                labelRect.pivot =
                    new Vector2(0f, 1f);

                labelRect.anchoredPosition =
                    new Vector2(16f, rowY);

                labelRect.sizeDelta =
                    new Vector2(220f, 40f);

                Text labelText =
                    labelRect.gameObject.AddComponent<Text>();

                RuntimeUiFactory.ConfigureText(
                    labelText,
                    displayName,
                    16,
                    FontStyle.Normal,
                    TextAnchor.MiddleLeft);

                string capturedActionName =
                    actionName;

                RectTransform rebindButtonRect =
                    RuntimeUiFactory.CreateUiObject(
                        $"Rebind_{actionName}",
                        panelRect);

                rebindButtonRect.anchorMin =
                    new Vector2(1f, 1f);

                rebindButtonRect.anchorMax =
                    new Vector2(1f, 1f);

                rebindButtonRect.pivot =
                    new Vector2(1f, 1f);

                rebindButtonRect.anchoredPosition =
                    new Vector2(-16f, rowY);

                rebindButtonRect.sizeDelta =
                    new Vector2(160f, 40f);

                Image rebindButtonImage =
                    rebindButtonRect.gameObject.AddComponent<Image>();

                rebindButtonImage.color =
                    NormalOptionColor;

                Button rebindButton =
                    rebindButtonRect.gameObject.AddComponent<Button>();

                rebindButton.targetGraphic =
                    rebindButtonImage;

                rebindButton.onClick.AddListener(
                    () => BeginRebind(
                        capturedActionName));

                RectTransform rebindLabelRect =
                    RuntimeUiFactory.CreateStretchedRect(
                        "Label",
                        rebindButtonRect);

                Text rebindLabelText =
                    rebindLabelRect.gameObject.AddComponent<Text>();

                RuntimeUiFactory.ConfigureText(
                    rebindLabelText,
                    string.Empty,
                    14,
                    FontStyle.Normal,
                    TextAnchor.MiddleCenter);

                rebindLabelText.raycastTarget =
                    false;

                rebindButtonTexts[i] =
                    rebindLabelText;
            }

            keyRemapPanel.SetActive(
                false); // 기본은 접힌 상태
        }

        private void SelectUiScale(
            float scaleValue)
        {
            settings.Ui.UiScale =
                scaleValue;

            SaveSettings();
            RefreshUiScaleHighlight();
        }

        private void RefreshUiScaleHighlight()
        {
            float[] values =
            {
                UiScaleSettings.Small,
                UiScaleSettings.Normal,
                UiScaleSettings.Large
            };

            for (int i = 0; i < uiScaleOptionImages.Length; i++)
            {
                bool isCurrent =
                    Mathf.Abs(
                        settings.Ui.UiScale
                        - values[i])
                    < 0.01f;

                if (uiScaleOptionImages[i] != null)
                {
                    uiScaleOptionImages[i].color =
                        isCurrent
                            ? SelectedOptionColor
                            : NormalOptionColor;
                }
            }
        }

        private void ToggleSubtitles()
        {
            settings.Accessibility.SfxSubtitles =
                !settings.Accessibility.SfxSubtitles;

            SaveSettings(); // 즉시 저장(기획서 9.1 - 설정은 변경 즉시 저장)
            RefreshSubtitleLabel();
        }

        private void RefreshSubtitleLabel()
        {
            if (subtitleToggleText != null)
            {
                subtitleToggleText.text =
                    settings.Accessibility.SfxSubtitles
                        ? "자막 표시: 켜짐"
                        : "자막 표시: 꺼짐";
            }
        }

        private void ToggleKeyRemapPanel()
        {
            bool nextState =
                !keyRemapPanel.activeSelf;

            keyRemapPanel.SetActive(
                nextState);

            RefreshKeyRemapToggleLabel();
        }

        private void RefreshKeyRemapToggleLabel()
        {
            if (keyRemapToggleText != null)
            {
                keyRemapToggleText.text =
                    keyRemapPanel.activeSelf
                        ? "키 설정 닫기"
                        : "키 설정";
            }
        }

        private void BeginRebind(
            string actionName)
        {
            if (rebindingActionName != null)
            {
                return; // 이미 다른 액션이 입력을 기다리는 중이면 무시
            }

            rebindingActionName =
                actionName;

            RefreshKeyRemapLabels();

            ApplicationFlow.Current?.StartKeyRebind(
                InputMapNames.Exploration,
                actionName,
                onCompleted: overridePath =>
                {
                    rebindingActionName =
                        null;

                    ApplicationFlow.Current?.SaveKeyBinding(
                        InputMapNames.Exploration,
                        actionName,
                        overridePath);

                    RefreshKeyRemapLabels();
                },
                onCanceled: () =>
                {
                    rebindingActionName =
                        null;

                    RefreshKeyRemapLabels();
                });
        }

        private void RefreshKeyRemapLabels()
        {
            RefreshSubtitleLabel();
            RefreshKeyRemapToggleLabel();

            for (int i = 0; i < RebindableActions.Length; i++)
            {
                string actionName =
                    RebindableActions[i].Item1;

                bool isWaitingForInput =
                    rebindingActionName == actionName;

                string bindingLabel =
                    isWaitingForInput
                        ? "입력 대기 중..."
                        : ApplicationFlow.Current?.GetKeyBindingDisplayString(
                              InputMapNames.Exploration,
                              actionName)
                          ?? "-";

                if (rebindButtonTexts[i] != null)
                {
                    rebindButtonTexts[i].text =
                        bindingLabel;
                }
            }
        }

        private void SaveSettings()
        {
            ApplicationFlow.Current?.SaveSettings( // 설정 파일에 즉시 저장
                settings);

            UiScaleSettings.Refresh(); // 이 화면에서 바뀐 배율을 곧바로 반영
        }
    }
}
