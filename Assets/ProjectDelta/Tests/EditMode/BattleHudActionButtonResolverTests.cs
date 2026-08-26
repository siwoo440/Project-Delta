using NUnit.Framework;
using ProjectDelta.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class BattleHudActionButtonResolverTests
    {
        [Test]
        public void ResolveFleeButton_FindsButtonByKoreanLabel()
        {
            GameObject firstObject =
                CreateButton(
                    "ActionButton_01",
                    "아이템");

            GameObject fleeObject =
                CreateButton(
                    "ActionButton_02",
                    "도주");

            GameObject thirdObject =
                CreateButton(
                    "ActionButton_03",
                    "유혹");

            try
            {
                Button resolved =
                    BattleHudActionButtonResolver.ResolveFleeButton(
                        null,
                        new[]
                        {
                            firstObject.GetComponent<Button>(),
                            fleeObject.GetComponent<Button>(),
                            thirdObject.GetComponent<Button>()
                        });

                Assert.That(
                    resolved,
                    Is.SameAs(
                        fleeObject.GetComponent<Button>()));
            }
            finally
            {
                Object.DestroyImmediate(
                    firstObject);

                Object.DestroyImmediate(
                    fleeObject);

                Object.DestroyImmediate(
                    thirdObject);
            }
        }

        [Test]
        public void ResolveFleeButton_UsesThirdActionButtonAsLegacyFallback()
        {
            GameObject firstObject =
                CreateButton(
                    "ActionButton_A",
                    "A");

            GameObject secondObject =
                CreateButton(
                    "ActionButton_B",
                    "B");

            GameObject thirdObject =
                CreateButton(
                    "ActionButton_C",
                    "C");

            try
            {
                Button resolved =
                    BattleHudActionButtonResolver.ResolveFleeButton(
                        null,
                        new[]
                        {
                            firstObject.GetComponent<Button>(),
                            secondObject.GetComponent<Button>(),
                            thirdObject.GetComponent<Button>()
                        });

                Assert.That(
                    resolved,
                    Is.SameAs(
                        thirdObject.GetComponent<Button>()));
            }
            finally
            {
                Object.DestroyImmediate(
                    firstObject);

                Object.DestroyImmediate(
                    secondObject);

                Object.DestroyImmediate(
                    thirdObject);
            }
        }

        [Test]
        public void ResolveFleeButton_PrefersExplicitReference()
        {
            GameObject explicitObject =
                CreateButton(
                    "ExplicitFlee",
                    "도주");

            GameObject fallbackObject =
                CreateButton(
                    "Fallback",
                    "도주");

            try
            {
                Button explicitButton =
                    explicitObject.GetComponent<Button>();

                Button resolved =
                    BattleHudActionButtonResolver.ResolveFleeButton(
                        explicitButton,
                        new[]
                        {
                            fallbackObject.GetComponent<Button>()
                        });

                Assert.That(
                    resolved,
                    Is.SameAs(
                        explicitButton));
            }
            finally
            {
                Object.DestroyImmediate(
                    explicitObject);

                Object.DestroyImmediate(
                    fallbackObject);
            }
        }

        private static GameObject CreateButton(
            string objectName,
            string label)
        {
            GameObject buttonObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));

            GameObject labelObject =
                new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(Text));

            labelObject.transform.SetParent(
                buttonObject.transform,
                false);

            labelObject.GetComponent<Text>().text =
                label;

            return buttonObject;
        }
    }
}
