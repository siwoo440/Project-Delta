using ProjectDelta.Application;
using ProjectDelta.Domain;
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

        // 49~54일차에 실제 Command가 연결될 행동 버튼 자리.
        [Header("Action Buttons (49~54일차 연결 예정)")]
        [SerializeField] private Button[] actionButtons =
            new Button[0];

        [Header("Day 47 Test")]
        [SerializeField] private Button testNextTurnButton;
        [SerializeField] private Button testWinButton;
        [SerializeField] private Button testLoseButton;
        [SerializeField] private Button testDismissButton;

        private void Awake()
        {
            ResolveEncounterController();
            BindButtons();
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

            if (testDismissButton != null)
            {
                testDismissButton.onClick.AddListener(
                    OnTestDismissClicked);
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

            if (testDismissButton != null)
            {
                testDismissButton.onClick.RemoveListener(
                    OnTestDismissClicked);
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

        private void OnTestDismissClicked()
        {
            if (encounterController == null)
            {
                return;
            }

            encounterController.TestDismissFinishedBattle();
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
            battleStateText.text =
                result != null
                    ? $"Battle : {encounterController.CurrentBattleState} / Turn {result.TurnCount} / Outcome {result.Outcome}"
                    : $"Battle : {encounterController.CurrentBattleState} / Turn {encounterController.BattleTurnNumber}";
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
        }

        private void RefreshEnemySlots(
            BattleContext context)
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
            }
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

            // HP는 전투 참가자 데이터가 기준이다.
            if (player != null)
            {
                ApplyVital(
                    healthFillImage,
                    healthText,
                    "HP",
                    player.CurrentHp,
                    player.MaxHp);
            }

            // MP·SP는 전투 모델에 아직 없으므로 런 상태를 표시만 한다 (66~72일차 스킬 단계에서 연결).
            PlayerRunState playerState =
                RunContext.Current != null
                    ? RunContext.Current.Player
                    : null;

            if (playerState == null)
            {
                return;
            }

            StatBlock finalStats =
                playerState.GetFinalStats();

            ApplyVital(
                manaFillImage,
                manaText,
                "MP",
                playerState.CurrentMana,
                finalStats.MaxMana);

            ApplyVital(
                staminaFillImage,
                staminaText,
                "SP",
                playerState.CurrentStamina,
                finalStats.MaxStamina);
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
            // 실제 행동 Command는 49~54일차에 연결되므로 지금은 자리만 유지한다.
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

            bool isBattleActive =
                encounterController.IsBattleActive;

            if (testNextTurnButton != null)
            {
                testNextTurnButton.interactable =
                    encounterController.CurrentBattleState == BattleState.TurnStart;
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

            // 종료된 전투를 닫을 때만 사용한다.
            if (testDismissButton != null)
            {
                testDismissButton.interactable =
                    encounterController.IsBattleFinished;
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
