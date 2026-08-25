using System;
using ProjectDelta.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectDelta.Editor
{
    public static class Day72BattleRewardInstaller
    {
        private const string DungeonScenePath =
            "Assets/ProjectDelta/Scenes/DungeonScene.unity";

        private const string RewardPanelName =
            "BattleRewardPanel";

        [MenuItem("Project Delta/72일차/72일차 보상 UI 적용")]
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
                IntegrateRewardPanel();
                RestoreOriginalScene(
                    originalScenePath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Project Delta] 72일차 적용 완료: 전투 승리 보상 UI 연결");
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

        private static void IntegrateRewardPanel()
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

            RemoveExistingRewardPanel(
                hudRoot);

            GameObject panelRoot =
                CreatePanel(
                    hudRoot.transform,
                    RewardPanelName,
                    new Vector2(
                        900f,
                        420f));

            panelRoot.transform.SetAsLastSibling();

            CreateText(
                panelRoot.transform,
                "TitleText",
                "전투 승리",
                new Vector2(
                    0f,
                    145f),
                new Vector2(
                    760f,
                    70f),
                38);

            CreateText(
                panelRoot.transform,
                "GuideText",
                "보상 하나를 선택하세요.",
                new Vector2(
                    0f,
                    85f),
                new Vector2(
                    760f,
                    50f),
                22);

            Button[] rewardButtons =
                new Button[3];

            Text[] rewardTexts =
                new Text[3];

            rewardButtons[0] =
                CreateRewardButton(
                    panelRoot.transform,
                    "RewardButton_Gold",
                    "골드 +100",
                    new Vector2(
                        -260f,
                        -55f),
                    out rewardTexts[0]);

            rewardButtons[1] =
                CreateRewardButton(
                    panelRoot.transform,
                    "RewardButton_Health",
                    "HP +10",
                    new Vector2(
                        0f,
                        -55f),
                    out rewardTexts[1]);

            rewardButtons[2] =
                CreateRewardButton(
                    panelRoot.transform,
                    "RewardButton_Mana",
                    "MP +5",
                    new Vector2(
                        260f,
                        -55f),
                    out rewardTexts[2]);

            BattleRewardPanelController rewardController =
                battleHud.GetComponent<BattleRewardPanelController>();

            if (rewardController == null)
            {
                rewardController =
                    Undo.AddComponent<BattleRewardPanelController>(
                        battleHud.gameObject);
            }

            SerializedObject serializedController =
                new SerializedObject(
                    rewardController);

            serializedController.FindProperty(
                    "encounterController").objectReferenceValue =
                encounterController;

            serializedController.FindProperty(
                    "panelRoot").objectReferenceValue =
                panelRoot;

            SerializedProperty buttonArray =
                serializedController.FindProperty(
                    "rewardButtons");

            buttonArray.arraySize =
                rewardButtons.Length;

            for (int index = 0; index < rewardButtons.Length; index++)
            {
                buttonArray.GetArrayElementAtIndex(
                    index).objectReferenceValue =
                    rewardButtons[index];
            }

            SerializedProperty textArray =
                serializedController.FindProperty(
                    "rewardTexts");

            textArray.arraySize =
                rewardTexts.Length;

            for (int index = 0; index < rewardTexts.Length; index++)
            {
                textArray.GetArrayElementAtIndex(
                    index).objectReferenceValue =
                    rewardTexts[index];
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();

            panelRoot.SetActive(
                false);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);
        }

        private static void RemoveExistingRewardPanel(
            GameObject hudRoot)
        {
            Transform oldPanel =
                hudRoot.transform.Find(
                    RewardPanelName);

            if (oldPanel != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    oldPanel.gameObject);
            }
        }

        private static GameObject CreatePanel(
            Transform parent,
            string objectName,
            Vector2 size)
        {
            GameObject panelObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            panelObject.layer =
                parent.gameObject.layer;

            panelObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                panelObject.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f);

            rect.anchorMax =
                rect.anchorMin;

            rect.pivot =
                rect.anchorMin;

            rect.anchoredPosition =
                Vector2.zero;

            rect.sizeDelta =
                size;

            Image image =
                panelObject.GetComponent<Image>();

            image.color =
                new Color(
                    0.08f,
                    0.08f,
                    0.08f,
                    0.97f);

            return panelObject;
        }

        private static Button CreateRewardButton(
            Transform parent,
            string objectName,
            string label,
            Vector2 anchoredPosition,
            out Text labelText)
        {
            GameObject buttonObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));

            buttonObject.layer =
                parent.gameObject.layer;

            buttonObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f);

            rect.anchorMax =
                rect.anchorMin;

            rect.pivot =
                rect.anchorMin;

            rect.anchoredPosition =
                anchoredPosition;

            rect.sizeDelta =
                new Vector2(
                    220f,
                    120f);

            Image image =
                buttonObject.GetComponent<Image>();

            image.color =
                new Color(
                    0.92f,
                    0.92f,
                    0.92f,
                    1f);

            labelText =
                CreateText(
                    buttonObject.transform,
                    "Label",
                    label,
                    Vector2.zero,
                    new Vector2(
                        200f,
                        100f),
                    24);

            labelText.color =
                Color.black;

            return buttonObject.GetComponent<Button>();
        }

        private static Text CreateText(
            Transform parent,
            string objectName,
            string text,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize)
        {
            GameObject textObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));

            textObject.layer =
                parent.gameObject.layer;

            textObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                textObject.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f);

            rect.anchorMax =
                rect.anchorMin;

            rect.pivot =
                rect.anchorMin;

            rect.anchoredPosition =
                anchoredPosition;

            rect.sizeDelta =
                size;

            Text label =
                textObject.GetComponent<Text>();

            label.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            label.fontSize =
                fontSize;

            label.alignment =
                TextAnchor.MiddleCenter;

            label.color =
                Color.white;

            label.text =
                text;

            return label;
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
