using System; // 시험 상점 가격의 범위 추첨
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class TrialShopCatalog // 정식 상품 구성이 아닌 시험 상점
    { // 클래스 시작
        public static ShopStockData Create(int floor, Random random) // 층의 최초 시험 상품과 가격 결정
        { // 생성 시작
            if (floor < 1 || floor > 2 || random == null) // 시험 층과 난수 검사
            { // 입력 오류 시작
                return null; // 잘못된 상점 생성 차단
            } // 입력 오류 끝
            return new ShopStockData // 결정된 층별 상품 저장본
            { // 저장본 구성 시작
                floor = floor, // 실제 상점 층
                offers = new[] // 시험 상품 목록
                { // 상품 목록 시작
                    new ShopOfferData { offerId = "trial-small", itemId = "trial-small-potion", price = random.Next(20, 61), initialStock = 3, remaining = 3 }, // 일반 소비품의 기획 가격 범위
                    new ShopOfferData { offerId = "trial-large", itemId = "trial-large-potion", price = random.Next(50, 121), initialStock = 2, remaining = 2 }, // 고급 소비품의 기획 가격 범위
                    new ShopOfferData { offerId = "trial-mana", itemId = "trial-mana-potion", price = random.Next(20, 61), initialStock = 2, remaining = 2 }, // 시험 마나 소비품 가격
                    new ShopOfferData { offerId = "trial-sword", itemId = EquipmentCatalog.BasicSwordId, price = random.Next(80, 161), initialStock = 1, remaining = 1 } // 일반 시험 장비 가격
                } // 상품 목록 끝
            }; // 상점 저장본 끝
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
