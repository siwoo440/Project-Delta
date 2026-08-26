using NUnit.Framework;
using ProjectDelta.Data;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class BattleEncounterCheckpointTests
    {
        [TearDown]
        public void TearDown()
        {
            BattleEncounterCheckpointStore.Clear();
        }

        [Test]
        public void CaptureAndApplyPreservesEncounterCheckpoint()
        {
            BattleEncounterCheckpointStore.Capture(
                "Room_07",
                "MON_SLIME",
                new Vector2Int(
                    2,
                    -1),
                new[]
                {
                    "MON_SLIME",
                    "MON_BAT"
                });

            RunData data =
                new RunData();

            BattleEncounterCheckpointStore.ApplyTo(
                data);

            Assert.That(
                data.BattleEncounterCheckpoint.IsPending,
                Is.True);

            Assert.That(
                data.BattleEncounterCheckpoint.RoomId,
                Is.EqualTo("Room_07"));

            Assert.That(
                data.BattleEncounterCheckpoint.MonsterDefinitionId,
                Is.EqualTo("MON_SLIME"));

            Assert.That(
                data.BattleEncounterCheckpoint.MonsterGridPosition,
                Is.EqualTo(new Vector2Int(2, -1)));

            CollectionAssert.AreEqual(
                new[]
                {
                    "MON_SLIME",
                    "MON_BAT"
                },
                data.BattleEncounterCheckpoint.MonsterGroupDefinitionIds);
        }

        [Test]
        public void RestoreRecreatesPendingCheckpoint()
        {
            BattleEncounterCheckpointData saved =
                new BattleEncounterCheckpointData
                {
                    IsPending = true,
                    RoomId = "Room_03",
                    MonsterDefinitionId = "MON_WOLF",
                    MonsterGridPosition = new Vector2Int(-1, 1)
                };

            saved.MonsterGroupDefinitionIds.Add(
                "MON_WOLF");

            BattleEncounterCheckpointStore.Restore(
                saved);

            Assert.That(
                BattleEncounterCheckpointStore.HasPending,
                Is.True);

            BattleEncounterCheckpointData restored =
                BattleEncounterCheckpointStore.Pending;

            Assert.That(
                restored.RoomId,
                Is.EqualTo("Room_03"));

            Assert.That(
                restored.MonsterDefinitionId,
                Is.EqualTo("MON_WOLF"));
        }

        [Test]
        public void ClearRemovesPendingCheckpoint()
        {
            BattleEncounterCheckpointStore.Capture(
                "Room_01",
                "MON_TEST",
                Vector2Int.zero,
                null);

            BattleEncounterCheckpointStore.Clear();

            Assert.That(
                BattleEncounterCheckpointStore.HasPending,
                Is.False);
        }

        [Test]
        public void InvalidSavedCheckpointDoesNotRestore()
        {
            BattleEncounterCheckpointStore.Restore(
                new BattleEncounterCheckpointData());

            Assert.That(
                BattleEncounterCheckpointStore.HasPending,
                Is.False);
        }
    }
}
