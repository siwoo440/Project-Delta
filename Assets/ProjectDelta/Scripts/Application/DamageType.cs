namespace ProjectDelta.Application
{
    // 58일차: 피해가 어떤 방어 수치로 감쇠되는지 구분한다 (기획서 4.2 피해 유형 표).
    // 정력 피해는 성인 이벤트 전투 전용 계산식이 필요해 아직 이 목록에 넣지 않는다.
    public enum DamageType
    {
        Normal, // 일반 공격·직접 공격 스킬 - 방어력 사용
        StatusEffect, // 상태 이상 - 저항 사용
        DamageOverTime, // 지속 피해 - 저항 또는 효과별 수치(기본값으로 저항 사용)
        Fixed // 고정 피해 - 방어력 무시
    }
}
