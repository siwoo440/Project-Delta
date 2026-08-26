namespace ProjectDelta.Domain
{
    // 아이템 행동이 금지인지, 바로 가능한지, 별도 조건 검사가 필요한지를 표현한다.
    public enum ItemActionAvailability
    {
        // 사용할 수 없다.
        Unavailable = 0,

        // 별도 조건 없이 사용할 수 있다.
        Available = 1,

        // 아이템별 또는 상황별 추가 조건 검사가 필요하다.
        Conditional = 2
    }

    // 아이템 종류별 공통 행동 규칙의 단일 기준이다.
    public static class ItemCategoryRules
    {
        // 실제 게임에서 사용하는 7개 분류인지 확인한다.
        public static bool IsGameplayCategory(
            ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Consumable:
                case ItemCategory.ExplorationTool:
                case ItemCategory.KeyItem:
                case ItemCategory.Treasure:
                case ItemCategory.Equipment:
                case ItemCategory.Relic:
                case ItemCategory.Cursed:
                    return true;

                default:
                    return false;
            }
        }

        // UI에 표시할 한글 분류명을 반환한다.
        public static string GetDisplayName(
            ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Consumable:
                    return "소비 아이템";

                case ItemCategory.ExplorationTool:
                    return "탐험 도구";

                case ItemCategory.KeyItem:
                    return "중요 아이템";

                case ItemCategory.Treasure:
                    return "보물";

                case ItemCategory.Equipment:
                    return "장비";

                case ItemCategory.Relic:
                    return "유물";

                case ItemCategory.Cursed:
                    return "저주";

                default:
                    return "미분류";
            }
        }

        // 종류별 사용 가능 상태를 반환한다.
        public static ItemActionAvailability GetUseAvailability(
            ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Consumable:
                case ItemCategory.ExplorationTool:
                    return ItemActionAvailability.Available;

                default:
                    return ItemActionAvailability.Unavailable;
            }
        }

        // 종류별 판매 가능 상태를 반환한다.
        public static ItemActionAvailability GetSellAvailability(
            ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Consumable:
                case ItemCategory.ExplorationTool:
                case ItemCategory.Treasure:
                case ItemCategory.Equipment:
                    return ItemActionAvailability.Available;

                case ItemCategory.Relic:
                    return ItemActionAvailability.Conditional;

                default:
                    return ItemActionAvailability.Unavailable;
            }
        }

        // 종류별 버리기 가능 상태를 반환한다.
        public static ItemActionAvailability GetDiscardAvailability(
            ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Consumable:
                case ItemCategory.ExplorationTool:
                case ItemCategory.Treasure:
                case ItemCategory.Equipment:
                    return ItemActionAvailability.Available;

                case ItemCategory.Relic:
                case ItemCategory.Cursed:
                    return ItemActionAvailability.Conditional;

                default:
                    return ItemActionAvailability.Unavailable;
            }
        }

        // 종류별 장착 가능 상태를 반환한다.
        public static ItemActionAvailability GetEquipAvailability(
            ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Equipment:
                    return ItemActionAvailability.Available;

                case ItemCategory.Cursed:
                    return ItemActionAvailability.Conditional;

                default:
                    return ItemActionAvailability.Unavailable;
            }
        }

        // 즉시 사용 가능한 종류인지 반환한다.
        public static bool CanUse(
            ItemCategory category)
        {
            return GetUseAvailability(
                category)
                == ItemActionAvailability.Available;
        }

        // 즉시 판매 가능한 종류인지 반환한다.
        public static bool CanSell(
            ItemCategory category)
        {
            return GetSellAvailability(
                category)
                == ItemActionAvailability.Available;
        }

        // 즉시 버릴 수 있는 종류인지 반환한다.
        public static bool CanDiscard(
            ItemCategory category)
        {
            return GetDiscardAvailability(
                category)
                == ItemActionAvailability.Available;
        }

        // 즉시 장착 가능한 종류인지 반환한다.
        public static bool CanEquip(
            ItemCategory category)
        {
            return GetEquipAvailability(
                category)
                == ItemActionAvailability.Available;
        }
    }
}
