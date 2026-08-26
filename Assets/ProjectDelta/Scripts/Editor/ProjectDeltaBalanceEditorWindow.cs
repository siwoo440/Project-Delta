using System;
using System.Collections.Generic;
using ProjectDelta.Data;
using UnityEditor;
using UnityEngine;

namespace ProjectDelta.Editor
{
    // 87일차: 몬스터·스킬·상태이상·성장·드롭 원본을 한 창에서 검색하고 직접 수정한다.
    public sealed class ProjectDeltaBalanceEditorWindow : EditorWindow
    {
        private const float LeftPanelWidth = 310f;

        private static readonly string[] TabLabels =
        {
            "Monster",
            "Skill",
            "Status",
            "Growth",
            "Drop"
        };

        private readonly List<UnityEngine.Object> assets =
            new List<UnityEngine.Object>();

        private BalanceEditorCategory currentCategory =
            BalanceEditorCategory.Monster;

        private string searchText =
            string.Empty;

        private UnityEngine.Object selectedAsset;

        private Vector2 assetListScroll;
        private Vector2 inspectorScroll;

        // Unity 상단 Project Delta 메뉴에서 Balance Editor를 연다.
        [MenuItem("Project Delta/Balance Editor")]
        private static void OpenWindow()
        {
            ProjectDeltaBalanceEditorWindow window =
                GetWindow<ProjectDeltaBalanceEditorWindow>();

            window.titleContent =
                new GUIContent(
                    "Balance Editor");

            window.minSize =
                new Vector2(
                    820f,
                    520f);

            window.Show();
        }

        private void OnEnable()
        {
            titleContent =
                new GUIContent(
                    "Balance Editor");

            minSize =
                new Vector2(
                    820f,
                    520f);

            ReloadAssets();
        }

        private void OnFocus()
        {
            ReloadAssets();
            Repaint();
        }

        private void OnProjectChange()
        {
            ReloadAssets();
            Repaint();
        }

        private void OnGUI()
        {
            DrawTitle();
            DrawTabs();
            DrawSearchAndActions();
            EditorGUILayout.Space(
                4f);
            DrawMainArea();
        }

        // 창의 용도를 한눈에 알 수 있도록 상단 제목과 안내를 표시한다.
        private static void DrawTitle()
        {
            EditorGUILayout.LabelField(
                "Project Delta - Balance Editor",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "기존 ScriptableObject 원본을 직접 편집합니다. 변경은 Unity Undo(Ctrl+Z)를 지원하며 저장은 Ctrl+S 또는 '모든 수정 저장' 버튼을 사용합니다.",
                MessageType.Info);
        }

        // 카테고리 탭을 전환하면 해당 타입의 에셋만 다시 수집한다.
        private void DrawTabs()
        {
            int selectedIndex =
                GUILayout.Toolbar(
                    (int)currentCategory,
                    TabLabels);

            if (selectedIndex
                == (int)currentCategory)
            {
                return;
            }

            currentCategory =
                (BalanceEditorCategory)selectedIndex;

            selectedAsset =
                null;

            assetListScroll =
                Vector2.zero;

            inspectorScroll =
                Vector2.zero;

            ReloadAssets();
        }

        // 검색창, 목록 새로고침, 전체 Asset 저장 버튼을 제공한다.
        private void DrawSearchAndActions()
        {
            EditorGUILayout.BeginHorizontal();

            string nextSearchText =
                EditorGUILayout.TextField(
                    "검색",
                    searchText);

            if (!string.Equals(
                    nextSearchText,
                    searchText,
                    StringComparison.Ordinal))
            {
                searchText =
                    nextSearchText;
            }

            if (GUILayout.Button(
                    "검색 지우기",
                    GUILayout.Width(
                        80f)))
            {
                searchText =
                    string.Empty;

                GUI.FocusControl(
                    null);
            }

            if (GUILayout.Button(
                    "새로고침",
                    GUILayout.Width(
                        70f)))
            {
                ReloadAssets();
            }

            if (GUILayout.Button(
                    "모든 수정 저장",
                    GUILayout.Width(
                        100f)))
            {
                AssetDatabase.SaveAssets();

                ShowNotification(
                    new GUIContent(
                        "Balance Asset 저장 완료"));
            }

            EditorGUILayout.EndHorizontal();
        }

        // 왼쪽 에셋 목록과 오른쪽 원본 Inspector를 두 영역으로 나눈다.
        private void DrawMainArea()
        {
            EditorGUILayout.BeginHorizontal();

            DrawAssetList();
            DrawSelectedInspector();

            EditorGUILayout.EndHorizontal();
        }

        // 현재 탭의 에셋을 검색어로 필터링해 왼쪽 목록에 표시한다.
        private void DrawAssetList()
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox,
                GUILayout.Width(
                    LeftPanelWidth));

