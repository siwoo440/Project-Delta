using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EventBattleContextTests
    {
        [Test]
        public void SelectedTarget_AddFavor_ClampsToFavorToWinAndMarksWon()
        {
            EventBattleContext context =
                CreateContext();

            context.SelectedTarget.AddFavor(
                999);

            Assert.AreEqual(
                EventBattleContext.FavorToWin,
                context.SelectedTarget.Favor);

            Assert.IsTrue(
                context.HasWon);
        }

        [Test]
        public void SelectedTarget_AddFavor_ClampsToZero()
        {
            EventBattleContext context =
                CreateContext();

            context.SelectedTarget.AddFavor(
                -999);

            Assert.AreEqual(
                0,
                context.SelectedTarget.Favor);
        }

        [Test]
        public void RegisterAttempt_TracksAttemptCount()
        {
            EventBattleContext context =
                CreateContext();

            context.RegisterAttempt();

            context.RegisterAttempt();

            Assert.AreEqual(
                2,
                context.AttemptCount);
        }

        [Test]
        public void PlayerCanAct_AnyActionAffordable_ReturnsTrue()
        {
            EventBattleContext context =
                CreateContext();

            Assert.IsTrue(
                context.PlayerCanAct(
                    EventBattleActionCatalog.All));
        }

        [Test]
        public void PlayerCanAct_NeitherAffordable_ReturnsFalse()
        {
            BattleParticipant player =
                new BattleParticipant(
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
                    1,
                    1);

            BattleParticipant target =
                new BattleParticipant(
                    "MON_TEST",
                    "MON_TEST",
                    BattleTeam.Enemy,
                    10,
                    5,
                    3,
                    1,
                    80,
                    5);

            EventBattleContext context =
                new EventBattleContext(
                    EventBattleEntrySource.Seduction,
                    player,
                    new[] { new EventBattleParticipantState(target) });

            Assert.IsFalse(
                context.PlayerCanAct(
                    EventBattleActionCatalog.All));
        }

        private static EventBattleContext CreateContext()
        {
            BattleParticipant player =
                new BattleParticipant(
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

            BattleParticipant target =
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
                    20);

            return new EventBattleContext(
                EventBattleEntrySource.Seduction,
                player,
                new[] { new EventBattleParticipantState(target) });
        }
    }
}
