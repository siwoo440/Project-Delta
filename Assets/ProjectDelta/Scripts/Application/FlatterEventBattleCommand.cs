namespace ProjectDelta.Application
{
    // 118일차: 공통 행동 12종 중 하나. 구애(CourtEventBattleCommand)와 같이 매력 기반 -
    // 마나를 쓰고 매력-저항 차이만큼 호감도가 갈린다.
    public sealed class FlatterEventBattleCommand : IEventBattleCommand
    {
        private const int BaseFavorGain = 8;

        public string Id =>
            "Flatter";

        public string DisplayName =>
            "칭찬";

        public int ManaCost =>
            6;

        public int StaminaCost =>
            0;

        public int InitiativeModifier =>
            1;

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
                    -2,
                    4);

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
                $"칭찬 / 호감도 +{favorGained}",
                favorGained);
        }
    }
}
