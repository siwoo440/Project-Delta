using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    // 68일차: SkillDefinition이 들고 있는 Data 계층 enum(SkillDamageType·SkillDefenseInteraction,
    // 66일차)을 실제 계산에 쓰는 Application 계층 enum(DamageType·DefenseInteraction)으로 옮긴다.
    // ProjectDelta.Data 어셈블리는 ProjectDelta.Application을 참조할 수 없어(asmdef 의존 방향)
    // 두 세트로 나뉘어 있으므로, 실제로 계산을 호출하는 이 계층에서 매핑을 담당한다.
    public static class SkillEffectMapping
    {
        public static DamageType ToDamageType(
            SkillDamageType skillDamageType)
        {
            switch (skillDamageType)
            {
                case SkillDamageType.StatusEffect:
                    return DamageType.StatusEffect;

                case SkillDamageType.DamageOverTime:
                    return DamageType.DamageOverTime;

                case SkillDamageType.Fixed:
                    return DamageType.Fixed;

                default:
                    return DamageType.Normal;
            }
        }

        public static DefenseInteraction ToDefenseInteraction(
            SkillDefenseInteraction skillDefenseInteraction)
        {
            switch (skillDefenseInteraction)
            {
                case SkillDefenseInteraction.PenetratesDefense:
                    return DefenseInteraction.PenetratesDefense;

                case SkillDefenseInteraction.IgnoresDefense:
                    return DefenseInteraction.IgnoresDefense;

                default:
                    return DefenseInteraction.Defendable;
            }
        }
    }
}
