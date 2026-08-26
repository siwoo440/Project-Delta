using System;
using System.Collections.Generic;
using ProjectDelta.Data;
using UnityEditor;
using UnityEngine;

namespace ProjectDelta.Editor
{
    // 87일차: Balance Editor에서 공통으로 사용하는 검색·경고·성장표 계산 기능을 담당한다.
    public static class BalanceEditorUtility
    {
        // 검색어가 비어 있으면 모든 에셋을 표시하고, 있으면 에셋명·ID·표시명을 대소문자 구분 없이 검색한다.
        public static bool MatchesSearch(
            UnityEngine.Object asset,
            string searchText)
        {
            if (asset == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    searchText))
            {
                return true;
            }

            string searchableText =
                BuildSearchText(
                    asset);

            return searchableText.IndexOf(
                    searchText.Trim(),
                    StringComparison.OrdinalIgnoreCase)
                >= 0;
        }

        // 왼쪽 목록에서 보기 좋은 "ID | 표시명" 형태의 라벨을 만든다.
        public static string GetDisplayLabel(
            UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return "(없음)";
            }

            SerializedObject serializedObject =
                new SerializedObject(
                    asset);

            string id =
                ReadString(
                    serializedObject,
                    "id");

            string displayName =
                ReadString(
                    serializedObject,
                    "displayName");

            if (!string.IsNullOrWhiteSpace(
                    id)
                && !string.IsNullOrWhiteSpace(
                    displayName))
            {
                return $"{id}  |  {displayName}";
            }

            if (!string.IsNullOrWhiteSpace(
                    id))
            {
                return id;
            }

            if (!string.IsNullOrWhiteSpace(
                    displayName))
            {
                return displayName;
            }

