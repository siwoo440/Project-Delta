using System.Collections;
using ProjectDelta.Application;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    public sealed class AutoSaveToastController : MonoBehaviour
    {
        private const float VisibleSeconds = 1.25f;
        private const float FadeSeconds = 0.35f;

        private static AutoSaveToastController instance;

        private CanvasGroup toastGroup;
        private Coroutine hideRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            GameObject root =
                new GameObject(
                    "AutoSaveToastController");

            instance =
                root.AddComponent<AutoSaveToastController>(); // 런타임 저장 알림 생성

            DontDestroyOnLoad(
                root);
        }

        private void Awake()
        {
            if (instance != null
                && instance != this)
            {
                Destroy(
                    gameObject);
                return;
            }

            instance =
                this;

            CreateUi();
        }

        private void OnEnable()
        {
            AutoSaveNotification.Saved +=
                Show;
        }

        private void OnDisable()
        {
            AutoSaveNotification.Saved -=
                Show;
        }

        private void CreateUi()
        {
            Canvas canvas =
                gameObject.AddComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            canvas.sortingOrder =
                32760;

            CanvasScaler scaler =
                gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(
                    1920f,
                    1080f);

            scaler.matchWidthOrHeight =
                0.5f;

            GameObject panelObject =
                new GameObject(
                    "AutoSaveToastPanel",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(CanvasGroup));

            panelObject.transform.SetParent(
                transform,
                false);

            RectTransform panelRect =
                panelObject.GetComponent<RectTransform>();

            panelRect.anchorMin =
                new Vector2(
                    0f,
                    1f);

            panelRect.anchorMax =
                new Vector2(
                    0f,
                    1f);

            panelRect.pivot =
                new Vector2(
                    0f,
                    1f);

            panelRect.anchoredPosition =
                new Vector2(
                    24f,
                    -24f);

            panelRect.sizeDelta =
                new Vector2(
                    360f,
                    56f);

            Image background =
                panelObject.GetComponent<Image>();

            background.color =
                new Color(
                    0.05f,
                    0.06f,
                    0.08f,
                    0.88f);

            background.raycastTarget =
                false;

            toastGroup =
                panelObject.GetComponent<CanvasGroup>();

            toastGroup.alpha =
                0f;

            toastGroup.blocksRaycasts =
                false;

            toastGroup.interactable =
                false;

            GameObject textObject =
                new GameObject(
                    "Message",
                    typeof(RectTransform),
                    typeof(Text));

            textObject.transform.SetParent(
                panelObject.transform,
                false);

            RectTransform textRect =
                textObject.GetComponent<RectTransform>();

            textRect.anchorMin =
                Vector2.zero;

            textRect.anchorMax =
                Vector2.one;

            textRect.offsetMin =
                new Vector2(
                    18f,
                    6f);

            textRect.offsetMax =
                new Vector2(
                    -18f,
                    -6f);

            Text message =
                textObject.GetComponent<Text>();

            message.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            message.text =
                "자동 저장 되었습니다.";

            message.fontSize =
                22;

            message.fontStyle =
                FontStyle.Bold;

            message.alignment =
                TextAnchor.MiddleLeft;

            message.color =
                Color.white;

            message.raycastTarget =
                false;
        }

        private void Show()
        {
            if (toastGroup == null)
            {
                return;
            }

            if (hideRoutine != null)
            {
                StopCoroutine(
                    hideRoutine);
            }

            hideRoutine =
                StartCoroutine(
                    ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            toastGroup.alpha =
                1f;

            yield return new WaitForSecondsRealtime(
                VisibleSeconds);

            float elapsed =
                0f;

            while (elapsed < FadeSeconds)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                toastGroup.alpha =
                    1f - Mathf.Clamp01(
                        elapsed / FadeSeconds);

                yield return null;
            }

            toastGroup.alpha =
                0f;

            hideRoutine =
                null;
        }
    }
}
