using System.Collections.Generic;
using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 47일차: Battle 생명주기를 명시적인 상태 머신으로 관리한다.
    public sealed class BattleSession
    {
        public BattleState State { get; private set; } =
            BattleState.Idle;

        public BattleContext Context { get; private set; }

        public int RoundNumber { get; private set; }

        public BattleParticipant CurrentActor { get; private set; }

        public BattleParticipant SelectedTarget { get; private set; }

        public BattleResult Result { get; private set; }

        private LinkedList<BattleParticipant> pendingActorsThisRound =
            new LinkedList<BattleParticipant>();

        private readonly HashSet<string> extraActionGrantedThisRound =
            new HashSet<string>();

        public IReadOnlyCollection<BattleParticipant> PendingActorsThisRound =>
            pendingActorsThisRound;

        public bool HasPendingActorsThisRound =>
            pendingActorsThisRound.Count > 0;

        public bool IsActive =>
            State != BattleState.Idle
            && State != BattleState.Finished;

        public bool TryBeginBattle(
            BattleContext context)
        {
            if (State != BattleState.Idle)
            {
                return false;
            }

            if (context == null
                || context.Player == null
                || context.Enemies == null
                || context.Enemies.Count == 0
                || context.Enemies.Count > BattleContext.MaxEnemySlots)
            {
                return false;
            }

            // 83일차: 도주 후 탐험에 남아 있던 상태를 새 전투 시작 전에 플레이어에게 복원한다.
            // 복원 직후 런타임 보관분은 비워 전투가 정상 종료되면 상태가 자동으로 끝나게 한다.
            if (RunContext.Current != null)
            {
                PersistentPlayerStatusService.RestoreToBattleAndClear(
                    RunContext.Current.Player,
                    context.Player);
            }

            Context =
                context;

            RoundNumber =
                0;

            Result =
                null;

            State =
                BattleState.Starting;

            return true;
        }

        public bool TryStartRound()
        {
            if (State != BattleState.Starting
                && State != BattleState.RoundEnd)
            {
                return false;
            }

            RoundNumber++;

            CurrentActor =
                null;

            BattleRoundStatusProcessor.ApplyStartOfRoundEffects(
                Context);

            pendingActorsThisRound =
                new LinkedList<BattleParticipant>(
                    BattleTurnOrder.Build(
                        Context));

            extraActionGrantedThisRound.Clear();

            State =
                BattleState.RoundStart;

            return true;
        }

        public bool TryEnterAwaitingAction()
        {
            if (State != BattleState.RoundStart
                && State != BattleState.ResolvingAction)
            {
                return false;
            }

            BattleParticipant nextActor =
                null;

            while (pendingActorsThisRound.Count > 0)
            {
                BattleParticipant candidate =
                    pendingActorsThisRound.First.Value;

                pendingActorsThisRound.RemoveFirst();

                if (candidate == null)
                {
                    continue;
                }

                if (!candidate.IsAlive)
                {
                    continue;
                }

                if (candidate.HasActiveStatusEffectOfKind(
                        StatusEffectKind.Stun))
                {
                    continue;
                }

                nextActor =
                    candidate;

                break;
            }

            if (nextActor == null)
            {
                return false;
            }

            CurrentActor =
                nextActor;

            CurrentActor.SetDefending(
                false);

            SelectedTarget =
                null;

            State =
                BattleState.AwaitingAction;

            return true;
        }

        public bool TrySelectTarget(
            BattleParticipant target)
        {
            if (State != BattleState.AwaitingAction
                || CurrentActor == null)
            {
                return false;
            }

            if (!BattleTargeting.IsValidTarget(
                    Context,
                    CurrentActor,
                    target))
            {
                return false;
            }

            SelectedTarget =
                target;

            return true;
        }

        public bool TryBeginResolveAction()
        {
            if (State != BattleState.AwaitingAction)
            {
                return false;
            }

            State =
                BattleState.ResolvingAction;

            return true;
        }

        public bool TryGrantExtraAction(
            BattleParticipant actor)
        {
            if (State != BattleState.AwaitingAction
                && State != BattleState.ResolvingAction)
            {
                return false;
            }

            if (actor == null
                || !actor.IsAlive
                || Context == null
                || !Context.TryGetParticipant(
                    actor.InstanceId,
                    out BattleParticipant found)
                || found != actor)
            {
                return false;
            }

            if (!extraActionGrantedThisRound.Add(
                    actor.InstanceId))
            {
                return false;
            }

            pendingActorsThisRound.AddFirst(
                actor);

            return true;
        }

        public bool TryEndRound()
        {
            if (State != BattleState.ResolvingAction)
            {
                return false;
            }

            if (pendingActorsThisRound.Count > 0)
            {
                return false;
            }

            BattleRoundStatusProcessor.ApplyEndOfRoundDamageAndHealing(
                Context);

            BattleRoundStatusProcessor.DecrementDurationsAndRemoveExpired(
                Context);

            CurrentActor =
                null;

            State =
                BattleState.RoundEnd;

            return true;
        }

        public bool TryFinishBattle(
            BattleOutcome outcome)
        {
            if (!IsActive)
            {
                return false;
            }

            CurrentActor =
                null;

            SelectedTarget =
                null;

            Result =
                new BattleResult(
                    outcome,
                    RoundNumber);

            // 83일차: 도주 성공에서만 현재 상태이상을 탐험 런 상태로 옮긴다.
            // 승리·패배는 기존 규칙대로 전투 한정 상태를 끝낸다.
            if (RunContext.Current != null)
            {
                if (outcome == BattleOutcome.Escaped)
                {
                    PersistentPlayerStatusService.CaptureFromBattleAfterEscape(
                        Context?.Player,
                        RunContext.Current.Player);
                }
                else
                {
                    PersistentPlayerStatusService.ClearPersistentEffects(
                        RunContext.Current.Player);
                }
            }

            ClearAllParticipantStatusEffects();

            State =
                BattleState.Finished;

            return true;
        }

        private void ClearAllParticipantStatusEffects()
        {
            if (Context == null)
            {
                return;
            }

            Context.Player?.RemoveAllStatusEffects();

            if (Context.Enemies == null)
            {
                return;
            }

            foreach (BattleParticipant enemy in Context.Enemies)
            {
                enemy?.RemoveAllStatusEffects();
            }
        }

        public bool TryReset()
        {
            if (State != BattleState.Finished)
            {
                return false;
            }

            Context =
                null;

            RoundNumber =
                0;

            CurrentActor =
                null;

            SelectedTarget =
                null;

            Result =
                null;

            pendingActorsThisRound.Clear();
            extraActionGrantedThisRound.Clear();

            State =
                BattleState.Idle;

            return true;
        }

        public void ForceReset()
        {
            Context =
                null;

            RoundNumber =
                0;

            CurrentActor =
                null;

            SelectedTarget =
                null;

            Result =
                null;

            pendingActorsThisRound.Clear();
            extraActionGrantedThisRound.Clear();

            State =
                BattleState.Idle;
        }
    }
}
