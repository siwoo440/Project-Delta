using ProjectDelta.Application;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EncounterPanelController : MonoBehaviour
    {
        [Header("Encounter")]
        [SerializeField] private ExplorationMonsterEncounterController encounterController;
        [SerializeField] private GameObject panelRoot;

        [Header("Target Info")]
        [SerializeField] private Text stateText;
        [SerializeField] private Text monsterIdText;
        [SerializeField] private Text roomIdText;
        [SerializeField] private Text gridPositionText;
        [SerializeField] private Text resultText;

        [Header("Actions")]
        [SerializeField] private Button battleButton;
        [SerializeField] private Button escapeButton;
        [SerializeField] private Button testEndButton;

        private bool wasVisible;

        private void Awake()
        {
            ResolveEncounterController();
            BindButtons();
            SetPanelVisible(false);
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
                && encounterController.CurrentState == EncounterState.Active;

            SetPanelVisible(
                shouldShow);

            if (!shouldShow)
            {
                wasVisible =
                    false;

                return;
            }

            if (!wasVisible)
            {
                wasVisible =
                    true;

                SetResultText(
                    string.Empty);
            }

            RefreshTargetInfo();
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
            if (battleButton != null)
            {
                battleButton.onClick.AddListener(
                    OnBattleClicked);
            }

            if (escapeButton != null)
            {
                escapeButton.onClick.AddListener(
                    OnEscapeClicked);
            }

            if (testEndButton != null)
            {
                testEndButton.onClick.AddListener(
                    OnTestEndClicked);
            }
        }

        private void UnbindButtons()
        {
            if (battleButton != null)
            {
                battleButton.onClick.RemoveListener(
                    OnBattleClicked);
            }

            if (escapeButton != null)
            {
                escapeButton.onClick.RemoveListener(
                    OnEscapeClicked);
            }

            if (testEndButton != null)
            {
                testEndButton.onClick.RemoveListener(
                    OnTestEndClicked);
            }
        }

        private void OnBattleClicked()
        {
            if (encounterController == null)
            {
                return;
            }

            ShowCommandResult(
                encounterController.SelectBattleCommand());
        }

        private void OnEscapeClicked()
        {
            if (encounterController == null)
            {
                return;
            }

            ShowCommandResult(
                encounterController.SelectEscapeCommand());
        }

        private void OnTestEndClicked()
        {
            if (encounterController == null)
            {
                return;
            }

            encounterController.CompleteTestEncounter();
        }

        private void RefreshTargetInfo()
        {
            if (encounterController == null)
            {
                return;
            }

            EncounterContext context =
                encounterController.CurrentContext;

            if (stateText != null)
            {
                stateText.text =
                    $"State : {encounterController.CurrentState}";
            }

            if (context == null)
            {
                if (monsterIdText != null)
                {
                    monsterIdText.text =
                        "Monster : -";
                }

                if (roomIdText != null)
                {
                    roomIdText.text =
                        "Room : -";
                }

                if (gridPositionText != null)
                {
                    gridPositionText.text =
                        "Grid : -";
                }

                return;
            }

            if (monsterIdText != null)
            {
                monsterIdText.text =
                    $"Monster : {context.MonsterDefinitionId}";
            }

            if (roomIdText != null)
            {
                roomIdText.text =
                    $"Room : {context.RoomId}";
            }

            if (gridPositionText != null)
            {
                gridPositionText.text =
                    $"Grid : {context.MonsterGridPosition}";
            }
        }

        private void ShowCommandResult(
            EncounterCommandResult result)
        {
            if (result == null)
            {
                SetResultText(
                    string.Empty);

                return;
            }

            string prefix =
                result.Accepted
                    ? "선택"
                    : "실패";

            SetResultText(
                $"{prefix} : {result.Message}");
        }

        private void SetResultText(
            string message)
        {
            if (resultText != null)
            {
                resultText.text =
                    message;
            }
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