            int visibleCount =
                CountVisibleAssets();

            EditorGUILayout.LabelField(
                $"{GetCategoryLabel(currentCategory)} ({visibleCount}/{assets.Count})",
                EditorStyles.boldLabel);

            assetListScroll =
                EditorGUILayout.BeginScrollView(
                    assetListScroll);

            bool drewAny =
                false;

            for (int index = 0;
                 index < assets.Count;
                 index++)
            {
                UnityEngine.Object asset =
                    assets[index];

                if (!BalanceEditorUtility.MatchesSearch(
                        asset,
                        searchText))
                {
                    continue;
                }

                drewAny =
                    true;

                bool isSelected =
                    selectedAsset == asset;

                GUIStyle buttonStyle =
                    new GUIStyle(
                        EditorStyles.miniButton);

                if (isSelected)
                {
                    buttonStyle.fontStyle =
                        FontStyle.Bold;
                }

                if (GUILayout.Button(
                        BalanceEditorUtility.GetDisplayLabel(
                            asset),
                        buttonStyle,
                        GUILayout.Height(
                            26f)))
                {
                    selectedAsset =
                        asset;

                    inspectorScroll =
                        Vector2.zero;

                    GUI.FocusControl(
                        null);
                }
            }

            if (!drewAny)
            {
                EditorGUILayout.HelpBox(
                    "현재 검색 조건에 맞는 에셋이 없습니다.",
                    MessageType.None);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // 선택한 에셋은 별도 복사본 없이 SerializedObject로 직접 수정한다.
        private void DrawSelectedInspector()
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox);

            if (selectedAsset == null)
            {
                EditorGUILayout.HelpBox(
                    "왼쪽 목록에서 수정할 밸런스 에셋을 선택하세요.",
                    MessageType.Info);

                EditorGUILayout.EndVertical();
                return;
            }

            DrawSelectedHeader();

            inspectorScroll =
                EditorGUILayout.BeginScrollView(
                    inspectorScroll);

            SerializedObject serializedObject =
                new SerializedObject(
                    selectedAsset);

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            DrawAllSerializedProperties(
                serializedObject);

            bool changed =
                EditorGUI.EndChangeCheck();

            bool applied =
                serializedObject.ApplyModifiedProperties();

            if (changed
                || applied)
            {
                EditorUtility.SetDirty(
                    selectedAsset);
            }

            DrawWarnings();

            if (selectedAsset
                is PlayerGrowthDefinition growthDefinition)
            {
                DrawGrowthSummary(
                    growthDefinition);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // 선택 에셋 이름과 Project 창 선택/핑 기능을 제공한다.
        private void DrawSelectedHeader()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                BalanceEditorUtility.GetDisplayLabel(
                    selectedAsset),
                EditorStyles.boldLabel);

            if (GUILayout.Button(
                    "Project에서 선택",
                    GUILayout.Width(
                        105f)))
            {
                Selection.activeObject =
                    selectedAsset;

                EditorGUIUtility.PingObject(
                    selectedAsset);
            }

