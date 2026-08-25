using System;
using System.Collections.Generic;
using System.IO;
using ProjectDelta.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectDelta.Editor
{
    public static class Day71DefeatFlowInstaller
    {
        private const string BootstrapScenePath =
            "Assets/ProjectDelta/Scenes/BootstrapScene.unity";

        private const string DungeonScenePath =
            "Assets/ProjectDelta/Scenes/DungeonScene.unity";

        private const string DefeatScenePath =
            "Assets/ProjectDelta/Scenes/DefeatScene.unity";

        private const string LegacyInstallerPath =
            "Assets/ProjectDelta/Editor/Day70SurrenderInstaller.cs";

        [MenuItem("Project Delta/71일차/71일차 전체 적용")]
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
                RemoveLegacySurrenderCanvas();
                IntegrateSurrenderIntoBattleHud();
                CreateDefeatScene();
                EnsureDefeatSceneInBuildSettings();
                RestoreOriginalScene(originalScenePath);
                DeleteLegacyInstaller();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Project Delta] 71일차 적용 완료: 항복 HUD 통합 + DefeatScene 생성");
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

        private static void RemoveLegacySurrenderCanvas()
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    BootstrapScenePath,
                    OpenSceneMode.Single);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "BattleSurrenderCanvas")
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(
                    root);

                EditorSceneManager.MarkSceneDirty(
                    scene);

                break;
            }

            EditorSceneManager.SaveScene(
                scene);
        }

        private static void IntegrateSurrenderIntoBattleHud()
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
                    "encounterController").objectReferenceValue as ExplorationMonsterEncounterController;

            if (hudRoot == null)
            {
                throw new InvalidOperationException(
                    "BattleHudController의 Hud Root가 연결되어 있지 않습니다.");
            }

            RemoveExistingIntegratedSurrender(
                hudRoot);

            Button surrenderButton =
                CreateButton(
                    hudRoot.transform,
                    "SurrenderButton",
                    "항복",
                    new Vector2(
                        24f,
                        24f),
                    new Vector2(
                        220f,
                        64f),
                    new Vector2(
                        0f,
                        0f),
                    new Vector2(
                        0f,
                        0f),
                    new Vector2(
                        0f,
                        0f));

            GameObject confirmationRoot =
                CreatePanel(
                    hudRoot.transform,
                    "SurrenderConfirmation",
                    new Vector2(
                        520f,
                        260f));

            CreateText(
                confirmationRoot.transform,
                "MessageText",
                "정말 항복하시겠습니까?",
                new Vector2(
                    0f,
                    58f),
                new Vector2(
                    440f,
                    70f),
                30);

            Button confirmButton =
                CreateButton(
                    confirmationRoot.transform,
                    "ConfirmButton",
                    "확인",
                    new Vector2(
                        -120f,
                        -70f),
                    new Vector2(
                        180f,
                        58f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f));

            Button cancelButton =
                CreateButton(
                    confirmationRoot.transform,
                    "CancelButton",
                    "취소",
                    new Vector2(
                        120f,
                        -70f),
                    new Vector2(
                        180f,
                        58f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f));

            BattleSurrenderController surrenderController =
                hudRoot.GetComponent<BattleSurrenderController>();

            if (surrenderController == null)
            {
                surrenderController =
                    Undo.AddComponent<BattleSurrenderController>(
                        hudRoot);
            }

            SerializedObject serializedController =
                new SerializedObject(
                    surrenderController);

            serializedController.FindProperty(
                    "encounterController").objectReferenceValue =
                encounterController;

            serializedController.FindProperty(
                    "surrenderButton").objectReferenceValue =
                surrenderButton;

            serializedController.FindProperty(
                    "confirmationRoot").objectReferenceValue =
                confirmationRoot;

            serializedController.FindProperty(
                    "confirmButton").objectReferenceValue =
                confirmButton;

            serializedController.FindProperty(
                    "cancelButton").objectReferenceValue =
                cancelButton;

            serializedController.ApplyModifiedPropertiesWithoutUndo();

            confirmationRoot.SetActive(
                false);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);
        }

        private static void RemoveExistingIntegratedSurrender(
            GameObject hudRoot)
        {
            Transform oldButton =
                hudRoot.transform.Find(
                    "SurrenderButton");

            if (oldButton != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    oldButton.gameObject);
            }

            Transform oldConfirmation =
                hudRoot.transform.Find(
                    "SurrenderConfirmation");

            if (oldConfirmation != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    oldConfirmation.gameObject);
            }

            BattleSurrenderController[] surrenderControllers =
                UnityEngine.Object.FindObjectsByType<BattleSurrenderController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (BattleSurrenderController controller in surrenderControllers)
            {
                if (controller.gameObject == hudRoot)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(
                    controller);
            }
        }

        private static void CreateDefeatScene()
        {
            Scene scene =
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);

            CreateEventSystem();

            GameObject canvasObject =
                new GameObject(
                    "DefeatCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster),
                    typeof(DefeatSceneController));

            Canvas canvas =
                canvasObject.GetComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(
                    1920f,
                    1080f);

            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            scaler.matchWidthOrHeight =
                0.5f;

            GameObject background =
                CreateFullScreenPanel(
                    canvasObject.transform,
                    "Background");

            Text titleText =
                CreateText(
                    background.transform,
                    "TitleText",
                    "패배",
                    new Vector2(
                        0f,
                        230f),
                    new Vector2(
                        700f,
                        100f),
                    52);

            Text reasonText =
                CreateText(
                    background.transform,
                    "ReasonText",
                    "패배 원인 : -",
                    new Vector2(
                        0f,
                        110f),
                    new Vector2(
                        800f,
                        60f),
                    28);

            Text floorText =
                CreateText(
                    background.transform,
                    "FloorText",
                    "도달 층 : -",
                    new Vector2(
                        0f,
                        40f),
                    new Vector2(
                        800f,
                        60f),
                    26);

            Text roundText =
                CreateText(
                    background.transform,
                    "RoundText",
                    "패배 라운드 : -",
                    new Vector2(
                        0f,
                        -30f),
                    new Vector2(
                        800f,
                        60f),
                    26);

            Text attackerText =
                CreateText(
                    background.transform,
                    "AttackerText",
                    "마지막 공격자 : -",
                    new Vector2(
                        0f,
                        -100f),
                    new Vector2(
                        800f,
                        60f),
                    26);

            Button returnButton =
                CreateButton(
                    background.transform,
                    "ReturnToTitleButton",
                    "타이틀로 돌아가기",
                    new Vector2(
                        0f,
                        -240f),
                    new Vector2(
                        320f,
                        72f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f));

            DefeatSceneController controller =
                canvasObject.GetComponent<DefeatSceneController>();

            SerializedObject serializedController =
                new SerializedObject(
                    controller);

            serializedController.FindProperty(
                    "titleText").objectReferenceValue =
                titleText;

            serializedController.FindProperty(
                    "reasonText").objectReferenceValue =
                reasonText;

            serializedController.FindProperty(
                    "floorText").objectReferenceValue =
                floorText;

            serializedController.FindProperty(
                    "roundText").objectReferenceValue =
                roundText;

            serializedController.FindProperty(
                    "attackerText").objectReferenceValue =
                attackerText;

            serializedController.FindProperty(
                    "returnToTitleButton").objectReferenceValue =
                returnButton;

            serializedController.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(
                scene);

            if (!EditorSceneManager.SaveScene(
                    scene,
                    DefeatScenePath))
            {
                throw new InvalidOperationException(
                    "DefeatScene 저장에 실패했습니다.");
            }
        }

        private static void CreateEventSystem()
        {
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
        }

        private static void EnsureDefeatSceneInBuildSettings()
        {
            EditorBuildSettingsScene[] currentScenes =
                EditorBuildSettings.scenes;

            foreach (EditorBuildSettingsScene buildScene in currentScenes)
            {
                if (buildScene.path == DefeatScenePath)
                {
                    return;
                }
            }

            List<EditorBuildSettingsScene> updatedScenes =
                new List<EditorBuildSettingsScene>(
                    currentScenes)
                {
                    new EditorBuildSettingsScene(
                        DefeatScenePath,
                        true)
                };

            EditorBuildSettings.scenes =
                updatedScenes.ToArray();
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

        private static void DeleteLegacyInstaller()
        {
            if (!AssetDatabase.LoadAssetAtPath<MonoScript>(
                    LegacyInstallerPath))
            {
                return;
            }

            AssetDatabase.DeleteAsset(
                LegacyInstallerPath);
        }

        private static GameObject CreateFullScreenPanel(
            Transform parent,
            string objectName)
        {
            GameObject panelObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            panelObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                panelObject.GetComponent<RectTransform>();

            rect.anchorMin =
                Vector2.zero;

            rect.anchorMax =
                Vector2.one;

            rect.offsetMin =
                Vector2.zero;

            rect.offsetMax =
                Vector2.zero;

            Image image =
                panelObject.GetComponent<Image>();

            image.color =
                new Color(
                    0.08f,
                    0.08f,
                    0.08f,
                    1f);

            return panelObject;
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
                    0.12f,
                    0.12f,
                    0.12f,
                    0.96f);

            return panelObject;
        }

        private static Button CreateButton(
            Transform parent,
            string objectName,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot)
        {
            GameObject buttonObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));

            buttonObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();

            rect.anchorMin =
                anchorMin;

            rect.anchorMax =
                anchorMax;

            rect.pivot =
                pivot;

            rect.anchoredPosition =
                anchoredPosition;

            rect.sizeDelta =
                size;

            Image image =
                buttonObject.GetComponent<Image>();

            image.color =
                new Color(
                    0.25f,
                    0.25f,
                    0.25f,
                    1f);

            Button button =
                buttonObject.GetComponent<Button>();

            button.targetGraphic =
                image;

            CreateText(
                buttonObject.transform,
                "Label",
                label,
                Vector2.zero,
                size,
                22);

            return button;
        }

        private static Text CreateText(
            Transform parent,
            string objectName,
            string value,
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

            Text text =
                textObject.GetComponent<Text>();

            text.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            text.fontSize =
                fontSize;

            text.alignment =
                TextAnchor.MiddleCenter;

            text.color =
                Color.white;

            text.text =
                value;

            return text;
        }
    }
}
