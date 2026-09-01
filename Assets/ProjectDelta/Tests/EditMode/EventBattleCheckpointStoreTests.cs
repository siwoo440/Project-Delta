using NUnit.Framework;
using ProjectDelta.Data;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EventBattleCheckpointStoreTests
    {
        [TearDown]
        public void TearDown()
        {
            EventBattleCheckpointStore.Clear();
        }

        [Test]
        public void CaptureAndApplyPreservesCheckpoint()
        {
            EventBattleCheckpointStore.Capture(
                "Room_07",
                "Seduction",
                3,
                12,
                8,
                new[] { "MON_SUCCUBUS" },
                new[] { 40 },
                new[] { 1 });

            RunData data =
                new RunData();

            EventBattleCheckpointStore.ApplyTo(
                data);

            Assert.That(
                data.EventBattleCheckpoint.IsPending,
                Is.True);

            Assert.That(
                data.EventBattleCheckpoint.RoomId,
                Is.EqualTo("Room_07"));

            Assert.That(
                data.EventBattleCheckpoint.SourceLabel,
                Is.EqualTo("Seduction"));

            Assert.That(
                data.EventBattleCheckpoint.AttemptCount,
                Is.EqualTo(3));

            Assert.That(
                data.EventBattleCheckpoint.PlayerManaAtCheckpoint,
                Is.EqualTo(12));

            Assert.That(
                data.EventBattleCheckpoint.PlayerStaminaAtCheckpoint,
                Is.EqualTo(8));

            CollectionAssert.AreEqual(
                new[] { "MON_SUCCUBUS" },
                data.EventBattleCheckpoint.TargetDefinitionIds);

            CollectionAssert.AreEqual(
                new[] { 40 },
                data.EventBattleCheckpoint.TargetFavors);

            CollectionAssert.AreEqual(
                new[] { 1 },
                data.EventBattleCheckpoint.TargetStages);
        }

        [Test]
        public void CaptureWithoutRoomId_ClearsInsteadOfCapturing()
        {
            EventBattleCheckpointStore.Capture(
                string.Empty,
                "Seduction",
                0,
                0,
                0,
                null,
                null,
                null);

            Assert.That(
                EventBattleCheckpointStore.HasPending,
                Is.False);
        }

        [Test]
        public void ClearRemovesPendingCheckpoint()
        {
            EventBattleCheckpointStore.Capture(
                "Room_01",
                "Seduction",
                1,
                10,
                10,
                new[] { "MON_TEST" },
                new[] { 0 },
                new[] { 1 });

            EventBattleCheckpointStore.Clear();

            Assert.That(
                EventBattleCheckpointStore.HasPending,
                Is.False);
        }

        [Test]
        public void RestoreAndClear_PendingCheckpoint_InvokesCallbackAndClears()
        {
            EventBattleCheckpointStore.Capture(
                "Room_02",
                "Seduction",
                2,
                5,
                5,
                new[] { "MON_TEST" },
                new[] { 20 },
                new[] { 1 });

            string recoveredMessage =
                null;

            EventBattleCheckpointStore.RestoreAndClear(
                EventBattleCheckpointStore.Pending,
                message => recoveredMessage = message);

            Assert.That(
                recoveredMessage,
                Is.Not.Null.And.Contains("Room_02"));

            Assert.That(
                EventBattleCheckpointStore.HasPending,
                Is.False);
        }

        [Test]
        public void RestoreAndClear_NotPending_DoesNotInvokeCallback()
        {
            bool invoked =
                false;

            EventBattleCheckpointStore.RestoreAndClear(
                new EventBattleCheckpointData(),
                message => invoked = true);

            Assert.That(
                invoked,
                Is.False);

            Assert.That(
                EventBattleCheckpointStore.HasPending,
                Is.False);
        }
    }
}
