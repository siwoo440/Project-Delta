using NUnit.Framework; // 시험 장비 검증 도구
using ProjectDelta.Core; // 장비 특성 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TrialEquipmentTraitsTests // 실제 시험 장비 검증
    { // 클래스 시작
        [Test] // 같은 종류의 고유 시험 장비 검사
        public void Create_ProducesIndependentVisibleTraits() // 개별 식별자와 공개 효과 확인
        { // 검증 시작
            var first = TrialEquipmentTraits.Create(EquipmentCatalog.BasicSwordId, EquipmentVariantRules.Rare, true); // 저주받은 시험 검
            var second = TrialEquipmentTraits.Create(EquipmentCatalog.BasicSwordId, EquipmentVariantRules.Rare, false); // 저주 없는 시험 검
            Assert.That(TrialEquipmentTraits.IsValid(first), Is.True); // 첫 장비 유효성 확인
            Assert.That(TrialEquipmentTraits.IsValid(second), Is.True); // 두 번째 장비 유효성 확인
            Assert.That(first.instanceId, Is.Not.EqualTo(second.instanceId)); // 동일한 기본 장비의 개별 식별
            Assert.That(first.options.Length, Is.EqualTo(2)); // 희귀 등급의 추가 옵션 두 개
            Assert.That(TrialEquipmentTraits.Describe(first), Does.Contain("저주").And.Contain("속도-2")); // 획득 전 저주 효과 공개
            Assert.That(TrialEquipmentTraits.Describe(second), Does.Not.Contain("저주")); // 저주 없는 장비 표시
        } // 검증 끝
        [Test] // 개별 옵션과 저주 보정 및 독립성 검사
        public void Equip_AppliesAndPreservesVariantEffects() // 실제 장착 능력치와 저장 복사 확인
        { // 검증 시작
            var item = TrialEquipmentTraits.Create(EquipmentCatalog.BasicSwordId, EquipmentVariantRules.Rare, true); // 저주받은 시험 검
            Assert.That(EquipmentState.Empty.TryEquip(EquipmentSlot.Weapon, item.definitionId, item, out var state), Is.True); // 개별 장비 장착
            item.options[0].value = 9; // 외부 원본 변경
            Assert.That(state.TryCalculate(50, 40, 10, 0, 150, 10, out var stats), Is.True); // 실제 장착 능력치 계산
            Assert.That(stats.Attack, Is.EqualTo(57)); // 기본 공격 50과 검 5 및 추가 옵션 2
            Assert.That(stats.Defense, Is.EqualTo(42)); // 기본 방어 40과 추가 옵션 2
            Assert.That(stats.Speed, Is.EqualTo(8)); // 기본 속도 10과 저주 감소 2
            var copy = state.GetVariant(EquipmentSlot.Weapon); // 장착 특성 독립 조회
            copy.options[0].value = 10; // 외부 조회 복사 변경
            Assert.That(state.GetVariant(EquipmentSlot.Weapon).options[0].value, Is.EqualTo(2)); // 장착 원본 옵션 보존
        } // 검증 끝
        [Test] // 시험 옵션의 무작위 선택 검증
        public void CreateRandom_ChoosesDistinctSupportedOptions() // 등급별 옵션 추첨과 중복 방지 확인
        { // 검증 시작
            var random = new System.Random(42); // 재현 가능한 시험 난수
            var chosen = new System.Collections.Generic.HashSet<string>(); // 실제 추첨된 첫 옵션 목록
            for (var index = 0; index < 30; index++) // 여러 시험 후보 생성
            { // 후보 검사 시작
                var item = TrialEquipmentTraits.CreateRandom(EquipmentCatalog.BasicSwordId, EquipmentVariantRules.Rare, true, random); // 무작위 희귀 저주 검
                Assert.That(TrialEquipmentTraits.IsValid(item), Is.True); // 실제 후보 유효성 확인
                Assert.That(item.options[0].optionId, Is.Not.EqualTo(item.options[1].optionId)); // 중복 허용 정책 반영
                chosen.Add(item.options[0].optionId); // 첫 옵션의 실제 추첨 결과
            } // 후보 검사 끝
            Assert.That(chosen.Count, Is.GreaterThan(1)); // 시험 난수에 따른 옵션 변화 확인
        } // 검증 끝
        [Test] // 잘못된 시험 옵션과 인스턴스 검사
        public void Validation_RejectsUnsupportedOrMalformedData() // 손상된 장비 차단 확인
        { // 검증 시작
            var item = TrialEquipmentTraits.Create(EquipmentCatalog.BasicSwordId, EquipmentVariantRules.Common, false); // 유효한 시험 검
            item.options[0].value = 11; // 시험 범위 초과 값
            Assert.That(TrialEquipmentTraits.IsValid(item), Is.False); // 잘못된 시험 수치 차단
            item.options[0].value = 2; // 유효한 수치 복원
            item.instanceId = "bad"; // 잘못된 개별 식별자
            Assert.That(TrialEquipmentTraits.IsValid(item), Is.False); // 유효하지 않은 식별자 차단
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
