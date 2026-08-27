using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 107일차: 이벤트 선택지 조건(능력치·아이템·골드·플래그) 판정을 검증한다.
    public sealed class EventConditionServiceTests
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

        [Test]
        public void Evaluate_NoConditions_IsAlwaysAvailable()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "그냥 지나간다");

            EventChoiceAvailabilityResult result =
                EventConditionService.Evaluate(
                    choice,
                    context);

            Assert.That(
                result.IsAvailable,
                Is.True);
        }

        [Test]
        public void Evaluate_StatConditionMet_IsAvailable()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            // PlayerRunState.CreateDefault 기준 기본 공격력은 50이다.
            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "힘으로 밀어붙인다",
                    new EventCondition(
                        EventConditionKind.Stat,
                        EventStatType.Attack,
                        40));

            EventChoiceAvailabilityResult result =
                EventConditionService.Evaluate(
                    choice,
                    context);

            Assert.That(
                result.IsAvailable,
                Is.True);
        }

        [Test]
        public void Evaluate_StatConditionNotMet_IsUnavailableWithReason()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "힘으로 밀어붙인다",
                    new EventCondition(
                        EventConditionKind.Stat,
                        EventStatType.Attack,
                        9999));

            EventChoiceAvailabilityResult result =
                EventConditionService.Evaluate(
                    choice,
                    context);

            Assert.That(
                result.IsAvailable,
                Is.False);

            Assert.That(
                result.UnavailableReason,
                Does.Contain("공격력"));
        }

        [Test]
        public void Evaluate_ItemConditionMet_IsAvailable()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            context.Inventory.TryAdd(
                "TORCH",
                "횃불",
                2,
                5,
                out _);

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "횃불로 길을 밝힌다",
                    new EventCondition(
                        EventConditionKind.Item,
                        "TORCH",
                        1));

            EventChoiceAvailabilityResult result =
                EventConditionService.Evaluate(
                    choice,
                    context);

            Assert.That(
                result.IsAvailable,
                Is.True);
        }

        [Test]
        public void Evaluate_ItemConditionNotMet_IsUnavailable()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "횃불로 길을 밝힌다",
                    new EventCondition(
                        EventConditionKind.Item,
                        "TORCH",
                        1));

            EventChoiceAvailabilityResult result =
                EventConditionService.Evaluate(
                    choice,
                    context);

            Assert.That(
                result.IsAvailable,
                Is.False);

            Assert.That(
                result.UnavailableReason,
                Does.Contain("TORCH"));
        }

        [Test]
        public void Evaluate_GoldConditionMet_IsAvailable()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            context.Player.Gold =
                100;

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "뇌물을 준다",
                    new EventCondition(
                        EventConditionKind.Gold,
                        (string)null,
                        50));

            EventChoiceAvailabilityResult result =
                EventConditionService.Evaluate(
                    choice,
                    context);

            Assert.That(
                result.IsAvailable,
                Is.True);
        }

        [Test]
        public void Evaluate_GoldConditionNotMet_IsUnavailable()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            context.Player.Gold =
                10;

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "뇌물을 준다",
                    new EventCondition(
                        EventConditionKind.Gold,
                        (string)null,
                        50));

            EventChoiceAvailabilityResult result =
                EventConditionService.Evaluate(
                    choice,
                    context);

            Assert.That(
                result.IsAvailable,
                Is.False);

            Assert.That(
                result.UnavailableReason,
                Does.Contain("골드"));
        }

        [Test]
        public void Evaluate_FlagRequiredAndSet_IsAvailable()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            context.Events.SetFlag(
                "MET_NPC_A",
                true);

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "NPC 이야기를 꺼낸다",
                    new EventCondition(
                        EventConditionKind.Flag,
                        "MET_NPC_A",
                        1));

            EventChoiceAvailabilityResult result =
                EventConditionService.Evaluate(
                    choice,
                    context);

            Assert.That(
                result.IsAvailable,
                Is.True);
        }

        [Test]
        public void Evaluate_FlagRequiredButNotSet_IsUnavailable()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "NPC 이야기를 꺼낸다",
                    new EventCondition(
                        EventConditionKind.Flag,
                        "MET_NPC_A",
                        1));

            EventChoiceAvailabilityResult result =
                EventConditionService.Evaluate(
                    choice,
                    context);

            Assert.That(
                result.IsAvailable,
                Is.False);
        }

        [Test]
        public void Evaluate_FlagMustBeAbsentAndIsAbsent_IsAvailable()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "처음 온 척한다",
                    new EventCondition(
                        EventConditionKind.Flag,
                        "MET_NPC_A",
                        0));

            EventChoiceAvailabilityResult result =
                EventConditionService.Evaluate(
                    choice,
                    context);

            Assert.That(
                result.IsAvailable,
                Is.True);
        }

        [Test]
        public void Evaluate_MultipleConditions_FirstFailureWinsAndOthersAreNotNeeded()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            context.Player.Gold =
                0;

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "복합 조건",
                    new EventCondition(
                        EventConditionKind.Gold,
                        (string)null,
                        100),
                    new EventCondition(
                        EventConditionKind.Item,
                        "TORCH",
                        1));

            EventChoiceAvailabilityResult result =
                EventConditionService.Evaluate(
                    choice,
                    context);

            Assert.That(
                result.IsAvailable,
                Is.False);

            Assert.That(
                result.UnavailableReason,
                Does.Contain("골드"));
        }

        [Test]
        public void Evaluate_MultipleConditionsAllMet_IsAvailable()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            context.Player.Gold =
                100;

            context.Inventory.TryAdd(
                "TORCH",
                "횃불",
                1,
                5,
                out _);

            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "복합 조건",
                    new EventCondition(
                        EventConditionKind.Gold,
                        (string)null,
                        100),
                    new EventCondition(
                        EventConditionKind.Item,
                        "TORCH",
                        1));

            EventChoiceAvailabilityResult result =
                EventConditionService.Evaluate(
                    choice,
                    context);

            Assert.That(
                result.IsAvailable,
                Is.True);
        }

        [Test]
        public void Evaluate_NullContext_IsUnavailable()
        {
            EventChoiceDefinition choice =
                new EventChoiceDefinition(
                    "아무 선택지");

            EventChoiceAvailabilityResult result =
                EventConditionService.Evaluate(
                    choice,
                    null);

            Assert.That(
                result.IsAvailable,
                Is.False);
        }

        [Test]
        public void Evaluate_NullChoice_IsUnavailable()
        {
            RunContext context =
                RunContext.Begin(
                    "TEST_RUN");

            EventChoiceAvailabilityResult result =
                EventConditionService.Evaluate(
                    null,
                    context);

            Assert.That(
                result.IsAvailable,
                Is.False);
        }
    }
}
