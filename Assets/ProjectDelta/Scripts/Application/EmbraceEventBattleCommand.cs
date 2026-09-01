namespace ProjectDelta.Application
{
    // 118일차: 공통 행동 12종 중 하나. 매력 기반 - 마나를 쓴다. 고백(ConfessEventBattleCommand)
    // 다음으로 상승폭이 크지만 주도권 보정은 그보다 덜 깎인다.
    public sealed class EmbraceEventBattleCommand : IEventBattleCommand
    {
        private const int BaseFavorGain = 14;

        public string Id =>
            "Embrace";

        public string DisplayName =>
            "포옹";

        public int ManaCost =>
            10;

        public int StaminaCost =>
            0;

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

            if (!context.Player.TrySpendMana(
                    ManaCost))
            {
                return EventBattleCommandResult.Reject(
                    Id,
                    "마나가 부족합니다.");
            }

            int statDelta =
                context.Player.Charm
                - context.SelectedTarget.Participant.Resistance;

            int variance =
                rng.NextInt(
                    -2,
                    5);

            int favorGained =
                (int)(
                    (BaseFavorGain
                        + statDelta
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
                $"포옹 / 호감도 +{favorGained}",
                favorGained);
        }
    }
}
