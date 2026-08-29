using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    // 109일차: 이벤트 플래그 저장·복원과, 복원 후에도 1회성 이벤트는 다시 실행되지
    // 않고 재등장 가능 이벤트는 다시 실행되는지를 검증한다.
    public sealed class EventPersistenceTests
    {
        [SetUp]
        public void SetUp()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            DungeonSaveMapper.ClearPendingRestore();
        }

        [TearDown]
        public void TearDown()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            DungeonSaveMapper.ClearPendingRestore();
        }

        [Test]
        public void SaveRestore_PreservesEventFlags()
        {
            RunContext source =
                RunContext.Begin(
                    "DAY109_SOURCE");

            source.Events.SetFlag(
                "EVENT_RESOLVED_EVT_A",
                true);

            source.Events.SetFlag(
                "MET_NPC_A",
                true);

            RunData saved =
                DungeonSaveMapper.BuildFromRunContext(
                    source);

            Assert.That(
                saved.EventFlags,
                Does.Contain("EVENT_RESOLVED_EVT_A"));

            Assert.That(
                saved.EventFlags,
                Does.Contain("MET_NPC_A"));

            RunContext.End();

            RunContext restored =
                RunContext.Begin(
                    "DAY109_RESTORED");

            DungeonSaveMapper.ApplyBasics(
                restored,
                saved);

            Assert.That(
                restored.Events.HasFlag(
                    "EVENT_RESOLVED_EVT_A"),
                Is.True);

            Assert.That(
                restored.Events.HasFlag(
                    "MET_NPC_A"),
                Is.True);
        }

        [Test]
        public void FullPipeline_OneTimeEvent_StaysResolvedAfterSaveAndRestore()
        {
            RunContext source =
                RunContext.Begin(
                    "DAY109_SOURCE");

            EventDefinition eventDefinition =
                ScriptableObject.CreateInstance<EventDefinition>();

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "선택한다",
                    new EventCondition[0],
                    new[]
                    {
                        new EventEffect(
                            EventEffectKind.GainGold,
                            10)
                    });

            try
            {
                SetDefinitionId(
                    eventDefinition,
                    "EVT_ONCE_ONLY");

                EventResultApplicationResult firstAttempt =
                    EventResultService.ApplyChoice(
                        eventDefinition,
                        choice,
                        source);

                Assert.That(
                    firstAttempt.Success,
                    Is.True);

                RunData saved =
                    DungeonSaveMapper.BuildFromRunContext(
                        source);

                RunContext.End();

                RunContext restored =
                    RunContext.Begin(
                        "DAY109_RESTORED");

                DungeonSaveMapper.ApplyBasics(
                    restored,
                    saved);

                EventResultApplicationResult secondAttempt =
                    EventResultService.ApplyChoice(
                        eventDefinition,
                        choice,
                        restored);

                Assert.That(
                    secondAttempt.Success,
                    Is.False);

                Assert.That(
                    secondAttempt.FailureReason,
                    Is.EqualTo(
                        EventResultFailureReason.AlreadyResolved));

                // 골드가 복원 후 두 번째로 지급되지 않아야 한다(10 그대로).
                Assert.That(
                    restored.Player.Gold,
                    Is.EqualTo(10));
            }
            finally
            {
                Object.DestroyImmediate(
                    eventDefinition);
            }
        }

        [Test]
        public void FullPipeline_RepeatableEvent_CanBeAppliedAgainAfterRestore()
        {
            RunContext source =
                RunContext.Begin(
                    "DAY109_SOURCE");

            EventDefinition eventDefinition =
                ScriptableObject.CreateInstance<EventDefinition>();

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "다시 방문한다",
                    new EventCondition[0],
                    new[]
                    {
                        new EventEffect(
                            EventEffectKind.GainGold,
                            10)
                    });

            try
            {
                SetDefinitionId(
                    eventDefinition,
                    "EVT_REPEATABLE");

                SetIsRepeatable(
                    eventDefinition,
                    true);

                EventResultService.ApplyChoice(
                    eventDefinition,
                    choice,
                    source);

                RunData saved =
                    DungeonSaveMapper.BuildFromRunContext(
                        source);

                RunContext.End();

                RunContext restored =
                    RunContext.Begin(
                        "DAY109_RESTORED");

                DungeonSaveMapper.ApplyBasics(
                    restored,
                    saved);

                EventResultApplicationResult secondAttempt =
                    EventResultService.ApplyChoice(
                        eventDefinition,
                        choice,
                        restored);

                Assert.That(
                    secondAttempt.Success,
                    Is.True);

                // 반복 가능 이벤트라 골드가 두 번(10 + 10) 지급되어야 한다.
                Assert.That(
                    restored.Player.Gold,
                    Is.EqualTo(20));
            }
            finally
            {
                Object.DestroyImmediate(
                    eventDefinition);
            }
        }

        private static void SetDefinitionId(
            EventDefinition definition,
            string id)
        {
            FieldInfo idField =
                typeof(DefinitionBase).GetField(
                    "id",
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            idField.SetValue(
                definition,
                id);
        }

        private static void SetIsRepeatable(
            EventDefinition definition,
            bool value)
        {
            FieldInfo field =
                typeof(EventDefinition).GetField(
                    "isRepeatable",
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            field.SetValue(
                definition,
                value);
        }
    }
}
