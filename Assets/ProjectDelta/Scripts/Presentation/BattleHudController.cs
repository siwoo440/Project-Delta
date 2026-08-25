using System.Collections.Generic;
using ProjectDelta.Application;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 47일차: 전투 화면 레이아웃을 담당한다.
    // 오른쪽 = 플레이어 상태 일러스트, 위쪽 = 적 슬롯 1~4(맨 왼쪽이 1번),
    // 왼쪽 가운데 아래 = 행동 버튼 자리, 그 위 = 캐릭터 체력바.
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

        // 49일차: 공격, 52일차: 방어 버튼을 실제로 연결한다.
        [Header("Action Buttons")]
        [SerializeField] private Button attackButton;
        [SerializeField] private Button defendButton;

        // 이후 일차에 실제 Command가 연결될 나머지 행동 버튼 자리 (행동·아이템·도주·유혹).
        // 유혹은 기획서에 있는 행동으로, 자리만 먼저 만들어두고 이후 일차에서 구현한다.
        [SerializeField] private Button[] actionButtons =
            new Button[0];

        [Header("Day 47 Test")]
        [SerializeField] private Button testNextTurnButton;
        [SerializeField] private Button testWinButton;
        [SerializeField] private Button testLoseButton;

        // 56일차: 적 턴이 버튼 없이 자동으로 진행돼 행동이 눈에 안 보이는 문제를 보완하기 위한
        // 마지막으로 재생한 행동 순번. encounterController.LastActionSequence와 달라지면
        // 그 사이에 새 행동이 있었다는 뜻이라 해당 슬롯에 튀어오르는 연출을 재생한다.
        private int lastPlayedActionSequence;

        private void Awake()
        {
            ResolveEncounterController();
            BindButtons();
            BindEnemySlotClicks();
            SetHudVisible(false);
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void Update()
        {
            ResolveEncounterController();

            // 종료 결과를 확인할 수 있도록 Finished 상태에서도 전투 화면을 유지한다.
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

            // 49일차: 공격, 52일차: 방어 버튼을 연결한다. 나머지는 이후 일차에 연결한다.
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
        }

        // 49일차: 적 슬롯 클릭 시 해당 슬롯 인덱스를 대상으로 선택한다.
        private void BindEnemySlotClicks()
        {
            if (enemySlots == null)
            {
                return;
            }

            for (int slotIndex = 0; slotIndex < enemySlots.Length; slotIndex++)
            {
                BattleParticipantSlotView slot =
                    enemySlots[slotIndex];

                if (slot == null)
                {
                    continue;
                }

                int capturedSlotIndex =
                    slotIndex; // 클로저 캡처용 지역 변수

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

        // 49일차: 적 슬롯을 클릭하면 해당 참가자를 공격 대상으로 지정(재지정)한다.
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

        // 49일차: 지정된 대상으로 공격을 확정한다.
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

        private void RefreshBattleState()
        {
            if (battleStateText == null)
            {
                return;
            }

            BattleResult result =
                encounterController.LastBattleResult;

            // 종료된 전투는 상태 대신 결과를 보여준다.
            if (result != null)
            {
                battleStateText.text =
                    $"Battle : {encounterController.CurrentBattleState} / Round {result.RoundCount} / Outcome {result.Outcome}";

                return;
            }

            // 48일차: 가장 최근에 행동한(또는 행동 중인) 참가자를 함께 보여준다.
            BattleParticipant actor =
                encounterController.CurrentBattleActor;

            string actorText =
                actor != null
                    ? $" / Actor {actor.InstanceId} (Speed {actor.Speed})"
                    : string.Empty;

            // 49일차: 대상 지정·공격 확정 결과 메시지를 함께 보여준다.
            // 59일차: 문자열 메시지 하나 대신 로그 목록(Logs)을 담는 BattleActionResult로 바뀌었다.
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

        // 56일차: 적 턴이 버튼 없이 자동으로 진행돼 행동이 눈에 안 보이는 문제를 보완한다.
        // 슬롯을 최신 상태로 바인딩한 뒤에 호출해야 방금 행동한 참가자의 슬롯을 정확히 찾을 수 있다.
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

            for (int slotIndex = 0; slotIndex < enemySlots.Length; slotIndex++)
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

            // 49일차: 지금 선택 가능한 대상·선택된 대상을 슬롯에 반영한다.
            IReadOnlyList<BattleParticipant> validTargets =
                encounterController.GetValidBattleTargets();

            BattleParticipant selectedTarget =
                encounterController.SelectedBattleTarget;

            for (int slotIndex = 0; slotIndex < enemySlots.Length; slotIndex++)
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
            for (int index = 0; index < participants.Count; index++)
            {
                if (participants[index] == participant)
                {
                    return true;
                }
            }

            return false;
        }

        // 45일차 탐험 빌보드와 같은 Resources 경로에서 몬스터 일러스트를 가져온다.
        private static Sprite LoadMonsterPortrait(
            string monsterDefinitionId)
        {
            string resourcePath =
                MonsterBillboardView.BuildResourcePath(
                    monsterDefinitionId);

            return string.IsNullOrEmpty(resourcePath)
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

            // 54일차: HP·MP·SP 모두 전투 참가자 데이터가 기준이다 (전투 중에는 참가자 값이
            // 최신이고, PlayerRunState는 전투가 끝날 때만 되돌아 맞춰진다).
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
            // 행동·방어·아이템·도주는 50~54일차에 연결되므로 지금은 자리만 유지한다.
            if (actionButtons != null)
            {
                foreach (Button actionButton in actionButtons)
                {
                    if (actionButton != null)
                    {
                        actionButton.interactable =
                            false;
                    }
                }
            }

            // 49일차: 대상이 지정된 AwaitingAction 상태에서만 공격을 확정할 수 있다.
            if (attackButton != null)
            {
                attackButton.interactable =
                    encounterController.CurrentBattleState == BattleState.AwaitingAction
                    && encounterController.SelectedBattleTarget != null;
            }

            // 52일차: 방어는 대상 선택이 필요 없어 AwaitingAction이기만 하면 바로 확정할 수 있다.
            if (defendButton != null)
            {
                defendButton.interactable =
                    encounterController.CurrentBattleState == BattleState.AwaitingAction;
            }

            bool isBattleActive =
                encounterController.IsBattleActive;

            if (testNextTurnButton != null)
            {
                // 48일차: 이번 라운드에 아직 행동할 참가자가 남아있을 때(RoundStart 또는 ResolvingAction)만 진행 가능.
                testNextTurnButton.interactable =
                    encounterController.CurrentBattleState == BattleState.RoundStart
                    || encounterController.CurrentBattleState == BattleState.ResolvingAction;
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
