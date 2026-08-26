using System;
using System.IO;
using System.Text;
using ProjectDelta.Data;
using UnityEditor;
using UnityEngine;

namespace ProjectDelta.Editor
{
    public static class Day79PlayerGrowthInstaller
    {
        private const string GrowthFolderPath =
            "Assets/ProjectDelta/Resources";

        private const string GrowthAssetPath =
            GrowthFolderPath
            + "/PlayerGrowthDefinition.asset";

        private const string EncounterControllerPath =
            "Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs";

        private const string DungeonSaveMapperPath =
            "Assets/ProjectDelta/Scripts/Data/DungeonSaveMapper.cs";

        private static readonly int[] ExperienceTable =
        {
            100,
            150,
            220,
            300,
            400,
            520,
            660,
            820,
            1000
        };

        [MenuItem("Project Delta/79일차/79일차 경험치·레벨업 적용")]
        public static void Install()
        {
            EnsureFolder(
                GrowthFolderPath);

            CreateOrUpdateGrowthDefinition();
            ApplyExperienceRewardsToMonsters();

            bool encounterChanged =
                PatchEncounterController();

            bool mapperChanged =
                PatchDungeonSaveMapper();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Project Delta] 79일차 적용 완료 / "
                + "몬스터 EXP + Lv.10 성장표 + 승리 성장 + 성장 저장·복원"
                + (encounterChanged || mapperChanged
                    ? " / 소스 변경으로 Unity가 다시 컴파일합니다."
                    : " / 소스 패치는 이미 적용되어 있습니다."));
        }

        private static void CreateOrUpdateGrowthDefinition()
        {
            PlayerGrowthDefinition definition =
                AssetDatabase.LoadAssetAtPath<PlayerGrowthDefinition>(
                    GrowthAssetPath);

            if (definition == null)
            {
                definition =
                    ScriptableObject.CreateInstance<PlayerGrowthDefinition>();

                AssetDatabase.CreateAsset(
                    definition,
                    GrowthAssetPath);
            }

            SerializedObject serialized =
                new SerializedObject(
                    definition);

            SerializedProperty maxLevel =
                serialized.FindProperty(
                    "maxLevel");

            SerializedProperty statPoints =
                serialized.FindProperty(
                    "statPointsPerLevel");

            SerializedProperty experienceTable =
                serialized.FindProperty(
                    "experienceToNextLevel");

            if (maxLevel == null
                || statPoints == null
                || experienceTable == null)
            {
                throw new InvalidOperationException(
                    "PlayerGrowthDefinition 직렬화 필드를 찾지 못했습니다.");
            }

            maxLevel.intValue =
                PlayerGrowthDefinition.DefaultMaxLevel;

            statPoints.intValue =
                PlayerGrowthDefinition.DefaultStatPointsPerLevel;

            experienceTable.arraySize =
                ExperienceTable.Length;

            for (int index = 0;
                 index < ExperienceTable.Length;
                 index++)
            {
                experienceTable
                    .GetArrayElementAtIndex(
                        index)
                    .intValue =
                    ExperienceTable[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(
                definition);
        }

        private static void ApplyExperienceRewardsToMonsters()
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    "t:MonsterDefinition",
                    new[]
                    {
                        "Assets/ProjectDelta"
                    });

            int updatedCount =
                0;

            for (int index = 0;
                 index < guids.Length;
                 index++)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        guids[index]);

                MonsterDefinition monster =
                    AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
                        path);

                if (monster == null)
                {
                    continue;
                }

                SerializedObject serialized =
                    new SerializedObject(
                        monster);

                SerializedProperty reward =
                    serialized.FindProperty(
                        "experienceReward");

                if (reward == null)
                {
                    throw new InvalidOperationException(
                        $"MonsterDefinition.experienceReward를 찾지 못했습니다: {path}");
                }

                reward.intValue =
                    GetDefaultReward(
                        monster.Rarity);

                serialized.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(
                    monster);

                updatedCount++;
            }

            Debug.Log(
                $"[Project Delta] 79일차 Monster EXP 적용 / {updatedCount}개");
        }

        private static int GetDefaultReward(
            MonsterRarity rarity)
        {
            switch (rarity)
            {
                case MonsterRarity.Rare:
                    return 50;

                case MonsterRarity.Boss:
                    return 120;

                default:
                    return 20;
            }
        }

        private static bool PatchEncounterController()
        {
            if (!File.Exists(
                    EncounterControllerPath))
            {
                throw new FileNotFoundException(
                    "ExplorationMonsterEncounterController.cs를 찾지 못했습니다.",
                    EncounterControllerPath);
            }

            string source =
                File.ReadAllText(
                    EncounterControllerPath)
                    .Replace(
                        "\r\n",
                        "\n");

            bool changed =
                false;

            if (!source.Contains(
                    "public BattleGrowthResult LastBattleGrowthResult"))
            {
                const string propertyMarker = @"        public bool IsBattleRewardPending =>
            pendingVictoryEncounterResult != null
            && BattleRewardState.IsPending; // 72일차 보상 선택 대기 여부
";

                const string propertyReplacement = @"        // 79일차: 가장 최근 승리에서 적용된 경험치·레벨업 결과.
        // 81일차 정식 보상 화면이 이 값을 그대로 표시할 수 있도록 보존한다.
        public BattleGrowthResult LastBattleGrowthResult { get; private set; }

        public bool IsBattleRewardPending =>
            pendingVictoryEncounterResult != null
            && BattleRewardState.IsPending; // 72일차 보상 선택 대기 여부
";

                source =
                    ReplaceRequired(
                        source,
                        propertyMarker,
                        propertyReplacement,
                        "성장 결과 Property");
                changed =
                    true;
            }

            if (!source.Contains(
                    "LastBattleGrowthResult = null; // 79일차 성장 결과 정리"))
            {
                const string disableMarker = @"            BattleRewardState.Clear(); // 72일차 보상 상태 정리
            wasMoving = false;
";

                const string disableReplacement = @"            BattleRewardState.Clear(); // 72일차 보상 상태 정리
            LastBattleGrowthResult = null; // 79일차 성장 결과 정리
            wasMoving = false;
";

                source =
                    ReplaceRequired(
                        source,
                        disableMarker,
                        disableReplacement,
                        "OnDisable 성장 결과 정리");
                changed =
                    true;
            }

            if (!source.Contains(
                    "LastBattleGrowthResult = null; // 79일차 이전 전투 성장 결과 초기화"))
            {
                const string beginMarker = @"            BattleRewardState.Clear(); // 72일차 이전 보상 상태 초기화
            pendingVictoryEncounterResult = null; // 72일차 이전 보상 결과 초기화
            BattleDefeatService.BeginBattle(); // 70일차 패배 추적 정보 초기화
";

                const string beginReplacement = @"            BattleRewardState.Clear(); // 72일차 이전 보상 상태 초기화
            pendingVictoryEncounterResult = null; // 72일차 이전 보상 결과 초기화
            LastBattleGrowthResult = null; // 79일차 이전 전투 성장 결과 초기화
            BattleDefeatService.BeginBattle(); // 70일차 패배 추적 정보 초기화
";

                source =
                    ReplaceRequired(
                        source,
                        beginMarker,
                        beginReplacement,
                        "전투 시작 성장 결과 초기화");
                changed =
                    true;
            }

            if (!source.Contains(
                    "ApplyVictoryGrowth(); // 79일차 경험치·레벨업"))
            {
                const string victoryMarker = @"            if (outcome == BattleOutcome.Victory)
            {
                if (!EncounterResultResolver.TryCreateTestResult(
";

                const string victoryReplacement = @"            if (outcome == BattleOutcome.Victory)
            {
                ApplyVictoryGrowth(); // 79일차 경험치·레벨업

                if (!EncounterResultResolver.TryCreateTestResult(
";

                source =
                    ReplaceRequired(
                        source,
                        victoryMarker,
                        victoryReplacement,
                        "승리 성장 연결");
                changed =
                    true;
            }

            if (!source.Contains(
                    "private void ApplyVictoryGrowth()"))
            {
                const string helperMarker = @"        private void FinalizeActiveEncounter(
";

                const string helperCode = @"        // 79일차: 승리가 확정된 BattleContext의 실제 Enemy 구성 전체를 경험치로 환산한다.
        // FinishBattle()이 성공한 뒤 한 번만 호출되므로 보상 선택 버튼을 여러 번 눌러도 중복 지급되지 않는다.
        private void ApplyVictoryGrowth()
        {
            if (RunContext.Current == null
                || battleSession.Context == null
                || battleSession.Context.Enemies == null)
            {
                LastBattleGrowthResult =
                    null;

                return;
            }

            List<MonsterDefinition> defeatedMonsters =
                new List<MonsterDefinition>();

            foreach (BattleParticipant enemy
                     in battleSession.Context.Enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                MonsterDefinition definition =
                    ResolveMonsterDefinition(
                        enemy.DefinitionId);

                if (definition != null)
                {
                    defeatedMonsters.Add(
                        definition);
                }
            }

            PlayerGrowthDefinition growthDefinition =
                Resources.Load<PlayerGrowthDefinition>(
                    ""PlayerGrowthDefinition"");

            bool createdRuntimeFallback =
                false;

            if (growthDefinition == null)
            {
                growthDefinition =
                    PlayerGrowthDefinition.CreateDefaultRuntime();

                createdRuntimeFallback =
                    true;
            }

            LastBattleGrowthResult =
                PlayerGrowthService.ApplyBattleExperience(
                    RunContext.Current.Player,
                    defeatedMonsters,
                    growthDefinition);

            Debug.Log(
                $""[Project Delta] 79일차 Battle Growth / EXP +{LastBattleGrowthResult.EarnedExperience} / ""
                + $""Lv.{LastBattleGrowthResult.PreviousLevel} → Lv.{LastBattleGrowthResult.CurrentLevel} / ""
                + $""Stat Point +{LastBattleGrowthResult.GainedStatPoints}"",
                this);

            if (createdRuntimeFallback)
            {
                Destroy(
                    growthDefinition);
            }
        }

";

                source =
                    ReplaceRequired(
                        source,
                        helperMarker,
                        helperCode
                        + helperMarker,
                        "승리 성장 Helper");
                changed =
                    true;
            }

            if (changed)
            {
                File.WriteAllText(
                    EncounterControllerPath,
                    source,
                    new UTF8Encoding(
                        false));

                AssetDatabase.ImportAsset(
                    EncounterControllerPath,
                    ImportAssetOptions.ForceUpdate);
            }

            return changed;
        }

        private static bool PatchDungeonSaveMapper()
        {
            if (!File.Exists(
                    DungeonSaveMapperPath))
            {
                throw new FileNotFoundException(
                    "DungeonSaveMapper.cs를 찾지 못했습니다.",
                    DungeonSaveMapperPath);
            }

            string source =
                File.ReadAllText(
                    DungeonSaveMapperPath)
                    .Replace(
                        "\r\n",
                        "\n");

            bool changed =
                false;

            if (!source.Contains(
                    "data.PlayerStats.Level ="))
            {
                const string saveMarker = @"            data.BasicInfo.DungeonSeed =
                context.Dungeon.CurrentDungeonSeed;
";

                const string saveReplacement = @"            data.BasicInfo.DungeonSeed =
                context.Dungeon.CurrentDungeonSeed;

            // 79일차: 런타임 성장 상태를 기존 RunData.PlayerStats에 저장한다.
            data.PlayerStats.Level =
                Math.Max(
                    1,
                    Math.Min(
                        PlayerGrowthDefinition.DefaultMaxLevel,
                        context.Player.Level));

            data.PlayerStats.Experience =
                Math.Max(
                    0,
                    context.Player.Experience);

            data.PlayerStats.UnspentStatPoints =
                Math.Max(
                    0,
                    context.Player.UnusedStatPoints);
";

                source =
                    ReplaceRequired(
                        source,
                        saveMarker,
                        saveReplacement,
                        "성장 상태 저장");
                changed =
                    true;
            }

            if (!source.Contains(
                    "context.Player.UnusedStatPoints ="))
            {
                const string restoreMarker = @"            context.Dungeon.SetFloor(savedFloor);
";

                const string restoreReplacement = @"            context.Dungeon.SetFloor(savedFloor);

            // 79일차: 구버전 저장의 Level=0도 Lv.1로 안전하게 복원한다.
            if (savedRun.PlayerStats != null)
            {
                context.Player.Level =
                    Math.Max(
                        1,
                        Math.Min(
                            PlayerGrowthDefinition.DefaultMaxLevel,
                            savedRun.PlayerStats.Level));

                context.Player.Experience =
                    Math.Max(
                        0,
                        savedRun.PlayerStats.Experience);

                context.Player.UnusedStatPoints =
                    Math.Max(
                        0,
                        savedRun.PlayerStats.UnspentStatPoints);
            }
";

                source =
                    ReplaceRequired(
                        source,
                        restoreMarker,
                        restoreReplacement,
                        "성장 상태 복원");
                changed =
                    true;
            }

            if (changed)
            {
                File.WriteAllText(
                    DungeonSaveMapperPath,
                    source,
                    new UTF8Encoding(
                        false));

                AssetDatabase.ImportAsset(
                    DungeonSaveMapperPath,
                    ImportAssetOptions.ForceUpdate);
            }

            return changed;
        }

        private static string ReplaceRequired(
            string source,
            string oldText,
            string newText,
            string operationName)
        {
            if (!source.Contains(
                    oldText))
            {
                throw new InvalidOperationException(
                    $"79일차 소스 패치 실패: {operationName} 위치를 찾지 못했습니다. 최신 78일차 main 상태인지 확인해 주세요.");
            }

            return source.Replace(
                oldText,
                newText);
        }

        private static void EnsureFolder(
            string folderPath)
        {
            string[] parts =
                folderPath.Split(
                    '/');

            string current =
                parts[0];

            for (int index = 1;
                 index < parts.Length;
                 index++)
            {
                string next =
                    current
                    + "/"
                    + parts[index];

                if (!AssetDatabase.IsValidFolder(
                        next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        parts[index]);
                }

                current =
                    next;
            }
        }
    }
}
