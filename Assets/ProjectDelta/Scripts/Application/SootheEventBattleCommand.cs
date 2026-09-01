namespace ProjectDelta.Application
{
    // 117일차: 정력을 써서 안전하게 조금씩 쌓는 행동. 구애(CourtEventBattleCommand)보다
    // 상승폭은 작지만 능력치 차이에 흔들리지 않고 항상 일정하게 오른다.
    public sealed class SootheEventBattleCommand : IEventBattleCommand
    {
        private const int BaseFavorGain = 6;

        public string Id =>
            "Soothe";

        public string DisplayName =>
            "달래기";

        public int ManaCost =>
            0;

        public int StaminaCost =>
            8;

        public int InitiativeModifier =>
            0;

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

            context.SelectedTarget.AddFavor(
                favorGained);

            return EventBattleCommandResult.Accept(
                Id,
                $"달래기 / 호감도 +{favorGained}",
                favorGained);
        }
    }
}
