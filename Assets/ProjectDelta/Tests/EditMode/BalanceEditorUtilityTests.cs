using System.Collections.Generic;
using NUnit.Framework;
using ProjectDelta.Data;
using ProjectDelta.Editor;
using UnityEditor;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    // 87일차 Balance Editor의 검색·경고·성장표 계산을 EditMode에서 검증한다.
    public sealed class BalanceEditorUtilityTests
    {
        private readonly List<Object> createdObjects =
            new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0;
                 index < createdObjects.Count;
                 index++)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(
                        createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void MatchesSearch_EmptySearch_ReturnsTrue()
        {
            MonsterDefinition monster =
                CreateMonster(
                    "MON_GOBLIN",
                    "Goblin",
                    10);

            bool matched =
                BalanceEditorUtility.MatchesSearch(
                    monster,
                    string.Empty);

            Assert.That(
                matched,
                Is.True);
        }

        [Test]
        public void MatchesSearch_IdAndDisplayName_AreCaseInsensitive()
        {
            MonsterDefinition monster =
                CreateMonster(
                    "MON_GOBLIN",
                    "Forest Goblin",
                    10);

            Assert.That(
                BalanceEditorUtility.MatchesSearch(
                    monster,
                    "mon_gob"),
                Is.True);

            Assert.That(
                BalanceEditorUtility.MatchesSearch(
                    monster,
                    "FOREST"),
                Is.True);

            Assert.That(
                BalanceEditorUtility.MatchesSearch(
                    monster,
                    "slime"),
                Is.False);
        }

        [Test]
        public void GetDisplayLabel_WithIdAndDisplayName_CombinesBoth()
        {
            MonsterDefinition monster =
                CreateMonster(
                    "MON_GOBLIN",
                    "Goblin",
                    10);

            string label =
                BalanceEditorUtility.GetDisplayLabel(
                    monster);

            Assert.That(
                label,
                Does.Contain(
                    "MON_GOBLIN"));

            Assert.That(
                label,
                Does.Contain(
                    "Goblin"));
        }

        [Test]
        public void GetWarnings_MonsterWithZeroHp_ReturnsWarning()
        {
            MonsterDefinition monster =
                CreateMonster(
                    "MON_BROKEN",
                    "Broken",
                    0);

            IReadOnlyList<string> warnings =
                BalanceEditorUtility.GetWarnings(
                    monster);

            Assert.That(
                string.Join(
                    "\n",
                    warnings),
                Does.Contain(
                    "최대 HP"));
        }

        [Test]
        public void GetWarnings_DropMaximumBelowMinimum_ReturnsWarning()
        {
            MonsterDropTable dropTable =
                ScriptableObject.CreateInstance<MonsterDropTable>();

            createdObjects.Add(
                dropTable);

            SerializedObject serializedObject =
                new SerializedObject(
                    dropTable);

            serializedObject.FindProperty(
                    "minimumGold")
                .intValue =
                20;

            serializedObject.FindProperty(
                    "maximumGold")
                .intValue =
                10;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            IReadOnlyList<string> warnings =
                BalanceEditorUtility.GetWarnings(
                    dropTable);

            Assert.That(
                string.Join(
                    "\n",
                    warnings),
                Does.Contain(
                    "최대 드롭 골드"));
        }

        [Test]
        public void BuildGrowthRows_CalculatesCumulativeExperience()
        {
            PlayerGrowthDefinition growth =
                PlayerGrowthDefinition.CreateRuntime(
                    4,
                    1,
                    new[]
                    {
                        100,
                        150,
                        220
                    });

            createdObjects.Add(
                growth);

            IReadOnlyList<BalanceGrowthRow> rows =
                BalanceEditorUtility.BuildGrowthRows(
                    growth);

            Assert.That(
                rows.Count,
                Is.EqualTo(
                    3));

            Assert.That(
                rows[0].RequiredExperience,
                Is.EqualTo(
                    100));

            Assert.That(
                rows[1].CumulativeExperience,
                Is.EqualTo(
                    250));

            Assert.That(
                rows[2].CumulativeExperience,
                Is.EqualTo(
                    470));
        }

        // private 직렬화 필드를 실제 Asset과 동일한 SerializedObject 경로로 설정한다.
        private MonsterDefinition CreateMonster(
            string id,
            string displayName,
            int maxHp)
        {
            MonsterDefinition monster =
                ScriptableObject.CreateInstance<MonsterDefinition>();

            createdObjects.Add(
                monster);

            monster.name =
                id;

            SerializedObject serializedObject =
                new SerializedObject(
                    monster);

            serializedObject.FindProperty(
                    "id")
                .stringValue =
                id;

            serializedObject.FindProperty(
                    "displayName")
                .stringValue =
                displayName;

            serializedObject.FindProperty(
                    "maxHp")
                .intValue =
                maxHp;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return monster;
        }
    }
}
