using System;
using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ChestInteractionView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject normalRoot;
        [SerializeField] private Text interactionPromptText;
        [SerializeField] private Button closeButton;

        [Header("Chest")]
        [SerializeField] private Transform chestItemsRoot;
        [SerializeField] private Button chestItemTemplate;

        [Header("Inventory")]
        [SerializeField] private Transform inventoryItemsRoot;
        [SerializeField] private Button inventoryItemTemplate;

        [Header("Overflow")]
        [SerializeField] private GameObject overflowRoot;
        [SerializeField] private Text overflowMessageText;
        [SerializeField] private GameObject replacementListRoot;
        [SerializeField] private Transform replacementItemsRoot;
        [SerializeField] private Button replacementItemTemplate;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button replaceButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button backButton;

        private readonly List<Button> chestRows =
            new List<Button>();

        private readonly List<Button> inventoryRows =
            new List<Button>();

        private readonly List<Button> replacementRows =
            new List<Button>();

        public event Action<int> ChestItemClicked;
        public event Action<int> ReplacementSlotClicked;
        public event Action CloseRequested;
        public event Action LeaveRequested;
        public event Action BeginReplacementRequested;
        public event Action CancelAcquisitionRequested;
        public event Action BackReplacementRequested;

        private void Awake()
        {
            HookStaticButtons();
            SetPanelVisible(false);
            SetInteractionPrompt(string.Empty);
        }

        public void SetInteractionPrompt(string message)
        {
            if (interactionPromptText == null)
            {
                return;
            }

            string normalized =
                message ?? string.Empty;

            if (interactionPromptText.text != normalized)
            {
                interactionPromptText.text = normalized;
            }

            bool visible =
                !string.IsNullOrEmpty(normalized);

            if (interactionPromptText.gameObject.activeSelf != visible)
            {
                interactionPromptText.gameObject.SetActive(visible);
            }
        }

        public void SetPanelVisible(bool visible)
        {
            if (panelRoot != null
                && panelRoot.activeSelf != visible)
            {
                panelRoot.SetActive(visible);
            }
        }

        public void Render(
            IReadOnlyList<string> chestItems,
            InventoryRunState inventory,
            bool overflowOpen,
            bool selectingReplacement,
            string overflowStatusText)
        {
            SetPanelVisible(true);

            if (normalRoot != null)
            {
                normalRoot.SetActive(true);
            }

            RenderChestRows(
                chestItems,
                !overflowOpen);

            RenderInventoryRows(inventory);

            if (closeButton != null)
            {
                closeButton.interactable =
                    !overflowOpen;
            }

            if (overflowRoot != null)
            {
                overflowRoot.SetActive(overflowOpen);
            }

            if (!overflowOpen)
            {
                return;
            }

            if (overflowMessageText != null)
            {
                overflowMessageText.text =
                    overflowStatusText ?? string.Empty;
            }

            if (replacementListRoot != null)
            {
                replacementListRoot.SetActive(selectingReplacement);
            }

            if (selectingReplacement)
            {
                RenderReplacementRows(inventory);
            }
            else
            {
                HideUnusedRows(replacementRows, 0);
            }

            SetButtonVisible(leaveButton, !selectingReplacement);
            SetButtonVisible(replaceButton, !selectingReplacement);
            SetButtonVisible(cancelButton, !selectingReplacement);
            SetButtonVisible(backButton, selectingReplacement);
        }

        private void HookStaticButtons()
        {
            HookButton(
                closeButton,
                () => CloseRequested?.Invoke());

            HookButton(
                leaveButton,
                () => LeaveRequested?.Invoke());

            HookButton(
                replaceButton,
                () => BeginReplacementRequested?.Invoke());

            HookButton(
                cancelButton,
                () => CancelAcquisitionRequested?.Invoke());

            HookButton(
                backButton,
                () => BackReplacementRequested?.Invoke());
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
            button.onClick.AddListener(action);
        }

        private void RenderChestRows(
            IReadOnlyList<string> chestItems,
            bool interactable)
        {
            int count =
                chestItems != null
                    ? chestItems.Count
                    : 0;

            if (count == 0)
            {
                Button row =
                    GetOrCreateRow(
                        chestRows,
                        chestItemTemplate,
                        chestItemsRoot,
                        0);

                SetRow(
                    row,
                    "(비어있음)",
                    false,
                    null);

                HideUnusedRows(chestRows, 1);
                return;
            }

            for (int index = 0; index < count; index++)
            {
                int capturedIndex = index;

                Button row =
                    GetOrCreateRow(
                        chestRows,
                        chestItemTemplate,
                        chestItemsRoot,
                        index);

                SetRow(
                    row,
                    chestItems[index],
                    interactable,
                    () => ChestItemClicked?.Invoke(capturedIndex));
            }

            HideUnusedRows(chestRows, count);
        }

        private void RenderInventoryRows(
            InventoryRunState inventory)
        {
            int count =
                inventory != null
                    ? inventory.Slots.Count
                    : 0;

            for (int index = 0; index < count; index++)
            {
                Button row =
                    GetOrCreateRow(
                        inventoryRows,
                        inventoryItemTemplate,
                        inventoryItemsRoot,
                        index);

                SetRow(
                    row,
                    BuildInventorySlotText(
                        index,
                        inventory.Slots[index]),
                    false,
                    null);
            }

            HideUnusedRows(inventoryRows, count);
        }

        private void RenderReplacementRows(
            InventoryRunState inventory)
        {
            int count =
                inventory != null
                    ? inventory.Slots.Count
                    : 0;

            for (int index = 0; index < count; index++)
            {
                int capturedIndex = index;
                InventorySlotState slot = inventory.Slots[index];

                ItemCategory category =
                    slot == null
                    || slot.IsEmpty
                        ? ItemCategory.Uncategorized
                        : RuntimeItemDefinitionLookup.ResolveCategory(
                            slot.ItemId);

                bool canReplace =
                    slot != null
                    && !slot.IsEmpty
                    && InventoryAcquisitionService.CanReplaceTarget(
                        category);

                Button row =
                    GetOrCreateRow(
                        replacementRows,
                        replacementItemTemplate,
                        replacementItemsRoot,
                        index);

                SetRow(
                    row,
                    BuildInventorySlotText(index, slot),
                    canReplace,
                    () => ReplacementSlotClicked?.Invoke(capturedIndex));
            }

            HideUnusedRows(replacementRows, count);
        }

        private static string BuildInventorySlotText(
            int index,
            InventorySlotState slot)
        {
            if (slot == null
                || slot.IsEmpty)
            {
                return $"{index + 1}. (빈 슬롯)";
            }

            return $"{index + 1}. {slot.DisplayName} ×{slot.Quantity}";
        }

        private static Button GetOrCreateRow(
            List<Button> rows,
            Button template,
            Transform parent,
            int index)
        {
            if (template == null
                || parent == null)
            {
                return null;
            }

            while (rows.Count <= index)
            {
                Button row =
                    Instantiate(
                        template,
                        parent);

                row.name =
                    $"{template.name}_Runtime_{rows.Count + 1}";

                rows.Add(row);
            }

            Button result = rows[index];

            if (result != null
                && !result.gameObject.activeSelf)
            {
                result.gameObject.SetActive(true);
            }

            return result;
        }

        private static void SetRow(
            Button row,
            string text,
            bool interactable,
            UnityEngine.Events.UnityAction action)
        {
            if (row == null)
            {
                return;
            }

            row.interactable = interactable;

            Text label =
                row.GetComponentInChildren<Text>(true);

            if (label != null)
            {
                label.text = text ?? string.Empty;
            }

            row.onClick.RemoveAllListeners();

            if (action != null)
            {
                row.onClick.AddListener(action);
            }
        }

        private static void HideUnusedRows(
            List<Button> rows,
            int usedCount)
        {
            for (int index = usedCount; index < rows.Count; index++)
            {
                Button row = rows[index];

                if (row != null
                    && row.gameObject.activeSelf)
                {
                    row.gameObject.SetActive(false);
                }
            }
        }

        private static void SetButtonVisible(
            Button button,
            bool visible)
        {
            if (button != null
                && button.gameObject.activeSelf != visible)
            {
                button.gameObject.SetActive(visible);
            }
        }
    }
}
