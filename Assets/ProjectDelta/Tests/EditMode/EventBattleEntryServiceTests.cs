using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EventBattleEntryServiceTests
    {
        [Test]
        public void TryEnter_AliveParticipants_CreatesContext()
        {
            bool entered =
                EventBattleEntryService.TryEnter(
                    EventBattleEntrySource.Seduction,
                    CreatePlayer(),
                    CreateTarget(),
                    out EventBattleContext context);

            Assert.IsTrue(
                entered);

            Assert.IsNotNull(
                context);

            Assert.AreEqual(
                EventBattleEntrySource.Seduction,
                context.Source);

            Assert.AreEqual(
                0,
                context.SelectedTarget.Favor);
        }

        [Test]
        public void TryEnter_NullPlayer_ReturnsFalse()
        {
            bool entered =
                EventBattleEntryService.TryEnter(
                    EventBattleEntrySource.Seduction,
                    null,
                    CreateTarget(),
                    out EventBattleContext context);

            Assert.IsFalse(
                entered);

            Assert.IsNull(
                context);
        }

        [Test]
        public void TryEnter_DeadTarget_ReturnsFalse()
        {
            BattleParticipant deadTarget =
                new BattleParticipant(
                    "MON_TEST",
                    "MON_TEST",
                    BattleTeam.Enemy,
                    10,
                    5,
                    3,
                    1,
                    80,
                    5,
                    10,
                    20,
                    0,
                    0,
                    currentHp: 0);

            bool entered =
                EventBattleEntryService.TryEnter(
                    EventBattleEntrySource.Seduction,
                    CreatePlayer(),
                    deadTarget,
                    out EventBattleContext context);

            Assert.IsFalse(
                entered);
        }

        private static BattleParticipant CreatePlayer()
        {
            return new BattleParticipant(
                "PLAYER",
                "PLAYER",
                BattleTeam.Player,
                10,
                5,
                4,
                2,
                80,
                5,
                30,
                10,
                20,
                20);
        }

        private static BattleParticipant CreateTarget()
        {
            return new BattleParticipant(
                "MON_TEST",
                "MON_TEST",
                BattleTeam.Enemy,
                10,
                5,
                3,
                1,
                80,
                5,
                10,
                20);
        }
    }
}
