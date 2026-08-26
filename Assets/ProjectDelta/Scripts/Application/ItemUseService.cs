using System;
using System.Collections.Generic;
using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    public enum ItemUseFailureReason
    {
        None = 0,

        InvalidInventory = 1,

        InvalidSlot = 2,

        ItemNotFound = 3,

        ItemMismatch = 4,

        ItemNotUsable = 5,

        WrongContext = 6,

        NoEffects = 7,

        NoApplicableEffect = 8,

        BattleActionUnavailable = 9,

        NotPlayerTurn = 10
    }

    public sealed class ItemUseResult
    {
        public bool Success { get; set; }

        public ItemUseFailureReason FailureReason { get; set; }

        public int ConsumedQuantity { get; set; }

        public int HpRecovered { get; set; }

        public int ManaRecovered { get; set; }

        public int StaminaRecovered { get; set; }

        public bool HasResourceChange =>
            HpRecovered > 0
            || ManaRecovered > 0
            || StaminaRecovered > 0;

        public static ItemUseResult Failed(
            ItemUseFailureReason reason)
        {
            return new ItemUseResult
            {
                Success =
                    false,
                FailureReason =
                    reason
            };
        }
    }

    public static class ItemUseService
    {
        public static ItemUseResult PreviewExploration(
            InventoryRunState inventory,
            int slotIndex,
            PlayerRunState player,
            ItemDefinition definition)
        {
            ItemUseResult validation =
                ValidateCommon(
                    inventory,
                    slotIndex,
                    definition,
                    ItemUseContext.Exploration);

            if (validation != null)
            {
                return validation;
            }

            if (player == null)
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.InvalidInventory);
            }

            StatBlock finalStats =
                player.GetFinalStats();

            return BuildResult(
                definition,
                player.CurrentHp,
                Math.Max(
                    0,
                    finalStats.MaxHealth),
                player.CurrentMana,
                Math.Max(
                    0,
                    finalStats.MaxMana),
                player.CurrentStamina,
                Math.Max(
                    0,
                    finalStats.MaxStamina));
        }

        public static ItemUseResult CommitExploration(
            InventoryRunState inventory,
            int slotIndex,
            PlayerRunState player,
            ItemDefinition definition)
        {
            ItemUseResult preview =
                PreviewExploration(
                    inventory,
                    slotIndex,
                    player,
                    definition);

            if (!preview.Success)
            {
                return preview;
            }

            if (!TryConsumeOne(
                    inventory,
                    slotIndex))
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.InvalidSlot);
            }

            player.CurrentHp =
                Math.Max(
                    0,
                    player.CurrentHp
                    + preview.HpRecovered);

            player.CurrentMana =
                Math.Max(
                    0,
                    player.CurrentMana
                    + preview.ManaRecovered);

            player.CurrentStamina =
                Math.Max(
                    0,
                    player.CurrentStamina
                    + preview.StaminaRecovered);

            preview.ConsumedQuantity =
                1;

            return preview;
        }

        public static ItemUseResult PreviewBattle(
            InventoryRunState inventory,
            int slotIndex,
            BattleParticipant player,
            ItemDefinition definition)
        {
            ItemUseResult validation =
                ValidateCommon(
                    inventory,
                    slotIndex,
                    definition,
                    ItemUseContext.Battle);

            if (validation != null)
            {
                return validation;
            }

            if (player == null)
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.BattleActionUnavailable);
            }

            return BuildResult(
                definition,
                player.CurrentHp,
                player.MaxHp,
                player.CurrentMana,
                player.MaxMana,
                player.CurrentStamina,
                player.MaxStamina);
        }

        public static ItemUseResult CommitBattle(
            InventoryRunState inventory,
            int slotIndex,
            BattleParticipant player,
            ItemDefinition definition)
        {
            ItemUseResult preview =
                PreviewBattle(
                    inventory,
                    slotIndex,
                    player,
                    definition);

            if (!preview.Success)
            {
                return preview;
            }

            if (!TryConsumeOne(
                    inventory,
                    slotIndex))
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.InvalidSlot);
            }

            if (preview.HpRecovered > 0)
            {
                player.Heal(
                    preview.HpRecovered);
            }

            if (preview.ManaRecovered > 0)
            {
                player.RestoreMana(
                    preview.ManaRecovered);
            }

            if (preview.StaminaRecovered > 0)
            {
                player.RestoreStamina(
                    preview.StaminaRecovered);
            }

            preview.ConsumedQuantity =
                1;

            return preview;
        }

        private static ItemUseResult ValidateCommon(
            InventoryRunState inventory,
            int slotIndex,
            ItemDefinition definition,
            ItemUseContext requestedContext)
        {
            if (inventory == null)
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.InvalidInventory);
            }

            if (!inventory.TryGetSlot(
                    slotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.InvalidSlot);
            }

            if (definition == null)
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.ItemNotFound);
            }

            if (!MatchesDefinition(
                    slot,
                    definition))
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.ItemMismatch);
            }

            if (!ItemCategoryRules.CanUse(
                    definition.Category))
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.ItemNotUsable);
            }

            if (!AllowsContext(
                    definition.UseContext,
                    requestedContext))
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.WrongContext);
            }

            return null;
        }

        private static bool MatchesDefinition(
            InventorySlotState slot,
            ItemDefinition definition)
        {
            if (slot == null
                || definition == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(
                    definition.Id)
                && slot.ItemId
                    == definition.Id)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(
                    definition.name)
                && slot.ItemId
                    == definition.name)
            {
                return true;
            }

            return !string.IsNullOrEmpty(
                    definition.DisplayName)
                && (slot.ItemId
                        == definition.DisplayName
                    || slot.DisplayName
                        == definition.DisplayName);
        }

        private static bool AllowsContext(
            ItemUseContext configuredContext,
            ItemUseContext requestedContext)
        {
            return configuredContext
                    == ItemUseContext.Both
                || configuredContext
                    == requestedContext;
        }

        private static ItemUseResult BuildResult(
            ItemDefinition definition,
            int currentHp,
            int maxHp,
            int currentMana,
            int maxMana,
            int currentStamina,
            int maxStamina)
        {
            IReadOnlyList<ItemUseEffectDefinition> effects =
                definition != null
                    ? definition.UseEffects
                    : null;

            if (effects == null
                || effects.Count == 0)
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.NoEffects);
            }

            int hpRequest =
                0;

            int manaRequest =
                0;

            int staminaRequest =
                0;

            bool hasSupportedEffect =
                false;

            for (int index = 0;
                 index < effects.Count;
                 index++)
            {
                ItemUseEffectDefinition effect =
                    effects[index];

                if (effect == null
                    || effect.Value <= 0)
                {
                    continue;
                }

                switch (effect.Kind)
                {
                    case ItemUseEffectKind.RestoreHp:
                        hpRequest +=
                            effect.Value;

                        hasSupportedEffect =
                            true;

                        break;

                    case ItemUseEffectKind.RestoreMana:
                        manaRequest +=
                            effect.Value;

                        hasSupportedEffect =
                            true;

                        break;

                    case ItemUseEffectKind.RestoreStamina:
                        staminaRequest +=
                            effect.Value;

                        hasSupportedEffect =
                            true;

                        break;
                }
            }

            if (!hasSupportedEffect)
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.NoEffects);
            }

            int hpRecovered =
                Math.Min(
                    Math.Max(
                        0,
                        maxHp - currentHp),
                    hpRequest);

            int manaRecovered =
                Math.Min(
                    Math.Max(
                        0,
                        maxMana - currentMana),
                    manaRequest);

            int staminaRecovered =
                Math.Min(
                    Math.Max(
                        0,
                        maxStamina - currentStamina),
                    staminaRequest);

            if (hpRecovered <= 0
                && manaRecovered <= 0
                && staminaRecovered <= 0)
            {
                return ItemUseResult.Failed(
                    ItemUseFailureReason.NoApplicableEffect);
            }

            return new ItemUseResult
            {
                Success =
                    true,
                FailureReason =
                    ItemUseFailureReason.None,
                HpRecovered =
                    hpRecovered,
                ManaRecovered =
                    manaRecovered,
                StaminaRecovered =
                    staminaRecovered
            };
        }

        private static bool TryConsumeOne(
            InventoryRunState inventory,
            int slotIndex)
        {
            return inventory != null
                && inventory.TryRemoveQuantityAt(
                    slotIndex,
                    1,
                    out int removedQuantity)
                && removedQuantity == 1;
        }
    }
}
