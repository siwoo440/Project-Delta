using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 133일차: 기획서 7.4절 "CG 갤러리" - 왼쪽에 몬스터+NPC 전체 목록(초상화+이름) 버튼,
    // 오른쪽에 선택한 캐릭터의 CG를 한 페이지 6장씩 큰 사각형으로 보여주고 화살표로
    // 페이지를 넘긴다. 실제 CG 이미지 자산이 없어서 지금은 해금 여부에 따라 색상 박스로
    // 대신한다 - 필요한 오브젝트는 전부 런타임에 코드로 생성한다(프리팹 없이 완결).
    public sealed class CgGalleryController : MonoBehaviour
    {
        // 캐릭터 한 명(몬스터 또는 NPC)의 갤러리 표시 정보.
        private sealed class GalleryCharacter
        {
            public string Id;
            public string DisplayName;
            public Color Tint;
            public List<string> CgIds;
        }

        private const float LeftEdgeGap = 24f; // 왼쪽 화면 끝과의 틈
        private const float LeftPanelWidth = 300f;
        private const float PanelGap = 16f;
        private const float RowHeight = 64f;
        private const float CgSlotSize = 280f;
        private const float CgSlotGap = 24f;
        private const int CgColumns = 3;
        private const int CgRows = 2;
        private const int CgPerPage = CgColumns * CgRows; // 133일차 추가 요청: 한 페이지 6장

        private readonly List<GalleryCharacter> characters =
            new List<GalleryCharacter>();

        private readonly List<Image> characterRowBackgrounds =
            new List<Image>();

        private Image[] cgSlotImages;
        private Text[] cgSlotLabels;
        private Text selectedTitleText;
        private Text pageIndicatorText;
        private Button prevPageButton;
        private Button nextPageButton;

        private int selectedIndex = -1;
        private int currentPage;

        private static readonly Color NormalRowColor =
            new Color(0.12f, 0.12f, 0.16f, 0.9f);

        private static readonly Color SelectedRowColor =
            new Color(0.30f, 0.45f, 0.65f, 0.95f);

        private static readonly Color LockedSlotColor =
            new Color(0.15f, 0.15f, 0.15f, 1f);

        private static readonly Color EmptySlotColor =
            new Color(0f, 0f, 0f, 0f);

        private void Awake()
        {
            EnsureEventSystem();
            LoadCharacters();

            Transform canvasTransform =
                BuildCanvas();

            BuildLeftPanel(
                canvasTransform);

            BuildRightPanel(
                canvasTransform);

            BuildBackButton(
                canvasTransform);

            if (characters.Count > 0)
            {
                SelectCharacter(
                    0);
            }
        }

        // 133일차: 왼쪽 목록에 몬스터 전체 + NPC 전체를 함께 채운다.
        private void LoadCharacters()
        {
            characters.Clear();

            LoadMonsters();
            LoadNpcs();
        }

        private void LoadMonsters()
        {
            // Resources 폴더 밖에 있던 시절엔 이 화면(타이틀 직후, 던전을 한 번도 안 거친
            // 상태)에서 Resources.FindObjectsOfTypeAll이 아무것도 못 찾았다 - 몬스터
            // 정의 에셋을 Resources로 옮기고 LoadAll로 직접 불러오도록 바꿨다.
            MonsterDefinition[] all =
                Resources.LoadAll<MonsterDefinition>(
                    "Monster Definition");

            for (int i = 0; i < all.Length; i++)
            {
                MonsterDefinition definition =
                    all[i];

                if (definition == null
                    || string.IsNullOrEmpty(
                        definition.Id)
                    || definition.Tier == MonsterTier.Boss)
                {
                    // 도감/CG는 기획서상 일반 몬스터 20종 기준이라 보스(마왕 등)는 뺀다.
                    continue;
                }

                List<string> cgIds =
                    new List<string>();

                int[] thresholds =
                    MonsterCgRule.AffinityThresholds;

                for (int t = 0; t < thresholds.Length; t++)
                {
                    cgIds.Add(
                        MonsterCgRule.BuildCgId(
                            definition.Id,
                            thresholds[t]));
                }

                characters.Add(
                    new GalleryCharacter
                    {
                        Id = definition.Id,
                        DisplayName = definition.DisplayName,
                        Tint = GetTint(
                            definition.Id),
                        CgIds = cgIds
                    });
            }

            characters.Sort(
                (a, b) => string.CompareOrdinal(
                    a.DisplayName,
                    b.DisplayName));
        }

        // 133일차: NPC는 정식 정의 에셋이 없고 런타임에만 생성되는 역할 4종이 전부라,
        // NpcRosterCatalog(NpcRuntimeBootstrapController와 공유)에서 그대로 가져온다.
        private void LoadNpcs()
        {
            NpcRoleConfig[] roles =
                NpcRosterCatalog.RoleConfigs;

            for (int i = 0; i < roles.Length; i++)
            {
                NpcRoleConfig role =
                    roles[i];

                List<string> cgIds =
                    new List<string>();

                int[] thresholds =
                    NpcCgRule.AffinityThresholds;

                for (int t = 0; t < thresholds.Length; t++)
                {
                    cgIds.Add(
                        NpcCgRule.BuildCgId(
                            role.Id,
                            thresholds[t]));
                }

                characters.Add(
                    new GalleryCharacter
                    {
                        Id = role.Id,
                        DisplayName = role.DisplayName,
                        Tint = GetTint(
                            role.Id),
                        CgIds = cgIds
                    });
            }
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
                    "CgGalleryCanvas",
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

            scaler.referenceResolution =
                new Vector2(1920f, 1080f);

            CreateBackground(
                canvasObject.transform);

            CreateTitle(
                canvasObject.transform);

            return canvasObject.transform;
        }

        private void CreateBackground(
            Transform parent)
        {
            RectTransform background =
                CreateStretchedRect(
                    "Background",
                    parent);

            Image image =
                background.gameObject.AddComponent<Image>();

            image.color =
                new Color(0.05f, 0.05f, 0.08f, 1f);

            image.raycastTarget =
                false;
        }

        private void CreateTitle(
            Transform parent)
        {
            RectTransform titleRect =
                CreateUiObject(
                    "Title",
                    parent);

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
                "CG 갤러리",
                28,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
        }

        // 133일차 추가 요청: 왼쪽 패널이 화면 왼쪽에 살짝 틈을 두고 밀착되도록 앵커를
        // (0,0)~(0,1)로 잡고 offsetMin.x로만 간격을 준다.
        private void BuildLeftPanel(
            Transform parent)
        {
            RectTransform panelRect =
                CreateUiObject(
                    "LeftPanel",
                    parent);

            panelRect.anchorMin =
                new Vector2(0f, 0f);

            panelRect.anchorMax =
                new Vector2(0f, 1f);

            panelRect.pivot =
                new Vector2(0f, 0.5f);

            panelRect.anchoredPosition =
                new Vector2(LeftEdgeGap, 0f);

            panelRect.sizeDelta =
                new Vector2(LeftPanelWidth, -160f);

            Image panelImage =
                panelRect.gameObject.AddComponent<Image>();

            panelImage.color =
                new Color(0.1f, 0.1f, 0.14f, 0.85f);

            RectTransform viewport =
                CreateStretchedRect(
                    "Viewport",
                    panelRect);

            // 133일차 버그 수정: Mask 컴포넌트는 자신의 그래픽 알파값으로 스텐실을
            // 채운다 - Color.clear(alpha 0)를 쓰면 마스크 영역 전체가 "보이지 않음"으로
            // 처리돼 자식(몬스터/NPC 버튼 전체)이 통째로 사라져버렸다. 알파에 의존하지
            // 않는 RectMask2D로 바꿔서 이 문제 자체를 없앤다.
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content =
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

            content.sizeDelta =
                new Vector2(0f, characters.Count * RowHeight);

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

            BuildCharacterButtons(
                content);
        }

        // 133일차: "모든 몬스터와 NPC를 버튼으로" - 목록 전체를 빠짐없이 만든다.
        private void BuildCharacterButtons(
            Transform content)
        {
            characterRowBackgrounds.Clear();

            for (int i = 0; i < characters.Count; i++)
            {
                GalleryCharacter character =
                    characters[i];

                RectTransform rowRect =
                    CreateUiObject(
                        $"Row_{character.Id}",
                        content);

                rowRect.anchorMin =
                    new Vector2(0f, 1f);

                rowRect.anchorMax =
                    new Vector2(1f, 1f);

                rowRect.pivot =
                    new Vector2(0.5f, 1f);

                rowRect.anchoredPosition =
                    new Vector2(0f, -i * RowHeight);

                rowRect.sizeDelta =
                    new Vector2(0f, RowHeight);

                Image rowImage =
                    rowRect.gameObject.AddComponent<Image>();

                rowImage.color =
                    NormalRowColor;

                characterRowBackgrounds.Add(
                    rowImage);

                Button rowButton =
                    rowRect.gameObject.AddComponent<Button>();

                rowButton.targetGraphic =
                    rowImage;

                int capturedIndex =
                    i;

                rowButton.onClick.AddListener(
                    () => SelectCharacter(
                        capturedIndex));

                // 왼쪽 직사각형 초상화 자리(실제 일러스트가 없어 고유색으로 대신함).
                RectTransform portraitRect =
                    CreateUiObject(
                        "Portrait",
                        rowRect);

                portraitRect.anchorMin =
                    new Vector2(0f, 0.5f);

                portraitRect.anchorMax =
                    new Vector2(0f, 0.5f);

                portraitRect.pivot =
                    new Vector2(0f, 0.5f);

                portraitRect.anchoredPosition =
                    new Vector2(10f, 0f);

                portraitRect.sizeDelta =
                    new Vector2(48f, RowHeight - 16f);

                Image portraitImage =
                    portraitRect.gameObject.AddComponent<Image>();

                portraitImage.color =
                    character.Tint;

                portraitImage.raycastTarget =
                    false;

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
                    new Vector2(70f, 0f);

                nameRect.offsetMax =
                    new Vector2(-8f, 0f);

                Text nameText =
                    nameRect.gameObject.AddComponent<Text>();

                ConfigureText(
                    nameText,
                    character.DisplayName,
                    18,
                    FontStyle.Normal,
                    TextAnchor.MiddleLeft);

                nameText.raycastTarget =
                    false;
            }
        }

        private void BuildRightPanel(
            Transform parent)
        {
            RectTransform panelRect =
                CreateUiObject(
                    "RightPanel",
                    parent);

            float leftPanelRightEdge =
                LeftEdgeGap
                + LeftPanelWidth
                + PanelGap;

            panelRect.anchorMin =
                new Vector2(0f, 0f);

            panelRect.anchorMax =
                new Vector2(1f, 1f);

            panelRect.pivot =
                new Vector2(0f, 0.5f);

            panelRect.offsetMin =
                new Vector2(leftPanelRightEdge, 80f);

            panelRect.offsetMax =
                new Vector2(-24f, -80f);

            Image panelImage =
                panelRect.gameObject.AddComponent<Image>();

            panelImage.color =
                new Color(0.1f, 0.1f, 0.14f, 0.85f);

            RectTransform titleRect =
                CreateUiObject(
                    "SelectedTitle",
                    panelRect);

            titleRect.anchorMin =
                new Vector2(0f, 1f);

            titleRect.anchorMax =
                new Vector2(1f, 1f);

            titleRect.pivot =
                new Vector2(0.5f, 1f);

            titleRect.anchoredPosition =
                new Vector2(0f, -12f);

            titleRect.sizeDelta =
                new Vector2(-24f, 32f);

            selectedTitleText =
                titleRect.gameObject.AddComponent<Text>();

            ConfigureText(
                selectedTitleText,
                string.Empty,
                20,
                FontStyle.Bold,
                TextAnchor.MiddleLeft);

            BuildCgGrid(
                panelRect);

            BuildPaginationControls(
                panelRect);
        }

        // 133일차 추가 요청: 한 페이지에 6장(3열×2행), 화살표로 페이지 이동.
        // 추가 요청: 오른쪽 패널 "중앙"에 더 크게 - 격자 전체 폭만큼의 가운데 정렬
        // 컨테이너를 만들고 그 안에서만 칸을 배치해, 패널이 얼마나 넓든 항상 중앙에 온다.
        private void BuildCgGrid(
            Transform parent)
        {
            float gridWidth =
                (CgColumns * CgSlotSize)
                + ((CgColumns - 1) * CgSlotGap);

            RectTransform gridContainer =
                CreateUiObject(
                    "CgGrid",
                    parent);

            gridContainer.anchorMin =
                new Vector2(0.5f, 1f);

            gridContainer.anchorMax =
                new Vector2(0.5f, 1f);

            gridContainer.pivot =
                new Vector2(0.5f, 1f);

            gridContainer.anchoredPosition =
                new Vector2(0f, -60f);

            gridContainer.sizeDelta =
                new Vector2(
                    gridWidth,
                    (CgRows * CgSlotSize) + ((CgRows - 1) * CgSlotGap));

            cgSlotImages =
                new Image[CgPerPage];

            cgSlotLabels =
                new Text[CgPerPage];

            for (int i = 0; i < CgPerPage; i++)
            {
                int column =
                    i % CgColumns;

                int row =
                    i / CgColumns;

                RectTransform slotRect =
                    CreateUiObject(
                        $"CgSlot_{i}",
                        gridContainer);

                slotRect.anchorMin =
                    new Vector2(0f, 1f);

                slotRect.anchorMax =
                    new Vector2(0f, 1f);

                slotRect.pivot =
                    new Vector2(0f, 1f);

                slotRect.anchoredPosition =
                    new Vector2(
                        column * (CgSlotSize + CgSlotGap),
                        -row * (CgSlotSize + CgSlotGap));

                slotRect.sizeDelta =
                    new Vector2(CgSlotSize, CgSlotSize);

                Image slotImage =
                    slotRect.gameObject.AddComponent<Image>();

                cgSlotImages[i] =
                    slotImage;

                RectTransform labelRect =
                    CreateStretchedRect(
                        "Label",
                        slotRect);

                Text labelText =
                    labelRect.gameObject.AddComponent<Text>();

                ConfigureText(
                    labelText,
                    "?",
                    32,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter);

                labelText.raycastTarget =
                    false;

                cgSlotLabels[i] =
                    labelText;
            }
        }

        // 추가 요청: 페이지 넘김 버튼도 오른쪽 패널 "중앙 하단"에 오도록 가운데 정렬
        // 컨테이너 기준으로 배치한다.
        private void BuildPaginationControls(
            Transform parent)
        {
            float gridHeight =
                (CgRows * CgSlotSize)
                + ((CgRows - 1) * CgSlotGap);

            float containerY =
                -60f
                - gridHeight
                - 24f;

            const float ButtonWidth = 120f;
            const float IndicatorWidth = 140f;
            const float ControlGap = 12f;

            float totalWidth =
                (ButtonWidth * 2f)
                + IndicatorWidth
                + (ControlGap * 2f);

            RectTransform paginationContainer =
                CreateUiObject(
                    "PaginationControls",
                    parent);

            paginationContainer.anchorMin =
                new Vector2(0.5f, 1f);

            paginationContainer.anchorMax =
                new Vector2(0.5f, 1f);

            paginationContainer.pivot =
                new Vector2(0.5f, 1f);

            paginationContainer.anchoredPosition =
                new Vector2(0f, containerY);

            paginationContainer.sizeDelta =
                new Vector2(totalWidth, 36f);

            prevPageButton =
                CreateSmallButton(
                    paginationContainer,
                    "◀ 이전",
                    new Vector2(0f, 0f),
                    () => ChangePage(-1));

            pageIndicatorText =
                CreateLabelAt(
                    paginationContainer,
                    new Vector2(ButtonWidth + ControlGap, 0f),
                    new Vector2(IndicatorWidth, 36f),
                    string.Empty);

            nextPageButton =
                CreateSmallButton(
                    paginationContainer,
                    "다음 ▶",
                    new Vector2(ButtonWidth + ControlGap + IndicatorWidth + ControlGap, 0f),
                    () => ChangePage(1));
        }

        private Button CreateSmallButton(
            Transform parent,
            string label,
            Vector2 position,
            UnityEngine.Events.UnityAction onClick)
        {
            RectTransform buttonRect =
                CreateUiObject(
                    $"Button_{label}",
                    parent);

            buttonRect.anchorMin =
                new Vector2(0f, 1f);

            buttonRect.anchorMax =
                new Vector2(0f, 1f);

            buttonRect.pivot =
                new Vector2(0f, 1f);

            buttonRect.anchoredPosition =
                position;

            buttonRect.sizeDelta =
                new Vector2(120f, 36f);

            Image buttonImage =
                buttonRect.gameObject.AddComponent<Image>();

            buttonImage.color =
                new Color(0.2f, 0.2f, 0.26f, 1f);

            Button button =
                buttonRect.gameObject.AddComponent<Button>();

            button.targetGraphic =
                buttonImage;

            button.onClick.AddListener(
                onClick);

            RectTransform labelRect =
                CreateStretchedRect(
                    "Label",
                    buttonRect);

            Text labelText =
                labelRect.gameObject.AddComponent<Text>();

            ConfigureText(
                labelText,
                label,
                16,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);

            labelText.raycastTarget =
                false;

            return button;
        }

        private Text CreateLabelAt(
            Transform parent,
            Vector2 position,
            Vector2 size,
            string content)
        {
            RectTransform rect =
                CreateUiObject(
                    "PageIndicator",
                    parent);

            rect.anchorMin =
                new Vector2(0f, 1f);

            rect.anchorMax =
                new Vector2(0f, 1f);

            rect.pivot =
                new Vector2(0f, 1f);

            rect.anchoredPosition =
                position;

            rect.sizeDelta =
                size;

            Text text =
                rect.gameObject.AddComponent<Text>();

            ConfigureText(
                text,
                content,
                16,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);

            return text;
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
                new Vector2(LeftEdgeGap, 24f);

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

        // 133일차 추가 요청: "처음 들어오면 가장 위(첫 번째) 캐릭터가 선택돼있게".
        private void SelectCharacter(
            int index)
        {
            if (index < 0
                || index >= characters.Count)
            {
                return;
            }

            selectedIndex =
                index;

            currentPage =
                0;

            for (int i = 0; i < characterRowBackgrounds.Count; i++)
            {
                characterRowBackgrounds[i].color =
                    i == selectedIndex
                        ? SelectedRowColor
                        : NormalRowColor;
            }

            RefreshCgGrid();
        }

        private void ChangePage(
            int delta)
        {
            if (selectedIndex < 0
                || selectedIndex >= characters.Count)
            {
                return;
            }

            int pageCount =
                GetPageCount(
                    characters[selectedIndex]);

            currentPage =
                Mathf.Clamp(
                    currentPage + delta,
                    0,
                    Mathf.Max(0, pageCount - 1));

            RefreshCgGrid();
        }

        private static int GetPageCount(
            GalleryCharacter character)
        {
            if (character.CgIds.Count == 0)
            {
                return 1;
            }

            return Mathf.CeilToInt(
                character.CgIds.Count
                / (float)CgPerPage);
        }

        private void RefreshCgGrid()
        {
            if (selectedIndex < 0
                || selectedIndex >= characters.Count)
            {
                return;
            }

            GalleryCharacter character =
                characters[selectedIndex];

            selectedTitleText.text =
                $"{character.DisplayName}의 CG";

            int pageCount =
                GetPageCount(
                    character);

            pageIndicatorText.text =
                $"{currentPage + 1} / {pageCount} Page";

            prevPageButton.interactable =
                currentPage > 0;

            nextPageButton.interactable =
                currentPage < pageCount - 1;

            int startIndex =
                currentPage * CgPerPage;

            for (int slot = 0; slot < CgPerPage; slot++)
            {
                int cgIndex =
                    startIndex + slot;

                if (cgIndex >= character.CgIds.Count)
                {
                    cgSlotImages[slot].color =
                        EmptySlotColor;

                    cgSlotLabels[slot].text =
                        string.Empty;

                    continue;
                }

                string cgId =
                    character.CgIds[cgIndex];

                bool unlocked =
                    ApplicationFlow.Current != null
                    && ApplicationFlow.Current.IsCgUnlocked(
                        cgId);

                cgSlotImages[slot].color =
                    unlocked
                        ? character.Tint
                        : LockedSlotColor;

                cgSlotLabels[slot].text =
                    unlocked
                        ? DescribeCgId(
                            cgId)
                        : "?";

                cgSlotLabels[slot].fontSize =
                    unlocked
                        ? 18
                        : 32;
            }
        }

        private static string DescribeCgId(
            string cgId)
        {
            int lastUnderscore =
                cgId.LastIndexOf(
                    '_');

            return lastUnderscore >= 0
                ? $"호감도 {cgId.Substring(lastUnderscore + 1)}"
                : cgId;
        }

        private static Color GetTint(
            string id)
        {
            int hash =
                !string.IsNullOrEmpty(id)
                    ? id.GetHashCode()
                    : 0;

            float hue =
                Mathf.Abs(
                    hash
                    % 360)
                / 360f;

            return Color.HSVToRGB(
                hue,
                0.55f,
                0.75f);
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
