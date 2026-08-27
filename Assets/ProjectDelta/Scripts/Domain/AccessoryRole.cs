namespace ProjectDelta.Domain
{
    // 102일차: 장신구(Accessory)의 역할 분류 6종. 순수 분류 태그이며
    // 아직 스탯 계산에 직접 관여하지 않는다.
    //
    // 기획서에는 "전투형·회피형·탐험형 등" 6역할이라고만 적혀 있고 나머지 3종의
    // 이름이 명시되어 있지 않아, 우선 자원형·매력형·저항형으로 채워뒀다.
    // 정확한 명칭이 정해지면 이 enum 값과 표시명만 교체하면 된다.
    public enum AccessoryRole
    {
        None = 0,
        Combat = 1,
        Evasion = 2,
        Exploration = 3,
        Resource = 4,
        Charm = 5,
        Resistance = 6
    }

    public static class AccessoryRoleRules
    {
        public static string GetDisplayName(
            AccessoryRole role)
        {
            switch (role)
            {
                case AccessoryRole.Combat:
                    return "전투형";

                case AccessoryRole.Evasion:
                    return "회피형";

                case AccessoryRole.Exploration:
                    return "탐험형";

                case AccessoryRole.Resource:
                    return "자원형";

                case AccessoryRole.Charm:
                    return "매력형";

                case AccessoryRole.Resistance:
                    return "저항형";

                default:
                    return "미분류";
            }
        }
    }
}
