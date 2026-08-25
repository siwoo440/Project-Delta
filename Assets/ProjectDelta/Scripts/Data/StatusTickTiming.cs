namespace ProjectDelta.Data
{
    // 61일차: StatusEffectDefinition.TickTiming (기획서 10.3).
    // 기획서 4.4가 명시하는 시점은 지금은 라운드 종료 하나뿐이다(중독·출혈은 "라운드 종료 시
    // 지속 피해", 재생은 "라운드 종료 시 체력 회복"). 다른 시점이 필요한 상태가 생기면 추가한다.
    public enum StatusTickTiming
    {
        RoundEnd
    }
}
