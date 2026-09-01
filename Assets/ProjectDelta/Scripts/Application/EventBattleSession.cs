namespace ProjectDelta.Application
{
    // 117일차: 일반 BattleSession(47일차)과 같은 역할 - 생명주기 상태만 관리하고 실제 판정
    // (호감도 증감·자원 소모)은 Presentation(EventBattleController)이 Command 결과로 적용한다.
    public sealed class EventBattleSession
    {
        public EventBattleState State { get; private set; } =
            EventBattleState.Idle;

        public EventBattleContext Context { get; private set; }

        public EventBattleResult Result { get; private set; }

        public bool IsActive =>
            State == EventBattleState.Active;

        public bool TryBegin(
            EventBattleContext context)
        {
            if (State != EventBattleState.Idle
                || context == null)
            {
                return false;
            }

            Context =
                context;

            Result =
                null;

            State =
                EventBattleState.Active;

            return true;
        }

        public bool TryFinish(
            EventBattleOutcome outcome)
        {
            if (State != EventBattleState.Active
                || Context == null)
            {
                return false;
            }

            Result =
                new EventBattleResult(
                    outcome,
                    ResolveFinalFavor(),
                    Context.AttemptCount);

            State =
                EventBattleState.Finished;

            return true;
        }

        public bool TryReset()
        {
            if (State != EventBattleState.Finished)
            {
                return false;
            }

            State =
                EventBattleState.Idle;

            Context =
                null;

            return true;
        }

        // 119일차: 대상이 여러 명이라 "최종 호감도" 하나로 요약해야 한다 - 승리한 대상이
        // 있으면 그 값(100), 없으면 가장 진행이 많이 된 대상의 값을 대표로 쓴다.
        private int ResolveFinalFavor()
        {
            if (Context?.Targets == null)
            {
                return 0;
            }

            int best =
                0;

            for (int index = 0; index < Context.Targets.Count; index++)
            {
                int favor =
                    Context.Targets[index].Favor;

                if (favor > best)
                {
                    best =
                        favor;
                }
            }

            return best;
        }

        public void ForceReset()
        {
            State =
                EventBattleState.Idle;

            Context =
                null;

            Result =
                null;
        }
    }
}
