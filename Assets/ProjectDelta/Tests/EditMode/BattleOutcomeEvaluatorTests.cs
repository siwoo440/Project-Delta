using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Battle Outcome Evaluator 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleOutcomeEvaluatorTests
    {
        [Test]
        public void TryEvaluate_PlayerDead_ReturnsDefeat()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    0); // MaxHp 0 → 사망

            BattleParticipant enemy =
                CreateParticipant(
                    "ENEMY_1",
                    BattleTeam.Enemy,
                    10); // 생존

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            Assert.IsTrue(
                BattleOutcomeEvaluator.TryEvaluate(
                    context,
                    out BattleOutcome outcome)); // 판정 성립 확인

            Assert.AreEqual(
                BattleOutcome.Defeat,
                outcome);
        }

        [Test]
        public void TryEvaluate_AllEnemiesDead_ReturnsVictory()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    20); // 생존

            BattleParticipant enemy1 =
                CreateParticipant(
                    "ENEMY_1",
                    BattleTeam.Enemy,
                    0); // 사망

            BattleParticipant enemy2 =
                CreateParticipant(
                    "ENEMY_2",
                    BattleTeam.Enemy,
                    0); // 사망

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy1, enemy2 });

            Assert.IsTrue(
                BattleOutcomeEvaluator.TryEvaluate(
                    context,
                    out BattleOutcome outcome));

            Assert.AreEqual(
                BattleOutcome.Victory,
                outcome);
        }

        [Test]
        public void TryEvaluate_BothSidesHaveSurvivors_ReturnsFalse()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    20);

            BattleParticipant enemy1 =
                CreateParticipant(
                    "ENEMY_1",
                    BattleTeam.Enemy,
                    0); // 사망

            BattleParticipant enemy2 =
                CreateParticipant(
                    "ENEMY_2",
                    BattleTeam.Enemy,
                    10); // 생존

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy1, enemy2 });

            Assert.IsFalse(
                BattleOutcomeEvaluator.TryEvaluate(
                    context,
                    out BattleOutcome _)); // 아직 전투 진행 중 확인
        }

        [Test]
        public void TryEvaluate_MutualDestruction_PrefersDefeat()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    0); // 사망

            BattleParticipant enemy =
                CreateParticipant(
                    "ENEMY_1",
                    BattleTeam.Enemy,
                    0); // 사망

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            Assert.IsTrue(
                BattleOutcomeEvaluator.TryEvaluate(
                    context,
                    out BattleOutcome outcome));

            Assert.AreEqual(
                BattleOutcome.Defeat,
                outcome); // 상호 전멸 시 Defeat 우선 확인
        }

        [Test]
        public void TryEvaluate_NullContext_ReturnsFalse()
        {
            Assert.IsFalse(
                BattleOutcomeEvaluator.TryEvaluate(
                    null,
                    out BattleOutcome _));
        }

        private static BattleParticipant CreateParticipant(
            string instanceId,
            BattleTeam team,
            int maxHp)
        {
            return new BattleParticipant(
                instanceId,
                instanceId,
                team,
                maxHp,
                5,
                4,
                2,
                80,
                5,
                0);
        }
    }
}
