using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Battle Participant 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleParticipantTests
    {
        [Test]
        public void Constructor_StoresSevenCombatStats()
        {
            BattleParticipant participant =
                new BattleParticipant(
                    "PLAYER",
                    "PLAYER",
                    BattleTeam.Player,
                    20,
                    7,
                    8,
                    9,
                    10,
                    11,
                    12,
                    13);

            Assert.AreEqual(
                7,
                participant.Speed);

            Assert.AreEqual(
                8,
                participant.Attack);

            Assert.AreEqual(
                9,
                participant.Defense);

            Assert.AreEqual(
                10,
                participant.Accuracy);

            Assert.AreEqual(
                11,
                participant.Evasion);

            Assert.AreEqual(
                12,
                participant.Charm);

            Assert.AreEqual(
                13,
                participant.Resistance);
        }

        [Test]
        public void ApplyDamage_ReducesCurrentHpByAmount()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20); // MaxHp 20

            int appliedDamage =
                participant.ApplyDamage(
                    7);

            Assert.AreEqual(
                7,
                appliedDamage); // 실제 적용된 피해량 확인

            Assert.AreEqual(
                13,
                participant.CurrentHp); // HP 감소 확인

            Assert.IsTrue(
                participant.IsAlive); // 생존 유지 확인
        }

        [Test]
        public void ApplyDamage_ExceedingCurrentHp_ClampsAtZeroAndReturnsActualDamage()
        {
            BattleParticipant participant =
                CreateParticipant(
                    10); // MaxHp 10

            int appliedDamage =
                participant.ApplyDamage(
                    999); // 남은 HP보다 훨씬 큰 피해

            Assert.AreEqual(
                10,
                appliedDamage); // 실제로는 남은 HP만큼만 적용 확인

            Assert.AreEqual(
                0,
                participant.CurrentHp); // 0 이하로 내려가지 않음 확인

            Assert.IsFalse(
                participant.IsAlive); // 사망 판정 확인
        }

        [Test]
        public void ApplyDamage_ZeroOrNegativeAmount_DoesNothing()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20);

            Assert.AreEqual(
                0,
                participant.ApplyDamage(
                    0)); // 0 피해 무시 확인

            Assert.AreEqual(
                0,
                participant.ApplyDamage(
                    -5)); // 음수 피해 무시 확인

            Assert.AreEqual(
                20,
                participant.CurrentHp); // HP 변화 없음 확인
        }

        [Test]
        public void ApplyDamage_MultipleHits_AccumulatesDamage()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20);

            participant.ApplyDamage(
                5);

            participant.ApplyDamage(
                6);

            Assert.AreEqual(
                9,
                participant.CurrentHp); // 20 - 5 - 6 = 9 확인
        }

        [Test]
        public void SetDefending_TogglesIsDefending()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20);

            Assert.IsFalse(
                participant.IsDefending); // 기본값 false 확인

            participant.SetDefending(
                true);

            Assert.IsTrue(
                participant.IsDefending);

            participant.SetDefending(
                false);

            Assert.IsFalse(
                participant.IsDefending);
        }

        private static BattleParticipant CreateParticipant(
            int maxHp)
        {
            return new BattleParticipant(
                "PLAYER",
                "PLAYER",
                BattleTeam.Player,
                maxHp,
                5,
                6,
                3,
                90,
                10,
                0,
                0);
        }
    }
}
