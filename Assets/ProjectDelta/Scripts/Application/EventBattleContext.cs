namespace ProjectDelta.Application
{
    // 117일차: 일반 BattleContext(47일차)와 상호 배타적인 전용 Context. 기존 BattleParticipant를
    // 그대로 재사용한다 - 정력·마나 자원과 매력·저항 능력치가 이미 있어 새 참가자 타입을 또
    // 만들 필요가 없었다. 대신 이 전투에서만 의미 있는 값(호감도 진행도·진입 경로)을 따로 담는다.
    public sealed class EventBattleContext
    {
        public const int FavorToWin = 100;

        public EventBattleEntrySource Source { get; }

        public BattleParticipant Player { get; }

        public BattleParticipant Target { get; }

        public int Favor { get; private set; }

        public int AttemptCount { get; private set; }

        public EventBattleContext(
            EventBattleEntrySource source,
            BattleParticipant player,
            BattleParticipant target)
        {
            Source =
                source;

            Player =
                player;

            Target =
                target;
        }

        public void AddFavor(
            int amount)
        {
            AttemptCount++;

            Favor =
                Favor + amount < 0
                    ? 0
                    : Favor + amount > FavorToWin
                        ? FavorToWin
                        : Favor + amount;
        }

        public bool HasWon =>
            Favor >= FavorToWin;

        // 117일차: 플레이어가 어떤 행동도 살 수 없으면(마나·정력 둘 다 부족) 더 진행할 수 없다.
        public bool PlayerCanAct(
            int courtManaCost,
            int soothStaminaCost)
        {
            return Player != null
                && (Player.CurrentMana >= courtManaCost
                    || Player.CurrentStamina >= soothStaminaCost);
        }
    }
}
