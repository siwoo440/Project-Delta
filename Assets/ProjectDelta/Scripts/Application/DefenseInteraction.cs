namespace ProjectDelta.Application
{
    // 57일차: 피해가 방어(IsDefending) 감소율과 어떻게 상호작용하는지 구분한다 (기획서 4.2).
    // 몬스터·스킬 데이터에 "방어 가능 여부"로 표시되는 값과 대응한다.
    public enum DefenseInteraction
    {
        Defendable, // 방어 가능 - 방어 감소율을 전체 적용한다.
        PenetratesDefense, // 방어 관통 - 방어 감소율을 일부만 적용한다.
        IgnoresDefense // 방어 불가 - 방어 감소율을 적용하지 않는다.
    }
}
