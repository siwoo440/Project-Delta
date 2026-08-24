namespace ProjectDelta.Application
{
    // 51일차: 매 공격 판정 이후 전투가 끝났는지(전멸) 확인한다.
    // BattleTargeting과 같은 "Player vs Enemies" 진영 규칙을 그대로 따른다.
    public static class BattleOutcomeEvaluator
    {
        // Player가 죽었으면 Defeat, 살아있는 Enemy가 하나도 없으면 Victory.
        // 둘 다 아니면(양쪽 다 생존자가 있으면) false를 반환해 전투가 계속됨을 알린다.
        // 두 조건이 동시에 성립하는 경우(상호 전멸)에는 Defeat를 우선한다.
        public static bool TryEvaluate(
            BattleContext context,
            out BattleOutcome outcome)
        {
            outcome = default;

            if (context == null)
            {
                return false;
            }

            if (context.Player == null
                || !context.Player.IsAlive)
            {
                outcome =
                    BattleOutcome.Defeat;

                return true;
            }

            if (!HasAnyAliveEnemy(
                    context))
            {
                outcome =
                    BattleOutcome.Victory;

                return true;
            }

            return false;
        }

        private static bool HasAnyAliveEnemy(
            BattleContext context)
        {
            if (context.Enemies == null)
            {
                return false;
            }

            foreach (BattleParticipant enemy in context.Enemies)
            {
                if (enemy != null
                    && enemy.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
