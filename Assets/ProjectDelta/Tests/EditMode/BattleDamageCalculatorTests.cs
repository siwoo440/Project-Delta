using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Battle Damage Calculator 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleDamageCalculatorTests
    {
        [Test]
        public void CalculateHitChancePercent_AddsAccuracyAndSubtractsEvasion()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    accuracy: 20,
                    evasion: 0,
                    attack: 0,
                    defense: 0,
                    penetration: 0);

            BattleParticipant defender =
                CreateParticipant(
                    accuracy: 0,
                    evasion: 15,
                    attack: 0,
                    defense: 0,
                    penetration: 0);

            int hitChance =
                BattleDamageCalculator.CalculateHitChancePercent(
                    attacker,
                    defender);

            // 기본 70 + 명중 20 - 회피 15 = 75
            Assert.AreEqual(
                75,
                hitChance);
        }

        [Test]
        public void CalculateHitChancePercent_NeverGoesBelowMinimum()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    accuracy: 0,
                    evasion: 0,
                    attack: 0,
                    defense: 0,
                    penetration: 0);

            BattleParticipant defender =
                CreateParticipant(
                    accuracy: 0,
                    evasion: 500, // 극단적으로 높은 회피
                    attack: 0,
                    defense: 0,
                    penetration: 0);

            int hitChance =
                BattleDamageCalculator.CalculateHitChancePercent(
                    attacker,
                    defender);

            Assert.AreEqual(
                BattleDamageCalculator.MinHitChancePercent,
                hitChance); // 최소 명중률 보장 확인
        }

        [Test]
        public void CalculateHitChancePercent_NeverExceedsMaximum()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    accuracy: 500, // 극단적으로 높은 명중
                    evasion: 0,
                    attack: 0,
                    defense: 0,
                    penetration: 0);

            BattleParticipant defender =
                CreateParticipant(
                    accuracy: 0,
                    evasion: 0,
                    attack: 0,
                    defense: 0,
                    penetration: 0);

            int hitChance =
                BattleDamageCalculator.CalculateHitChancePercent(
                    attacker,
                    defender);

            Assert.AreEqual(
                BattleDamageCalculator.MaxHitChancePercent,
                hitChance); // 명중률 상한 확인
        }

        [Test]
        public void CalculateDamage_AttackPlusPenetrationMinusDefense()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 10,
                    penetration: 3,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    penetration: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 5);

            int damage =
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender);

            // 10 + 3 - 5 = 8
            Assert.AreEqual(
                8,
                damage);
        }

        [Test]
        public void CalculateDamage_NeverGoesBelowMinimum()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 1,
                    penetration: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    penetration: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 999); // 극단적으로 높은 방어력

            int damage =
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender);

            Assert.AreEqual(
                BattleDamageCalculator.MinDamage,
                damage); // 최소 피해 보장 확인
        }

        [Test]
        public void CalculateDamage_DefendingTarget_ReducesDamageByReductionPercent()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 10,
                    penetration: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    penetration: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            defender.SetDefending(
                true);

            int damage =
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender);

            // 10 - 0 = 10, 방어로 50% 감소 → 5
            Assert.AreEqual(
                5,
                damage);
        }

        [Test]
        public void CalculateDamage_DefendingTarget_StillNeverGoesBelowMinimum()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 1,
                    penetration: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    penetration: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            defender.SetDefending(
                true);

            int damage =
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender);

            Assert.AreEqual(
                BattleDamageCalculator.MinDamage,
                damage); // 방어로 더 줄어도 최소 피해는 유지 확인
        }

        [Test]
        public void Resolve_RollBelowHitChance_ReturnsHitWithDamage()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 10,
                    penetration: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    penetration: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 4);

            // 기본 명중률 70% → roll 69는 70보다 작으므로 명중
            BattleDamageResult result =
                BattleDamageCalculator.Resolve(
                    attacker,
                    defender,
                    69);

            Assert.IsTrue(
                result.IsHit);

            Assert.AreEqual(
                6,
                result.Damage); // 10 - 4 = 6

            Assert.AreEqual(
                70,
                result.HitChancePercent);
        }

        [Test]
        public void Resolve_RollAtOrAboveHitChance_ReturnsMissWithZeroDamage()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 10,
                    penetration: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    penetration: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 4);

            // 기본 명중률 70% → roll 70은 70보다 작지 않으므로 빗나감
            BattleDamageResult result =
                BattleDamageCalculator.Resolve(
                    attacker,
                    defender,
                    70);

            Assert.IsFalse(
                result.IsHit);

            Assert.AreEqual(
                0,
                result.Damage);
        }

        private static BattleParticipant CreateParticipant(
            int attack,
            int defense,
            int accuracy,
            int evasion,
            int penetration)
        {
            return new BattleParticipant(
                "TEST",
                "TEST",
                BattleTeam.Player,
                20,
                5,
                attack,
                defense,
                accuracy,
                evasion,
                penetration);
        }
    }
}
