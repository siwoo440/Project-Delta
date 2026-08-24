using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Battle Context 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleContextTests
    {
        [Test]
        public void TryGetEnemyAtSlot_ReturnsEnemiesInLeftToRightSlotOrder()
        {
            BattleContext context =
                CreateFullContext(); // 적 4명 Context 생성

            for (int slotIndex = 0; slotIndex < BattleContext.MaxEnemySlots; slotIndex++)
            {
                Assert.IsTrue(
                    context.TryGetEnemyAtSlot(
                        slotIndex,
                        out BattleParticipant enemy)); // 슬롯 조회 성공 확인

                Assert.AreEqual(
                    $"MON_TEST#{slotIndex + 1}",
                    enemy.InstanceId); // 맨 왼쪽부터 1~4번 순서 확인
            }
        }

        [Test]
        public void TryGetEnemyAtSlot_OutOfRangeSlot_ReturnsFalse()
        {
            BattleContext context =
                CreateFullContext(); // 적 4명 Context 생성

            Assert.IsFalse(
                context.TryGetEnemyAtSlot(
                    -1,
                    out BattleParticipant _)); // 음수 슬롯 거부 확인

            Assert.IsFalse(
                context.TryGetEnemyAtSlot(
                    BattleContext.MaxEnemySlots,
                    out BattleParticipant _)); // 최대 슬롯 초과 거부 확인
        }

        [Test]
        public void TryGetEnemyAtSlot_EmptySlot_ReturnsFalse()
        {
            BattleContext context =
                new BattleContext(
                    CreatePlayer(),
                    new[]
                    {
                        CreateEnemy(1)
                    }); // 적 1명만 있는 Context

            Assert.IsTrue(
                context.TryGetEnemyAtSlot(
                    0,
                    out BattleParticipant _)); // 1번 슬롯 조회 확인

            Assert.IsFalse(
                context.TryGetEnemyAtSlot(
                    1,
                    out BattleParticipant _)); // 빈 2번 슬롯 거부 확인
        }

        [Test]
        public void TryGetParticipant_FindsPlayerAndEachEnemyByInstanceId()
        {
            BattleContext context =
                CreateFullContext(); // 적 4명 Context 생성

            Assert.IsTrue(
                context.TryGetParticipant(
                    "PLAYER",
                    out BattleParticipant player)); // 플레이어 조회 확인

            Assert.AreEqual(
                BattleTeam.Player,
                player.Team); // 플레이어 진영 확인

            Assert.IsTrue(
                context.TryGetParticipant(
                    "MON_TEST#3",
                    out BattleParticipant enemy)); // 3번 슬롯 적 조회 확인

            Assert.AreEqual(
                BattleTeam.Enemy,
                enemy.Team); // 적 진영 확인

            Assert.IsFalse(
                context.TryGetParticipant(
                    "MON_TEST#9",
                    out BattleParticipant _)); // 없는 참가자 거부 확인
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
                0); // 테스트용 플레이어 참가자
        }

        private static BattleParticipant CreateEnemy(
            int slotNumber)
        {
            return new BattleParticipant(
                $"MON_TEST#{slotNumber}",
                "MON_TEST",
                BattleTeam.Enemy,
                10,
                5,
                4,
                2,
                80,
                5,
                0); // 슬롯 번호로 구분되는 테스트용 적 참가자
        }

        private static BattleContext CreateFullContext()
        {
            BattleParticipant[] enemies =
                new BattleParticipant[BattleContext.MaxEnemySlots]; // 적 슬롯 배열 생성

            for (int slotIndex = 0; slotIndex < enemies.Length; slotIndex++)
            {
                enemies[slotIndex] =
                    CreateEnemy(
                        slotIndex + 1); // 슬롯별 적 생성
            }

            return new BattleContext(
                CreatePlayer(),
                enemies); // 적 4명 Context 반환
        }
    }
}
