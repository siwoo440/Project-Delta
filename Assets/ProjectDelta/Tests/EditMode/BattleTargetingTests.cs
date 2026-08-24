using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Battle Targeting 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleTargetingTests
    {
        [Test]
        public void GetValidTargets_PlayerActor_ReturnsAliveEnemiesOnly()
        {
            BattleParticipant player =
                CreatePlayer();

            BattleParticipant aliveEnemy =
                CreateEnemy(
                    "ENEMY_ALIVE",
                    10);

            BattleParticipant deadEnemy =
                CreateEnemy(
                    "ENEMY_DEAD",
                    0); // MaxHp 0 → IsAlive false

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { aliveEnemy, deadEnemy });

            var targets =
                BattleTargeting.GetValidTargets(
                    context,
                    player);

            Assert.AreEqual(
                1,
                targets.Count); // 살아있는 적만 포함 확인

            Assert.AreEqual(
                "ENEMY_ALIVE",
                targets[0].InstanceId);
        }

        [Test]
        public void GetValidTargets_EnemyActor_ReturnsAlivePlayerOnly()
        {
            BattleParticipant player =
                CreatePlayer();

            BattleParticipant enemy =
                CreateEnemy(
                    "ENEMY_1",
                    10);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            var targets =
                BattleTargeting.GetValidTargets(
                    context,
                    enemy);

            Assert.AreEqual(
                1,
                targets.Count); // Player만 대상 확인

            Assert.AreEqual(
                "PLAYER",
                targets[0].InstanceId);
        }

        [Test]
        public void GetValidTargets_NullContextOrActor_ReturnsEmpty()
        {
            BattleParticipant player =
                CreatePlayer();

            Assert.AreEqual(
                0,
                BattleTargeting.GetValidTargets(
                    null,
                    player).Count); // Context 없음 확인

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateEnemy("ENEMY_1", 10) });

            Assert.AreEqual(
                0,
                BattleTargeting.GetValidTargets(
                    context,
                    null).Count); // 행동자 없음 확인
        }

        [Test]
        public void IsValidTarget_AliveOpposingParticipant_ReturnsTrue()
        {
            BattleParticipant player =
                CreatePlayer();

            BattleParticipant enemy =
                CreateEnemy(
                    "ENEMY_1",
                    10);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            Assert.IsTrue(
                BattleTargeting.IsValidTarget(
                    context,
                    player,
                    enemy)); // 정상 대상 확인
        }

        [Test]
        public void IsValidTarget_SameTeam_ReturnsFalse()
        {
            BattleParticipant player =
                CreatePlayer();

            BattleParticipant enemy =
                CreateEnemy(
                    "ENEMY_1",
                    10);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            Assert.IsFalse(
                BattleTargeting.IsValidTarget(
                    context,
                    player,
                    player)); // 아군(자기 자신) 오폭 금지 확인
        }

        [Test]
        public void IsValidTarget_DeadTarget_ReturnsFalse()
        {
            BattleParticipant player =
                CreatePlayer();

            BattleParticipant deadEnemy =
                CreateEnemy(
                    "ENEMY_DEAD",
                    0);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { deadEnemy });

            Assert.IsFalse(
                BattleTargeting.IsValidTarget(
                    context,
                    player,
                    deadEnemy)); // 사망한 대상 거부 확인
        }

        [Test]
        public void IsValidTarget_ParticipantOutsideContext_ReturnsFalse()
        {
            BattleParticipant player =
                CreatePlayer();

            BattleParticipant enemy =
                CreateEnemy(
                    "ENEMY_1",
                    10);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            BattleParticipant outsider =
                CreateEnemy(
                    "OUTSIDER",
                    10);

            Assert.IsFalse(
                BattleTargeting.IsValidTarget(
                    context,
                    player,
                    outsider)); // Context에 속하지 않은 대상 거부 확인
        }

        private static BattleParticipant CreatePlayer()
        {
            return new BattleParticipant(
                "PLAYER",
                "PLAYER",
                BattleTeam.Player,
                20,
                5,
                6,
                3,
                90,
                10,
                0);
        }

        private static BattleParticipant CreateEnemy(
            string instanceId,
            int maxHp)
        {
            return new BattleParticipant(
                instanceId,
                instanceId,
                BattleTeam.Enemy,
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
