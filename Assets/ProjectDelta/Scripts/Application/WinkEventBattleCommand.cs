namespace ProjectDelta.Application
{
    // 118일차: 공통 행동 12종 중 하나. 정력 기반 - 가장 값싸고 주도권 보정이 가장 크다.
    public sealed class WinkEventBattleCommand : IEventBattleCommand
    {
        private const int BaseFavorGain = 4;

        public string Id =>
            "Wink";

        public string DisplayName =>
            "윙크";

        public int ManaCost =>
            0;

        public int StaminaCost =>
            4;

        public int InitiativeModifier =>
            3;

        public EventBattleCommandResult Execute(
            EventBattleContext context,
            IRandomSource rng)
        {
            if (context == null
                || context.Player == null
                || context.Target == null
                || rng == null)
            {
                return EventBattleCommandResult.Reject(
                    Id,
                    "현재 이벤트 전투 정보가 없습니다.");
            }

            if (!context.Player.TrySpendStamina(
                    StaminaCost))
            {
                return EventBattleCommandResult.Reject(
                    Id,
                    "정력이 부족합니다.");
            }

            int variance =
                rng.NextInt(
                    -1,
                    3);

            int favorGained =
                (int)(
                    (BaseFavorGain
                        + variance)
                    * context.PlayerActionFavorMultiplier);

            if (favorGained < 0)
            {
                favorGained =
                    0;
            }

            context.AddFavor(
                favorGained);

            return EventBattleCommandResult.Accept(
                Id,
                $"윙크 / 호감도 +{favorGained}",
                favorGained);
        }
    }
}
