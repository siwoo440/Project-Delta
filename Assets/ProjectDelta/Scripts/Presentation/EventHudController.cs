using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 108일차: 이벤트 본문·선택지·조건 미충족 사유·결과 메시지를 표시하는 최소 화면.
    // 실제로 이벤트를 트리거하는 방(114일차 예정 특수 방)이 아직 없어서,
    // Open(definition)을 외부에서 직접 호출해 연결하는 구조로 만들었다.
    [DisallowMultipleComponent]
    public sealed class EventHudController : MonoBehaviour
    {
        private const int MaxVisibleChoices = 6;

        [Header("Panel")]
        [SerializeField]
        private GameObject eventPanel;

        [SerializeField]
        private Text titleText;

        [SerializeField]
        private Text bodyText;

        [SerializeField]
        private Text resultMessageText;

        [Header("Choices")]
        [SerializeField]
        private Button[] choiceButtons =
            new Button[MaxVisibleChoices];

        [SerializeField]
        private Text[] choiceButtonTexts =
            new Text[MaxVisibleChoices];

        // 결과가 확정된 뒤 플레이어가 메시지를 읽고 직접 닫을 수 있게 한다.
        // 선택 즉시 패널을 닫아버리면 결과 메시지를 볼 틈이 없기 때문이다.
        [SerializeField]
        private Button closeButton;

        private bool isResolved;

        // 상호작용 중 탐험 이동을 잠그고 싶을 때만 연결한다(선택 사항).
        [Header("Movement Lock (Optional)")]
        [SerializeField]
        private PlayerGridMovementController movementController;

        private EventDefinition currentEvent;

        private void Awake()
        {
            HookButtons();
            Close();
        }

        public void Open(
            EventDefinition eventDefinition)
        {
            if (eventDefinition == null)
            {
                return;
            }

            currentEvent =
                eventDefinition;

            isResolved =
                false;

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(
                    false);
            }

            if (eventPanel != null)
            {
                eventPanel.SetActive(
                    true);
            }

            if (movementController != null)
            {
                movementController.IsInputLocked =
                    true;
            }

            if (titleText != null)
            {
                titleText.text =
                    eventDefinition.Title;
            }

            if (bodyText != null)
            {
                bodyText.text =
                    eventDefinition.Body;
            }

            if (resultMessageText != null)
            {
                resultMessageText.text =
                    string.Empty;
            }

            RefreshChoices();
        }

        public void Close()
        {
            currentEvent =
                null;

            if (eventPanel != null)
            {
                eventPanel.SetActive(
                    false);
            }

            if (movementController != null)
            {
                movementController.IsInputLocked =
                    false;
            }
        }

        private void HookButtons()
        {
            if (choiceButtons == null)
            {
                return;
            }

            for (int index = 0;
                 index < choiceButtons.Length;
                 index++)
            {
                Button button =
                    choiceButtons[index];

                if (button == null)
                {
                    continue;
                }

                int capturedIndex =
                    index;

                button.onClick.RemoveAllListeners();

                button.onClick.AddListener(
                    () => OnChoiceClicked(
                        capturedIndex));
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();

                closeButton.onClick.AddListener(
                    Close);
            }
        }

        private void RefreshChoices()
        {
            IReadOnlyList<EventChoiceDefinition> choices =
                currentEvent != null
                    ? currentEvent.Choices
                    : System.Array.Empty<EventChoiceDefinition>();

            RunContext context =
                RunContext.Current;

            for (int index = 0;
                 index < MaxVisibleChoices;
                 index++)
            {
                bool hasChoice =
                    index < choices.Count;

                EventChoiceDefinition choice =
                    hasChoice
                        ? choices[index]
                        : null;

                EventChoiceAvailabilityResult availability =
                    hasChoice
                        ? EventConditionService.Evaluate(
                            choice,
                            context)
                        : null;

                if (choiceButtons != null
                    && index < choiceButtons.Length
                    && choiceButtons[index] != null)
                {
                    choiceButtons[index].gameObject.SetActive(
                        hasChoice);

                    choiceButtons[index].interactable =
                        hasChoice
                        && !isResolved
                        && availability.IsAvailable;
                }

                if (choiceButtonTexts != null
                    && index < choiceButtonTexts.Length
                    && choiceButtonTexts[index] != null
                    && hasChoice)
                {
                    choiceButtonTexts[index].text =
                        availability.IsAvailable
                            ? choice.ChoiceText
                            : $"{choice.ChoiceText} ({availability.UnavailableReason})";
                }
            }
        }

        private void OnChoiceClicked(
            int index)
        {
            if (currentEvent == null
                || isResolved)
            {
                return;
            }

            IReadOnlyList<EventChoiceDefinition> choices =
                currentEvent.Choices;

            if (index < 0
                || index >= choices.Count)
            {
                return;
            }

            EventChoiceDefinition choice =
                choices[index];

            RunContext context =
                RunContext.Current;

            EventChoiceAvailabilityResult availability =
                EventConditionService.Evaluate(
                    choice,
                    context);

            if (!availability.IsAvailable)
            {
                if (resultMessageText != null)
                {
                    resultMessageText.text =
                        availability.UnavailableReason;
                }

                return;
            }

            EventResultApplicationResult result =
                EventResultService.ApplyChoice(
                    currentEvent,
                    choice,
                    context);

            if (resultMessageText != null)
            {
                resultMessageText.text =
                    BuildResultMessage(
                        result);
            }

            // 108일차: 결과 확정 후에도 메시지를 읽을 수 있도록 즉시 닫지 않는다.
            // 선택지를 전부 비활성화하고 닫기 버튼을 눌러야 탐험으로 복귀한다.
            isResolved =
                true;

            RefreshChoices();

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(
                    true);
            }
        }

        private static string BuildResultMessage(
            EventResultApplicationResult result)
        {
            if (result == null)
            {
                return "결과를 적용할 수 없습니다.";
            }

            if (!result.Success)
            {
                return result.FailureReason
                        == EventResultFailureReason.AlreadyResolved
                    ? "이미 결과가 적용된 이벤트입니다."
                    : "결과를 적용할 수 없습니다.";
            }

            return result.AppliedEffectSummaries.Count > 0
                ? string.Join(
                    " / ",
                    result.AppliedEffectSummaries)
                : "선택을 확정했습니다.";
        }
    }
}
