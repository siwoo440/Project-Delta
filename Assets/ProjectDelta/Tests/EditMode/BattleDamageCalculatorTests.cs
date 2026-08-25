using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Battle Damage Calculator 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleDamageCalculatorTests
    {
        // 55일차: 편차 0%(varianceRoll 5 → 100%)에서 baseDamage만 그대로 확인하고 싶을 때 쓴다.
        private const int NoVarianceRoll = 5;

        [Test]
        public void CalculateHitChancePercent_AddsAccuracyAndSubtractsEvasion()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    accuracy: 20,
                    evasion: 0,
                    attack: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    accuracy: 0,
                    evasion: 15,
                    attack: 0,
                    defense: 0);

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
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    accuracy: 0,
                    evasion: 500, // 극단적으로 높은 회피
                    attack: 0,
                    defense: 0);

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
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    accuracy: 0,
                    evasion: 0,
                    attack: 0,
                    defense: 0);

            int hitChance =
                BattleDamageCalculator.CalculateHitChancePercent(
                    attacker,
                    defender);

            Assert.AreEqual(
                BattleDamageCalculator.MaxHitChancePercent,
                hitChance); // 명중률 상한 확인
        }

        [Test]
        public void CalculateDamage_RatioFormula_AttackTimes100OverHundredPlusDefense()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 10,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 100);

            int damage =
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender,
                    NoVarianceRoll);

            // 기본 피해 = 10 × 100 ÷ (100 + 100) = 5, 편차 100%
            Assert.AreEqual(
                5,
                damage);
        }

        [Test]
        public void CalculateDamage_ZeroDefense_EqualsAttackAtNoVariance()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 10,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            int damage =
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender,
                    NoVarianceRoll);

            // 방어력 0 → 기본 피해 = 공격력 × 100 ÷ 100 = 공격력 그대로
            Assert.AreEqual(
                10,
                damage);
        }

        [Test]
        public void CalculateDamage_VarianceRollAtMinimum_AppliesNinetyFivePercent()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 100,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            int damage =
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender,
                    0); // varianceRoll 0 → 95%

            Assert.AreEqual(
                95,
                damage);
        }

        [Test]
        public void CalculateDamage_VarianceRollAtMaximum_AppliesHundredFivePercent()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 100,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            int damage =
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender,
                    10); // varianceRoll 10 → 105%

            Assert.AreEqual(
                105,
                damage);
        }

        [Test]
        public void CalculateDamage_VarianceRollOutOfRange_ClampsToNearestBound()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 100,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            Assert.AreEqual(
                95,
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender,
                    -5)); // 범위 밖 음수 → 최소 편차로 고정

            Assert.AreEqual(
                105,
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender,
                    999)); // 범위 밖 큰 값 → 최대 편차로 고정
        }

        [Test]
        public void CalculateDamage_NeverGoesBelowMinimum()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 1,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 999); // 극단적으로 높은 방어력

            int damage =
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender,
                    NoVarianceRoll);

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
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            defender.SetDefending(
                true);

            int damage =
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender,
                    NoVarianceRoll);

            // 방어력 0, 편차 100% → 기본 피해 10, 방어로 50% 감소 → 5
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
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            defender.SetDefending(
                true);

            int damage =
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender,
                    NoVarianceRoll);

            Assert.AreEqual(
                BattleDamageCalculator.MinDamage,
                damage); // 방어로 더 줄어도 최소 피해는 유지 확인
        }

        [Test]
        public void CalculateDamage_CharmAndResistance_DoNotAffectCurrentDamageFormula()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 10,
                    defense: 0,
                    accuracy: 0,
                    evasion: 0,
                    charm: 99,
                    resistance: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    defense: 4,
                    accuracy: 0,
                    evasion: 0,
                    charm: 0,
                    resistance: 99);

            int damage =
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender,
                    NoVarianceRoll);

            // 기본 피해 = 10 × 100 ÷ 104 = 9 (정수 나눗셈), 매력·저항은 현재 공식에 영향 없음
            Assert.AreEqual(
                9,
                damage);
        }

        [Test]
        public void Resolve_RollBelowHitChance_ReturnsHitWithDamage()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 10,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 4);

            // 기본 명중률 70% → roll 69는 70보다 작으므로 명중
            BattleDamageResult result =
                BattleDamageCalculator.Resolve(
                    attacker,
                    defender,
                    69,
                    NoVarianceRoll);

            Assert.IsTrue(
                result.IsHit);

            // 기본 피해 = 10 × 100 ÷ 104 = 9 (정수 나눗셈), 편차 100%
            Assert.AreEqual(
                9,
                result.Damage);

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
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 4);

            // 기본 명중률 70% → roll 70은 70보다 작지 않으므로 빗나감
            BattleDamageResult result =
                BattleDamageCalculator.Resolve(
                    attacker,
                    defender,
                    70,
                    NoVarianceRoll);

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
            int charm = 0,
            int resistance = 0)
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
                charm,
                resistance);
        }
    }
}
