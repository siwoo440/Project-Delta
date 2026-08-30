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
    public sealed class NpcInteractionController : MonoBehaviour
    {
        private enum ServiceScreen
        {
            None,
            Menu,
            Shop,
            Heal,
            Info,
            Relic
        }

        private const int HealCost = 15;
        private const int CurseRemovalCost = 20;
        private const int SacrificeReward = 10;

        private PlayerGridMovementController movementController;
        private PlayerLookController lookController;
        private Transform viewTransform;
        private NpcContentMarker openNpc;
        private readonly NpcInteractionService interactionService =
            new NpcInteractionService();

        private bool isPanelOpen;
        private string promptText;
        private string statusText;
        private ServiceScreen serviceScreen =
            ServiceScreen.None;

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

            if (movementController != null)
            {
                movementController.IsInputLocked =
                    true;
            }

            lookController?.SetCursorFreeForUi(
                true);
        }

        private void CloseInteraction()
        {
            isPanelOpen =
                false;

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

            const float panelWidth = 480f;
            const float panelHeight = 380f;

            Rect panelRect =
                new Rect(
                    (Screen.width - panelWidth) * 0.5f,
                    (Screen.height - panelHeight) * 0.5f,
                    panelWidth,
                    panelHeight);

            GUI.Box(
                panelRect,
                string.Empty);

            GUILayout.BeginArea(
                new Rect(
                    panelRect.x + 22f,
                    panelRect.y + 18f,
                    panelRect.width - 44f,
                    panelRect.height - 36f));

            GUIStyle titleStyle =
                new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 22,
                    fontStyle = FontStyle.Bold
                };

            titleStyle.normal.textColor =
                Color.white;

            GUILayout.Label(
                openNpc.Definition.DisplayName,
                titleStyle,
                GUILayout.Height(34f));

            GUILayout.Label(
                $"ID : {openNpc.Definition.Id}");

            GUILayout.Label(
                $"호감도 : {openNpc.RelationshipState.Affinity} / 100   |   관계 : {openNpc.RelationshipState.Stage}");

            GUILayout.Label(
                $"서비스 : {openNpc.Definition.ServiceTypes}   |   전투 가능 : {(openNpc.Definition.CanBattle ? "가능" : "불가")}");

            GUILayout.Space(
                10f);

            GUILayout.Label(
                string.IsNullOrEmpty(statusText)
                    ? "무엇을 할지 선택하세요."
                    : statusText,
                GUILayout.Height(48f));

            GUILayout.Space(
                8f);

            if (serviceScreen == ServiceScreen.None)
            {
                DrawMainButtons();
            }
            else
            {
                DrawServiceScreen();
            }

            GUILayout.EndArea();
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
                    "떠나기",
                    GUILayout.Height(36f)))
            {
                ApplyResult(
                    interactionService.Resolve(
                        openNpc.Definition,
                        openNpc.RelationshipState,
                        NpcInteractionCommand.Leave));
            }
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

                default:
                    DrawServiceMenu();
                    break;
            }
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

            if (shop == null
                || shop.Products.Count == 0)
            {
                GUILayout.Label(
                    "판매할 물건이 없습니다.");
            }
            else
            {
                for (int i = 0; i < shop.Products.Count; i++)
                {
                    ShopProductState product =
                        shop.Products[i];

                    if (product == null)
                    {
                        continue;
                    }

                    if (GUILayout.Button(
                            $"{product.DisplayName}  -  {product.Price} G  [구매]",
                            GUILayout.Height(30f)))
                    {
                        BuyProduct(
                            shop,
                            i);
                    }
                }
            }

            GUILayout.Space(
                10f);

            GUILayout.Label(
                "판매");

            DrawSellList();

            DrawBackButton();
        }

        // 114일차: ShopService.Sell은 105일차부터 있었지만 실제로 호출하는 UI가
        // 없었다 - 여기서 처음 연결한다. 판매가는 정가의 50%(ShopService.SellPriceRatio).
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
                    * ShopService.SellPriceRatio);

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
            RelicRunState relics =
                RunContext.Current?.Relics;

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

        private void DrawBackButton()
        {
            GUILayout.Space(
                6f);

            if (GUILayout.Button(
                    "뒤로",
                    GUILayout.Height(30f)))
            {
                serviceScreen =
                    ServiceScreen.Menu;

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
