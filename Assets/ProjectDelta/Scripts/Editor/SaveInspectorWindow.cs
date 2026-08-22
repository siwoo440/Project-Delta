using System.IO;
using ProjectDelta.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace ProjectDelta.Editor
{
    // 기획서 10.6절 에디터 도구 "Save Inspector | 개발 저장 데이터 확인".
    // 경로 규칙은 SavePaths(실제 도메인 코드)를 그대로 사용하고, 직접 재구현하지 않는다.
    public sealed class SaveInspectorWindow : EditorWindow
    {
        [MenuItem("Window/Project Delta/Save Inspector")]
        private static void Open()
        {
            GetWindow<SaveInspectorWindow>("Save Inspector");
        }

        private Vector2 _scroll;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("저장 폴더", SavePaths.SaveDirectory);
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawFileSection("Profile", SavePaths.ProfilePath);
            DrawFileSection("Run", SavePaths.RunPath);
            DrawFileSection("Settings", SavePaths.SettingsPath);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawFileSection(string label, string path)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            if (!File.Exists(path))
            {
                EditorGUILayout.LabelField("파일 없음");
                EditorGUILayout.Space();
                return;
            }

            EditorGUILayout.TextArea(File.ReadAllText(path), GUILayout.MinHeight(120));

            if (GUILayout.Button($"{label} 파일 손상시키기 (테스트용, 되돌릴 수 없음)"))
            {
                File.WriteAllText(path, "corrupted-for-testing");
            }

            EditorGUILayout.Space();
        }
    }
}
