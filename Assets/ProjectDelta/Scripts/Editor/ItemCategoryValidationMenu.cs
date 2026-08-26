using System.Collections.Generic;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEditor;
using UnityEngine;

namespace ProjectDelta.Editor
{
    // ItemDefinition 에셋의 분류 누락과 대표적인 Stack 설정 실수를 찾는다.
    public static class ItemCategoryValidationMenu
    {
        [MenuItem(
            "Project Delta/91일차/아이템 분류 검증")]
        private static void ValidateAllDefinitions()
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    "t:ItemDefinition");

            List<string> uncategorizedPaths =
                new List<string>();

            List<string> stackWarnings =
                new List<string>();

            for (int index = 0;
                 index < guids.Length;
                 index++)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        guids[index]);

                ItemDefinition definition =
                    AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                        path);

                if (definition == null)
                {
                    continue;
                }

                if (definition.Category
                    == ItemCategory.Uncategorized)
                {
                    uncategorizedPaths.Add(
                        path);
                }

                if ((definition.Category
                        == ItemCategory.Equipment
                    || definition.Category
                        == ItemCategory.Relic)
                    && definition.MaxStackSize > 1)
                {
                    stackWarnings.Add(
                        $"{path} : {definition.Category}, MaxStackSize={definition.MaxStackSize}");
                }
            }

            for (int index = 0;
                 index < uncategorizedPaths.Count;
                 index++)
            {
                Debug.LogWarning(
                    $"[Project Delta][91일차] Category 미지정: {uncategorizedPaths[index]}");
            }

            for (int index = 0;
                 index < stackWarnings.Count;
                 index++)
            {
                Debug.LogWarning(
                    $"[Project Delta][91일차] Stack 설정 확인 필요: {stackWarnings[index]}");
            }

            if (uncategorizedPaths.Count == 0
                && stackWarnings.Count == 0)
            {
                Debug.Log(
                    $"[Project Delta][91일차] ItemDefinition {guids.Length}개 분류 검증 완료. 경고 없음.");

                return;
            }

            Debug.Log(
                $"[Project Delta][91일차] ItemDefinition {guids.Length}개 검사 / 미분류 {uncategorizedPaths.Count}개 / Stack 확인 {stackWarnings.Count}개.");
        }
    }
}
