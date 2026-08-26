using ProjectDelta.Application;
using ProjectDelta.Data;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BattleIntentRuntimeController : MonoBehaviour
    {
        [SerializeField] private ExplorationMonsterEncounterController encounterController;
        [SerializeField] private MonsterDefinition monsterDefinition;

        private readonly IRandomSource aiRng =
            new CombatRng();

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

            if (BattleIntentService.TryConsume(
                    actor.InstanceId,
                    out _))
            {
                return;
            }

            // 74일차 수정:
            // Intent가 취소되어 실제 Command가 실행되지 않았더라도 적의 차례 자체가 소비되면
            // 그때 취소 대기 상태를 해제한다. 다음 라운드부터는 다시 새 Intent를 만들 수 있다.
            BattleIntentService.TryConsumeCancellation(
                actor.InstanceId,
                out _);
        }

        private void RefreshEnemyIntents(
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

                // 취소된 예고는 해당 Enemy의 차례가 실제로 소비될 때까지 유지한다.
                // 따라서 이 구간에서는 공격/방어/스킬을 새로 뽑지 않는다.
                if (BattleIntentService.HasPendingCancellation(
                        enemy.InstanceId))
                {
                    continue;
                }

                BattleParticipant target =
                    context.Player;

                bool isSilenced =
                    BattleStatusRestrictionPolicy.IsSilenced(
                        enemy);

                bool isSatisfied =
                    false;

                if (BattleIntentService.TryGet(
                        enemy.InstanceId,
                        out BattleIntent currentIntent))
                {
                    BattleIntentCancelReason cancelReason =
                        BattleIntentExecutionPolicy.EvaluateCurrentCancelReason(
                            context,
                            enemy,
                            currentIntent,
                            isSatisfied);

                    if (cancelReason != BattleIntentCancelReason.None)
                    {
                        BattleIntentService.Cancel(
                            enemy.InstanceId,
                            cancelReason);
                    }

                    continue;
                }

                if (!enemy.IsAlive
                    || enemy.HasActiveStatusEffectOfKind(
                        StatusEffectKind.Stun))
                {
                    continue;
                }

                MonsterAiProfile profile =
                    monsterDefinition != null
                        ? monsterDefinition.AiProfile
                        : null;

                if (!MonsterAiDecisionService.TryCreateIntent(
                        enemy,
                        target,
                        profile,
                        isSilenced,
                        aiRng,
                        out BattleIntent probeIntent))
                {
                    continue;
                }

                BattleIntentCancelReason createBlockReason =
                    BattleIntentExecutionPolicy.EvaluateCurrentCancelReason(
                        context,
                        enemy,
                        probeIntent,
                        isSatisfied);

                if (createBlockReason != BattleIntentCancelReason.None)
                {
                    continue;
                }

                BattleIntentService.TryRegister(
                    probeIntent);
            }
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
