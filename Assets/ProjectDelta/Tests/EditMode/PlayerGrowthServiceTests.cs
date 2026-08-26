using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class PlayerGrowthServiceTests
    {
        private readonly List<Object> createdObjects =
            new List<Object>();

        [TearDown]
        public void TearDown()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            for (int index = 0; index < createdObjects.Count; index++)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(
                        createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void ExperienceBelowThresholdDoesNotLevelUp()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            PlayerGrowthDefinition growth =
                CreateGrowthDefinition();

            BattleGrowthResult result =
                PlayerGrowthService.ApplyExperience(
                    player,
                    40,
                    growth);

            Assert.That(
                player.Level,
                Is.EqualTo(1));

            Assert.That(
                player.Experience,
                Is.EqualTo(40));

            Assert.That(
                player.UnusedStatPoints,
                Is.EqualTo(0));

            Assert.That(
                result.GainedLevels,
                Is.EqualTo(0));
        }

        [Test]
        public void ExactThresholdLevelsUpAndGrantsStatPoint()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            PlayerGrowthDefinition growth =
                CreateGrowthDefinition();

            BattleGrowthResult result =
                PlayerGrowthService.ApplyExperience(
                    player,
                    100,
                    growth);

            Assert.That(
                player.Level,
                Is.EqualTo(2));

            Assert.That(
                player.Experience,
                Is.EqualTo(0));

            Assert.That(
                player.UnusedStatPoints,
                Is.EqualTo(1));

            Assert.That(
                result.GainedStatPoints,
                Is.EqualTo(1));
        }

        [Test]
        public void LargeExperienceGainCanLevelUpMultipleTimes()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            PlayerGrowthDefinition growth =
                CreateGrowthDefinition();

            BattleGrowthResult result =
                PlayerGrowthService.ApplyExperience(
                    player,
                    300,
                    growth);

            Assert.That(
                player.Level,
                Is.EqualTo(3));

            Assert.That(
                player.Experience,
                Is.EqualTo(50));

            Assert.That(
                player.UnusedStatPoints,
                Is.EqualTo(2));

            Assert.That(
                result.GainedLevels,
                Is.EqualTo(2));
        }

        [Test]
        public void LevelTenDiscardsOverflowAndStopsFurtherGrowth()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Level =
                9;

            player.Experience =
                950;

            PlayerGrowthDefinition growth =
                CreateGrowthDefinition();

            BattleGrowthResult first =
                PlayerGrowthService.ApplyExperience(
                    player,
                    100,
                    growth);

            Assert.That(
                player.Level,
                Is.EqualTo(10));

            Assert.That(
                player.Experience,
                Is.EqualTo(0));

            Assert.That(
                first.GainedLevels,
                Is.EqualTo(1));

            int statPointsAtCap =
                player.UnusedStatPoints;

            BattleGrowthResult second =
                PlayerGrowthService.ApplyExperience(
                    player,
                    9999,
                    growth);

            Assert.That(
                player.Level,
                Is.EqualTo(10));

            Assert.That(
                player.Experience,
                Is.EqualTo(0));

            Assert.That(
                player.UnusedStatPoints,
                Is.EqualTo(statPointsAtCap));

            Assert.That(
                second.GainedLevels,
                Is.EqualTo(0));
        }

        [Test]
        public void BattleExperienceSumsAllMonsterRewards()
        {
            MonsterDefinition normal =
                CreateMonster(
                    20);

            MonsterDefinition rare =
                CreateMonster(
                    50);

            MonsterDefinition boss =
                CreateMonster(
                    120);

            int total =
                PlayerGrowthService.CalculateBattleExperience(
                    new[]
                    {
                        normal,
                        rare,
                        boss
                    });

            Assert.That(
                total,
                Is.EqualTo(190));
        }

        [Test]
        public void SaveMapperPreservesLevelExperienceAndUnusedStatPoints()
        {
            RunContext source =
                RunContext.Begin(
                    "DAY79_SAVE_TEST");

            source.Player.Level =
                4;

            source.Player.Experience =
                77;

            source.Player.UnusedStatPoints =
                3;

            RunData saved =
                DungeonSaveMapper.BuildFromRunContext(
                    source);

            Assert.That(
                saved.PlayerStats.Level,
                Is.EqualTo(4));

            Assert.That(
                saved.PlayerStats.Experience,
                Is.EqualTo(77));

            Assert.That(
                saved.PlayerStats.UnspentStatPoints,
                Is.EqualTo(3));

            RunContext.End();

            RunContext restored =
                RunContext.Begin(
                    "DAY79_RESTORE_TEST");

            DungeonSaveMapper.ApplyBasics(
                restored,
                saved);

            Assert.That(
                restored.Player.Level,
                Is.EqualTo(4));

            Assert.That(
                restored.Player.Experience,
                Is.EqualTo(77));

            Assert.That(
                restored.Player.UnusedStatPoints,
                Is.EqualTo(3));
        }

        private PlayerGrowthDefinition CreateGrowthDefinition()
        {
            PlayerGrowthDefinition definition =
                PlayerGrowthDefinition.CreateRuntime(
                    10,
                    1,
                    new[]
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
                    });

            createdObjects.Add(
                definition);

            return definition;
        }

        private MonsterDefinition CreateMonster(
            int experienceReward)
        {
            MonsterDefinition monster =
                ScriptableObject.CreateInstance<MonsterDefinition>();

            FieldInfo field =
                typeof(MonsterDefinition).GetField(
                    "experienceReward",
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null);

            field.SetValue(
                monster,
                experienceReward);

            createdObjects.Add(
                monster);

            return monster;
        }
    }
}
