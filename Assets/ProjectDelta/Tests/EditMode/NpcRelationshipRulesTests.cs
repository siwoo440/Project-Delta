using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class NpcRelationshipRulesTests
    {
        [TestCase(0, NpcRelationshipStage.Neutral)]
        [TestCase(33, NpcRelationshipStage.Neutral)]
        [TestCase(34, NpcRelationshipStage.Interest)]
        [TestCase(66, NpcRelationshipStage.Interest)]
        [TestCase(67, NpcRelationshipStage.Trust)]
        [TestCase(84, NpcRelationshipStage.Trust)]
        [TestCase(85, NpcRelationshipStage.Special)]
        [TestCase(99, NpcRelationshipStage.Special)]
        [TestCase(100, NpcRelationshipStage.EndingAvailable)]
        public void GetStage_기획서구간에맞는관계단계를반환한다(
            int affinity,
            NpcRelationshipStage expected)
        {
            Assert.AreEqual(
                expected,
                NpcRelationshipRules.GetStage(
                    affinity));
        }

        [Test]
        public void RelationshipState_호감도를0에서100사이로제한한다()
        {
            NpcRelationshipState state =
                new NpcRelationshipState(
                    "NPC_TEST",
                    95,
                    false);

            state.ChangeAffinity(
                20);

            Assert.AreEqual(
                100,
                state.Affinity);

            state.ChangeAffinity(
                -200);

            Assert.AreEqual(
                0,
                state.Affinity);
        }
    }
}
