using System;
using UnityEngine;

namespace ProjectDelta.Data
{
    [CreateAssetMenu(
        fileName = "PlayerGrowthDefinition",
        menuName = "Project Delta/Data/Player Growth Definition")]
    public sealed class PlayerGrowthDefinition : ScriptableObject
    {
        public const int DefaultMaxLevel = 10;
        public const int DefaultStatPointsPerLevel = 1;

        private static readonly int[] DefaultExperienceToNextLevel =
        {
            100,
            150,
            220,
            300,
            400,
            520,
            660,
            820,
            1000
        };

        [Header("레벨")]
        [Min(1)]
        [SerializeField] private int maxLevel = DefaultMaxLevel;

        [Min(0)]
        [SerializeField] private int statPointsPerLevel =
            DefaultStatPointsPerLevel;

        [Header("현재 레벨 → 다음 레벨 필요 경험치")]
        [SerializeField] private int[] experienceToNextLevel =
        {
            100,
            150,
            220,
            300,
            400,
            520,
            660,
            820,
            1000
        };

        public int MaxLevel =>
            Math.Max(
                1,
                maxLevel);

        public int StatPointsPerLevel =>
            Math.Max(
                0,
                statPointsPerLevel);

        public int GetRequiredExperienceForNextLevel(
            int currentLevel)
        {
            if (currentLevel < 1
                || currentLevel >= MaxLevel)
            {
                return 0;
            }

            int index =
                currentLevel - 1;

            if (experienceToNextLevel == null
                || index < 0
                || index >= experienceToNextLevel.Length)
            {
                return 0;
            }

            return Math.Max(
                1,
                experienceToNextLevel[index]);
        }

        public static PlayerGrowthDefinition CreateDefaultRuntime()
        {
            return CreateRuntime(
                DefaultMaxLevel,
                DefaultStatPointsPerLevel,
                DefaultExperienceToNextLevel);
        }

        public static PlayerGrowthDefinition CreateRuntime(
            int maxLevel,
            int statPointsPerLevel,
            int[] experienceToNextLevel)
        {
            PlayerGrowthDefinition definition =
                CreateInstance<PlayerGrowthDefinition>();

            definition.maxLevel =
                Math.Max(
                    1,
                    maxLevel);

            definition.statPointsPerLevel =
                Math.Max(
                    0,
                    statPointsPerLevel);

            definition.experienceToNextLevel =
                experienceToNextLevel != null
                    ? (int[])experienceToNextLevel.Clone()
                    : Array.Empty<int>();

            return definition;
        }
    }
}
