using System;
using System.IO;
using ProjectDelta.Data;
using UnityEditor;
using UnityEngine;

namespace ProjectDelta.EditorTools
{
    public static class Day80BattleDropInstaller
    {
        private const string ControllerPath =
            "Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs";

        private const string DataRoot =
            "Assets/ProjectDelta/Data";

        private const string DropTableFolder =
            DataRoot + "/DropTables";

        private const string ItemFolder =
            DataRoot + "/Items";

        private const string TestItemPath =
            ItemFolder + "/Day80_TestDropItem.asset";

        [MenuItem("Project Delta/80일차/80일차 전투 드롭 설치")]
        private static void Install()
        {
            EnsureFolder(
                "Assets",
                "ProjectDelta");

            EnsureFolder(
                "Assets/ProjectDelta",
                "Data");

            EnsureFolder(
                DataRoot,
                "DropTables");

            EnsureFolder(
                DataRoot,
                "Items");

            ItemDefinition testItem =
                CreateOrLoadTestItem();

            int assignedCount =
                CreateAndAssignDropTables(
                    testItem);

            bool controllerChanged =
                PatchEncounterController();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[Project Delta] 80일차 전투 드롭 설치 완료 / DropTable 연결 {assignedCount}개 / Controller 수정 {controllerChanged}");
        }

        private static bool PatchEncounterController()
        {
            if (!File.Exists(
                    ControllerPath))
            {
                throw new FileNotFoundException(
                    "80일차 설치 대상 Controller를 찾을 수 없습니다.",
                    ControllerPath);
            }

            string source =
                File.ReadAllText(
                    ControllerPath);

            bool usesWindowsLineEndings =
                source.Contains("\r\n");

            // Windows 체크아웃의 CRLF와 Git 원본의 LF를 모두 같은 앵커로 처리한다.
            source =
                source.Replace(
                    "\r\n",
                    "\n");

            bool changed =
                false;

            changed |= InsertAfterOnce(
                ref source,
                "        private readonly IRandomSource combatRng =\n            new CombatRng();",
                "\n\n        // 80일차: 전투 RNG와 분리된 골드·아이템 드롭 전용 RNG.\n        private readonly IRandomSource rewardRng =\n            new RewardRng();",
                "private readonly IRandomSource rewardRng");

            changed |= InsertAfterOnce(
                ref source,
                "        public BattleGrowthResult LastBattleGrowthResult { get; private set; }",
                "\n\n        // 80일차: 가장 최근 승리에서 한 번 판정된 골드·아이템 드롭 결과.\n        // 81일차 정식 보상 화면이 재추첨 없이 이 결과를 그대로 표시한다.\n        public BattleDropResult LastBattleDropResult { get; private set; }",
                "public BattleDropResult LastBattleDropResult");

            changed |= InsertAfterOnce(
                ref source,
                "            LastBattleGrowthResult = null; // 79일차 성장 결과 정리",
                "\n            LastBattleDropResult = null; // 80일차 드롭 결과 정리",
                "LastBattleDropResult = null; // 80일차 드롭 결과 정리");

            changed |= InsertAfterOnce(
                ref source,
                "            LastBattleGrowthResult = null; // 79일차 이전 전투 성장 결과 초기화",
                "\n            LastBattleDropResult = null; // 80일차 이전 전투 드롭 결과 초기화",
                "LastBattleDropResult = null; // 80일차 이전 전투 드롭 결과 초기화");

            changed |= InsertAfterOnce(
                ref source,
                "                ApplyVictoryGrowth(); // 79일차 경험치·레벨업",
                "\n                ApplyVictoryDrops(); // 80일차 골드·아이템 드롭 판정",
                "ApplyVictoryDrops(); // 80일차 골드·아이템 드롭 판정");

            string methodAnchor =
                "        // 79일차: 승리가 확정된 BattleContext의 실제 Enemy 구성 전체를 경험치로 환산한다.";

            if (!source.Contains(
                    "private void ApplyVictoryDrops()"))
            {
                int methodIndex =
                    source.IndexOf(
                        methodAnchor,
                        StringComparison.Ordinal);

                if (methodIndex < 0)
                {
                    throw new InvalidOperationException(
                        "80일차 드롭 메서드를 삽입할 79일차 성장 메서드 위치를 찾지 못했습니다. 최신 main 기준 파일인지 확인하세요.");
                }

                string dropMethod =
                    "        // 80일차: 승리가 확정된 BattleContext의 실제 Enemy 구성 전체를 드롭 테이블로 환산한다.\n"
                    + "        // FinishBattle()의 Victory 분기에서 한 번만 호출하므로 보상 UI를 열어도 재추첨하지 않는다.\n"
                    + "        private void ApplyVictoryDrops()\n"
                    + "        {\n"
                    + "            if (battleSession.Context == null\n"
                    + "                || battleSession.Context.Enemies == null)\n"
                    + "            {\n"
                    + "                LastBattleDropResult =\n"
                    + "                    BattleDropResult.Empty;\n\n"
                    + "                return;\n"
                    + "            }\n\n"
                    + "            System.Collections.Generic.List<MonsterDefinition> defeatedMonsters =\n"
                    + "                new System.Collections.Generic.List<MonsterDefinition>();\n\n"
                    + "            foreach (BattleParticipant enemy\n"
                    + "                     in battleSession.Context.Enemies)\n"
                    + "            {\n"
                    + "                if (enemy == null)\n"
                    + "                {\n"
                    + "                    continue;\n"
                    + "                }\n\n"
                    + "                MonsterDefinition definition =\n"
                    + "                    ResolveMonsterDefinition(\n"
                    + "                        enemy.DefinitionId);\n\n"
                    + "                if (definition != null)\n"
                    + "                {\n"
                    + "                    defeatedMonsters.Add(\n"
                    + "                        definition);\n"
                    + "                }\n"
                    + "            }\n\n"
                    + "            LastBattleDropResult =\n"
                    + "                BattleDropService.RollBattleDrops(\n"
                    + "                    defeatedMonsters,\n"
                    + "                    rewardRng);\n\n"
                    + "            Debug.Log(\n"
                    + "                $\"[Project Delta] 80일차 Battle Drop / Gold {LastBattleDropResult.Gold} / Item Type {LastBattleDropResult.Items.Count}\",\n"
                    + "                this);\n"
                    + "        }\n\n";

                source =
                    source.Insert(
                        methodIndex,
                        dropMethod);

                changed =
                    true;
            }

            if (!changed)
            {
                return false;
            }

            if (usesWindowsLineEndings)
            {
                source =
                    source.Replace(
                        "\n",
                        "\r\n");
            }

            File.WriteAllText(
                ControllerPath,
                source,
                new System.Text.UTF8Encoding(false));

            AssetDatabase.ImportAsset(
                ControllerPath,
                ImportAssetOptions.ForceUpdate);

            return true;
        }

