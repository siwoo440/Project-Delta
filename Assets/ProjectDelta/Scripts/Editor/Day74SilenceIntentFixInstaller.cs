using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectDelta.Editor
{
    public static class Day74SilenceIntentFixInstaller
    {
        private const string EncounterControllerPath =
            "Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs";

        [MenuItem("Project Delta/74일차/74일차 침묵 Intent 수정")]
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
                    "BattleIntentExecutionPolicy.EvaluateCurrentCancelReason("))
            {
                const string oldBlock = @"            if (intent == null)
            {
                return false;
            }

            switch (intent.CommandId)
";

                const string newBlock = @"            if (intent == null)
            {
                return false;
            }

            // 74일차 수정: HUD Update가 아직 돌지 않은 같은 프레임이라도 실제 실행 직전에
            // 현재 상태를 다시 검사하여 오래된 Skill Intent가 침묵을 무시하지 못하게 한다.
            BattleIntentCancelReason cancelReason =
                BattleIntentExecutionPolicy.EvaluateCurrentCancelReason(
                    battleSession.Context,
                    actor,
                    intent);

            if (cancelReason != BattleIntentCancelReason.None)
            {
                BattleIntentService.Cancel(
                    actor.InstanceId,
                    cancelReason);

                return ResolveCancelledEnemyIntent(
                    actor,
                    cancelReason);
            }

            switch (intent.CommandId)
";

                if (!source.Contains(
                        oldBlock))
                {
                    throw new InvalidOperationException(
                        "ExecuteEnemyIntent의 Intent 실행 위치를 찾지 못했습니다. 최신 74일차 파일인지 확인해 주세요.");
                }

                source =
                    source.Replace(
                        oldBlock,
                        newBlock);

                changed =
                    true;
            }

            if (!source.Contains(
                    "private bool ResolveCancelledEnemyIntent("))
            {
                const string insertMarker =
                    "        private bool TrySelectIntentTarget(";

                if (!source.Contains(
                        insertMarker))
                {
                    throw new InvalidOperationException(
                        "취소 Intent 처리 메서드 삽입 위치를 찾지 못했습니다.");
                }

                source =
                    source.Replace(
                        insertMarker,
                        CancelledIntentMethod
                        + "\n"
                        + insertMarker);

                changed =
                    true;
            }

            if (!changed)
            {
                Debug.Log(
                    "[Project Delta] 74일차 침묵 Intent 수정은 이미 적용되어 있습니다.");

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
                "[Project Delta] 74일차 침묵 Intent 수정 적용 완료");
        }

        private const string CancelledIntentMethod = @"        private bool ResolveCancelledEnemyIntent(
            BattleParticipant actor,
            BattleIntentCancelReason cancelReason)
        {
            if (actor == null)
            {
                return true;
            }

            // 취소된 예고를 다른 공격으로 교체하지 않는다.
            // 해당 Enemy의 행동만 소비하고 정상적인 다음 행동자/다음 라운드 흐름으로 넘긴다.
            if (!battleSession.TryBeginResolveAction())
            {
                Debug.LogError(
                    $""[Project Delta] 74일차 Intent 취소 턴 소비 실패 / Actor {actor.InstanceId} / Reason {cancelReason}"",
                    this);

                return true;
            }

            LastActingParticipant =
                actor;

            LastActionSequence++;

            string message =
                $""행동 예고 취소 / {actor.InstanceId} / {cancelReason}"";

            LastBattleActionResult =
                BattleActionResult.Accept(
                    ""IntentCancelled"",
                    new[] { message },
                    Array.Empty<BattleDamageChange>(),
                    Array.Empty<BattleParticipant>(),
                    false,
                    null);

            Debug.Log(
                $""[Project Delta] 74일차 {message}"",
                this);

            if (battleSession.HasPendingActorsThisRound)
            {
                return true;
            }

            if (!battleSession.TryEndRound())
            {
                return true;
            }

            if (BattleOutcomeEvaluator.TryEvaluate(
                    battleSession.Context,
                    out BattleOutcome roundEndOutcome))
            {
                FinishBattle(
                    roundEndOutcome);

                return true;
            }

            if (!battleSession.TryStartRound())
            {
                return true;
            }

            Debug.Log(
                $""[Project Delta] 74일차 Battle Round {battleSession.RoundNumber} Start / Intent 취소 후 진행"",
                this);

            return true;
        }
";
    }
}
