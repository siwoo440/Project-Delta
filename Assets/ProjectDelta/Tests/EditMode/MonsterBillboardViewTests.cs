using NUnit.Framework;
using ProjectDelta.Presentation;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class MonsterBillboardViewTests
    {
        [Test]
        public void BuildResourcePath_UsesMonsterDefinitionId()
        {
            Assert.AreEqual(
                "MonsterSprites/MON_TEST",
                MonsterBillboardView.BuildResourcePath(
                    "MON_TEST"));
        }

        [Test]
        public void CalculateYawRotation_IgnoresCameraHeight()
        {
            Quaternion rotation =
                MonsterBillboardView.CalculateYawRotation(
                    Vector3.zero,
                    new Vector3(1f, 5f, 1f),
                    Quaternion.identity);

            Vector3 expectedForward =
                new Vector3(1f, 0f, 1f).normalized;

            Assert.That(
                Vector3.Dot(
                    rotation * Vector3.forward,
                    expectedForward),
                Is.GreaterThan(0.999f));

            Assert.That(
                Mathf.Abs(
                    (rotation * Vector3.forward).y),
                Is.LessThan(0.001f));
        }

        [Test]
        public void CalculateYawRotation_SameHorizontalPosition_KeepsFallback()
        {
            Quaternion fallback =
                Quaternion.Euler(
                    0f,
                    37f,
                    0f);

            Quaternion rotation =
                MonsterBillboardView.CalculateYawRotation(
                    new Vector3(2f, 0f, 3f),
                    new Vector3(2f, 10f, 3f),
                    fallback);

            Assert.That(
                Quaternion.Angle(
                    fallback,
                    rotation),
                Is.LessThan(0.001f));
        }
    }
}
