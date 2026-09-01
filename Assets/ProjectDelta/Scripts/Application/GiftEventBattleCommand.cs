namespace ProjectDelta.Application
{
    // 118일차: 공통 행동 12종 중 하나. 달래기(SootheEventBattleCommand)와 같이 정력 기반 -
    // 능력치와 무관하게 안정적으로 오르지만 주도권을 다음으로 넘겨줄 확률이 높다.
    public sealed class GiftEventBattleCommand : IEventBattleCommand
    {
        private const int BaseFavorGain = 10;

        public string Id =>
            "Gift";

        public string DisplayName =>
            "선물";

        public int ManaCost =>
            0;

        public int StaminaCost =>
            10;

        public int InitiativeModifier =>
            -1;

        public EventBattleCommandResult Execute(
            EventBattleContext context,
            IRandomSource rng)
        {
            if (context == null
                || context.Player == null
                || context.SelectedTarget == null
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

            context.SelectedTarget.AddFavor(
                favorGained);

            return EventBattleCommandResult.Accept(
                Id,
                $"선물 / 호감도 +{favorGained}",
                favorGained);
        }
    }
}
