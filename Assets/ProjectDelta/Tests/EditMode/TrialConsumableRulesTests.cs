using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 아이템과 인벤토리 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TrialConsumableRulesTests // 시험 소비품 검증
    { // 검증 클래스 시작
        [TestCase("trial-small-potion", TrialConsumableResource.Health, 25)] // 소형 체력 회복약
        [TestCase("trial-large-potion", TrialConsumableResource.Health, 60)] // 대형 체력 회복약
        [TestCase("trial-mana-potion", TrialConsumableResource.Mana, 30)] // 마나 회복약
        public void Definition_ContainsExpectedResourceAndAmount(string id, TrialConsumableResource resource, int amount) // 종류별 회복 수치 검증
        { // 검증 시작
            var item = TrialConsumableRules.Find(id); // 시험 아이템 정의 조회
            Assert.That(item, Is.Not.Null); // 실제 정의 확인
            Assert.That(item.Resource, Is.EqualTo(resource)); // 회복 자원 확인
            Assert.That(item.Recovery, Is.EqualTo(amount)); // 회복량 확인
            Assert.That(item.StackLimit, Is.EqualTo(5)); // 미확정 시험 중첩 한도 확인
        } // 검증 끝
        [TestCase("trial-small-potion", 10, 100, 35)] // 소형 회복
        [TestCase("trial-large-potion", 10, 100, 70)] // 대형 회복
        [TestCase("trial-mana-potion", 80, 100, 100)] // 최대 자원 제한
        public void Use_ConsumesSelectedSlotAndCapsRecovery(string id, int current, int maximum, int expected) // 사용과 최대치 제한 확인
        { // 검증 시작
            var inventory = new InventoryState(); // 빈 인벤토리 준비
            inventory.Add(id, id, 2, 5); // 선택 아이템 두 개 준비
            Assert.That(TrialConsumableRules.TryUse(inventory, 0, current, maximum, out var recovered), Is.True); // 회복 성공 검사
            Assert.That(recovered, Is.EqualTo(expected)); // 회복 후 자원 확인
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1)); // 아이템 한 개 소비 확인
        } // 검증 끝
        [Test] // 최대 자원의 사용 차단 검증
        public void Use_AtMaximumDoesNotConsume() // 최대치 소비 방지 확인
        { // 검증 시작
            var inventory = new InventoryState(); // 빈 인벤토리 준비
            inventory.Add("trial-mana-potion", "마나 회복약", 1, 5); // 시험 아이템 준비
            Assert.That(TrialConsumableRules.TryUse(inventory, 0, 100, 100, out var recovered), Is.False); // 회복 불가 검사
            Assert.That(recovered, Is.EqualTo(100)); // 기존 자원 보존 확인
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1)); // 소지품 보존 확인
        } // 검증 끝
        [Test] // 잘못된 사용의 보존 검증
        public void Use_InvalidInputsDoNotConsume() // 대상과 자원 범위 검사
        { // 검증 시작
            var inventory = new InventoryState(); // 빈 인벤토리 준비
            inventory.Add("unknown-item", "알 수 없음", 1, 1); // 미등록 아이템 준비
            inventory.Add("trial-small-potion", "소형 회복약", 1, 5); // 유효 아이템 준비
            Assert.That(TrialConsumableRules.Find("unknown-item"), Is.Null); // 미등록 정의 거절
            Assert.That(TrialConsumableRules.TryUse(inventory, 0, 1, 100, out _), Is.False); // 미등록 아이템 사용 거절
            Assert.That(TrialConsumableRules.TryUse(inventory, 1, -1, 100, out _), Is.False); // 음수 자원 거절
            Assert.That(TrialConsumableRules.TryUse(inventory, 1, 101, 100, out _), Is.False); // 최대 자원 초과 거절
            Assert.That(TrialConsumableRules.TryUse(inventory, -1, 1, 100, out _), Is.False); // 음수 슬롯 거절
            Assert.That(TrialConsumableRules.TryUse(null, 0, 1, 100, out _), Is.False); // 누락 인벤토리 거절
            Assert.That(inventory.Slots[1].Quantity, Is.EqualTo(1)); // 유효 아이템 보존 확인
        } // 검증 끝
        [Test] // 마나 0 회복 검증
        public void Use_RecoversZeroMana() // 마나 소진 후 사용 확인
        { // 검증 시작
            var inventory = new InventoryState(); // 빈 인벤토리 준비
            inventory.Add("trial-mana-potion", "마나 회복약", 1, 5); // 마나 회복약 준비
            Assert.That(TrialConsumableRules.TryUse(inventory, 0, 0, 100, out var recovered), Is.True); // 마나 소진 회복 검사
            Assert.That(recovered, Is.EqualTo(30)); // 회복된 마나 확인
        } // 검증 끝
    } // 검증 클래스 끝
} // 이름 공간 끝
