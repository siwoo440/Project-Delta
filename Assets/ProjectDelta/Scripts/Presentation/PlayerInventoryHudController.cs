using System.Collections.Generic;
using System.Reflection;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PlayerInventoryHudController : MonoBehaviour
    {
        private const int VisibleSlotCount = 10;

        [Header("Definitions")]
        [SerializeField]
        private ItemDefinition[] itemDefinitions =
            new ItemDefinition[0];

        [Header("Inventory")]
        [SerializeField]
        private GameObject inventoryPanel;

        [SerializeField]
        private Button[] slotButtons =
            new Button[VisibleSlotCount];

        [SerializeField]
        private Image[] slotBackgrounds =
            new Image[VisibleSlotCount];

        [SerializeField]
        private Image[] slotItemIcons =
            new Image[VisibleSlotCount];

        [SerializeField]
        private Text[] slotNumberTexts =
            new Text[VisibleSlotCount];

        [SerializeField]
        private Text[] slotQuantityTexts =
            new Text[VisibleSlotCount];

        [Header("Selected Item")]
        [SerializeField]
        private GameObject selectedItemPanel;

        [SerializeField]
        private Image selectedItemIcon;

        [SerializeField]
        private Text selectedItemNameText;

        [SerializeField]
        private Text selectedItemDescriptionText;

        private readonly Dictionary<string, ItemDefinition> itemLookup =
            new Dictionary<string, ItemDefinition>();

        private int selectedSlotIndex =
            -1;

        [Header("Actions")]
        [SerializeField]
        private Button useButton;

        [SerializeField]
        private Button moveButton;

        [SerializeField]
        private Button discardButton;

        [SerializeField]
        private Text moveButtonText;

        [SerializeField]
        private GameObject discardConfirmPanel;

        [SerializeField]
        private Text discardConfirmText;

        [SerializeField]
        private Button discardOneButton;

        [SerializeField]
        private Button discardAllButton;

        [SerializeField]
        private Button discardCancelButton;

        private bool isMoveMode;
        private int moveSourceSlotIndex =
            -1;

        private ExplorationMonsterEncounterController encounterController;

        private string lastUseMessage =
            string.Empty;

        // 96일차: 전체 인벤토리 UI를 매 프레임 다시 그리지 않기 위한 상태 서명.
        private bool hasRefreshSignature;
        private ulong lastRefreshSignature;

        private bool IsDiscardConfirmOpen =>
            discardConfirmPanel != null
            && discardConfirmPanel.activeSelf;

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            // 선택 아이템이 있을 때만 조우 컨트롤러를 늦게 찾는다.
            // 한 번 찾은 뒤에는 ResolveEncounterController()가 즉시 반환한다.
            if (selectedSlotIndex >= 0
                && encounterController == null)
            {
                ResolveEncounterController();
            }

            if (!ShouldRefreshInventory())
            {
                return;
            }

            RefreshInventory();
        }

        public void Configure(
            ItemDefinition[] definitions)
        {
            itemDefinitions =
                definitions
                ?? new ItemDefinition[0];

            Initialize();
        }

        public void Configure()
        {
            Initialize();
        }

        private void Initialize()
        {
            RebuildItemLookup();

            InventoryRunState.MaxStackResolver =
                ResolveMaxStackSizeInternal;

            ResizeSlotArrayLengths();
            AutoBindQuantityTexts();
            HookButtons();

            selectedSlotIndex =
                -1;

            isMoveMode =
                false;

            moveSourceSlotIndex =
                -1;

            lastUseMessage =
                string.Empty;

            if (discardConfirmPanel != null)
            {
                discardConfirmPanel.SetActive(
                    false);
            }

            ShowSelectedItemPanel(
                false);

            RefreshInventory();
        }

        private void ResizeSlotArrayLengths()
        {
            if (slotButtons == null
                || slotButtons.Length
                    != VisibleSlotCount)
            {
                System.Array.Resize(
                    ref slotButtons,
                    VisibleSlotCount);
            }

            if (slotBackgrounds == null
                || slotBackgrounds.Length
                    != VisibleSlotCount)
            {
                System.Array.Resize(
                    ref slotBackgrounds,
                    VisibleSlotCount);
            }

            if (slotItemIcons == null
                || slotItemIcons.Length
                    != VisibleSlotCount)
            {
                System.Array.Resize(
                    ref slotItemIcons,
                    VisibleSlotCount);
            }

            if (slotNumberTexts == null
                || slotNumberTexts.Length
                    != VisibleSlotCount)
            {
                System.Array.Resize(
                    ref slotNumberTexts,
                    VisibleSlotCount);
            }

            if (slotQuantityTexts == null
                || slotQuantityTexts.Length
                    != VisibleSlotCount)
            {
                System.Array.Resize(
                    ref slotQuantityTexts,
                    VisibleSlotCount);
            }
        }

        private void RebuildItemLookup()
        {
            itemLookup.Clear();

            if (itemDefinitions == null)
            {
                return;
            }

            for (int index = 0;
                 index < itemDefinitions.Length;
                 index++)
            {
                ItemDefinition definition =
                    itemDefinitions[index];

                if (definition == null)
                {
                    continue;
                }

                AddLookupKey(
                    definition.Id,
                    definition);

                AddLookupKey(
                    definition.name,
                    definition);

                AddLookupKey(
                    definition.DisplayName,
                    definition);
            }
        }

        private void AddLookupKey(
            string key,
            ItemDefinition definition)
        {
            if (string.IsNullOrEmpty(
                    key)
                || definition == null)
            {
                return;
            }

            itemLookup[key] =
                definition;
        }

        private void AutoBindQuantityTexts()
        {
            if (slotButtons == null)
            {
                return;
            }

            for (int index = 0;
                 index < slotButtons.Length;
                 index++)
            {
                if (slotQuantityTexts[index]
                    != null)
                {
                    continue;
                }

                Button slotButton =
                    slotButtons[index];

                if (slotButton == null)
                {
                    continue;
                }

                Transform quantityTransform =
                    slotButton.transform.Find(
                        "SlotQuantityText");

                if (quantityTransform != null)
                {
                    slotQuantityTexts[index] =
                        quantityTransform.GetComponent<Text>();
                }
            }
        }

        private void HookButtons()
        {
            if (slotButtons != null)
            {
                for (int index = 0;
                     index < slotButtons.Length;
                     index++)
                {
                    Button button =
                        slotButtons[index];

                    if (button == null)
                    {
                        continue;
                    }

                    int capturedIndex =
                        index;

                    button.onClick.RemoveAllListeners();

                    button.onClick.AddListener(
                        () => OnSlotClicked(
                            capturedIndex));
                }
            }

            HookButton(
                useButton,
                OnUseButtonClicked);

            HookButton(
                moveButton,
                OnMoveButtonClicked);

            HookButton(
                discardButton,
                OnDiscardButtonClicked);

            HookButton(
                discardOneButton,
                OnDiscardOneClicked);

            HookButton(
                discardAllButton,
                OnDiscardAllClicked);

            HookButton(
                discardCancelButton,
                CloseDiscardConfirmPanel);
        }

        private static void HookButton(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(
                action);
        }

        private void OnSlotClicked(
            int slotIndex)
        {
            if (IsDiscardConfirmOpen)
            {
                return;
            }

            if (isMoveMode)
            {
                HandleMoveDestination(
                    slotIndex);

                return;
            }

            InventoryRunState inventory =
                GetInventory();

            if (inventory == null
                || !inventory.TryGetSlot(
                    slotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                ClearSelection();

                return;
            }

            selectedSlotIndex =
                slotIndex;

            lastUseMessage =
                string.Empty;

            RefreshSelectedItem();
        }

        private void OnUseButtonClicked()
        {
            if (isMoveMode
                || IsDiscardConfirmOpen)
            {
                return;
            }

            InventoryRunState inventory =
                GetInventory();

            if (inventory == null
                || selectedSlotIndex < 0
                || !inventory.TryGetSlot(
                    selectedSlotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                return;
            }

            ItemDefinition definition =
                ResolveDefinition(
                    slot);

            if (definition == null)
            {
                lastUseMessage =
                    "아이템 정의를 찾을 수 없습니다.";

                RefreshSelectedItem();
                return;
            }

            ResolveEncounterController();

            ItemUseResult result;

            if (encounterController != null
                && encounterController.HasBattle)
            {
                result =
                    TryUseDuringBattle(
                        definition);
            }
            else
            {
                RunContext context =
                    RunContext.Current;

                result =
                    ItemUseService.CommitExploration(
                        inventory,
                        selectedSlotIndex,
                        context != null
                            ? context.Player
                            : null,
                        definition);

                if (result.Success)
                {
                    ApplicationFlow.Current?.SaveDungeonProgress();
                }
            }

            lastUseMessage =
                BuildUseResultMessage(
                    result);

            RefreshInventory();
        }

        private void OnMoveButtonClicked()
        {
            if (IsDiscardConfirmOpen)
            {
                return;
            }

            if (isMoveMode)
            {
                CancelMoveMode();
                return;
            }

            InventoryRunState inventory =
                GetInventory();

            if (inventory == null
                || selectedSlotIndex < 0
                || !inventory.TryGetSlot(
                    selectedSlotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                return;
            }

            isMoveMode =
                true;

            moveSourceSlotIndex =
                selectedSlotIndex;

            lastUseMessage =
                "이동할 슬롯을 선택하세요. 같은 슬롯을 다시 누르면 취소됩니다.";

            RefreshInventory();
        }

        private void HandleMoveDestination(
            int destinationSlotIndex)
        {
            if (!isMoveMode)
            {
                return;
            }

            if (destinationSlotIndex
                == moveSourceSlotIndex)
            {
                CancelMoveMode();
                return;
            }

            InventoryRunState inventory =
                GetInventory();

            InventoryInteractionResult result =
                InventoryInteractionService.Move(
                    inventory,
                    moveSourceSlotIndex,
                    destinationSlotIndex);

            if (!result.Success)
            {
                lastUseMessage =
                    "해당 슬롯으로 이동할 수 없습니다.";

                RefreshInventory();
                return;
            }

            ApplicationFlow.Current?.SaveDungeonProgress();

            selectedSlotIndex =
                destinationSlotIndex;

            isMoveMode =
                false;

            moveSourceSlotIndex =
                -1;

            lastUseMessage =
                "아이템 이동을 완료했습니다.";

            RefreshInventory();
        }

        private void CancelMoveMode()
        {
            isMoveMode =
                false;

            moveSourceSlotIndex =
                -1;

            lastUseMessage =
                string.Empty;

            RefreshInventory();
        }

        private void OnDiscardButtonClicked()
        {
            if (isMoveMode
                || IsDiscardConfirmOpen)
            {
                return;
            }

            InventoryRunState inventory =
                GetInventory();

            if (inventory == null
                || selectedSlotIndex < 0
                || !inventory.TryGetSlot(
                    selectedSlotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                return;
            }

            ItemDefinition definition =
                ResolveDefinition(
                    slot);

            ItemCategory category =
                definition != null
                    ? definition.Category
                    : ItemCategory.Uncategorized;

            if (!ItemCategoryRules.CanDiscard(
                    category))
            {
                lastUseMessage =
                    "이 아이템은 버릴 수 없습니다.";

                RefreshSelectedItem();
                return;
            }

            if (discardConfirmText != null)
            {
                string displayName =
                    definition != null
                    && !string.IsNullOrEmpty(
                        definition.DisplayName)
                        ? definition.DisplayName
                        : slot.DisplayName;

                discardConfirmText.text =
                    $"{displayName} ×{slot.Quantity}\n버릴 수량을 선택하세요.";
            }

            discardConfirmPanel.SetActive(
                true);

            discardConfirmPanel.transform.SetAsLastSibling();

            RefreshInventory();
        }

        private void OnDiscardOneClicked()
        {
            ExecuteDiscard(
                false);
        }

        private void OnDiscardAllClicked()
        {
            ExecuteDiscard(
                true);
        }

        private void ExecuteDiscard(
            bool discardAll)
        {
            InventoryRunState inventory =
                GetInventory();

            if (inventory == null
                || selectedSlotIndex < 0
                || !inventory.TryGetSlot(
                    selectedSlotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                CloseDiscardConfirmPanel();
                return;
            }

            ItemDefinition definition =
                ResolveDefinition(
                    slot);

            ItemCategory category =
                definition != null
                    ? definition.Category
                    : ItemCategory.Uncategorized;

            InventoryInteractionResult result =
                discardAll
                    ? InventoryInteractionService.DiscardAll(
                        inventory,
                        selectedSlotIndex,
                        category)
                    : InventoryInteractionService.DiscardOne(
                        inventory,
                        selectedSlotIndex,
                        category);

            if (!result.Success)
            {
                lastUseMessage =
                    result.FailureReason
                        == InventoryInteractionFailureReason.DiscardNotAllowed
                            ? "이 아이템은 버릴 수 없습니다."
                            : "아이템을 버리지 못했습니다.";

                CloseDiscardConfirmPanel();
                RefreshInventory();
                return;
            }

            ApplicationFlow.Current?.SaveDungeonProgress();

            CloseDiscardConfirmPanel();

            if (!inventory.TryGetSlot(
                    selectedSlotIndex,
                    out InventorySlotState remainingSlot)
                || remainingSlot == null
                || remainingSlot.IsEmpty)
            {
                ClearSelection();
                return;
            }

            lastUseMessage =
                discardAll
                    ? "아이템을 전부 버렸습니다."
                    : "아이템 1개를 버렸습니다.";

            RefreshInventory();
        }

        private void CloseDiscardConfirmPanel()
        {
            if (discardConfirmPanel != null)
            {
                discardConfirmPanel.SetActive(
                    false);
            }

            RefreshInventory();
        }

        private ItemUseResult TryUseDuringBattle(
            ItemDefinition definition)
        {
            if (encounterController == null)
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.BattleActionUnavailable);
            }

            MethodInfo method =
                encounterController.GetType().GetMethod(
                    "ConfirmUseInventoryItem",
                    BindingFlags.Instance
                    | BindingFlags.Public,
                    null,
                    new[]
                    {
                        typeof(int),
                        typeof(ItemDefinition)
                    },
                    null);

            if (method == null)
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.BattleActionUnavailable);
            }

            object returnedValue =
                method.Invoke(
                    encounterController,
                    new object[]
                    {
                        selectedSlotIndex,
                        definition
                    });

            return returnedValue as ItemUseResult
                ?? ItemUseResult.Failed(
                    ItemUseFailureReason.BattleActionUnavailable);
        }

        private void RefreshInventory()
        {
            InventoryRunState inventory =
                GetInventory();

            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(
                    true);
            }

            for (int index = 0;
                 index < VisibleSlotCount;
                 index++)
            {
                InventorySlotState slot =
                    inventory != null
                    && index < inventory.Slots.Count
                        ? inventory.Slots[index]
                        : null;

                RefreshSlot(
                    index,
                    slot);
            }

            if (selectedSlotIndex >= 0)
            {
                RefreshSelectedItem();
            }

            // 직접 호출된 Refresh도 현재 상태를 기준으로 기록하여
            // 다음 프레임에 같은 UI를 한 번 더 갱신하지 않는다.
            CaptureRefreshSignature();
        }

        private bool ShouldRefreshInventory()
        {
            ulong currentSignature =
                CalculateRefreshSignature();

            return !hasRefreshSignature
                || currentSignature
                    != lastRefreshSignature;
        }

        private void CaptureRefreshSignature()
        {
            lastRefreshSignature =
                CalculateRefreshSignature();

            hasRefreshSignature =
                true;
        }

        private ulong CalculateRefreshSignature()
        {
            // FNV-1a 64bit. UI에 영향을 주는 작은 상태만 비교하므로
            // 매 프레임 전체 Graphic/Text를 갱신하는 것보다 훨씬 가볍다.
            ulong signature =
                1469598103934665603UL;

            AddRefreshSignature(
                ref signature,
                selectedSlotIndex);

            AddRefreshSignature(
                ref signature,
                isMoveMode);

            AddRefreshSignature(
                ref signature,
                moveSourceSlotIndex);

            AddRefreshSignature(
                ref signature,
                IsDiscardConfirmOpen);

            AddRefreshSignature(
                ref signature,
                lastUseMessage);

            InventoryRunState inventory =
                GetInventory();

            AddRefreshSignature(
                ref signature,
                inventory != null);

            if (inventory != null)
            {
                AddRefreshSignature(
                    ref signature,
                    inventory.Slots.Count);

                int slotCount =
                    Mathf.Min(
                        VisibleSlotCount,
                        inventory.Slots.Count);

                for (int index = 0;
                     index < slotCount;
                     index++)
                {
                    InventorySlotState slot =
                        inventory.Slots[index];

                    bool hasItem =
                        slot != null
                        && !slot.IsEmpty;

                    AddRefreshSignature(
                        ref signature,
                        hasItem);

                    if (!hasItem)
                    {
                        continue;
                    }

                    AddRefreshSignature(
                        ref signature,
                        slot.ItemId);

                    AddRefreshSignature(
                        ref signature,
                        slot.DisplayName);

                    AddRefreshSignature(
                        ref signature,
                        slot.Quantity);

                    AddRefreshSignature(
                        ref signature,
                        slot.MaxStackSize);
                }
            }

            // 선택 아이템이 없으면 자원/전투 상태는 인벤토리 표시 결과에 영향을 주지 않는다.
            if (selectedSlotIndex < 0)
            {
                return signature;
            }

            RunContext runContext =
                RunContext.Current;

            PlayerRunState player =
                runContext != null
                    ? runContext.Player
                    : null;

            AddRefreshSignature(
                ref signature,
                player != null);

            if (player != null)
            {
                AddRefreshSignature(
                    ref signature,
                    player.CurrentHp);

                AddRefreshSignature(
                    ref signature,
                    player.CurrentMana);

                AddRefreshSignature(
                    ref signature,
                    player.CurrentStamina);

                StatBlock finalStats =
                    player.GetFinalStats();

                AddRefreshSignature(
                    ref signature,
                    finalStats.MaxHealth);

                AddRefreshSignature(
                    ref signature,
                    finalStats.MaxMana);

                AddRefreshSignature(
                    ref signature,
                    finalStats.MaxStamina);
            }

            AddRefreshSignature(
                ref signature,
                encounterController != null);

            if (encounterController == null)
            {
                return signature;
            }

            AddRefreshSignature(
                ref signature,
                encounterController.HasBattle);

            AddRefreshSignature(
                ref signature,
                encounterController.IsBattleActive);

            AddRefreshSignature(
                ref signature,
                (int)encounterController.CurrentBattleState);

            BattleParticipant actor =
                encounterController.CurrentBattleActor;

            AddRefreshSignature(
                ref signature,
                actor != null);

            if (actor != null)
            {
                AddRefreshSignature(
                    ref signature,
                    actor.InstanceId);

                AddRefreshSignature(
                    ref signature,
                    (int)actor.Team);
            }

            BattleContext battleContext =
                encounterController.CurrentBattleContext;

            BattleParticipant battlePlayer =
                battleContext != null
                    ? battleContext.Player
                    : null;

            AddRefreshSignature(
                ref signature,
                battlePlayer != null);

            if (battlePlayer != null)
            {
                AddRefreshSignature(
                    ref signature,
                    battlePlayer.CurrentHp);

                AddRefreshSignature(
                    ref signature,
                    battlePlayer.MaxHp);

                AddRefreshSignature(
                    ref signature,
                    battlePlayer.CurrentMana);

                AddRefreshSignature(
                    ref signature,
                    battlePlayer.MaxMana);

                AddRefreshSignature(
                    ref signature,
                    battlePlayer.CurrentStamina);

                AddRefreshSignature(
                    ref signature,
                    battlePlayer.MaxStamina);
            }

            return signature;
        }

        private static void AddRefreshSignature(
            ref ulong signature,
            bool value)
        {
            AddRefreshSignature(
                ref signature,
                value
                    ? 1
                    : 0);
        }

        private static void AddRefreshSignature(
            ref ulong signature,
            int value)
        {
            unchecked
            {
                signature ^=
                    (ulong)(uint)value;

                signature *=
                    1099511628211UL;
            }
        }

        private static void AddRefreshSignature(
            ref ulong signature,
            string value)
        {
            unchecked
            {
                if (string.IsNullOrEmpty(
                        value))
                {
                    signature ^=
                        0UL;

                    signature *=
                        1099511628211UL;

                    return;
                }

                for (int index = 0;
                     index < value.Length;
                     index++)
                {
                    signature ^=
                        value[index];

                    signature *=
                        1099511628211UL;
                }

                // 서로 다른 문자열 경계를 구분한다.
                signature ^=
                    255UL;

                signature *=
                    1099511628211UL;
            }
        }

        private void RefreshSlot(
            int index,
            InventorySlotState slot)
        {
            bool hasItem =
                slot != null
                && !slot.IsEmpty;

            if (slotNumberTexts != null
                && index < slotNumberTexts.Length
                && slotNumberTexts[index] != null)
            {
                slotNumberTexts[index].text =
                    isMoveMode
                    && index == moveSourceSlotIndex
                        ? $"▶ {index + 1}"
                        : (index + 1).ToString();
            }

            ItemDefinition definition =
                hasItem
                    ? ResolveDefinition(
                        slot)
                    : null;

            if (slotItemIcons != null
                && index < slotItemIcons.Length
                && slotItemIcons[index] != null)
            {
                Image icon =
                    slotItemIcons[index];

                icon.sprite =
                    definition != null
                        ? definition.Icon
                        : null;

                icon.enabled =
                    definition != null
                    && definition.Icon != null;
            }

            if (slotQuantityTexts != null
                && index < slotQuantityTexts.Length
                && slotQuantityTexts[index] != null)
            {
                Text quantityText =
                    slotQuantityTexts[index];

                quantityText.text =
                    hasItem
                    && slot.Quantity > 1
                        ? $"×{slot.Quantity}"
                        : string.Empty;

                quantityText.enabled =
                    hasItem
                    && slot.Quantity > 1;
            }

            if (slotButtons != null
                && index < slotButtons.Length
                && slotButtons[index] != null)
            {
                slotButtons[index].interactable =
                    !IsDiscardConfirmOpen
                    && (hasItem
                        || isMoveMode);
            }

            if (slotBackgrounds != null
                && index < slotBackgrounds.Length
                && slotBackgrounds[index] != null)
            {
                slotBackgrounds[index].enabled =
                    true;
            }
        }

        private void RefreshSelectedItem()
        {
            InventoryRunState inventory =
                GetInventory();

            if (inventory == null
                || selectedSlotIndex < 0
                || !inventory.TryGetSlot(
                    selectedSlotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                ClearSelection();
                return;
            }

            ItemDefinition definition =
                ResolveDefinition(
                    slot);

            ShowSelectedItemPanel(
                true);

            if (selectedItemIcon != null)
            {
                selectedItemIcon.sprite =
                    definition != null
                        ? definition.Icon
                        : null;

                selectedItemIcon.enabled =
                    definition != null
                    && definition.Icon != null;
            }

            if (selectedItemNameText != null)
            {
                selectedItemNameText.text =
                    definition != null
                    && !string.IsNullOrEmpty(
                        definition.DisplayName)
                        ? definition.DisplayName
                        : slot.DisplayName;
            }

            if (selectedItemDescriptionText != null)
            {
                string baseDescription =
                    definition != null
                        ? definition.Description
                        : string.Empty;

                string categoryDisplayName =
                    definition != null
                        ? ItemCategoryRules.GetDisplayName(
                            definition.Category)
                        : ItemCategoryRules.GetDisplayName(
                            ItemCategory.Uncategorized);

                string description =
                    $"[{categoryDisplayName}]\n보유 수량 ×{slot.Quantity}\n{baseDescription}".Trim();

                if (!string.IsNullOrEmpty(
                        lastUseMessage))
                {
                    description +=
                        $"\n\n{lastUseMessage}";
                }

                selectedItemDescriptionText.text =
                    description;
            }

            RefreshActionButtons(
                inventory,
                definition);
        }

        private void RefreshActionButtons(
            InventoryRunState inventory,
            ItemDefinition definition)
        {
            bool modalOpen =
                IsDiscardConfirmOpen;

            if (moveButton != null)
            {
                moveButton.gameObject.SetActive(
                    true);

                moveButton.interactable =
                    !modalOpen;

                if (moveButtonText != null)
                {
                    moveButtonText.text =
                        isMoveMode
                            ? "이동 취소"
                            : "이동";
                }
            }

            ItemCategory category =
                definition != null
                    ? definition.Category
                    : ItemCategory.Uncategorized;

            if (discardButton != null)
            {
                discardButton.gameObject.SetActive(
                    true);

                discardButton.interactable =
                    !modalOpen
                    && !isMoveMode
                    && ItemCategoryRules.CanDiscard(
                        category);
            }

            if (useButton == null)
            {
                return;
            }

            bool categoryAllowsUse =
                definition != null
                && ItemCategoryRules.CanUse(
                    category);

            useButton.gameObject.SetActive(
                categoryAllowsUse);

            if (!categoryAllowsUse)
            {
                return;
            }

            if (modalOpen
                || isMoveMode)
            {
                useButton.interactable =
                    false;

                return;
            }

            ResolveEncounterController();

            ItemUseResult preview;

            if (encounterController != null
                && encounterController.HasBattle)
            {
                BattleContext battleContext =
                    encounterController.CurrentBattleContext;

                BattleParticipant actor =
                    encounterController.CurrentBattleActor;

                bool playerCanAct =
                    encounterController.IsBattleActive
                    && encounterController.CurrentBattleState
                        == BattleState.AwaitingAction
                    && battleContext != null
                    && actor != null
                    && actor == battleContext.Player;

                preview =
                    playerCanAct
                        ? ItemUseService.PreviewBattle(
                            inventory,
                            selectedSlotIndex,
                            battleContext.Player,
                            definition)
                        : ItemUseResult.Failed(
                            ItemUseFailureReason.NotPlayerTurn);
            }
            else
            {
                RunContext context =
                    RunContext.Current;

                preview =
                    ItemUseService.PreviewExploration(
                        inventory,
                        selectedSlotIndex,
                        context != null
                            ? context.Player
                            : null,
                        definition);
            }

            useButton.interactable =
                preview.Success;
        }

        private ItemDefinition ResolveDefinition(
            InventorySlotState slot)
        {
            if (slot == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(
                    slot.ItemId)
                && itemLookup.TryGetValue(
                    slot.ItemId,
                    out ItemDefinition definition))
            {
                return definition;
            }

            if (!string.IsNullOrEmpty(
                    slot.DisplayName)
                && itemLookup.TryGetValue(
                    slot.DisplayName,
                    out definition))
            {
                return definition;
            }

            RuntimeItemDefinitionLookup.TryFind(
                slot.ItemId,
                out definition);

            return definition;
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

        private int ResolveMaxStackSizeInternal(
            string itemId)
        {
            if (!string.IsNullOrEmpty(
                    itemId)
                && itemLookup.TryGetValue(
                    itemId,
                    out ItemDefinition definition)
                && definition != null)
            {
                return definition.MaxStackSize;
            }

            return RuntimeItemDefinitionLookup.ResolveMaxStackSize(
                itemId);
        }

        private static string BuildUseResultMessage(
            ItemUseResult result)
        {
            if (result == null)
            {
                return "아이템을 사용할 수 없습니다.";
            }

            if (!result.Success)
            {
                switch (result.FailureReason)
                {
                    case ItemUseFailureReason.InvalidSlot:
                        return "사용할 아이템 슬롯을 찾을 수 없습니다.";

                    case ItemUseFailureReason.ItemNotFound:
                    case ItemUseFailureReason.ItemMismatch:
                        return "아이템 데이터가 슬롯과 일치하지 않습니다.";

                    case ItemUseFailureReason.ItemNotUsable:
                        return "사용할 수 없는 종류의 아이템입니다.";

                    case ItemUseFailureReason.WrongContext:
                        return "현재 상황에서는 사용할 수 없습니다.";

                    case ItemUseFailureReason.NoEffects:
                        return "사용 효과가 설정되지 않았습니다.";

                    case ItemUseFailureReason.NoApplicableEffect:
                        return "현재 적용할 수 있는 회복 효과가 없습니다.";

                    case ItemUseFailureReason.NotPlayerTurn:
                        return "플레이어 행동 차례에만 사용할 수 있습니다.";

                    case ItemUseFailureReason.BattleActionUnavailable:
                        return "지금은 전투 아이템을 사용할 수 없습니다.";

                    default:
                        return "아이템을 사용할 수 없습니다.";
                }
            }

            List<string> changes =
                new List<string>();

            if (result.HpRecovered > 0)
            {
                changes.Add(
                    $"HP +{result.HpRecovered}");
            }

            if (result.ManaRecovered > 0)
            {
                changes.Add(
                    $"MP +{result.ManaRecovered}");
            }

            if (result.StaminaRecovered > 0)
            {
                changes.Add(
                    $"정력 +{result.StaminaRecovered}");
            }

            return changes.Count > 0
                ? $"사용 완료 : {string.Join(" / ", changes)}"
                : "아이템을 사용했습니다.";
        }

        private void ClearSelection()
        {
            selectedSlotIndex =
                -1;

            isMoveMode =
                false;

            moveSourceSlotIndex =
                -1;

            lastUseMessage =
                string.Empty;

            if (discardConfirmPanel != null)
            {
                discardConfirmPanel.SetActive(
                    false);
            }

            ShowSelectedItemPanel(
                false);

            RefreshInventory();
        }

        private void ShowSelectedItemPanel(
            bool visible)
        {
            if (selectedItemPanel != null)
            {
                selectedItemPanel.SetActive(
                    visible);
            }
        }

        private InventoryRunState GetInventory()
        {
            return RunContext.Current != null
                ? RunContext.Current.Inventory
                : null;
        }
    }
}
