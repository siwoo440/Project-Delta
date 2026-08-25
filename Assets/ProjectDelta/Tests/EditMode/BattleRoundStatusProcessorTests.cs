using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // BattleRoundStatusProcessor 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleRoundStatusProcessorTests
    {
        [Test]
        public void ApplyStartOfRoundEffects_NullContext_DoesNotThrow()
        {
            // 60일차: 아직 지속 시작 효과가 없어 빈 구현이지만, null Context로도 안전해야 한다.
            Assert.DoesNotThrow(
                () => BattleRoundStatusProcessor.ApplyStartOfRoundEffects(
                    null));
        }

        [Test]
        public void ApplyEndOfRoundDamageAndHealing_NegativeAppliedValue_DealsDamage()
        {
            BattleParticipant player =
                CreatePlayer(
                    20);

            player.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_POISON",
                    "MON_TEST",
                    2,
                    1,
                    -5)); // 중독: 라운드 종료 시 5 피해

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateEnemy() });

            BattleRoundStatusProcessor.ApplyEndOfRoundDamageAndHealing(
                context);

            Assert.AreEqual(
                15,
                player.CurrentHp);
        }

        [Test]
        public void ApplyEndOfRoundDamageAndHealing_PositiveAppliedValue_Heals()
        {
            BattleParticipant player =
                CreatePlayer(
                    20);

            player.ApplyDamage(
                10); // 20 → 10

            player.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_REGEN",
                    "PLAYER",
                    2,
                    1,
                    4)); // 재생: 라운드 종료 시 4 회복

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateEnemy() });

            BattleRoundStatusProcessor.ApplyEndOfRoundDamageAndHealing(
                context);

            Assert.AreEqual(
                14,
                player.CurrentHp);
        }

        [Test]
        public void ApplyEndOfRoundDamageAndHealing_DeadParticipant_IsSkipped()
        {
            BattleParticipant player =
                CreatePlayer(
                    20);

            player.ApplyDamage(
                20); // 사망 처리

            player.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_REGEN",
                    "PLAYER",
                    2,
                    1,
                    4));

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateEnemy() });

            BattleRoundStatusProcessor.ApplyEndOfRoundDamageAndHealing(
                context);

            Assert.AreEqual(
                0,
                player.CurrentHp); // 죽은 참가자는 회복되지 않는다
        }

        [Test]
        public void DecrementDurationsAndRemoveExpired_RemovesOnlyExpiredEntries()
        {
            BattleParticipant player =
                CreatePlayer(
                    20);

            StatusEffectInstance oneRoundLeft =
                new StatusEffectInstance(
                    "STATUS_SHORT",
                    "MON_TEST",
                    1,
                    1,
                    -5);

            StatusEffectInstance twoRoundsLeft =
                new StatusEffectInstance(
                    "STATUS_LONG",
                    "MON_TEST",
                    2,
                    1,
                    -5);

            player.AddStatusEffect(
                oneRoundLeft);

            player.AddStatusEffect(
                twoRoundsLeft);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateEnemy() });

            BattleRoundStatusProcessor.DecrementDurationsAndRemoveExpired(
                context);

            Assert.AreEqual(
                1,
                player.StatusEffects.Count); // 1라운드 남았던 상태만 만료돼 제거됨

            Assert.AreSame(
                twoRoundsLeft,
                player.StatusEffects[0]);

            Assert.AreEqual(
                1,
                twoRoundsLeft.RemainingRounds); // 남은 상태도 1 감소는 됨
        }

        private static BattleParticipant CreatePlayer(
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
                0);
        }

        private static BattleParticipant CreateEnemy()
        {
            return new BattleParticipant(
                "MON_TEST",
                "MON_TEST",
                BattleTeam.Enemy,
                10,
                5,
                4,
                2,
                80,
                5,
                0);
        }
    }
}
