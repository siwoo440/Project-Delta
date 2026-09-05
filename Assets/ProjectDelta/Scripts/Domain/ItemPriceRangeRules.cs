namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 130일차: 기획서 6.5절 "상점 상품 및 가격 규칙" - 카테고리별 정가 대신 범위를 두고,
    // 상점에 진열될 때(NPC가 층에 처음 등장할 때) 그 범위 안에서 실제 가격을 굴린다.
    // ItemDefinition에는 아직 장비 세부 등급(일반/고급/희귀/전설) 필드가 없어서, 기획서의
    // 세부 등급 구간은 대표값(가장 넓은 실사용 구간)으로 단순화했다 - 추후 장비에 등급
    // 필드가 생기면 그 등급까지 반영하도록 확장할 자리다.
    public static class ItemPriceRangeRules // 카테고리별 상점 가격 범위 규칙
    {
        public static bool TryGetPriceRange( // 카테고리(+가방 여부)로 가격 범위 조회
            ItemCategory category, // 아이템 분류
            bool isBag, // 가방 아이템(BagTier != None) 여부 - 분류보다 우선한다
            out int minPrice, // 최소 가격(포함)
            out int maxPrice) // 최대 가격(포함)
        {
            if (isBag) // 가방이면 분류와 무관하게 가방 가격대 사용
            {
                minPrice = 120; // 가방 최소가
                maxPrice = 700; // 가방 최대가
                return true; // 가격대 있음
            }

            switch (category) // 분류별 가격대 분기
            {
                case ItemCategory.Consumable: // 소비 아이템
                    minPrice = 20; // 최소가
                    maxPrice = 60; // 최대가(일반 소비 아이템 기준)
                    return true; // 가격대 있음

                case ItemCategory.ExplorationTool: // 탐험 도구
                    minPrice = 30; // 최소가
                    maxPrice = 150; // 최대가
                    return true; // 가격대 있음

                case ItemCategory.Equipment: // 장비
                    minPrice = 80; // 최소가(일반 장비 기준)
                    maxPrice = 160; // 최대가(일반 장비 기준)
                    return true; // 가격대 있음

                case ItemCategory.Relic: // 유물
                    minPrice = 250; // 최소가(일반 유물 기준)
                    maxPrice = 600; // 최대가(일반 유물 기준)
                    return true; // 가격대 있음

                case ItemCategory.Cursed: // 저주 유물
                    minPrice = 300; // 최소가
                    maxPrice = 700; // 최대가
                    return true; // 가격대 있음

                default: // Treasure·KeyItem·Uncategorized 등은 상점에서 판매하지 않는다
                    minPrice = 0; // 값 없음
                    maxPrice = 0; // 값 없음
                    return false; // 가격대 없음(상점에 진열 안 함)
            }
        }
    }
}
