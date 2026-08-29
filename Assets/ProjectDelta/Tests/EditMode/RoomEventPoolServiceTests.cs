using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    // 111일차: Event 방에서 보여줄 EventDefinition을 후보 목록에서 고르는 로직을 검증한다.
    public sealed class RoomEventPoolServiceTests
    {
        private static EventDefinition CreateEventWithId(
            string id)
        {
            EventDefinition definition =
                ScriptableObject.CreateInstance<EventDefinition>();

            System.Reflection.FieldInfo idField =
                typeof(DefinitionBase).GetField(
                    "id",
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic);

            idField.SetValue(
                definition,
                id);

            return definition;
        }

        [Test]
        public void Pick_EmptyOrNullPool_ReturnsNull()
        {
            Assert.That(
                RoomEventPoolService.Pick(
                    null),
                Is.Null);

            Assert.That(
                RoomEventPoolService.Pick(
                    new EventDefinition[0]),
                Is.Null);
        }

        [Test]
        public void Pick_SingleEntryPool_AlwaysReturnsThatEntry()
        {
            EventDefinition definition =
                CreateEventWithId(
                    "EVENT_ONLY");

            EventDefinition[] pool =
                { definition };

            for (int trial = 0; trial < 20; trial++)
            {
                Assert.That(
                    RoomEventPoolService.Pick(
                        pool),
                    Is.SameAs(
                        definition));
            }
        }

        [Test]
        public void Pick_ManyTrials_OnlyReturnsPoolMembers()
        {
            EventDefinition first =
                CreateEventWithId(
                    "EVENT_A");

            EventDefinition second =
                CreateEventWithId(
                    "EVENT_B");

            EventDefinition[] pool =
                { first, second };

            System.Random random =
                new System.Random(
                    2026);

            for (int trial = 0; trial < 200; trial++)
            {
                EventDefinition picked =
                    RoomEventPoolService.Pick(
                        pool,
                        random);

                Assert.That(
                    picked == first
                    || picked == second,
                    Is.True);
            }
        }
    }
}
