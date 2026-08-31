namespace ProjectDelta.Application
{
    // 118일차: 공통 행동 12종 중 하나. 정력 기반.
    public sealed class DanceEventBattleCommand : IEventBattleCommand
    {
        private const int BaseFavorGain = 11;

        public string Id =>
            "Dance";

        public string DisplayName =>
            "춤";

        public int ManaCost =>
            0;

        public int StaminaCost =>
            12;

        public int InitiativeModifier =>
            0;

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
                    -2,
                    5);

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
                $"춤 / 호감도 +{favorGained}",
                favorGained);
        }
    }
}
