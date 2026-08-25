using System; // 기본 예외와 수학 기능
using ProjectDelta.Data; // 상태 정의와 중첩 규칙

namespace ProjectDelta.Application // 상태 적용 서비스 네임스페이스
{
    public static class StatusEffectApplicationService // 상태 성공률과 중첩 처리 서비스
    {
        public const int MinSuccessChance = 5; // 최소 상태 성공률
        public const int MaxSuccessChance = 95; // 최대 상태 성공률
        public const int LowUpperBound = 34; // 낮음 단계 최대값
        public const int NormalUpperBound = 69; // 보통 단계 최대값

        public static int CalculateFinalSuccessChance(int effectBaseChance, int attackerStatusModifier, int targetResistance, int skillEquipmentRelicModifier) // 최종 상태 성공률 계산
        {
            int rawChance = effectBaseChance + attackerStatusModifier - targetResistance + skillEquipmentRelicModifier; // 기획서 상태 성공률 공식
            return Math.Max(MinSuccessChance, Math.Min(MaxSuccessChance, rawChance)); // 5~95 범위 제한
        }

        public static StatusSuccessLevel GetSuccessLevel(int successChance) // 성공률 표시 단계 계산
        {
            int normalizedChance = Math.Max(MinSuccessChance, Math.Min(MaxSuccessChance, successChance)); // 표시용 성공률 범위 제한

            if (normalizedChance <= LowUpperBound) // 낮음 범위 확인
            {
                return StatusSuccessLevel.Low; // 낮음 단계 반환
            }

            if (normalizedChance <= NormalUpperBound) // 보통 범위 확인
            {
                return StatusSuccessLevel.Normal; // 보통 단계 반환
            }

            return StatusSuccessLevel.High; // 높음 단계 반환
        }

        public static StatusEffectApplyResult TryApply(BattleParticipant target, StatusEffectDefinition definition, string sourceInstanceId, int durationRounds, int appliedValue, int effectBaseChance, int attackerStatusModifier, int skillEquipmentRelicModifier, IRandomSource randomSource) // 상태 정의 기반 적용 시도
        {
            if (definition == null) // 상태 정의 누락 확인
            {
                throw new ArgumentNullException(nameof(definition)); // 잘못된 호출 차단
            }

            return TryApply(target, definition.Id, sourceInstanceId, durationRounds, appliedValue, definition.EffectKind, definition.StackRule, definition.MaxStack, effectBaseChance, attackerStatusModifier, skillEquipmentRelicModifier, randomSource, definition.TargetStat); // 정의값을 공통 적용 경로로 전달
        }

        // 65일차: targetStat은 StatModifier가 아닌 상태에서는 쓰이지 않으므로 기존 호출부
        // 호환을 위해 마지막 선택 인자로 둔다.
        public static StatusEffectApplyResult TryApply(BattleParticipant target, string definitionId, string sourceInstanceId, int durationRounds, int appliedValue, StatusEffectKind effectKind, StatusStackRule stackRule, int maxStack, int effectBaseChance, int attackerStatusModifier, int skillEquipmentRelicModifier, IRandomSource randomSource, BattleStatType targetStat = BattleStatType.Attack) // 상태 적용 시도
        {
            if (target == null) // 대상 누락 확인
            {
                throw new ArgumentNullException(nameof(target)); // 잘못된 호출 차단
            }

            if (string.IsNullOrWhiteSpace(definitionId)) // 상태 ID 누락 확인
            {
                throw new ArgumentException("Status effect definition id is required.", nameof(definitionId)); // 빈 상태 ID 차단
            }

            if (randomSource == null) // 난수 발생원 누락 확인
            {
                throw new ArgumentNullException(nameof(randomSource)); // 잘못된 호출 차단
            }

            int targetResistance = BattleStatModifierService.GetEffectiveResistance(target); // 65일차: 저항 상승도 상태 성공률 방어에 반영
            int finalSuccessChance = CalculateFinalSuccessChance(effectBaseChance, attackerStatusModifier, targetResistance, skillEquipmentRelicModifier); // 대상 저항 포함 최종 성공률 계산
            StatusSuccessLevel successLevel = GetSuccessLevel(finalSuccessChance); // UI용 성공 단계 계산
            int roll = randomSource.NextInt(1, 101); // 1~100 상태 성공 굴림

            if (roll > finalSuccessChance) // 성공률 초과 굴림 확인
            {
                return new StatusEffectApplyResult(finalSuccessChance, successLevel, roll, false, 0, 0); // 실패 결과 반환
            }

            int normalizedDuration = Math.Max(1, durationRounds); // 최소 1라운드 보장
            StatusEffectInstance existingStatus = FindExistingStatus(target, definitionId); // 동일 상태 검색

            if (existingStatus == null) // 최초 상태 적용 확인
            {
                StatusEffectInstance newStatus = new StatusEffectInstance(definitionId, sourceInstanceId, normalizedDuration, 1, appliedValue, effectKind, targetStat); // 새 상태 인스턴스 생성
                target.AddStatusEffect(newStatus); // 대상 상태 목록에 추가
                return new StatusEffectApplyResult(finalSuccessChance, successLevel, roll, true, newStatus.StackCount, newStatus.RemainingRounds); // 신규 적용 결과 반환
            }

            ApplyStackRule(existingStatus, stackRule, maxStack, normalizedDuration); // 기존 상태 중첩 규칙 처리
            return new StatusEffectApplyResult(finalSuccessChance, successLevel, roll, true, existingStatus.StackCount, existingStatus.RemainingRounds); // 재적용 결과 반환
        }

        private static StatusEffectInstance FindExistingStatus(BattleParticipant target, string definitionId) // 동일 상태 검색
        {
            for (int index = 0; index < target.StatusEffects.Count; index++) // 활성 상태 순회
            {
                StatusEffectInstance statusEffect = target.StatusEffects[index]; // 현재 상태 가져오기

                if (string.Equals(statusEffect.DefinitionId, definitionId, StringComparison.Ordinal)) // 같은 정의 ID 확인
                {
                    return statusEffect; // 기존 상태 반환
                }
            }

            return null; // 기존 상태 없음 반환
        }

        private static void ApplyStackRule(StatusEffectInstance existingStatus, StatusStackRule stackRule, int maxStack, int durationRounds) // 중첩 규칙 적용
        {
            switch (stackRule) // 상태별 중첩 규칙 분기
            {
                case StatusStackRule.NoStack: // 중첩 불가 상태 처리
                    return; // 기존 상태 그대로 유지
                case StatusStackRule.RefreshDuration: // 시간 갱신 상태 처리
                    existingStatus.RefreshDuration(durationRounds); // 지속시간 갱신
                    return; // 처리 종료
                case StatusStackRule.Stack: // 실제 중첩 상태 처리
                    existingStatus.IncreaseStack(maxStack); // 최대 중첩까지 증가
                    existingStatus.RefreshDuration(durationRounds); // 재부여 시 지속시간 갱신
                    return; // 처리 종료
                default: // 정의되지 않은 규칙 처리
                    throw new ArgumentOutOfRangeException(nameof(stackRule), stackRule, "Unknown status stack rule."); // 잘못된 규칙 차단
            }
        }
    }
}
