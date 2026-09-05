using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using DungeonRunState = ProjectDelta.Domain.DungeonRunState; // Data/Domain 동명 타입 충돌 방지
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 113일차: 정면 한 칸의 NPC를 F로 선택하고 대화/서비스/떠나기 공통 흐름을 제공한다.
    // 114일차: "서비스"를 눌렀을 때 실제 상점·회복·정보·유물 정리 화면으로 이어지게 한다.
    // 115일차: 선물·구조·공격(적대 전환+전투)을 추가하고, 화면을 캐릭터 일러스트(상단)
    // + 대화창(하단 좌)·선택지 버튼(하단 우) 구성으로 다시 짰다.
    // 130일차: 왼쪽 열(일러스트+정보+행동 버튼) / 오른쪽 열(서비스 내용) 2단 구성으로 개편.
    // 140일차: 기획서 8.2절 "화면별 정식 UI" 전환 - OnGUI를 런타임 Canvas로 옮겼다.
    // 판정 로직(NpcInteractionService 등 호출)은 전혀 건드리지 않고, 상태가 바뀔 때마다
    // 오른쪽 콘텐츠 영역을 통째로 다시 그리는 방식(OnGUI의 "매 프레임 다시 그리기"를
    // "상태 변경 시점에 다시 그리기"로 옮긴 것)으로 구성했다.
    public sealed class NpcInteractionController : MonoBehaviour
    {
        private enum ServiceScreen
        {
            None,
            Menu,
            Shop,
            Heal,
            Info,
            Relic,
            Gift
        }

        private enum ShopTab
        {
            Buy,
            Sell
        }

        private const int HealCost = 15;
        private const int CurseRemovalCost = 20;
        private const int SacrificeReward = 10;
        private const int GiftAffinityGain = 10;

        private const float IllustrationSize = 240f;
        private const float LeftColumnWidth = 320f;
        private const float MainAreaWidth = 620f;
        private const float PanelHeight = 520f;
        private const float PanelGap = 14f;
        private const float RowHeight = 34f;
        private const float RowGap = 4f;
        private const float ListHeight = 260f;

        private static readonly ItemCategory[] ShopFilterableCategories =
        {
            ItemCategory.Consumable,
            ItemCategory.ExplorationTool,
            ItemCategory.Equipment,
            ItemCategory.Relic
        };

        private static readonly Color NormalButtonColor =
            new Color(0.2f, 0.2f, 0.26f, 1f);

        private static readonly Color SelectedButtonColor =
            new Color(0.30f, 0.45f, 0.65f, 1f);

        private PlayerGridMovementController movementController;
        private PlayerLookController lookController;
        private Transform viewTransform;
        private NpcContentMarker openNpc;

        private readonly NpcInteractionService interactionService =
            new NpcInteractionService();

        private DungeonFloorController floorController;
        private ExplorationMonsterEncounterController encounterController;

        private bool isPanelOpen;
        private string promptText;
        private string statusText;
        private ServiceScreen serviceScreen =
            ServiceScreen.None;

        private ShopTab shopTab =
            ShopTab.Buy;

        private ItemCategory? shopCategoryFilter;

        // Canvas UI 참조 - Awake에서 한 번만 만들고, 상태가 바뀔 때마다 다시 그린다.
        private Text promptUiText;
        private GameObject panelRoot;
        private Image illustrationImage;
        private Text illustrationNameText;
        private Text infoText;
        private Text servicesLabelText;
        private Button serviceButton;
        private Button attackButton;
        private RectTransform mainContentRoot;

        private void Awake()
        {
            movementController =
                GetComponent<PlayerGridMovementController>();

            lookController =
                GetComponent<PlayerLookController>();

            Camera mainCamera =
                Camera.main;

            viewTransform =
                mainCamera != null
                    ? mainCamera.transform
                    : transform;

            floorController =
                FindFirstObjectByType<DungeonFloorController>();

            encounterController =
                FindFirstObjectByType<ExplorationMonsterEncounterController>();

            RuntimeUiFactory.EnsureEventSystem();

            BuildCanvas();
        }

        private void Update()
        {
            if (isPanelOpen)
            {
                if (Keyboard.current != null
                    && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    CloseInteraction();
                }

                return;
            }

            if (movementController == null
                || movementController.PlayerState == null
                || movementController.IsMoving
                || movementController.IsInputLocked)
            {
                SetPromptText(
                    string.Empty);

                return;
            }

            NpcContentMarker npc =
                FindNpcInFront();

            SetPromptText(
                npc != null
                    ? $"{npc.Definition?.DisplayName ?? "NPC"} 대화 [F]"
                    : string.Empty);

            if (npc != null
                && Keyboard.current != null
                && Keyboard.current.fKey.wasPressedThisFrame)
            {
                OpenInteraction(
                    npc);
            }
        }

        private void SetPromptText(
            string text)
        {
            promptText =
                text;

            if (promptUiText == null)
            {
                return;
            }

            promptUiText.text =
                text;

            promptUiText.gameObject.SetActive(
                !string.IsNullOrEmpty(text));
        }

        private void OpenInteraction(
            NpcContentMarker npc)
        {
            if (npc == null
                || npc.Definition == null
                || npc.RelationshipState == null)
            {
                return;
            }

            openNpc =
                npc;

            isPanelOpen =
                true;

            SetPromptText(
                string.Empty);

            statusText =
                "무엇을 할지 선택하세요.";

            serviceScreen =
                ServiceScreen.None;

            openNpc.RelationshipState.RegisterEncounter();

            // 130일차: 대화 UI가 화면을 크게 차지하는 동안 뒤에 있는 NPC 3D 모델(캡슐)이
            // 겹쳐 보이지 않도록 렌더러만 끈다.
            SetNpcModelVisible(
                npc,
                false);

            if (movementController != null)
            {
                movementController.IsInputLocked =
                    true;
            }

            lookController?.SetCursorFreeForUi(
                true);

            panelRoot.SetActive(
                true);

            RefreshLeftColumn();
            RefreshMainArea();
        }

        private static void SetNpcModelVisible(
            NpcContentMarker npc,
            bool visible)
        {
            if (npc == null)
            {
                return;
            }

            Renderer npcRenderer =
                npc.GetComponent<Renderer>();

            if (npcRenderer != null)
            {
                npcRenderer.enabled =
                    visible;
            }
        }

        private void CloseInteraction()
        {
            isPanelOpen =
                false;

            SetNpcModelVisible(
                openNpc,
                true);

            openNpc =
                null;

            statusText =
                string.Empty;

            serviceScreen =
                ServiceScreen.None;

            if (movementController != null)
            {
                movementController.IsInputLocked =
                    false;
            }

            lookController?.SetCursorFreeForUi(
                false);

            panelRoot.SetActive(
                false);
        }

        private NpcContentMarker FindNpcInFront()
        {
            RoomView roomView =
                movementController != null
                    ? movementController.CurrentRoomView
                    : null;

            PlayerRunState playerState =
                movementController != null
                    ? movementController.PlayerState
                    : null;

            if (roomView == null
                || playerState == null)
            {
                return null;
            }

            float yaw =
                viewTransform != null
                    ? viewTransform.eulerAngles.y
                    : transform.eulerAngles.y;

            CardinalDirection facing =
                GridMovement.GetFacingFromYaw(
                    yaw);

            GridPosition delta =
                GridMovement.GetDirectionDelta(
                    facing);

            GridPosition frontPosition =
                new GridPosition(
                    playerState.CurrentGridPosition.X + delta.X,
                    playerState.CurrentGridPosition.Z + delta.Z);

            foreach (RoomContentMarker marker
                     in roomView.GetMarkers(
                         RoomContentType.NpcPoint))
            {
                if (marker == null
                    || !marker.gameObject.activeInHierarchy
                    || marker.GridPosition != frontPosition)
                {
                    continue;
                }

                NpcContentMarker npc =
                    marker.GetComponent<NpcContentMarker>();

                if (npc != null)
                {
                    return npc;
                }
            }

            return null;
        }

        // ===================== Canvas 빌드 =====================

        private void BuildCanvas()
        {
            Transform canvasTransform =
                RuntimeUiFactory.BuildScreenCanvas(
                    transform,
                    "NpcInteractionCanvas",
                    null);

            BuildPromptText(
                canvasTransform);

            BuildPanel(
                canvasTransform);
        }

        private void BuildPromptText(
            Transform parent)
        {
            RectTransform rect =
                RuntimeUiFactory.CreateUiObject(
                    "Prompt",
                    parent);

            rect.anchorMin =
                new Vector2(0.5f, 0f);

            rect.anchorMax =
                new Vector2(0.5f, 0f);

            rect.pivot =
                new Vector2(0.5f, 0f);

            rect.anchoredPosition =
                new Vector2(0f, 92f);

            rect.sizeDelta =
                new Vector2(360f, 34f);

            promptUiText =
                rect.gameObject.AddComponent<Text>();

            RuntimeUiFactory.ConfigureText(
                promptUiText,
                string.Empty,
                16,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);

            rect.gameObject.SetActive(
                false);
        }

        private void BuildPanel(
            Transform parent)
        {
            float totalWidth =
                LeftColumnWidth
                + PanelGap
                + MainAreaWidth;

            RectTransform panelRect =
                RuntimeUiFactory.CreateUiObject(
                    "InteractionPanel",
                    parent);

            panelRoot =
                panelRect.gameObject;

            panelRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            panelRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            panelRect.pivot =
                new Vector2(0.5f, 0.5f);

            panelRect.anchoredPosition =
                Vector2.zero;

            panelRect.sizeDelta =
                new Vector2(totalWidth, PanelHeight);

            BuildLeftColumn(
                panelRect);

            BuildMainArea(
                panelRect);

            panelRoot.SetActive(
                false);
        }

        private void BuildLeftColumn(
            Transform parent)
        {
            RectTransform columnRect =
                RuntimeUiFactory.CreateUiObject(
                    "LeftColumn",
                    parent);

            columnRect.anchorMin =
                new Vector2(0f, 1f);

            columnRect.anchorMax =
                new Vector2(0f, 1f);

            columnRect.pivot =
                new Vector2(0f, 1f);

            columnRect.anchoredPosition =
                Vector2.zero;

            columnRect.sizeDelta =
                new Vector2(LeftColumnWidth, PanelHeight);

            // 일러스트 자리 - 실제 자산이 없어 역할별 색상 + 이름으로 대신한다.
            RectTransform illustrationRect =
                RuntimeUiFactory.CreateUiObject(
                    "Illustration",
                    columnRect);

            illustrationRect.anchorMin =
                new Vector2(0f, 1f);

            illustrationRect.anchorMax =
                new Vector2(0f, 1f);

            illustrationRect.pivot =
                new Vector2(0f, 1f);

            illustrationRect.anchoredPosition =
                Vector2.zero;

            illustrationRect.sizeDelta =
                new Vector2(LeftColumnWidth, IllustrationSize);

            illustrationImage =
                illustrationRect.gameObject.AddComponent<Image>();

            RectTransform illustrationNameRect =
                RuntimeUiFactory.CreateStretchedRect(
                    "Name",
                    illustrationRect);

            illustrationNameText =
                illustrationNameRect.gameObject.AddComponent<Text>();

            RuntimeUiFactory.ConfigureText(
                illustrationNameText,
                string.Empty,
                24,
                FontStyle.Bold,
                TextAnchor.LowerCenter);

            illustrationNameText.raycastTarget =
                false;

            float infoY =
                -(IllustrationSize + PanelGap);

            RectTransform infoRect =
                RuntimeUiFactory.CreateUiObject(
                    "Info",
                    columnRect);

            infoRect.anchorMin =
                new Vector2(0f, 1f);

            infoRect.anchorMax =
                new Vector2(0f, 1f);

            infoRect.pivot =
                new Vector2(0f, 1f);

            infoRect.anchoredPosition =
                new Vector2(0f, infoY);

            infoRect.sizeDelta =
                new Vector2(LeftColumnWidth, 54f);

            infoText =
                infoRect.gameObject.AddComponent<Text>();

            RuntimeUiFactory.ConfigureText(
                infoText,
                string.Empty,
                14,
                FontStyle.Normal,
                TextAnchor.UpperLeft);

            float servicesLabelY =
                infoY
                - 54f
                - 4f;

            RectTransform servicesLabelRect =
                RuntimeUiFactory.CreateUiObject(
                    "ServicesLabel",
                    columnRect);

            servicesLabelRect.anchorMin =
                new Vector2(0f, 1f);

            servicesLabelRect.anchorMax =
                new Vector2(0f, 1f);

            servicesLabelRect.pivot =
                new Vector2(0f, 1f);

            servicesLabelRect.anchoredPosition =
                new Vector2(0f, servicesLabelY);

            servicesLabelRect.sizeDelta =
                new Vector2(LeftColumnWidth, 22f);

            servicesLabelText =
                servicesLabelRect.gameObject.AddComponent<Text>();

            RuntimeUiFactory.ConfigureText(
                servicesLabelText,
                string.Empty,
                13,
                FontStyle.Bold,
                TextAnchor.UpperLeft);

            servicesLabelText.color =
                new Color(0.85f, 0.72f, 0.35f);

            float buttonsY =
                servicesLabelY
                - 22f
                - 4f;

            BuildMainButtons(
                columnRect,
                buttonsY);
        }

        private void BuildMainButtons(
            Transform parent,
            float startY)
        {
            float y =
                startY;

            BuildLeftColumnButton(
                parent,
                "TalkButton",
                y,
                "대화",
                () =>
                {
                    ApplyResult(
                        interactionService.Resolve(
                            openNpc.Definition,
                            openNpc.RelationshipState,
                            NpcInteractionCommand.Talk));

                    RefreshLeftColumn();
                });

            y -=
                RowHeight + RowGap;

            serviceButton =
                BuildLeftColumnButton(
                    parent,
                    "ServiceButton",
                    y,
                    "서비스",
                    () =>
                    {
                        NpcInteractionResult result =
                            interactionService.Resolve(
                                openNpc.Definition,
                                openNpc.RelationshipState,
                                NpcInteractionCommand.Service);

                        ApplyResult(
                            result);

                        if (result.ResultType
                            == NpcInteractionResultType.OpenService)
                        {
                            serviceScreen =
                                ServiceScreen.Menu;

                            RefreshMainArea();
                        }
                    });

            y -=
                RowHeight + RowGap;

            BuildLeftColumnButton(
                parent,
                "GiftButton",
                y,
                "선물",
                () =>
                {
                    statusText =
                        string.Empty;

                    serviceScreen =
                        ServiceScreen.Gift;

                    RefreshMainArea();
                });

            y -=
                RowHeight + RowGap;

            attackButton =
                BuildLeftColumnButton(
                    parent,
                    "AttackButton",
                    y,
                    "공격",
                    () =>
                    {
                        NpcInteractionResult attackResult =
                            interactionService.ResolveAttack(
                                openNpc.Definition,
                                openNpc.RelationshipState);

                        if (attackResult.ResultType
                            == NpcInteractionResultType.StartBattle)
                        {
                            TriggerNpcBattle();
                        }
                        else
                        {
                            ApplyResult(
                                attackResult);
                        }
                    });
        }

        private Button BuildLeftColumnButton(
            Transform parent,
            string name,
            float y,
            string label,
            UnityEngine.Events.UnityAction onClick)
        {
            RectTransform buttonRect =
                RuntimeUiFactory.CreateUiObject(
                    name,
                    parent);

            buttonRect.anchorMin =
                new Vector2(0f, 1f);

            buttonRect.anchorMax =
                new Vector2(0f, 1f);

            buttonRect.pivot =
                new Vector2(0f, 1f);

            buttonRect.anchoredPosition =
                new Vector2(0f, y);

            buttonRect.sizeDelta =
                new Vector2(LeftColumnWidth, RowHeight);

            Image buttonImage =
                buttonRect.gameObject.AddComponent<Image>();

            buttonImage.color =
                NormalButtonColor;

            Button button =
                buttonRect.gameObject.AddComponent<Button>();

            button.targetGraphic =
                buttonImage;

            button.onClick.AddListener(
                onClick);

            RectTransform labelRect =
                RuntimeUiFactory.CreateStretchedRect(
                    "Label",
                    buttonRect);

            Text labelText =
                labelRect.gameObject.AddComponent<Text>();

            RuntimeUiFactory.ConfigureText(
                labelText,
                label,
                16,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);

            labelText.raycastTarget =
                false;

            return button;
        }

        private void BuildMainArea(
            Transform parent)
        {
            RectTransform mainRect =
                RuntimeUiFactory.CreateUiObject(
                    "MainArea",
                    parent);

            mainRect.anchorMin =
                new Vector2(0f, 1f);

            mainRect.anchorMax =
                new Vector2(0f, 1f);

            mainRect.pivot =
                new Vector2(0f, 1f);

            mainRect.anchoredPosition =
                new Vector2(LeftColumnWidth + PanelGap, 0f);

            mainRect.sizeDelta =
                new Vector2(MainAreaWidth, PanelHeight);

            Image backgroundImage =
                mainRect.gameObject.AddComponent<Image>();

            backgroundImage.color =
                new Color(0.1f, 0.1f, 0.14f, 0.85f);

            RectTransform contentRect =
                RuntimeUiFactory.CreateUiObject(
                    "Content",
                    mainRect);

            contentRect.anchorMin =
                new Vector2(0f, 1f);

            contentRect.anchorMax =
                new Vector2(0f, 1f);

            contentRect.pivot =
                new Vector2(0f, 1f);

            contentRect.anchoredPosition =
                new Vector2(16f, -12f);

            contentRect.sizeDelta =
                new Vector2(MainAreaWidth - 32f, PanelHeight - 24f);

            mainContentRoot =
                contentRect;
        }

        // ===================== 상태 갱신 =====================

        private void RefreshLeftColumn()
        {
            if (openNpc == null
                || openNpc.Definition == null
                || openNpc.RelationshipState == null)
            {
                CloseInteraction();
                return;
            }

            illustrationImage.color =
                GetIllustrationColor(
                    openNpc.Definition.ServiceTypes);

            illustrationNameText.text =
                openNpc.Definition.DisplayName;

            infoText.text =
                $"호감도 {openNpc.RelationshipState.Affinity}/100 ({openNpc.RelationshipState.Stage})   |   {(openNpc.RelationshipState.IsHostile ? "적대" : "우호")}\n"
                + (string.IsNullOrEmpty(statusText)
                    ? "무엇을 할지 선택하세요."
                    : statusText);

            servicesLabelText.text =
                $"{openNpc.Definition.DisplayName} 서비스";

            serviceButton.interactable =
                openNpc.Definition.ServiceTypes != NpcServiceType.None;

            attackButton.interactable =
                openNpc.Definition.CanBattle;
        }

        private static Color GetIllustrationColor(
            NpcServiceType serviceTypes)
        {
            if ((serviceTypes & NpcServiceType.Trade) != 0)
            {
                return new Color(0.30f, 0.78f, 0.95f, 1f);
            }

            if ((serviceTypes & NpcServiceType.Healing) != 0)
            {
                return new Color(0.35f, 0.90f, 0.45f, 1f);
            }

            if ((serviceTypes
                    & (NpcServiceType.MapInformation
                        | NpcServiceType.ExplorationInformation))
                != 0)
            {
                return new Color(0.95f, 0.85f, 0.30f, 1f);
            }

            if ((serviceTypes
                    & (NpcServiceType.RelicTrade
                        | NpcServiceType.RelicResearch))
                != 0)
            {
                return new Color(0.75f, 0.35f, 0.90f, 1f);
            }

            return new Color(0.55f, 0.55f, 0.55f, 1f);
        }

        // 115일차: 적대 전환 즉시 이 NPC를 기존 몬스터 조우 파이프라인에 태운다.
        private void TriggerNpcBattle()
        {
            if (openNpc == null
                || floorController == null
                || encounterController == null
                || movementController == null
                || movementController.PlayerState == null)
            {
                CloseInteraction();
                return;
            }

            RoomContentMarker roomMarker =
                openNpc.GetComponent<RoomContentMarker>();

            if (roomMarker == null)
            {
                CloseInteraction();
                return;
            }

            string roomId =
                movementController.PlayerState.CurrentRoomId;

            ExplorationMonsterMarker monsterMarker =
                openNpc.GetComponent<ExplorationMonsterMarker>();

            if (monsterMarker == null)
            {
                monsterMarker =
                    openNpc.gameObject.AddComponent<ExplorationMonsterMarker>();
            }

            monsterMarker.Configure(
                roomId,
                openNpc.Definition.Id,
                roomMarker.GridPosition,
                new[] { openNpc.Definition.Id });

            floorController.RegisterRuntimeMonsterMarker(
                roomId,
                monsterMarker);

            CloseInteraction();

            encounterController.TryBeginEncounterAtCurrentPosition();
        }

        private void RefreshMainArea()
        {
            ClearChildren(
                mainContentRoot);

            switch (serviceScreen)
            {
                case ServiceScreen.Shop:
                    DrawShopScreen();
                    break;

                case ServiceScreen.Heal:
                    DrawHealScreen();
                    break;

                case ServiceScreen.Info:
                    DrawInfoScreen();
                    break;

                case ServiceScreen.Relic:
                    DrawRelicScreen();
                    break;

                case ServiceScreen.Gift:
                    DrawGiftScreen();
                    break;

                case ServiceScreen.Menu:
                    DrawServiceMenu();
                    break;

                default:
                    CreateLabel(
                        mainContentRoot,
                        0f,
                        "대화, 서비스, 선물, 공격 중 하나를 선택하세요.\n(Esc로 나가기)",
                        60f);
                    break;
            }
        }

        private static void ClearChildren(
            Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(
                    parent.GetChild(i).gameObject);
            }
        }

        // ===================== 서비스 메뉴 =====================

        private void DrawServiceMenu()
        {
            NpcServiceType services =
                openNpc.Definition.ServiceTypes;

            float y =
                0f;

            if ((services & NpcServiceType.Trade) != 0)
            {
                CreateMenuButton(
                    y,
                    "상점",
                    () =>
                    {
                        statusText =
                            string.Empty;

                        serviceScreen =
                            ServiceScreen.Shop;

                        RefreshMainArea();
                    });

                y -=
                    RowHeight + RowGap;
            }

            if ((services & NpcServiceType.Healing) != 0)
            {
                CreateMenuButton(
                    y,
                    "회복",
                    () =>
                    {
                        statusText =
                            string.Empty;

                        serviceScreen =
                            ServiceScreen.Heal;

                        RefreshMainArea();
                    });

                y -=
                    RowHeight + RowGap;
            }

            if ((services
                    & (NpcServiceType.MapInformation
                        | NpcServiceType.ExplorationInformation))
                != 0)
            {
                CreateMenuButton(
                    y,
                    "정보",
                    () =>
                    {
                        statusText =
                            string.Empty;

                        serviceScreen =
                            ServiceScreen.Info;

                        RefreshMainArea();
                    });

                y -=
                    RowHeight + RowGap;
            }

            if ((services
                    & (NpcServiceType.RelicTrade
                        | NpcServiceType.RelicResearch))
                != 0)
            {
                CreateMenuButton(
                    y,
                    "유물 정리",
                    () =>
                    {
                        statusText =
                            string.Empty;

                        serviceScreen =
                            ServiceScreen.Relic;

                        RefreshMainArea();
                    });

                y -=
                    RowHeight + RowGap;
            }

            CreateBackButton(
                y - 8f,
                ServiceScreen.None);
        }

        private void CreateMenuButton(
            float y,
            string label,
            UnityEngine.Events.UnityAction onClick)
        {
            CreateRow(
                mainContentRoot,
                y,
                RowHeight,
                MainAreaWidth - 32f,
                label,
                onClick);
        }

        // ===================== 상점 =====================

        private void DrawShopScreen()
        {
            ShopRunState shop =
                openNpc.ServiceState?.Shop;

            float y =
                0f;

            CreateShopHeader(
                y);

            y -=
                30f + RowGap;

            CreateTabButton(
                0f,
                y,
                "구매",
                shopTab == ShopTab.Buy,
                () =>
                {
                    shopTab =
                        ShopTab.Buy;

                    RefreshMainArea();
                });

            CreateTabButton(
                150f,
                y,
                "판매",
                shopTab == ShopTab.Sell,
                () =>
                {
                    shopTab =
                        ShopTab.Sell;

                    RefreshMainArea();
                });

            y -=
                30f + RowGap;

            if (shopTab == ShopTab.Buy)
            {
                y =
                    DrawShopCategoryRow(
                        y);

                DrawBuyList(
                    shop,
                    y);
            }
            else
            {
                DrawSellList(
                    y);
            }

            CreateBackButton(
                -(PanelHeight - 24f - RowHeight - 8f),
                ServiceScreen.Menu);
        }

        private void CreateShopHeader(
            float y)
        {
            int gold =
                RunContext.Current?.Player.Gold
                ?? 0;

            string summary =
                BuildShopUpgradeSummary();

            string headerText =
                string.IsNullOrEmpty(summary)
                    ? $"보유 골드  {gold} G"
                    : $"보유 골드  {gold} G      {summary}";

            CreateLabel(
                mainContentRoot,
                y,
                headerText,
                26f);
        }

        // 130일차: 상점 강화(할인율·판매가 보너스)가 적용 중이면 그대로 보여준다.
        private string BuildShopUpgradeSummary()
        {
            if (ApplicationFlow.Current == null)
            {
                return string.Empty;
            }

            ShopUpgradeSnapshot upgrade =
                ApplicationFlow.Current.GetShopUpgradeSnapshot();

            int sellPercent =
                (int)System.Math.Round(
                    ApplicationFlow.Current.GetShopSellPriceRatio()
                    * 100.0);

            if (upgrade.DiscountPercent <= 0
                && sellPercent <= 50)
            {
                return string.Empty;
            }

            return $"강화 적용 중 - 구매 할인 {upgrade.DiscountPercent}% / 판매가 {sellPercent}%";
        }

        private float DrawShopCategoryRow(
            float y)
        {
            float x =
                0f;

            const float categoryButtonWidth =
                110f;

            CreateTabButton(
                x,
                y,
                "전체",
                shopCategoryFilter == null,
                () =>
                {
                    shopCategoryFilter =
                        null;

                    RefreshMainArea();
                },
                categoryButtonWidth);

            x +=
                categoryButtonWidth + 6f;

            for (int i = 0; i < ShopFilterableCategories.Length; i++)
            {
                ItemCategory category =
                    ShopFilterableCategories[i];

                CreateTabButton(
                    x,
                    y,
                    ItemCategoryRules.GetDisplayName(
                        category),
                    shopCategoryFilter == category,
                    () =>
                    {
                        shopCategoryFilter =
                            category;

                        RefreshMainArea();
                    },
                    categoryButtonWidth);

                x +=
                    categoryButtonWidth + 6f;
            }

            return y - 26f - RowGap;
        }

        private void DrawBuyList(
            ShopRunState shop,
            float startY)
        {
            List<(string label, UnityEngine.Events.UnityAction onClick)> items =
                new List<(string, UnityEngine.Events.UnityAction)>();

            if (shop != null)
            {
                for (int i = 0; i < shop.Products.Count; i++)
                {
                    ShopProductState product =
                        shop.Products[i];

                    if (product == null
                        || !PassesShopCategoryFilter(
                            product))
                    {
                        continue;
                    }

                    int capturedIndex =
                        i;

                    items.Add(
                        (
                            $"{product.DisplayName}  -  {product.Price} G",
                            () => BuyProduct(
                                shop,
                                capturedIndex)
                        ));
                }
            }

            DrawScrollList(
                startY,
                items,
                "구매",
                shop == null || shop.Products.Count == 0
                    ? "판매할 물건이 없습니다."
                    : "이 분류에 해당하는 물건이 없습니다.");
        }

        private bool PassesShopCategoryFilter(
            ShopProductState product)
        {
            if (shopCategoryFilter == null)
            {
                return true;
            }

            return RuntimeItemDefinitionLookup.TryFind(
                    product.ItemId,
                    out ItemDefinition definition)
                && definition.Category == shopCategoryFilter;
        }

        private void BuyProduct(
            ShopRunState shop,
            int index)
        {
            RunContext context =
                RunContext.Current;

            if (context == null)
            {
                statusText =
                    "지금은 구매할 수 없습니다.";
            }
            else
            {
                ShopActionResult result =
                    ShopService.Buy(
                        shop,
                        context.Inventory,
                        context.Player,
                        index);

                statusText =
                    result.Success
                        ? $"구매했습니다. ({-result.GoldChange} G 소비)"
                        : DescribeShopFailure(
                            result.FailureReason);
            }

            RefreshLeftColumn();
            RefreshMainArea();
        }

        private void DrawSellList(
            float startY)
        {
            InventoryRunState inventory =
                RunContext.Current?.Inventory;

            double sellPriceRatio =
                ApplicationFlow.Current != null
                    ? ApplicationFlow.Current.GetShopSellPriceRatio()
                    : ShopService.DefaultSellPriceRatio;

            List<(string label, UnityEngine.Events.UnityAction onClick)> items =
                new List<(string, UnityEngine.Events.UnityAction)>();

            if (inventory != null)
            {
                for (int slotIndex = 0; slotIndex < inventory.Slots.Count; slotIndex++)
                {
                    InventorySlotState slot =
                        inventory.Slots[slotIndex];

                    if (slot == null
                        || slot.IsEmpty
                        || !RuntimeItemDefinitionLookup.TryFind(
                            slot.ItemId,
                            out ItemDefinition definition)
                        || !ItemCategoryRules.CanSell(
                            definition.Category))
                    {
                        continue;
                    }

                    int sellPrice =
                        (int)(definition.BasePrice
                        * sellPriceRatio);

                    int capturedSlotIndex =
                        slotIndex;

                    ItemDefinition capturedDefinition =
                        definition;

                    items.Add(
                        (
                            $"{definition.DisplayName} ×{slot.Quantity}  -  {sellPrice} G",
                            () => SellSlot(
                                inventory,
                                capturedSlotIndex,
                                capturedDefinition)
                        ));
                }
            }

            DrawScrollList(
                startY,
                items,
                "판매",
                "팔 수 있는 물건이 없습니다.");
        }

        private void SellSlot(
            InventoryRunState inventory,
            int slotIndex,
            ItemDefinition definition)
        {
            RunContext context =
                RunContext.Current;

            if (context == null)
            {
                statusText =
                    "지금은 판매할 수 없습니다.";
            }
            else
            {
                ShopActionResult result =
                    ShopInteractionService.Sell(
                        inventory,
                        context.Player,
                        slotIndex,
                        definition);

                statusText =
                    result.Success
                        ? $"판매했습니다. (+{result.GoldChange} G)"
                        : DescribeShopFailure(
                            result.FailureReason);
            }

            RefreshLeftColumn();
            RefreshMainArea();
        }

        private static string DescribeShopFailure(
            ShopActionFailureReason reason)
        {
            switch (reason)
            {
                case ShopActionFailureReason.NotEnoughGold:
                    return "골드가 부족합니다.";

                case ShopActionFailureReason.InventoryFull:
                    return "인벤토리에 공간이 없습니다.";

                case ShopActionFailureReason.ItemNotSellable:
                    return "판매할 수 없는 아이템입니다.";

                case ShopActionFailureReason.InvalidSlot:
                    return "이미 사라진 아이템입니다.";

                default:
                    return "처리할 수 없습니다.";
            }
        }

        // ===================== 회복 / 정보 =====================

        private void DrawHealScreen()
        {
            float y =
                0f;

            CreateServiceHeader(
                y);

            y -=
                26f + RowGap;

            PlayerRunState player =
                RunContext.Current?.Player;

            if (player != null)
            {
                StatBlock finalStats =
                    player.GetFinalStats();

                CreateLabel(
                    mainContentRoot,
                    y,
                    $"체력 {player.CurrentHp}/{finalStats.MaxHealth}   마나 {player.CurrentMana}/{finalStats.MaxMana}   정력 {player.CurrentStamina}/{finalStats.MaxStamina}",
                    24f);
            }

            y -=
                24f + RowGap;

            CreateRow(
                mainContentRoot,
                y,
                RowHeight,
                MainAreaWidth - 32f,
                $"회복하기 ({HealCost} G)",
                () =>
                {
                    NpcServiceActionResult result =
                        NpcHealingService.Heal(
                            player,
                            HealCost);

                    statusText =
                        result.Success
                            ? $"체력·마나·정력을 모두 회복했습니다. ({-result.GoldChange} G 소비)"
                            : DescribeServiceFailure(
                                result.FailureReason);

                    RefreshLeftColumn();
                    RefreshMainArea();
                });

            CreateBackButton(
                y - RowHeight - 8f,
                ServiceScreen.Menu);
        }

        private void DrawInfoScreen()
        {
            float y =
                0f;

            CreateServiceHeader(
                y);

            y -=
                26f + RowGap;

            DungeonRunState dungeon =
                RunContext.Current?.Dungeon;

            if (dungeon == null)
            {
                CreateLabel(
                    mainContentRoot,
                    y,
                    "정보를 불러올 수 없습니다.",
                    24f);

                y -=
                    24f;
            }
            else
            {
                int visited = 0;
                int combatCount = 0;
                int trapCount = 0;
                int eventCount = 0;
                int normalCount = 0;

                foreach (RoomInstance room in dungeon.AllRooms)
                {
                    if (room == null
                        || !room.Visited)
                    {
                        continue;
                    }

                    visited++;

                    switch (room.RoomType)
                    {
                        case RoomType.Combat:
                            combatCount++;
                            break;

                        case RoomType.Trap:
                            trapCount++;
                            break;

                        case RoomType.Event:
                            eventCount++;
                            break;

                        default:
                            normalCount++;
                            break;
                    }
                }

                CreateLabel(
                    mainContentRoot,
                    y,
                    $"{dungeon.CurrentFloor}층 - 지금까지 둘러본 {visited}개 방 기준",
                    22f);

                y -=
                    22f;

                CreateLabel(
                    mainContentRoot,
                    y,
                    $"전투 {combatCount} / 함정 {trapCount} / 이벤트 {eventCount} / 일반 {normalCount}",
                    22f);

                y -=
                    22f;

                CreateLabel(
                    mainContentRoot,
                    y,
                    "가보지 않은 방의 정보는 알려줄 수 없습니다.",
                    22f);

                y -=
                    22f;
            }

            CreateBackButton(
                y - 8f,
                ServiceScreen.Menu);
        }

        private void CreateServiceHeader(
            float y)
        {
            int gold =
                RunContext.Current?.Player.Gold
                ?? 0;

            CreateLabel(
                mainContentRoot,
                y,
                $"보유 골드  {gold} G",
                26f);
        }

        // ===================== 유물 정리 =====================

        private void DrawRelicScreen()
        {
            float y =
                0f;

            CreateServiceHeader(
                y);

            y -=
                30f + RowGap;

            RelicRunState relics =
                RunContext.Current?.Relics;

            List<(string label, UnityEngine.Events.UnityAction onClick)> items =
                new List<(string, UnityEngine.Events.UnityAction)>();

            if (relics != null)
            {
                for (int i = 0; i < relics.Relics.Count; i++)
                {
                    RelicInstanceState relic =
                        relics.Relics[i];

                    if (relic == null)
                    {
                        continue;
                    }

                    RelicInstanceState capturedRelic =
                        relic;

                    if (relic.IsCursed)
                    {
                        items.Add(
                            (
                                $"{relic.DisplayName} (저주)  -  저주 제거 {CurseRemovalCost} G",
                                () => RemoveCurse(
                                    relics,
                                    capturedRelic)
                            ));
                    }
                    else
                    {
                        items.Add(
                            (
                                $"{relic.DisplayName}  -  희생 +{SacrificeReward} G",
                                () => SacrificeRelic(
                                    relics,
                                    capturedRelic)
                            ));
                    }
                }
            }

            DrawScrollList(
                y,
                items,
                "실행",
                "정리할 유물이 없습니다.");

            CreateBackButton(
                -(PanelHeight - 24f - RowHeight - 8f),
                ServiceScreen.Menu);
        }

        private void RemoveCurse(
            RelicRunState relics,
            RelicInstanceState relic)
        {
            NpcServiceActionResult result =
                NpcRelicService.RemoveCursedRelic(
                    relics,
                    RunContext.Current.Player,
                    relic.RelicId,
                    CurseRemovalCost);

            statusText =
                result.Success
                    ? $"{relic.DisplayName}의 저주를 제거했습니다."
                    : DescribeServiceFailure(
                        result.FailureReason);

            RefreshLeftColumn();
            RefreshMainArea();
        }

        private void SacrificeRelic(
            RelicRunState relics,
            RelicInstanceState relic)
        {
            NpcServiceActionResult result =
                NpcRelicService.SacrificeRelic(
                    relics,
                    RunContext.Current.Player,
                    relic.RelicId,
                    SacrificeReward);

            statusText =
                result.Success
                    ? $"{relic.DisplayName}을(를) 희생하고 {result.GoldChange} G를 받았습니다."
                    : DescribeServiceFailure(
                        result.FailureReason);

            RefreshLeftColumn();
            RefreshMainArea();
        }

        private static string DescribeServiceFailure(
            NpcServiceFailureReason reason)
        {
            switch (reason)
            {
                case NpcServiceFailureReason.NotEnoughGold:
                    return "골드가 부족합니다.";

                case NpcServiceFailureReason.AlreadyFull:
                    return "이미 체력·마나·정력이 가득 찼습니다.";

                case NpcServiceFailureReason.RelicNotFound:
                    return "해당 유물을 찾을 수 없습니다.";

                case NpcServiceFailureReason.RelicNotCursed:
                    return "저주가 없는 유물입니다.";

                default:
                    return "지금은 처리할 수 없습니다.";
            }
        }

        // ===================== 선물 =====================

        private void DrawGiftScreen()
        {
            InventoryRunState inventory =
                RunContext.Current?.Inventory;

            List<(string label, UnityEngine.Events.UnityAction onClick)> items =
                new List<(string, UnityEngine.Events.UnityAction)>();

            if (inventory != null)
            {
                for (int slotIndex = 0; slotIndex < inventory.Slots.Count; slotIndex++)
                {
                    InventorySlotState slot =
                        inventory.Slots[slotIndex];

                    if (slot == null
                        || slot.IsEmpty)
                    {
                        continue;
                    }

                    int capturedSlotIndex =
                        slotIndex;

                    string capturedDisplayName =
                        slot.DisplayName;

                    items.Add(
                        (
                            $"{slot.DisplayName} ×{slot.Quantity}",
                            () => GiveGift(
                                inventory,
                                capturedSlotIndex,
                                capturedDisplayName)
                        ));
                }
            }

            DrawScrollList(
                0f,
                items,
                "선물하기",
                "선물할 아이템이 없습니다.");

            CreateBackButton(
                -(PanelHeight - 24f - RowHeight - 8f),
                ServiceScreen.None);
        }

        private void GiveGift(
            InventoryRunState inventory,
            int slotIndex,
            string itemDisplayName)
        {
            if (inventory == null
                || !inventory.TryRemoveQuantityAt(
                    slotIndex,
                    1,
                    out int removedQuantity)
                || removedQuantity != 1)
            {
                statusText =
                    "선물을 줄 수 없습니다.";

                RefreshMainArea();
                return;
            }

            ApplyResult(
                interactionService.ResolveGift(
                    openNpc.RelationshipState,
                    itemDisplayName,
                    GiftAffinityGain));

            RefreshLeftColumn();
            RefreshMainArea();
        }

        // ===================== 공용 그리기 헬퍼 =====================

        private void DrawScrollList(
            float startY,
            List<(string label, UnityEngine.Events.UnityAction onClick)> items,
            string buttonLabel,
            string emptyMessage)
        {
            RectTransform viewportRect =
                RuntimeUiFactory.CreateUiObject(
                    "ScrollViewport",
                    mainContentRoot);

            viewportRect.anchorMin =
                new Vector2(0f, 1f);

            viewportRect.anchorMax =
                new Vector2(0f, 1f);

            viewportRect.pivot =
                new Vector2(0f, 1f);

            viewportRect.anchoredPosition =
                new Vector2(0f, startY);

            viewportRect.sizeDelta =
                new Vector2(MainAreaWidth - 32f, ListHeight);

            viewportRect.gameObject.AddComponent<RectMask2D>();

            RectTransform contentRect =
                RuntimeUiFactory.CreateUiObject(
                    "ScrollContent",
                    viewportRect);

            contentRect.anchorMin =
                new Vector2(0f, 1f);

            contentRect.anchorMax =
                new Vector2(1f, 1f);

            contentRect.pivot =
                new Vector2(0.5f, 1f);

            contentRect.anchoredPosition =
                Vector2.zero;

            contentRect.sizeDelta =
                new Vector2(0f, Mathf.Max(items.Count, 1) * (RowHeight + RowGap));

            ScrollRect scrollRect =
                viewportRect.gameObject.AddComponent<ScrollRect>();

            scrollRect.viewport =
                viewportRect;

            scrollRect.content =
                contentRect;

            scrollRect.horizontal =
                false;

            scrollRect.vertical =
                true;

            if (items.Count == 0)
            {
                CreateLabel(
                    contentRect,
                    0f,
                    emptyMessage,
                    RowHeight);

                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                float rowY =
                    -(i * (RowHeight + RowGap));

                CreateRow(
                    contentRect,
                    rowY,
                    RowHeight,
                    MainAreaWidth - 32f,
                    items[i].label,
                    items[i].onClick,
                    buttonLabel);
            }
        }

        // 라벨 + 오른쪽 버튼 한 줄. buttonLabel이 null이면 행 전체가 버튼(메뉴용).
        private void CreateRow(
            Transform parent,
            float y,
            float height,
            float width,
            string label,
            UnityEngine.Events.UnityAction onClick,
            string buttonLabel = null)
        {
            if (buttonLabel == null)
            {
                RectTransform fullButtonRect =
                    RuntimeUiFactory.CreateUiObject(
                        "MenuRow",
                        parent);

                fullButtonRect.anchorMin =
                    new Vector2(0f, 1f);

                fullButtonRect.anchorMax =
                    new Vector2(0f, 1f);

                fullButtonRect.pivot =
                    new Vector2(0f, 1f);

                fullButtonRect.anchoredPosition =
                    new Vector2(0f, y);

                fullButtonRect.sizeDelta =
                    new Vector2(width, height);

                Image fullButtonImage =
                    fullButtonRect.gameObject.AddComponent<Image>();

                fullButtonImage.color =
                    NormalButtonColor;

                Button fullButton =
                    fullButtonRect.gameObject.AddComponent<Button>();

                fullButton.targetGraphic =
                    fullButtonImage;

                fullButton.onClick.AddListener(
                    onClick);

                RectTransform fullLabelRect =
                    RuntimeUiFactory.CreateStretchedRect(
                        "Label",
                        fullButtonRect);

                Text fullLabelText =
                    fullLabelRect.gameObject.AddComponent<Text>();

                RuntimeUiFactory.ConfigureText(
                    fullLabelText,
                    label,
                    16,
                    FontStyle.Normal,
                    TextAnchor.MiddleCenter);

                fullLabelText.raycastTarget =
                    false;

                return;
            }

            float actionButtonWidth =
                170f;

            RectTransform rowRect =
                RuntimeUiFactory.CreateUiObject(
                    "Row",
                    parent);

            rowRect.anchorMin =
                new Vector2(0f, 1f);

            rowRect.anchorMax =
                new Vector2(0f, 1f);

            rowRect.pivot =
                new Vector2(0f, 1f);

            rowRect.anchoredPosition =
                new Vector2(0f, y);

            rowRect.sizeDelta =
                new Vector2(width, height);

            Image rowImage =
                rowRect.gameObject.AddComponent<Image>();

            rowImage.color =
                new Color(0.14f, 0.14f, 0.18f, 0.9f);

            RectTransform labelRect =
                RuntimeUiFactory.CreateUiObject(
                    "Label",
                    rowRect);

            labelRect.anchorMin =
                new Vector2(0f, 0f);

            labelRect.anchorMax =
                new Vector2(0f, 1f);

            labelRect.pivot =
                new Vector2(0f, 0.5f);

            labelRect.anchoredPosition =
                new Vector2(10f, 0f);

            labelRect.sizeDelta =
                new Vector2(width - actionButtonWidth - 20f, 0f);

            Text labelText =
                labelRect.gameObject.AddComponent<Text>();

            RuntimeUiFactory.ConfigureText(
                labelText,
                label,
                14,
                FontStyle.Normal,
                TextAnchor.MiddleLeft);

            RectTransform actionButtonRect =
                RuntimeUiFactory.CreateUiObject(
                    "Action",
                    rowRect);

            actionButtonRect.anchorMin =
                new Vector2(1f, 0.5f);

            actionButtonRect.anchorMax =
                new Vector2(1f, 0.5f);

            actionButtonRect.pivot =
                new Vector2(1f, 0.5f);

            actionButtonRect.anchoredPosition =
                new Vector2(-6f, 0f);

            actionButtonRect.sizeDelta =
                new Vector2(actionButtonWidth, height - 6f);

            Image actionButtonImage =
                actionButtonRect.gameObject.AddComponent<Image>();

            actionButtonImage.color =
                NormalButtonColor;

            Button actionButton =
                actionButtonRect.gameObject.AddComponent<Button>();

            actionButton.targetGraphic =
                actionButtonImage;

            actionButton.onClick.AddListener(
                onClick);

            RectTransform actionLabelRect =
                RuntimeUiFactory.CreateStretchedRect(
                    "Label",
                    actionButtonRect);

            Text actionLabelText =
                actionLabelRect.gameObject.AddComponent<Text>();

            RuntimeUiFactory.ConfigureText(
                actionLabelText,
                buttonLabel,
                13,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);

            actionLabelText.raycastTarget =
                false;
        }

        private void CreateTabButton(
            float x,
            float y,
            string label,
            bool isSelected,
            UnityEngine.Events.UnityAction onClick,
            float width = 130f)
        {
            RectTransform buttonRect =
                RuntimeUiFactory.CreateUiObject(
                    $"Tab_{label}",
                    mainContentRoot);

            buttonRect.anchorMin =
                new Vector2(0f, 1f);

            buttonRect.anchorMax =
                new Vector2(0f, 1f);

            buttonRect.pivot =
                new Vector2(0f, 1f);

            buttonRect.anchoredPosition =
                new Vector2(x, y);

            buttonRect.sizeDelta =
                new Vector2(width, 26f);

            Image buttonImage =
                buttonRect.gameObject.AddComponent<Image>();

            buttonImage.color =
                isSelected
                    ? SelectedButtonColor
                    : NormalButtonColor;

            Button button =
                buttonRect.gameObject.AddComponent<Button>();

            button.targetGraphic =
                buttonImage;

            button.onClick.AddListener(
                onClick);

            RectTransform labelRect =
                RuntimeUiFactory.CreateStretchedRect(
                    "Label",
                    buttonRect);

            Text labelText =
                labelRect.gameObject.AddComponent<Text>();

            RuntimeUiFactory.ConfigureText(
                labelText,
                label,
                13,
                isSelected
                    ? FontStyle.Bold
                    : FontStyle.Normal,
                TextAnchor.MiddleCenter);

            labelText.raycastTarget =
                false;
        }

        private void CreateLabel(
            Transform parent,
            float y,
            string text,
            float height)
        {
            RectTransform labelRect =
                RuntimeUiFactory.CreateUiObject(
                    "Label",
                    parent);

            labelRect.anchorMin =
                new Vector2(0f, 1f);

            labelRect.anchorMax =
                new Vector2(0f, 1f);

            labelRect.pivot =
                new Vector2(0f, 1f);

            labelRect.anchoredPosition =
                new Vector2(0f, y);

            labelRect.sizeDelta =
                new Vector2(MainAreaWidth - 32f, height);

            Text labelText =
                labelRect.gameObject.AddComponent<Text>();

            RuntimeUiFactory.ConfigureText(
                labelText,
                text,
                14,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
        }

        private void CreateBackButton(
            float y,
            ServiceScreen target)
        {
            CreateRow(
                mainContentRoot,
                y,
                RowHeight,
                MainAreaWidth - 32f,
                "뒤로",
                () =>
                {
                    serviceScreen =
                        target;

                    statusText =
                        string.Empty;

                    RefreshLeftColumn();
                    RefreshMainArea();
                });
        }

        private void ApplyResult(
            NpcInteractionResult result)
        {
            if (result == null)
            {
                return;
            }

            statusText =
                result.Message;

            if (result.ResultType == NpcInteractionResultType.ReturnToExploration)
            {
                CloseInteraction();
            }
        }

        private void OnDisable()
        {
            if (isPanelOpen)
            {
                CloseInteraction();
            }
        }
    }
}
