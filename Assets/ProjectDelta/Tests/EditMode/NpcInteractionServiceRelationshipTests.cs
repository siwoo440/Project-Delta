using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    // 115일차: 선물·구조·공격(적대 전환) 결과를 검증한다.
    public sealed class NpcInteractionServiceRelationshipTests
    {
        private static NpcDefinition CreateBattleCapableNpc()
        {
            NpcDefinition definition =
                ScriptableObject.CreateInstance<NpcDefinition>();

            definition.ConfigureRuntime(
                "NPC_HOSTILE_TEST",
                "떠돌이",
                NpcServiceType.None,
                NpcHostilityMode.CanBecomeHostile,
                0);

            return definition;
        }

        [Test]
        public void ResolveGift_IncreasesAffinity()
        {
            NpcRelationshipState state =
                new NpcRelationshipState(
                    "NPC_A",
                    50,
                    false);

            NpcInteractionResult result =
                new NpcInteractionService().ResolveGift(
                    state,
                    "회복 물약",
                    10);

            Assert.That(
                result.ResultType,
                Is.EqualTo(
                    NpcInteractionResultType.ContinueInteraction));

            Assert.That(
                state.Affinity,
                Is.EqualTo(60));
        }

        [Test]
        public void ResolveRescue_FirstTime_IncreasesAffinityAndMarksRescued()
        {
            NpcRelationshipState state =
                new NpcRelationshipState(
                    "NPC_A",
                    30,
                    false);

            NpcInteractionResult result =
                new NpcInteractionService().ResolveRescue(
                    state,
                    20);

            Assert.That(
                result.ResultType,
                Is.EqualTo(
                    NpcInteractionResultType.ContinueInteraction));

            Assert.That(
                state.Affinity,
                Is.EqualTo(50));

            Assert.That(
                state.HasBeenRescued,
                Is.True);
        }

        [Test]
        public void ResolveRescue_SecondTime_DoesNotChangeAffinityAgain()
        {
            NpcRelationshipState state =
                new NpcRelationshipState(
                    "NPC_A",
                    30,
                    false);

            NpcInteractionService service =
                new NpcInteractionService();

            service.ResolveRescue(
                state,
                20);

            NpcInteractionResult secondResult =
                service.ResolveRescue(
                    state,
                    20);

            Assert.That(
                state.Affinity,
                Is.EqualTo(50));

            Assert.That(
                secondResult.ResultType,
                Is.EqualTo(
                    NpcInteractionResultType.ContinueInteraction));
        }

        [Test]
        public void ResolveAttack_BattleCapableNpc_SetsHostileAndReturnsStartBattle()
        {
            NpcDefinition definition =
                CreateBattleCapableNpc();

            try
            {
                NpcRelationshipState state =
                    new NpcRelationshipState(
                        definition.Id,
                        0,
                        false);

                NpcInteractionResult result =
                    new NpcInteractionService().ResolveAttack(
                        definition,
                        state);

                Assert.That(
                    result.ResultType,
                    Is.EqualTo(
                        NpcInteractionResultType.StartBattle));

                Assert.That(
                    state.IsHostile,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void ResolveAttack_CannotBattleNpc_DoesNotSetHostile()
        {
            NpcDefinition definition =
                ScriptableObject.CreateInstance<NpcDefinition>();

            try
            {
                definition.ConfigureRuntime(
                    "NPC_PEACEFUL_TEST",
                    "평화주의자",
                    NpcServiceType.None,
                    NpcHostilityMode.Never,
                    0);

                NpcRelationshipState state =
                    new NpcRelationshipState(
                        definition.Id,
                        0,
                        false);

                NpcInteractionResult result =
                    new NpcInteractionService().ResolveAttack(
                        definition,
                        state);

                Assert.That(
                    result.ResultType,
                    Is.EqualTo(
                        NpcInteractionResultType.ContinueInteraction));

                Assert.That(
                    state.IsHostile,
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }
    }
}
