using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Presentation;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class BattleHudPortraitCacheTests
    {
        [Test]
        public void LoadMonsterPortrait_UsesCachedSpriteWhenDefinitionIdWasAlreadyLoaded()
        {
            GameObject gameObject =
                new GameObject(
                    "BattleHudPortraitCacheTests");

            Texture2D texture =
                new Texture2D(
                    1,
                    1);

            Sprite expectedSprite =
                Sprite.Create(
                    texture,
                    new Rect(
                        0f,
                        0f,
                        1f,
                        1f),
                    new Vector2(
                        0.5f,
                        0.5f));

            try
            {
                BattleHudController controller =
                    gameObject.AddComponent<BattleHudController>();

                FieldInfo cacheField =
                    typeof(BattleHudController).GetField(
                        "monsterPortraitCache",
                        BindingFlags.Instance
                        | BindingFlags.NonPublic);

                Assert.That(
                    cacheField,
                    Is.Not.Null);

                Dictionary<string, Sprite> cache =
                    cacheField.GetValue(
                        controller)
                    as Dictionary<string, Sprite>;

                Assert.That(
                    cache,
                    Is.Not.Null);

                cache["CACHE_ONLY_TEST"] =
                    expectedSprite;

                MethodInfo loadMethod =
                    typeof(BattleHudController).GetMethod(
                        "LoadMonsterPortrait",
                        BindingFlags.Instance
                        | BindingFlags.NonPublic);

                Assert.That(
                    loadMethod,
                    Is.Not.Null);

                Sprite actualSprite =
                    loadMethod.Invoke(
                        controller,
                        new object[]
                        {
                            "CACHE_ONLY_TEST"
                        })
                    as Sprite;

                Assert.That(
                    actualSprite,
                    Is.SameAs(
                        expectedSprite));
            }
            finally
            {
                Object.DestroyImmediate(
                    expectedSprite);

                Object.DestroyImmediate(
                    texture);

                Object.DestroyImmediate(
                    gameObject);
            }
        }
    }
}
