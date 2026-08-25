using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // BattleEscapeCalculator 사용
using ProjectDelta.Data; // StatusEffectKind·BattleStatType 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleEscapeCalculatorTests
    {
        [Test]
        public void CalculateEscapeChancePercent_EqualSpeed_ReturnsBaseChance()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    10);

            BattleParticipant enemy =
                CreateParticipant(
                    "MON_TEST",
                    BattleTeam.Enemy,
                    10);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            Assert.AreEqual(
                BattleEscapeCalculator.BaseEscapeChancePercent,
                BattleEscapeCalculator.CalculateEscapeChancePercent(
                    context,
                    player));
        }

        [Test]
        public void CalculateEscapeChancePercent_FasterActor_IncreasesChance()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    30); // 상대보다 20 빠름

            BattleParticipant enemy =
                CreateParticipant(
                    "MON_TEST",
                    BattleTeam.Enemy,
                    10);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            // 기본 50 + (30 - 10) = 70
            Assert.AreEqual(
                70,
                BattleEscapeCalculator.CalculateEscapeChancePercent(
                    context,
                    player));
        }

        [Test]
        public void CalculateEscapeChancePercent_SlowerActor_DecreasesChance()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    5);

            BattleParticipant enemy =
                CreateParticipant(
                    "MON_TEST",
                    BattleTeam.Enemy,
                    15); // 상대보다 10 느림

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            // 기본 50 - 10 = 40
            Assert.AreEqual(
                40,
                BattleEscapeCalculator.CalculateEscapeChancePercent(
                    context,
                    player));
        }

        [Test]
        public void CalculateEscapeChancePercent_NeverGoesBelowMinimum()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    1);

            BattleParticipant enemy =
                CreateParticipant(
                    "MON_TEST",
                    BattleTeam.Enemy,
                    999); // 극단적으로 빠른 상대

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            Assert.AreEqual(
                BattleEscapeCalculator.MinEscapeChancePercent,
                BattleEscapeCalculator.CalculateEscapeChancePercent(
                    context,
                    player));
        }

        [Test]
        public void CalculateEscapeChancePercent_NeverExceedsMaximum()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    999); // 극단적으로 빠른 시전자

            BattleParticipant enemy =
                CreateParticipant(
                    "MON_TEST",
                    BattleTeam.Enemy,
                    1);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            Assert.AreEqual(
                BattleEscapeCalculator.MaxEscapeChancePercent,
                BattleEscapeCalculator.CalculateEscapeChancePercent(
                    context,
                    player));
        }

        [Test]
        public void CalculateEscapeChancePercent_MultipleEnemies_UsesAverageSpeed()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    20);

            BattleParticipant fastEnemy =
                CreateParticipant(
                    "ENEMY_FAST",
                    BattleTeam.Enemy,
                    30);

            BattleParticipant slowEnemy =
                CreateParticipant(
                    "ENEMY_SLOW",
                    BattleTeam.Enemy,
                    10);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { fastEnemy, slowEnemy });

            // 상대 평균 Speed = (30 + 10) / 2 = 20 → 기본 50 + (20 - 20) = 50
            Assert.AreEqual(
                50,
                BattleEscapeCalculator.CalculateEscapeChancePercent(
                    context,
                    player));
        }

        [Test]
        public void CalculateEscapeChancePercent_SpeedUpBuff_IsReflectedInChance()
        {
            // 65일차 BattleStatModifierService가 계산하는 유효 Speed를 그대로 쓰는지 확인한다.
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    10);

            player.AddStatusEffect(
                new StatusEffectInstance(
                    "SE013",
                    "PLAYER",
                    2,
                    1,
                    10,
                    StatusEffectKind.StatModifier,
                    BattleStatType.Speed)); // 속도 상승 +10 → 유효 Speed 20

            BattleParticipant enemy =
                CreateParticipant(
                    "MON_TEST",
                    BattleTeam.Enemy,
                    10);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            // 기본 50 + (유효 20 - 10) = 60
            Assert.AreEqual(
                60,
                BattleEscapeCalculator.CalculateEscapeChancePercent(
                    context,
                    player));
        }

        private static BattleParticipant CreateParticipant(
            string instanceId,
            BattleTeam team,
            int speed)
        {
            return new BattleParticipant(
                instanceId,
                instanceId,
                team,
                10,
                speed,
                4,
                2,
                80,
                5,
                0);
        }
    }
}
