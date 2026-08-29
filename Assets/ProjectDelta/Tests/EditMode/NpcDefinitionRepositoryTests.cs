using NUnit.Framework;
using ProjectDelta.Data;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class NpcDefinitionRepositoryTests
    {
        [Test]
        public void ConfigureRuntime_Npc기본데이터를구성한다()
        {
            NpcDefinition definition =
                ScriptableObject.CreateInstance<NpcDefinition>();

            try
            {
                definition.ConfigureRuntime(
                    "NPC_MERCHANT_TEST",
                    "상인",
                    NpcServiceType.Trade,
                    NpcHostilityMode.CanBecomeHostile,
                    20);

                Assert.AreEqual(
                    "NPC_MERCHANT_TEST",
                    definition.Id);

                Assert.AreEqual(
                    "상인",
                    definition.DisplayName);

                Assert.AreEqual(
                    NpcServiceType.Trade,
                    definition.ServiceTypes);

                Assert.IsTrue(
                    definition.CanBattle);

                Assert.AreEqual(
                    20,
                    definition.InitialAffinity);
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void DataRepository_Npc를영구Id로조회한다()
        {
            NpcDefinition definition =
                ScriptableObject.CreateInstance<NpcDefinition>();

            try
            {
                definition.ConfigureRuntime(
                    "NPC_MERCHANT_TEST",
                    "상인",
                    NpcServiceType.Trade,
                    NpcHostilityMode.CanBecomeHostile,
                    0);

                DataRepository repository =
                    new DataRepository();

                repository.Npcs.Load(
                    new[]
                    {
                        definition
                    });

                Assert.AreSame(
                    definition,
                    repository.GetNpc(
                        "NPC_MERCHANT_TEST"));
            }
            finally
            {
                Object.DestroyImmediate(
                    definition);
            }
        }
    }
}
