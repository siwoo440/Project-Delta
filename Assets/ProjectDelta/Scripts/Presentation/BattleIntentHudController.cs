using ProjectDelta.Application;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
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

            for (int slotIndex = 0; slotIndex < intentTexts.Length; slotIndex++)
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
                        : $"\n→ {intent.TargetInstanceId}";

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

        private static string GetIconLabel(
            BattleIntentIconType iconType)
        {
            switch (iconType)
            {
                case BattleIntentIconType.Attack:
                    return "ATK";

                case BattleIntentIconType.Defend:
                    return "DEF";

                case BattleIntentIconType.Buff:
                    return "BUFF";

                case BattleIntentIconType.Debuff:
                    return "DEBUFF";

                case BattleIntentIconType.Status:
                    return "STATUS";

                case BattleIntentIconType.Heal:
                    return "HEAL";

                case BattleIntentIconType.Special:
                    return "SPECIAL";

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