        private static bool InsertAfterOnce(
            ref string source,
            string anchor,
            string insertion,
            string alreadyInstalledToken)
        {
            if (source.Contains(
                    alreadyInstalledToken))
            {
                return false;
            }

            int index =
                source.IndexOf(
                    anchor,
                    StringComparison.Ordinal);

            if (index < 0)
            {
                throw new InvalidOperationException(
                    $"80일차 설치 위치를 찾지 못했습니다: {anchor}");
            }

            int insertIndex =
                index
                + anchor.Length;

            source =
                source.Insert(
                    insertIndex,
                    insertion);

            return true;
        }

        private static int CreateAndAssignDropTables(
            ItemDefinition testItem)
        {
            string[] monsterGuids =
                AssetDatabase.FindAssets(
                    "t:MonsterDefinition");

            Array.Sort(
                monsterGuids,
                StringComparer.Ordinal);

            int assignedCount =
                0;

            for (int index = 0;
                 index < monsterGuids.Length;
                 index++)
            {
                string monsterPath =
                    AssetDatabase.GUIDToAssetPath(
                        monsterGuids[index]);

                MonsterDefinition monster =
                    AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
                        monsterPath);

                if (monster == null)
                {
                    continue;
                }

                SerializedObject monsterObject =
                    new SerializedObject(
                        monster);

                SerializedProperty dropTableProperty =
                    monsterObject.FindProperty(
                        "dropTable");

                if (dropTableProperty == null)
                {
                    throw new InvalidOperationException(
                        $"MonsterDefinition.dropTable 필드를 찾지 못했습니다: {monsterPath}");
                }

                if (dropTableProperty.objectReferenceValue != null)
                {
                    continue;
                }

                string safeId =
                    SanitizeFileName(
                        string.IsNullOrEmpty(monster.Id)
                            ? monster.name
                            : monster.Id);

                string tablePath =
                    $"{DropTableFolder}/{safeId}_DropTable.asset";

                MonsterDropTable table =
                    AssetDatabase.LoadAssetAtPath<MonsterDropTable>(
                        tablePath);

                if (table == null)
                {
                    table =
                        ScriptableObject.CreateInstance<MonsterDropTable>();

                    AssetDatabase.CreateAsset(
                        table,
                        tablePath);

                    ConfigureNewDropTable(
                        table,
                        monster,
                        testItem);
                }

                dropTableProperty.objectReferenceValue =
                    table;

                monsterObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(
                    monster);

                assignedCount++;
            }