            return asset.name;
        }

        // 현재 원본 ScriptableObject의 직렬화 값을 읽어 명백하게 잘못된 밸런스 값만 경고한다.
        public static IReadOnlyList<string> GetWarnings(
            UnityEngine.Object asset)
        {
            List<string> warnings =
                new List<string>();

            if (asset == null)
            {
                return warnings;
            }

            SerializedObject serializedObject =
                new SerializedObject(
                    asset);

            if (asset is MonsterDefinition)
            {
                AddMonsterWarnings(
                    serializedObject,
                    warnings);
            }
            else if (asset is SkillDefinition)
            {
                AddSkillWarnings(
                    serializedObject,
                    warnings);
            }
            else if (asset is StatusEffectDefinition)
            {
                AddStatusWarnings(
                    serializedObject,
                    warnings);
            }
            else if (asset is PlayerGrowthDefinition)
            {
                AddGrowthWarnings(
                    serializedObject,
                    warnings);
            }
            else if (asset is MonsterDropTable)
            {
                AddDropWarnings(
                    serializedObject,
                    warnings);
            }

            return warnings;
        }

        // Growth 탭 하단에서 레벨별 필요 경험치와 누적 경험치를 표시하기 위한 행을 만든다.
        public static IReadOnlyList<BalanceGrowthRow> BuildGrowthRows(
            PlayerGrowthDefinition definition)
        {
            List<BalanceGrowthRow> rows =
                new List<BalanceGrowthRow>();

            if (definition == null)
            {
                return rows;
            }

            SerializedObject serializedObject =
                new SerializedObject(
                    definition);

            SerializedProperty maxLevelProperty =
                serializedObject.FindProperty(
                    "maxLevel");

            SerializedProperty experienceProperty =
                serializedObject.FindProperty(
                    "experienceToNextLevel");

            int maxLevel =
                maxLevelProperty != null
                    ? Math.Max(
                        1,
                        maxLevelProperty.intValue)
                    : definition.MaxLevel;

            int cumulativeExperience =
                0;

            for (int currentLevel = 1;
                 currentLevel < maxLevel;
                 currentLevel++)
            {
                int arrayIndex =
                    currentLevel - 1;

                int requiredExperience =
                    0;

                if (experienceProperty != null
                    && experienceProperty.isArray
                    && arrayIndex >= 0
                    && arrayIndex < experienceProperty.arraySize)
                {
                    requiredExperience =
                        experienceProperty
                            .GetArrayElementAtIndex(
                                arrayIndex)
                            .intValue;
                }

                cumulativeExperience +=
                    Math.Max(
                        0,
                        requiredExperience);

                rows.Add(
                    new BalanceGrowthRow(
                        currentLevel,
                        currentLevel + 1,
                        requiredExperience,
                        cumulativeExperience));
            }

            return rows;
        }

        // 에셋명과 공통 직렬화 필드를 검색 문자열로 합친다.
        private static string BuildSearchText(
            UnityEngine.Object asset)
        {
            SerializedObject serializedObject =
                new SerializedObject(
                    asset);

            string id =
                ReadString(
                    serializedObject,
                    "id");

            string displayName =
                ReadString(
                    serializedObject,
                    "displayName");

            return string.Join(
                "\n",
                asset.name ?? string.Empty,
                id,
                displayName,
                asset.GetType().Name);
        }

        // 문자열 SerializedProperty가 없더라도 안전하게 빈 문자열을 반환한다.
        private static string ReadString(
            SerializedObject serializedObject,
            string propertyName)
        {
            SerializedProperty property =
                serializedObject.FindProperty(
                    propertyName);

            return property != null
                && property.propertyType
                    == SerializedPropertyType.String
                    ? property.stringValue
                    : string.Empty;
        }

        // 정수 SerializedProperty를 읽되 필드가 없으면 지정한 기본값을 사용한다.
        private static int ReadInt(
            SerializedObject serializedObject,
            string propertyName,
            int fallback = 0)
        {
            SerializedProperty property =
                serializedObject.FindProperty(
                    propertyName);

            return property != null
                && property.propertyType
                    == SerializedPropertyType.Integer
                    ? property.intValue
                    : fallback;
        }

        // 몬스터는 최대 체력·마나와 경험치 보상처럼 명확한 음수/0 오류만 검사한다.
        private static void AddMonsterWarnings(
            SerializedObject serializedObject,
            List<string> warnings)
        {
            if (ReadInt(
                    serializedObject,
                    "maxHp")
                <= 0)
            {
                warnings.Add(
                    "최대 HP는 1 이상이어야 합니다.");
            }

            if (ReadInt(
                    serializedObject,
                    "maxMana")
                < 0)
            {
                warnings.Add(
                    "최대 MP는 0 이상이어야 합니다.");
            }

            if (ReadInt(
                    serializedObject,
                    "experienceReward")
                < 0)
            {
                warnings.Add(
                    "경험치 보상은 0 이상이어야 합니다.");
            }
        }

        // 스킬은 Inspector의 Min/Range 제약과 전투 의미상 반드시 필요한 수치를 검사한다.
        private static void AddSkillWarnings(
            SerializedObject serializedObject,
            List<string> warnings)
        {
            int manaCost =
                ReadInt(
                    serializedObject,
                    "manaCost");

            int staminaCost =
                ReadInt(
                    serializedObject,
                    "staminaCost");

            int damageMultiplier =
                ReadInt(
                    serializedObject,
                    "damageMultiplierPercent");

            int criticalChance =
                ReadInt(
                    serializedObject,
                    "criticalChancePercent");

            int criticalMultiplier =
                ReadInt(
                    serializedObject,
                    "criticalMultiplierPercent");

            int statusChance =
                ReadInt(
                    serializedObject,
                    "statusEffectBaseChancePercent");

            int statusDuration =
                ReadInt(
                    serializedObject,
                    "statusEffectDurationRounds",
                    1);

            SerializedProperty grantedStatusProperty =
                serializedObject.FindProperty(
                    "grantedStatusEffect");

            if (manaCost < 0)
            {
                warnings.Add(
                    "마나 비용은 0 이상이어야 합니다.");
            }

            if (staminaCost < 0)
            {
                warnings.Add(
                    "정력 비용은 0 이상이어야 합니다.");
            }

            if (damageMultiplier <= 0)
            {
                warnings.Add(
                    "피해 배율은 1% 이상을 권장합니다.");
            }

            if (criticalChance < 0
                || criticalChance > 100)
            {
                warnings.Add(
                    "치명타 확률은 0~100 범위여야 합니다.");
            }

            if (criticalChance > 0
                && criticalMultiplier <= 0)
            {
                warnings.Add(
                    "치명타 확률이 있다면 치명타 배율도 1% 이상이어야 합니다.");
            }

            if (statusChance < 0
                || statusChance > 100)
            {
                warnings.Add(
                    "상태이상 기본 확률은 0~100 범위여야 합니다.");
            }

            if (grantedStatusProperty != null
                && grantedStatusProperty.objectReferenceValue != null
                && statusDuration < 1)
            {
                warnings.Add(
                    "상태이상을 부여하는 스킬의 지속 라운드는 1 이상이어야 합니다.");
            }
        }

        // 상태이상은 중첩 수와 라운드 종료 절대값을 검사한다.
        private static void AddStatusWarnings(
            SerializedObject serializedObject,
            List<string> warnings)
        {
            if (ReadInt(
                    serializedObject,
                    "maxStack",
                    1)
                < 1)
            {
                warnings.Add(
                    "최대 중첩 수는 1 이상이어야 합니다.");
            }

            if (ReadInt(
                    serializedObject,
                    "roundEndValue")
                < 0)
            {
                warnings.Add(
                    "라운드 종료 적용 수치는 절대값이므로 0 이상이어야 합니다.");
            }
        }

        // 성장 데이터는 레벨 수와 경험치 배열 길이, 각 필요 경험치 값을 함께 검사한다.
        private static void AddGrowthWarnings(
            SerializedObject serializedObject,
            List<string> warnings)
        {
            int maxLevel =
                ReadInt(
                    serializedObject,
                    "maxLevel",
                    1);

            int statPointsPerLevel =
                ReadInt(
                    serializedObject,
                    "statPointsPerLevel");

            SerializedProperty experienceProperty =
                serializedObject.FindProperty(
                    "experienceToNextLevel");

            if (maxLevel < 1)
            {
                warnings.Add(
                    "최대 레벨은 1 이상이어야 합니다.");
            }

            if (statPointsPerLevel < 0)
            {
                warnings.Add(
                    "레벨당 스탯 포인트는 0 이상이어야 합니다.");
            }

            int expectedExperienceCount =
                Math.Max(
                    0,
                    maxLevel - 1);

            int actualExperienceCount =
                experienceProperty != null
                && experienceProperty.isArray
                    ? experienceProperty.arraySize
                    : 0;

            if (actualExperienceCount
                != expectedExperienceCount)
            {
                warnings.Add(
                    $"필요 경험치 배열은 최대 레벨 기준 {expectedExperienceCount}개여야 합니다. 현재 {actualExperienceCount}개입니다.");
            }

            if (experienceProperty == null
                || !experienceProperty.isArray)
            {
                return;
            }

            for (int index = 0;
                 index < experienceProperty.arraySize;
                 index++)
            {
                int requiredExperience =
                    experienceProperty
                        .GetArrayElementAtIndex(
                            index)
                        .intValue;

                if (requiredExperience <= 0)
                {
                    warnings.Add(
                        $"Lv.{index + 1} → Lv.{index + 2} 필요 경험치는 1 이상이어야 합니다.");

                    break;
                }
            }
        }

        // 드롭 데이터는 골드 범위와 각 아이템의 확률·수량 범위를 검사한다.
        private static void AddDropWarnings(
            SerializedObject serializedObject,
            List<string> warnings)
        {
            int minimumGold =
                ReadInt(
                    serializedObject,
                    "minimumGold");

            int maximumGold =
                ReadInt(
                    serializedObject,
                    "maximumGold");

            if (minimumGold < 0
                || maximumGold < 0)
            {
                warnings.Add(
                    "드롭 골드는 0 이상이어야 합니다.");
            }

            if (maximumGold < minimumGold)
            {
                warnings.Add(
                    "최대 드롭 골드는 최소 드롭 골드 이상이어야 합니다.");
            }

            SerializedProperty itemDropsProperty =
                serializedObject.FindProperty(
                    "itemDrops");

            if (itemDropsProperty == null
                || !itemDropsProperty.isArray)
            {
                return;
            }

            for (int index = 0;
                 index < itemDropsProperty.arraySize;
                 index++)
            {
                SerializedProperty entry =
                    itemDropsProperty.GetArrayElementAtIndex(
                        index);

                SerializedProperty item =
                    entry.FindPropertyRelative(
                        "item");

                SerializedProperty chance =
                    entry.FindPropertyRelative(
                        "chanceBasisPoints");

                SerializedProperty minimumQuantity =
                    entry.FindPropertyRelative(
                        "minimumQuantity");

                SerializedProperty maximumQuantity =
                    entry.FindPropertyRelative(
                        "maximumQuantity");

                string prefix =
                    $"아이템 드롭 {index + 1}";

                if (item != null
                    && item.objectReferenceValue == null)
                {
                    warnings.Add(
                        $"{prefix}: Item이 비어 있습니다.");
                }

                if (chance != null
                    && (chance.intValue < 0
                        || chance.intValue
                            > MonsterDropEntry.MaximumChanceBasisPoints))
                {
                    warnings.Add(
                        $"{prefix}: 확률은 0~{MonsterDropEntry.MaximumChanceBasisPoints} bp 범위여야 합니다.");
                }

                if (minimumQuantity != null
                    && minimumQuantity.intValue < 1)
                {
                    warnings.Add(
                        $"{prefix}: 최소 수량은 1 이상이어야 합니다.");
                }

                if (maximumQuantity != null
                    && maximumQuantity.intValue < 1)
                {
                    warnings.Add(
                        $"{prefix}: 최대 수량은 1 이상이어야 합니다.");
                }

                if (minimumQuantity != null
                    && maximumQuantity != null
                    && maximumQuantity.intValue
                        < minimumQuantity.intValue)
                {
                    warnings.Add(
                        $"{prefix}: 최대 수량은 최소 수량 이상이어야 합니다.");
                }
            }
        }
    }

    // Growth 탭에 표시하는 계산 전용 행이며 실제 게임 데이터에는 저장하지 않는다.
    public readonly struct BalanceGrowthRow
    {
        public int FromLevel { get; }
        public int ToLevel { get; }
        public int RequiredExperience { get; }
        public int CumulativeExperience { get; }

        public BalanceGrowthRow(
            int fromLevel,
            int toLevel,
            int requiredExperience,
            int cumulativeExperience)
        {
            FromLevel =
                fromLevel;

            ToLevel =
                toLevel;

            RequiredExperience =
                requiredExperience;

            CumulativeExperience =
                cumulativeExperience;
        }
    }
}
