using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class NpcInteractionServiceTests
    {
        [Test]
        public void Service_서비스보유Npc면OpenService를반환한다()
        {
            NpcDefinition definition =
                CreateMerchant();

            try
            {
                NpcRelationshipState state =
                    new NpcRelationshipState(
                        definition.Id,
                        definition.InitialAffinity,
                        definition.StartsHostile);

                NpcInteractionResult result =
                    new NpcInteractionService().Resolve(
                        definition,
                        state,
                        NpcInteractionCommand.Service);

                Assert.AreEqual(
                    NpcInteractionResultType.OpenService,
                    result.ResultType);
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void Leave_탐험복귀결과를반환한다()
        {
            NpcDefinition definition =
                CreateMerchant();

            try
            {
                NpcRelationshipState state =
                    new NpcRelationshipState(
                        definition.Id,
                        0,
                        false);

                NpcInteractionResult result =
                    new NpcInteractionService().Resolve(
                        definition,
                        state,
                        NpcInteractionCommand.Leave);

                Assert.AreEqual(
                    NpcInteractionResultType.ReturnToExploration,
                    result.ResultType);
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        private static NpcDefinition CreateMerchant()
        {
            NpcDefinition definition =
                ScriptableObject.CreateInstance<NpcDefinition>();

            definition.ConfigureRuntime(
                "NPC_MERCHANT_TEST",
                "상인",
                NpcServiceType.Trade,
                NpcHostilityMode.CanBecomeHostile,
                0);

            return definition;
        }
    }
}
