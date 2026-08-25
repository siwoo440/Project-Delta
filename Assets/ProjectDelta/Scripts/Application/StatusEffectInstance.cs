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
        public int AppliedValue { get; } // 적용 수치 (절대값 기준, 방향은 EffectKind가 결정)
        public StatusEffectKind EffectKind { get; } // 64일차: 라운드 파이프라인이 실행할 효과 종류

        public bool IsExpired => RemainingRounds <= 0; // 만료 여부

        public StatusEffectInstance(string definitionId, string sourceInstanceId, int remainingRounds, int stackCount, int appliedValue, StatusEffectKind effectKind) // 상태 인스턴스 생성자
        {
            DefinitionId = definitionId; // 상태 정의 ID 저장
            SourceInstanceId = sourceInstanceId; // 상태 부여자 ID 저장
            RemainingRounds = remainingRounds; // 남은 라운드 저장
            StackCount = stackCount; // 중첩 수 저장
            AppliedValue = appliedValue; // 적용 수치 저장
            EffectKind = effectKind; // 효과 종류 저장
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
