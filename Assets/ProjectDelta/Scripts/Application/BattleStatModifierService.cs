using System;
using System.Collections.Generic;
using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    // 65일차: 강화·약화 상태(StatusEffectKind.StatModifier)를 실제 전투 계산에 반영한다.
    // BattleParticipant의 기본 능력치 값 자체는 바꾸지 않고, 계산이 필요한 지점(피해·명중·
    // 행동 순서)에서 이 서비스로 "보정된 값"을 구해 쓴다 (기획서 4.4).
    public static class BattleStatModifierService
    {
        public static int GetEffectiveAttack(
            BattleParticipant participant)
        {
            return GetEffectiveStat(
                participant,
                BattleStatType.Attack,
                participant.Attack);
        }

        public static int GetEffectiveDefense(
            BattleParticipant participant)
        {
            return GetEffectiveStat(
                participant,
                BattleStatType.Defense,
                participant.Defense);
        }

        public static int GetEffectiveSpeed(
            BattleParticipant participant)
        {
            return GetEffectiveStat(
                participant,
                BattleStatType.Speed,
                participant.Speed);
        }

        public static int GetEffectiveAccuracy(
            BattleParticipant participant)
        {
            return GetEffectiveStat(
                participant,
                BattleStatType.Accuracy,
                participant.Accuracy);
        }

        public static int GetEffectiveEvasion(
            BattleParticipant participant)
        {
            return GetEffectiveStat(
                participant,
                BattleStatType.Evasion,
                participant.Evasion);
        }

        public static int GetEffectiveResistance(
            BattleParticipant participant)
        {
            return GetEffectiveStat(
                participant,
                BattleStatType.Resistance,
                participant.Resistance);
        }

        // 만료되지 않은 StatModifier 상태 중 statType을 대상으로 하는 것을 전부 합산한다.
        // StackCount를 곱해 64일차 지속 피해·회복과 동일한 방식으로 중첩을 반영하며(현재
        // 강화·약화 상태는 전부 NoStack/RefreshDuration이라 StackCount가 항상 1이지만, 이후
        // 중첩 가능한 상태가 추가돼도 그대로 동작한다), 결과가 음수로 내려가면 0으로 고정한다.
        private static int GetEffectiveStat(
            BattleParticipant participant,
            BattleStatType statType,
            int baseValue)
        {
            int total =
                baseValue;

            IReadOnlyList<StatusEffectInstance> statusEffects =
                participant.StatusEffects;

            for (int index = 0; index < statusEffects.Count; index++)
            {
                StatusEffectInstance statusEffect =
                    statusEffects[index];

                if (statusEffect.IsExpired
                    || statusEffect.EffectKind != StatusEffectKind.StatModifier
                    || statusEffect.TargetStat != statType)
                {
                    continue;
                }

                total +=
                    statusEffect.AppliedValue
                    * Math.Max(
                        1,
                        statusEffect.StackCount);
            }

            return Math.Max(
                0,
                total);
        }
    }
}
