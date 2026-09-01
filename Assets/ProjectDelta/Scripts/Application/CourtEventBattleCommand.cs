namespace ProjectDelta.Application
{
    // 117일차: 마나를 써서 크게 승부를 보는 행동. 매력이 저항보다 높을수록 호감도 상승폭이
    // 커진다 - 116일차 EncounterPersuasionRule(성공/실패 판정)과 달리 이쪽은 항상 성공하고
    // "얼마나 오르는가"만 능력치 차이로 갈린다.
    public sealed class CourtEventBattleCommand : IEventBattleCommand
    {
        private const int BaseFavorGain = 12;

        public string Id =>
            "Court";

        public string DisplayName =>
            "구애";

        public int ManaCost =>
            10;

        public int StaminaCost =>
            0;

        public int InitiativeModifier =>
            2;

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
                    -3,
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
                $"구애 / 호감도 +{favorGained}",
                favorGained);
        }
    }
}
