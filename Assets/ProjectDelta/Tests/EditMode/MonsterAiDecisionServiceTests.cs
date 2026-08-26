using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class MonsterAiDecisionServiceTests
    {
        [Test]
        public void NullProfileFallsBackToBasicAttack()
        {
            BattleParticipant actor =
                CreateEnemy(
                    maxMana: 0);

            BattleParticipant player =
                CreatePlayer();

            bool created =
                MonsterAiDecisionService.TryCreateIntent(
                    actor,
                    player,
                    null,
                    false,
                    new FixedRandomSource(
                        0),
                    out BattleIntent intent);

            Assert.That(
                created,
                Is.True);

            Assert.That(
                intent.CommandId,
                Is.EqualTo("Attack"));

            Assert.That(
                intent.TargetInstanceId,
                Is.EqualTo("PLAYER"));
        }

        [Test]
        public void LowHpAddsConfiguredDefendWeight()
        {
            BattleParticipant actor =
                CreateEnemy(
                    maxHp: 100);

            actor.ApplyDamage(
                61);

            int weight =
                MonsterAiDecisionService.GetEffectiveDefendWeight(
                    actor,
                    20,
                    40,
                    30);

            Assert.That(
                weight,
                Is.EqualTo(50));
        }

        [Test]
        public void SkillIsExcludedWhenManaIsInsufficient()
        {
            SkillDefinition skill =
                CreateSkill(
                    "SKILL_TEST_MANA",
                    5,
                    SkillTargetType.Enemy);

            try
            {
                BattleParticipant actor =
                    CreateEnemy(
                        maxMana: 0);

                BattleParticipant player =
                    CreatePlayer();

                MonsterAiSkillEntry[] skills =
                {
                    new MonsterAiSkillEntry(
                        skill,
                        100)
                };

                bool created =
                    MonsterAiDecisionService.TryCreateIntent(
                        actor,
                        player,
                        0,
                        10,
                        0,
                        0,
                        skills,
                        false,
                        new FixedRandomSource(
                            0),
                        out BattleIntent intent);

                Assert.That(
                    created,
                    Is.True);

                Assert.That(
                    intent.CommandId,
                    Is.EqualTo("Defend"));
            }
            finally
            {
                Object.DestroyImmediate(
                    skill);
            }
        }

        [Test]
        public void SkillCanBeSelectedWhenResourcesAreEnough()
        {
            SkillDefinition skill =
                CreateSkill(
                    "SKILL_TEST_READY",
                    5,
                    SkillTargetType.Enemy);

            try
            {
                BattleParticipant actor =
                    CreateEnemy(
                        maxMana: 10);

                BattleParticipant player =
                    CreatePlayer();

                MonsterAiSkillEntry[] skills =
                {
                    new MonsterAiSkillEntry(
                        skill,
                        100)
                };

                bool created =
                    MonsterAiDecisionService.TryCreateIntent(
                        actor,
                        player,
                        0,
                        0,
                        0,
                        0,
                        skills,
                        false,
                        new FixedRandomSource(
                            0),
                        out BattleIntent intent);

                Assert.That(
                    created,
                    Is.True);

                Assert.That(
                    intent.CommandId,
                    Is.EqualTo("Skill"));

                Assert.That(
                    intent.Skill,
                    Is.SameAs(skill));

                Assert.That(
                    intent.TargetInstanceId,
                    Is.EqualTo("PLAYER"));
            }
            finally
            {
                Object.DestroyImmediate(
                    skill);
            }
        }

        [Test]
        public void SilenceBlocksSkillCandidates()
        {
            SkillDefinition skill =
                CreateSkill(
                    "SKILL_TEST_SILENCE",
                    0,
                    SkillTargetType.Enemy);

            try
            {
                BattleParticipant actor =
                    CreateEnemy(
                        maxMana: 10);

                BattleParticipant player =
                    CreatePlayer();

                MonsterAiSkillEntry[] skills =
                {
                    new MonsterAiSkillEntry(
                        skill,
                        100)
                };

                bool created =
                    MonsterAiDecisionService.TryCreateIntent(
                        actor,
                        player,
                        0,
                        10,
                        0,
                        0,
                        skills,
                        true,
                        new FixedRandomSource(
                            0),
                        out BattleIntent intent);

                Assert.That(
                    created,
                    Is.True);

                Assert.That(
                    intent.CommandId,
                    Is.EqualTo("Defend"));
            }
            finally
            {
                Object.DestroyImmediate(
                    skill);
            }
        }

        [Test]
        public void EnemyTargetSkillIsUnavailableWithoutLivingTarget()
        {
            SkillDefinition skill =
                CreateSkill(
                    "SKILL_TEST_NO_TARGET",
                    0,
                    SkillTargetType.Enemy);

            try
            {
                BattleParticipant actor =
                    CreateEnemy(
                        maxMana: 10);

                MonsterAiSkillEntry[] skills =
                {
                    new MonsterAiSkillEntry(
                        skill,
                        100)
                };

                bool created =
                    MonsterAiDecisionService.TryCreateIntent(
                        actor,
                        null,
                        0,
                        0,
                        0,
                        0,
                        skills,
                        false,
                        new FixedRandomSource(
                            0),
                        out _);

                Assert.That(
                    created,
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(
                    skill);
            }
        }

        private static BattleParticipant CreateEnemy(
            int maxHp = 100,
            int maxMana = 0)
        {
            return new BattleParticipant(
                "ENEMY",
                "MON_TEST",
                BattleTeam.Enemy,
                maxHp,
                5,
                10,
                5,
                80,
                5,
                maxMana: maxMana);
        }

        private static BattleParticipant CreatePlayer()
        {
            return new BattleParticipant(
                "PLAYER",
                "PLAYER",
                BattleTeam.Player,
                100,
                5,
                10,
                5,
                90,
                5);
        }

        private static SkillDefinition CreateSkill(
            string id,
            int manaCost,
            SkillTargetType targetType)
        {
            SkillDefinition skill =
                ScriptableObject.CreateInstance<SkillDefinition>();

            SetField(
                typeof(DefinitionBase),
                skill,
                "id",
                id);

            SetField(
                typeof(SkillDefinition),
                skill,
                "displayName",
                id);

            SetField(
                typeof(SkillDefinition),
                skill,
                "manaCost",
                manaCost);

            SetField(
                typeof(SkillDefinition),
                skill,
                "targetType",
                targetType);

            return skill;
        }

        private static void SetField(
            System.Type declaringType,
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field =
                declaringType.GetField(
                    fieldName,
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"필드를 찾지 못했습니다: {fieldName}");

            field.SetValue(
                target,
                value);
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly int requestedValue;

            public FixedRandomSource(
                int requestedValue)
            {
                this.requestedValue =
                    requestedValue;
            }

            public int NextInt(
                int minInclusive,
                int maxExclusive)
            {
                if (maxExclusive <= minInclusive)
                {
                    return minInclusive;
                }

                return Mathf.Clamp(
                    requestedValue,
                    minInclusive,
                    maxExclusive - 1);
            }
        }
    }
}
