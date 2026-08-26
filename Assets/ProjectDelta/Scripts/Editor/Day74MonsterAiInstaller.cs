using System;
using System.IO;
using System.Text;
using ProjectDelta.Data;
using ProjectDelta.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectDelta.Editor
{
    public static class Day74MonsterAiInstaller
    {
        private const string DungeonScenePath =
            "Assets/ProjectDelta/Scenes/DungeonScene.unity";

        private const string MonsterDefinitionPath =
            "Assets/ProjectDelta/Data/Monster/Monster Definition/MonsterDefinition.asset";

        private const string AiFolderPath =
            "Assets/ProjectDelta/Data/Monster/AI";

        private const string AiProfilePath =
            AiFolderPath
            + "/DefaultTestMonsterAiProfile.asset";

        private const string SkillFolderPath =
            "Assets/ProjectDelta/Data/Skills";

        private const string TestSkillPath =
            SkillFolderPath
            + "/TestMonsterHeavyAttack.asset";

        private const string EncounterControllerSourcePath =
            "Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs";

        [MenuItem("Project Delta/74일차/74일차 몬스터 AI 적용")]
        public static void Install()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string originalScenePath =
                SceneManager.GetActiveScene().path;

            try
            {
                EnsureFolder(
                    AiFolderPath);

                EnsureFolder(
                    SkillFolderPath);

                SkillDefinition testSkill =
                    CreateOrUpdateTestSkill();

                MonsterAiProfile profile =
                    CreateOrUpdateAiProfile(
                        testSkill);

                MonsterDefinition monsterDefinition =
                    AssignProfileToTestMonster(
                        profile);

                ConfigureDungeonScene(
                    monsterDefinition);

                RestoreOriginalScene(
                    originalScenePath);

                PatchEncounterControllerSource();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Project Delta] 74일차 적용 완료: 몬스터 AI 선택 + Intent 실행 연결");
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception);

                RestoreOriginalScene(
                    originalScenePath);

                throw;
            }
        }

        private static SkillDefinition CreateOrUpdateTestSkill()
        {
            SkillDefinition skill =
                AssetDatabase.LoadAssetAtPath<SkillDefinition>(
                    TestSkillPath);

            if (skill == null)
            {
                skill =
                    ScriptableObject.CreateInstance<SkillDefinition>();

                AssetDatabase.CreateAsset(
                    skill,
                    TestSkillPath);
            }

            SerializedObject serialized =
                new SerializedObject(
                    skill);

            SetString(
                serialized,
                "id",
                "SKILL_MON_HEAVY_ATTACK");

            SetString(
                serialized,
                "displayName",
                "강공격");

            SetEnum(
                serialized,
                "targetType",
                (int)SkillTargetType.Enemy);

            SetInt(
                serialized,
                "manaCost",
                0);

            SetInt(
                serialized,
                "staminaCost",
                0);

            SetInt(
                serialized,
                "damageMultiplierPercent",
                140);

            SetInt(
                serialized,
                "accuracyModifierPercent",
                0);

            SetInt(
                serialized,
                "criticalChancePercent",
                0);

            SetInt(
                serialized,
                "criticalMultiplierPercent",
                0);

            SerializedProperty statusProperty =
                serialized.FindProperty(
                    "grantedStatusEffect");

            if (statusProperty != null)
            {
                statusProperty.objectReferenceValue =
                    null;
            }

            SetBool(
                serialized,
                "grantsExtraAction",
                false);

            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(
                skill);

            return skill;
        }

        private static MonsterAiProfile CreateOrUpdateAiProfile(
            SkillDefinition testSkill)
        {
            MonsterAiProfile profile =
                AssetDatabase.LoadAssetAtPath<MonsterAiProfile>(
                    AiProfilePath);

            if (profile == null)
            {
                profile =
                    ScriptableObject.CreateInstance<MonsterAiProfile>();

                AssetDatabase.CreateAsset(
                    profile,
                    AiProfilePath);
            }

            SerializedObject serialized =
                new SerializedObject(
                    profile);

            SetInt(
                serialized,
                "attackWeight",
                55);

            SetInt(
                serialized,
                "defendWeight",
                25);

            SetInt(
                serialized,
                "lowHpThresholdPercent",
                40);

            SetInt(
                serialized,
                "lowHpDefendBonusWeight",
                30);

            SerializedProperty skills =
                serialized.FindProperty(
                    "skillEntries");

            if (skills == null)
            {
                throw new InvalidOperationException(
                    "MonsterAiProfile.skillEntries를 찾지 못했습니다.");
            }

            skills.arraySize =
                1;

            SerializedProperty entry =
                skills.GetArrayElementAtIndex(
                    0);

            SerializedProperty skillProperty =
                entry.FindPropertyRelative(
                    "skill");

            SerializedProperty weightProperty =
                entry.FindPropertyRelative(
                    "weight");

            skillProperty.objectReferenceValue =
                testSkill;

            weightProperty.intValue =
                20;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(
                profile);

            return profile;
        }

        private static MonsterDefinition AssignProfileToTestMonster(
            MonsterAiProfile profile)
        {
            MonsterDefinition monster =
                AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
                    MonsterDefinitionPath);

            if (monster == null)
            {
                throw new InvalidOperationException(
                    $"테스트 MonsterDefinition을 찾지 못했습니다: {MonsterDefinitionPath}");
            }

            SerializedObject serialized =
                new SerializedObject(
                    monster);

            SerializedProperty aiProfile =
                serialized.FindProperty(
                    "aiProfile");

            if (aiProfile == null)
            {
                throw new InvalidOperationException(
                    "MonsterDefinition.aiProfile을 찾지 못했습니다.");
            }

            aiProfile.objectReferenceValue =
                profile;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(
                monster);

            return monster;
        }

        private static void ConfigureDungeonScene(
            MonsterDefinition monsterDefinition)
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    DungeonScenePath,
                    OpenSceneMode.Single);

            BattleIntentRuntimeController intentController =
                UnityEngine.Object.FindFirstObjectByType<BattleIntentRuntimeController>();

            BattleHudController battleHud =
                UnityEngine.Object.FindFirstObjectByType<BattleHudController>();

            if (intentController == null
                && battleHud != null)
            {
                intentController =
                    Undo.AddComponent<BattleIntentRuntimeController>(
                        battleHud.gameObject);
            }

            if (intentController == null)
            {
                throw new InvalidOperationException(
                    "BattleIntentRuntimeController를 찾지 못했습니다. 73일차 적용 상태를 확인해 주세요.");
            }

            ExplorationMonsterEncounterController encounterController =
                UnityEngine.Object.FindFirstObjectByType<ExplorationMonsterEncounterController>();

            SerializedObject serialized =
                new SerializedObject(
                    intentController);

            SerializedProperty encounterProperty =
                serialized.FindProperty(
                    "encounterController");

            SerializedProperty monsterProperty =
                serialized.FindProperty(
                    "monsterDefinition");

            if (encounterProperty != null)
            {
                encounterProperty.objectReferenceValue =
                    encounterController;
            }

            if (monsterProperty == null)
            {
                throw new InvalidOperationException(
                    "BattleIntentRuntimeController.monsterDefinition을 찾지 못했습니다.");
            }

            monsterProperty.objectReferenceValue =
                monsterDefinition;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);
        }

        private static void PatchEncounterControllerSource()
        {
            if (!File.Exists(
                    EncounterControllerSourcePath))
            {
                throw new FileNotFoundException(
                    "ExplorationMonsterEncounterController.cs를 찾지 못했습니다.",
                    EncounterControllerSourcePath);
            }

            string source =
                File.ReadAllText(
                    EncounterControllerSourcePath);

            bool changed =
                false;

            if (!source.Contains(
                    "ExecuteEnemyIntent("))
            {
                const string oldAutoAction = @"                IReadOnlyList<BattleParticipant> validTargets =
                    BattleTargeting.GetValidTargets(
                        battleSession.Context,
                        actor);

                if (validTargets.Count > 0)
                {
                    battleSession.TrySelectTarget(
                        validTargets[0]);
                }

                ConfirmAttack();
";

                const string newAutoAction = @"                if (!ExecuteEnemyIntent(
                        actor))
                {
                    IReadOnlyList<BattleParticipant> fallbackTargets =
                        BattleTargeting.GetValidTargets(
                            battleSession.Context,
                            actor);

                    if (fallbackTargets.Count > 0)
                    {
                        battleSession.TrySelectTarget(
                            fallbackTargets[0]);
                    }

                    ConfirmAttack();
                }
