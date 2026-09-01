namespace ProjectDelta.Application
{
    // 118일차: 공통 행동 12종 중 하나. 매력 기반 - 마나를 쓴다.
    public sealed class WhisperEventBattleCommand : IEventBattleCommand
    {
        private const int BaseFavorGain = 7;

        public string Id =>
            "Whisper";

        public string DisplayName =>
            "속삭임";

        public int ManaCost =>
            7;

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
                    -1,
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

            context.SelectedTarget.AddFavor(
                favorGained);

            return EventBattleCommandResult.Accept(
                Id,
                $"속삭임 / 호감도 +{favorGained}",
                favorGained);
        }
    }
}
