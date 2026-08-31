namespace ProjectDelta.Application
{
    // 118일차: 공통 행동 12종 중 하나. 마나를 크게 쓰는 대신 성공하면 호감도가 크게 오르는
    // 고위험 행동 - 대신 주도권 보정이 크게 깎여 다음 차례를 넘겨줄 확률이 높아진다.
    public sealed class ConfessEventBattleCommand : IEventBattleCommand
    {
        private const int BaseFavorGain = 20;

        public string Id =>
            "Confess";

        public string DisplayName =>
            "고백";

        public int ManaCost =>
            15;

        public int StaminaCost =>
            0;

        public int InitiativeModifier =>
            -2;

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

            if (!context.Player.TrySpendMana(
                    ManaCost))
            {
                return EventBattleCommandResult.Reject(
                    Id,
                    "마나가 부족합니다.");
            }

            int statDelta =
                context.Player.Charm
                - context.Target.Resistance;

            int variance =
                rng.NextInt(
                    -4,
                    6);

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

            context.AddFavor(
                favorGained);

            return EventBattleCommandResult.Accept(
                Id,
                $"고백 / 호감도 +{favorGained}",
                favorGained);
        }
    }
}
