namespace ProjectDelta.Data
{
    // 67일차: SkillDefinition.TargetType. 지금은 참가자 진영이 Player(항상 1명)와 Enemy뿐이라
    // "적 하나를 고른다"와 "대상 선택 없이 자기 자신에게 쓴다"만 구분하면 된다. 아군이 여럿
    // 생기면(파티 시스템 등) Ally를 추가한다.
    public enum SkillTargetType
    {
        Enemy, // 상대 진영 중 살아있는 대상 하나를 선택해야 한다 (DefendBattleCommand와 달리 대상 필요).
        Self // 대상 선택이 필요 없다 - 시전자 자신에게 적용한다 (DefendBattleCommand와 같은 방식).
    }
}
