using NUnit.Framework; // 장비 규칙 검증 도구
using ProjectDelta.Core; // 독립 장비와 능력치 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class EquipmentRulesTests // 시험 장비의 장착과 합산 검증
    { // 클래스 시작
        [Test] // 정의된 시험 장비 검증
        public void Catalog_ExposesSpecifiedItemsAndRejectsUnknown() // 장비 정의와 슬롯 확인
        { // 검증 시작
            Assert.That(EquipmentCatalog.Find(EquipmentCatalog.BasicSwordId).Slot, Is.EqualTo(EquipmentSlot.Weapon)); // 검의 무기 부위 확인
            Assert.That(EquipmentCatalog.Find(EquipmentCatalog.BasicAxeId).Slot, Is.EqualTo(EquipmentSlot.Weapon)); // 도끼의 무기 부위 확인
            Assert.That(EquipmentCatalog.Find(EquipmentCatalog.LightArmorId).Slot, Is.EqualTo(EquipmentSlot.Armor)); // 경갑의 방어구 부위 확인
            Assert.That(EquipmentCatalog.Find(EquipmentCatalog.CombatAccessoryId).Slot, Is.EqualTo(EquipmentSlot.Accessory)); // 전투 장신구의 부위 확인
            Assert.That(EquipmentCatalog.Find(EquipmentCatalog.OldBagId).Slot, Is.EqualTo(EquipmentSlot.Bag)); // 낡은 주머니의 가방 부위 확인
            Assert.That(EquipmentCatalog.Find("unknown"), Is.Null); // 정의되지 않은 장비 차단
            Assert.That(EquipmentCatalog.Find(null), Is.Null); // 빈 장비 식별자 차단
        } // 검증 끝
        [Test] // 맨손과 전체 장비 합산 검증
        public void Stats_UseBareHandDefaultsAndCombineEquippedBonuses() // 맨손과 검 및 경갑과 가방 확인
        { // 검증 시작
            var empty = EquipmentState.Empty; // 장비 없는 시작 상태
            Assert.That(empty.TryCalculate(50, 40, 10, 40, 150, 10, out var bare), Is.True); // 맨손 기준 능력치 계산
            Assert.That(bare.Attack, Is.EqualTo(50)); // 맨손 공격력 유지
            Assert.That(bare.CriticalChance, Is.EqualTo(40)); // 기존 맨손 확률 유지
            Assert.That(bare.CriticalMultiplierPercent, Is.EqualTo(150)); // 기존 맨손 배율 유지
            Assert.That(empty.TryEquip(EquipmentSlot.Weapon, EquipmentCatalog.BasicSwordId, out var sword), Is.True); // 기본 검 장착
            Assert.That(sword.TryEquip(EquipmentSlot.Armor, EquipmentCatalog.LightArmorId, out var armor), Is.True); // 경갑 장착
            Assert.That(armor.TryEquip(EquipmentSlot.Bag, EquipmentCatalog.OldBagId, out var equipped), Is.True); // 낡은 주머니 장착
            Assert.That(equipped.TryCalculate(50, 40, 10, 12, 120, 10, out var stats), Is.True); // 실제 장비의 능력치 계산
            Assert.That(stats.Attack, Is.EqualTo(55)); // 검 공격력 보정 확인
            Assert.That(stats.Defense, Is.EqualTo(48)); // 경갑 방어력 보정 확인
            Assert.That(stats.Speed, Is.EqualTo(12)); // 경갑 속도 보정 확인
            Assert.That(stats.CriticalChance, Is.EqualTo(40)); // 검 치명타 확률 확인
            Assert.That(stats.CriticalMultiplierPercent, Is.EqualTo(150)); // 검 치명타 배율 확인
            Assert.That(stats.InventoryCapacity, Is.EqualTo(12)); // 가방의 추가 두 칸 확인
            Assert.That(equipped.TryEquip(EquipmentSlot.Accessory, EquipmentCatalog.CombatAccessoryId, out var complete), Is.True); // 전투 장신구 장착
            Assert.That(complete.TryCalculate(50, 40, 10, 40, 150, 10, out var all), Is.True); // 네 부위의 장비 합산
            Assert.That(all.Attack, Is.EqualTo(58)); // 검과 장신구의 공격력 합산 확인
            Assert.That(all.Defense, Is.EqualTo(50)); // 경갑과 장신구의 방어력 합산 확인
            Assert.That(all.Speed, Is.EqualTo(12)); // 경갑 속도 보정 유지
            Assert.That(all.InventoryCapacity, Is.EqualTo(12)); // 가방 수용량 유지
            Assert.That(empty.Get(EquipmentSlot.Weapon), Is.Null); // 기존 불변 상태 유지
        } // 검증 끝
        [Test] // 무기 교체와 해제 검증
        public void Equip_ChangesWeaponAndUnequipRestoresBareHand() // 검과 도끼의 배율 및 맨손 복구
        { // 검증 시작
            Assert.That(EquipmentState.Empty.TryEquip(EquipmentSlot.Weapon, EquipmentCatalog.BasicSwordId, out var sword), Is.True); // 기본 검 장착
            Assert.That(sword.TryEquip(EquipmentSlot.Weapon, EquipmentCatalog.BasicAxeId, out var axe), Is.True); // 기본 도끼로 교체
            Assert.That(axe.TryCalculate(50, 40, 10, 40, 150, 10, out var stats), Is.True); // 도끼 장착 능력치 계산
            Assert.That(stats.Attack, Is.EqualTo(60)); // 도끼 공격력 보정 확인
            Assert.That(stats.CriticalChance, Is.EqualTo(30)); // 도끼 치명타 확률 확인
            Assert.That(stats.CriticalMultiplierPercent, Is.EqualTo(200)); // 도끼 치명타 배율 확인
            Assert.That(sword.Get(EquipmentSlot.Weapon).Id, Is.EqualTo(EquipmentCatalog.BasicSwordId)); // 교체 전 상태 보존
            Assert.That(axe.TryUnequip(EquipmentSlot.Weapon, out var empty), Is.True); // 도끼 해제
            Assert.That(empty.TryCalculate(50, 40, 10, 40, 150, 10, out var bare), Is.True); // 맨손 능력치 재계산
            Assert.That(bare.Attack, Is.EqualTo(50)); // 맨손 공격력 복구
            Assert.That(bare.CriticalMultiplierPercent, Is.EqualTo(150)); // 맨손 배율 복구
        } // 검증 끝
        [Test] // 잘못된 장착과 저장 복원 검증
        public void State_RejectsWrongSlotsUnknownItemsAndInvalidSavedData() // 슬롯 불일치와 원본 보존
        { // 검증 시작
            var empty = EquipmentState.Empty; // 장비 없는 시작 상태
            Assert.That(empty.TryEquip(EquipmentSlot.Armor, EquipmentCatalog.BasicSwordId, out var invalid), Is.False); // 다른 부위의 검 장착 차단
            Assert.That(invalid, Is.Null); // 잘못된 장착 결과 없음
            Assert.That(empty.TryEquip(EquipmentSlot.Accessory, "unknown", out _), Is.False); // 미정 장신구 차단
            Assert.That(empty.TryUnequip((EquipmentSlot)99, out _), Is.False); // 잘못된 부위 해제 차단
            Assert.That(EquipmentState.TryRestore(EquipmentCatalog.BasicSwordId, EquipmentCatalog.LightArmorId, null, EquipmentCatalog.OldBagId, out var restored), Is.True); // 정확한 부위의 저장 복원
            Assert.That(restored.Get(EquipmentSlot.Bag).Id, Is.EqualTo(EquipmentCatalog.OldBagId)); // 가방 복원 확인
            Assert.That(EquipmentState.TryRestore(EquipmentCatalog.LightArmorId, null, null, null, out _), Is.False); // 잘못된 부위의 저장 차단
            Assert.That(EquipmentState.TryRestore(null, null, "unknown", null, out _), Is.False); // 미등록 저장 장비 차단
        } // 검증 끝
        [Test] // 계산 입력과 정수 범위 검증
        public void Stats_RejectInvalidAndOverflowWithoutMutation() // 잘못된 기본 능력치 차단
        { // 검증 시작
            Assert.That(EquipmentState.Empty.TryEquip(EquipmentSlot.Weapon, EquipmentCatalog.BasicSwordId, out var state), Is.True); // 기본 검 장착
            Assert.That(state.TryCalculate(-1, 0, 0, 0, 150, 10, out _), Is.False); // 음수 공격력 차단
            Assert.That(state.TryCalculate(0, 0, 0, 101, 150, 10, out _), Is.False); // 범위 밖 치명 확률 차단
            Assert.That(state.TryCalculate(0, 0, 0, 0, 0, 10, out _), Is.False); // 잘못된 치명 배율 차단
            Assert.That(state.TryCalculate(int.MaxValue, 0, 0, 0, 150, 10, out _), Is.False); // 공격력 정수 초과 차단
            Assert.That(state.Get(EquipmentSlot.Weapon).Id, Is.EqualTo(EquipmentCatalog.BasicSwordId)); // 계산 실패 후 장비 보존
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
