using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // BattleStatModifierService 사용
using ProjectDelta.Data; // StatusEffectKind·BattleStatType 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleStatModifierServiceTests
    {
        [Test]
        public void GetEffectiveAttack_NoStatusEffects_ReturnsBaseValue()
        {
            BattleParticipant participant =
                CreateParticipant(
                    attack: 10);

            Assert.AreEqual(
                10,
                BattleStatModifierService.GetEffectiveAttack(
                    participant));
        }

        [Test]
        public void GetEffectiveAttack_ActiveBuff_AddsModifierValue()
        {
            BattleParticipant participant =
                CreateParticipant(
                    attack: 10);

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "SE011",
                    "MON_TEST",
                    2,
                    1,
                    5,
                    StatusEffectKind.StatModifier,
                    BattleStatType.Attack)); // 공격 상승 +5

            Assert.AreEqual(
                15,
                BattleStatModifierService.GetEffectiveAttack(
                    participant));
        }

        [Test]
        public void GetEffectiveAttack_ActiveDebuff_SubtractsModifierValue()
        {
            BattleParticipant participant =
                CreateParticipant(
                    attack: 10);

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "SE003",
                    "MON_TEST",
                    2,
                    1,
                    -5,
                    StatusEffectKind.StatModifier,
                    BattleStatType.Attack)); // 약화 -5

            Assert.AreEqual(
                5,
                BattleStatModifierService.GetEffectiveAttack(
                    participant));
        }

        [Test]
        public void GetEffectiveAttack_ExpiredBuff_IsIgnored()
        {
            BattleParticipant participant =
                CreateParticipant(
                    attack: 10);

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "SE011",
                    "MON_TEST",
                    0, // 이미 만료
                    1,
                    5,
                    StatusEffectKind.StatModifier,
                    BattleStatType.Attack));

            Assert.AreEqual(
                10,
                BattleStatModifierService.GetEffectiveAttack(
                    participant));
        }

        [Test]
        public void GetEffectiveAttack_OtherTargetStat_IsIgnored()
        {
            // 방어 상승은 공격력에 영향을 주면 안 된다.
            BattleParticipant participant =
                CreateParticipant(
                    attack: 10);

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "SE012",
                    "MON_TEST",
                    2,
                    1,
                    5,
                    StatusEffectKind.StatModifier,
                    BattleStatType.Defense));

            Assert.AreEqual(
                10,
                BattleStatModifierService.GetEffectiveAttack(
                    participant));
        }

        [Test]
        public void GetEffectiveAttack_DamageOverTimeKindWithSameAppliedValue_IsIgnored()
        {
            // EffectKind가 StatModifier가 아니면 AppliedValue·TargetStat이 우연히 같아도 무시해야 한다.
            BattleParticipant participant =
                CreateParticipant(
                    attack: 10);

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "SE001",
                    "MON_TEST",
                    2,
                    1,
                    5,
                    StatusEffectKind.DamageOverTime,
                    BattleStatType.Attack));

            Assert.AreEqual(
                10,
                BattleStatModifierService.GetEffectiveAttack(
                    participant));
        }

        [Test]
        public void GetEffectiveSpeed_ActiveDebuff_NeverGoesBelowZero()
        {
            BattleParticipant participant =
                CreateParticipant(
                    speed: 2);

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "SE004",
                    "MON_TEST",
                    2,
                    1,
                    -10, // 기본 속도보다 큰 감소
                    StatusEffectKind.StatModifier,
                    BattleStatType.Speed));

            Assert.AreEqual(
                0,
                BattleStatModifierService.GetEffectiveSpeed(
                    participant));
        }

        [Test]
        public void GetEffectiveDefense_MultipleActiveModifiersOnSameStat_Accumulate()
        {
            BattleParticipant participant =
                CreateParticipant(
                    defense: 10);

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "SE012",
                    "MON_A",
                    2,
                    1,
                    5,
                    StatusEffectKind.StatModifier,
                    BattleStatType.Defense));

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "SE_OTHER_SOURCE",
                    "MON_B",
                    1,
                    1,
                    -3,
                    StatusEffectKind.StatModifier,
                    BattleStatType.Defense));

            Assert.AreEqual(
                12,
                BattleStatModifierService.GetEffectiveDefense(
                    participant)); // 10 + 5 - 3
        }

        private static BattleParticipant CreateParticipant(
            int attack = 0,
            int defense = 0,
            int speed = 5,
            int accuracy = 0,
            int evasion = 0,
            int resistance = 0)
        {
            return new BattleParticipant(
                "TEST",
                "TEST",
                BattleTeam.Player,
                20,
                speed,
                attack,
                defense,
                accuracy,
                evasion,
                0,
                resistance);
        }
    }
}
