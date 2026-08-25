using ProjectDelta.Application;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BattleRewardPanelController : MonoBehaviour
    {
        [Header("Battle")]
        [SerializeField] private ExplorationMonsterEncounterController encounterController;

        [Header("Reward UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button[] rewardButtons =
            new Button[0];
        [SerializeField] private Text[] rewardTexts =
            new Text[0];

        private bool wasVisible;

        private void Awake()
        {
            ResolveEncounterController();
            BindButtons();
            SetPanelVisible(
                false);
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void Update()
        {
            ResolveEncounterController();

            bool shouldShow =
                encounterController != null
                && encounterController.IsBattleRewardPending
                && BattleRewardState.IsPending;

            SetPanelVisible(
                shouldShow);

            if (shouldShow
                && !wasVisible)
            {
                RefreshOptions();
            }

            wasVisible =
                shouldShow;
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

        private void BindButtons()
        {
            if (rewardButtons == null)
            {
                return;
            }

            for (int index = 0; index < rewardButtons.Length; index++)
            {
                Button button =
                    rewardButtons[index];

                if (button == null)
                {
                    continue;
                }

                int capturedIndex =
                    index;

                button.onClick.AddListener(
                    () => OnRewardClicked(
                        capturedIndex));
            }
        }

        private void UnbindButtons()
        {
            if (rewardButtons == null)
            {
                return;
            }

            for (int index = 0; index < rewardButtons.Length; index++)
            {
                Button button =
                    rewardButtons[index];

                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
            }
        }

        private void RefreshOptions()
        {
            for (int index = 0; index < rewardButtons.Length; index++)
            {
                bool hasOption =
                    index < BattleRewardState.CurrentOptions.Count;

                Button button =
                    rewardButtons[index];

                Text rewardText =
                    rewardTexts != null
                    && index < rewardTexts.Length
                        ? rewardTexts[index]
                        : null;

                if (button != null)
                {
                    button.gameObject.SetActive(
                        hasOption);

                    button.interactable =
                        hasOption;
                }

                if (rewardText != null)
                {
                    rewardText.text =
                        hasOption
                            ? BattleRewardState.CurrentOptions[index].DisplayName
                            : string.Empty;
                }
            }
        }

        private void OnRewardClicked(
            int optionIndex)
        {
            if (encounterController == null
                || !BattleRewardState.IsPending
                || optionIndex < 0
                || optionIndex >= BattleRewardState.CurrentOptions.Count)
            {
                return;
            }

            BattleRewardOption option =
                BattleRewardState.CurrentOptions[optionIndex];

            encounterController.ConfirmBattleReward(
                option.Id);
        }

        private void SetPanelVisible(
            bool visible)
        {
            if (panelRoot == null)
            {
                return;
            }

            if (panelRoot.activeSelf != visible)
            {
                panelRoot.SetActive(
                    visible);
            }
        }
    }
}
