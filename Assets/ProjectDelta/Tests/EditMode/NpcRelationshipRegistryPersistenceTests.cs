using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 115일차: NpcRelationshipRegistry의 저장/복원(All, Restore, Clear)을 검증한다.
    public sealed class NpcRelationshipRegistryPersistenceTests
    {
        [SetUp]
        public void SetUp()
        {
            NpcRelationshipRegistry.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            NpcRelationshipRegistry.Clear();
        }

        [Test]
        public void Restore_RecreatesStateWithSavedValues()
        {
            NpcRelationshipRegistry.Restore(
                "NPC_TEST",
                72,
                true,
                5,
                true);

            Assert.That(
                NpcRelationshipRegistry.TryGet(
                    "NPC_TEST",
                    out NpcRelationshipState state),
                Is.True);

            Assert.That(
                state.Affinity,
                Is.EqualTo(72));

            Assert.That(
                state.IsHostile,
                Is.True);

            Assert.That(
                state.EncounterCount,
                Is.EqualTo(5));

            Assert.That(
                state.HasBeenRescued,
                Is.True);
        }

        [Test]
        public void All_ReflectsRegisteredStates()
        {
            NpcRelationshipRegistry.GetOrCreate(
                "NPC_A",
                10,
                false);

            NpcRelationshipRegistry.GetOrCreate(
                "NPC_B",
                20,
                false);

            Assert.That(
                NpcRelationshipRegistry.All.Count,
                Is.EqualTo(2));

            Assert.That(
                NpcRelationshipRegistry.All.ContainsKey(
                    "NPC_A"),
                Is.True);
        }

        [Test]
        public void Clear_RemovesAllStates()
        {
            NpcRelationshipRegistry.GetOrCreate(
                "NPC_A",
                10,
                false);

            NpcRelationshipRegistry.Clear();

            Assert.That(
                NpcRelationshipRegistry.All.Count,
                Is.EqualTo(0));

            Assert.That(
                NpcRelationshipRegistry.TryGet(
                    "NPC_A",
                    out _),
                Is.False);
        }
    }
}
