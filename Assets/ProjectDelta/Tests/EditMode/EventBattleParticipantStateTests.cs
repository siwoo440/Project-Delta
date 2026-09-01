using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EventBattleParticipantStateTests
    {
        [Test]
        public void AddFavor_ReachesFavorToWin_SingleStage_MarksWon()
        {
            EventBattleParticipantState state =
                CreateState(
                    1);

            state.AddFavor(
                150);

            Assert.IsTrue(
                state.HasWon);

            Assert.AreEqual(
                EventBattleContext.FavorToWin,
                state.Favor);

            Assert.IsFalse(
                state.IsActive);
        }

        [Test]
        public void AddFavor_ReachesFavorToWin_TwoStage_AdvancesStageInsteadOfWinning()
        {
            EventBattleParticipantState state =
                CreateState(
                    2);

            state.AddFavor(
                150);

            Assert.IsFalse(
                state.HasWon);

            Assert.AreEqual(
                2,
                state.CurrentStage);

            Assert.AreEqual(
                0,
                state.Favor);

            Assert.IsTrue(
                state.IsActive);

            state.AddFavor(
                150);

            Assert.IsTrue(
                state.HasWon);
        }

        [Test]
        public void MarkSatisfiedDeparture_MakesInactive()
        {
            EventBattleParticipantState state =
                CreateState(
                    1);

            state.MarkSatisfiedDeparture();

            Assert.IsTrue(
                state.HasLeftSatisfied);

            Assert.IsFalse(
                state.IsActive);
        }

        [Test]
        public void AddFavor_AfterDeparture_IsIgnored()
        {
            EventBattleParticipantState state =
                CreateState(
                    1);

            state.MarkSatisfiedDeparture();

            state.AddFavor(
                50);

            Assert.AreEqual(
                0,
                state.Favor);
        }

        private static EventBattleParticipantState CreateState(
            int stageCount)
        {
            BattleParticipant participant =
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

            return new EventBattleParticipantState(
                participant,
                stageCount);
        }
    }
}
