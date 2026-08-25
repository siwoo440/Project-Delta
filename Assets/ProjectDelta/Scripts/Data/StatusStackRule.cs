namespace ProjectDelta.Data
{
    // 61일차: StatusEffectDefinition.StackRule (기획서 10.3).
    // 실제 중첩 규칙(중독·출혈만 3중첩, 나머지는 시간 갱신 또는 중첩 불가)은 기획서 4.4에
    // 나오지만, 이를 강제하는 판정 로직은 64일차("상태 성공률·지속시간·중첩")에서 다룬다.
    // 이번 일차는 각 상태가 어떤 규칙을 참조하는지 담아두는 자리만 만든다.
    public enum StatusStackRule
    {
        NoStack, // 중첩 불가 (예: 기절)
        RefreshDuration, // 중첩 없이 지속시간만 갱신 (예: 둔화·혼란·침묵·강화 상태)
        Stack // 실제 중첩 (예: 중독·출혈, 최대 3중첩)
    }
}
