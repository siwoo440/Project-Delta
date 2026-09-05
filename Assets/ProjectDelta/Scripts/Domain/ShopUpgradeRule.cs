namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 130일차: 기획서 6.6절 "상점 강화" - 4개 트랙 모두 단계별 비용이 일정한 배수가
    // 아니어서(특히 희귀 상품 확률) 공식 대신 표로 그대로 옮겨 담는다.
    public static class ShopUpgradeRule // 상점 강화 4종 규칙
    {
        // 구매 가격 할인 - 5단계, 2%p씩 증가.
        private static readonly int[] DiscountCosts = // 단계별 비용(조각)
        {
            5, 10, 15, 20, 25 // 1~5단계 비용
        };

        // 상점 재고 증가 - 3단계, 1개씩 증가.
        private static readonly int[] StockCosts = // 단계별 비용(조각)
        {
            8, 16, 24 // 1~3단계 비용
        };

        // 희귀 상품 확률 - 5단계, 2%p씩 증가.
        private static readonly int[] RareChanceCosts = // 단계별 비용(조각)
        {
            8, 12, 16, 20, 25 // 1~5단계 비용
        };

        // 판매 가격 증가 - 4단계, 기본 50%에서 5%p씩 증가.
        private static readonly int[] SellBonusCosts = // 단계별 비용(조각)
        {
            6, 12, 18, 24 // 1~4단계 비용
        };

        public const int BaseStockCount = 6; // 강화 전 기본 재고 수(기획서에 명시 없어 임시 지정 - 추후 조정 가능)

        public static int GetDiscountPercent( // 구매 할인율(%) 조회
            int level) // 현재 레벨
        {
            return ClampLevel(level, DiscountCosts.Length) * 2; // 1단계당 2%p
        }

        public static int GetBonusStockCount( // 재고 증가분 조회
            int level) // 현재 레벨
        {
            return ClampLevel(level, StockCosts.Length); // 1단계당 +1개
        }

        public static int GetRareChancePercent( // 희귀 상품 확률(%) 조회
            int level) // 현재 레벨
        {
            return ClampLevel(level, RareChanceCosts.Length) * 2; // 1단계당 2%p
        }

        public static int GetSellPricePercent( // 판매가(%) 조회 - 기본 50%
            int level) // 현재 레벨
        {
            return 50 // 기본 판매가 50%
                + (ClampLevel(level, SellBonusCosts.Length) * 5); // 1단계당 5%p 추가
        }

        public static bool TryGetDiscountUpgradeCost( // 다음 할인 단계 비용 조회
            int currentLevel, // 현재 레벨
            out int cost) // 다음 단계 비용
        {
            return TryGetCost(DiscountCosts, currentLevel, out cost); // 표에서 조회
        }

        public static bool TryGetStockUpgradeCost( // 다음 재고 단계 비용 조회
            int currentLevel, // 현재 레벨
            out int cost) // 다음 단계 비용
        {
            return TryGetCost(StockCosts, currentLevel, out cost); // 표에서 조회
        }

        public static bool TryGetRareChanceUpgradeCost( // 다음 희귀 확률 단계 비용 조회
            int currentLevel, // 현재 레벨
            out int cost) // 다음 단계 비용
        {
            return TryGetCost(RareChanceCosts, currentLevel, out cost); // 표에서 조회
        }

        public static bool TryGetSellBonusUpgradeCost( // 다음 판매가 단계 비용 조회
            int currentLevel, // 현재 레벨
            out int cost) // 다음 단계 비용
        {
            return TryGetCost(SellBonusCosts, currentLevel, out cost); // 표에서 조회
        }

        private static bool TryGetCost( // 공통 비용 표 조회 도우미
            int[] costs, // 대상 비용 표
            int currentLevel, // 현재 레벨
            out int cost) // 다음 단계 비용
        {
            if (currentLevel < 0 // 레벨이 음수거나
                || currentLevel >= costs.Length) // 이미 최대 레벨이면
            {
                cost = 0; // 다음 단계 없음
                return false; // 구매 불가
            }

            cost = costs[currentLevel]; // 다음 단계(0-index) 비용
            return true; // 구매 가능
        }

        private static int ClampLevel( // 레벨을 0~최대 사이로 자른다
            int level, // 원본 레벨
            int maxLevel) // 최대 레벨(표 길이)
        {
            if (level < 0) // 0 미만이면
            {
                return 0; // 0으로 고정
            }

            return level > maxLevel // 최대치 초과면
                ? maxLevel // 최대치로 고정
                : level; // 범위 안이면 그대로
        }
    }
}
