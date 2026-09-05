using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 134일차: 기획서 7.5절 "Steam 도전과제" 전용 목록 화면 - CG 갤러리(133일차)와 같은
    // "메인 메뉴에서 별도 탭으로 진입 + 실제 상태가 True가 되면 해금 표시" 구조를 그대로
    // 따른다. 카테고리 탭으로 필터링한 뒤 하나의 세로 스크롤 목록에 100개를 전부 보여주고,
    // 숨김 도전과제는 미달성일 때 이름 대신 "???"로 가린다.
    public sealed class AchievementGalleryController : MonoBehaviour
    {
        private const float RowHeight = 56f;
        private const float TabButtonWidth = 160f;
        private const float TabButtonHeight = 40f;

        private static readonly Color NormalRowColor =
            new Color(0.12f, 0.12f, 0.16f, 0.9f);

        private static readonly Color UnlockedRowColor =
            new Color(0.20f, 0.45f, 0.28f, 0.95f);

        private static readonly Color TabNormalColor =
            new Color(0.16f, 0.16f, 0.22f, 1f);

        private static readonly Color TabSelectedColor =
            new Color(0.30f, 0.45f, 0.65f, 1f);

        // 화면에 노출할 카테고리 순서 - null은 "전체" 필터를 뜻한다.
        private static readonly AchievementCategory?[] CategoryFilters =
        {
            null,
            AchievementCategory.Ending,
            AchievementCategory.Defeat,
            AchievementCategory.Lifetime,
            AchievementCategory.ActionProficiency
        };

        private static readonly string[] CategoryFilterNames =
        {
            "전체",
            "엔딩",
            "패배 기록",
            "탐험·전투·성장",
            "행동 숙련도"
        };

        private RectTransform content;
        private Text summaryText;
        private readonly List<Button> tabButtons = new List<Button>();
        private int selectedFilterIndex;

        private void Awake()
        {
            EnsureEventSystem();

            Transform canvasTransform =
                BuildCanvas();

            BuildSummaryHeader(
                canvasTransform);

            BuildCategoryTabs(
                canvasTransform);

            BuildListPanel(
                canvasTransform);

            BuildBackButton(
                canvasTransform);

            RefreshList();
        }

        private static void EnsureEventSystem()
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

            DontDestroyOnLoad(
                eventSystemObject);
        }

        private Transform BuildCanvas()
        {
            GameObject canvasObject =
                new GameObject(
                    "AchievementGalleryCanvas",
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

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // 136일차: 설정 화면의 UI 배율(소/보통/대)을 CanvasScaler 화면에도 반영한다.
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
                "도전과제",
                28,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);

            return canvasObject.transform;
        }

        // 134일차: 기획서 7.5절 "직접 보상 없음" - 진행 상황만 확인하는 용도로 개수만 보여준다.
        private void BuildSummaryHeader(
            Transform parent)
        {
            RectTransform summaryRect =
                CreateUiObject(
                    "Summary",
                    parent);

            summaryRect.anchorMin =
                new Vector2(0.5f, 1f);

            summaryRect.anchorMax =
                new Vector2(0.5f, 1f);

            summaryRect.pivot =
                new Vector2(0.5f, 1f);

            summaryRect.anchoredPosition =
                new Vector2(0f, -96f);

            summaryRect.sizeDelta =
                new Vector2(400f, 32f);

            summaryText =
                summaryRect.gameObject.AddComponent<Text>();

            ConfigureText(
                summaryText,
                string.Empty,
                18,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);
        }

        private void BuildCategoryTabs(
            Transform parent)
        {
            float totalWidth =
                (TabButtonWidth * CategoryFilters.Length)
                + (12f * (CategoryFilters.Length - 1));

            RectTransform tabContainer =
                CreateUiObject(
                    "CategoryTabs",
                    parent);

            tabContainer.anchorMin =
                new Vector2(0.5f, 1f);

            tabContainer.anchorMax =
                new Vector2(0.5f, 1f);

            tabContainer.pivot =
                new Vector2(0.5f, 1f);

            tabContainer.anchoredPosition =
                new Vector2(0f, -148f);

            tabContainer.sizeDelta =
                new Vector2(totalWidth, TabButtonHeight);

            for (int i = 0; i < CategoryFilters.Length; i++)
            {
                int capturedIndex =
                    i;

                RectTransform buttonRect =
                    CreateUiObject(
                        $"Tab_{CategoryFilterNames[i]}",
                        tabContainer);

                buttonRect.anchorMin =
                    new Vector2(0f, 0f);

                buttonRect.anchorMax =
                    new Vector2(0f, 1f);

                buttonRect.pivot =
                    new Vector2(0f, 0.5f);

                buttonRect.anchoredPosition =
                    new Vector2(i * (TabButtonWidth + 12f), 0f);

                buttonRect.sizeDelta =
                    new Vector2(TabButtonWidth, 0f);

                Image buttonImage =
                    buttonRect.gameObject.AddComponent<Image>();

                buttonImage.color =
                    i == selectedFilterIndex
                        ? TabSelectedColor
                        : TabNormalColor;

                Button button =
                    buttonRect.gameObject.AddComponent<Button>();

                button.targetGraphic =
                    buttonImage;

                button.onClick.AddListener(
                    () => SelectFilter(capturedIndex));

                tabButtons.Add(
                    button);

                RectTransform labelRect =
                    CreateStretchedRect(
                        "Label",
                        buttonRect);

                Text labelText =
                    labelRect.gameObject.AddComponent<Text>();

                ConfigureText(
                    labelText,
                    CategoryFilterNames[i],
                    16,
                    FontStyle.Normal,
                    TextAnchor.MiddleCenter);

                labelText.raycastTarget =
                    false;
            }
        }

        private void BuildListPanel(
            Transform parent)
        {
            RectTransform panelRect =
                CreateUiObject(
                    "ListPanel",
                    parent);

            panelRect.anchorMin =
                new Vector2(0.5f, 0f);

            panelRect.anchorMax =
                new Vector2(0.5f, 1f);

            panelRect.pivot =
                new Vector2(0.5f, 0.5f);

            // 탭(-122)과 겹치지 않도록 위쪽 여백을 충분히 두고, offsetMin/offsetMax로
            // 좌우 폭(900)과 상하 여백을 한 번에 고정한다(가운데 점 앵커라도 두 값을
            // 모두 지정하면 그대로 사각형이 정해진다).
            panelRect.offsetMin =
                new Vector2(-450f, 40f);

            panelRect.offsetMax =
                new Vector2(450f, -190f);

            Image panelImage =
                panelRect.gameObject.AddComponent<Image>();

            panelImage.color =
                new Color(0.1f, 0.1f, 0.14f, 0.85f);

            RectTransform viewport =
                CreateStretchedRect(
                    "Viewport",
                    panelRect);

            viewport.gameObject.AddComponent<RectMask2D>();

            content =
                CreateUiObject(
                    "Content",
                    viewport);

            content.anchorMin =
                new Vector2(0f, 1f);

            content.anchorMax =
                new Vector2(1f, 1f);

            content.pivot =
                new Vector2(0.5f, 1f);

            content.anchoredPosition =
                Vector2.zero;

            ScrollRect scrollRect =
                panelRect.gameObject.AddComponent<ScrollRect>();

            scrollRect.viewport =
                viewport;

            scrollRect.content =
                content;

            scrollRect.horizontal =
                false;

            scrollRect.vertical =
                true;
        }

        private void BuildBackButton(
            Transform parent)
        {
            RectTransform buttonRect =
                CreateUiObject(
                    "BackButton",
                    parent);

            buttonRect.anchorMin =
                new Vector2(0f, 0f);

            buttonRect.anchorMax =
                new Vector2(0f, 0f);

            buttonRect.pivot =
                new Vector2(0f, 0f);

            buttonRect.anchoredPosition =
                new Vector2(24f, 24f);

            buttonRect.sizeDelta =
                new Vector2(160f, 44f);

            Image buttonImage =
                buttonRect.gameObject.AddComponent<Image>();

            buttonImage.color =
                new Color(0.2f, 0.2f, 0.26f, 1f);

            Button button =
                buttonRect.gameObject.AddComponent<Button>();

            button.targetGraphic =
                buttonImage;

            button.onClick.AddListener(
                () => ApplicationFlow.Current?.EnterTitle());

            RectTransform labelRect =
                CreateStretchedRect(
                    "Label",
                    buttonRect);

            Text labelText =
                labelRect.gameObject.AddComponent<Text>();

            ConfigureText(
                labelText,
                "뒤로",
                18,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);

            labelText.raycastTarget =
                false;
        }

        private void SelectFilter(
            int filterIndex)
        {
            selectedFilterIndex =
                filterIndex;

            for (int i = 0; i < tabButtons.Count; i++)
            {
                Image tabImage =
                    tabButtons[i].targetGraphic as Image;

                if (tabImage != null)
                {
                    tabImage.color =
                        i == selectedFilterIndex
                            ? TabSelectedColor
                            : TabNormalColor;
                }
            }

            RefreshList();
        }

        // 134일차: 100개 카탈로그를 현재 선택한 카테고리로 걸러 세로 목록을 다시 그린다.
        private void RefreshList()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(
                    content.GetChild(i).gameObject);
            }

            AchievementCategory? filter =
                CategoryFilters[selectedFilterIndex];

            List<AchievementDefinition> visible =
                new List<AchievementDefinition>();

            for (int i = 0; i < AchievementCatalog.All.Count; i++)
            {
                AchievementDefinition definition =
                    AchievementCatalog.All[i];

                if (filter == null
                    || definition.Category == filter.Value)
                {
                    visible.Add(
                        definition);
                }
            }

            content.sizeDelta =
                new Vector2(0f, visible.Count * RowHeight);

            int unlockedInCatalog = 0;

            for (int i = 0; i < AchievementCatalog.All.Count; i++)
            {
                if (ApplicationFlow.Current != null
                    && ApplicationFlow.Current.IsAchievementUnlocked(
                        AchievementCatalog.All[i].Id))
                {
                    unlockedInCatalog++;
                }
            }

            summaryText.text =
                $"달성 {unlockedInCatalog} / {AchievementCatalog.ExpectedCount}";

            for (int i = 0; i < visible.Count; i++)
            {
                BuildRow(
                    visible[i],
                    i);
            }
        }

        private void BuildRow(
            AchievementDefinition definition,
            int rowIndex)
        {
            bool unlocked =
                ApplicationFlow.Current != null
                && ApplicationFlow.Current.IsAchievementUnlocked(
                    definition.Id);

            RectTransform rowRect =
                CreateUiObject(
                    $"Row_{definition.Id}",
                    content);

            rowRect.anchorMin =
                new Vector2(0f, 1f);

            rowRect.anchorMax =
                new Vector2(1f, 1f);

            rowRect.pivot =
                new Vector2(0.5f, 1f);

            rowRect.anchoredPosition =
                new Vector2(0f, -rowIndex * RowHeight);

            rowRect.sizeDelta =
                new Vector2(0f, RowHeight);

            Image rowImage =
                rowRect.gameObject.AddComponent<Image>();

            rowImage.color =
                unlocked
                    ? UnlockedRowColor
                    : NormalRowColor;

            RectTransform nameRect =
                CreateUiObject(
                    "Name",
                    rowRect);

            nameRect.anchorMin =
                new Vector2(0f, 0f);

            nameRect.anchorMax =
                new Vector2(1f, 1f);

            nameRect.pivot =
                new Vector2(0.5f, 0.5f);

            nameRect.offsetMin =
                new Vector2(24f, 0f);

            nameRect.offsetMax =
                new Vector2(-180f, 0f);

            Text nameText =
                nameRect.gameObject.AddComponent<Text>();

            // 134일차: 기획서 7.5절 "숨김 도전과제" - 아직 못 얻은 숨김 항목은 이름조차
            // 노출하지 않는다.
            bool hideName =
                definition.IsHidden
                && !unlocked;

            ConfigureText(
                nameText,
                hideName
                    ? "??? (숨김 도전과제)"
                    : definition.DisplayName,
                18,
                FontStyle.Normal,
                TextAnchor.MiddleLeft);

            nameText.raycastTarget =
                false;

            RectTransform statusRect =
                CreateUiObject(
                    "Status",
                    rowRect);

            statusRect.anchorMin =
                new Vector2(1f, 0f);

            statusRect.anchorMax =
                new Vector2(1f, 1f);

            statusRect.pivot =
                new Vector2(1f, 0.5f);

            statusRect.anchoredPosition =
                new Vector2(-16f, 0f);

            statusRect.sizeDelta =
                new Vector2(140f, 0f);

            Text statusText =
                statusRect.gameObject.AddComponent<Text>();

            ConfigureText(
                statusText,
                unlocked
                    ? "달성"
                    : "미달성",
                16,
                FontStyle.Bold,
                TextAnchor.MiddleRight);

            statusText.color =
                unlocked
                    ? new Color(0.6f, 1f, 0.6f, 1f)
                    : new Color(0.7f, 0.7f, 0.7f, 1f);

            statusText.raycastTarget =
                false;
        }

        private static RectTransform CreateUiObject(
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

        private static RectTransform CreateStretchedRect(
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

        private static void ConfigureText(
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
    }
}
