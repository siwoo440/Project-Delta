using System.Collections.Generic;

namespace ProjectDelta.Application
{
    // 47일차: Battle 생명주기를 명시적인 상태 머신으로 관리한다.
    // ExplorationEncounterSession과 동일하게 Try* 메서드로만 상태를 전환한다.
    // 48일차: 한 라운드 안에서 살아있는 참가자 전원이 Speed 순서대로 한 번씩 행동하도록
    // RoundStart에서 순서 큐를 만들고, 큐가 빌 때까지 AwaitingAction↔ResolvingAction을 반복한다.
    // 59일차: 기획서 4.2·9.3이 쓰는 "라운드" 용어에 맞춰 Turn → Round로 정정했다.
    public sealed class BattleSession
    {
        public BattleState State { get; private set; } =
            BattleState.Idle; // 현재 Battle 상태

        public BattleContext Context { get; private set; } // 현재 Battle 참가자 구성

        public int RoundNumber { get; private set; } // 현재 라운드 번호

        public BattleParticipant CurrentActor { get; private set; } // 행동 대기·처리 중인 참가자

        public BattleParticipant SelectedTarget { get; private set; } // 49일차: CurrentActor가 지정한 대상 (재선택 가능)

        public BattleResult Result { get; private set; } // Battle 최종 결과

        private Queue<BattleParticipant> pendingActorsThisRound =
            new Queue<BattleParticipant>(); // 이번 라운드에 아직 행동하지 않은 참가자 순서 큐

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
                || context.Enemies.Count == 0) // 필수 참가자 확인
            {
                return false; // 참가자 누락 거부
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
                new Queue<BattleParticipant>(
                    BattleTurnOrder.Build(
                        Context)); // 이번 라운드 행동 순서 큐 생성 (Speed 내림차순, 지속 시작 효과 반영 후)

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
            BattleParticipant nextActor =
                null;

            while (pendingActorsThisRound.Count > 0)
            {
                BattleParticipant candidate =
                    pendingActorsThisRound.Dequeue(); // 순서 큐에서 다음 후보 선출

                if (candidate != null
                    && candidate.IsAlive) // 후보 생존 여부 확인
                {
                    nextActor =
                        candidate;

                    break; // 살아있는 행동자를 찾으면 즉시 중단
                }
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

            State =
                BattleState.Finished; // Finished 상태 전환

            return true; // 전환 성공
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

            State =
                BattleState.Idle; // Idle 강제 복귀
        }
    }
}
