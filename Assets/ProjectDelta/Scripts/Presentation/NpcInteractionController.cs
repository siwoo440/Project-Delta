using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using DungeonRunState = ProjectDelta.Domain.DungeonRunState; // Data/Domain 동명 타입 충돌 방지
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectDelta.Presentation
{
    // 113일차: 정면 한 칸의 NPC를 F로 선택하고 대화/서비스/떠나기 공통 흐름을 제공한다.
    // 114일차: "서비스"를 눌렀을 때 실제 상점·회복·정보·유물 정리 화면으로 이어지게 한다.
    // 115일차: 선물·구조·공격(적대 전환+전투)을 추가하고, 화면을 캐릭터 일러스트(상단)
    // + 대화창(하단 좌)·선택지 버튼(하단 우) 구성으로 다시 짰다.
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

        private const int HealCost = 15;
        private const int CurseRemovalCost = 20;
        private const int SacrificeReward = 10;
        private const int GiftAffinityGain = 10;
        private const int RescueAffinityGain = 20;

        // 115일차 UI 레이아웃 상수 - 상단 일러스트 / 하단 좌 대화창 / 하단 우 선택지.
        // 130일차: 왼쪽 열(일러스트+정보+행동 버튼)과 오른쪽 열(서비스 내용)로 나뉜
        // 레이아웃 - 참고 이미지의 "초상화+서비스 목록 / 메인 콘텐츠" 2단 구성을 따른다.
        private const float IllustrationSize = 240f;
        private const float LeftColumnWidth = 320f;
        private const float MainAreaWidth = 620f;
        private const float PanelHeight = 520f;
        private const float ActionButtonHeight = 36f;
        private const float PanelGap = 14f;

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

        // 130일차: 상점 목록이 길어질 수 있어 구매/판매를 각각 스크롤 영역으로 감싼다.
        private Vector2 shopBuyScroll;
        private Vector2 shopSellScroll;

        // 130일차: 유물 정리 화면도 상인 화면과 같은 스크롤 목록 스타일을 쓴다.
        private Vector2 relicScreenScroll;

        private const float ShopListHeight = 280f;

        private enum ShopTab
        {
            Buy,
            Sell
        }

        private ShopTab shopTab =
            ShopTab.Buy;

        // 130일차: 구매 탭 카테고리 필터 - null이면 "전체".
        private ItemCategory? shopCategoryFilter;

        private static readonly ItemCategory[] ShopFilterableCategories =
        {
            ItemCategory.Consumable,
            ItemCategory.ExplorationTool,
            ItemCategory.Equipment,
            ItemCategory.Relic
        };

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
                promptText =
                    string.Empty;

                return;
            }

            NpcContentMarker npc =
                FindNpcInFront();

            promptText =
                npc != null
                    ? $"{npc.Definition?.DisplayName ?? "NPC"} 대화 [F]"
                    : string.Empty;

            if (npc != null
                && Keyboard.current != null
                && Keyboard.current.fKey.wasPressedThisFrame)
            {
                OpenInteraction(
                    npc);
            }
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

            promptText =
                string.Empty;

            statusText =
                "무엇을 할지 선택하세요.";

            serviceScreen =
                ServiceScreen.None;

            openNpc.RelationshipState.RegisterEncounter();

            // 130일차: 대화 UI가 화면을 크게 차지하는 동안 뒤에 있는 NPC 3D 모델(캡슐)이
            // 겹쳐 보이지 않도록 렌더러만 끈다 - GameObject 자체를 끄면 이 컨트롤러가
            // 들고 있는 openNpc(NpcContentMarker)까지 함께 비활성화돼버리므로 안 된다.
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

        private void OnGUI()
        {
            if (!isPanelOpen)
            {
                DrawPrompt();
                return;
            }

            DrawInteractionPanel();
        }

        private void DrawPrompt()
        {
            if (string.IsNullOrEmpty(promptText))
            {
                return;
            }

            GUIStyle promptStyle =
                new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    fontStyle = FontStyle.Bold
                };

            promptStyle.normal.textColor =
                Color.white;

            Rect promptRect =
                new Rect(
                    (Screen.width - 360f) * 0.5f,
                    Screen.height - 92f,
                    360f,
                    34f);

            GUI.Label(
                promptRect,
                promptText,
                promptStyle);
        }

        private void DrawInteractionPanel()
        {
            if (openNpc == null
                || openNpc.Definition == null
                || openNpc.RelationshipState == null)
            {
                CloseInteraction();
                return;
            }

            float totalWidth =
                LeftColumnWidth
                + PanelGap
                + MainAreaWidth;

            float panelX =
                (Screen.width - totalWidth) * 0.5f;

            float panelY =
                (Screen.height - PanelHeight) * 0.5f;

            Rect leftColumnRect =
                new Rect(
                    panelX,
                    panelY,
                    LeftColumnWidth,
                    PanelHeight);

            Rect mainAreaRect =
                new Rect(
                    leftColumnRect.xMax + PanelGap,
                    panelY,
                    MainAreaWidth,
                    PanelHeight);

            DrawLeftColumn(
                leftColumnRect);

            DrawMainArea(
                mainAreaRect);
        }

        // 130일차: 초상화·이름/호감도·"○○ 서비스" 행동 버튼(대화·서비스·선물·공격)을
        // 왼쪽 열에 묶어서 항상 보이게 한다 - 참고 이미지의 좌측 패널 구성.
        // 어떤 서비스 화면(상점·회복 등)이 열려 있든 이 버튼들은 그대로 유지된다.
        private void DrawLeftColumn(
            Rect rect)
        {
            Rect illustrationRect =
                new Rect(
                    rect.x,
                    rect.y,
                    rect.width,
                    IllustrationSize);

            DrawCharacterIllustration(
                illustrationRect);

            float infoY =
                illustrationRect.yMax
                + PanelGap;

            GUIStyle headerStyle =
                new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    wordWrap = true
                };

            headerStyle.normal.textColor =
                Color.white;

            GUI.Label(
                new Rect(
                    rect.x,
                    infoY,
                    rect.width,
                    54f),
                $"호감도 {openNpc.RelationshipState.Affinity}/100 ({openNpc.RelationshipState.Stage})   |   {(openNpc.RelationshipState.IsHostile ? "적대" : "우호")}\n"
                + (string.IsNullOrEmpty(statusText)
                    ? "무엇을 할지 선택하세요."
                    : statusText),
                headerStyle);

            float servicesLabelY =
                infoY
                + 54f
                + 4f;

            GUIStyle servicesLabelStyle =
                new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold
                };

            servicesLabelStyle.normal.textColor =
                new Color(0.85f, 0.72f, 0.35f);

            GUI.Label(
                new Rect(
                    rect.x,
                    servicesLabelY,
                    rect.width,
                    22f),
                $"{openNpc.Definition.DisplayName} 서비스",
                servicesLabelStyle);

            float buttonsY =
                servicesLabelY
                + 22f
                + 4f;

            GUILayout.BeginArea(
                new Rect(
                    rect.x,
                    buttonsY,
                    rect.width,
                    rect.yMax - buttonsY));

            DrawMainButtons();

            GUILayout.EndArea();
        }

        // 130일차: 오른쪽 넓은 영역 - serviceScreen에 따라 상점/회복/정보/유물 정리
        // 화면을 그대로 보여준다. None이면 대화 상태만 알려주는 안내문을 둔다.
        private void DrawMainArea(
            Rect rect)
        {
            GUI.Box(
                rect,
                string.Empty);

            GUILayout.BeginArea(
                new Rect(
                    rect.x + 16f,
                    rect.y + 12f,
                    rect.width - 32f,
                    rect.height - 24f));

            if (serviceScreen == ServiceScreen.None)
            {
                GUILayout.Label(
                    "대화, 서비스, 선물, 공격 중 하나를 선택하세요.\n(Esc로 나가기)");
            }
            else
            {
                DrawServiceScreen();
            }

            GUILayout.EndArea();
        }

        // 115일차: 실제 캐릭터 일러스트 자산이 아직 없어서 역할별 색상 + 이름으로
        // 자리를 대신한다 - 나중에 진짜 일러스트가 생기면 이 메서드만 바꾸면 된다.
        private void DrawCharacterIllustration(
            Rect rect)
        {
            GUI.Box(
                rect,
                string.Empty);

            Color previousColor =
                GUI.color;

            GUI.color =
                GetIllustrationColor(
                    openNpc.Definition.ServiceTypes);

            GUI.DrawTexture(
                new Rect(
                    rect.x + 5f,
                    rect.y + 5f,
                    rect.width - 10f,
                    rect.height - 10f),
                Texture2D.whiteTexture);

            GUI.color =
                previousColor;

            GUIStyle nameStyle =
                new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.LowerCenter,
                    fontSize = 24,
                    fontStyle = FontStyle.Bold
                };

            nameStyle.normal.textColor =
                Color.white;

            GUI.Label(
                new Rect(
                    rect.x,
                    rect.y,
                    rect.width,
                    rect.height - 14f),
                openNpc.Definition.DisplayName,
                nameStyle);
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

        private void DrawMainButtons()
        {
            if (GUILayout.Button(
                    "대화",
                    GUILayout.Height(36f)))
            {
                ApplyResult(
                    interactionService.Resolve(
                        openNpc.Definition,
                        openNpc.RelationshipState,
                        NpcInteractionCommand.Talk));
            }

            GUI.enabled =
                openNpc.Definition.ServiceTypes != NpcServiceType.None;

            if (GUILayout.Button(
                    "서비스",
                    GUILayout.Height(36f)))
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
                }
            }

            GUI.enabled =
                true;

            if (GUILayout.Button(
                    "선물",
                    GUILayout.Height(36f)))
            {
                statusText =
                    string.Empty;

                serviceScreen =
                    ServiceScreen.Gift;
            }

            GUI.enabled =
                openNpc.Definition.CanBattle;

            if (GUILayout.Button(
                    "공격",
                    GUILayout.Height(36f)))
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
            }

            GUI.enabled =
                true;
        }

        // 115일차: 적대 전환 즉시 이 NPC를 기존 몬스터 조우 파이프라인에 태운다.
        // NpcDefinition 고유 능력치 대신 fallback 몬스터 스탯이 쓰일 수 있다는 한계는
        // ExplorationMonsterEncounterController.ResolveMonsterDefinition의 기존 fallback
        // 규칙을 그대로 물려받는다 - NPC 전용 능력치 반영은 다음 단계 과제로 남긴다.
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

        private void DrawServiceScreen()
        {
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

                default:
                    DrawServiceMenu();
                    break;
            }
        }

        private void DrawGiftScreen()
        {
            InventoryRunState inventory =
                RunContext.Current?.Inventory;

            bool hasGiftableSlot =
                false;

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

                    hasGiftableSlot =
                        true;

                    if (GUILayout.Button(
                            $"{slot.DisplayName} ×{slot.Quantity}  [선물하기]",
                            GUILayout.Height(30f)))
                    {
                        GiveGift(
                            inventory,
                            slotIndex,
                            slot.DisplayName);
                    }
                }
            }

            if (!hasGiftableSlot)
            {
                GUILayout.Label(
                    "선물할 아이템이 없습니다.");
            }

            DrawBackButton(
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

                return;
            }

            ApplyResult(
                interactionService.ResolveGift(
                    openNpc.RelationshipState,
                    itemDisplayName,
                    GiftAffinityGain));
        }

        private void DrawServiceMenu()
        {
            NpcServiceType services =
                openNpc.Definition.ServiceTypes;

            if ((services & NpcServiceType.Trade) != 0
                && GUILayout.Button(
                    "상점",
                    GUILayout.Height(34f)))
            {
                statusText =
                    string.Empty;

                serviceScreen =
                    ServiceScreen.Shop;
            }

            if ((services & NpcServiceType.Healing) != 0
                && GUILayout.Button(
                    "회복",
                    GUILayout.Height(34f)))
            {
                statusText =
                    string.Empty;

                serviceScreen =
                    ServiceScreen.Heal;
            }

            if ((services
                    & (NpcServiceType.MapInformation
                        | NpcServiceType.ExplorationInformation))
                != 0
                && GUILayout.Button(
                    "정보",
                    GUILayout.Height(34f)))
            {
                statusText =
                    string.Empty;

                serviceScreen =
                    ServiceScreen.Info;
            }

            if ((services
                    & (NpcServiceType.RelicTrade
                        | NpcServiceType.RelicResearch))
                != 0
                && GUILayout.Button(
                    "유물 정리",
                    GUILayout.Height(34f)))
            {
                statusText =
                    string.Empty;

                serviceScreen =
                    ServiceScreen.Relic;
            }

            GUILayout.Space(
                6f);

            if (GUILayout.Button(
                    "뒤로",
                    GUILayout.Height(30f)))
            {
                serviceScreen =
                    ServiceScreen.None;

                statusText =
                    "무엇을 할지 선택하세요.";
            }
        }

        private void DrawShopScreen()
        {
            ShopRunState shop =
                openNpc.ServiceState?.Shop;

            DrawShopHeader();

            GUILayout.BeginHorizontal();

            DrawShopTabButton(
                "구매",
                ShopTab.Buy);

            DrawShopTabButton(
                "판매",
                ShopTab.Sell);

            GUILayout.EndHorizontal();

            GUILayout.Space(
                6f);

            if (shopTab == ShopTab.Buy)
            {
                DrawShopCategoryRow();

                shopBuyScroll =
                    GUILayout.BeginScrollView(
                        shopBuyScroll,
                        GUILayout.Height(ShopListHeight));

                DrawBuyList(
                    shop);

                GUILayout.EndScrollView();
            }
            else
            {
                shopSellScroll =
                    GUILayout.BeginScrollView(
                        shopSellScroll,
                        GUILayout.Height(ShopListHeight));

                DrawSellList();

                GUILayout.EndScrollView();
            }

            DrawBackButton();
        }

        // 130일차: 참고 이미지 상단의 골드 표시 + 상점 강화 요약을 한 줄로 묶는다.
        // 130일차: 상인 화면에서 만든 "보유 골드 표시" 머리글을 회복·정보·유물 정리
        // 화면에도 그대로 재사용해 스타일을 통일한다.
        private void DrawServiceHeader()
        {
            GUILayout.BeginHorizontal();

            int gold =
                RunContext.Current?.Player.Gold
                ?? 0;

            GUILayout.Label(
                $"보유 골드  {gold} G",
                GUI.skin.box);

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }

        private void DrawShopHeader()
        {
            GUILayout.BeginHorizontal();

            int gold =
                RunContext.Current?.Player.Gold
                ?? 0;

            GUILayout.Label(
                $"보유 골드  {gold} G",
                GUI.skin.box);

            GUILayout.FlexibleSpace();

            DrawShopUpgradeSummary();

            GUILayout.EndHorizontal();
        }

        private void DrawShopTabButton(
            string label,
            ShopTab tab)
        {
            GUIStyle tabStyle =
                new GUIStyle(GUI.skin.button)
                {
                    fontStyle = shopTab == tab
                        ? FontStyle.Bold
                        : FontStyle.Normal
                };

            if (GUILayout.Button(
                    label,
                    tabStyle,
                    GUILayout.Height(30f)))
            {
                shopTab =
                    tab;
            }
        }

        // 130일차: 참고 이미지의 카테고리 아이콘 행 - 아이콘 자산이 없어 글자 버튼으로
        // 대신한다. ItemCategory 8종 중 상점에서 실제로 취급하는 4종만 후보로 둔다.
        private void DrawShopCategoryRow()
        {
            GUILayout.BeginHorizontal();

            GUIStyle allStyle =
                new GUIStyle(GUI.skin.button)
                {
                    fontStyle = shopCategoryFilter == null
                        ? FontStyle.Bold
                        : FontStyle.Normal
                };

            if (GUILayout.Button(
                    "전체",
                    allStyle,
                    GUILayout.Height(26f)))
            {
                shopCategoryFilter =
                    null;
            }

            for (int i = 0; i < ShopFilterableCategories.Length; i++)
            {
                ItemCategory category =
                    ShopFilterableCategories[i];

                GUIStyle categoryStyle =
                    new GUIStyle(GUI.skin.button)
                    {
                        fontStyle = shopCategoryFilter == category
                            ? FontStyle.Bold
                            : FontStyle.Normal
                    };

                if (GUILayout.Button(
                        ItemCategoryRules.GetDisplayName(
                            category),
                        categoryStyle,
                        GUILayout.Height(26f)))
                {
                    shopCategoryFilter =
                        category;
                }
            }

            GUILayout.EndHorizontal();
        }

        private void DrawBuyList(
            ShopRunState shop)
        {
            if (shop == null
                || shop.Products.Count == 0)
            {
                GUILayout.Label(
                    "판매할 물건이 없습니다.");

                return;
            }

            bool hasVisibleProduct =
                false;

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

                hasVisibleProduct =
                    true;

                if (GUILayout.Button(
                        $"{product.DisplayName}  -  {product.Price} G  [구매]",
                        GUILayout.Height(30f)))
                {
                    BuyProduct(
                        shop,
                        i);
                }
            }

            if (!hasVisibleProduct)
            {
                GUILayout.Label(
                    "이 분류에 해당하는 물건이 없습니다.");
            }
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

        // 130일차: 상점 강화(할인율·판매가 보너스)가 적용 중이면 그대로 보여준다 -
        // 강화가 하나도 없으면(전부 0/기본값) 굳이 표시하지 않는다.
        private void DrawShopUpgradeSummary()
        {
            if (ApplicationFlow.Current == null)
            {
                return;
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
                return;
            }

            GUILayout.Label(
                $"강화 적용 중 - 구매 할인 {upgrade.DiscountPercent}% / 판매가 {sellPercent}%");
        }

        // 114일차: ShopService.Sell은 105일차부터 있었지만 실제로 호출하는 UI가
        // 없었다 - 여기서 처음 연결했다. 130일차: 판매가 비율이 상점 강화에 따라
        // 달라져서 ApplicationFlow.GetShopSellPriceRatio()로 매번 다시 계산한다.
        private void DrawSellList()
        {
            InventoryRunState inventory =
                RunContext.Current?.Inventory;

            if (inventory == null
                || inventory.Slots.Count == 0)
            {
                GUILayout.Label(
                    "팔 수 있는 물건이 없습니다.");

                return;
            }

            double sellPriceRatio =
                ApplicationFlow.Current != null
                    ? ApplicationFlow.Current.GetShopSellPriceRatio()
                    : ShopService.DefaultSellPriceRatio;

            bool hasSellableSlot =
                false;

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

                hasSellableSlot =
                    true;

                int sellPrice =
                    (int)(definition.BasePrice
                    * sellPriceRatio);

                if (GUILayout.Button(
                        $"{definition.DisplayName} ×{slot.Quantity}  -  {sellPrice} G  [판매]",
                        GUILayout.Height(30f)))
                {
                    SellSlot(
                        inventory,
                        slotIndex,
                        definition);
                }
            }

            if (!hasSellableSlot)
            {
                GUILayout.Label(
                    "팔 수 있는 물건이 없습니다.");
            }
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

                return;
            }

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

                return;
            }

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

        private void DrawHealScreen()
        {
            DrawServiceHeader();

            PlayerRunState player =
                RunContext.Current?.Player;

            if (player != null)
            {
                StatBlock finalStats =
                    player.GetFinalStats();

                GUILayout.Label(
                    $"체력 {player.CurrentHp}/{finalStats.MaxHealth}   마나 {player.CurrentMana}/{finalStats.MaxMana}   정력 {player.CurrentStamina}/{finalStats.MaxStamina}");
            }

            if (GUILayout.Button(
                    $"회복하기 ({HealCost} G)",
                    GUILayout.Height(34f)))
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
            }

            DrawBackButton();
        }

        private void DrawInfoScreen()
        {
            DrawServiceHeader();

            DungeonRunState dungeon =
                RunContext.Current?.Dungeon;

            if (dungeon == null)
            {
                GUILayout.Label(
                    "정보를 불러올 수 없습니다.");
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

                GUILayout.Label(
                    $"{dungeon.CurrentFloor}층 - 지금까지 둘러본 {visited}개 방 기준");

                GUILayout.Label(
                    $"전투 {combatCount} / 함정 {trapCount} / 이벤트 {eventCount} / 일반 {normalCount}");

                GUILayout.Label(
                    "가보지 않은 방의 정보는 알려줄 수 없습니다.");
            }

            DrawBackButton();
        }

        private void DrawRelicScreen()
        {
            DrawServiceHeader();

            RelicRunState relics =
                RunContext.Current?.Relics;

            relicScreenScroll =
                GUILayout.BeginScrollView(
                    relicScreenScroll,
                    GUILayout.Height(ShopListHeight));

            if (relics == null
                || relics.Relics.Count == 0)
            {
                GUILayout.Label(
                    "정리할 유물이 없습니다.");
            }
            else
            {
                for (int i = 0; i < relics.Relics.Count; i++)
                {
                    RelicInstanceState relic =
                        relics.Relics[i];

                    if (relic == null)
                    {
                        continue;
                    }

                    GUILayout.BeginHorizontal();

                    GUILayout.Label(
                        relic.IsCursed
                            ? $"{relic.DisplayName} (저주)"
                            : relic.DisplayName);

                    if (relic.IsCursed)
                    {
                        if (GUILayout.Button(
                                $"저주 제거 ({CurseRemovalCost} G)",
                                GUILayout.Width(160f)))
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
                        }
                    }
                    else if (GUILayout.Button(
                                 $"희생 (+{SacrificeReward} G)",
                                 GUILayout.Width(160f)))
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
                    }

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();

            DrawBackButton();
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

        private void DrawBackButton(
            ServiceScreen target = ServiceScreen.Menu)
        {
            GUILayout.Space(
                6f);

            if (GUILayout.Button(
                    "뒤로",
                    GUILayout.Height(30f)))
            {
                serviceScreen =
                    target;

                statusText =
                    string.Empty;
            }
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
