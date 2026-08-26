using System;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    public static class BattleRewardPayoutService
    {
        public static int ApplyDropGold(
            PlayerRunState player,
            BattleDropResult drop)
        {
            if (player == null
                || drop == null
                || drop.Gold <= 0)
            {
                return 0;
            }

            int before =
                Math.Max(
                    0,
                    player.Gold);

            long after =
                (long)before
                + drop.Gold;

            player.Gold =
                after >= int.MaxValue
                    ? int.MaxValue
                    : (int)after;

            return player.Gold
                - before;
        }

        public static int ApplyDropItems(
            InventoryRunState inventory,
            BattleDropResult drop)
        {
            if (inventory == null
                || drop == null
                || drop.Items == null)
            {
                return 0;
            }

            int addedCount =
                0;

            for (int itemIndex = 0;
                 itemIndex < drop.Items.Count;
                 itemIndex++)
            {
                BattleDropItemResult item =
                    drop.Items[itemIndex];

                if (item == null
                    || string.IsNullOrEmpty(
                        item.ItemId)
                    || item.Quantity <= 0)
                {
                    continue;
                }

                for (int quantityIndex = 0;
                     quantityIndex < item.Quantity;
                     quantityIndex++)
                {
                    inventory.Add(
                        new InventoryItemStack(
                            item.ItemId,
                            string.IsNullOrEmpty(
                                item.DisplayName)
                                ? item.ItemId
                                : item.DisplayName));

                    if (addedCount
                        < int.MaxValue)
                    {
                        addedCount++;
                    }
                }
            }

            return addedCount;
        }

        public static void ApplyAutomaticDrops(
            RunContext context,
            BattleDropResult drop)
        {
            if (context == null)
            {
                return;
            }

            ApplyDropGold(
                context.Player,
                drop);

            ApplyDropItems(
                context.Inventory,
                drop);
        }
    }
}
