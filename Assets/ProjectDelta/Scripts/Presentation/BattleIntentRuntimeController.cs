using System;
using ProjectDelta.Application;
using ProjectDelta.Data;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BattleIntentRuntimeController : MonoBehaviour
    {
        [SerializeField] private ExplorationMonsterEncounterController encounterController;

        private BattleContext observedContext;
        private int lastObservedActionSequence;

        private void Awake()
        {
            ResolveEncounterController();
            ResetObservedState();
        }

        private void OnDisable()
        {
            BattleIntentService.Clear();
            ResetObservedState();
        }

        private void Update()
        {
            ResolveEncounterController();

            BattleContext context =
                encounterController != null
                    ? encounterController.CurrentBattleContext
                    : null;

            if (encounterController == null
                || !encounterController.HasBattle
                || context == null)
            {
                if (observedContext != null)
                {
                    BattleIntentService.Clear();
                    ResetObservedState();
                }

                return;
            }

            if (observedContext != context)
            {
                BattleIntentService.Clear();

                observedContext =
                    context;

                lastObservedActionSequence =
                    encounterController.LastActionSequence;
            }

            ObserveCompletedAction();
            RefreshEnemyIntents(
                context);
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

        private void ObserveCompletedAction()
        {
            int currentSequence =
                encounterController.LastActionSequence;

            if (currentSequence == lastObservedActionSequence)
            {
                return;
            }

            lastObservedActionSequence =
                currentSequence;

            BattleParticipant actor =
                encounterController.LastActingParticipant;

            if (actor == null
                || actor.Team != BattleTeam.Enemy)
            {
                return;
            }

            BattleIntentService.TryConsume(
                actor.InstanceId,
                out _);
        }

        private static void RefreshEnemyIntents(
            BattleContext context)
        {
            if (context.Enemies == null)
            {
                return;
            }

            foreach (BattleParticipant enemy in context.Enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                BattleParticipant target =
                    context.Player;

                bool targetAvailable =
                    target != null
                    && target.IsAlive;

                bool isStunned =
                    enemy.HasActiveStatusEffectOfKind(
                        StatusEffectKind.Stun);

                bool isSilenced =
                    HasActiveSilence(
                        enemy);

                bool isSatisfied =
                    false;

                if (BattleIntentService.TryGet(
                        enemy.InstanceId,
                        out BattleIntent currentIntent))
                {
                    BattleIntentCancelReason cancelReason =
                        BattleIntentService.EvaluateCancelReason(
                            currentIntent,
                            enemy.IsAlive,
                            isStunned,
                            isSilenced,
                            isSatisfied,
                            targetAvailable);

                    if (cancelReason != BattleIntentCancelReason.None)
                    {
                        BattleIntentService.Cancel(
                            enemy.InstanceId,
                            cancelReason);
                    }

                    continue;
                }

                BattleIntent probeIntent =
                    BattleIntent.CreateBasicAttack(
                        enemy,
                        target);

                BattleIntentCancelReason createBlockReason =
                    BattleIntentService.EvaluateCancelReason(
                        probeIntent,
                        enemy.IsAlive,
                        isStunned,
                        isSilenced,
                        isSatisfied,
                        targetAvailable);

                if (probeIntent == null
                    || createBlockReason != BattleIntentCancelReason.None)
                {
                    continue;
                }

                BattleIntentService.TryRegister(
                    probeIntent);
            }
        }

        private static bool HasActiveSilence(
            BattleParticipant participant)
        {
            if (participant == null
                || participant.StatusEffects == null)
            {
                return false;
            }

            for (int index = 0; index < participant.StatusEffects.Count; index++)
            {
                StatusEffectInstance status =
                    participant.StatusEffects[index];

                if (status == null
                    || status.IsExpired
                    || string.IsNullOrEmpty(
                        status.DefinitionId))
                {
                    continue;
                }

                if (status.DefinitionId.IndexOf(
                        "SILENCE",
                        StringComparison.OrdinalIgnoreCase) >= 0
                    || status.DefinitionId.IndexOf(
                        "침묵",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetObservedState()
        {
            observedContext =
                null;

            lastObservedActionSequence =
                -1;
        }
    }
}
