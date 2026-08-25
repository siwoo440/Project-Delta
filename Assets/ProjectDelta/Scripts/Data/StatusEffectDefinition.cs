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
        [Tooltip("라운드 종료 시 적용되는 정수 값. 양수는 피해, 음수는 회복처럼 사용할 수 있다.")]
        [SerializeField] private int roundEndValue;

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
    }
}
