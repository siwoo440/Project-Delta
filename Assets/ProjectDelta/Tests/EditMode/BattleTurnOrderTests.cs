using System.Collections.Generic;
using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Battle Turn Order 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleTurnOrderTests
    {
        [Test]
        public void Build_SortsBySpeedDescending()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    10); // Speed 10

            BattleParticipant enemy1 =
                CreateParticipant(
                    "ENEMY_1",
                    BattleTeam.Enemy,
                    20); // Speed 20

            BattleParticipant enemy2 =
                CreateParticipant(
                    "ENEMY_2",
                    BattleTeam.Enemy,
                    15); // Speed 15

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy1, enemy2 });

            IReadOnlyList<BattleParticipant> order =
                BattleTurnOrder.Build(
                    context);

            Assert.AreEqual(
                3,
                order.Count); // 전원 포함 확인

            Assert.AreEqual(
                "ENEMY_1",
                order[0].InstanceId); // Speed 20이 1순위 확인

            Assert.AreEqual(
                "ENEMY_2",
                order[1].InstanceId); // Speed 15가 2순위 확인

            Assert.AreEqual(
                "PLAYER",
                order[2].InstanceId); // Speed 10이 3순위 확인
        }

        [Test]
        public void Build_SameSpeedTie_PlayerActsBeforeEnemies()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    5); // Speed 5

            BattleParticipant enemy =
                CreateParticipant(
                    "ENEMY_1",
                    BattleTeam.Enemy,
                    5); // Speed 5 동률

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            IReadOnlyList<BattleParticipant> order =
                BattleTurnOrder.Build(
                    context);

            Assert.AreEqual(
                "PLAYER",
                order[0].InstanceId); // 동률이면 Player가 먼저 확인

            Assert.AreEqual(
                "ENEMY_1",
                order[1].InstanceId);
        }

        [Test]
        public void Build_SameSpeedTie_EnemiesKeepSlotOrder()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    1); // 가장 느림

            BattleParticipant enemySlot1 =
                CreateParticipant(
                    "ENEMY_SLOT_1",
                    BattleTeam.Enemy,
                    20);

            BattleParticipant enemySlot2 =
                CreateParticipant(
                    "ENEMY_SLOT_2",
                    BattleTeam.Enemy,
                    20); // Slot1과 동률

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemySlot1, enemySlot2 });

            IReadOnlyList<BattleParticipant> order =
                BattleTurnOrder.Build(
                    context);

            Assert.AreEqual(
                "ENEMY_SLOT_1",
                order[0].InstanceId); // 동률이면 왼쪽(1번) 슬롯이 먼저 확인

            Assert.AreEqual(
                "ENEMY_SLOT_2",
                order[1].InstanceId);
        }

        [Test]
        public void Build_ExcludesDeadParticipants()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    10);

            BattleParticipant deadEnemy =
                new BattleParticipant(
                    "ENEMY_DEAD",
                    "ENEMY_DEAD",
                    BattleTeam.Enemy,
                    0,
                    20); // MaxHp 0 → CurrentHp 0 → IsAlive false

            BattleParticipant aliveEnemy =
                CreateParticipant(
                    "ENEMY_ALIVE",
                    BattleTeam.Enemy,
                    5);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { deadEnemy, aliveEnemy });

            IReadOnlyList<BattleParticipant> order =
                BattleTurnOrder.Build(
                    context);

            Assert.AreEqual(
                2,
                order.Count); // 사망한 참가자 제외 확인

            CollectionAssert.DoesNotContain(
                order,
                deadEnemy);
        }

        [Test]
        public void Build_NullContext_ReturnsEmptyList()
        {
            IReadOnlyList<BattleParticipant> order =
                BattleTurnOrder.Build(
                    null);

            Assert.AreEqual(
                0,
                order.Count); // 빈 목록 확인
        }

        [Test]
        public void Build_NoEnemies_ReturnsPlayerOnly()
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player,
                    10);

            BattleContext context =
                new BattleContext(
                    player,
                    null);

            IReadOnlyList<BattleParticipant> order =
                BattleTurnOrder.Build(
                    context);

            Assert.AreEqual(
                1,
                order.Count);

            Assert.AreEqual(
                "PLAYER",
                order[0].InstanceId);
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
                speed);
        }
    }
}
