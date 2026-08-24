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

            // 47일차: 전투가 시작되면 행동 선택 패널을 숨기고 BattleHud로 화면을 넘긴다.
            bool shouldShow =
                encounterController != null
                && encounterController.CurrentState == EncounterState.Active
                && !encounterController.HasBattle;

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
            RefreshActionState();
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

        private void RefreshActionState()
        {
            if (encounterController == null)
            {
                SetActionButtonsInteractable(
                    false);

                SetTestEndButtonInteractable(
                    false);

                return;
            }

            EncounterActionAvailability availability =
                encounterController.GetActionAvailability();

            SetActionButtonsInteractable(
                availability.CanSelect);

            SetTestEndButtonInteractable(
                encounterController.HasSelectedEncounterAction
                && !encounterController.HasBattle);

            EncounterCommandResult lastResult =
                encounterController.LastCommandResult;

            if (lastResult != null)
            {
                string prefix =
                    lastResult.Accepted
                        ? "선택"
                        : "실패";

                if (!availability.CanSelect
                    && !string.IsNullOrEmpty(availability.Reason)
                    && lastResult.Message != availability.Reason)
                {
                    SetResultText(
                        $"{prefix} : {lastResult.Message}\n{availability.Reason}");
                }
                else
                {
                    SetResultText(
                        $"{prefix} : {lastResult.Message}");
                }

                return;
            }

            if (!availability.CanSelect)
            {
                SetResultText(
                    availability.Reason);
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

        private void SetActionButtonsInteractable(
            bool interactable)
        {
            if (battleButton != null)
            {
                battleButton.interactable =
                    interactable;
            }

            if (escapeButton != null)
            {
                escapeButton.interactable =
                    interactable;
            }
        }

        private void SetTestEndButtonInteractable(
            bool interactable)
        {
            if (testEndButton != null)
            {
                testEndButton.interactable =
                    interactable;
            }
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
