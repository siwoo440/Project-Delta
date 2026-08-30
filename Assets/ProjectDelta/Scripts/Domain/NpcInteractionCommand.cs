namespace ProjectDelta.Domain
{
    public enum NpcInteractionCommand
    {
        Talk = 0,
        Service = 1,
        Leave = 2,

        // 115일차: 우호 상호작용(선물·구조)과 적대 전환(공격) 추가.
        Gift = 3,
        Rescue = 4,
        Attack = 5
    }
}
