using ProjectDelta.Application;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 84일차: 최대 4명 적의 행동 예고를 전투 HUD에서 즉시 읽을 수 있도록 표시를 정리한다.
    [DisallowMultipleComponent]
    public sealed class BattleIntentHudController : MonoBehaviour
    {
        [SerializeField] private ExplorationMonsterEncounterController encounterController;
        [SerializeField] private GameObject intentRoot;
        [SerializeField] private Text[] intentTexts =
            new Text[0];

        private void Awake()
        {
            ResolveEncounterController();
            SetVisible(
                false);
        }

        private void Update()
        {
            ResolveEncounterController();

            bool shouldShow =
                encounterController != null
                && encounterController.HasBattle
                && encounterController.CurrentBattleContext != null;

            SetVisible(
                shouldShow);

            if (!shouldShow)
            {
                ClearTexts();
                return;
            }

            RefreshIntents(
                encounterController.CurrentBattleContext);
        }

        private void ResolveEncounterController()
        {
            if (encounterController != null)
            {
                return;
            }

            encounterController =
                FindFirstObjectByType<ExplorationMonsterEncounterController>();
        }

        private void RefreshIntents(
            BattleContext context)
        {
            if (intentTexts == null)
            {
                return;
            }

            for (int slotIndex = 0;
                 slotIndex < intentTexts.Length;
                 slotIndex++)
            {
                Text intentText =
                    intentTexts[slotIndex];

                if (intentText == null)
                {
                    continue;
                }

                if (context == null
                    || !context.TryGetEnemyAtSlot(
                        slotIndex,
                        out BattleParticipant enemy)
                    || enemy == null
                    || !enemy.IsAlive)
                {
                    intentText.text =
                        string.Empty;

                    continue;
                }

                if (!BattleIntentService.TryGet(
                        enemy.InstanceId,
                        out BattleIntent intent))
                {
                    BattleIntentCancelReason reason =
                        BattleIntentService.GetLastCancelReason(
                            enemy.InstanceId);

                    intentText.text =
                        reason != BattleIntentCancelReason.None
                            ? $"[취소] {GetCancelReasonLabel(reason)}"
                            : "[예고 없음]";

                    continue;
                }

                string targetLabel =
                    string.IsNullOrEmpty(
                        intent.TargetInstanceId)
                        ? string.Empty
                        : $"\n→ {GetTargetLabel(intent.TargetInstanceId)}";

                intentText.text =
                    $"[{GetIconLabel(intent.IconType)}] {intent.DisplayName}{targetLabel}";
            }
        }

        private void ClearTexts()
        {
            if (intentTexts == null)
            {
                return;
            }

            foreach (Text intentText in intentTexts)
            {
                if (intentText != null)
                {
                    intentText.text =
                        string.Empty;
                }
            }
        }

        private void SetVisible(
            bool visible)
        {
            if (intentRoot != null
                && intentRoot.activeSelf != visible)
            {
                intentRoot.SetActive(
                    visible);
            }
        }

        private static string GetTargetLabel(
            string targetInstanceId)
        {
            return targetInstanceId == "PLAYER"
                ? "플레이어"
                : targetInstanceId;
        }

        private static string GetIconLabel(
            BattleIntentIconType iconType)
        {
            switch (iconType)
            {
                case BattleIntentIconType.Attack:
                    return "공격";

                case BattleIntentIconType.Defend:
                    return "방어";

                case BattleIntentIconType.Buff:
                    return "강화";

                case BattleIntentIconType.Debuff:
                    return "약화";

                case BattleIntentIconType.Status:
                    return "상태";

                case BattleIntentIconType.Heal:
                    return "회복";

                case BattleIntentIconType.Special:
                    return "특수";

                default:
                    return "?";
            }
        }

        private static string GetCancelReasonLabel(
            BattleIntentCancelReason reason)
        {
            switch (reason)
            {
                case BattleIntentCancelReason.Stunned:
                    return "기절";

                case BattleIntentCancelReason.Silenced:
                    return "침묵";

                case BattleIntentCancelReason.ActorDefeated:
                    return "사망";

                case BattleIntentCancelReason.Satisfied:
                    return "만족 상태";

                case BattleIntentCancelReason.TargetUnavailable:
                    return "대상 부재";

                default:
                    return "알 수 없음";
            }
        }
    }
}
