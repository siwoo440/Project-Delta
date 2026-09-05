namespace ProjectDelta.Application // 애플리케이션 네임스페이스
{
    // 130일차: 상점 재고를 구성하는 순간 필요한 강화 수치 3종을 한 번에 담아 전달한다.
    public sealed class ShopUpgradeSnapshot // 상점 강화 수치 스냅샷
    {
        public int DiscountPercent { get; } // 구매 할인율(%)
        public int BonusStockCount { get; } // 기본 재고에 더할 추가 슬롯 수
        public int RareChancePercent { get; } // 희귀 상품(고급 카테고리) 우선 배정 확률(%)

        public ShopUpgradeSnapshot( // 스냅샷 생성자
            int discountPercent, // 구매 할인율
            int bonusStockCount, // 추가 재고 수
            int rareChancePercent) // 희귀 상품 확률
        {
            DiscountPercent = // 할인율 저장
                discountPercent;

            BonusStockCount = // 추가 재고 저장
                bonusStockCount;

            RareChancePercent = // 희귀 확률 저장
                rareChancePercent;
        }
    }
}
