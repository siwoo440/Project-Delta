using ProjectDelta.Application;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    [DisallowMultipleComponent]
    public sealed class DefeatSceneController : MonoBehaviour
    {
        [Header("Defeat UI")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text reasonText;
        [SerializeField] private Text floorText;
        [SerializeField] private Text roundText;
        [SerializeField] private Text attackerText;
        [SerializeField] private Button returnToTitleButton;

        private void Awake()
        {
            BindButton();
            RefreshView();
        }

        private void OnDestroy()
        {
            UnbindButton();
        }

        private void BindButton()
        {
            if (returnToTitleButton == null)
            {
                return;
            }

            returnToTitleButton.onClick.AddListener(
                ReturnToTitle);
        }

        private void UnbindButton()
        {
            if (returnToTitleButton == null)
            {
                return;
            }

            returnToTitleButton.onClick.RemoveListener(
                ReturnToTitle);
        }

        private void RefreshView()
        {
            if (titleText != null)
            {
                titleText.text =
                    "패배";
            }

            RunDefeatSummary summary =
                DefeatSceneState.Current;

            if (summary == null)
            {
                ApplyMissingSummary();
                return;
            }

            if (reasonText != null)
            {
                reasonText.text =
                    summary.Reason == BattleDefeatReason.Surrender
                        ? "패배 원인 : 항복"
                        : "패배 원인 : 전투 패배";
            }

            if (floorText != null)
            {
                floorText.text =
                    $"도달 층 : {summary.FloorNumber}층";
            }

            if (roundText != null)
            {
                roundText.text =
                    $"패배 라운드 : {summary.RoundNumber} Round";
            }

            if (attackerText != null)
            {
                attackerText.text =
                    BuildAttackerText(
                        summary);
            }
        }

        private void ApplyMissingSummary()
        {
            if (reasonText != null)
            {
                reasonText.text =
                    "패배 정보를 찾을 수 없습니다.";
            }

            if (floorText != null)
            {
                floorText.text =
                    "도달 층 : -";
            }

            if (roundText != null)
            {
                roundText.text =
                    "패배 라운드 : -";
            }

            if (attackerText != null)
            {
                attackerText.text =
                    "마지막 공격자 : -";
            }
        }

        private static string BuildAttackerText(
            RunDefeatSummary summary)
        {
            if (summary.Reason == BattleDefeatReason.Surrender)
            {
                return "마지막 공격자 : 없음";
            }

            if (!string.IsNullOrEmpty(
                    summary.AttackerDefinitionId))
            {
                return $"마지막 공격자 : {summary.AttackerDefinitionId}";
            }

            if (!string.IsNullOrEmpty(
                    summary.AttackerInstanceId))
            {
                return $"마지막 공격자 : {summary.AttackerInstanceId}";
            }

            return "마지막 공격자 : 확인 불가";
        }

        private void ReturnToTitle()
        {
            ApplicationFlow.Current?.ReturnToTitle();
        }
    }
}
