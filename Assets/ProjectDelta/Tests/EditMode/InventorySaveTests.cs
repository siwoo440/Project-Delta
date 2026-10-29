using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 인벤토리와 저장 데이터
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class InventorySaveTests // 인벤토리 저장 복원 검증
    { // 클래스 시작
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
