using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    // 108일차: 이벤트 선택 결과 적용과 "한 번만 확정" 중복 방지 규칙을 검증한다.
    public sealed class EventResultServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            InventoryRunState.MaxStackResolver =
                null;
        }

        [TearDown]
        public void TearDown()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }

            InventoryRunState.MaxStackResolver =
                null;
        }

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

        private static EventDefinition CreateRepeatableEventWithId(
            string id)
        {
            EventDefinition definition =
                CreateEventWithId(
                    id);

            System.Reflection.FieldInfo repeatableField =
                typeof(EventDefinition).GetField(
                    "isRepeatable",
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic);

            repeatableField.SetValue(
                definition,
                true);

            return definition;
        }

        // 109일차: 재등장 가능 이벤트는 "한 번만 확정" 게이트를 적용받지 않는다.
        [Test]
        public void ApplyChoice_RepeatableEvent_CanBeAppliedMultipleTimes()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            EventDefinition eventDefinition =
                CreateRepeatableEventWithId(
                    "EVT_SHRINE");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "기도한다",
                    new EventCondition[0],
                    new[]
                    {
                        new EventEffect(
                            EventEffectKind.GainGold,
                            5)
                    });

            try
            {
                EventResultService.ApplyChoice(
                    eventDefinition,
                    choice,
                    context);

                EventResultApplicationResult secondResult =
                    EventResultService.ApplyChoice(
                        eventDefinition,
                        choice,
                        context);

                Assert.That(
                    secondResult.Success,
                    Is.True);

                Assert.That(
                    context.Player.Gold,
                    Is.EqualTo(10));
            }
            finally
            {
                Object.DestroyImmediate(
                    eventDefinition);
            }
        }

        // 109일차: 재등장 가능 이벤트는 "확정됨" 플래그를 남기지 않는다(불필요한 플래그 누적 방지).
        [Test]
        public void ApplyChoice_RepeatableEvent_DoesNotSetResolvedFlag()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            EventDefinition eventDefinition =
                CreateRepeatableEventWithId(
                    "EVT_SHRINE_2");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "기도한다",
                    new EventCondition[0],
                    new[]
                    {
                        new EventEffect(
                            EventEffectKind.GainGold,
                            5)
                    });

            try
            {
                EventResultService.ApplyChoice(
                    eventDefinition,
                    choice,
                    context);

                Assert.That(
                    context.Events.HasFlag(
                        "EVENT_RESOLVED_EVT_SHRINE_2"),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(
                    eventDefinition);
            }
        }

        [Test]
        public void ApplyChoice_RestoreHp_ClampsToMaxAndReportsChange()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            context.Player.CurrentHp =
                10;

            EventDefinition eventDefinition =
                CreateEventWithId(
                    "EVT_HEAL");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "쉬어간다",
                    new EventCondition[0],
                    new[]
                    {
                        new EventEffect(
                            EventEffectKind.RestoreHp,
                            9999)
                    });

            try
            {
                EventResultApplicationResult result =
                    EventResultService.ApplyChoice(
                        eventDefinition,
                        choice,
                        context);

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    context.Player.CurrentHp,
                    Is.EqualTo(
                        context.Player.GetFinalStats().MaxHealth));

                Assert.That(
                    result.AppliedEffectSummaries.Count,
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(
                    eventDefinition);
            }
        }

        [Test]
        public void ApplyChoice_NegativeHp_DamagesButNotBelowZero()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            context.Player.CurrentHp =
                10;

            EventDefinition eventDefinition =
                CreateEventWithId(
                    "EVT_TRAP");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "함정을 건드린다",
                    new EventCondition[0],
                    new[]
                    {
                        new EventEffect(
                            EventEffectKind.RestoreHp,
                            -9999)
                    });

            try
            {
                EventResultService.ApplyChoice(
                    eventDefinition,
                    choice,
                    context);

                Assert.That(
                    context.Player.CurrentHp,
                    Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(
                    eventDefinition);
            }
        }

        [Test]
        public void ApplyChoice_GainGold_IncreasesGold()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            context.Player.Gold =
                10;

            EventDefinition eventDefinition =
                CreateEventWithId(
                    "EVT_GOLD");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "돈을 줍는다",
                    new EventCondition[0],
                    new[]
                    {
                        new EventEffect(
                            EventEffectKind.GainGold,
                            50)
                    });

            try
            {
                EventResultService.ApplyChoice(
                    eventDefinition,
                    choice,
                    context);

                Assert.That(
                    context.Player.Gold,
                    Is.EqualTo(60));
            }
            finally
            {
                Object.DestroyImmediate(
                    eventDefinition);
            }
        }

        [Test]
        public void ApplyChoice_SpendGold_DoesNotGoBelowZero()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            context.Player.Gold =
                10;

            EventDefinition eventDefinition =
                CreateEventWithId(
                    "EVT_TAX");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "통행세를 낸다",
                    new EventCondition[0],
                    new[]
                    {
                        new EventEffect(
                            EventEffectKind.GainGold,
                            -9999)
                    });

            try
            {
                EventResultService.ApplyChoice(
                    eventDefinition,
                    choice,
                    context);

                Assert.That(
                    context.Player.Gold,
                    Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(
                    eventDefinition);
            }
        }

        [Test]
        public void ApplyChoice_GainItem_AddsToInventory()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            // 최대 중첩을 넉넉히 잡아 2개가 한 슬롯에 들어가게 한다.
            InventoryRunState.MaxStackResolver =
                _ => 10;

            EventDefinition eventDefinition =
                CreateEventWithId(
                    "EVT_ITEM");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "아이템을 받는다",
                    new EventCondition[0],
                    new[]
                    {
                        new EventEffect(
                            EventEffectKind.GainItem,
                            "TORCH",
                            2,
                            "횃불")
                    });

            try
            {
                EventResultService.ApplyChoice(
                    eventDefinition,
                    choice,
                    context);

                Assert.That(
                    context.Inventory.Slots[0].ItemId,
                    Is.EqualTo("TORCH"));

                Assert.That(
                    context.Inventory.Slots[0].Quantity,
                    Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(
                    eventDefinition);
            }
        }

        [Test]
        public void ApplyChoice_SetFlag_UpdatesEventRunState()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            EventDefinition eventDefinition =
                CreateEventWithId(
                    "EVT_FLAG");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "약속한다",
                    new EventCondition[0],
                    new[]
                    {
                        new EventEffect(
                            EventEffectKind.SetFlag,
                            "PROMISED_NPC_A",
                            true)
                    });

            try
            {
                EventResultService.ApplyChoice(
                    eventDefinition,
                    choice,
                    context);

                Assert.That(
                    context.Events.HasFlag(
                        "PROMISED_NPC_A"),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(
                    eventDefinition);
            }
        }

        [Test]
        public void ApplyChoice_SecondCallOnSameEvent_FailsWithAlreadyResolved()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            EventDefinition eventDefinition =
                CreateEventWithId(
                    "EVT_ONCE");

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
                EventResultService.ApplyChoice(
                    eventDefinition,
                    choice,
                    context);

                EventResultApplicationResult secondResult =
                    EventResultService.ApplyChoice(
                        eventDefinition,
                        choice,
                        context);

                Assert.That(
                    secondResult.Success,
                    Is.False);

                Assert.That(
                    secondResult.FailureReason,
                    Is.EqualTo(
                        EventResultFailureReason.AlreadyResolved));

                // 골드가 두 번 지급되지 않아야 한다.
                Assert.That(
                    context.Player.Gold,
                    Is.EqualTo(10));
            }
            finally
            {
                Object.DestroyImmediate(
                    eventDefinition);
            }
        }

        [Test]
        public void ApplyChoice_RelationshipChange_IsNoOpButDoesNotThrow()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            EventDefinition eventDefinition =
                CreateEventWithId(
                    "EVT_RELATIONSHIP");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "호의를 베푼다",
                    new EventCondition[0],
                    new[]
                    {
                        new EventEffect(
                            EventEffectKind.RelationshipChange,
                            "NPC_A",
                            5,
                            "NPC A")
                    });

            try
            {
                EventResultApplicationResult result =
                    EventResultService.ApplyChoice(
                        eventDefinition,
                        choice,
                        context);

                Assert.That(
                    result.Success,
                    Is.True);

                Assert.That(
                    result.AppliedEffectSummaries.Count,
                    Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(
                    eventDefinition);
            }
        }

        [Test]
        public void ApplyChoice_NullContext_FailsWithInvalidState()
        {
            EventDefinition eventDefinition =
                CreateEventWithId(
                    "EVT_NULL");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "선택한다");

            try
            {
                EventResultApplicationResult result =
                    EventResultService.ApplyChoice(
                        eventDefinition,
                        choice,
                        null);

                Assert.That(
                    result.Success,
                    Is.False);

                Assert.That(
                    result.FailureReason,
                    Is.EqualTo(
                        EventResultFailureReason.InvalidState));
            }
            finally
            {
                Object.DestroyImmediate(
                    eventDefinition);
            }
        }
    }
}
