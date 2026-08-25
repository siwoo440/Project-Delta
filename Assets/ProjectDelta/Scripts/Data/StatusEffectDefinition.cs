using UnityEngine;

namespace ProjectDelta.Data
{
    /// <summary>
    /// 상태 이상 데이터 원본.
    /// 62~63일차에서는 기존 런타임 구조를 깨지 않고,
    /// 데이터 정의/식별/라운드 종료 처리에 필요한 값만 담당한다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StatusEffectDefinition",
        menuName = "Project Delta/Data/Status Effect Definition")]
    public class StatusEffectDefinition : DefinitionBase
    {
        [Header("Display")]
        [SerializeField] private string displayName;

        [Header("Duration / Stack")]
        [SerializeField] private StatusDurationType durationType;
        [SerializeField] private StatusStackRule stackRule;
        [Min(1)]
        [SerializeField] private int maxStack = 1;

        [Header("Round Tick")]
        [SerializeField] private StatusTickTiming tickTiming;
        [Tooltip("라운드 종료 시 적용되는 수치의 절대값. 방향은 부호가 아니라 EffectKind가 결정한다.")]
        [SerializeField] private int roundEndValue;

        [Header("Effect Kind")]
        [Tooltip("이 상태가 라운드 파이프라인에서 실제로 어떤 효과를 실행하는지 (64일차, 기획서 4.4).")]
        [SerializeField] private StatusEffectKind effectKind;

        [Header("Stat Modifier")]
        [Tooltip("EffectKind가 StatModifier일 때 보정할 능력치 (65일차).")]
        [SerializeField] private BattleStatType targetStat;
        [Tooltip("EffectKind가 StatModifier일 때 적용할 보정치. 양수는 강화, 음수는 약화.")]
        [SerializeField] private int statModifierValue;

        public string DisplayName
        {
            get
            {
                return string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
            }
        }

        public StatusDurationType DurationType => durationType;
        public StatusStackRule StackRule => stackRule;
        public int MaxStack => Mathf.Max(1, maxStack);
        public StatusTickTiming TickTiming => tickTiming;
        public int RoundEndValue => roundEndValue;
        public StatusEffectKind EffectKind => effectKind;
        public BattleStatType TargetStat => targetStat;
        public int StatModifierValue => statModifierValue;
    }
}
