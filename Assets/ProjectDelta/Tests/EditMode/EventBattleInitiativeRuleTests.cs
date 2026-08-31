using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EventBattleInitiativeRuleTests
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
        public void RollNext_PlayerHigherRoll_ReturnsPlayer()
        {
            EventBattleInitiativeHolder result =
                EventBattleInitiativeRule.RollNext(
                    30,
                    2,
                    10,
                    0,
                    EventBattleInitiativeHolder.Target,
                    new FixedRandomSource(10));

            Assert.AreEqual(
                EventBattleInitiativeHolder.Player,
                result);
        }

        [Test]
        public void RollNext_TargetHigherRoll_ReturnsTarget()
        {
            EventBattleInitiativeHolder result =
                EventBattleInitiativeRule.RollNext(
                    10,
                    0,
                    30,
                    2,
                    EventBattleInitiativeHolder.Player,
                    new FixedRandomSource(10));

            Assert.AreEqual(
                EventBattleInitiativeHolder.Target,
                result);
        }

        [Test]
        public void RollNext_Tie_KeepsCurrentHolder()
        {
            EventBattleInitiativeHolder result =
                EventBattleInitiativeRule.RollNext(
                    20,
                    0,
                    20,
                    0,
                    EventBattleInitiativeHolder.Target,
                    new FixedRandomSource(10));

            Assert.AreEqual(
                EventBattleInitiativeHolder.Target,
                result);
        }

        [Test]
        public void RollNext_NullRandomSource_KeepsCurrentHolder()
        {
            EventBattleInitiativeHolder result =
                EventBattleInitiativeRule.RollNext(
                    99,
                    0,
                    1,
                    0,
                    EventBattleInitiativeHolder.Player,
                    null);

            Assert.AreEqual(
                EventBattleInitiativeHolder.Player,
                result);
        }
    }
}
