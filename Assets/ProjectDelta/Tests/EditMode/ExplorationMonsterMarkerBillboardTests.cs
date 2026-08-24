using NUnit.Framework;
using ProjectDelta.Domain;
using ProjectDelta.Presentation;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class ExplorationMonsterMarkerBillboardTests
    {
        [Test]
        public void Configure_CreatesBillboardVisualAndKeepsFallbackWhenSpriteIsMissing()
        {
            GameObject monsterObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule);

            try
            {
                ExplorationMonsterMarker marker =
                    monsterObject.AddComponent<ExplorationMonsterMarker>();

                marker.Configure(
                    "ROOM_A",
                    "MON_DOES_NOT_EXIST",
                    GridPosition.Zero);

                Assert.IsNotNull(
                    monsterObject.transform.Find(
                        ExplorationMonsterMarker.BillboardObjectName));

                Assert.IsFalse(
                    marker.HasBillboardSprite);

                Assert.IsTrue(
                    monsterObject.GetComponent<Renderer>().enabled);
            }
            finally
            {
                Object.DestroyImmediate(
                    monsterObject);
            }
        }
    }
}
