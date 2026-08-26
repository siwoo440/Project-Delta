using System;
using System.Collections.Generic;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 89일차: 플레이어 정보와 5x2 인벤토리 슬롯을 한 패널에서 표시한다.
    public sealed class PlayerInventoryHudController : MonoBehaviour
    {
        private const int VisibleSlotCount = 10;

        [SerializeField]
        private Text levelText;

        [SerializeField]
        private Text hpText;

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
        private Button[] slotButtons =
            Array.Empty<Button>();

        [SerializeField]
        private Image[] slotBackgrounds =
            Array.Empty<Image>();

        [SerializeField]
        private Image[] slotItemIcons =
            Array.Empty<Image>();

        [SerializeField]
        private Text[] slotNumberTexts =
            Array.Empty<Text>();

        [SerializeField]
        private ItemDefinition[] itemDefinitions =
            Array.Empty<ItemDefinition>();

        private readonly Dictionary<string, ItemDefinition> itemDefinitionById =
            new Dictionary<string, ItemDefinition>(
                StringComparer.Ordinal);

        private int selectedSlotIndex =
            -1;

        private Color normalSlotColor =
            new Color(
                0.15f,
                0.17f,
                0.22f,
                0.96f);

        private Color selectedSlotColor =
            new Color(
                0.42f,
                0.52f,
                0.72f,
                1f);

        public void Configure(
            Text configuredLevelText,
            Text configuredHpText,
            Text configuredManaText,
            Text configuredStaminaText,
            Text configuredCombatStatsText,
            GameObject configuredSelectedItemPanel,
            Image configuredSelectedItemIcon,
            Text configuredSelectedItemNameText,
            Text configuredSelectedItemDescriptionText,
            Button[] configuredSlotButtons,
            Image[] configuredSlotBackgrounds,
            Image[] configuredSlotItemIcons,
            Text[] configuredSlotNumberTexts,
            ItemDefinition[] configuredItemDefinitions)
        {
            levelText =
                configuredLevelText;

            hpText =
                configuredHpText;

            manaText =
                configuredManaText;

            staminaText =
                configuredStaminaText;

            combatStatsText =
                configuredCombatStatsText;

            selectedItemPanel =
                configuredSelectedItemPanel;

            selectedItemIcon =
                configuredSelectedItemIcon;

            selectedItemNameText =
                configuredSelectedItemNameText;

            selectedItemDescriptionText =
                configuredSelectedItemDescriptionText;

            slotButtons =
                configuredSlotButtons
                ?? Array.Empty<Button>();

            slotBackgrounds =
                configuredSlotBackgrounds
                ?? Array.Empty<Image>();

            slotItemIcons =
                configuredSlotItemIcons
                ?? Array.Empty<Image>();

            slotNumberTexts =
                configuredSlotNumberTexts
                ?? Array.Empty<Text>();

            itemDefinitions =
                configuredItemDefinitions
                ?? Array.Empty<ItemDefinition>();

            RebuildDefinitionLookup();
            BindSlotButtons();
            HideSelectedItem();
            RefreshAll();
        }

        private void Awake()
        {
            RebuildDefinitionLookup();
            BindSlotButtons();
            HideSelectedItem();
        }

        private void OnEnable()
        {
            RefreshAll();
        }

        private void Update()
        {
            RefreshPlayerInfo();
            RefreshInventory();
        }

        private void RebuildDefinitionLookup()
        {
            itemDefinitionById.Clear();

            for (int index = 0;
                 index < itemDefinitions.Length;
                 index++)
            {
                ItemDefinition definition =
                    itemDefinitions[index];

                if (definition == null
                    || string.IsNullOrEmpty(
                        definition.Id))
                {
                    continue;
                }

                itemDefinitionById[definition.Id] =
                    definition;
            }
        }

        private void BindSlotButtons()
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
                    () =>
                        SelectSlot(
                            capturedIndex));
            }
        }

        private void RefreshAll()
        {
            RefreshPlayerInfo();
            RefreshInventory();
            RefreshSelectedItem();
        }

        private void RefreshPlayerInfo()
        {
            RunContext context =
                RunContext.Current;

            if (context == null
                || context.Player == null)
            {
                SetText(
                    levelText,
                    "Lv. -");

                SetText(
                    hpText,
                    "HP  - / -");

                SetText(
                    manaText,
                    "MP  - / -");

                SetText(
                    staminaText,
                    "정력  - / -");

                SetText(
                    combatStatsText,
                    "ATK -   DEF -   SPD -");

                return;
            }

            StatBlock stats =
                context.Player.GetFinalStats();

            SetText(
                levelText,
                $"Lv. {context.Player.Level}");

            SetText(
                hpText,
                $"HP  {context.Player.CurrentHp} / {stats.MaxHealth}");

            SetText(
                manaText,
                $"MP  {context.Player.CurrentMana} / {stats.MaxMana}");

            SetText(
                staminaText,
                $"정력  {context.Player.CurrentStamina} / {stats.MaxStamina}");

            SetText(
                combatStatsText,
                $"ATK {stats.Attack}   DEF {stats.Defense}   SPD {stats.Speed}");
        }

        private void RefreshInventory()
        {
            InventoryRunState inventory =
                RunContext.Current?.Inventory;

            for (int index = 0;
                 index < VisibleSlotCount;
                 index++)
            {
                InventorySlotState slot =
                    null;

                bool hasSlot =
                    inventory != null
                    && inventory.TryGetSlot(
                        index,
                        out slot);

                bool hasItem =
                    hasSlot
                    && slot != null
                    && !slot.IsEmpty;

                Image icon =
                    index < slotItemIcons.Length
                        ? slotItemIcons[index]
                        : null;

                if (icon != null)
                {
                    Sprite sprite =
                        hasItem
                            ? ResolveDefinition(
                                slot.ItemId)?.Icon
                            : null;

                    icon.sprite =
                        sprite;

                    icon.enabled =
                        sprite != null;
                }

                if (index < slotNumberTexts.Length
                    && slotNumberTexts[index] != null)
                {
                    slotNumberTexts[index].text =
                        (index + 1).ToString();
                }

                if (index < slotBackgrounds.Length
                    && slotBackgrounds[index] != null)
                {
                    slotBackgrounds[index].color =
                        selectedSlotIndex == index
                        && hasItem
                            ? selectedSlotColor
                            : normalSlotColor;
                }
            }

            if (selectedSlotIndex >= 0)
            {
                if (inventory == null
                    || !inventory.TryGetSlot(
                        selectedSlotIndex,
                        out InventorySlotState selectedSlot)
                    || selectedSlot == null
                    || selectedSlot.IsEmpty)
                {
                    selectedSlotIndex =
                        -1;

                    HideSelectedItem();
                }
            }
        }

        private void SelectSlot(int slotIndex)
        {
            InventoryRunState inventory =
                RunContext.Current?.Inventory;

            if (inventory == null
                || !inventory.TryGetSlot(
                    slotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                selectedSlotIndex =
                    -1;

                HideSelectedItem();
                RefreshInventory();
                return;
            }

            selectedSlotIndex =
                slotIndex;

            RefreshSelectedItem();
            RefreshInventory();
        }

        private void RefreshSelectedItem()
        {
            if (selectedSlotIndex < 0)
            {
                HideSelectedItem();
                return;
            }

            InventoryRunState inventory =
                RunContext.Current?.Inventory;

            if (inventory == null
                || !inventory.TryGetSlot(
                    selectedSlotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                HideSelectedItem();
                return;
            }

            ItemDefinition definition =
                ResolveDefinition(
                    slot.ItemId);

            if (selectedItemPanel != null)
            {
                selectedItemPanel.SetActive(
                    true);
            }

            if (selectedItemIcon != null)
            {
                selectedItemIcon.sprite =
                    definition?.Icon;

                selectedItemIcon.enabled =
                    selectedItemIcon.sprite != null;
            }

            SetText(
                selectedItemNameText,
                definition != null
                    && !string.IsNullOrEmpty(
                        definition.DisplayName)
                        ? definition.DisplayName
                        : slot.DisplayName);

            SetText(
                selectedItemDescriptionText,
                definition != null
                    && !string.IsNullOrEmpty(
                        definition.Description)
                        ? definition.Description
                        : "아이템 설명이 아직 등록되지 않았습니다.");
        }

        private void HideSelectedItem()
        {
            if (selectedItemPanel != null)
            {
                selectedItemPanel.SetActive(
                    false);
            }
        }

        private ItemDefinition ResolveDefinition(
            string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            itemDefinitionById.TryGetValue(
                itemId,
                out ItemDefinition definition);

            return definition;
        }

        private static void SetText(
            Text target,
            string value)
        {
            if (target != null)
            {
                target.text =
                    value;
            }
        }
    }
}
