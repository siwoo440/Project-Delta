namespace ProjectDelta.Data
{
    // 113일차: NPC가 일반 전투로 이어질 수 있는 기본 적대 규칙을 데이터화한다.
    public enum NpcHostilityMode
    {
        Never = 0,
        CanBecomeHostile = 1,
        StartsHostile = 2
    }
}
