using System; // 수학 기능 사용
using System.Collections.Generic; // 컬렉션 기능 사용
using ProjectDelta.Data; // 상태 효과 데이터 사용

namespace ProjectDelta.Application // 전투 응용 네임스페이스
{
    // 60일차: 기획서 4.2 라운드 구조 중 상태 이상 관련 세 단계를 처리한다.
    //
    //   라운드 시작 → 지속 시작 효과 적용 → 행동 순서 계산 → 참가자별 행동
    //   → 지속 피해와 회복 적용 → 상태 지속 시간 감소 → 전투 종료 판정 → 다음 라운드
    //
    // 64일차: "지속 피해와 회복 적용" 단계를 AppliedValue의 부호가 아니라 StatusEffectKind
    // 기준으로 실행하도록 정리했다 (기획서 4.4). 기절·추가 행동은 라운드 틱 효과가 아니라
    // BattleSession의 행동 순서 처리가 직접 담당한다.
    // 70일차: 지속 피해로 플레이어가 피해를 받았을 때 상태 부여자를 마지막 공격자로 추적한다.
    public static class BattleRoundStatusProcessor
    {
        public static void ApplyStartOfRoundEffects( // 라운드 시작 효과 처리
            BattleContext context) // 현재 전투 정보 입력
        {
        }

        public static void ApplyEndOfRoundDamageAndHealing( // 라운드 종료 지속 효과 처리
            BattleContext context) // 현재 전투 정보 입력
        {
            foreach (BattleParticipant participant in GetAllParticipants(
                         context)) // 전체 참가자 순회
            {
                if (!participant.IsAlive) // 생존 참가자 여부 확인
                {
                    continue;
                }

                foreach (StatusEffectInstance statusEffect in participant.StatusEffects) // 상태 효과 순회
                {
                    int tickAmount =
                        Math.Abs(
                            statusEffect.AppliedValue)
                        * Math.Max(
                            1,
                            statusEffect.StackCount); // 중첩 반영 틱 수치 계산

                    switch (statusEffect.EffectKind) // 상태 효과 종류 분기
                    {
                        case StatusEffectKind.DamageOverTime:
                            int appliedDamage =
                                participant.ApplyDamage(
                                    tickAmount); // 지속 피해 적용

                            BattleDefeatService.RecordAppliedDamageBySourceId(
                                context,
                                participant,
                                statusEffect.SourceInstanceId,
                                appliedDamage); // 지속 피해 공격자 기록
                            break;

                        case StatusEffectKind.HealOverTime:
                            participant.Heal(
                                tickAmount); // 지속 회복 적용
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        public static void DecrementDurationsAndRemoveExpired( // 상태 지속 시간 감소 처리
            BattleContext context) // 현재 전투 정보 입력
        {
            foreach (BattleParticipant participant in GetAllParticipants(
                         context)) // 전체 참가자 순회
            {
                foreach (StatusEffectInstance statusEffect in participant.StatusEffects) // 상태 효과 순회
                {
                    statusEffect.DecrementRemainingRounds(); // 남은 라운드 감소
                }

                participant.RemoveExpiredStatusEffects(); // 만료 상태 효과 제거
            }
        }

        private static IEnumerable<BattleParticipant> GetAllParticipants( // 전체 참가자 열거
            BattleContext context) // 현재 전투 정보 입력
        {
            if (context == null) // 전투 정보 존재 확인
            {
                yield break;
            }

            if (context.Player != null) // 플레이어 존재 확인
            {
                yield return context.Player;
            }

            if (context.Enemies == null) // 적 목록 존재 확인
            {
                yield break;
            }

            foreach (BattleParticipant enemy in context.Enemies) // 적 참가자 순회
            {
                if (enemy != null) // 유효 적 확인
                {
                    yield return enemy;
                }
            }
        }
    }
}
