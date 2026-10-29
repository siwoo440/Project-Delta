using System; // 개별 거래 검증 도구
using NUnit.Framework; // 상점 단위 검증 도구
using ProjectDelta.Core; // 상점과 인벤토리 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class ShopRulesTests // 시험 상점의 거래와 복원 검증
    { // 클래스 시작
        private static ShopState CreateShop() // 고정된 두 시험 상품의 상점 생성
        { // 생성 시작
            var data = new ShopStockData { floor = 1, offers = new[] { new ShopOfferData { offerId = "potion", itemId = "trial-small-potion", price = 41, initialStock = 2, remaining = 2 }, new ShopOfferData { offerId = "sword", itemId = EquipmentCatalog.BasicSwordId, price = 100, initialStock = 1, remaining = 1 } } }; // 기획 범위의 고정 시험 가격
            Assert.That(ShopState.TryRestore(data, out var shop), Is.True); // 정상 시험 상점 생성
            return shop; // 독립 상점 상태 전달
        } // 생성 끝
        [Test] // 구매와 재방문 및 저장 복원 검증
        public void Buy_PreservesPriceAndStockAcrossRestore() // 고정 가격과 재고 감소 확인
        { // 검증 시작
            var shop = CreateShop(); // 첫 방문의 고정 상점
            var inventory = new InventoryState(); // 최초 빈 인벤토리
            Assert.That(shop.TryBuy(0, 1, 100, inventory, out var result, out var error), Is.True, error); // 실제 회복약 구매
            Assert.That(result.Gold, Is.EqualTo(59)); // 구매 가격 차감
            Assert.That(result.Inventory.Slots[0].ItemId, Is.EqualTo("trial-small-potion")); // 실제 아이템 획득
            Assert.That(shop.Snapshot().offers[0].remaining, Is.EqualTo(2)); // 기존 불변 상점 보존
            Assert.That(inventory.Slots[0].IsEmpty, Is.True); // 기존 인벤토리 보존
            var snapshot = result.Shop.Snapshot(); // 거래 이후의 저장 상태
            Assert.That(ShopState.TryRestore(snapshot, out var restored), Is.True); // 저장 상태 복원
            Assert.That(restored.Snapshot().offers[0].price, Is.EqualTo(41)); // 동일 층의 고정 가격 유지
            Assert.That(restored.Snapshot().offers[0].remaining, Is.EqualTo(1)); // 동일 층의 남은 재고 유지
            snapshot.offers[0].remaining = 0; // 반환한 저장본만 수정
            Assert.That(restored.Snapshot().offers[0].remaining, Is.EqualTo(1)); // 저장본과 상점의 독립성 확인
        } // 검증 끝
        [Test] // 부족한 골드와 재고 및 공간 검증
        public void Buy_RejectsInsufficientGoldStockAndSpaceWithoutMutation() // 실패한 거래의 상태 보존
        { // 검증 시작
            var shop = CreateShop(); // 고정 상점 준비
            var inventory = new InventoryState(); // 빈 인벤토리
            Assert.That(shop.TryBuy(0, 1, 40, inventory, out _, out _), Is.False); // 부족한 골드 차단
            Assert.That(shop.TryBuy(0, 3, 1000, inventory, out _, out _), Is.False); // 부족한 재고 차단
            for (var index = 0; index < 10; index++) // 모든 기본 슬롯 채우기
            { // 슬롯 채우기 시작
                inventory.Add("full-" + index, "기존 아이템", 1, 1); // 각 슬롯의 독립 보유품
            } // 슬롯 채우기 끝
            Assert.That(shop.TryBuy(0, 1, 1000, inventory, out _, out _), Is.False); // 빈 공간 없는 구매 차단
            Assert.That(shop.Snapshot().offers[0].remaining, Is.EqualTo(2)); // 실패한 구매의 재고 보존
            Assert.That(inventory.Slots[0].ItemId, Is.EqualTo("full-0")); // 기존 아이템 보존
        } // 검증 끝
        [Test] // 일부 수량만 들어갈 때 전체 거래 취소 검증
        public void Buy_RejectsPartialStackWithoutChangingOriginalInventory() // 부분 삽입의 롤백 확인
        { // 검증 시작
            var shop = CreateShop(); // 두 회복약의 고정 재고
            var inventory = new InventoryState(); // 기본 인벤토리
            inventory.Add("trial-small-potion", "소형 회복약", 4, 5); // 한 개만 더 들어가는 기존 묶음
            for (var index = 0; index < 9; index++) // 남은 아홉 슬롯 채우기
            { // 슬롯 채우기 시작
                inventory.Add("full-" + index, "기존 아이템", 1, 1); // 각 슬롯의 독립 보유품
            } // 슬롯 채우기 끝
            Assert.That(shop.TryBuy(0, 2, 100, inventory, out _, out _), Is.False); // 일부만 넣을 수 있는 구매 차단
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(4)); // 원본 묶음의 수량 보존
            Assert.That(shop.Snapshot().offers[0].remaining, Is.EqualTo(2)); // 원본 상점의 재고 보존
        } // 검증 끝
        [Test] // 판매 금액의 시험 버림과 원본 보존 검증
        public void Sell_UsesHalfOfCurrentPurchasePriceRoundedDown() // 홀수 가격의 개별 판매가 확인
        { // 검증 시작
            var shop = CreateShop(); // 현재 가격 41의 시험 상점
            var inventory = new InventoryState(); // 최초 인벤토리
            inventory.Add("trial-small-potion", "소형 회복약", 2, 5); // 판매할 회복약 묶음
            Assert.That(shop.TrySell(0, 0, 2, 10, inventory, out var result, out var error), Is.True, error); // 같은 상품 두 개 판매
            Assert.That(result.Gold, Is.EqualTo(50)); // 개당 20골드의 시험 버림 적용
            Assert.That(result.Inventory.Slots[0].IsEmpty, Is.True); // 확인된 수량만 판매
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(2)); // 기존 인벤토리 보존
            Assert.That(result.Shop.Snapshot().offers[0].remaining, Is.EqualTo(2)); // 판매가 상점 재고를 임의로 늘리지 않음
            Assert.That(shop.TrySell(0, 0, 3, 10, inventory, out _, out _), Is.False); // 소지 수량 초과 차단
            Assert.That(shop.TrySell(0, 0, 1, int.MaxValue, inventory, out _, out _), Is.False); // 골드 정수 초과 차단
        } // 검증 끝
        [Test] // 개별 장비의 구매와 같은 상품의 판매 검증
        public void Equipment_TransfersTheUniqueVariantWithoutReroll() // 옵션과 개별 식별자의 보존 확인
        { // 검증 시작
            var variant = TrialEquipmentTraits.Create(EquipmentCatalog.BasicSwordId, EquipmentVariantRules.Rare, true); // 공개된 시험 저주 장비
            var data = new ShopStockData { floor = 1, offers = new[] { new ShopOfferData { offerId = "unique", itemId = variant.definitionId, price = 100, initialStock = 1, remaining = 1, equipment = variant } } }; // 단일 개별 상품
            Assert.That(ShopState.TryRestore(data, out var shop), Is.True); // 유효한 개별 상품 확인
            var source = new InventoryState(); // 빈 인벤토리
            Assert.That(shop.TryBuy(0, 1, 100, source, out var purchase, out var error), Is.True, error); // 개별 장비 구매
            Assert.That(purchase.Inventory.Slots[0].Equipment.instanceId, Is.EqualTo(variant.instanceId)); // 실제 상품의 인스턴스 유지
            Assert.That(purchase.Inventory.Slots[0].Equipment.cursed, Is.True); // 사전 공개된 저주 보존
            Assert.That(purchase.Shop.TryBuy(0, 1, 1000, source, out _, out _), Is.False); // 단일 장비의 중복 판매 차단
            Assert.That(purchase.Shop.TrySell(0, 0, 1, 0, purchase.Inventory, out var sold, out error), Is.True, error); // 동일 개별 장비의 판매
            Assert.That(sold.Gold, Is.EqualTo(50)); // 현재 구매 가격의 절반 지급
            Assert.That(sold.Inventory.Slots[0].IsEmpty, Is.True); // 판매한 인스턴스 제거
        } // 검증 끝
        [Test] // 손상된 재고와 가격 및 식별자 검증
        public void Restore_RejectsMalformedOrDuplicateOfferData() // 손상된 상점의 복원 차단
        { // 검증 시작
            var snapshot = CreateShop().Snapshot(); // 정상 저장본 준비
            snapshot.offers[0].remaining = 3; // 최초 재고를 넘는 현재 재고
            Assert.That(ShopState.TryRestore(snapshot, out _), Is.False); // 재고 초과 차단
            snapshot = CreateShop().Snapshot(); // 정상 저장본 재준비
            snapshot.offers[0].price = 0; // 유효하지 않은 가격
            Assert.That(ShopState.TryRestore(snapshot, out _), Is.False); // 잘못된 가격 차단
            snapshot = CreateShop().Snapshot(); // 정상 저장본 재준비
            snapshot.offers[1].offerId = snapshot.offers[0].offerId; // 중복 상품 식별자
            Assert.That(ShopState.TryRestore(snapshot, out _), Is.False); // 중복 상품 차단
            snapshot = CreateShop().Snapshot(); // 정상 저장본 재준비
            snapshot.offers[0].itemId = "unknown"; // 등록되지 않은 아이템
            Assert.That(ShopState.TryRestore(snapshot, out _), Is.False); // 알 수 없는 상품 차단
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
