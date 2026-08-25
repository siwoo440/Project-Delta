using UnityEngine;

namespace ProjectDelta.Data
{
    // 61일차: 기획서 10.3 "상태 이상은 정의 데이터와 인스턴스로 나눈다".
    // 실제 약화 9종·강화 7종(기획서 4.4)은 62~63일차에서 이 정의를 채워 만든다.
    // Effects는 문서에 세부 구조가 없어, 라운드 종료 시 적용하는 지속 피해·회복 값 하나로
    // 시작한다(중독·출혈·재생이 전부 이 형태). 여러 효과를 조합하는 상태가 생기면 확장한다.
    [CreateAssetMenu(fileName = "StatusEffectDefinition", menuName = "ProjectDelta/Data/Status Effect Definition")]
    public sealed class StatusEffectDefinition : DefinitionBase
    {
        [SerializeField] private string displayName;
        [SerializeField] private StatusDurationType durationType;
        [SerializeField] private StatusStackRule stackRule;
        [SerializeField] private int maxStack = 1;
        [SerializeField] private StatusTickTiming tickTiming;

        // 61일차: Effects의 최소 형태. 라운드 종료 시 HP에 더하는 값(음수 = 지속 피해, 양수 = 회복).
        [SerializeField] private int tickValue;

        public string DisplayName => displayName;
        public StatusDurationType DurationType => durationType;
        public StatusStackRule StackRule => stackRule;
        public int MaxStack => maxStack;
        public StatusTickTiming TickTiming => tickTiming;
        public int TickValue => tickValue;
    }
}
