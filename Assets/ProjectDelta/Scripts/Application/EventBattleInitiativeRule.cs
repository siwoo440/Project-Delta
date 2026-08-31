namespace ProjectDelta.Application
{
    // 118일차: 행동 처리 후 다음 주도권을 정한다 - "플레이어 매력/행동 보정과 몬스터
    // 매력/행동 보정에 d20을 더해 비교하고, 동점이면 지금 주도권을 유지한다".
    public static class EventBattleInitiativeRule
    {
        public static EventBattleInitiativeHolder RollNext(
            int playerCharm,
            int playerActionInitiativeModifier,
            int targetCharm,
            int targetActionInitiativeModifier,
            EventBattleInitiativeHolder currentHolder,
            IRandomSource rng)
        {
            if (rng == null)
            {
                return currentHolder;
            }

            int playerRoll =
                playerCharm
                + playerActionInitiativeModifier
                + rng.NextInt(
                    1,
                    21);

            int targetRoll =
                targetCharm
                + targetActionInitiativeModifier
                + rng.NextInt(
                    1,
                    21);

            if (playerRoll > targetRoll)
            {
                return EventBattleInitiativeHolder.Player;
            }

            if (targetRoll > playerRoll)
            {
                return EventBattleInitiativeHolder.Target;
            }

            return currentHolder;
        }
    }
}
