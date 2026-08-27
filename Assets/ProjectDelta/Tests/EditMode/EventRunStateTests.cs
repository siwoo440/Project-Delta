using System.Collections.Generic;
using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 107일차: 이벤트 플래그 저장소 동작을 검증한다.
    public sealed class EventRunStateTests
    {
        [Test]
        public void HasFlag_UnsetFlag_ReturnsFalse()
        {
            EventRunState events =
                new EventRunState();

            Assert.That(
                events.HasFlag(
                    "MET_NPC_A"),
                Is.False);
        }

        [Test]
        public void SetFlag_True_MakesHasFlagReturnTrue()
        {
            EventRunState events =
                new EventRunState();

            events.SetFlag(
                "MET_NPC_A",
                true);

            Assert.That(
                events.HasFlag(
                    "MET_NPC_A"),
                Is.True);
        }

        [Test]
        public void SetFlag_False_ClearsFlag()
        {
            EventRunState events =
                new EventRunState();

            events.SetFlag(
                "MET_NPC_A",
                true);

            events.SetFlag(
                "MET_NPC_A",
                false);

            Assert.That(
                events.HasFlag(
                    "MET_NPC_A"),
                Is.False);
        }

        [Test]
        public void SetFlag_EmptyName_IsIgnored()
        {
            EventRunState events =
                new EventRunState();

            events.SetFlag(
                string.Empty,
                true);

            Assert.That(
                events.Flags.Count,
                Is.EqualTo(0));
        }

        [Test]
        public void RestoreFrom_ReplacesExistingFlags()
        {
            EventRunState events =
                new EventRunState();

            events.SetFlag(
                "OLD_FLAG",
                true);

            events.RestoreFrom(
                new List<string>
                {
                    "FLAG_A",
                    "FLAG_B"
                });

            Assert.That(
                events.HasFlag(
                    "OLD_FLAG"),
                Is.False);

            Assert.That(
                events.HasFlag(
                    "FLAG_A"),
                Is.True);

            Assert.That(
                events.HasFlag(
                    "FLAG_B"),
                Is.True);
        }

        [Test]
        public void RestoreFrom_Null_ClearsAllFlags()
        {
            EventRunState events =
                new EventRunState();

            events.SetFlag(
                "FLAG_A",
                true);

            events.RestoreFrom(
                null);

            Assert.That(
                events.Flags.Count,
                Is.EqualTo(0));
        }
    }
}
