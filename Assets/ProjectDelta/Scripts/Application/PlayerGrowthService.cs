using System;
using System.Collections.Generic;
using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    public static class PlayerGrowthService
    {
        public static int CalculateBattleExperience(
            IEnumerable<MonsterDefinition> defeatedMonsters)
        {
            if (defeatedMonsters == null)
            {
                return 0;
            }

            long total =
                0;

            foreach (MonsterDefinition monster
                     in defeatedMonsters)
            {
                if (monster == null)
                {
                    continue;
                }

                total +=
                    monster.ExperienceReward;

                if (total >= int.MaxValue)
                {
                    return int.MaxValue;
                }
            }

            return (int)total;
        }

        // 125일차: 경험치와 같은 방식으로 처치한 몬스터 목록을 기억의 조각 보상으로 환산한다.
        public static int CalculateMemoryShards(
            IEnumerable<MonsterDefinition> defeatedMonsters)
        {
            if (defeatedMonsters == null)
            {
                return 0;
            }

            long total =
                0;

            foreach (MonsterDefinition monster
                     in defeatedMonsters)
            {
                if (monster == null)
                {
                    continue;
                }

                total +=
                    monster.MemoryShardReward;

                if (total >= int.MaxValue)
                {
                    return int.MaxValue;
                }
            }

            return (int)total;
        }

        public static BattleGrowthResult ApplyBattleExperience(
            PlayerRunState player,
            IEnumerable<MonsterDefinition> defeatedMonsters,
            PlayerGrowthDefinition growthDefinition)
        {
            int earnedExperience =
                CalculateBattleExperience(
                    defeatedMonsters);

            return ApplyExperience(
                player,
                earnedExperience,
                growthDefinition);
        }

        public static BattleGrowthResult ApplyExperience(
            PlayerRunState player,
            int earnedExperience,
            PlayerGrowthDefinition growthDefinition)
        {
            if (player == null)
            {
                throw new ArgumentNullException(
                    nameof(player));
            }

            if (growthDefinition == null)
            {
                throw new ArgumentNullException(
                    nameof(growthDefinition));
            }

            int maxLevel =
                Math.Max(
                    1,
                    growthDefinition.MaxLevel);

            int normalizedLevel =
                Math.Max(
                    1,
                    Math.Min(
                        maxLevel,
                        player.Level));

            player.Level =
                normalizedLevel;

            player.Experience =
                Math.Max(
                    0,
                    player.Experience);

            player.UnusedStatPoints =
                Math.Max(
                    0,
                    player.UnusedStatPoints);

            int previousLevel =
                player.Level;

            int previousExperience =
                player.Experience;

            int safeEarnedExperience =
                Math.Max(
                    0,
                    earnedExperience);

            if (player.Level >= maxLevel)
            {
                player.Experience =
                    0;

                return CreateResult(
                    safeEarnedExperience,
                    previousLevel,
                    previousExperience,
                    player,
                    0,
                    0,
                    maxLevel);
            }

            long availableExperience =
                (long)player.Experience
                + safeEarnedExperience;

            int gainedLevels =
                0;

            int gainedStatPoints =
                0;

            while (player.Level < maxLevel)
            {
                int required =
                    growthDefinition
                        .GetRequiredExperienceForNextLevel(
                            player.Level);

                if (required <= 0
                    || availableExperience < required)
                {
                    break;
                }

                availableExperience -=
                    required;

                player.Level++;

                gainedLevels++;

                gainedStatPoints =
                    SaturatingAdd(
                        gainedStatPoints,
                        growthDefinition.StatPointsPerLevel);

                player.UnusedStatPoints =
                    SaturatingAdd(
                        player.UnusedStatPoints,
                        growthDefinition.StatPointsPerLevel);
            }

            if (player.Level >= maxLevel)
            {
                // Lv.10 이후의 경험치는 다음 성장 체계가 생기기 전까지 보관하지 않는다.
                availableExperience =
                    0;
            }

            player.Experience =
                (int)Math.Min(
                    int.MaxValue,
                    Math.Max(
                        0L,
                        availableExperience));

            return CreateResult(
                safeEarnedExperience,
                previousLevel,
                previousExperience,
                player,
                gainedLevels,
                gainedStatPoints,
                maxLevel);
        }

        private static BattleGrowthResult CreateResult(
            int earnedExperience,
            int previousLevel,
            int previousExperience,
            PlayerRunState player,
            int gainedLevels,
            int gainedStatPoints,
            int maxLevel)
        {
            return new BattleGrowthResult(
                earnedExperience,
                previousLevel,
                player.Level,
                previousExperience,
                player.Experience,
                gainedLevels,
                gainedStatPoints,
                player.Level >= maxLevel);
        }

        private static int SaturatingAdd(
            int current,
            int value)
        {
            long result =
                (long)Math.Max(
                    0,
                    current)
                + Math.Max(
                    0,
                    value);

            return result >= int.MaxValue
                ? int.MaxValue
                : (int)result;
        }
    }
}