";

                if (!source.Contains(
                        oldAutoAction))
                {
                    throw new InvalidOperationException(
                        "적 자동 공격 코드 위치를 찾지 못했습니다. 최신 커밋과 파일 상태를 확인해 주세요.");
                }

                source =
                    source.Replace(
                        oldAutoAction,
                        newAutoAction);

                const string insertMarker =
                    "        // 49일차: AwaitingAction 상태에서 CurrentActor의 공격 대상을 지정·재지정한다.";

                if (!source.Contains(
                        insertMarker))
                {
                    throw new InvalidOperationException(
                        "74일차 AI 실행 메서드 삽입 위치를 찾지 못했습니다.");
                }

                source =
                    source.Replace(
                        insertMarker,
                        EnemyIntentExecutionMethods
                        + "\n"
                        + insertMarker);

                changed =
                    true;
            }

            if (!changed)
            {
                Debug.Log(
                    "[Project Delta] 74일차 Encounter Controller 소스 패치는 이미 적용되어 있습니다.");

                return;
            }

            File.WriteAllText(
                EncounterControllerSourcePath,
                source,
                new UTF8Encoding(
                    false));

            AssetDatabase.ImportAsset(
                EncounterControllerSourcePath,
                ImportAssetOptions.ForceUpdate);
        }

        private const string EnemyIntentExecutionMethods = @"        // 74일차: Enemy는 73일차에 미리 저장한 Intent를 실제 차례에서 그대로 실행한다.
        private bool ExecuteEnemyIntent(
            BattleParticipant actor)
        {
            if (actor == null
                || battleSession.Context == null)
            {
                return false;
            }

            if (!BattleIntentService.TryGet(
                    actor.InstanceId,
                    out BattleIntent intent))
            {
                MonsterAiProfile profile =
                    testMonsterDefinition != null
                        ? testMonsterDefinition.AiProfile
                        : null;

                bool skillsBlocked =
                    IsAiSkillBlocked(
                        actor);

                if (!MonsterAiDecisionService.TryCreateIntent(
                        actor,
                        battleSession.Context.Player,
                        profile,
                        skillsBlocked,
                        combatRng,
                        out intent))
                {
                    intent =
                        BattleIntent.CreateBasicAttack(
                            actor,
                            battleSession.Context.Player);
                }

                if (intent != null)
                {
                    BattleIntentService.TryRegister(
                        intent);
                }
            }

            if (intent == null)
            {
                return false;
            }

            switch (intent.CommandId)
            {
                case ""Attack"":
                    if (!TrySelectIntentTarget(
                            intent))
                    {
                        return false;
                    }

                    return ConfirmAttack()
                        != null;

                case ""Defend"":
                    return ConfirmDefend()
                        != null;

                case ""Skill"":
                    if (intent.Skill == null)
                    {
                        return false;
                    }

                    if (intent.Skill.TargetType == SkillTargetType.Enemy
                        && !TrySelectIntentTarget(
                            intent))
                    {
                        return false;
                    }

                    return ConfirmSkill(
                        intent.Skill)
                        != null;

                default:
                    return false;
            }
        }

        private bool TrySelectIntentTarget(
            BattleIntent intent)
        {
            if (intent == null
                || string.IsNullOrEmpty(
                    intent.TargetInstanceId)
                || battleSession.Context == null
                || !battleSession.Context.TryGetParticipant(
                    intent.TargetInstanceId,
                    out BattleParticipant target)
                || target == null
                || !target.IsAlive)
            {
                return false;
            }

            return battleSession.TrySelectTarget(
                target);
        }

        private static bool IsAiSkillBlocked(
            BattleParticipant actor)
        {
            if (actor == null
                || actor.StatusEffects == null)
            {
                return false;
            }

            for (int index = 0;
                 index < actor.StatusEffects.Count;
                 index++)
            {
                StatusEffectInstance status =
                    actor.StatusEffects[index];

                if (status == null
                    || status.IsExpired
                    || string.IsNullOrEmpty(
                        status.DefinitionId))
                {
                    continue;
                }

                if (status.DefinitionId.IndexOf(
                        ""SILENCE"",
                        StringComparison.OrdinalIgnoreCase) >= 0
                    || status.DefinitionId.IndexOf(
                        ""침묵"",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
";

        private static void EnsureFolder(
            string folderPath)
        {
            string[] parts =
                folderPath.Split(
                    '/');

            string current =
                parts[0];

            for (int index = 1; index < parts.Length; index++)
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

        private static void SetInt(
            SerializedObject serialized,
            string propertyName,
            int value)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            if (property != null)
            {
                property.intValue =
                    value;
            }
        }

        private static void SetString(
            SerializedObject serialized,
            string propertyName,
            string value)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            if (property != null)
            {
                property.stringValue =
                    value;
            }
        }

        private static void SetBool(
            SerializedObject serialized,
            string propertyName,
            bool value)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            if (property != null)
            {
                property.boolValue =
                    value;
            }
        }

        private static void SetEnum(
            SerializedObject serialized,
            string propertyName,
            int value)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            if (property != null)
            {
                property.enumValueIndex =
                    value;
            }
        }

        private static void RestoreOriginalScene(
            string originalScenePath)
        {
            if (string.IsNullOrEmpty(
                    originalScenePath))
            {
                EditorSceneManager.OpenScene(
                    DungeonScenePath,
                    OpenSceneMode.Single);

                return;
            }

            EditorSceneManager.OpenScene(
                originalScenePath,
                OpenSceneMode.Single);
        }
    }
}
