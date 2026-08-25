using System;
using System.Collections.Generic;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    public enum BattleRewardType
    {
        Gold,
        Health,
        Mana
    }

    public sealed class BattleRewardOption
    {
        public string Id { get; }
        public string DisplayName { get; }
        public BattleRewardType Type { get; }
        public int Amount { get; }

        public BattleRewardOption(
            string id,
            string displayName,
            BattleRewardType type,
            int amount)
        {
            Id =
                id;

            DisplayName =
                displayName;

            Type =
                type;

            Amount =
                Math.Max(
                    0,
                    amount);
        }
    }

    public static class BattleRewardState
    {
        private static readonly List<BattleRewardOption> currentOptions =
            new List<BattleRewardOption>();

        public static IReadOnlyList<BattleRewardOption> CurrentOptions =>
            currentOptions;

        public static bool IsPending { get; private set; }

        public static string LastClaimedRewardId { get; private set; }

        public static void BeginDefaultRewards()
        {
            currentOptions.Clear();

            currentOptions.Add(
                new BattleRewardOption(
                    "REWARD_GOLD_100",
                    "골드 +100",
                    BattleRewardType.Gold,
                    100));

            currentOptions.Add(
                new BattleRewardOption(
                    "REWARD_HEAL_10",
                    "HP +10",
                    BattleRewardType.Health,
                    10));

            currentOptions.Add(
                new BattleRewardOption(
                    "REWARD_MANA_5",
                    "MP +5",
                    BattleRewardType.Mana,
                    5));

            LastClaimedRewardId =
                null;

            IsPending =
                true;
        }

        public static bool TryClaim(
            string rewardId,
            PlayerRunState player)
        {
            if (!IsPending
                || player == null
                || string.IsNullOrEmpty(
                    rewardId))
            {
                return false;
            }

            BattleRewardOption selected =
                FindOption(
                    rewardId);

            if (selected == null)
            {
                return false;
            }

            ApplyReward(
                selected,
                player);

            LastClaimedRewardId =
                selected.Id;

            IsPending =
                false;

            return true;
        }

        public static void Clear()
        {
            currentOptions.Clear();

            LastClaimedRewardId =
                null;

            IsPending =
                false;
        }

        private static BattleRewardOption FindOption(
            string rewardId)
        {
            foreach (BattleRewardOption option in currentOptions)
            {
                if (option.Id == rewardId)
                {
                    return option;
                }
            }

            return null;
        }

        private static void ApplyReward(
            BattleRewardOption option,
            PlayerRunState player)
        {
            switch (option.Type)
            {
                case BattleRewardType.Gold:
                    player.Gold +=
                        option.Amount;
                    break;

                case BattleRewardType.Health:
                    StatBlock healthStats =
                        player.GetFinalStats();

                    player.CurrentHp =
                        Math.Min(
                            healthStats.MaxHealth,
                            player.CurrentHp
                            + option.Amount);
                    break;

                case BattleRewardType.Mana:
                    StatBlock manaStats =
                        player.GetFinalStats();

                    player.CurrentMana =
                        Math.Min(
                            manaStats.MaxMana,
                            player.CurrentMana
                            + option.Amount);
                    break;
            }
        }
    }
}
