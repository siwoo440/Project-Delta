using System.Collections.Generic;
using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    // 47일차: Battle 생명주기를 명시적인 상태 머신으로 관리한다.
    // ExplorationEncounterSession과 동일하게 Try* 메서드로만 상태를 전환한다.
    // 48일차: 한 라운드 안에서 살아있는 참가자 전원이 Speed 순서대로 한 번씩 행동하도록
    // RoundStart에서 순서 큐를 만들고, 큐가 빌 때까지 AwaitingAction↔ResolvingAction을 반복한다.
    // 59일차: 기획서 4.2·9.3이 쓰는 "라운드" 용어에 맞춰 Turn → Round로 정정했다.
    // 64일차: 기절 상태는 행동 순서 큐에서 곧바로 건너뛰고, 추가 행동은 같은 큐의 맨 앞에
    // 다시 끼워 넣는 방식으로 처리한다 (기획서 4.2·4.4).
    public sealed class BattleSession
    {
        public BattleState State { get; private set; } =
            BattleState.Idle; // 현재 Battle 상태

        public BattleContext Context { get; private set; } // 현재 Battle 참가자 구성

        public int RoundNumber { get; private set; } // 현재 라운드 번호

        public BattleParticipant CurrentActor { get; private set; } // 행동 대기·처리 중인 참가자

        public BattleParticipant SelectedTarget { get; private set; } // 49일차: CurrentActor가 지정한 대상 (재선택 가능)

        public BattleResult Result { get; private set; } // Battle 최종 결과

        // 64일차: 추가 행동을 큐 맨 앞에 끼워 넣어야 해서 Queue 대신 LinkedList로 관리한다.
        private LinkedList<BattleParticipant> pendingActorsThisRound =
            new LinkedList<BattleParticipant>(); // 이번 라운드에 아직 행동하지 않은 참가자 순서 큐

        // 64일차: 같은 참가자가 추가 행동으로 다시 추가 행동을 만드는 무한 연쇄를 막기 위한
        // "이번 라운드에 이미 추가 행동을 받았는가" 기록. 라운드가 새로 시작할 때 비운다.
        private readonly HashSet<string> extraActionGrantedThisRound =
            new HashSet<string>();

        public IReadOnlyCollection<BattleParticipant> PendingActorsThisRound =>
            pendingActorsThisRound; // 이번 라운드에 남은 행동 순서 (읽기 전용)

        public bool HasPendingActorsThisRound =>
            pendingActorsThisRound.Count > 0; // 이번 라운드에 더 행동할 참가자가 있는지 여부

        public bool IsActive =>
            State != BattleState.Idle
            && State != BattleState.Finished; // Battle 진행 중 여부

        public bool TryBeginBattle(
            BattleContext context)
        {
            if (State != BattleState.Idle) // 중복 Battle 확인
            {
                return false; // 이미 진행 중이면 시작 거부
            }

            if (context == null
                || context.Player == null
                || context.Enemies == null
                || context.Enemies.Count == 0
                || context.Enemies.Count > BattleContext.MaxEnemySlots) // 필수 참가자·최대 인원 확인
            {
                return false; // 참가자 누락 또는 75일차 확정 최대 인원(4명) 초과 거부
            }

            Context =
                context; // Battle Context 저장

            RoundNumber =
                0; // 라운드 번호 초기화

            Result =
                null; // 이전 결과 제거

            State =
                BattleState.Starting; // Starting 상태 전환

            return true; // Battle 시작 성공
        }

        public bool TryStartRound()
        {
            if (State != BattleState.Starting
                && State != BattleState.RoundEnd) // 라운드 시작 가능한 상태 확인
            {
                return false; // 잘못된 전환 거부
            }

            RoundNumber++; // 라운드 번호 증가

            CurrentActor =
                null; // 행동 대상 초기화

            // 60일차: 기획서 4.2 라운드 구조 "라운드 시작 → 지속 시작 효과 적용 → 행동 순서 계산".
            BattleRoundStatusProcessor.ApplyStartOfRoundEffects(
                Context);

            pendingActorsThisRound =
                new LinkedList<BattleParticipant>(
                    BattleTurnOrder.Build(
                        Context)); // 이번 라운드 행동 순서 큐 생성 (Speed 내림차순, 지속 시작 효과 반영 후)

            extraActionGrantedThisRound.Clear(); // 새 라운드이므로 추가 행동 소비 기록 초기화

            State =
                BattleState.RoundStart; // RoundStart 상태 전환

            return true; // 전환 성공
        }

        // 48일차: RoundStart(첫 행동자) 또는 ResolvingAction(다음 행동자)에서 호출한다.
        // 순서 큐에서 다음 참가자를 직접 뽑아 오므로 더 이상 대상을 인자로 받지 않는다.
        public bool TryEnterAwaitingAction()
        {
            if (State != BattleState.RoundStart
                && State != BattleState.ResolvingAction) // 다음 행동자로 넘어갈 수 있는 상태 확인
            {
                return false; // 잘못된 전환 거부
            }

            // 51일차: 자기 차례가 오기 전에 죽은 참가자는 건너뛴다 (전투 이탈).
            // 큐에 넣을 때는 살아있었어도, 같은 라운드 안에서 먼저 행동한 다른 참가자에게 죽을 수 있다.
            // 64일차: 기절한 참가자도 같은 방식으로 건너뛴다. 입력을 요구하지 않고 차례만
            // 소비하며, 기절 지속시간은 라운드 종료 지속시간 감소 단계에서만 줄어든다
            // (여기서 따로 감소시키면 이중 차감이 된다).
            BattleParticipant nextActor =
                null;

            while (pendingActorsThisRound.Count > 0)
            {
                BattleParticipant candidate =
                    pendingActorsThisRound.First.Value; // 순서 큐에서 다음 후보 선출

                pendingActorsThisRound.RemoveFirst();

                if (candidate == null)
                {
                    continue;
                }

                if (!candidate.IsAlive) // 후보 생존 여부 확인
                {
                    continue; // 죽은 참가자는 건너뜀
                }

                if (candidate.HasActiveStatusEffectOfKind(
                        StatusEffectKind.Stun)) // 기절 여부 확인
                {
                    continue; // 기절한 참가자는 차례만 소비하고 건너뜀
                }

                nextActor =
                    candidate;

                break; // 살아있고 기절하지 않은 행동자를 찾으면 즉시 중단
            }

            if (nextActor == null) // 남은 행동자가 모두 사망했는지 확인
            {
                return false; // 더 진행할 행동자가 없으면 거부 (TryEndRound를 사용해야 함)
            }

            CurrentActor =
                nextActor; // 선출된 행동자 저장

            // 52일차: 방어는 "자기 다음 차례가 돌아올 때까지" 유지된다. 그 차례가 바로 지금이므로 해제한다.
            CurrentActor.SetDefending(
                false);

            SelectedTarget =
                null; // 새 행동자이므로 이전 대상 선택 초기화

            State =
                BattleState.AwaitingAction; // AwaitingAction 상태 전환

            return true; // 전환 성공
        }

        // 49일차: AwaitingAction 상태에서만 대상을 지정·재지정할 수 있다.
        public bool TrySelectTarget(
            BattleParticipant target)
        {
            if (State != BattleState.AwaitingAction
                || CurrentActor == null) // 대상 선택 가능한 상태 확인
            {
                return false; // 잘못된 전환 거부
            }

            if (!BattleTargeting.IsValidTarget(
                    Context,
                    CurrentActor,
                    target)) // 유효 대상 여부 확인
            {
                return false; // 유효하지 않은 대상 거부
            }

            SelectedTarget =
                target; // 대상 저장 (재호출 시 마지막 선택으로 교체)

            return true; // 선택 성공
        }

        public bool TryBeginResolveAction()
        {
            if (State != BattleState.AwaitingAction) // AwaitingAction 상태 확인
            {
                return false; // 잘못된 전환 거부
            }

            State =
                BattleState.ResolvingAction; // ResolvingAction 상태 전환

            return true; // 전환 성공
        }

        // 64일차: 스킬 등 행동 처리 중 특정 참가자에게 추가 행동을 부여한다 (기획서 4.4).
        // 상태 지속 피해 처리기가 아니라 행동 순서 큐가 직접 담당하며, actor를 큐 맨 앞에
        // 다시 끼워 넣어 정상 순서로 넘어가기 전에 한 번 더 행동하게 한다.
        // 같은 참가자는 라운드당 한 번만 추가 행동을 받을 수 있다 (추가 행동이 다시 추가
        // 행동을 만드는 무한 연쇄를 막기 위한 소비 규칙).
        public bool TryGrantExtraAction(
            BattleParticipant actor)
        {
            if (State != BattleState.AwaitingAction
                && State != BattleState.ResolvingAction) // 행동 처리 중에만 부여 가능
            {
                return false;
            }

            if (actor == null
                || !actor.IsAlive
                || Context == null
                || !Context.TryGetParticipant(
                    actor.InstanceId,
                    out BattleParticipant found)
                || found != actor) // Context에 속한 참가자인지 확인
            {
                return false;
            }

            if (!extraActionGrantedThisRound.Add(
                    actor.InstanceId)) // 이번 라운드에 이미 추가 행동을 받았으면 거부
            {
                return false;
            }

            pendingActorsThisRound.AddFirst(
                actor); // 큐 맨 앞에 끼워 넣어 다음 차례에 바로 다시 행동하게 함

            return true;
        }

        // 48일차: 이번 라운드에 남은 행동자가 없을 때만 허용한다.
        // 아직 순서 큐에 참가자가 남아 있다면 TryEnterAwaitingAction()으로 다음 행동자를 진행해야 한다.
        public bool TryEndRound()
        {
            if (State != BattleState.ResolvingAction) // ResolvingAction 상태 확인
            {
                return false; // 잘못된 전환 거부
            }

            if (pendingActorsThisRound.Count > 0) // 이번 라운드에 남은 행동자 확인
            {
                return false; // 아직 행동하지 않은 참가자가 있으면 거부
            }

            // 60일차: 기획서 4.2 라운드 구조 "지속 피해와 회복 적용 → 상태 지속 시간 감소".
            // 참가자별 행동이 모두 끝난 뒤, 전투 종료 판정 전에 적용한다.
            BattleRoundStatusProcessor.ApplyEndOfRoundDamageAndHealing(
                Context);

            BattleRoundStatusProcessor.DecrementDurationsAndRemoveExpired(
                Context);

            CurrentActor =
                null; // 행동 대상 초기화

            State =
                BattleState.RoundEnd; // RoundEnd 상태 전환

            return true; // 전환 성공
        }

        public bool TryFinishBattle(
            BattleOutcome outcome)
        {
            if (!IsActive) // Battle 진행 중 여부 확인
            {
                return false; // Idle·Finished 상태에서는 거부
            }

            CurrentActor =
                null; // 행동 대상 초기화

            SelectedTarget =
                null; // 대상 선택 초기화

            Result =
                new BattleResult(
                    outcome,
                    RoundNumber); // 최종 결과 생성

            // 64일차: 전투 한정 상태를 여기서 정리한다. Rounds·UntilCombatEnd 구분 없이
            // 중독·기절 등 어떤 상태도 다음 전투까지 남지 않아야 한다 (기획서 4.2).
            ClearAllParticipantStatusEffects();

            State =
                BattleState.Finished; // Finished 상태 전환

            return true; // 전환 성공
        }

        // 64일차: TryFinishBattle()에서 전투가 끝난 모든 참가자의 상태 이상을 제거할 때 쓴다.
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
            if (State != BattleState.Finished) // Finished 상태 확인
            {
                return false; // 잘못된 초기화 거부
            }

            Context =
                null; // Battle Context 제거

            RoundNumber =
                0; // 라운드 번호 초기화

            CurrentActor =
                null; // 행동 대상 초기화

            SelectedTarget =
                null; // 대상 선택 초기화

            Result =
                null; // 결과 제거

            pendingActorsThisRound.Clear(); // 남은 행동 순서 큐 정리

            extraActionGrantedThisRound.Clear(); // 추가 행동 소비 기록 정리

            State =
                BattleState.Idle; // Idle 상태 복귀

            return true; // 초기화 성공
        }

        // 씬 비활성화·Encounter 강제 중단 시 잠금 상태를 남기지 않기 위한 안전 초기화.
        public void ForceReset()
        {
            Context =
                null; // Battle Context 강제 제거

            RoundNumber =
                0; // 라운드 번호 강제 초기화

            CurrentActor =
                null; // 행동 대상 강제 초기화

            SelectedTarget =
                null; // 대상 선택 강제 초기화

            Result =
                null; // 결과 강제 제거

            pendingActorsThisRound.Clear(); // 남은 행동 순서 큐 강제 정리

            extraActionGrantedThisRound.Clear(); // 추가 행동 소비 기록 강제 정리

            State =
                BattleState.Idle; // Idle 강제 복귀
        }
    }
}
