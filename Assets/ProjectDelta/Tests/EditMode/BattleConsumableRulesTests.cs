using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 전투와 아이템 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class BattleConsumableRulesTests // 전투 소비품 검증
    { // 검증 클래스 시작
        [TestCase("trial-small-potion", 25)] // 소형 체력 회복
        [TestCase("trial-large-potion", 60)] // 대형 체력 회복
        public void HealthPotion_HealsFighterAndConsumesSelectedItem(string id, int amount) // 전투 체력과 아이템 소비 확인
        { // 검증 시작
            var inventory = new InventoryState(); // 빈 인벤토리 준비
            inventory.Add(id, id, 1, 5); // 선택 회복약 준비
            var fighter = new CombatFighter(new BattleActor("플레이어", 1, true, 0), 100, 1, 1, 1, 1, 10); // 피해를 입은 플레이어 준비
            Assert.That(BattleConsumableRules.TryUse(inventory, 0, fighter, 20, 100, out var mana, out var restored), Is.True); // 실제 사용 성공 검사
            Assert.That(fighter.Health, Is.EqualTo(10 + amount)); // 실제 전투 체력 회복 확인
            Assert.That(restored, Is.EqualTo(amount)); // 실제 회복량 확인
            Assert.That(mana, Is.EqualTo(20)); // 마나 보존 확인
            Assert.That(inventory.Slots[0].IsEmpty, Is.True); // 선택 회복약 소비 확인
        } // 검증 끝
        [Test] // 마나 회복과 상한 검증
        public void ManaPotion_RestoresOnlyAvailableMana() // 마나 상한과 실제 회복량 확인
        { // 검증 시작
            var inventory = new InventoryState(); // 빈 인벤토리 준비
            inventory.Add("trial-mana-potion", "마나 회복약", 1, 5); // 마나 회복약 준비
            var fighter = new CombatFighter(new BattleActor("플레이어", 1, true, 0), 100, 1, 1, 1, 1, 30); // 생존한 플레이어 준비
            Assert.That(BattleConsumableRules.TryUse(inventory, 0, fighter, 90, 100, out var mana, out var restored), Is.True); // 마나 회복 성공 검사
            Assert.That(mana, Is.EqualTo(100)); // 최대 마나 제한 확인
            Assert.That(restored, Is.EqualTo(10)); // 실제 마나 회복량 확인
            Assert.That(fighter.Health, Is.EqualTo(30)); // 체력 변경 방지 확인
            Assert.That(inventory.Slots[0].IsEmpty, Is.True); // 선택 아이템 소비 확인
        } // 검증 끝
        [Test] // 실패 시 자원 보존 검증
        public void InvalidUse_DoesNotConsumeOrChangeResources() // 최대치와 잘못된 슬롯 검사
        { // 검증 시작
            var inventory = new InventoryState(); // 빈 인벤토리 준비
            inventory.Add("trial-mana-potion", "마나 회복약", 1, 5); // 마나 회복약 준비
            inventory.Add("trial-small-potion", "소형 회복약", 1, 5); // 체력 회복약 준비
            var fighter = new CombatFighter(new BattleActor("플레이어", 1, true, 0), 100, 1, 1, 1, 1); // 최대 체력 플레이어 준비
            Assert.That(BattleConsumableRules.TryUse(inventory, 0, fighter, 100, 100, out var mana, out var restored), Is.False); // 최대 마나 사용 거절
            Assert.That(mana, Is.EqualTo(100)); // 마나 보존 확인
            Assert.That(restored, Is.Zero); // 회복 없음 확인
            Assert.That(BattleConsumableRules.TryUse(inventory, 1, fighter, 10, 100, out _, out _), Is.False); // 최대 체력 사용 거절
            Assert.That(BattleConsumableRules.TryUse(inventory, -1, fighter, 10, 100, out _, out _), Is.False); // 범위 밖 슬롯 거절
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1)); // 마나약 보존 확인
            Assert.That(inventory.Slots[1].Quantity, Is.EqualTo(1)); // 체력약 보존 확인
        } // 검증 끝
        [Test] // 사망 참가자의 사용 차단 검증
        public void DeadFighter_CannotUseEvenManaPotion() // 사망 시 소비 차단 확인
        { // 검증 시작
            var inventory = new InventoryState(); // 빈 인벤토리 준비
            inventory.Add("trial-mana-potion", "마나 회복약", 1, 5); // 마나 회복약 준비
            var fighter = new CombatFighter(new BattleActor("플레이어", 1, true, 0), 100, 1, 1, 1, 1, 0); // 사망 플레이어 준비
            Assert.That(BattleConsumableRules.TryUse(inventory, 0, fighter, 0, 100, out var mana, out var restored), Is.False); // 사망 시 사용 거절
            Assert.That(mana, Is.Zero); // 마나 보존 확인
            Assert.That(restored, Is.Zero); // 회복 없음 확인
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1)); // 아이템 보존 확인
        } // 검증 끝
    } // 검증 클래스 끝
} // 이름 공간 끝
