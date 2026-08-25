namespace ProjectDelta.Data
{
    // 66일차: SkillDefinition.DefenseInteraction (기획서 4.2).
    // ProjectDelta.Application.DefenseInteraction과 값이 같은 이유는 SkillDamageType과 동일하다
    // (asmdef상 ProjectDelta.Data는 ProjectDelta.Application을 참조할 수 없다).
    public enum SkillDefenseInteraction
    {
        Defendable, // 방어 가능 - 방어 감소율을 전체 적용한다.
        PenetratesDefense, // 방어 관통 - 방어 감소율을 일부만 적용한다.
        IgnoresDefense // 방어 불가 - 방어 감소율을 적용하지 않는다.
    }
}
