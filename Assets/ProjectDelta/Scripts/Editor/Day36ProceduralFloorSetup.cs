using ProjectDelta.Data;
using ProjectDelta.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectDelta.Editor
{
    public static class Day36ProceduralFloorSetup
    {
        private const string DataFolder = "Assets/ProjectDelta/Data/Rooms/Day31";
        private const string PrefabFolder = "Assets/ProjectDelta/Prefabs/Dungeon/Day31";

        private static readonly string[] Suffixes = { "NS", "NE", "T", "CROSS" };

        [MenuItem("Project Delta/Day36/Configure Procedural Dungeon Floor")]
        public static void Configure()
        {
            DungeonFloorController floorController =
                Object.FindFirstObjectByType<DungeonFloorController>();

            if (floorController == null)
            {
                EditorUtility.DisplayDialog(
                    "Project Delta - Day36",
                    "현재 Scene에서 DungeonFloorController를 찾을 수 없습니다.",
                    "확인");
                return;
            }

            int removedLegacyRoomCount = RemoveLegacyTestRooms(floorController);

            SerializedObject serialized = new SerializedObject(floorController);
            serialized.FindProperty("useProceduralGeneration").boolValue = true;
            serialized.FindProperty("generateFirstFloorOnStart").boolValue = true;
            serialized.FindProperty("roomWorldSize").floatValue = 10f;
            serialized.FindProperty("baseSeed").intValue = 3600;
            serialized.FindProperty("maxGenerationAttempts").intValue = 10;
            serialized.FindProperty("targetRoomCount").intValue = 8;
            serialized.FindProperty("minMainPathLength").intValue = 6;
            serialized.FindProperty("maxMainPathLength").intValue = 6;
            serialized.FindProperty("branchChance").floatValue = 1f;
            serialized.FindProperty("minBranchLength").intValue = 1;
            serialized.FindProperty("maxBranchLength").intValue = 1;
            serialized.FindProperty("specialCandidateChance").floatValue = 0.30f;
            serialized.FindProperty("loopChance").floatValue = 0f;

            PlayerGridMovementController player =
                Object.FindFirstObjectByType<PlayerGridMovementController>();
            serialized.FindProperty("playerController").objectReferenceValue = player;

            SerializedProperty bindings = serialized.FindProperty("roomBindings");
            bindings.arraySize = Suffixes.Length;

            for (int i = 0; i < Suffixes.Length; i++)
            {
                string suffix = Suffixes[i];

                RoomDefinition definition = AssetDatabase.LoadAssetAtPath<RoomDefinition>(
                    $"{DataFolder}/RoomDefinition_Test_{suffix}.asset");

                GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{PrefabFolder}/Room_Test_{suffix}.prefab");

                RoomView prefab = prefabObject != null
                    ? prefabObject.GetComponent<RoomView>()
                    : null;

                if (definition == null || prefab == null)
                {
                    EditorUtility.DisplayDialog(
                        "Project Delta - Day36",
                        $"{suffix} Day31 RoomDefinition 또는 RoomView 프리팹을 찾을 수 없습니다.\n먼저 Project Delta/Day31/Generate Multi-Exit Test Rooms를 실행하세요.",
                        "확인");
                    return;
                }

                SerializedProperty element = bindings.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("definition").objectReferenceValue = definition;
                element.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                element.FindPropertyRelative("useAsEntry").boolValue = suffix == "CROSS";
                element.FindPropertyRelative("includeInGenerationPool").boolValue = suffix == "CROSS";
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(floorController);
            EditorSceneManager.MarkSceneDirty(floorController.gameObject.scene);
            EditorSceneManager.SaveScene(floorController.gameObject.scene);

            EditorUtility.DisplayDialog(
                "Project Delta - Day36",
                $"절차 생성 설정을 완료했습니다.\nTestRoom_A/B 삭제: {removedLegacyRoomCount}개\nScene도 저장했습니다.",
                "확인");
        }

        private static int RemoveLegacyTestRooms(DungeonFloorController floorController)
        {
            int removedCount = 0;

            foreach (RoomView roomView in Object.FindObjectsByType<RoomView>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (roomView == null
                    || roomView.gameObject.scene != floorController.gameObject.scene)
                {
                    continue;
                }

                if (roomView.gameObject.name != "TestRoom_A"
                    && roomView.gameObject.name != "TestRoom_B")
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(roomView.gameObject);
                removedCount++;
            }

            return removedCount;
        }
    }
}
