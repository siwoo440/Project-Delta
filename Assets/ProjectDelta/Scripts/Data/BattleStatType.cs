namespace ProjectDelta.Data
{
    // 65일차: StatusEffectDefinition.TargetStat (기획서 4.4 강화·약화 상태).
    // 전투 능력치 7종(공격·방어·속도·명중·회피·매력·저항) 중 실제로 상태 이상이 보정하는
    // 6종만 담는다. 매력(Charm)을 보정하는 상태는 아직 없어 제외한다.
    public enum BattleStatType
    {
        Attack,
        Defense,
        Speed,
        Accuracy,
        Evasion,
        Resistance
    }
}
