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

        private Button useButton;
        private ExplorationMonsterEncounterController encounterController;
        private string lastUseMessage =
            string.Empty;

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
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
            EnsureUseButton();
            HookButtons();

            selectedSlotIndex =
                -1;

            lastUseMessage =
                string.Empty;

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

            if (useButton != null)
            {
                useButton.onClick.RemoveAllListeners();

                useButton.onClick.AddListener(
                    OnUseButtonClicked);
            }
        }

        private void EnsureUseButton()
        {
            if (selectedItemPanel == null)
            {
                return;
            }

            Button[] existingButtons =
                selectedItemPanel.GetComponentsInChildren<Button>(
                    true);

            for (int index = 0;
                 index < existingButtons.Length;
                 index++)
            {
                if (existingButtons[index] != null
                    && existingButtons[index].name
                        == "UseButton")
                {
                    useButton =
                        existingButtons[index];

                    return;
                }
            }

            GameObject buttonObject =
                new GameObject(
                    "UseButton",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));

            buttonObject.transform.SetParent(
                selectedItemPanel.transform,
                false);

            RectTransform rectTransform =
                buttonObject.GetComponent<RectTransform>();

            rectTransform.anchorMin =
                new Vector2(
                    1f,
                    0f);

            rectTransform.anchorMax =
                new Vector2(
                    1f,
                    0f);

            rectTransform.pivot =
                new Vector2(
                    1f,
                    0f);

            rectTransform.sizeDelta =
                new Vector2(
                    90f,
                    32f);

            rectTransform.anchoredPosition =
                new Vector2(
                    -10f,
                    10f);

            Image background =
                buttonObject.GetComponent<Image>();

            background.color =
                new Color(
                    0.18f,
                    0.18f,
                    0.18f,
                    0.95f);

            useButton =
                buttonObject.GetComponent<Button>();

            GameObject textObject =
                new GameObject(
                    "Text",
                    typeof(RectTransform),
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

            Text text =
                textObject.GetComponent<Text>();

            text.text =
                "사용";

            text.alignment =
                TextAnchor.MiddleCenter;

            text.fontSize =
                16;

            text.color =
                Color.white;

            text.raycastTarget =
                false;

            text.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");
        }

        private void OnSlotClicked(
            int slotIndex)
        {
            InventoryRunState inventory =
                GetInventory();

            if (inventory == null
                || !inventory.TryGetSlot(
                    slotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                selectedSlotIndex =
                    -1;

                lastUseMessage =
                    string.Empty;

                ShowSelectedItemPanel(
                    false);

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
        }

        private void RefreshSlot(
            int index,
            InventorySlotState slot)
        {
            if (slotNumberTexts != null
                && index < slotNumberTexts.Length
                && slotNumberTexts[index] != null)
            {
                slotNumberTexts[index].text =
                    (index + 1).ToString();
            }

            bool hasItem =
                slot != null
                && !slot.IsEmpty;

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
                    hasItem;
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
                selectedSlotIndex =
                    -1;

                ShowSelectedItemPanel(
                    false);

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

            RefreshUseButton(
                inventory,
                slot,
                definition);
        }

        private void RefreshUseButton(
            InventoryRunState inventory,
            InventorySlotState slot,
            ItemDefinition definition)
        {
            if (useButton == null)
            {
                return;
            }

            bool categoryAllowsUse =
                definition != null
                && ItemCategoryRules.CanUse(
                    definition.Category);

            useButton.gameObject.SetActive(
                categoryAllowsUse);

            if (!categoryAllowsUse)
            {
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
