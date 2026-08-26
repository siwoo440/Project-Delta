using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectDelta.Editor
{
    // 86일차: 최신 85일차 소스에서 전투 연출 시간 세 지점만 배속 상태를 사용하도록 자동 수정한다.
    [InitializeOnLoad]
    public static class Day86BattleSpeedInstaller
    {
        private const string EncounterPath =
            "Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs";

        private const string ParticipantSlotPath =
            "Assets/ProjectDelta/Scripts/Presentation/BattleParticipantSlotView.cs";

        private const string MenuPath =
            "Project Delta/86일차/86일차 전투 1x 2x 속도 적용";

        private const string EncounterOldText =
            "                yield return new WaitForSeconds(\n"
            + "                    EnemyActionVisibleDelaySeconds);";

        private const string EncounterNewText =
            "                yield return new WaitForSeconds(\n"
            + "                    BattleSpeedState.ScaleDuration(\n"
            + "                        EnemyActionVisibleDelaySeconds));";

        private const string BumpOldText =
            "            yield return AnimatePortraitPosition(\n"
            + "                portraitRestPosition,\n"
            + "                upPosition,\n"
            + "                bumpDuration);\n"
            + "\n"
            + "            yield return AnimatePortraitPosition(\n"
            + "                upPosition,\n"
            + "                portraitRestPosition,\n"
            + "                bumpDuration);";

        private const string BumpNewText =
            "            yield return AnimatePortraitPosition(\n"
            + "                portraitRestPosition,\n"
            + "                upPosition,\n"
            + "                BattleSpeedState.ScaleDuration(\n"
            + "                    bumpDuration));\n"
            + "\n"
            + "            yield return AnimatePortraitPosition(\n"
            + "                upPosition,\n"
            + "                portraitRestPosition,\n"
            + "                BattleSpeedState.ScaleDuration(\n"
            + "                    bumpDuration));";

        private const string DamageOldText =
            "            if (damageVisibleDuration > 0f)\n"
            + "            {\n"
            + "                yield return new WaitForSecondsRealtime(\n"
            + "                    damageVisibleDuration);\n"
            + "            }\n"
            + "\n"
            + "            float elapsed =\n"
            + "                0f;\n"
            + "\n"
            + "            while (elapsed < damageFadeDuration)\n"
            + "            {\n"
            + "                elapsed +=\n"
            + "                    Time.unscaledDeltaTime;\n"
            + "\n"
            + "                float ratio =\n"
            + "                    damageFadeDuration > 0f\n"
            + "                        ? Mathf.Clamp01(\n"
            + "                            elapsed / damageFadeDuration)\n"
            + "                        : 1f;";

        private const string DamageNewText =
            "            float scaledVisibleDuration =\n"
            + "                BattleSpeedState.ScaleDuration(\n"
            + "                    damageVisibleDuration);\n"
            + "\n"
            + "            if (scaledVisibleDuration > 0f)\n"
            + "            {\n"
            + "                yield return new WaitForSecondsRealtime(\n"
            + "                    scaledVisibleDuration);\n"
            + "            }\n"
            + "\n"
            + "            float scaledFadeDuration =\n"
            + "                BattleSpeedState.ScaleDuration(\n"
            + "                    damageFadeDuration);\n"
            + "\n"
            + "            float elapsed =\n"
            + "                0f;\n"
            + "\n"
            + "            while (elapsed < scaledFadeDuration)\n"
            + "            {\n"
            + "                elapsed +=\n"
            + "                    Time.unscaledDeltaTime;\n"
            + "\n"
            + "                float ratio =\n"
            + "                    scaledFadeDuration > 0f\n"
            + "                        ? Mathf.Clamp01(\n"
            + "                            elapsed / scaledFadeDuration)\n"
            + "                        : 1f;";

        static Day86BattleSpeedInstaller()
        {
            EditorApplication.delayCall +=
                ApplyAutomatically;
        }

        private static void ApplyAutomatically()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            ApplyChanges(
                false);
        }

        [MenuItem(MenuPath)]
        private static void ApplyFromMenu()
        {
            ApplyChanges(
                true);
        }

        private static void ApplyChanges(
            bool showAlreadyAppliedMessage)
        {
            bool changed =
                false;

            bool encounterSucceeded =
                ReplaceRequired(
                    EncounterPath,
                    EncounterOldText,
                    EncounterNewText,
                    1,
                    ref changed);

            bool bumpSucceeded =
                ReplaceRequired(
                    ParticipantSlotPath,
                    BumpOldText,
                    BumpNewText,
                    1,
                    ref changed);

            bool damageSucceeded =
                ReplaceRequired(
                    ParticipantSlotPath,
                    DamageOldText,
                    DamageNewText,
                    1,
                    ref changed);

            if (!encounterSucceeded
                || !bumpSucceeded
                || !damageSucceeded)
            {
                Debug.LogError(
                    "[Project Delta] 86일차 자동 적용 실패. 최신 main(85일차) 기준 파일인지 확인하세요.");

                return;
            }

            if (changed)
            {
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Project Delta] 86일차 전투 1×·2× 속도 자동 적용 완료. 재컴파일 후 전투에서 우측 아래 속도 버튼을 확인하세요.");

                return;
            }

            if (showAlreadyAppliedMessage)
            {
                Debug.Log(
                    "[Project Delta] 86일차 전투 속도 변경은 이미 적용되어 있습니다.");
            }
        }

        private static bool ReplaceRequired(
            string assetPath,
            string oldText,
            string newText,
            int expectedOldCount,
            ref bool changed)
        {
            string fullPath =
                BuildFullPath(
                    assetPath);

            if (!File.Exists(
                    fullPath))
            {
                Debug.LogError(
                    $"[Project Delta] 86일차 파일을 찾을 수 없습니다: {assetPath}");

                return false;
            }

            string original =
                File.ReadAllText(
                    fullPath);

            bool usesCrLf =
                original.Contains(
                    "\r\n",
                    StringComparison.Ordinal);

            string normalized =
                original.Replace(
                    "\r\n",
                    "\n");

            if (normalized.Contains(
                    newText,
                    StringComparison.Ordinal))
            {
                return true;
            }

            int oldCount =
                CountOccurrences(
                    normalized,
                    oldText);

            if (oldCount != expectedOldCount)
            {
                Debug.LogError(
                    $"[Project Delta] 86일차 수정 위치 불일치: {assetPath} / 예상 {expectedOldCount}개 / 실제 {oldCount}개");

                return false;
            }

            string updated =
                normalized.Replace(
                    oldText,
                    newText);

            if (usesCrLf)
            {
                updated =
                    updated.Replace(
                        "\n",
                        "\r\n");
            }

            File.WriteAllText(
                fullPath,
                updated,
                new UTF8Encoding(
                    false));

            changed =
                true;

            return true;
        }

        private static string BuildFullPath(
            string assetPath)
        {
            string projectRoot =
                Directory.GetParent(
                    UnityEngine.Application.dataPath)?.FullName;

            if (string.IsNullOrEmpty(
                    projectRoot))
            {
                return assetPath;
            }

            return Path.Combine(
                projectRoot,
                assetPath.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
        }

        private static int CountOccurrences(
            string source,
            string value)
        {
            if (string.IsNullOrEmpty(
                    source)
                || string.IsNullOrEmpty(
                    value))
            {
                return 0;
            }

            int count =
                0;

            int searchIndex =
                0;

            while (true)
            {
                int foundIndex =
                    source.IndexOf(
                        value,
                        searchIndex,
                        StringComparison.Ordinal);

                if (foundIndex < 0)
                {
                    return count;
                }

                count++;

                searchIndex =
                    foundIndex
                    + value.Length;
            }
        }
    }
}
