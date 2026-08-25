using System;
using System.Collections.Generic;
using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    // 60일차: 기획서 4.2 라운드 구조 중 상태 이상 관련 세 단계를 처리한다.
    //
    //   라운드 시작 → 지속 시작 효과 적용 → 행동 순서 계산 → 참가자별 행동
    //   → 지속 피해와 회복 적용 → 상태 지속 시간 감소 → 전투 종료 판정 → 다음 라운드
    //
    // 64일차: "지속 피해와 회복 적용" 단계를 AppliedValue의 부호가 아니라 StatusEffectKind
    // 기준으로 실행하도록 정리했다 (기획서 4.4). 기절·추가 행동은 라운드 틱 효과가 아니라
    // BattleSession의 행동 순서 처리가 직접 담당한다.
    public static class BattleRoundStatusProcessor
    {
        // "지속 시작 효과 적용" 단계. 라운드 시작 시(행동 순서 계산 전) 적용되는 효과는
        // 아직 기획서에 구체적으로 정의돼 있지 않아 지금은 자리만 유지한다.
        public static void ApplyStartOfRoundEffects(
            BattleContext context)
        {
        }

        // "지속 피해와 회복 적용" 단계. StatusEffectKind가 DamageOverTime·HealOverTime인
        // 상태만 실행 대상이며, 나머지(Stun·ExtraAction·Neutral)는 여기서 아무 일도 하지
        // 않는다. StackCount를 곱해 중첩 수를 실제 피해·회복량에 반영한다 (기획서 4.4).
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
                    int tickAmount =
                        Math.Abs(
                            statusEffect.AppliedValue)
                        * Math.Max(
                            1,
                            statusEffect.StackCount); // 중첩 수만큼 지속 피해·회복량 배증

                    switch (statusEffect.EffectKind)
                    {
                        case StatusEffectKind.DamageOverTime:
                            participant.ApplyDamage(
                                tickAmount);
                            break;

                        case StatusEffectKind.HealOverTime:
                            participant.Heal(
                                tickAmount);
                            break;

                        default:
                            break; // Stun·ExtraAction·Neutral은 라운드 종료 지속 피해·회복 대상이 아님
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
