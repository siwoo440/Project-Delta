namespace ProjectDelta.Data
{
    // 66일차: SkillDefinition.DamageType (기획서 4.2 피해 유형 표).
    // ProjectDelta.Application.DamageType과 값이 같지만, ProjectDelta.Data 어셈블리는
    // ProjectDelta.Application을 참조할 수 없어(asmdef 의존 방향: Application → Data) 별도로
    // 둔다. 실제 계산을 호출하는 쪽(스킬 Command, Application 계층)에서 이 값을
    // Application.DamageType으로 옮겨 쓴다.
    public enum SkillDamageType
    {
        Normal, // 일반 공격·직접 공격 스킬 - 방어력 사용
        StatusEffect, // 상태 이상 - 저항 사용
        DamageOverTime, // 지속 피해 - 저항 사용
        Fixed // 고정 피해 - 방어력 무시
    }
}
