namespace ProjectDelta.Application
{
    // 47일차: 플레이어와 몬스터를 전투에서 동일하게 다루기 위한 참가자 런타임 데이터.
    // 48~51일차의 행동 순서·피해 계산이 이 데이터를 계속 사용한다.
    public sealed class BattleParticipant
    {
        public string InstanceId { get; }
        public string DefinitionId { get; }
        public BattleTeam Team { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public int Speed { get; }

        public bool IsAlive =>
            CurrentHp > 0;

        public BattleParticipant(
            string instanceId,
            string definitionId,
            BattleTeam team,
            int maxHp,
            int speed)
        {
            InstanceId =
                instanceId;

            DefinitionId =
                definitionId;

            Team =
                team;

            MaxHp =
                maxHp;

            CurrentHp =
                maxHp;

            Speed =
                speed;
        }
    }
}
