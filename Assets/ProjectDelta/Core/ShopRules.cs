using System; // 저장 식별자와 정수 범위
using System.Collections.Generic; // 고정 재고와 중복 검사
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    [Serializable] // 층별 상점의 상품 저장 형식
    public sealed class ShopOfferData // 가격과 수량이 확정된 상품
    { // 클래스 시작
        public string offerId; // 상점 내부의 고유 상품 식별자
        public string itemId; // 실제 소비품 또는 장비 식별자
        public int price; // 처음 생성한 단위 구매 가격
        public int initialStock; // 최초 상품 수량
        public int remaining; // 현재 남은 상품 수량
        public EquipmentVariantData equipment; // 선택적인 단일 개별 장비
    } // 클래스 끝
    [Serializable] // 재방문과 재시작을 위한 상점 저장 형식
    public sealed class ShopStockData // 한 층에 고정된 상품과 재고
    { // 클래스 시작
        public int floor; // 처음 상품을 확정한 층
        public ShopOfferData[] offers; // 고유 상품과 남은 수량
    } // 클래스 끝
    public sealed class ShopTradeResult // 저장 전 독립 거래 결과
    { // 클래스 시작
        public ShopState Shop { get; } // 거래 이후의 독립 상점 상태
        public InventoryState Inventory { get; } // 거래 이후의 독립 인벤토리
        public int Gold { get; } // 거래 이후의 골드 잔액
        internal ShopTradeResult(ShopState shop, InventoryState inventory, int gold) // 검증된 결과 구성
        { // 생성 시작
            Shop = shop; // 확인된 상점 보관
            Inventory = inventory; // 확인된 인벤토리 보관
            Gold = gold; // 확인된 골드 보관
        } // 생성 끝
    } // 클래스 끝
    public sealed class ShopState // 외부 변경을 막는 층별 고정 상점
    { // 클래스 시작
        private const int MaxOffers = 64; // 저장 검증의 상품 수 제한
        private readonly ShopStockData data; // 독립된 현재 가격과 재고
        private ShopState(ShopStockData data) // 검증된 상점 구성
        { // 구성 시작
            this.data = Copy(data); // 외부 변경을 막는 상품 복사
        } // 구성 끝
        public static bool TryRestore(ShopStockData saved, out ShopState shop) // 처음 확정한 상품 또는 저장된 상점 복원
        { // 복원 시작
            shop = null; // 기본 복원 결과 없음
            if (saved == null || saved.floor < 1 || saved.offers == null || saved.offers.Length > MaxOffers) // 층과 상품 목록 범위 검사
            { // 기본 오류 시작
                return false; // 누락되거나 지나치게 많은 상품 차단
            } // 기본 오류 끝
            var offerIds = new HashSet<string>(StringComparer.Ordinal); // 상점 내부의 고유 상품 식별자
            var instanceIds = new HashSet<Guid>(); // 고유한 개별 장비 식별자
            foreach (var offer in saved.offers) // 저장된 상품 순회
            { // 상품 검사 시작
                if (offer == null || !ValidId(offer.offerId) || !offerIds.Add(offer.offerId) || offer.price <= 0 || offer.initialStock < 0 || offer.remaining < 0 || offer.remaining > offer.initialStock || !TryItem(offer.itemId, out _, out _)) // 상품 식별과 고정 가격 및 재고 검사
                { // 상품 오류 시작
                    return false; // 알 수 없는 상품과 잘못된 재고 차단
                } // 상품 오류 끝
                if (offer.equipment != null && (!TrialEquipmentTraits.IsValid(offer.equipment) || offer.equipment.definitionId != offer.itemId || offer.initialStock != 1 || !Guid.TryParseExact(offer.equipment.instanceId, "N", out var instanceId) || !instanceIds.Add(instanceId))) // 개별 장비의 단일 재고와 고유 식별자 검사
                { // 개별 장비 오류 시작
                    return false; // 손상 장비와 중복 인스턴스 차단
                } // 개별 장비 오류 끝
            } // 상품 검사 끝
            shop = new ShopState(saved); // 검증된 독립 상품과 재고
            return true; // 정상 상점 복원 성공
        } // 복원 끝
        public ShopStockData Snapshot() // 현재 고정 가격과 재고의 저장본
        { // 복사 시작
            return Copy(data); // 외부 변경과 분리된 상점 데이터
        } // 복사 끝
        public bool TryBuy(int offerIndex, int quantity, int gold, InventoryState inventory, out ShopTradeResult result, out string error) // 확정된 가격과 재고로 구매 준비
        { // 구매 시작
            result = null; // 기본 거래 결과 없음
            error = string.Empty; // 기본 오류 없음
            if (inventory == null || gold < 0 || offerIndex < 0 || offerIndex >= data.offers.Length || quantity <= 0) // 거래 대상과 범위 검사
            { // 입력 오류 시작
                error = "구매할 상품을 확인할 수 없습니다."; // 잘못된 요청 안내
                return false; // 기존 상점과 인벤토리 보존
            } // 입력 오류 끝
            var offer = data.offers[offerIndex]; // 고정된 실제 상품
            if (quantity > offer.remaining || offer.equipment != null && quantity != 1) // 실제 재고와 단일 장비 검사
            { // 재고 부족 시작
                error = "상품 재고가 부족합니다."; // 실제 재고 안내
                return false; // 잘못된 수량 차단
            } // 재고 부족 끝
            var cost = (long)offer.price * quantity; // 정수 범위를 보호한 전체 가격
            if (cost > gold) // 현재 잔액 검사
            { // 잔액 부족 시작
                error = "골드가 부족합니다."; // 현재 잔액 부족 안내
                return false; // 골드와 재고 보존
            } // 잔액 부족 끝
            var nextInventory = InventoryState.FromSavedSlots(inventory.SaveSlots()); // 원본 인벤토리의 독립 복사
            if (nextInventory == null || !TryItem(offer.itemId, out var name, out var limit)) // 인벤토리와 상품 정의 검사
            { // 복사 오류 시작
                error = "인벤토리 상태를 확인할 수 없습니다."; // 인벤토리 오류 안내
                return false; // 변경되지 않은 원본 보존
            } // 복사 오류 끝
            var added = offer.equipment == null ? nextInventory.Add(offer.itemId, name, quantity, limit).Remaining == 0 : nextInventory.TryAddEquipment(offer.equipment); // 전체 수량 또는 고유 장비 수용
            if (!added) // 실제 상품의 전체 수용 검사
            { // 공간 부족 시작
                error = "인벤토리 공간이 부족하거나 장비가 중복됩니다."; // 실제 수용 실패 안내
                return false; // 부분 지급과 골드 차감 차단
            } // 공간 부족 끝
            var snapshot = Copy(data); // 거래 후 재고의 독립 저장본
            snapshot.offers[offerIndex].remaining -= quantity; // 구매한 수량만큼 재고 감소
            result = new ShopTradeResult(new ShopState(snapshot), nextInventory, (int)(gold - cost)); // 가격과 재고 및 인벤토리의 독립 결과
            return true; // 유효한 구매 준비 완료
        } // 구매 끝
        public bool TrySell(int offerIndex, int slotIndex, int quantity, int gold, InventoryState inventory, out ShopTradeResult result, out string error) // 해당 상품의 현재 가격을 기준으로 판매 준비
        { // 판매 시작
            result = null; // 기본 거래 결과 없음
            error = string.Empty; // 기본 오류 없음
            if (inventory == null || gold < 0 || offerIndex < 0 || offerIndex >= data.offers.Length || slotIndex < 0 || slotIndex >= inventory.Slots.Count || quantity <= 0) // 상품과 슬롯 및 수량 범위 검사
            { // 입력 오류 시작
                error = "판매할 아이템을 확인할 수 없습니다."; // 잘못된 판매 요청 안내
                return false; // 기존 인벤토리와 골드 보존
            } // 입력 오류 끝
            var offer = data.offers[offerIndex]; // 현재 가격을 제공하는 상품
            var slot = inventory.Slots[slotIndex]; // 실제 판매할 보유 묶음
            if (slot.IsEmpty || slot.ItemId != offer.itemId || quantity > slot.Quantity) // 같은 상품과 보유 수량 검사
            { // 상품 오류 시작
                error = "해당 가격의 아이템이 없거나 수량이 부족합니다."; // 상품과 수량 안내
                return false; // 다른 상품과 과다 판매 차단
            } // 상품 오류 끝
            var item = slot.Equipment; // 실제 판매할 개별 장비 복사
            if ((item == null) != (offer.equipment == null) || item != null && (quantity != 1 || offer.remaining != 0 || !string.Equals(item.instanceId, offer.equipment.instanceId, StringComparison.OrdinalIgnoreCase))) // 개별 장비의 동일 상품과 구매 상태 검사
            { // 장비 가격 오류 시작
                error = "이 개별 장비의 판매 가격은 아직 정해지지 않았습니다."; // 다른 장비의 가격 적용 방지
                return false; // 확인되지 않은 가격의 판매 차단
            } // 장비 가격 오류 끝
            var proceeds = (long)(offer.price / 2) * quantity; // 상품별 반값의 시험 버림과 정수 범위 보호
            if (proceeds > int.MaxValue - (long)gold) // 판매 후 잔액 범위 검사
            { // 범위 초과 시작
                error = "판매 후 골드가 허용 범위를 초과합니다."; // 정수 범위 초과 안내
                return false; // 넘침과 아이템 소실 차단
            } // 범위 초과 끝
            var nextInventory = InventoryState.FromSavedSlots(inventory.SaveSlots()); // 원본 인벤토리의 독립 복사
            if (nextInventory == null || !nextInventory.Remove(slotIndex, quantity, out var removed) || removed != quantity) // 실제 전체 수량 제거 검사
            { // 제거 오류 시작
                error = "판매할 아이템을 확인할 수 없습니다."; // 실제 제거 오류 안내
                return false; // 원본 아이템과 골드 보존
            } // 제거 오류 끝
            result = new ShopTradeResult(new ShopState(data), nextInventory, gold + (int)proceeds); // 기존 재고와 변경된 소지품 및 골드
            return true; // 유효한 판매 준비 완료
        } // 판매 끝
        private static bool TryItem(string id, out string name, out int stackLimit) // 실제 지원 아이템 정의 조회
        { // 조회 시작
            name = null; // 기본 이름 없음
            stackLimit = 0; // 기본 중첩 한도 없음
            var consumable = TrialConsumableRules.Find(id); // 등록된 시험 소비품 조회
            if (consumable != null) // 실제 소비품 검사
            { // 소비품 시작
                name = consumable.Name; // 등록된 소비품 이름
                stackLimit = consumable.StackLimit; // 실제 소비품 중첩 한도
                return true; // 유효한 소비품 확인
            } // 소비품 끝
            var equipment = EquipmentCatalog.Find(id); // 등록된 시험 장비 조회
            if (equipment == null) // 실제 장비 검사
            { // 미등록 장비 시작
                return false; // 유물과 미등록 상품 차단
            } // 미등록 장비 끝
            name = equipment.Name; // 등록된 장비 이름
            stackLimit = 1; // 장비의 중첩 금지
            return true; // 유효한 장비 확인
        } // 조회 끝
        private static bool ValidId(string id) // 고유 상품 식별자의 범위 검사
        { // 검사 시작
            return !string.IsNullOrWhiteSpace(id) && id.Length <= 128; // 공백과 긴 식별자 차단
        } // 검사 끝
        private static ShopStockData Copy(ShopStockData source) // 상점과 개별 장비의 독립 복사
        { // 복사 시작
            var offers = new ShopOfferData[source.offers.Length]; // 상품별 독립 배열
            for (var index = 0; index < offers.Length; index++) // 실제 상품 순회
            { // 상품 복사 시작
                var offer = source.offers[index]; // 현재 고정 상품
                offers[index] = new ShopOfferData { offerId = offer.offerId, itemId = offer.itemId, price = offer.price, initialStock = offer.initialStock, remaining = offer.remaining, equipment = EquipmentVariantRules.Copy(offer.equipment) }; // 가격과 수량 및 개별 장비 복사
            } // 상품 복사 끝
            return new ShopStockData { floor = source.floor, offers = offers }; // 층별 상점의 독립 데이터
        } // 복사 끝
    } // 클래스 끝
} // 이름 공간 끝
