using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 인벤토리와 저장 데이터
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class InventorySaveTests // 인벤토리 저장 복원 검증
    { // 클래스 시작
        [Test] // 확장과 안전한 축소 검증
        public void Resize_PreservesLateSlotsAndRejectsOverflow() // 축소 시 아이템 보존 확인
        { // 검증 시작
            var inventory = new InventoryState(); // 기본 인벤토리 준비
            Assert.That(inventory.TryResize(12), Is.True); // 가방 확장 확인
            inventory.Add("item", "시험 아이템", 1, 1); // 시험 아이템 준비
            inventory.TrySwap(0, 11); // 확장된 끝 슬롯으로 이동
            Assert.That(inventory.TryResize(10), Is.True); // 빈 공간이 있는 축소 확인
            Assert.That(inventory.Slots[0].ItemId, Is.EqualTo("item")); // 후반 아이템의 앞쪽 이동 확인
            for (var index = 1; index < 10; index++) // 남은 기본 슬롯 순회
            { // 채우기 시작
                inventory.Add("item-" + index, "시험 아이템", 1, 1); // 서로 다른 아이템 추가
            } // 채우기 끝
            Assert.That(inventory.TryResize(11), Is.True); // 추가 공간 확보
            inventory.Add("last", "마지막 아이템", 1, 1); // 축소 대상 슬롯 채우기
            Assert.That(inventory.TryResize(10), Is.False); // 보관 공간 부족 거절
            Assert.That(inventory.Slots.Count, Is.EqualTo(11)); // 실패 후 용량 보존
            Assert.That(inventory.Slots[10].ItemId, Is.EqualTo("last")); // 실패 후 아이템 보존
        } // 검증 끝
        [Test] // 확장 저장과 범위 검증
        public void Restore_PreservesExpandedSlotsAndRejectsInvalidSizes() // 확장 슬롯 저장 복원 확인
        { // 검증 시작
            var inventory = new InventoryState(); // 기본 인벤토리 준비
            Assert.That(inventory.TryResize(23), Is.True); // 지원 최대 시험 용량
            inventory.Add("item", "시험 아이템", 1, 1); // 시험 아이템 추가
            inventory.TrySwap(0, 22); // 마지막 슬롯으로 이동
            var restored = InventoryState.FromSavedSlots(inventory.SaveSlots()); // 확장 슬롯 저장 복원
            Assert.That(restored.Slots.Count, Is.EqualTo(23)); // 확장 용량 보존
            Assert.That(restored.Slots[22].ItemId, Is.EqualTo("item")); // 마지막 슬롯 위치 보존
            Assert.That(inventory.TryResize(9), Is.False); // 기본 용량 미만 거절
            Assert.That(inventory.TryResize(24), Is.False); // 지원 용량 초과 거절
            Assert.That(InventoryState.FromSavedSlots(new SaveItemSlot[24]), Is.Null); // 지원 범위 밖 저장 거절
        } // 검증 끝
        [Test] // 슬롯 순서와 수량 보존 검증
        public void Snapshot_RestoresExactSlotsWithoutAliasing() // 슬롯 순서 보존
        { // 검증 시작
            var inventory = new InventoryState(); // 시험 인벤토리 생성
            inventory.Add("potion", "회복약", 3, 5); // 시험 회복약 추가
            inventory.TrySwap(0, 3); // 빈 칸을 포함한 위치 변경
            var snapshot = inventory.SaveSlots(); // 독립 저장 슬롯 생성
            var restored = InventoryState.FromSavedSlots(snapshot); // 저장 슬롯 복원
            Assert.That(restored.Slots[0].IsEmpty, Is.True); // 앞쪽 빈 칸 보존
            Assert.That(restored.Slots[3].ItemId, Is.EqualTo("potion")); // 아이템 위치 보존
            Assert.That(restored.Slots[3].Quantity, Is.EqualTo(3)); // 아이템 수량 보존
            snapshot[3].quantity = 1; // 저장본 변경
            Assert.That(restored.Slots[3].Quantity, Is.EqualTo(3)); // 복원 상태 독립성 확인
        } // 검증 끝
        [Test] // 잘못된 저장 슬롯 거절 검증
        public void Restore_RejectsInvalidSlotCountAndQuantity() // 잘못된 슬롯 거절
        { // 검증 시작
            Assert.That(InventoryState.FromSavedSlots(new SaveItemSlot[0]), Is.Null); // 잘못된 슬롯 수 거절
            var inventory = new InventoryState(); // 시험 인벤토리 생성
            var snapshot = inventory.SaveSlots(); // 빈 인벤토리 저장
            snapshot[4] = new SaveItemSlot { itemId = "potion", quantity = 6, stackLimit = 5 }; // 중첩 수량 초과
            Assert.That(InventoryState.FromSavedSlots(snapshot), Is.Null); // 잘못된 수량 거절
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
