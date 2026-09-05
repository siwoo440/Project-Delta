using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 139일차: CG·도전과제 갤러리(133~134일차)에서 반복되던 런타임 Canvas UI 생성
    // 보일러플레이트를 공용으로 뺐다 - 이번 일차부터 OnGUI를 Canvas로 옮기는 화면
    // (타이틀/로비/설정)이 이걸 쓴다. 기존 갤러리 두 화면은 이미 검증된 상태라 굳이
    // 건드리지 않는다.
    public static class RuntimeUiFactory
    {
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject =
                new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));

            Object.DontDestroyOnLoad(
                eventSystemObject);
        }

        public static RectTransform CreateUiObject(
            string name,
            Transform parent)
        {
            GameObject go =
                new GameObject(
                    name,
                    typeof(RectTransform));

            go.transform.SetParent(
                parent,
                false);

            return go.GetComponent<RectTransform>();
        }

        public static RectTransform CreateStretchedRect(
            string name,
            Transform parent)
        {
            RectTransform rect =
                CreateUiObject(
                    name,
                    parent);

            rect.anchorMin =
                Vector2.zero;

            rect.anchorMax =
                Vector2.one;

            rect.offsetMin =
                Vector2.zero;

            rect.offsetMax =
                Vector2.zero;

            return rect;
        }

        public static void ConfigureText(
            Text text,
            string content,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment)
        {
            text.text =
                content;

            text.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            text.fontSize =
                fontSize;

            text.fontStyle =
                fontStyle;

            text.alignment =
                alignment;

            text.color =
                Color.white;
        }

        // 화면 최상단 Canvas + CanvasScaler(+UI 배율 반영) + 배경 + 제목까지 한 번에 만든다.
        public static Transform BuildScreenCanvas(
            Transform parent,
            string canvasObjectName,
            string title)
        {
            GameObject canvasObject =
                new GameObject(
                    canvasObjectName,
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

            canvasObject.transform.SetParent(
                parent,
                false);

            Canvas canvas =
                canvasObject.GetComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            UiScaleSettings.Refresh();

            UiScaleSettings.ApplyToCanvasScaler(
                scaler,
                new Vector2(1920f, 1080f));

            RectTransform background =
                CreateStretchedRect(
                    "Background",
                    canvasObject.transform);

            Image backgroundImage =
                background.gameObject.AddComponent<Image>();

            backgroundImage.color =
                new Color(0.05f, 0.05f, 0.08f, 1f);

            backgroundImage.raycastTarget =
                false;

            if (!string.IsNullOrEmpty(title))
            {
                RectTransform titleRect =
                    CreateUiObject(
                        "Title",
                        canvasObject.transform);

                titleRect.anchorMin =
                    new Vector2(0.5f, 1f);

                titleRect.anchorMax =
                    new Vector2(0.5f, 1f);

                titleRect.pivot =
                    new Vector2(0.5f, 1f);

                titleRect.anchoredPosition =
                    new Vector2(0f, -24f);

                titleRect.sizeDelta =
                    new Vector2(600f, 60f);

                Text titleText =
                    titleRect.gameObject.AddComponent<Text>();

                ConfigureText(
                    titleText,
                    title,
                    28,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter);
            }

            return canvasObject.transform;
        }

        // 화면 중앙 정렬 버튼 하나(배경 Image + Button + 라벨 Text)를 만든다.
        public static Button CreateCenteredButton(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            string label,
            int fontSize,
            UnityAction onClick,
            out Text labelText)
        {
            RectTransform buttonRect =
                CreateUiObject(
                    name,
                    parent);

            buttonRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            buttonRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            buttonRect.pivot =
                new Vector2(0.5f, 0.5f);

            buttonRect.anchoredPosition =
                anchoredPosition;

            buttonRect.sizeDelta =
                size;

            Image buttonImage =
                buttonRect.gameObject.AddComponent<Image>();

            buttonImage.color =
                new Color(0.2f, 0.2f, 0.26f, 1f);

            Button button =
                buttonRect.gameObject.AddComponent<Button>();

            button.targetGraphic =
                buttonImage;

            if (onClick != null)
            {
                button.onClick.AddListener(
                    onClick);
            }

            RectTransform labelRect =
                CreateStretchedRect(
                    "Label",
                    buttonRect);

            labelText =
                labelRect.gameObject.AddComponent<Text>();

            ConfigureText(
                labelText,
                label,
                fontSize,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);

            labelText.raycastTarget =
                false;

            return button;
        }
    }
}