            EditorGUILayout.EndHorizontal();
        }

        // 일반 Inspector가 보여주는 직렬화 필드를 동일하게 그려 데이터 이중화를 피한다.
        private static void DrawAllSerializedProperties(
            SerializedObject serializedObject)
        {
            SerializedProperty iterator =
                serializedObject.GetIterator();

            bool enterChildren =
                true;

            while (iterator.NextVisible(
                enterChildren))
            {
                enterChildren =
                    false;

                bool isScriptProperty =
                    iterator.propertyPath
                    == "m_Script";

                using (new EditorGUI.DisabledScope(
                    isScriptProperty))
                {
                    EditorGUILayout.PropertyField(
                        iterator,
                        true);
                }
            }
        }

        // Utility가 발견한 잘못된 값은 자동 수정하지 않고 HelpBox로만 알린다.
        private void DrawWarnings()
        {
            IReadOnlyList<string> warnings =
                BalanceEditorUtility.GetWarnings(
                    selectedAsset);

            if (warnings.Count == 0)
            {
                EditorGUILayout.Space(
                    4f);

                EditorGUILayout.HelpBox(
                    "현재 기본 검증 경고가 없습니다.",
                    MessageType.Info);

                return;
            }

            EditorGUILayout.Space(
                4f);

            EditorGUILayout.LabelField(
                $"검증 경고 ({warnings.Count})",
                EditorStyles.boldLabel);

            for (int index = 0;
                 index < warnings.Count;
                 index++)
            {
                EditorGUILayout.HelpBox(
                    warnings[index],
                    MessageType.Warning);
            }
        }

        // Growth 데이터는 배열 아래에 사람이 읽기 쉬운 레벨별/누적 경험치 표를 추가한다.
        private static void DrawGrowthSummary(
            PlayerGrowthDefinition growthDefinition)
        {
            IReadOnlyList<BalanceGrowthRow> rows =
                BalanceEditorUtility.BuildGrowthRows(
                    growthDefinition);

            EditorGUILayout.Space(
                8f);

            EditorGUILayout.LabelField(
                "성장 경험치 요약",
                EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                "구간",
                EditorStyles.miniBoldLabel,
                GUILayout.Width(
                    110f));

            EditorGUILayout.LabelField(
                "필요 EXP",
                EditorStyles.miniBoldLabel,
                GUILayout.Width(
                    100f));

            EditorGUILayout.LabelField(
                "누적 EXP",
                EditorStyles.miniBoldLabel,
                GUILayout.Width(
                    100f));

            EditorGUILayout.EndHorizontal();

            for (int index = 0;
                 index < rows.Count;
                 index++)
            {
                BalanceGrowthRow row =
                    rows[index];

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(
                    $"Lv.{row.FromLevel} → Lv.{row.ToLevel}",
                    GUILayout.Width(
                        110f));

                EditorGUILayout.LabelField(
                    row.RequiredExperience.ToString(),
                    GUILayout.Width(
                        100f));

                EditorGUILayout.LabelField(
                    row.CumulativeExperience.ToString(),
                    GUILayout.Width(
                        100f));

                EditorGUILayout.EndHorizontal();
            }
        }

        // 현재 검색 조건을 만족하는 에셋 수를 계산해 목록 헤더에 표시한다.
        private int CountVisibleAssets()
        {
            int visibleCount =
                0;

            for (int index = 0;
                 index < assets.Count;
                 index++)
            {
                if (BalanceEditorUtility.MatchesSearch(
                        assets[index],
                        searchText))
                {
                    visibleCount++;
                }
            }

            return visibleCount;
        }

        // 현재 탭 타입에 맞는 ScriptableObject를 AssetDatabase에서 찾아 정렬한다.
        private void ReloadAssets()
        {
            assets.Clear();

            Type assetType =
                GetAssetType(
                    currentCategory);

            string[] guids =
                AssetDatabase.FindAssets(
                    $"t:{assetType.Name}");

            for (int index = 0;
                 index < guids.Length;
                 index++)
            {
                string assetPath =
                    AssetDatabase.GUIDToAssetPath(
                        guids[index]);

                UnityEngine.Object asset =
                    AssetDatabase.LoadAssetAtPath(
                        assetPath,
                        assetType);

                if (asset != null)
                {
                    assets.Add(
                        asset);
                }
            }

            assets.Sort(
                CompareAssets);

            if (selectedAsset != null
                && !assets.Contains(
                    selectedAsset))
            {
                selectedAsset =
                    null;
            }
        }

        // 목록은 ID/표시 라벨 기준으로 정렬해 데이터가 늘어나도 찾기 쉽게 유지한다.
        private static int CompareAssets(
            UnityEngine.Object left,
            UnityEngine.Object right)
        {
            return string.Compare(
                BalanceEditorUtility.GetDisplayLabel(
                    left),
                BalanceEditorUtility.GetDisplayLabel(
                    right),
                StringComparison.OrdinalIgnoreCase);
        }

        // 각 탭이 검색할 실제 ScriptableObject 타입을 지정한다.
        private static Type GetAssetType(
            BalanceEditorCategory category)
        {
            switch (category)
            {
                case BalanceEditorCategory.Monster:
                    return typeof(MonsterDefinition);

                case BalanceEditorCategory.Skill:
                    return typeof(SkillDefinition);

                case BalanceEditorCategory.Status:
                    return typeof(StatusEffectDefinition);

                case BalanceEditorCategory.Growth:
                    return typeof(PlayerGrowthDefinition);

                case BalanceEditorCategory.Drop:
                    return typeof(MonsterDropTable);

                default:
                    return typeof(MonsterDefinition);
            }
        }

        // 탭 목록 헤더에서 사용할 한글 분류명을 반환한다.
        private static string GetCategoryLabel(
            BalanceEditorCategory category)
        {
            switch (category)
            {
                case BalanceEditorCategory.Monster:
                    return "몬스터";

                case BalanceEditorCategory.Skill:
                    return "스킬";

                case BalanceEditorCategory.Status:
                    return "상태이상";

                case BalanceEditorCategory.Growth:
                    return "성장";

                case BalanceEditorCategory.Drop:
                    return "드롭";

                default:
                    return "데이터";
            }
        }
    }

    // Toolbar 인덱스와 맞춰 유지하는 Balance Editor 전용 카테고리다.
    public enum BalanceEditorCategory
    {
        Monster = 0,
        Skill = 1,
        Status = 2,
        Growth = 3,
        Drop = 4
    }
}
