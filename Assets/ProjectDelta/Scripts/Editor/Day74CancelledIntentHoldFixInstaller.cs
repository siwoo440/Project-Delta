using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectDelta.Editor
{
    public static class Day74CancelledIntentHoldFixInstaller
    {
        private const string EncounterControllerPath =
            "Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs";

        [MenuItem("Project Delta/74일차/74일차 취소 Intent 재생성 방지 수정")]
        public static void Install()
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
                    EncounterControllerPath);

            bool changed =
                false;

            if (!source.Contains(
                    "BattleIntentService.HasPendingCancellation("))
            {
                const string oldBlock = @"            if (!BattleIntentService.TryGet(
                    actor.InstanceId,
                    out BattleIntent intent))
            {
                MonsterAiProfile profile =
";

                const string newBlock = @"            if (!BattleIntentService.TryGet(
                    actor.InstanceId,
                    out BattleIntent intent))
            {
                // 이미 예고가 취소된 Enemy는 그 취소된 차례를 먼저 소비한다.
                // 여기서 새 AI Intent를 만들면 예고 취소 직후 공격/방어로 바뀌는 문제가 생긴다.
                if (BattleIntentService.HasPendingCancellation(
                        actor.InstanceId))
                {
                    BattleIntentCancelReason pendingReason =
                        BattleIntentService.GetLastCancelReason(
                            actor.InstanceId);

                    return ResolveCancelledEnemyIntent(
                        actor,
                        pendingReason);
                }

                MonsterAiProfile profile =
";

                if (!source.Contains(
                        oldBlock))
                {
                    throw new InvalidOperationException(
                        "ExecuteEnemyIntent의 Intent 미보유 분기 위치를 찾지 못했습니다. 최신 74일차 소스인지 확인해 주세요.");
                }

                source =
                    source.Replace(
                        oldBlock,
                        newBlock);

                changed =
                    true;
            }

            if (!changed)
            {
                Debug.Log(
                    "[Project Delta] 74일차 취소 Intent 재생성 방지 수정은 이미 적용되어 있습니다.");

                return;
            }

            File.WriteAllText(
                EncounterControllerPath,
                source,
                new UTF8Encoding(
                    false));

            AssetDatabase.ImportAsset(
                EncounterControllerPath,
                ImportAssetOptions.ForceUpdate);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Project Delta] 74일차 취소 Intent 재생성 방지 수정 적용 완료");
        }
    }
}
