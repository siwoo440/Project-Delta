using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Presentation;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class BattleHudDisplayFormatterTests
    {
        [Test]
        public void FormatStatusEffects_Empty_ReturnsEmptyText()
        {
            BattleParticipant participant =
                CreateParticipant();

            string text =
                BattleHudDisplayFormatter.FormatStatusEffects(
                    participant.StatusEffects);

            Assert.That(
                text,
                Is.EqualTo(string.Empty));
        }

        [Test]
        public void FormatStatusEffects_DamageOverTime_ShowsStackAndRounds()
        {
            BattleParticipant participant =
                CreateParticipant();

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_POISON",
                    "ENEMY",
                    3,
                    2,
                    5,
                    StatusEffectKind.DamageOverTime));

            string text =
                BattleHudDisplayFormatter.FormatStatusEffects(
                    participant.StatusEffects);

            Assert.That(
                text,
                Does.Contain("중독"));

            Assert.That(
                text,
                Does.Contain("×2"));

            Assert.That(
                text,
                Does.Contain("3R"));
        }

        [Test]
        public void FormatStatusEffects_StatModifier_ShowsStatAndSignedValue()
        {
            BattleParticipant participant =
                CreateParticipant();

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_ATTACK_UP",
                    "PLAYER",
                    2,
                    1,
                    10,
                    StatusEffectKind.StatModifier,
                    BattleStatType.Attack));

            string text =
                BattleHudDisplayFormatter.FormatStatusEffects(
                    participant.StatusEffects);

            Assert.That(
                text,
                Does.Contain("공격 +10"));

            Assert.That(
                text,
                Does.Contain("2R"));
        }

        [Test]
        public void FormatStatusEffects_ExpiredStatus_IsHidden()
        {
            BattleParticipant participant =
                CreateParticipant();

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_EXPIRED",
                    "PLAYER",
                    0,
                    1,
                    5,
                    StatusEffectKind.DamageOverTime));

            string text =
                BattleHudDisplayFormatter.FormatStatusEffects(
                    participant.StatusEffects);

            Assert.That(
                text,
                Is.EqualTo(string.Empty));
        }

        [Test]
        public void FormatDamageChange_Miss_ReturnsMiss()
        {
            BattleParticipant attacker =
                CreateParticipant();

            BattleParticipant target =
                CreateEnemy();

            BattleDamageChange change =
                new BattleDamageChange(
                    attacker,
                    target,
                    BattleDamageResult.Miss(75),
                    0);

            Assert.That(
                BattleHudDisplayFormatter.FormatDamageChange(
                    change),
                Is.EqualTo("MISS"));
        }

        [Test]
        public void FormatDamageChange_Critical_ShowsCriticalAndAppliedDamage()
        {
            BattleParticipant attacker =
                CreateParticipant();

            BattleParticipant target =
                CreateEnemy();

            BattleDamageChange change =
                new BattleDamageChange(
                    attacker,
                    target,
                    BattleDamageResult.Hit(
                        24,
                        90,
                        20,
                        100,
                        true),
                    17);

            Assert.That(
                BattleHudDisplayFormatter.FormatDamageChange(
                    change),
                Is.EqualTo("치명타! -17"));
        }

        [Test]
        public void FormatDamageChange_NormalHit_ShowsAppliedDamage()
        {
            BattleParticipant attacker =
                CreateParticipant();

            BattleParticipant target =
                CreateEnemy();

            BattleDamageChange change =
                new BattleDamageChange(
                    attacker,
                    target,
                    BattleDamageResult.Hit(
                        9,
                        90,
                        9,
                        100,
                        false),
                    9);

            Assert.That(
                BattleHudDisplayFormatter.FormatDamageChange(
                    change),
                Is.EqualTo("-9"));
        }

        private static BattleParticipant CreateParticipant()
        {
            return new BattleParticipant(
                "PLAYER",
                "PLAYER",
                BattleTeam.Player,
                100,
                50,
                40,
                30,
                90,
                20,
                10,
                10,
                50,
                100);
        }

        private static BattleParticipant CreateEnemy()
        {
            return new BattleParticipant(
                "ENEMY",
                "MON_TEST",
                BattleTeam.Enemy,
                100,
                40,
                30,
                20,
                80,
                10);
        }
    }
}
