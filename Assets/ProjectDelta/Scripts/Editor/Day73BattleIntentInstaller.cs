using System;
using ProjectDelta.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectDelta.Editor
{
    public static class Day73BattleIntentInstaller
    {
        private const string DungeonScenePath =
            "Assets/ProjectDelta/Scenes/DungeonScene.unity";

        private const string LegacyIntentRootName =
            "BattleIntentPanel";

        private const string SlotIntentTextName =
            "BattleIntentText";

        private const string DebugMenuScriptPath =
            "Assets/ProjectDelta/Scripts/Presentation/DungeonDebugMenuController.cs";

        [MenuItem("Project Delta/73일차/73일차 행동 예고 UI 적용")]
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
                ApplyToDungeonScene();

                RestoreOriginalScene(
                    originalScenePath);

                DeleteObsoleteDebugMenuScript();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Project Delta] 73일차 UI 정리 완료: 행동 예고 위치 / 항복 버튼 / 미니맵 / 디버그 버튼");
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

        private static void ApplyToDungeonScene()
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    DungeonScenePath,
                    OpenSceneMode.Single);

            BattleHudController battleHud =
                UnityEngine.Object.FindFirstObjectByType<BattleHudController>();

            if (battleHud == null)
            {
                throw new InvalidOperationException(
                    "DungeonScene에서 BattleHudController를 찾지 못했습니다.");
            }

            SerializedObject hudSerialized =
                new SerializedObject(
                    battleHud);

            GameObject hudRoot =
                hudSerialized.FindProperty(
                    "hudRoot").objectReferenceValue as GameObject;

            ExplorationMonsterEncounterController encounterController =
                hudSerialized.FindProperty(
                    "encounterController").objectReferenceValue
                    as ExplorationMonsterEncounterController;

            if (hudRoot == null)
            {
                throw new InvalidOperationException(
                    "BattleHudController의 Hud Root가 연결되어 있지 않습니다.");
            }

            RemoveLegacyIntentPanel(
                hudRoot);

            Text[] intentTexts =
                CreateIntentTextsUnderEnemyHp(
                    hudSerialized);

            ConfigureIntentControllers(
                battleHud,
                encounterController,
                intentTexts);

            RepositionSurrenderButton(
                battleHud,
                hudRoot);

            ConfigureMinimapVisibility(
                battleHud,
                encounterController);

            RemoveDungeonDebugMenuComponents(
                scene);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);
        }

        private static Text[] CreateIntentTextsUnderEnemyHp(
            SerializedObject hudSerialized)
        {
            SerializedProperty enemySlots =
                hudSerialized.FindProperty(
                    "enemySlots");

            if (enemySlots == null)
            {
                throw new InvalidOperationException(
                    "BattleHudController의 enemySlots를 찾지 못했습니다.");
            }

            Text[] intentTexts =
                new Text[enemySlots.arraySize];

            for (int slotIndex = 0;
                 slotIndex < enemySlots.arraySize;
                 slotIndex++)
            {
                BattleParticipantSlotView slot =
                    enemySlots.GetArrayElementAtIndex(
                        slotIndex).objectReferenceValue
                    as BattleParticipantSlotView;

                if (slot == null)
                {
                    continue;
                }

                RemoveOldSlotIntentText(
                    slot);

                intentTexts[slotIndex] =
                    CreateIntentTextBelowHealth(
                        slot,
                        slotIndex);
            }

            return intentTexts;
        }

        private static Text CreateIntentTextBelowHealth(
            BattleParticipantSlotView slot,
            int slotIndex)
        {
            SerializedObject slotSerialized =
                new SerializedObject(
                    slot);

            Text healthText =
                slotSerialized.FindProperty(
                    "healthText").objectReferenceValue
                    as Text;

            GameObject textObject =
                new GameObject(
                    SlotIntentTextName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));

            textObject.layer =
                slot.gameObject.layer;

            textObject.transform.SetParent(
                slot.transform,
                false);

            RectTransform rect =
                textObject.GetComponent<RectTransform>();

            if (healthText != null)
            {
                RectTransform healthRect =
                    healthText.rectTransform;

                rect.anchorMin =
                    healthRect.anchorMin;

                rect.anchorMax =
                    healthRect.anchorMax;

                rect.pivot =
                    healthRect.pivot;

                rect.anchoredPosition =
                    healthRect.anchoredPosition
                    + new Vector2(
                        0f,
                        -38f);

                float width =
                    Mathf.Max(
                        180f,
                        healthRect.sizeDelta.x);

                rect.sizeDelta =
                    new Vector2(
                        width,
                        42f);
            }
            else
            {
                rect.anchorMin =
                    new Vector2(
                        0.5f,
                        0f);

                rect.anchorMax =
                    rect.anchorMin;

                rect.pivot =
                    new Vector2(
                        0.5f,
                        0.5f);

                rect.anchoredPosition =
                    new Vector2(
                        0f,
                        18f);

                rect.sizeDelta =
                    new Vector2(
                        190f,
                        42f);
            }

            Text text =
                textObject.GetComponent<Text>();

            text.font =
                healthText != null
                    && healthText.font != null
                        ? healthText.font
                        : Resources.GetBuiltinResource<Font>(
                            "LegacyRuntime.ttf");

            text.fontSize =
                healthText != null
                    ? Mathf.Max(
                        13,
                        healthText.fontSize - 1)
                    : 14;

            text.alignment =
                TextAnchor.UpperCenter;

            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;

            text.verticalOverflow =
                VerticalWrapMode.Overflow;

            text.lineSpacing =
                0.9f;

            text.color =
                Color.white;

            text.raycastTarget =
                false;

            text.text =
                $"[예고 없음]";

            return text;
        }

        private static void ConfigureIntentControllers(
            BattleHudController battleHud,
            ExplorationMonsterEncounterController encounterController,
            Text[] intentTexts)
        {
            BattleIntentRuntimeController runtimeController =
                battleHud.GetComponent<BattleIntentRuntimeController>();

            if (runtimeController == null)
            {
                runtimeController =
                    Undo.AddComponent<BattleIntentRuntimeController>(
                        battleHud.gameObject);
            }

            SerializedObject runtimeSerialized =
                new SerializedObject(
                    runtimeController);

            runtimeSerialized.FindProperty(
                "encounterController").objectReferenceValue =
                encounterController;

            runtimeSerialized.ApplyModifiedPropertiesWithoutUndo();

            BattleIntentHudController hudController =
                battleHud.GetComponent<BattleIntentHudController>();

            if (hudController == null)
            {
                hudController =
                    Undo.AddComponent<BattleIntentHudController>(
                        battleHud.gameObject);
            }

            SerializedObject intentHudSerialized =
                new SerializedObject(
                    hudController);

            intentHudSerialized.FindProperty(
                "encounterController").objectReferenceValue =
                encounterController;

            intentHudSerialized.FindProperty(
                "intentRoot").objectReferenceValue =
                null;

            SerializedProperty intentTextArray =
                intentHudSerialized.FindProperty(
                    "intentTexts");

            intentTextArray.arraySize =
                intentTexts.Length;

            for (int index = 0;
                 index < intentTexts.Length;
                 index++)
            {
                intentTextArray.GetArrayElementAtIndex(
                    index).objectReferenceValue =
                    intentTexts[index];
            }

            intentHudSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RepositionSurrenderButton(
            BattleHudController battleHud,
            GameObject hudRoot)
        {
            BattleSurrenderController surrenderController =
                battleHud.GetComponent<BattleSurrenderController>();

            if (surrenderController == null)
            {
                surrenderController =
                    UnityEngine.Object.FindFirstObjectByType<BattleSurrenderController>();
            }

            if (surrenderController == null)
            {
                throw new InvalidOperationException(
                    "BattleSurrenderController를 찾지 못했습니다.");
            }

            SerializedObject surrenderSerialized =
                new SerializedObject(
                    surrenderController);

            Button surrenderButton =
                surrenderSerialized.FindProperty(
                    "surrenderButton").objectReferenceValue
                    as Button;

            if (surrenderButton == null)
            {
                throw new InvalidOperationException(
                    "항복 버튼 참조를 찾지 못했습니다.");
            }

            Button charmButton =
                FindButtonByLabel(
                    hudRoot.transform,
                    "유혹");

            if (charmButton == null)
            {
                Transform fallback =
                    FindDescendantByName(
                        hudRoot.transform,
                        "ActionButton6");

                charmButton =
                    fallback != null
                        ? fallback.GetComponent<Button>()
                        : null;
            }

            if (charmButton == null)
            {
                throw new InvalidOperationException(
                    "유혹 버튼을 찾지 못했습니다.");
            }

            RectTransform charmRect =
                charmButton.GetComponent<RectTransform>();

            RectTransform surrenderRect =
                surrenderButton.GetComponent<RectTransform>();

            Transform actionParent =
                charmRect.parent;

            surrenderRect.SetParent(
                actionParent,
                false);

            surrenderButton.gameObject.layer =
                charmButton.gameObject.layer;

            surrenderRect.anchorMin =
                charmRect.anchorMin;

            surrenderRect.anchorMax =
                charmRect.anchorMax;

            surrenderRect.pivot =
                charmRect.pivot;

            surrenderRect.sizeDelta =
                charmRect.sizeDelta;

            surrenderRect.localScale =
                charmRect.localScale;

            surrenderRect.localRotation =
                charmRect.localRotation;

            float horizontalStep =
                CalculateActionButtonStep(
                    charmRect);

            surrenderRect.anchoredPosition =
                new Vector2(
                    charmRect.anchoredPosition.x
                    + horizontalStep,
                    charmRect.anchoredPosition.y);

            surrenderRect.SetSiblingIndex(
                Mathf.Min(
                    charmRect.GetSiblingIndex() + 1,
                    actionParent.childCount - 1));

            Text surrenderLabel =
                surrenderButton.GetComponentInChildren<Text>(
                    true);

            Text charmLabel =
                charmButton.GetComponentInChildren<Text>(
                    true);

            if (surrenderLabel != null)
            {
                surrenderLabel.text =
                    "항복";

                if (charmLabel != null)
                {
                    surrenderLabel.font =
                        charmLabel.font;

                    surrenderLabel.fontSize =
                        charmLabel.fontSize;

                    surrenderLabel.alignment =
                        charmLabel.alignment;
                }
            }
        }

        private static float CalculateActionButtonStep(
            RectTransform referenceButton)
        {
            float fallbackStep =
                Mathf.Max(
                    1f,
                    referenceButton.rect.width)
                + 8f;

            RectTransform parent =
                referenceButton.parent as RectTransform;

            if (parent == null)
            {
                return fallbackStep;
            }

            float nearestDistance =
                float.MaxValue;

            for (int index = 0;
                 index < parent.childCount;
                 index++)
            {
                RectTransform other =
                    parent.GetChild(
                        index) as RectTransform;

                if (other == null
                    || other == referenceButton
                    || other.GetComponent<Button>() == null)
                {
                    continue;
                }

                float distance =
                    referenceButton.anchoredPosition.x
                    - other.anchoredPosition.x;

                if (distance > 1f
                    && distance < nearestDistance)
                {
                    nearestDistance =
                        distance;
                }
            }

            return nearestDistance < float.MaxValue
                ? nearestDistance
                : fallbackStep;
        }

        private static void ConfigureMinimapVisibility(
            BattleHudController battleHud,
            ExplorationMonsterEncounterController encounterController)
        {
            DungeonMinimapController minimapController =
                UnityEngine.Object.FindFirstObjectByType<DungeonMinimapController>();

            if (minimapController == null)
            {
                Debug.LogWarning(
                    "[Project Delta] DungeonMinimapController를 찾지 못해 전투 중 미니맵 숨김 연결을 건너뜁니다.");

                return;
            }

            BattleExplorationUiVisibilityController visibilityController =
                battleHud.GetComponent<BattleExplorationUiVisibilityController>();

            if (visibilityController == null)
            {
                visibilityController =
                    Undo.AddComponent<BattleExplorationUiVisibilityController>(
                        battleHud.gameObject);
            }

            SerializedObject visibilitySerialized =
                new SerializedObject(
                    visibilityController);

            visibilitySerialized.FindProperty(
                "encounterController").objectReferenceValue =
                encounterController;

            visibilitySerialized.FindProperty(
                "minimapController").objectReferenceValue =
                minimapController;

            visibilitySerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveDungeonDebugMenuComponents(
            Scene scene)
        {
            GameObject[] roots =
                scene.GetRootGameObjects();

            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                MonoBehaviour[] behaviours =
                    roots[rootIndex].GetComponentsInChildren<MonoBehaviour>(
                        true);

                for (int index = behaviours.Length - 1;
                     index >= 0;
                     index--)
                {
                    MonoBehaviour behaviour =
                        behaviours[index];

                    if (behaviour == null)
                    {
                        continue;
                    }

                    Type type =
                        behaviour.GetType();

                    if (type.FullName
                        != "ProjectDelta.Presentation.DungeonDebugMenuController")
                    {
                        continue;
                    }

                    Undo.DestroyObjectImmediate(
                        behaviour);
                }
            }
        }

        private static void DeleteObsoleteDebugMenuScript()
        {
            if (AssetDatabase.LoadAssetAtPath<MonoScript>(
                    DebugMenuScriptPath) == null)
            {
                return;
            }

            if (!AssetDatabase.DeleteAsset(
                    DebugMenuScriptPath))
            {
                Debug.LogWarning(
                    "[Project Delta] DungeonDebugMenuController.cs 자동 삭제에 실패했습니다. 파일을 수동 삭제해 주세요.");
            }
        }

        private static void RemoveLegacyIntentPanel(
            GameObject hudRoot)
        {
            Transform existing =
                hudRoot.transform.Find(
                    LegacyIntentRootName);

            if (existing != null)
            {
                Undo.DestroyObjectImmediate(
                    existing.gameObject);
            }
        }

        private static void RemoveOldSlotIntentText(
            BattleParticipantSlotView slot)
        {
            for (int childIndex = slot.transform.childCount - 1;
                 childIndex >= 0;
                 childIndex--)
            {
                Transform child =
                    slot.transform.GetChild(
                        childIndex);

                if (child.name == SlotIntentTextName)
                {
                    Undo.DestroyObjectImmediate(
                        child.gameObject);
                }
            }
        }

        private static Button FindButtonByLabel(
            Transform root,
            string label)
        {
            Button[] buttons =
                root.GetComponentsInChildren<Button>(
                    true);

            for (int index = 0;
                 index < buttons.Length;
                 index++)
            {
                Text text =
                    buttons[index].GetComponentInChildren<Text>(
                        true);

                if (text != null
                    && string.Equals(
                        text.text,
                        label,
                        StringComparison.Ordinal))
                {
                    return buttons[index];
                }
            }

            return null;
        }

        private static Transform FindDescendantByName(
            Transform root,
            string objectName)
        {
            if (root == null)
            {
                return null;
            }

            Transform direct =
                root.Find(
                    objectName);

            if (direct != null)
            {
                return direct;
            }

            for (int index = 0;
                 index < root.childCount;
                 index++)
            {
                Transform result =
                    FindDescendantByName(
                        root.GetChild(index),
                        objectName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
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
