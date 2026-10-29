using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 인벤토리 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class InventoryStateTests // 슬롯과 획득 규칙 검증
    { // 클래스 시작
        [Test] // 슬롯 교환과 수량 보존 검증
        public void Swap_MovesAndExchangesCompleteStacks() // 빈 칸 이동과 서로 다른 묶음 교환
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            inventory.Add("potion", "물약", 3, 5); // 첫 아이템 묶음
            inventory.Add("herb", "약초", 2, 3); // 두 번째 아이템 묶음
            var method = typeof(InventoryState).GetMethod("TrySwap"); // 슬롯 교환 규칙 조회
            Assert.That(method, Is.Not.Null); // 슬롯 교환 기능 제공 확인
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                0, 8 // 슬롯과 아이템 인수
            }), Is.True); // 빈 칸으로 이동
            Assert.That(inventory.Slots[0].IsEmpty, Is.True); // 이전 칸 비움
            Assert.That(inventory.Slots[8].ItemId, Is.EqualTo("potion")); // 이동한 아이템 확인
            Assert.That(inventory.Slots[8].Quantity, Is.EqualTo(3)); // 이동한 수량 확인
            Assert.That(inventory.Slots[8].StackLimit, Is.EqualTo(5)); // 이동한 중첩 한도 확인
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                8, 1 // 슬롯과 아이템 인수
            }), Is.True); // 두 묶음 교환
            Assert.That(inventory.Slots[1].ItemId, Is.EqualTo("potion")); // 첫 묶음 위치 확인
            Assert.That(inventory.Slots[8].ItemId, Is.EqualTo("herb")); // 두 번째 묶음 위치 확인
            Assert.That(inventory.Slots[8].Quantity, Is.EqualTo(2)); // 두 번째 묶음 수량 확인
            Assert.That(inventory.Slots[8].StackLimit, Is.EqualTo(3)); // 두 번째 묶음 한도 확인
        } // 검증 끝
        [Test] // 잘못된 슬롯과 같은 슬롯 검증
        public void Swap_InvalidIndicesAndSameSlotPreserveContents() // 범위 밖 입력과 취소 처리
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            inventory.Add("potion", "물약", 2, 5); // 시험 아이템 보관
            var method = typeof(InventoryState).GetMethod("TrySwap"); // 슬롯 교환 규칙 조회
            Assert.That(method, Is.Not.Null); // 슬롯 교환 기능 제공 확인
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                -1, 0 // 슬롯과 아이템 인수
            }), Is.False); // 범위 밖 출발 거절
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                0, 10 // 슬롯과 아이템 인수
            }), Is.False); // 범위 밖 도착 거절
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                0, 0 // 슬롯과 아이템 인수
            }), Is.True); // 같은 칸 선택 처리
            Assert.That(inventory.Slots[0].ItemId, Is.EqualTo("potion")); // 기존 아이템 보존
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(2)); // 기존 수량 보존
        } // 검증 끝
        [Test] // 가득 찬 슬롯의 안전한 교체 검증
        public void Replace_ValidatesBeforeReplacingWholeStack() // 확인된 슬롯의 전체 묶음 교체
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            inventory.Add("herb", "약초", 3, 5); // 기존 아이템 묶음
            var method = typeof(InventoryState).GetMethod("TryReplace"); // 슬롯 교체 규칙 조회
            Assert.That(method, Is.Not.Null); // 안전한 교체 기능 제공 확인
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                0, "", "새 아이템", 1, 1 // 슬롯과 아이템 인수
            }), Is.False); // 빈 아이템 식별자 거절
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                0, "new", "새 아이템", 2, 1 // 슬롯과 아이템 인수
            }), Is.False); // 중첩 한도 초과 거절
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                1, "new", "새 아이템", 1, 1 // 슬롯과 아이템 인수
            }), Is.False); // 빈 슬롯의 교체 거절
            Assert.That(inventory.Slots[0].ItemId, Is.EqualTo("herb")); // 잘못된 요청 후 기존 아이템 보존
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(3)); // 기존 수량 보존
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                0, "new", "새 아이템", 1, 1 // 슬롯과 아이템 인수
            }), Is.True); // 확인된 슬롯 교체
            Assert.That(inventory.Slots[0].ItemId, Is.EqualTo("new")); // 새 아이템 확인
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1)); // 새 수량 확인
            Assert.That(inventory.Slots[0].StackLimit, Is.EqualTo(1)); // 새 중첩 한도 확인
        } // 검증 끝
        [Test] // 기본 슬롯 검증
        public void NewInventory_HasTenEmptySlots() // 새 게임의 빈 슬롯 확인
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            Assert.That(inventory.Slots.Count, Is.EqualTo(10)); // 기존 기본 10칸 확인
            foreach (var slot in inventory.Slots) // 모든 슬롯 순회
            { // 슬롯 검사 시작
                Assert.That(slot.IsEmpty, Is.True); // 빈 슬롯 확인
            } // 슬롯 검사 끝
        } // 검증 끝
        [Test] // 기존 중첩 우선 검증
        public void Add_FillsExistingStacksBeforeEmptySlots() // 기존 묶음과 새 슬롯 분배
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            var first = inventory.Add("potion", "물약", 3, 5); // 첫 묶음 획득
            var second = inventory.Add("potion", "물약", 4, 5); // 같은 아이템 추가 획득
            Assert.That(first.Added, Is.EqualTo(3)); // 첫 획득 수량
            Assert.That(second.Added, Is.EqualTo(4)); // 추가 획득 수량
            Assert.That(second.Remaining, Is.Zero); // 남은 수량 없음
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(5)); // 기존 묶음 먼저 채움
            Assert.That(inventory.Slots[1].Quantity, Is.EqualTo(2)); // 나머지 수량 새 슬롯 배치
        } // 검증 끝
        [Test] // 가득 찬 인벤토리 검증
        public void Add_ReportsRemainingWhenCapacityIsFull() // 공간 부족 수량 보존
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            var result = inventory.Add("stone", "돌", 22, 2); // 열 묶음을 넘는 아이템 획득
            Assert.That(result.Added, Is.EqualTo(20)); // 실제 저장된 수량 확인
            Assert.That(result.Remaining, Is.EqualTo(2)); // 넣지 못한 수량 확인
            Assert.That(inventory.Slots[9].Quantity, Is.EqualTo(2)); // 마지막 슬롯 중첩 확인
            var blocked = inventory.Add("other", "다른 아이템", 1, 1); // 빈 슬롯이 없는 추가 획득
            Assert.That(blocked.Added, Is.Zero); // 기존 슬롯 덮어쓰기 방지
            Assert.That(blocked.Remaining, Is.EqualTo(1)); // 넣지 못한 아이템 보존
        } // 검증 끝
        [Test] // 수량 제거 검증
        public void Remove_ClampsAmountAndClearsEmptySlot() // 일부 제거와 빈 슬롯 초기화
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            inventory.Add("key", "열쇠", 3, 5); // 세 개의 열쇠 획득
            Assert.That(inventory.Remove(0, 2, out var first), Is.True); // 일부 수량 제거
            Assert.That(first, Is.EqualTo(2)); // 실제 제거 수량 확인
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1)); // 남은 수량 확인
            Assert.That(inventory.Remove(0, 9, out var last), Is.True); // 남은 수량 이상 제거
            Assert.That(last, Is.EqualTo(1)); // 실제 보유 수량만 제거
            Assert.That(inventory.Slots[0].IsEmpty, Is.True); // 빈 슬롯 초기화
            Assert.That(inventory.Remove(0, 1, out _), Is.False); // 빈 슬롯 제거 차단
        } // 검증 끝
        [Test] // 잘못된 입력 검증
        public void Add_InvalidInputDoesNotChangeSlots() // 잘못된 획득 입력 차단
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            var invalid = inventory.Add("", "", 2, 5); // 없는 아이템 식별자
            Assert.That(invalid.Added, Is.Zero); // 잘못된 획득 차단
            Assert.That(invalid.Remaining, Is.EqualTo(2)); // 미획득 수량 보존
            Assert.That(inventory.Add("item", "아이템", -2, 5).Added, Is.Zero); // 음수 수량 차단
            Assert.That(inventory.Remove(-1, 1, out _), Is.False); // 잘못된 슬롯 차단
            Assert.That(inventory.Slots[0].IsEmpty, Is.True); // 원래 슬롯 보존
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
