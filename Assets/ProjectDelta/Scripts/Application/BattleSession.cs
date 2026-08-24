namespace ProjectDelta.Application
{
    // 47일차: Battle 생명주기를 명시적인 상태 머신으로 관리한다.
    // ExplorationEncounterSession과 동일하게 Try* 메서드로만 상태를 전환한다.
    public sealed class BattleSession
    {
        public BattleState State { get; private set; } =
            BattleState.Idle; // 현재 Battle 상태

        public BattleContext Context { get; private set; } // 현재 Battle 참가자 구성

        public int TurnNumber { get; private set; } // 현재 턴 번호

        public BattleParticipant CurrentActor { get; private set; } // 행동 대기·처리 중인 참가자

        public BattleResult Result { get; private set; } // Battle 최종 결과

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

            TurnNumber =
                0; // 턴 번호 초기화

            Result =
                null; // 이전 결과 제거

            State =
                BattleState.Starting; // Starting 상태 전환

            return true; // Battle 시작 성공
        }

        public bool TryStartTurn()
        {
            if (State != BattleState.Starting
                && State != BattleState.TurnEnd) // 턴 시작 가능한 상태 확인
            {
                return false; // 잘못된 전환 거부
            }

            TurnNumber++; // 턴 번호 증가

            CurrentActor =
                null; // 행동 대상 초기화

            State =
                BattleState.TurnStart; // TurnStart 상태 전환

            return true; // 전환 성공
        }

        public bool TryEnterAwaitingAction(
            BattleParticipant actor)
        {
            if (State != BattleState.TurnStart) // TurnStart 상태 확인
            {
                return false; // 잘못된 전환 거부
            }

            if (actor == null
                || !actor.IsAlive) // 행동 가능 참가자 확인
            {
                return false; // 유효하지 않은 행동자 거부
            }

            if (!Context.TryGetParticipant(
                    actor.InstanceId,
                    out BattleParticipant resolved)
                || resolved != actor) // 현재 Battle 소속 참가자인지 확인
            {
                return false; // 소속 불일치 거부
            }

            CurrentActor =
                actor; // 행동 대상 저장

            State =
                BattleState.AwaitingAction; // AwaitingAction 상태 전환

            return true; // 전환 성공
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

        public bool TryEndTurn()
        {
            if (State != BattleState.ResolvingAction) // ResolvingAction 상태 확인
            {
                return false; // 잘못된 전환 거부
            }

            CurrentActor =
                null; // 행동 대상 초기화

            State =
                BattleState.TurnEnd; // TurnEnd 상태 전환

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

            Result =
                new BattleResult(
                    outcome,
                    TurnNumber); // 최종 결과 생성

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

            TurnNumber =
                0; // 턴 번호 초기화

            CurrentActor =
                null; // 행동 대상 초기화

            Result =
                null; // 결과 제거

            State =
                BattleState.Idle; // Idle 상태 복귀

            return true; // 초기화 성공
        }

        // 씬 비활성화·Encounter 강제 중단 시 잠금 상태를 남기지 않기 위한 안전 초기화.
        public void ForceReset()
        {
            Context =
                null; // Battle Context 강제 제거

            TurnNumber =
                0; // 턴 번호 강제 초기화

            CurrentActor =
                null; // 행동 대상 강제 초기화

            Result =
                null; // 결과 강제 제거

            State =
                BattleState.Idle; // Idle 강제 복귀
        }
    }
}
