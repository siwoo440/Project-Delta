using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 탐험과 전투 모두에서 같은 위치의 플레이어 자원 HUD를 유지한다.
    [DisallowMultipleComponent]
    public sealed class PersistentPlayerVitalsController : MonoBehaviour
    {
        [SerializeField]
        private ExplorationMonsterEncounterController encounterController;

        [SerializeField]
        private Image healthFillImage;

        [SerializeField]
        private Text healthText;

        [SerializeField]
        private Image manaFillImage;

        [SerializeField]
        private Text manaText;

        [SerializeField]
        private Image staminaFillImage;

        [SerializeField]
        private Text staminaText;

        [SerializeField]
        private GameObject actionButtonPanel;

        public void Configure(
            ExplorationMonsterEncounterController configuredEncounterController,
            Image configuredHealthFillImage,
            Text configuredHealthText,
            Image configuredManaFillImage,
            Text configuredManaText,
            Image configuredStaminaFillImage,
            Text configuredStaminaText,
            GameObject configuredActionButtonPanel)
        {
            encounterController =
                configuredEncounterController;

            healthFillImage =
                configuredHealthFillImage;

            healthText =
                configuredHealthText;

            manaFillImage =
                configuredManaFillImage;

            manaText =
                configuredManaText;

            staminaFillImage =
                configuredStaminaFillImage;

            staminaText =
                configuredStaminaText;

            actionButtonPanel =
                configuredActionButtonPanel;

            RefreshNow();
        }

        private void Awake()
        {
            ResolveEncounterController();
        }

        private void OnEnable()
        {
            ResolveEncounterController();
            RefreshNow();
        }

        private void Update()
        {
            ResolveEncounterController();
            RefreshNow();
        }

        public static bool ShouldShowActionButtons(
            bool hasBattle)
        {
            return hasBattle;
        }

        public static string FormatVital(
            string label,
            int current,
            int max)
        {
            return $"{label}  {current} / {max}";
        }

        public static float CalculateRatio(
            int current,
            int max)
        {
            if (max <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01(
                current / (float)max);
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

        private void RefreshNow()
        {
            bool hasBattle =
                encounterController != null
                && encounterController.HasBattle;

            if (actionButtonPanel != null)
            {
                actionButtonPanel.SetActive(
                    ShouldShowActionButtons(
                        hasBattle));
            }

            if (hasBattle)
            {
                RefreshFromBattle();
                return;
            }

            RefreshFromRunContext();
        }

        private void RefreshFromBattle()
        {
            if (encounterController == null)
            {
                return;
            }

            var context =
                encounterController.CurrentBattleContext;

            var player =
                context != null
                    ? context.Player
                    : null;

            if (player == null)
            {
                RefreshFromRunContext();
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
                "정력",
                player.CurrentStamina,
                player.MaxStamina);
        }

        private void RefreshFromRunContext()
        {
            RunContext context =
                RunContext.Current;

            if (context == null
                || context.Player == null)
            {
                return;
            }

            StatBlock stats =
                context.Player.GetFinalStats();

            ApplyVital(
                healthFillImage,
                healthText,
                "HP",
                context.Player.CurrentHp,
                stats.MaxHealth);

            ApplyVital(
                manaFillImage,
                manaText,
                "MP",
                context.Player.CurrentMana,
                stats.MaxMana);

            ApplyVital(
                staminaFillImage,
                staminaText,
                "정력",
                context.Player.CurrentStamina,
                stats.MaxStamina);
        }

        private static void ApplyVital(
            Image fillImage,
            Text valueText,
            string label,
            int current,
            int max)
        {
            int safeMax =
                Mathf.Max(
                    0,
                    max);

            int safeCurrent =
                safeMax > 0
                    ? Mathf.Clamp(
                        current,
                        0,
                        safeMax)
                    : 0;

            if (fillImage != null)
            {
                fillImage.fillAmount =
                    CalculateRatio(
                        safeCurrent,
                        safeMax);
            }

            if (valueText != null)
            {
                valueText.text =
                    FormatVital(
                        label,
                        safeCurrent,
                        safeMax);
            }
        }
    }
}
