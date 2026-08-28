using System;
using System.Collections.Generic;
using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    public enum EventResultFailureReason
    {
        None = 0,
        InvalidState = 1,

        // 같은 이벤트는 한 번만 확정할 수 있다(중복 적용 방지).
        AlreadyResolved = 2
    }

    public sealed class EventResultApplicationResult
    {
        public bool Success { get; private set; }

        public EventResultFailureReason FailureReason { get; private set; }

        public IReadOnlyList<string> AppliedEffectSummaries { get; private set; }

        public static EventResultApplicationResult Succeeded(
            IReadOnlyList<string> summaries)
        {
            return new EventResultApplicationResult
            {
                Success = true,
                FailureReason = EventResultFailureReason.None,
                AppliedEffectSummaries = summaries
                    ?? Array.Empty<string>()
            };
        }

        public static EventResultApplicationResult Failed(
            EventResultFailureReason reason)
        {
            return new EventResultApplicationResult
            {
                Success = false,
                FailureReason = reason,
                AppliedEffectSummaries = Array.Empty<string>()
            };
        }
    }

    // 108일차: 선택지 결과(EventEffect 목록)를 실제로 적용한다.
    // 같은 EventDefinition.Id는 EventRunState 플래그로 한 번만 확정되게 막는다 -
    // 반복 등장 가능한 이벤트를 구분하는 것은 112일차(이벤트 저장·기록) 범위다.
    public static class EventResultService
    {
        private const string ResolvedFlagPrefix =
            "EVENT_RESOLVED_";

        public static EventResultApplicationResult ApplyChoice(
            EventDefinition eventDefinition,
            EventChoiceDefinition choice,
            RunContext context)
        {
            if (eventDefinition == null
                || choice == null
                || context == null
                || context.Events == null)
            {
                return EventResultApplicationResult.Failed(
                    EventResultFailureReason.InvalidState);
            }

            string resolvedFlag =
                BuildResolvedFlagName(
                    eventDefinition.Id);

            if (context.Events.HasFlag(
                    resolvedFlag))
            {
                return EventResultApplicationResult.Failed(
                    EventResultFailureReason.AlreadyResolved);
            }

            List<string> summaries =
                new List<string>();

            IReadOnlyList<EventEffect> effects =
                choice.Results;

            for (int index = 0;
                 index < effects.Count;
                 index++)
            {
                EventEffect effect =
                    effects[index];

                if (effect == null)
                {
                    continue;
                }

                string summary =
                    ApplyEffect(
                        effect,
                        context);

                if (!string.IsNullOrEmpty(
                        summary))
                {
                    summaries.Add(
                        summary);
                }
            }

            context.Events.SetFlag(
                resolvedFlag,
                true);

            return EventResultApplicationResult.Succeeded(
                summaries);
        }

        private static string ApplyEffect(
            EventEffect effect,
            RunContext context)
        {
            switch (effect.Kind)
            {
                case EventEffectKind.RestoreHp:
                    return ApplyResourceChange(
                        context.Player,
                        ResourceKind.Hp,
                        effect.Value);

                case EventEffectKind.RestoreMana:
                    return ApplyResourceChange(
                        context.Player,
                        ResourceKind.Mana,
                        effect.Value);

                case EventEffectKind.RestoreStamina:
                    return ApplyResourceChange(
                        context.Player,
                        ResourceKind.Stamina,
                        effect.Value);

                case EventEffectKind.GainGold:
                    return ApplyGoldChange(
                        context.Player,
                        effect.Value);

                case EventEffectKind.GainItem:
                    return ApplyItemChange(
                        context.Inventory,
                        effect);

                case EventEffectKind.SetFlag:
                    context.Events.SetFlag(
                        effect.TargetId,
                        effect.FlagValue);

                    return $"조건 '{effect.TargetId}' {(effect.FlagValue ? "설정" : "해제")}됨";

                case EventEffectKind.RelationshipChange:
                    // 113일차 이후 관계 시스템이 생기면 여기서 실제 호감도를 조정한다.
                    // 지금은 저장할 곳이 없어 데이터만 통과시키고 적용은 하지 않는다.
                    return null;

                default:
                    return null;
            }
        }

        private enum ResourceKind
        {
            Hp,
            Mana,
            Stamina
        }

        private static string ApplyResourceChange(
            PlayerRunState player,
            ResourceKind resource,
            int amount)
        {
            if (player == null
                || amount == 0)
            {
                return null;
            }

            StatBlock finalStats =
                player.GetFinalStats();

            switch (resource)
            {
                case ResourceKind.Hp:
                    player.CurrentHp =
                        Clamp(
                            player.CurrentHp
                            + amount,
                            0,
                            finalStats.MaxHealth);

                    return BuildResourceSummary(
                        "체력",
                        amount);

                case ResourceKind.Mana:
                    player.CurrentMana =
                        Clamp(
                            player.CurrentMana
                            + amount,
                            0,
                            finalStats.MaxMana);

                    return BuildResourceSummary(
                        "마나",
                        amount);

                case ResourceKind.Stamina:
                    player.CurrentStamina =
                        Clamp(
                            player.CurrentStamina
                            + amount,
                            0,
                            finalStats.MaxStamina);

                    return BuildResourceSummary(
                        "정력",
                        amount);

                default:
                    return null;
            }
        }

        private static string ApplyGoldChange(
            PlayerRunState player,
            int amount)
        {
            if (player == null
                || amount == 0)
            {
                return null;
            }

            if (amount > 0)
            {
                int earned =
                    GoldService.Earn(
                        player,
                        amount);

                return $"골드 +{earned}";
            }

            int spendAmount =
                Math.Min(
                    player.Gold,
                    -amount);

            GoldService.TrySpend(
                player,
                spendAmount);

            return $"골드 -{spendAmount}";
        }

        private static string ApplyItemChange(
            InventoryRunState inventory,
            EventEffect effect)
        {
            if (inventory == null
                || string.IsNullOrEmpty(
                    effect.TargetId)
                || effect.Value == 0)
            {
                return null;
            }

            if (effect.Value > 0)
            {
                inventory.TryAdd(
                    effect.TargetId,
                    effect.DisplayName,
                    effect.Value,
                    out _);

                return $"{effect.DisplayName} +{effect.Value}개";
            }

            int remainingToRemove =
                -effect.Value;

            for (int index = 0;
                 index < inventory.Slots.Count
                 && remainingToRemove > 0;
                 index++)
            {
                InventorySlotState slot =
                    inventory.Slots[index];

                if (slot == null
                    || slot.IsEmpty
                    || slot.ItemId != effect.TargetId)
                {
                    continue;
                }

                if (inventory.TryRemoveQuantityAt(
                        index,
                        remainingToRemove,
                        out int removedQuantity))
                {
                    remainingToRemove -=
                        removedQuantity;
                }
            }

            int actuallyRemoved =
                -effect.Value
                - remainingToRemove;

            return $"{effect.DisplayName} -{actuallyRemoved}개";
        }

        private static string BuildResourceSummary(
            string label,
            int amount)
        {
            return amount > 0
                ? $"{label} +{amount}"
                : $"{label} {amount}";
        }

        private static int Clamp(
            int value,
            int min,
            int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static string BuildResolvedFlagName(
            string eventId)
        {
            return ResolvedFlagPrefix
                + (eventId
                    ?? string.Empty);
        }
    }
}
