using System; // 수치 제한 기능
using ProjectDelta.Data; // 상태 효과 종류

namespace ProjectDelta.Application // 상태 인스턴스 네임스페이스
{
    public sealed class StatusEffectInstance // 실제 적용된 상태 한 건
    {
        public string DefinitionId { get; } // 상태 정의 ID
        public string SourceInstanceId { get; } // 최초 상태 부여자 ID
        public int RemainingRounds { get; private set; } // 남은 지속 라운드
        public int StackCount { get; private set; } // 현재 중첩 수
        // 적용 수치. DamageOverTime·HealOverTime은 절대값 기준(방향은 EffectKind가 결정)이지만,
        // 65일차 StatModifier는 대상 능력치 하나에 부호 있는 보정치로 직접 쓴다 (양수 강화, 음수 약화).
        public int AppliedValue { get; }
        public StatusEffectKind EffectKind { get; } // 64일차: 라운드 파이프라인이 실행할 효과 종류
        public BattleStatType TargetStat { get; } // 65일차: EffectKind가 StatModifier일 때 보정할 능력치

        public bool IsExpired => RemainingRounds <= 0; // 만료 여부

        // targetStat은 StatModifier가 아닌 상태에서는 쓰이지 않으므로, 기존 호출부 호환을 위해
        // 기본값(Attack)을 둔 선택 인자로 둔다.
        public StatusEffectInstance(string definitionId, string sourceInstanceId, int remainingRounds, int stackCount, int appliedValue, StatusEffectKind effectKind, BattleStatType targetStat = BattleStatType.Attack) // 상태 인스턴스 생성자
        {
            DefinitionId = definitionId; // 상태 정의 ID 저장
            SourceInstanceId = sourceInstanceId; // 상태 부여자 ID 저장
            RemainingRounds = remainingRounds; // 남은 라운드 저장
            StackCount = stackCount; // 중첩 수 저장
            AppliedValue = appliedValue; // 적용 수치 저장
            EffectKind = effectKind; // 효과 종류 저장
            TargetStat = targetStat; // 보정 대상 능력치 저장
        }

        public void DecrementRemainingRounds() // 라운드 종료 지속시간 감소
        {
            RemainingRounds = Math.Max(0, RemainingRounds - 1); // 0 아래로 내려가지 않게 감소
        }

        public void RefreshDuration(int remainingRounds) // 동일 상태 재부여 지속시간 갱신
        {
            RemainingRounds = Math.Max(1, remainingRounds); // 최소 1라운드로 갱신
        }

        public void IncreaseStack(int maxStack) // 중첩 수 증가
        {
            int normalizedMaxStack = Math.Max(1, maxStack); // 최대 중첩 최소값 보정
            StackCount = Math.Min(normalizedMaxStack, Math.Max(1, StackCount + 1)); // 최대값을 넘지 않게 증가
        }
    }
}