            return assignedCount;
        }

        private static void ConfigureNewDropTable(
            MonsterDropTable table,
            MonsterDefinition monster,
            ItemDefinition testItem)
        {
            GetDefaultGoldRange(
                monster.Rarity,
                out int minimumGold,
                out int maximumGold);

            SerializedObject tableObject =
                new SerializedObject(
                    table);

            tableObject.FindProperty(
                "minimumGold").intValue =
                minimumGold;

            tableObject.FindProperty(
                "maximumGold").intValue =
                maximumGold;

            SerializedProperty drops =
                tableObject.FindProperty(
                    "itemDrops");

            drops.ClearArray();

            // 실제 아이템 콘텐츠는 후속 데이터 작업에서 채운다.
            // MON_TEST에만 100% 테스트 드롭 하나를 넣어 시스템을 즉시 검증할 수 있게 한다.
            if (monster.Id == "MON_TEST"
                && testItem != null)
            {
                drops.arraySize =
                    1;

                SerializedProperty entry =
                    drops.GetArrayElementAtIndex(
                        0);

                entry.FindPropertyRelative(
                    "item").objectReferenceValue =
                    testItem;

                entry.FindPropertyRelative(
                    "chanceBasisPoints").intValue =
                    MonsterDropEntry.MaximumChanceBasisPoints;

                entry.FindPropertyRelative(
                    "minimumQuantity").intValue =
                    1;

                entry.FindPropertyRelative(
                    "maximumQuantity").intValue =
                    1;
            }

            tableObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(
                table);
        }

        private static ItemDefinition CreateOrLoadTestItem()
        {
            ItemDefinition item =
                AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                    TestItemPath);

            if (item != null)
            {
                return item;
            }

            item =
                ScriptableObject.CreateInstance<ItemDefinition>();

            AssetDatabase.CreateAsset(
                item,
                TestItemPath);

            SerializedObject itemObject =
                new SerializedObject(
                    item);

            SerializedProperty id =
                itemObject.FindProperty(
                    "id");

            SerializedProperty displayName =
                itemObject.FindProperty(
                    "displayName");

            if (id == null
                || displayName == null)
            {
                throw new InvalidOperationException(
                    "Day80 테스트 ItemDefinition의 직렬화 필드를 찾지 못했습니다.");
            }

            id.stringValue =
                "ITEM_DAY80_TEST_DROP";

            displayName.stringValue =
                "전투 드롭 테스트 아이템";

            itemObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(
                item);

            return item;
        }

        private static void GetDefaultGoldRange(
            MonsterRarity rarity,
            out int minimumGold,
            out int maximumGold)
        {
            switch (rarity)
            {
                case MonsterRarity.Boss:
                    minimumGold =
                        40;

                    maximumGold =
                        80;
                    return;

                case MonsterRarity.Rare:
                    minimumGold =
                        15;

                    maximumGold =
                        30;
                    return;

                default:
                    minimumGold =
                        5;

                    maximumGold =
                        12;
                    return;
            }
        }

        private static string SanitizeFileName(
            string value)
        {
            char[] invalid =
                Path.GetInvalidFileNameChars();

            string result =
                value;

            for (int index = 0;
                 index < invalid.Length;
                 index++)
            {
                result =
                    result.Replace(
                        invalid[index],
                        '_');
            }

            return result;
        }

        private static void EnsureFolder(
            string parent,
            string child)
        {
            string path =
                parent
                + "/"
                + child;

            if (!AssetDatabase.IsValidFolder(
                    path))
            {
                AssetDatabase.CreateFolder(
                    parent,
                    child);
            }
        }
    }
}
