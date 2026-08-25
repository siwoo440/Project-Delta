namespace ProjectDelta.Data
{
    // 61일차: StatusEffectDefinition.DurationType (기획서 10.3).
    public enum StatusDurationType
    {
        Rounds, // 지정된 라운드 수만큼 유지 (약한 상태 1라운드, 일반 2~3, 강한 3라운드 이상 - 기획서 4.4)
        UntilCombatEnd // 라운드 수와 무관하게 전투가 끝날 때 제거 (기획서 4.2 "전투 종료 후 상태")
    }
}
