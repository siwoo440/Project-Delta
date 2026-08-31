namespace ProjectDelta.Application
{
    // 118일차: 공통 행동 12종 중 하나. 정력 기반 - 값싸고 주도권도 잘 지킨다.
    public sealed class ListenEventBattleCommand : IEventBattleCommand
    {
        private const int BaseFavorGain = 5;

        public string Id =>
            "Listen";

        public string DisplayName =>
            "경청";

        public int ManaCost =>
            0;

        public int StaminaCost =>
            6;

        public int InitiativeModifier =>
            2;

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
                    4);

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
                $"경청 / 호감도 +{favorGained}",
                favorGained);
        }
    }
}
