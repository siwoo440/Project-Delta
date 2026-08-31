namespace ProjectDelta.Application
{
    // 118일차: 공통 행동 12종 중 하나. 매력 기반 - 마나를 쓴다.
    public sealed class SingEventBattleCommand : IEventBattleCommand
    {
        private const int BaseFavorGain = 13;

        public string Id =>
            "Sing";

        public string DisplayName =>
            "노래";

        public int ManaCost =>
            12;

        public int StaminaCost =>
            0;

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
                    -3,
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

            context.AddFavor(
                favorGained);

            return EventBattleCommandResult.Accept(
                Id,
                $"노래 / 호감도 +{favorGained}",
                favorGained);
        }
    }
}
