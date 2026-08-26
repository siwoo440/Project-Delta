using System;
using System.Collections.Generic;
using System.Reflection;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    public sealed class PlayerInventoryHudController : MonoBehaviour
    {
        [SerializeField]
        private Text levelText;

        [SerializeField]
        private Text healthText;

        [SerializeField]
        private Text manaText;

        [SerializeField]
        private Text staminaText;

        [SerializeField]
        private Text combatStatsText;

        [SerializeField]
        private GameObject selectedItemPanel;

        [SerializeField]
        private Image selectedItemIcon;

        [SerializeField]
        private Text selectedItemNameText;

        [SerializeField]
        private Text selectedItemDescriptionText;

        [SerializeField]
        private Button[] slotButtons;

        [SerializeField]
        private Image[] slotBackgrounds;

        [SerializeField]
        private Image[] slotIcons;

        [SerializeField]
        private Text[] slotNumberTexts;

        [SerializeField]
        private Text[] slotQuantityTexts;

        [SerializeField]
        private ItemDefinition[] itemDefinitions;

        private readonly Dictionary<string, ItemDefinition> itemLookup =
            new Dictionary<string, ItemDefinition>();

        private int selectedSlotIndex = -1;

        public void Configure(
            Text configuredLevelText,
            Text configuredHealthText,
            Text configuredManaText,
            Text configuredStaminaText,
            Text configuredCombatStatsText,
            GameObject configuredSelectedItemPanel,
            Image configuredSelectedItemIcon,
            Text configuredSelectedItemNameText,
            Text configuredSelectedItemDescriptionText,
            Button[] configuredSlotButtons,
            Image[] configuredSlotBackgrounds,
            Image[] configuredSlotIcons,
            Text[] configuredSlotNumberTexts,
            ItemDefinition[] configuredItemDefinitions)
        {
            Configure(
                configuredLevelText,
                configuredHealthText,
                configuredManaText,
                configuredStaminaText,
                configuredCombatStatsText,
                configuredSelectedItemPanel,
                configuredSelectedItemIcon,
                configuredSelectedItemNameText,
                configuredSelectedItemDescriptionText,
                configuredSlotButtons,
                configuredSlotBackgrounds,
                configuredSlotIcons,
                configuredSlotNumberTexts,
                null,
                configuredItemDefinitions);
        }

        public void Configure(
            Text configuredLevelText,
            Text configuredHealthText,
            Text configuredManaText,
            Text configuredStaminaText,
            Text configuredCombatStatsText,
            GameObject configuredSelectedItemPanel,
            Image configuredSelectedItemIcon,
            Text configuredSelectedItemNameText,
            Text configuredSelectedItemDescriptionText,
            Button[] configuredSlotButtons,
            Image[] configuredSlotBackgrounds,
            Image[] configuredSlotIcons,
            Text[] configuredSlotNumberTexts,
            Text[] configuredSlotQuantityTexts,
            ItemDefinition[] configuredItemDefinitions)
        {
            levelText = configuredLevelText;
            healthText = configuredHealthText;
            manaText = configuredManaText;
            staminaText = configuredStaminaText;
            combatStatsText = configuredCombatStatsText;
            selectedItemPanel = configuredSelectedItemPanel;
            selectedItemIcon = configuredSelectedItemIcon;
            selectedItemNameText = configuredSelectedItemNameText;
            selectedItemDescriptionText = configuredSelectedItemDescriptionText;
            slotButtons = configuredSlotButtons;
            slotBackgrounds = configuredSlotBackgrounds;
            slotIcons = configuredSlotIcons;
            slotNumberTexts = configuredSlotNumberTexts;
            slotQuantityTexts = configuredSlotQuantityTexts;
            itemDefinitions = configuredItemDefinitions;

            Initialize();
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Initialize()
        {
            AutoBindQuantityTexts();
            RebuildItemLookup();
            HookButtons();

            if (selectedItemPanel != null)
            {
                selectedItemPanel.SetActive(
                    false);
            }

            InventoryRunState.MaxStackResolver =
                ResolveMaxStackSizeByItemId;

            Refresh();
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

                string definitionId =
                    TryReadDefinitionId(
                        definition);

                if (string.IsNullOrEmpty(definitionId))
                {
                    definitionId =
                        definition.name;
                }

                if (!itemLookup.ContainsKey(definitionId))
                {
                    itemLookup.Add(
                        definitionId,
                        definition);
                }
            }
        }

        private int ResolveMaxStackSizeByItemId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return 1;
            }

            return itemLookup.TryGetValue(
                itemId,
                out ItemDefinition definition)
                ? Math.Max(
                    1,
                    definition.MaxStackSize)
                : 1;
        }

        private string TryReadDefinitionId(ItemDefinition definition)
        {
            PropertyInfo property =
                definition.GetType().GetProperty(
                    "DefinitionId",
                    BindingFlags.Public | BindingFlags.Instance);

            if (property != null
                && property.PropertyType == typeof(string))
            {
                return property.GetValue(
                    definition) as string;
            }

            property =
                definition.GetType().GetProperty(
                    "Id",
                    BindingFlags.Public | BindingFlags.Instance);

            if (property != null
                && property.PropertyType == typeof(string))
            {
                return property.GetValue(
                    definition) as string;
            }

            FieldInfo field =
                definition.GetType().GetField(
                    "definitionId",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            if (field != null
                && field.FieldType == typeof(string))
            {
                return field.GetValue(
                    definition) as string;
            }

            field =
                definition.GetType().GetField(
                    "id",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            if (field != null
                && field.FieldType == typeof(string))
            {
                return field.GetValue(
                    definition) as string;
            }

            return string.Empty;
        }

        private void AutoBindQuantityTexts()
        {
            if (slotButtons == null
                || slotButtons.Length <= 0)
            {
                return;
            }

            if (slotQuantityTexts != null
                && slotQuantityTexts.Length == slotButtons.Length)
            {
                return;
            }

            slotQuantityTexts =
                new Text[slotButtons.Length];

            for (int index = 0;
                 index < slotButtons.Length;
                 index++)
            {
                if (slotButtons[index] == null)
                {
                    continue;
                }

                Transform target =
                    slotButtons[index].transform.Find(
                        "SlotQuantityText");

                if (target != null)
                {
                    slotQuantityTexts[index] =
                        target.GetComponent<Text>();
                }
            }
        }

        private void HookButtons()
        {
            if (slotButtons == null)
            {
                return;
            }

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
                    () => SelectSlot(
                        capturedIndex));
            }
        }

        private void SelectSlot(int slotIndex)
        {
            selectedSlotIndex =
                slotIndex;

            RefreshSelectedItem();
        }

        private void Refresh()
        {
            RefreshPlayerTexts();
            RefreshSlots();
            RefreshSelectedItem();
        }

        private void RefreshPlayerTexts()
        {
            RunContext context =
                RunContext.Current;

            if (context == null
                || context.Player == null)
            {
                return;
            }

            object player =
                context.Player;

            object finalStats =
                context.Player.GetFinalStats();

            if (levelText != null)
            {
                levelText.text =
                    $"Lv. {ReadInt(player, "Level", 1)}";
            }

            if (combatStatsText != null)
            {
                combatStatsText.text =
                    $"ATK {ReadInt(finalStats, "Attack", 0)}   DEF {ReadInt(finalStats, "Defense", 0)}   SPD {ReadInt(finalStats, "Speed", 0)}";
            }

            if (healthText != null)
            {
                healthText.text =
                    $"HP {ReadInt(player, "CurrentHp", 0)} / {ReadInt(finalStats, "MaxHealth", 0)}";
            }

            if (manaText != null)
            {
                manaText.text =
                    $"MP {ReadInt(player, "CurrentMana", 0)} / {ReadInt(finalStats, "MaxMana", 0)}";
            }

            if (staminaText != null)
            {
                staminaText.text =
                    $"정력 {ReadInt(player, "CurrentStamina", 0)} / {ReadInt(finalStats, "MaxStamina", 0)}";
            }
        }

        private int ReadInt(
            object target,
            string memberName,
            int fallback)
        {
            if (target == null)
            {
                return fallback;
            }

            Type type =
                target.GetType();

            PropertyInfo property =
                type.GetProperty(
                    memberName,
                    BindingFlags.Public | BindingFlags.Instance);

            if (property != null
                && property.PropertyType == typeof(int))
            {
                return (int)property.GetValue(
                    target);
            }

            FieldInfo field =
                type.GetField(
                    memberName,
                    BindingFlags.Public | BindingFlags.Instance);

            if (field != null
                && field.FieldType == typeof(int))
            {
                return (int)field.GetValue(
                    target);
            }

            return fallback;
        }

        private void RefreshSlots()
        {
            RunContext context =
                RunContext.Current;

            if (context == null
                || context.Inventory == null
                || slotButtons == null)
            {
                return;
            }

            for (int index = 0;
                 index < slotButtons.Length;
                 index++)
            {
                InventorySlotState slot =
                    index < context.Inventory.Slots.Count
                        ? context.Inventory.Slots[index]
                        : null;

                if (slotNumberTexts != null
                    && index < slotNumberTexts.Length
                    && slotNumberTexts[index] != null)
                {
                    slotNumberTexts[index].text =
                        (index + 1).ToString();
                }

                if (slotQuantityTexts != null
                    && index < slotQuantityTexts.Length
                    && slotQuantityTexts[index] != null)
                {
                    slotQuantityTexts[index].text =
                        slot != null
                        && !slot.IsEmpty
                        && slot.Quantity > 1
                            ? $"×{slot.Quantity}"
                            : string.Empty;
                }

                if (slotIcons != null
                    && index < slotIcons.Length
                    && slotIcons[index] != null)
                {
                    bool hasItem =
                        slot != null
                        && !slot.IsEmpty;

                    slotIcons[index].enabled =
                        hasItem;

                    if (hasItem)
                    {
                        slotIcons[index].sprite =
                            ResolveIcon(
                                slot.ItemId);
                        slotIcons[index].color =
                            Color.white;
                    }
                }

                if (slotBackgrounds != null
                    && index < slotBackgrounds.Length
                    && slotBackgrounds[index] != null)
                {
                    bool selected =
                        index == selectedSlotIndex;

                    slotBackgrounds[index].color =
                        selected
                            ? new Color(
                                0.27f,
                                0.36f,
                                0.55f,
                                1f)
                            : new Color(
                                0.14f,
                                0.16f,
                                0.21f,
                                1f);
                }
            }
        }

        private Sprite ResolveIcon(string itemId)
        {
            return itemLookup.TryGetValue(
                itemId,
                out ItemDefinition definition)
                ? definition.Icon
                : null;
        }

        private void RefreshSelectedItem()
        {
            RunContext context =
                RunContext.Current;

            if (selectedItemPanel == null
                || context == null
                || context.Inventory == null
                || selectedSlotIndex < 0
                || selectedSlotIndex >= context.Inventory.Slots.Count)
            {
                if (selectedItemPanel != null)
                {
                    selectedItemPanel.SetActive(
                        false);
                }

                return;
            }

            InventorySlotState slot =
                context.Inventory.Slots[selectedSlotIndex];

            if (slot == null
                || slot.IsEmpty)
            {
                selectedItemPanel.SetActive(
                    false);
                return;
            }

            selectedItemPanel.SetActive(
                true);

            ItemDefinition definition =
                itemLookup.TryGetValue(
                    slot.ItemId,
                    out ItemDefinition found)
                    ? found
                    : null;

            if (selectedItemIcon != null)
            {
                selectedItemIcon.sprite =
                    definition != null
                        ? definition.Icon
                        : null;
                selectedItemIcon.enabled =
                    selectedItemIcon.sprite != null;
            }

            if (selectedItemNameText != null)
            {
                selectedItemNameText.text =
                    string.IsNullOrEmpty(slot.DisplayName)
                        ? slot.ItemId
                        : slot.DisplayName;
            }

            if (selectedItemDescriptionText != null)
            {
                string baseDescription =
                    definition != null
                        ? definition.Description
                        : string.Empty;

                selectedItemDescriptionText.text =
                    $"보유 수량 ×{slot.Quantity}\n{baseDescription}".Trim();
            }
        }
    }
}
