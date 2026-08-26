using System;
using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data;

namespace ProjectDelta.Presentation
{
    // 84일차: 전투 HUD에서 사용할 피해·상태이상 표시 문자열 변환.
    public static class BattleHudDisplayFormatter
    {
        public static string FormatStatusEffects(
            IReadOnlyList<StatusEffectInstance> statusEffects)
        {
            if (statusEffects == null
                || statusEffects.Count == 0)
            {
                return string.Empty;
            }

            var lines =
                new List<string>();

            for (int index = 0;
                 index < statusEffects.Count;
                 index++)
            {
                StatusEffectInstance status =
                    statusEffects[index];

                if (status == null
                    || status.IsExpired)
                {
                    continue;
                }

                lines.Add(
                    FormatStatusEffect(
                        status));
            }

            return string.Join(
                "\n",
                lines);
        }

        public static string FormatDamageChange(
            BattleDamageChange change)
        {
            if (change == null)
            {
                return string.Empty;
            }

            BattleDamageResult result =
                change.DamageResult;

            if (result != null
                && !result.IsHit)
            {
                return "MISS";
            }

            int appliedDamage =
                Math.Max(
                    0,
                    change.AppliedDamage);

            if (result != null
                && result.IsCritical)
            {
                return appliedDamage > 0
                    ? $"치명타! -{appliedDamage}"
                    : "치명타!";
            }

            return appliedDamage > 0
                ? $"-{appliedDamage}"
                : "0";
        }

        public static string FormatVitalDelta(
            int previous,
            int current)
        {
            int delta =
                current - previous;

            if (delta < 0)
            {
                return delta.ToString();
            }

            if (delta > 0)
            {
                return $"+{delta}";
            }

            return string.Empty;
        }

        private static string FormatStatusEffect(
            StatusEffectInstance status)
        {
            string label =
                GetStatusLabel(
                    status);

            string stack =
                status.StackCount > 1
                    ? $" ×{status.StackCount}"
                    : string.Empty;

            return $"{label}{stack} · {status.RemainingRounds}R";
        }

        private static string GetStatusLabel(
            StatusEffectInstance status)
        {
            string knownLabel =
                GetKnownDefinitionLabel(
                    status.DefinitionId);

            if (!string.IsNullOrEmpty(
                    knownLabel))
            {
                return knownLabel;
            }

            switch (status.EffectKind)
            {
                case StatusEffectKind.DamageOverTime:
                    return "지속 피해";

                case StatusEffectKind.HealOverTime:
                    return "지속 회복";

                case StatusEffectKind.Stun:
                    return "기절";

                case StatusEffectKind.ExtraAction:
                    return "추가 행동";

                case StatusEffectKind.StatModifier:
                    return $"{GetStatLabel(status.TargetStat)} {GetSignedValue(status.AppliedValue)}";

                default:
                    return string.IsNullOrEmpty(
                            status.DefinitionId)
                        ? "상태"
                        : CleanDefinitionId(
                            status.DefinitionId);
            }
        }

        private static string GetKnownDefinitionLabel(
            string definitionId)
        {
            if (string.IsNullOrEmpty(
                    definitionId))
            {
                return string.Empty;
            }

            string normalized =
                definitionId.ToUpperInvariant();

            if (normalized.Contains(
                    "POISON"))
            {
                return "중독";
            }

            if (normalized.Contains(
                    "BLEED"))
            {
                return "출혈";
            }

            if (normalized.Contains(
                    "REGEN"))
            {
                return "재생";
            }

            if (normalized.Contains(
                    "STUN"))
            {
                return "기절";
            }

            if (normalized.Contains(
                    "SILENCE"))
            {
                return "침묵";
            }

            if (normalized.Contains(
                    "BIND"))
            {
                return "구속";
            }

            if (normalized.Contains(
                    "CHARM"))
            {
                return "매혹";
            }

            if (normalized.Contains(
                    "SLOW"))
            {
                return "둔화";
            }

            return string.Empty;
        }

        private static string CleanDefinitionId(
            string definitionId)
        {
            const string prefix =
                "STATUS_";

            if (definitionId.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return definitionId.Substring(
                    prefix.Length);
            }

            return definitionId;
        }

        private static string GetSignedValue(
            int value)
        {
            return value >= 0
                ? $"+{value}"
                : value.ToString();
        }

        private static string GetStatLabel(
            BattleStatType statType)
        {
            switch (statType)
            {
                case BattleStatType.Attack:
                    return "공격";

                case BattleStatType.Defense:
                    return "방어";

                case BattleStatType.Speed:
                    return "속도";

                case BattleStatType.Accuracy:
                    return "명중";

                case BattleStatType.Evasion:
                    return "회피";

                case BattleStatType.Resistance:
                    return "저항";

                default:
                    return "능력치";
            }
        }
    }
}
