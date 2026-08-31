using System.Collections.Generic;
using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EventBattleAffinityRuleTests
    {
        [Test]
        public void ResolveMultiplier_StrongAction_ReturnsHalf()
        {
            float multiplier =
                EventBattleAffinityRule.ResolveMultiplier(
                    new List<string> { "Court" },
                    new List<string> { "Soothe" },
                    "Court");

            Assert.AreEqual(
                EventBattleAffinityRule.StrongMultiplier,
                multiplier);
        }

        [Test]
        public void ResolveMultiplier_WeakAction_ReturnsOneAndHalf()
        {
            float multiplier =
                EventBattleAffinityRule.ResolveMultiplier(
                    new List<string> { "Court" },
                    new List<string> { "Soothe" },
                    "Soothe");

            Assert.AreEqual(
                EventBattleAffinityRule.WeakMultiplier,
                multiplier);
        }

        [Test]
        public void ResolveMultiplier_UnlistedAction_ReturnsOne()
        {
            float multiplier =
                EventBattleAffinityRule.ResolveMultiplier(
                    new List<string> { "Court" },
                    new List<string> { "Soothe" },
                    "Flatter");

            Assert.AreEqual(
                EventBattleAffinityRule.NormalMultiplier,
                multiplier);
        }

        [Test]
        public void ResolveMultiplier_NullLists_ReturnsOne()
        {
            float multiplier =
                EventBattleAffinityRule.ResolveMultiplier(
                    null,
                    null,
                    "Court");

            Assert.AreEqual(
                EventBattleAffinityRule.NormalMultiplier,
                multiplier);
        }

        [Test]
        public void ResolveMultiplier_EmptyActionId_ReturnsOne()
        {
            float multiplier =
                EventBattleAffinityRule.ResolveMultiplier(
                    new List<string> { "Court" },
                    new List<string> { "Soothe" },
                    string.Empty);

            Assert.AreEqual(
                EventBattleAffinityRule.NormalMultiplier,
                multiplier);
        }
    }
}
