using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Battle Damage Calculator 사용
using ProjectDelta.Data; // StatusEffectKind·BattleStatType 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleDamageCalculatorTests
    {
        // 55일차: 편차 0%(varianceRoll 5 → 100%)에서 baseDamage만 그대로 확인하고 싶을 때 쓴다.
        private const int NoVarianceRoll = 5;

        [Test]
        public void CalculateHitChancePercent_AddsAccuracyAndSubtractsHalfEvasion()
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
                    evasion: 20,
                    attack: 0,
                    defense: 0);

            int hitChance =
                BattleDamageCalculator.CalculateHitChancePercent(
                    attacker,
                    defender);

            // 56일차: 회피는 50%만 반영 → 기본 70 + 명중 20 - (회피 20 × 50%) = 80
            Assert.AreEqual(
                80,
                hitChance);
        }

        [Test]
        public void CalculateHitChancePercent_WeightsEvasionByFiftyPercentWithFloor()
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
                    evasion: 15, // 홀수 회피 → 가중치 적용 후 정수 나눗셈으로 버림
                    attack: 0,
                    defense: 0);

            int hitChance =
                BattleDamageCalculator.CalculateHitChancePercent(
                    attacker,
                    defender);

            // 회피 15 × 50% = 7.5 → 정수 나눗셈으로 7까지 버림, 기본 70 - 7 = 63
            Assert.AreEqual(
                63,
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

            // 방어력 0 → 감소율 30 + 0 = 30%. 편차 100% → 기본 피해 10, 방어로 30% 감소 → 7
            Assert.AreEqual(
                7,
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

        // 57일차: 기획서 4.2 예상 감소율 표(방어력 25→약36%, 50→약40%, 100→약45%, 200→약50%)를 그대로 확인한다.
        [TestCase(25, 36)]
        [TestCase(50, 40)]
        [TestCase(100, 45)]
        [TestCase(200, 50)]
        public void CalculateDefendReductionPercent_MatchesPlanningDocTable(
            int defense,
            int expectedReductionPercent)
        {
            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    defense: defense,
                    accuracy: 0,
                    evasion: 0);

            int reductionPercent =
                BattleDamageCalculator.CalculateDefendReductionPercent(
                    defender);

            Assert.AreEqual(
                expectedReductionPercent,
                reductionPercent);
        }

        [Test]
        public void CalculateDefendReductionPercent_NeverExceedsSixtyPercent()
        {
            // defense ÷ (defense + 100)은 아무리 방어력이 커져도 1에 정수 나눗셈으로는 도달하지
            // 못해 60%에 점근한다. 그래도 Min 상한이 실제로 60%를 넘기지 않는다는 것만 확인한다.
            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    defense: 100000, // 극단적으로 높은 방어력
                    accuracy: 0,
                    evasion: 0);

            int reductionPercent =
                BattleDamageCalculator.CalculateDefendReductionPercent(
                    defender);

            Assert.LessOrEqual(
                reductionPercent,
                BattleDamageCalculator.DefendMaxReductionPercent);
        }

        [Test]
        public void CalculateDamage_PenetratesDefense_AppliesOnlyPartialReduction()
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
                    defense: 0); // 감소율 30%

            defender.SetDefending(
                true);

            int damage =
                BattleDamageCalculator.CalculateDamage(
                    attacker,
                    defender,
                    NoVarianceRoll,
                    DefenseInteraction.PenetratesDefense);

            // 방어 관통 가중치 50% → 감소율 30% × 50% = 15%. 기본 피해 10 × (100-15)% = 8 (버림)
            Assert.AreEqual(
                8,
                damage);
        }

        [Test]
        public void CalculateDamage_IgnoresDefense_AppliesNoReductionEvenWhileDefending()
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
                    NoVarianceRoll,
                    DefenseInteraction.IgnoresDefense);

            // 방어 불가 피해는 방어 중이어도 그대로 들어간다.
            Assert.AreEqual(
                10,
                damage);
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

        // 58일차: 기획서 4.2 피해 유형 표 — 상태 이상·지속 피해는 방어력이 아니라 저항을 쓴다.
        [TestCase(DamageType.StatusEffect)]
        [TestCase(DamageType.DamageOverTime)]
        public void CalculateBaseDamage_StatusEffectOrDamageOverTime_UsesResistanceInsteadOfDefense(
            DamageType damageType)
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
                    defense: 999, // 방어력은 무시돼야 하므로 극단적인 값을 넣는다
                    resistance: 100);

            int baseDamage =
                BattleDamageCalculator.CalculateBaseDamage(
                    attacker,
                    defender,
                    damageType);

            // 방어력(999)이 아니라 저항(100)을 써야 한다: 10 × 100 ÷ (100 + 100) = 5
            Assert.AreEqual(
                5,
                baseDamage);
        }

        [Test]
        public void CalculateBaseDamage_Fixed_IgnoresDefense()
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
                    defense: 999); // 고정 피해는 이 값을 완전히 무시해야 한다

            int baseDamage =
                BattleDamageCalculator.CalculateBaseDamage(
                    attacker,
                    defender,
                    DamageType.Fixed);

            // 방어력 무시 → 공격력 그대로: 10 × 100 ÷ 100 = 10
            Assert.AreEqual(
                10,
                baseDamage);
        }

        [Test]
        public void CanCriticalHit_MultiplierNotSpecified_ReturnsFalseRegardlessOfChance()
        {
            // 58일차: 기획서 4.2 "치명타 배율이 지정되지 않은 피해는 치명타가 발생하지 않는다".
            Assert.IsFalse(
                BattleDamageCalculator.CanCriticalHit(
                    BattleDamageCalculator.NoCriticalMultiplierPercent));
        }

        [Test]
        public void IsCriticalHit_RollBelowChanceWithMultiplier_ReturnsTrue()
        {
            Assert.IsTrue(
                BattleDamageCalculator.IsCriticalHit(
                    criticalChancePercent: 50,
                    criticalMultiplierPercent: 150,
                    criticalRoll: 49));
        }

        [Test]
        public void IsCriticalHit_RollAtOrAboveChance_ReturnsFalse()
        {
            Assert.IsFalse(
                BattleDamageCalculator.IsCriticalHit(
                    criticalChancePercent: 50,
                    criticalMultiplierPercent: 150,
                    criticalRoll: 50));
        }

        [Test]
        public void IsCriticalHit_NoMultiplierSpecified_ReturnsFalseEvenIfRollWouldHit()
        {
            // 확률상으로는 반드시 맞을 굴림(0)이라도 배율이 없으면 치명타가 아니다.
            Assert.IsFalse(
                BattleDamageCalculator.IsCriticalHit(
                    criticalChancePercent: 100,
                    criticalMultiplierPercent: BattleDamageCalculator.NoCriticalMultiplierPercent,
                    criticalRoll: 0));
        }

        [Test]
        public void CalculateDamage_CriticalHit_AppliesMultiplierOnTopOfVariance()
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
                    NoVarianceRoll,
                    DefenseInteraction.Defendable,
                    DamageType.Normal,
                    criticalChancePercent: 100,
                    criticalMultiplierPercent: 150,
                    criticalRoll: 0);

            // 기본 피해 10, 편차 100%, 치명타 배율 150% → 15
            Assert.AreEqual(
                15,
                damage);
        }

        [Test]
        public void CalculateDamage_DefaultParameters_NeverCritical()
        {
            // 매개변수를 생략하는 기존 호출부(기본 공격)는 치명타 확률이 0%라 절대 치명타가 아니다.
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

            // 치명타가 적용됐다면 15가 나왔을 상황에서도 10 그대로여야 한다.
            Assert.AreEqual(
                10,
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

            // 58일차: 치명타 매개변수를 생략했으므로 치명타가 아니어야 한다.
            Assert.IsFalse(
                result.IsCritical);
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

        [Test]
        public void CalculateBaseDamage_AttackerHasAttackUpBuff_UsesBoostedAttack()
        {
            // 65일차: 공격 상승(StatModifier)이 실제 피해 계산에 반영되는지 확인한다.
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 10,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            attacker.AddStatusEffect(
                new StatusEffectInstance(
                    "SE011",
                    "PLAYER",
                    2,
                    1,
                    5,
                    StatusEffectKind.StatModifier,
                    BattleStatType.Attack)); // 공격 상승 +5 → 유효 공격력 15

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            int baseDamage =
                BattleDamageCalculator.CalculateBaseDamage(
                    attacker,
                    defender);

            Assert.AreEqual(
                15,
                baseDamage); // 방어력 0 → 유효 공격력 그대로
        }

        [Test]
        public void CalculateBaseDamage_DefenderHasDefenseUpBuff_ReducesDamage()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    attack: 20,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            BattleParticipant defender =
                CreateParticipant(
                    attack: 0,
                    accuracy: 0,
                    evasion: 0,
                    defense: 0);

            defender.AddStatusEffect(
                new StatusEffectInstance(
                    "SE012",
                    "PLAYER",
                    2,
                    1,
                    100,
                    StatusEffectKind.StatModifier,
                    BattleStatType.Defense)); // 방어 상승 +100 → 유효 방어력 100

            int baseDamage =
                BattleDamageCalculator.CalculateBaseDamage(
                    attacker,
                    defender);

            // 20 × 100 ÷ (100 + 100) = 10
            Assert.AreEqual(
                10,
                baseDamage);
        }

        [Test]
        public void CalculateHitChancePercent_DefenderHasEvasionUpBuff_LowersHitChance()
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
                    evasion: 0,
                    attack: 0,
                    defense: 0);

            defender.AddStatusEffect(
                new StatusEffectInstance(
                    "SE015",
                    "PLAYER",
                    2,
                    1,
                    20,
                    StatusEffectKind.StatModifier,
                    BattleStatType.Evasion)); // 회피 상승 +20 → 유효 회피 20

            int hitChance =
                BattleDamageCalculator.CalculateHitChancePercent(
                    attacker,
                    defender);

            // 기본 70 - (유효 회피 20 × 50%) = 60
            Assert.AreEqual(
                60,
                hitChance);
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
