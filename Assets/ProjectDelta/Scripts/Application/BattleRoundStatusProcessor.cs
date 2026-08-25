using System.Collections.Generic;

namespace ProjectDelta.Application
{
    // 60일차: 기획서 4.2 라운드 구조 중 상태 이상 관련 세 단계를 처리한다.
    //
    //   라운드 시작 → 지속 시작 효과 적용 → 행동 순서 계산 → 참가자별 행동
    //   → 지속 피해와 회복 적용 → 상태 지속 시간 감소 → 전투 종료 판정 → 다음 라운드
    //
    // 실제 상태 이상(중독·출혈·재생 등, 기획서 4.4)은 62~63일차에 생기므로, 지금은 각
    // 참가자의 StatusEffects가 항상 비어 있어 아무 효과도 나타나지 않는다. BattleSession이
    // 매 라운드 이 메서드들을 실제로 호출하는 자리를 만들어두는 것이 이번 일차의 목적이다.
    public static class BattleRoundStatusProcessor
    {
        // "지속 시작 효과 적용" 단계. 라운드 시작 시(행동 순서 계산 전) 적용되는 효과는
        // 아직 기획서에 구체적으로 정의돼 있지 않아 지금은 자리만 유지한다.
        public static void ApplyStartOfRoundEffects(
            BattleContext context)
        {
        }

        // "지속 피해와 회복 적용" 단계. AppliedValue가 음수면 피해, 양수면 회복으로 다룬다
        // (중독·출혈은 음수, 재생은 양수 - 기획서 4.4).
        public static void ApplyEndOfRoundDamageAndHealing(
            BattleContext context)
        {
            foreach (BattleParticipant participant in GetAllParticipants(
                         context))
            {
                if (!participant.IsAlive)
                {
                    continue; // 이미 죽은 참가자에게는 지속 효과를 적용하지 않는다
                }

                foreach (StatusEffectInstance statusEffect in participant.StatusEffects)
                {
                    if (statusEffect.AppliedValue < 0)
                    {
                        participant.ApplyDamage(
                            -statusEffect.AppliedValue);
                    }
                    else if (statusEffect.AppliedValue > 0)
                    {
                        participant.Heal(
                            statusEffect.AppliedValue);
                    }
                }
            }
        }

        // "상태 지속 시간 감소" 단계. 감소 후 만료된 상태는 즉시 제거한다.
        public static void DecrementDurationsAndRemoveExpired(
            BattleContext context)
        {
            foreach (BattleParticipant participant in GetAllParticipants(
                         context))
            {
                foreach (StatusEffectInstance statusEffect in participant.StatusEffects)
                {
                    statusEffect.DecrementRemainingRounds();
                }

                participant.RemoveExpiredStatusEffects();
            }
        }

        private static IEnumerable<BattleParticipant> GetAllParticipants(
            BattleContext context)
        {
            if (context == null)
            {
                yield break;
            }

            if (context.Player != null)
            {
                yield return context.Player;
            }

            if (context.Enemies == null)
            {
                yield break;
            }

            foreach (BattleParticipant enemy in context.Enemies)
            {
                if (enemy != null)
                {
                    yield return enemy;
                }
            }
        }
    }
}
