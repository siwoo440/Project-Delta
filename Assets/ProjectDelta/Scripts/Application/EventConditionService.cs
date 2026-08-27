using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    public enum EventChoiceAvailability
    {
        Available = 0,
        Unavailable = 1
    }

    public sealed class EventChoiceAvailabilityResult
    {
        public EventChoiceAvailability Availability { get; private set; }

        // Unavailable일 때만 값이 있다. "숨기지 않고 사유를 표시한다"는 요구를 위한 필드다.
        public string UnavailableReason { get; private set; }

        public bool IsAvailable =>
            Availability
                == EventChoiceAvailability.Available;

        public static EventChoiceAvailabilityResult Available()
        {
            return new EventChoiceAvailabilityResult
            {
                Availability = EventChoiceAvailability.Available
            };
        }

        public static EventChoiceAvailabilityResult Unavailable(
            string reason)
        {
            return new EventChoiceAvailabilityResult
            {
                Availability = EventChoiceAvailability.Unavailable,
                UnavailableReason = reason
            };
        }
    }

    // 107일차: 선택지별 능력치·아이템·골드·플래그 조건을 검사한다.
    // 선택지는 조건을 전부 만족해야 Available이고, 첫 번째로 실패한 조건의
    // 사유를 그대로 UI에 보여줄 수 있게 반환한다(선택지를 숨기지 않는다).
    public static class EventConditionService
    {
        public static EventChoiceAvailabilityResult Evaluate(
            EventChoiceDefinition choice,
            RunContext context)
        {
            if (choice == null)
            {
                return EventChoiceAvailabilityResult.Unavailable(
                    "선택지를 찾을 수 없습니다.");
            }

            if (context == null)
            {
                return EventChoiceAvailabilityResult.Unavailable(
                    "현재 진행 중인 런이 없습니다.");
            }

            for (int index = 0;
                 index < choice.Conditions.Count;
                 index++)
            {
                EventCondition condition =
                    choice.Conditions[index];

                if (condition == null
                    || condition.Kind == EventConditionKind.None)
                {
                    continue;
                }

                if (!EvaluateCondition(
                        condition,
                        context,
                        out string failureReason))
                {
                    return EventChoiceAvailabilityResult.Unavailable(
                        failureReason);
                }
            }

            return EventChoiceAvailabilityResult.Available();
        }

        private static bool EvaluateCondition(
            EventCondition condition,
            RunContext context,
            out string failureReason)
        {
            switch (condition.Kind)
            {
                case EventConditionKind.Stat:
                    return EvaluateStat(
                        condition,
                        context.Player,
                        out failureReason);

                case EventConditionKind.Item:
                    return EvaluateItem(
                        condition,
                        context.Inventory,
                        out failureReason);

                case EventConditionKind.Gold:
                    return EvaluateGold(
                        condition,
                        context.Player,
                        out failureReason);

                case EventConditionKind.Flag:
                    return EvaluateFlag(
                        condition,
                        context.Events,
                        out failureReason);

                default:
                    failureReason =
                        string.Empty;

                    return true;
            }
        }

        private static bool EvaluateStat(
            EventCondition condition,
            PlayerRunState player,
            out string failureReason)
        {
            StatBlock finalStats =
                player != null
                    ? player.GetFinalStats()
                    : new StatBlock();

            int actualValue =
                ResolveStatValue(
                    finalStats,
                    condition.StatType);

            if (actualValue
                >= condition.RequiredValue)
            {
                failureReason =
                    string.Empty;

                return true;
            }

            failureReason =
                $"{GetStatDisplayName(condition.StatType)} {condition.RequiredValue} 이상 필요 (현재 {actualValue})";

            return false;
        }

        private static bool EvaluateItem(
            EventCondition condition,
            InventoryRunState inventory,
            out string failureReason)
        {
            int owned =
                0;

            if (inventory != null)
            {
                for (int index = 0;
                     index < inventory.Slots.Count;
                     index++)
                {
                    InventorySlotState slot =
                        inventory.Slots[index];

                    if (slot != null
                        && !slot.IsEmpty
                        && slot.ItemId == condition.TargetId)
                    {
                        owned +=
                            slot.Quantity;
                    }
                }
            }

            if (owned
                >= condition.RequiredValue)
            {
                failureReason =
                    string.Empty;

                return true;
            }

            failureReason =
                $"{condition.TargetId} {condition.RequiredValue}개 필요 (보유 {owned}개)";

            return false;
        }

        private static bool EvaluateGold(
            EventCondition condition,
            PlayerRunState player,
            out string failureReason)
        {
            int gold =
                player != null
                    ? player.Gold
                    : 0;

            if (gold
                >= condition.RequiredValue)
            {
                failureReason =
                    string.Empty;

                return true;
            }

            failureReason =
                $"골드 {condition.RequiredValue} 이상 필요 (보유 {gold})";

            return false;
        }

        private static bool EvaluateFlag(
            EventCondition condition,
            EventRunState events,
            out string failureReason)
        {
            bool hasFlag =
                events != null
                && events.HasFlag(
                    condition.TargetId);

            if (hasFlag
                == condition.RequiredFlagValue)
            {
                failureReason =
                    string.Empty;

                return true;
            }

            failureReason =
                condition.RequiredFlagValue
                    ? $"조건 '{condition.TargetId}'이(가) 필요합니다."
                    : $"조건 '{condition.TargetId}'이(가) 없어야 합니다.";

            return false;
        }

        private static int ResolveStatValue(
            StatBlock stats,
            EventStatType statType)
        {
            switch (statType)
            {
                case EventStatType.MaxHealth:
                    return stats.MaxHealth;

                case EventStatType.MaxMana:
                    return stats.MaxMana;

                case EventStatType.MaxStamina:
                    return stats.MaxStamina;

                case EventStatType.Attack:
                    return stats.Attack;

                case EventStatType.Defense:
                    return stats.Defense;

                case EventStatType.Speed:
                    return stats.Speed;

                case EventStatType.Charm:
                    return stats.Charm;

                case EventStatType.Evasion:
                    return stats.Evasion;

                case EventStatType.Resistance:
                    return stats.Resistance;

                default:
                    return 0;
            }
        }

        private static string GetStatDisplayName(
            EventStatType statType)
        {
            switch (statType)
            {
                case EventStatType.MaxHealth:
                    return "최대 체력";

                case EventStatType.MaxMana:
                    return "최대 마나";

                case EventStatType.MaxStamina:
                    return "최대 정력";

                case EventStatType.Attack:
                    return "공격력";

                case EventStatType.Defense:
                    return "방어력";

                case EventStatType.Speed:
                    return "속도";

                case EventStatType.Charm:
                    return "매력";

                case EventStatType.Evasion:
                    return "회피";

                case EventStatType.Resistance:
                    return "저항";

                default:
                    return "능력치";
            }
        }
    }
}
