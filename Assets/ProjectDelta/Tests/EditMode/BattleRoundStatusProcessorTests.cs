using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // BattleRoundStatusProcessor 사용
using ProjectDelta.Data; // StatusEffectKind 사용

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
        public void ApplyEndOfRoundDamageAndHealing_DamageOverTime_DealsDamage()
        {
            // 64일차: 부호가 아니라 EffectKind로 피해를 판정한다 (중독·출혈).
            BattleParticipant player =
                CreatePlayer(
                    20);

            player.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_POISON",
                    "MON_TEST",
                    2,
                    1,
                    5,
                    StatusEffectKind.DamageOverTime)); // 중독: 라운드 종료 시 5 피해

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
        public void ApplyEndOfRoundDamageAndHealing_HealOverTime_Heals()
        {
            // 64일차: 재생 등 회복도 부호가 아니라 EffectKind로 판정한다.
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
                    4,
                    StatusEffectKind.HealOverTime)); // 재생: 라운드 종료 시 4 회복

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
        public void ApplyEndOfRoundDamageAndHealing_StackedDamageOverTime_MultipliesByStackCount()
        {
            // 64일차: 중첩 수가 지속 피해 계산에 반영되어야 한다 (중독·출혈 최대 3중첩).
            BattleParticipant player =
                CreatePlayer(
                    20);

            player.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_POISON",
                    "MON_TEST",
                    2,
                    3,
                    5,
                    StatusEffectKind.DamageOverTime)); // 3중첩 중독: 5 * 3 = 15 피해

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateEnemy() });

            BattleRoundStatusProcessor.ApplyEndOfRoundDamageAndHealing(
                context);

            Assert.AreEqual(
                5,
                player.CurrentHp);
        }

        [Test]
        public void ApplyEndOfRoundDamageAndHealing_NeutralKind_DoesNothing()
        {
            // 64일차: 약화·강화 상태(Neutral)는 라운드 종료 지속 피해·회복 대상이 아니다.
            BattleParticipant player =
                CreatePlayer(
                    20);

            player.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_WEAKNESS",
                    "MON_TEST",
                    2,
                    1,
                    10,
                    StatusEffectKind.Neutral)); // 능력치 보정용 수치, HP에 영향 없어야 함

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateEnemy() });

            BattleRoundStatusProcessor.ApplyEndOfRoundDamageAndHealing(
                context);

            Assert.AreEqual(
                20,
                player.CurrentHp);
        }

        [Test]
        public void ApplyEndOfRoundDamageAndHealing_StunKind_DoesNothing()
        {
            // 64일차: 기절은 라운드 파이프라인의 지속 피해·회복 대상이 아니라 행동 순서에서 처리한다.
            BattleParticipant player =
                CreatePlayer(
                    20);

            player.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_STUN",
                    "MON_TEST",
                    1,
                    1,
                    0,
                    StatusEffectKind.Stun));

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateEnemy() });

            BattleRoundStatusProcessor.ApplyEndOfRoundDamageAndHealing(
                context);

            Assert.AreEqual(
                20,
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
                    4,
                    StatusEffectKind.HealOverTime));

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
                    5,
                    StatusEffectKind.DamageOverTime);

            StatusEffectInstance twoRoundsLeft =
                new StatusEffectInstance(
                    "STATUS_LONG",
                    "MON_TEST",
                    2,
                    1,
                    5,
                    StatusEffectKind.DamageOverTime);

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

        [Test]
        public void FullEndOfRoundPipeline_OneRoundStatus_TicksThenExpiresAfterLastEffect()
        {
            // 64일차: "지속 효과 → 지속시간 감소 → 만료 제거" 순서 유지 확인.
            // 1라운드짜리 상태는 마지막 효과를 적용한 뒤에 만료되어야 한다 (효과를 건너뛰고 만료되면 안 됨).
            BattleParticipant player =
                CreatePlayer(
                    20);

            player.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_POISON",
                    "MON_TEST",
                    1,
                    1,
                    5,
                    StatusEffectKind.DamageOverTime));

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateEnemy() });

            BattleRoundStatusProcessor.ApplyEndOfRoundDamageAndHealing(
                context);

            BattleRoundStatusProcessor.DecrementDurationsAndRemoveExpired(
                context);

            Assert.AreEqual(
                15,
                player.CurrentHp); // 만료되기 전에 마지막 효과는 적용됨

            Assert.AreEqual(
                0,
                player.StatusEffects.Count); // 효과 적용 후 즉시 만료돼 제거됨
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
