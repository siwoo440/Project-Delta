using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class Day74SilenceIntentRegressionTests
    {
        [Test]
        public void ExecutionPolicyCancelsAlreadyPreparedSkillIntentWhenActorBecomesSilenced()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player);

            BattleParticipant enemy =
                CreateParticipant(
                    "ENEMY",
                    BattleTeam.Enemy);

            enemy.AddStatusEffect(
                CreateSilence());

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            BattleIntent intent =
                new BattleIntent(
                    enemy.InstanceId,
                    player.InstanceId,
                    "Skill",
                    "강공격",
                    BattleIntentIconType.Attack,
                    "SKILL_MON_HEAVY_ATTACK",
                    true);

            BattleIntentCancelReason reason =
                BattleIntentExecutionPolicy.EvaluateCurrentCancelReason(
                    context,
                    enemy,
                    intent);

            Assert.That(
                reason,
                Is.EqualTo(BattleIntentCancelReason.Silenced));
        }

        [Test]
        public void ExecutionPolicyDoesNotCancelNormalAttackBecauseOfSilence()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player);

            BattleParticipant enemy =
                CreateParticipant(
                    "ENEMY",
                    BattleTeam.Enemy);

            enemy.AddStatusEffect(
                CreateSilence());

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            BattleIntent intent =
                BattleIntent.CreateBasicAttack(
                    enemy,
                    player);

            BattleIntentCancelReason reason =
                BattleIntentExecutionPolicy.EvaluateCurrentCancelReason(
                    context,
                    enemy,
                    intent);

            Assert.That(
                reason,
                Is.EqualTo(BattleIntentCancelReason.None));
        }

        [Test]
        public void SkillCommandRejectsSilencedActorEvenWithoutIntentRuntimeUpdate()
        {
            SkillDefinition skill =
                CreateSkill(
                    "SKILL_TEST_SILENCE_GUARD");

            try
            {
                BattleParticipant player =
                    CreateParticipant(
                        "PLAYER",
                        BattleTeam.Player);

                BattleParticipant enemy =
                    CreateParticipant(
                        "ENEMY",
                        BattleTeam.Enemy);

                enemy.AddStatusEffect(
                    CreateSilence());

                BattleContext context =
                    new BattleContext(
                        player,
                        new[] { enemy });

                SkillBattleCommand command =
                    new SkillBattleCommand(
                        skill);

                BattleCommandResult result =
                    command.Execute(
                        context,
                        enemy,
                        player);

                Assert.That(
                    result.Accepted,
                    Is.False);

                StringAssert.Contains(
                    "침묵",
                    result.Message);
            }
            finally
            {
                Object.DestroyImmediate(
                    skill);
            }
        }

        private static BattleParticipant CreateParticipant(
            string instanceId,
            BattleTeam team)
        {
            return new BattleParticipant(
                instanceId,
                instanceId,
                team,
                100,
                10,
                10,
                5,
                90,
                5,
                maxMana: 10,
                maxStamina: 10);
        }

        private static StatusEffectInstance CreateSilence()
        {
            return new StatusEffectInstance(
                "STATUS_SILENCE",
                "PLAYER",
                2,
                1,
                0,
                StatusEffectKind.Neutral);
        }

        private static SkillDefinition CreateSkill(
            string id)
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
                "테스트 스킬");

            SetField(
                typeof(SkillDefinition),
                skill,
                "targetType",
                SkillTargetType.Enemy);

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
    }
}
