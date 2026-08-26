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
        private Button moveButton;
        private Button discardButton;

        private Text moveButtonText;

        private GameObject discardConfirmPanel;
        private Text discardConfirmText;
        private Button discardOneButton;
        private Button discardAllButton;
        private Button discardCancelButton;

        private bool isMoveMode;
        private int moveSourceSlotIndex =
            -1;

        private ExplorationMonsterEncounterController encounterController;

        private string lastUseMessage =
            string.Empty;

        private bool IsDiscardConfirmOpen =>
            discardConfirmPanel != null
            && discardConfirmPanel.activeSelf;

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
            EnsureActionUi();
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

        private void EnsureActionUi()
        {
            if (selectedItemPanel == null)
            {
                return;
            }

            useButton =
                EnsureActionButton(
                    "UseButton",
                    "사용",
                    -210f);

            moveButton =
                EnsureActionButton(
                    "MoveButton",
                    "이동",
                    -110f);

            discardButton =
                EnsureActionButton(
                    "DiscardButton",
                    "버리기",
                    -10f);

            moveButtonText =
                moveButton != null
                    ? FindButtonText(
                        moveButton)
                    : null;

            EnsureDiscardConfirmPanel();
        }

        private Button EnsureActionButton(
            string objectName,
            string label,
            float anchoredX)
        {
            Button button =
                FindButtonByName(
                    objectName);

            if (button == null)
            {
                GameObject buttonObject =
                    new GameObject(
                        objectName,
                        typeof(RectTransform),
                        typeof(Image),
                        typeof(Button));

                buttonObject.transform.SetParent(
                    selectedItemPanel.transform,
                    false);

                Image background =
                    buttonObject.GetComponent<Image>();

                background.color =
                    new Color(
                        0.18f,
                        0.18f,
                        0.18f,
                        0.95f);

                button =
                    buttonObject.GetComponent<Button>();
            }

            RectTransform rectTransform =
                button.GetComponent<RectTransform>();

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
                    anchoredX,
                    10f);

            Text text =
                EnsureButtonText(
                    button,
                    label);

            text.text =
                label;

            return button;
        }

        private Button FindButtonByName(
            string objectName)
        {
            if (selectedItemPanel == null)
            {
                return null;
            }

            Button[] buttons =
                selectedItemPanel.GetComponentsInChildren<Button>(
                    true);

            for (int index = 0;
                 index < buttons.Length;
                 index++)
            {
                Button button =
                    buttons[index];

                if (button != null
                    && button.name
                        == objectName)
                {
                    return button;
                }
            }

            return null;
        }

        private static Text FindButtonText(
            Button button)
        {
            if (button == null)
            {
                return null;
            }

            Transform textTransform =
                button.transform.Find(
                    "Text");

            return textTransform != null
                ? textTransform.GetComponent<Text>()
                : button.GetComponentInChildren<Text>(
                    true);
        }

        private static Text EnsureButtonText(
            Button button,
            string label)
        {
            Text text =
                FindButtonText(
                    button);

            if (text != null)
            {
                return text;
            }

            GameObject textObject =
                new GameObject(
                    "Text",
                    typeof(RectTransform),
                    typeof(Text));

            textObject.transform.SetParent(
                button.transform,
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

            text =
                textObject.GetComponent<Text>();

            text.text =
                label;

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

            return text;
        }

        private void EnsureDiscardConfirmPanel()
        {
            Transform existing =
                selectedItemPanel.transform.Find(
                    "DiscardConfirmPanel");

            if (existing != null)
            {
                discardConfirmPanel =
                    existing.gameObject;
            }
            else
            {
                discardConfirmPanel =
                    new GameObject(
                        "DiscardConfirmPanel",
                        typeof(RectTransform),
                        typeof(Image));

                discardConfirmPanel.transform.SetParent(
                    selectedItemPanel.transform,
                    false);
            }

            RectTransform panelRect =
                discardConfirmPanel.GetComponent<RectTransform>();

            panelRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f);

            panelRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f);

            panelRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f);

            panelRect.sizeDelta =
                new Vector2(
                    360f,
                    150f);

            panelRect.anchoredPosition =
                Vector2.zero;

            Image panelImage =
                discardConfirmPanel.GetComponent<Image>();

            panelImage.color =
                new Color(
                    0.06f,
                    0.06f,
                    0.06f,
                    0.98f);

            discardConfirmText =
                EnsureDiscardConfirmText();

            discardOneButton =
                EnsureDiscardPanelButton(
                    "DiscardOneButton",
                    "1개 버리기",
                    -110f);

            discardAllButton =
                EnsureDiscardPanelButton(
                    "DiscardAllButton",
                    "전부 버리기",
                    0f);

            discardCancelButton =
                EnsureDiscardPanelButton(
                    "DiscardCancelButton",
                    "취소",
                    110f);

            discardConfirmPanel.transform.SetAsLastSibling();

            discardConfirmPanel.SetActive(
                false);
        }

        private Text EnsureDiscardConfirmText()
        {
            Transform existing =
                discardConfirmPanel.transform.Find(
                    "MessageText");

            Text text;

            if (existing != null)
            {
                text =
                    existing.GetComponent<Text>();
            }
            else
            {
                GameObject textObject =
                    new GameObject(
                        "MessageText",
                        typeof(RectTransform),
                        typeof(Text));

                textObject.transform.SetParent(
                    discardConfirmPanel.transform,
                    false);

                text =
                    textObject.GetComponent<Text>();
            }

            RectTransform rectTransform =
                text.GetComponent<RectTransform>();

            rectTransform.anchorMin =
                new Vector2(
                    0f,
                    0f);

            rectTransform.anchorMax =
                new Vector2(
                    1f,
                    1f);

            rectTransform.offsetMin =
                new Vector2(
                    15f,
                    58f);

            rectTransform.offsetMax =
                new Vector2(
                    -15f,
                    -15f);

            text.alignment =
                TextAnchor.MiddleCenter;

            text.fontSize =
                16;

            text.color =
                Color.white;

            text.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            text.raycastTarget =
                false;

            return text;
        }

        private Button EnsureDiscardPanelButton(
            string objectName,
            string label,
            float anchoredX)
        {
            Transform existing =
                discardConfirmPanel.transform.Find(
                    objectName);

            Button button;

            if (existing != null)
            {
                button =
                    existing.GetComponent<Button>();
            }
            else
            {
                GameObject buttonObject =
                    new GameObject(
                        objectName,
                        typeof(RectTransform),
                        typeof(Image),
                        typeof(Button));

                buttonObject.transform.SetParent(
                    discardConfirmPanel.transform,
                    false);

                button =
                    buttonObject.GetComponent<Button>();

                Image image =
                    buttonObject.GetComponent<Image>();

                image.color =
                    new Color(
                        0.18f,
                        0.18f,
                        0.18f,
                        1f);
            }

            RectTransform rectTransform =
                button.GetComponent<RectTransform>();

            rectTransform.anchorMin =
                new Vector2(
                    0.5f,
                    0f);

            rectTransform.anchorMax =
                new Vector2(
                    0.5f,
                    0f);

            rectTransform.pivot =
                new Vector2(
                    0.5f,
                    0f);

            rectTransform.sizeDelta =
                new Vector2(
                    100f,
                    34f);

            rectTransform.anchoredPosition =
                new Vector2(
                    anchoredX,
                    15f);

            EnsureButtonText(
                button,
                label).text =
                label;

            return button;
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
