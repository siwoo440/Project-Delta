using NUnit.Framework; // 규칙 검증 도구
using ProjectDelta.Core; // 영구 성장 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class PermanentGrowthRulesTests // 영구 성장 조회 검증
    { // 검증 클래스 시작
        [TestCase(PermanentGrowthKind.MaxHealth, 5)] // 체력 단계 효과
        [TestCase(PermanentGrowthKind.MaxMana, 3)] // 마나 단계 효과
        [TestCase(PermanentGrowthKind.MaxStamina, 5)] // 정력 단계 효과
        [TestCase(PermanentGrowthKind.Attack, 1)] // 공격 단계 효과
        [TestCase(PermanentGrowthKind.Defense, 1)] // 방어 단계 효과
        [TestCase(PermanentGrowthKind.Speed, 1)] // 속도 단계 효과
        [TestCase(PermanentGrowthKind.Charm, 1)] // 매력 단계 효과
        [TestCase(PermanentGrowthKind.Evasion, 1)] // 회피 단계 효과
        [TestCase(PermanentGrowthKind.Resistance, 1)] // 저항 단계 효과
        public void Stats_UseTenLevelLimits(PermanentGrowthKind kind, int unit) // 능력치 최대 효과
        { // 검증 시작
            Assert.That(PermanentGrowthRules.TryEffect(kind, 10, out var effect), Is.True); // 최대 단계 조회
            Assert.That(effect, Is.EqualTo(unit * 10)); // 단계별 능력치 합계
            Assert.That(PermanentGrowthRules.TryEffect(kind, 11, out _), Is.False); // 최대 초과 차단
            Assert.That(PermanentGrowthRules.TryNextCost(kind, 10, out _), Is.False); // 추가 구매 차단
        } // 검증 끝
        [Test] // 능력치 비용 표 검증
        public void StatCosts_MatchEachStep() // 자원과 전투 비용 구분
        { // 검증 시작
            var resource = new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 12 }; // 자원 단계 비용
            var combat = new[] { 3, 4, 5, 6, 7, 8, 9, 10, 12, 15 }; // 전투 단계 비용
            for (var i = 0; i < 10; i++) // 모든 단계 순회
            { // 순회 시작
                Assert.That(PermanentGrowthRules.TryNextCost(PermanentGrowthKind.MaxHealth, i, out var first), Is.True); // 다음 자원 비용
                Assert.That(first, Is.EqualTo(resource[i])); // 자원 비용 확인
                Assert.That(PermanentGrowthRules.TryNextCost(PermanentGrowthKind.Attack, i, out var second), Is.True); // 다음 전투 비용
                Assert.That(second, Is.EqualTo(combat[i])); // 전투 비용 확인
            } // 순회 끝
        } // 검증 끝
        [TestCase(PermanentGrowthKind.Inventory, 5, 5, 25)] // 인벤토리 한도
        [TestCase(PermanentGrowthKind.Relic, 2, 2, 30)] // 유물 추가 한도
        [TestCase(PermanentGrowthKind.ShopDiscount, 5, 10, 25)] // 구매 할인율
        [TestCase(PermanentGrowthKind.ShopStock, 3, 3, 24)] // 상점 재고
        [TestCase(PermanentGrowthKind.ShopRare, 5, 10, 25)] // 희귀 확률
        [TestCase(PermanentGrowthKind.ShopSell, 4, 70, 24)] // 판매 비율
        [TestCase(PermanentGrowthKind.Map, 3, 3, 30)] // 지도 공개 단계
        [TestCase(PermanentGrowthKind.Trap, 5, 25, 25)] // 함정 보정
        [TestCase(PermanentGrowthKind.Secret, 5, 25, 30)] // 비밀 방 확률 보정
        [TestCase(PermanentGrowthKind.StartingConsumable, 3, 3, 20)] // 소비품 단계
        [TestCase(PermanentGrowthKind.StartingGold, 5, 250, 25)] // 시작 골드
        public void OtherTracks_ExposeConfirmedMaximum(PermanentGrowthKind kind, int max, int expected, int finalCost) // 분야별 최대치
        { // 검증 시작
            Assert.That(PermanentGrowthRules.TryEffect(kind, max, out var value), Is.True); // 최대 단계 조회
            Assert.That(value, Is.EqualTo(expected)); // 최종 효과 확인
            Assert.That(PermanentGrowthRules.TryNextCost(kind, max - 1, out var cost), Is.True); // 최종 단계 비용
            Assert.That(cost, Is.EqualTo(finalCost)); // 최종 비용 확인
            Assert.That(PermanentGrowthRules.TryNextCost(kind, max, out _), Is.False); // 상한 이후 비용 차단
            Assert.That(PermanentGrowthRules.TryEffect(kind, -1, out _), Is.False); // 음수 단계 차단
            Assert.That(PermanentGrowthRules.TryEffect(kind, max + 1, out _), Is.False); // 초과 단계 차단
        } // 검증 끝
        [TestCase(PermanentGrowthKind.Inventory, "5,10,15,20,25")] // 인벤토리 단계별 비용
        [TestCase(PermanentGrowthKind.Relic, "15,30")] // 유물 단계별 비용
        [TestCase(PermanentGrowthKind.ShopDiscount, "5,10,15,20,25")] // 할인 단계별 비용
        [TestCase(PermanentGrowthKind.ShopStock, "8,16,24")] // 재고 단계별 비용
        [TestCase(PermanentGrowthKind.ShopRare, "8,12,16,20,25")] // 희귀 단계별 비용
        [TestCase(PermanentGrowthKind.ShopSell, "6,12,18,24")] // 판매 단계별 비용
        [TestCase(PermanentGrowthKind.Map, "10,20,30")] // 지도 단계별 비용
        [TestCase(PermanentGrowthKind.Trap, "5,10,15,20,25")] // 함정 단계별 비용
        [TestCase(PermanentGrowthKind.Secret, "6,12,18,24,30")] // 비밀 방 단계별 비용
        [TestCase(PermanentGrowthKind.StartingConsumable, "5,12,20")] // 소비품 단계별 비용
        [TestCase(PermanentGrowthKind.StartingGold, "5,10,15,20,25")] // 시작 골드 단계별 비용
        public void OtherTracks_CostsMatchEveryStep(PermanentGrowthKind kind, string expected) // 전체 단계별 비용 검증
        { // 검증 시작
            var items = expected.Split(','); // 확인할 비용 목록
            for (var index = 0; index < items.Length; index++) // 단계별 비용 순회
            { // 순회 시작
                Assert.That(PermanentGrowthRules.TryNextCost(kind, index, out var cost), Is.True); // 다음 단계 비용 조회
                Assert.That(cost, Is.EqualTo(int.Parse(items[index]))); // 기획 비용 확인
            } // 순회 끝
        } // 검증 끝
        [Test] // 미등록 종류와 기본값 검증
        public void InvalidAndZeroLevels_AreHandled() // 입력 경계 확인
        { // 검증 시작
            Assert.That(PermanentGrowthRules.TryEffect((PermanentGrowthKind)999, 0, out _), Is.False); // 미등록 종류 차단
            Assert.That(PermanentGrowthRules.TryNextCost(PermanentGrowthKind.Attack, -1, out _), Is.False); // 음수 비용 차단
            Assert.That(PermanentGrowthRules.TryEffect(PermanentGrowthKind.ShopSell, 0, out var sell), Is.True); // 기본 판매 비율
            Assert.That(sell, Is.EqualTo(50)); // 기본 오십 퍼센트
        } // 검증 끝
    } // 검증 클래스 끝
} // 이름 공간 끝
