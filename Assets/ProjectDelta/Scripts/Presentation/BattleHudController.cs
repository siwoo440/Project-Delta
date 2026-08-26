using System.Collections.Generic;
using ProjectDelta.Application;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 47일차: 전투 화면 레이아웃을 담당한다.
    [DisallowMultipleComponent]
    public sealed class BattleHudController : MonoBehaviour
    {
        [Header("Battle")]
        [SerializeField] private ExplorationMonsterEncounterController encounterController;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private Text battleStateText;

        [Header("Enemy Slots (맨 왼쪽이 1번)")]
        [SerializeField] private BattleParticipantSlotView[] enemySlots =
            new BattleParticipantSlotView[BattleContext.MaxEnemySlots];

        [Header("Player Status")]
        [SerializeField] private BattleParticipantSlotView playerSlot;
        [SerializeField] private Sprite playerPortrait;

        [Header("Player Vitals (행동 버튼 위)")]
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Text healthText;
        [SerializeField] private Image manaFillImage;
        [SerializeField] private Text manaText;
        [SerializeField] private Image staminaFillImage;
        [SerializeField] private Text staminaText;

        [Header("Action Buttons")]
        [SerializeField] private Button attackButton;
        [SerializeField] private Button defendButton;

        // 83일차: 기존 씬에는 전용 직렬화 참조가 없으므로 actionButtons에서 자동 탐색한다.
        [SerializeField] private Button fleeButton;

        [SerializeField] private Button[] actionButtons =
            new Button[0];

        [Header("Day 47 Test")]
        [SerializeField] private Button testNextTurnButton;
        [SerializeField] private Button testWinButton;
        [SerializeField] private Button testLoseButton;

        private int lastPlayedActionSequence;

        private void Awake()
        {
            ResolveEncounterController();
            ResolveFleeButtonReference();
            BindButtons();
            BindEnemySlotClicks();
            SetHudVisible(
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
                && encounterController.HasBattle;

            SetHudVisible(
                shouldShow);

            if (!shouldShow)
            {
                return;
            }

            RefreshBattleState();
            RefreshParticipants();
            RefreshPlayerVitals();
            RefreshButtons();
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

        private void ResolveFleeButtonReference()
        {
            if (fleeButton != null)
            {
                return;
            }

            fleeButton =
                BattleHudActionButtonResolver.ResolveFleeButton(
                    fleeButton,
                    actionButtons);
        }

        private void BindButtons()
        {
            if (testNextTurnButton != null)
            {
                testNextTurnButton.onClick.AddListener(
                    OnTestNextTurnClicked);
            }

            if (testWinButton != null)
            {
                testWinButton.onClick.AddListener(
                    OnTestWinClicked);
            }

            if (testLoseButton != null)
            {
                testLoseButton.onClick.AddListener(
                    OnTestLoseClicked);
            }

            if (attackButton != null)
            {
                attackButton.onClick.AddListener(
                    OnAttackButtonClicked);
            }

            if (defendButton != null)
            {
                defendButton.onClick.AddListener(
                    OnDefendButtonClicked);
            }

            // 83일차: 69일차에 구현된 ConfirmFlee를 실제 HUD 버튼에 연결한다.
            if (fleeButton != null)
            {
                fleeButton.onClick.AddListener(
                    OnFleeButtonClicked);
            }
        }

        private void BindEnemySlotClicks()
        {
            if (enemySlots == null)
            {
                return;
            }

            for (int slotIndex = 0;
                 slotIndex < enemySlots.Length;
                 slotIndex++)
            {
                BattleParticipantSlotView slot =
                    enemySlots[slotIndex];

                if (slot == null)
                {
                    continue;
                }

                int capturedSlotIndex =
                    slotIndex;

                slot.SetOnClick(
                    () => OnEnemySlotClicked(
                        capturedSlotIndex));
            }
        }

        private void UnbindButtons()
        {
            if (testNextTurnButton != null)
            {
                testNextTurnButton.onClick.RemoveListener(
                    OnTestNextTurnClicked);
            }

            if (testWinButton != null)
            {
                testWinButton.onClick.RemoveListener(
                    OnTestWinClicked);
            }

            if (testLoseButton != null)
            {
                testLoseButton.onClick.RemoveListener(
                    OnTestLoseClicked);
            }

            if (attackButton != null)
            {
                attackButton.onClick.RemoveListener(
                    OnAttackButtonClicked);
            }

            if (defendButton != null)
            {
                defendButton.onClick.RemoveListener(
                    OnDefendButtonClicked);
            }

            if (fleeButton != null)
            {
                fleeButton.onClick.RemoveListener(
                    OnFleeButtonClicked);
            }
        }

        private void OnTestNextTurnClicked()
        {
            if (encounterController == null)
            {
                return;
            }

            encounterController.TestAdvanceBattleTurn();
        }

        private void OnTestWinClicked()
        {
            if (encounterController == null)
            {
                return;
            }

            encounterController.TestWinBattle();
        }

        private void OnTestLoseClicked()
        {
            if (encounterController == null)
            {
                return;
            }

            encounterController.TestLoseBattle();
        }

        private void OnEnemySlotClicked(
            int slotIndex)
        {
            if (encounterController == null)
            {
                return;
            }

            BattleContext context =
                encounterController.CurrentBattleContext;

            if (context == null
                || !context.TryGetEnemyAtSlot(
                    slotIndex,
                    out BattleParticipant enemy))
            {
                return;
            }

            encounterController.TrySelectBattleTarget(
                enemy);
        }

        private void OnAttackButtonClicked()
        {
            if (encounterController == null)
            {
                return;
            }

            encounterController.ConfirmAttack();
        }

        private void OnDefendButtonClicked()
        {
            if (encounterController == null)
            {
                return;
            }

            encounterController.ConfirmDefend();
        }

        private void OnFleeButtonClicked()
        {
            if (encounterController == null)
            {
                return;
            }

            encounterController.ConfirmFlee();
        }

        private void RefreshBattleState()
        {
            if (battleStateText == null)
            {
                return;
            }

            BattleResult result =
                encounterController.LastBattleResult;

            if (result != null)
            {
                battleStateText.text =
                    $"Battle : {encounterController.CurrentBattleState} / Round {result.RoundCount} / Outcome {result.Outcome}";

                return;
            }

            BattleParticipant actor =
                encounterController.CurrentBattleActor;

            string actorText =
                actor != null
                    ? $" / Actor {actor.InstanceId} (Speed {actor.Speed})"
                    : string.Empty;

            BattleActionResult actionResult =
                encounterController.LastBattleActionResult;

            string commandText =
                actionResult != null
                && actionResult.Logs != null
                && actionResult.Logs.Count > 0
                    ? $"\n{string.Join("\n", actionResult.Logs)}"
                    : string.Empty;

            battleStateText.text =
                $"Battle : {encounterController.CurrentBattleState} / Round {encounterController.BattleRoundNumber}{actorText}{commandText}";
        }

        private void RefreshParticipants()
        {
            BattleContext context =
                encounterController.CurrentBattleContext;

            RefreshEnemySlots(
                context);

            if (playerSlot != null)
            {
                playerSlot.Bind(
                    context != null
                        ? context.Player
                        : null,
                    playerPortrait);
            }

            PlayActionBumpIfNewActionHappened(
                context);
        }

        private void PlayActionBumpIfNewActionHappened(
            BattleContext context)
        {
            int currentSequence =
                encounterController.LastActionSequence;

            if (currentSequence == lastPlayedActionSequence)
            {
                return;
            }

            lastPlayedActionSequence =
                currentSequence;

            BattleParticipant actor =
                encounterController.LastActingParticipant;

            if (context == null
                || actor == null)
            {
                return;
            }

            if (actor == context.Player)
            {
                playerSlot?.PlayActionBump();

                return;
            }

            if (enemySlots == null)
            {
                return;
            }

            for (int slotIndex = 0;
                 slotIndex < enemySlots.Length;
                 slotIndex++)
            {
                if (context.TryGetEnemyAtSlot(
                        slotIndex,
                        out BattleParticipant enemy)
                    && enemy == actor)
                {
                    enemySlots[slotIndex]?.PlayActionBump();

                    return;
                }
            }
        }

        private void RefreshEnemySlots(
            BattleContext context)
        {
            if (enemySlots == null)
            {
                return;
            }

            IReadOnlyList<BattleParticipant> validTargets =
                encounterController.GetValidBattleTargets();

            BattleParticipant selectedTarget =
                encounterController.SelectedBattleTarget;

            for (int slotIndex = 0;
                 slotIndex < enemySlots.Length;
                 slotIndex++)
            {
                BattleParticipantSlotView slot =
                    enemySlots[slotIndex];

                if (slot == null)
                {
                    continue;
                }

                if (context == null
                    || !context.TryGetEnemyAtSlot(
                        slotIndex,
                        out BattleParticipant enemy))
                {
                    slot.Clear();
                    continue;
                }

                slot.Bind(
                    enemy,
                    LoadMonsterPortrait(
                        enemy.DefinitionId));

                slot.SetSelectable(
                    ContainsParticipant(
                        validTargets,
                        enemy));

                slot.SetSelected(
                    enemy == selectedTarget);
            }
        }

        private static bool ContainsParticipant(
            IReadOnlyList<BattleParticipant> participants,
            BattleParticipant participant)
        {
            for (int index = 0;
                 index < participants.Count;
                 index++)
            {
                if (participants[index] == participant)
                {
                    return true;
                }
            }

            return false;
        }

        private static Sprite LoadMonsterPortrait(
            string monsterDefinitionId)
        {
            string resourcePath =
                MonsterBillboardView.BuildResourcePath(
                    monsterDefinitionId);

            return string.IsNullOrEmpty(
                    resourcePath)
                ? null
                : Resources.Load<Sprite>(
                    resourcePath);
        }

        private void RefreshPlayerVitals()
        {
            BattleContext context =
                encounterController.CurrentBattleContext;

            BattleParticipant player =
                context != null
                    ? context.Player
                    : null;

            if (player == null)
            {
                return;
            }

            ApplyVital(
                healthFillImage,
                healthText,
                "HP",
                player.CurrentHp,
                player.MaxHp);

            ApplyVital(
                manaFillImage,
                manaText,
                "MP",
                player.CurrentMana,
                player.MaxMana);

            ApplyVital(
                staminaFillImage,
                staminaText,
                "SP",
                player.CurrentStamina,
                player.MaxStamina);
        }

        private static void ApplyVital(
            Image fillImage,
            Text valueText,
            string label,
            int current,
            int max)
        {
            float ratio =
                max > 0
                    ? Mathf.Clamp01(
                        current / (float)max)
                    : 0f;

            if (fillImage != null)
            {
                fillImage.fillAmount =
                    ratio;
            }

            if (valueText != null)
            {
                valueText.text =
                    $"{label}  {current} / {max}";
            }
        }

        private void RefreshButtons()
        {
            ResolveFleeButtonReference();

            // 아직 구현되지 않은 공용 행동 버튼은 기본적으로 비활성화한다.
            if (actionButtons != null)
            {
                foreach (Button actionButton
                         in actionButtons)
                {
                    if (actionButton != null)
                    {
                        actionButton.interactable =
                            false;
                    }
                }
            }

            if (attackButton != null)
            {
                attackButton.interactable =
                    encounterController.CurrentBattleState
                        == BattleState.AwaitingAction
                    && encounterController.SelectedBattleTarget
                        != null;
            }

            if (defendButton != null)
            {
                defendButton.interactable =
                    encounterController.CurrentBattleState
                        == BattleState.AwaitingAction;
            }

            // 83일차: 도주는 플레이어 행동 차례에서만 활성화한다.
            // actionButtons 전체 비활성화 뒤 다시 설정하므로 매 프레임 false로 덮이던 문제를 해소한다.
            if (fleeButton != null)
            {
                BattleParticipant actor =
                    encounterController.CurrentBattleActor;

                fleeButton.interactable =
                    encounterController.CurrentBattleState
                        == BattleState.AwaitingAction
                    && actor != null
                    && actor.Team == BattleTeam.Player;
            }

            bool isBattleActive =
                encounterController.IsBattleActive;

            if (testNextTurnButton != null)
            {
                testNextTurnButton.interactable =
                    encounterController.CurrentBattleState
                        == BattleState.RoundStart
                    || encounterController.CurrentBattleState
                        == BattleState.ResolvingAction;
            }

            if (testWinButton != null)
            {
                testWinButton.interactable =
                    isBattleActive;
            }

            if (testLoseButton != null)
            {
                testLoseButton.interactable =
                    isBattleActive;
            }
        }

        private void SetHudVisible(
            bool visible)
        {
            if (hudRoot == null)
            {
                return;
            }

            if (hudRoot.activeSelf != visible)
            {
                hudRoot.SetActive(
                    visible);
            }
        }
    }
}
