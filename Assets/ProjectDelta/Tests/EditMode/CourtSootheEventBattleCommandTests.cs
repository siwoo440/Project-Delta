using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class CourtSootheEventBattleCommandTests
    {
        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly int fixedRoll;

            public FixedRandomSource(
                int fixedRoll)
            {
                this.fixedRoll =
                    fixedRoll;
            }

            public int NextInt(
                int minInclusive,
                int maxExclusive)
            {
                return fixedRoll;
            }
        }

        [Test]
        public void Court_ExposesStableIdAndDisplayNameAndCosts()
        {
            IEventBattleCommand command =
                new CourtEventBattleCommand();

            Assert.AreEqual(
                "Court",
                command.Id);

            Assert.AreEqual(
                "구애",
                command.DisplayName);

            Assert.Greater(
                command.ManaCost,
                0);

            Assert.AreEqual(
                0,
                command.StaminaCost);
        }

        [Test]
        public void Soothe_ExposesStableIdAndDisplayNameAndCosts()
        {
            IEventBattleCommand command =
                new SootheEventBattleCommand();

            Assert.AreEqual(
                "Soothe",
                command.Id);

            Assert.AreEqual(
                "달래기",
                command.DisplayName);

            Assert.Greater(
                command.StaminaCost,
                0);

            Assert.AreEqual(
                0,
                command.ManaCost);
        }

        [Test]
        public void Court_EnoughMana_SpendsManaAndAddsFavor()
        {
            EventBattleContext context =
                CreateContext();

            int manaBefore =
                context.Player.CurrentMana;

            IEventBattleCommand command =
                new CourtEventBattleCommand();

            EventBattleCommandResult result =
                command.Execute(
                    context,
                    new FixedRandomSource(0));

            Assert.IsTrue(
                result.Accepted);

            Assert.Greater(
                result.FavorGained,
                0);

            Assert.AreEqual(
                context.SelectedTarget.Favor,
                result.FavorGained);

            Assert.AreEqual(
                manaBefore - command.ManaCost,
                context.Player.CurrentMana);
        }

        [Test]
        public void Court_NotEnoughMana_RejectsWithoutChangingFavor()
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
                    5,
                    20);

            BattleParticipant target =
                CreateTarget();

            EventBattleContext context =
                new EventBattleContext(
                    EventBattleEntrySource.Seduction,
                    player,
                    new[] { new EventBattleParticipantState(target) });

            EventBattleCommandResult result =
                new CourtEventBattleCommand()
                    .Execute(
                        context,
                        new FixedRandomSource(0));

            Assert.IsFalse(
                result.Accepted);

            Assert.AreEqual(
                0,
                context.SelectedTarget.Favor);
        }

        [Test]
        public void Soothe_EnoughStamina_SpendsStaminaAndAddsFavor()
        {
            EventBattleContext context =
                CreateContext();

            int staminaBefore =
                context.Player.CurrentStamina;

            IEventBattleCommand command =
                new SootheEventBattleCommand();

            EventBattleCommandResult result =
                command.Execute(
                    context,
                    new FixedRandomSource(0));

            Assert.IsTrue(
                result.Accepted);

            Assert.Greater(
                result.FavorGained,
                0);

            Assert.AreEqual(
                staminaBefore - command.StaminaCost,
                context.Player.CurrentStamina);
        }

        [Test]
        public void Execute_NullContext_Rejects()
        {
            Assert.IsFalse(
                new CourtEventBattleCommand()
                    .Execute(
                        null,
                        new FixedRandomSource(0))
                    .Accepted);

            Assert.IsFalse(
                new SootheEventBattleCommand()
                    .Execute(
                        null,
                        new FixedRandomSource(0))
                    .Accepted);
        }

        private static EventBattleContext CreateContext()
        {
            return new EventBattleContext(
                EventBattleEntrySource.Seduction,
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
                    20),
                new[] { new EventBattleParticipantState(CreateTarget()) });
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
