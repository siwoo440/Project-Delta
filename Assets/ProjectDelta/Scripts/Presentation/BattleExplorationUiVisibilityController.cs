using UnityEngine;

namespace ProjectDelta.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BattleExplorationUiVisibilityController : MonoBehaviour
    {
        [SerializeField] private ExplorationMonsterEncounterController encounterController;
        [SerializeField] private DungeonMinimapController minimapController;

        private bool lastBattleVisibilityState;

        private void Awake()
        {
            ResolveReferences();
            ApplyVisibility(
                true);
        }

        private void Update()
        {
            ResolveReferences();
            ApplyVisibility(
                false);
        }

        private void OnDisable()
        {
            if (minimapController != null)
            {
                minimapController.enabled =
                    true;
            }
        }

        private void ResolveReferences()
        {
            if (encounterController == null)
            {
                encounterController =
                    FindFirstObjectByType<ExplorationMonsterEncounterController>();
            }

            if (minimapController == null)
            {
                minimapController =
                    FindFirstObjectByType<DungeonMinimapController>();
            }
        }

        private void ApplyVisibility(
            bool force)
        {
            if (minimapController == null)
            {
                return;
            }

            bool hasBattle =
                encounterController != null
                && encounterController.HasBattle;

            if (!force
                && lastBattleVisibilityState == hasBattle)
            {
                return;
            }

            lastBattleVisibilityState =
                hasBattle;

            minimapController.enabled =
                !hasBattle;
        }
    }
}
