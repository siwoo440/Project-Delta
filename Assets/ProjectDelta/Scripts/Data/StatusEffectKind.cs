namespace ProjectDelta.Data
{
    // 64일차: StatusEffectDefinition.EffectKind (기획서 4.4).
    // 63일차까지는 AppliedValue의 부호(양수/음수)로 피해·회복을 구분했지만, 강화 상태의
    // 수치(+10 등)와 충돌할 수 있어 "값의 부호"가 아니라 "상태가 어떤 효과인가"로 실행
    // 규칙을 분리한다. 공격 상승 등 실제 능력치 보정 계산은 아직 없으므로 Neutral로 둔다.
    public enum StatusEffectKind
    {
        Neutral, // 라운드 파이프라인이 자동으로 실행할 효과 없음 (약화·강화 상태 등, 능력치 보정은 별도 일차)
        DamageOverTime, // 라운드 종료 시 지속 피해 (중독·출혈)
        HealOverTime, // 라운드 종료 시 지속 회복 (재생)
        Stun, // 자기 차례를 건너뜀 (기절)
        ExtraAction // 이번 라운드에 추가 행동을 부여 (재부여 판정은 StatusStackRule을 그대로 따름)
    }
}
