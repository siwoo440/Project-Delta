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
        private const float IllustrationSize = 300f;
        private const float DialogueBoxWidth = 560f;
        private const float DialogueBoxHeight = 220f;
        private const float ButtonsColumnWidth = 240f;
        private const float PanelGap = 14f;
        private const float BottomMargin = 40f;

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

            float totalWidth =
                DialogueBoxWidth
                + PanelGap
                + ButtonsColumnWidth;

            float dialogueX =
                (Screen.width - totalWidth) * 0.5f;

            float dialogueY =
                Screen.height
                - BottomMargin
                - DialogueBoxHeight;

            Rect dialogueRect =
                new Rect(
                    dialogueX,
                    dialogueY,
                    DialogueBoxWidth,
                    DialogueBoxHeight);

            Rect buttonsRect =
                new Rect(
                    dialogueRect.xMax + PanelGap,
                    dialogueY,
                    ButtonsColumnWidth,
                    DialogueBoxHeight);

            float illustrationY =
                Mathf.Max(
                    40f,
                    dialogueY - PanelGap - IllustrationSize);

            Rect illustrationRect =
                new Rect(
                    (Screen.width - IllustrationSize) * 0.5f,
                    illustrationY,
                    IllustrationSize,
                    IllustrationSize);

            DrawCharacterIllustration(
                illustrationRect);

            DrawDialogueBox(
                dialogueRect);

            GUI.Box(
                buttonsRect,
                string.Empty);

            GUILayout.BeginArea(
                new Rect(
                    buttonsRect.x + 12f,
                    buttonsRect.y + 12f,
                    buttonsRect.width - 24f,
                    buttonsRect.height - 24f));

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

        private void DrawDialogueBox(
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

            GUIStyle headerStyle =
                new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14
                };

            headerStyle.normal.textColor =
                new Color(0.8f, 0.8f, 0.8f, 1f);

            GUILayout.Label(
                $"{openNpc.Definition.DisplayName}   |   호감도 {openNpc.RelationshipState.Affinity}/100 ({openNpc.RelationshipState.Stage})   |   {(openNpc.RelationshipState.IsHostile ? "적대" : "우호")}",
                headerStyle);

            GUILayout.Space(
                8f);

            GUIStyle bodyStyle =
                new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    wordWrap = true
                };

            bodyStyle.normal.textColor =
                Color.white;

            GUILayout.Label(
                string.IsNullOrEmpty(statusText)
                    ? "무엇을 할지 선택하세요."
                    : statusText,
                bodyStyle);

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
                    "선물",
                    GUILayout.Height(36f)))
            {
                statusText =
                    string.Empty;

                serviceScreen =
                    ServiceScreen.Gift;
            }

            GUI.enabled =
                !openNpc.RelationshipState.HasBeenRescued;

            if (GUILayout.Button(
                    "구조",
                    GUILayout.Height(36f)))
            {
                ApplyResult(
                    interactionService.ResolveRescue(
                        openNpc.RelationshipState,
                        RescueAffinityGain));
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
