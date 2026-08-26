using ProjectDelta.Application;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 86일차: 전투 중에만 표시되는 1×·2× 속도 전환 버튼을 런타임에 자동 생성한다.
    [DisallowMultipleComponent]
    public sealed class BattleSpeedRuntimeHud : MonoBehaviour
    {
        private const float ButtonWidth = 112f;
        private const float ButtonHeight = 48f;
        private const float ButtonMargin = 24f;

        private ExplorationMonsterEncounterController encounterController;
        private GameObject buttonObject;
        private Button speedButton;
        private Text speedText;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            BattleSpeedState.ResetToNormal();
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimeHud()
        {
            BattleSpeedRuntimeHud existing =
                FindFirstObjectByType<BattleSpeedRuntimeHud>();

            if (existing != null)
            {
                return;
            }

            GameObject hostObject =
                new GameObject(
                    nameof(BattleSpeedRuntimeHud));

            DontDestroyOnLoad(
                hostObject);

            hostObject.AddComponent<BattleSpeedRuntimeHud>();
        }

        private void Awake()
        {
            CreateCanvasUi();
            RefreshSpeedLabel();
            SetVisible(
                false);
        }

        private void OnDestroy()
        {
            if (speedButton != null)
            {
                speedButton.onClick.RemoveListener(
                    OnSpeedButtonClicked);
            }
        }

        private void Update()
        {
            ResolveEncounterController();

            bool shouldShow =
                encounterController != null
                && encounterController.HasBattle;

            SetVisible(
                shouldShow);
        }

        private void ResolveEncounterController()
        {
            if (encounterController != null)
            {
                return;
            }

            encounterController =
                FindFirstObjectByType<ExplorationMonsterEncounterController>();
        }

        private void OnSpeedButtonClicked()
        {
            BattleSpeedState.Toggle();
            RefreshSpeedLabel();
        }

        private void CreateCanvasUi()
        {
            GameObject canvasObject =
                new GameObject(
                    "BattleSpeedCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

            canvasObject.transform.SetParent(
                transform,
                false);

            Canvas canvas =
                canvasObject.GetComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            // 85일차 F1 로그(10000)보다 아래에 두고 일반 전투 HUD보다 위에 표시한다.
            canvas.sortingOrder =
                9000;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(
                    1920f,
                    1080f);

            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            scaler.matchWidthOrHeight =
                0.5f;

            buttonObject =
                new GameObject(
                    "BattleSpeedButton",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));

            buttonObject.transform.SetParent(
                canvasObject.transform,
                false);

            RectTransform buttonRect =
                buttonObject.GetComponent<RectTransform>();

            buttonRect.anchorMin =
                new Vector2(
                    1f,
                    0f);

            buttonRect.anchorMax =
                new Vector2(
                    1f,
                    0f);

            buttonRect.pivot =
                new Vector2(
                    1f,
                    0f);

            buttonRect.anchoredPosition =
                new Vector2(
                    -ButtonMargin,
                    ButtonMargin);

            buttonRect.sizeDelta =
                new Vector2(
                    ButtonWidth,
                    ButtonHeight);

            Image background =
                buttonObject.GetComponent<Image>();

            background.color =
                new Color(
                    0.08f,
                    0.09f,
                    0.12f,
                    0.92f);

            speedButton =
                buttonObject.GetComponent<Button>();

            speedButton.targetGraphic =
                background;

            speedButton.onClick.AddListener(
                OnSpeedButtonClicked);

            GameObject textObject =
                new GameObject(
                    "SpeedText",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));

            textObject.transform.SetParent(
                buttonObject.transform,
                false);

            RectTransform textRect =
                textObject.GetComponent<RectTransform>();

            textRect.anchorMin =
                Vector2.zero;

            textRect.anchorMax =
                Vector2.one;

            textRect.offsetMin =
                Vector2.zero;

            textRect.offsetMax =
                Vector2.zero;

            speedText =
                textObject.GetComponent<Text>();

            speedText.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            speedText.fontSize =
                22;

            speedText.fontStyle =
                FontStyle.Bold;

            speedText.alignment =
                TextAnchor.MiddleCenter;

            speedText.color =
                Color.white;

            speedText.raycastTarget =
                false;
        }

        private void RefreshSpeedLabel()
        {
            if (speedText == null)
            {
                return;
            }

            speedText.text =
                BattleSpeedState.DisplayLabel;
        }

        private void SetVisible(
            bool visible)
        {
            if (buttonObject == null)
            {
                return;
            }

            if (buttonObject.activeSelf != visible)
            {
                buttonObject.SetActive(
                    visible);
            }
        }
    }
}
