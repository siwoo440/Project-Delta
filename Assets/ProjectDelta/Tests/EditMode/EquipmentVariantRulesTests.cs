using NUnit.Framework; // 장비 개별 속성 검증 도구
using ProjectDelta.Core; // 독립 장비 옵션과 저주 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class EquipmentVariantRulesTests // 장비 등급과 저주 데이터 검증
    { // 클래스 시작
        private static EquipmentOptionPolicy[] Policies() // 검증용 옵션별 중복 정책
        { // 정책 준비 시작
            return new[] { new EquipmentOptionPolicy { optionId = "attack", allowDuplicate = false }, new EquipmentOptionPolicy { optionId = "resistance", allowDuplicate = true } }; // 고정 옵션별 시험 정책
        } // 정책 준비 끝
        private static EquipmentVariantData Item(string grade, EquipmentOptionData[] options) // 검증용 개별 장비 구성
        { // 구성 시작
            return new EquipmentVariantData { instanceId = "item-1", definitionId = "sword-1", grade = grade, options = options, curseEffects = new EquipmentOptionData[0] }; // 기본 저주 없는 장비
        } // 구성 끝
        private static EquipmentOptionData Option(string id, int value) // 저장 가능한 단일 옵션 구성
        { // 구성 시작
            return new EquipmentOptionData { optionId = id, value = value }; // 옵션 식별자와 정수 값
        } // 구성 끝
        [Test] // 등급별 옵션 수와 부호 검증
        public void Grade_RequiresOneOrTwoOptionsAndAllowsSignedValues() // 기획된 등급 수량 확인
        { // 검증 시작
            Assert.That(EquipmentVariantRules.RequiredOptionCount("COMMON"), Is.EqualTo(1)); // 일반 장비 한 옵션 확인
            Assert.That(EquipmentVariantRules.RequiredOptionCount("UNCOMMON"), Is.EqualTo(1)); // 고급 장비 한 옵션 확인
            Assert.That(EquipmentVariantRules.RequiredOptionCount("RARE"), Is.EqualTo(2)); // 희귀 장비 두 옵션 확인
            Assert.That(EquipmentVariantRules.RequiredOptionCount("LEGENDARY"), Is.EqualTo(2)); // 전설 장비 두 옵션 확인
            Assert.That(EquipmentVariantRules.RequiredOptionCount("unknown"), Is.EqualTo(-1)); // 알 수 없는 등급 차단
            Assert.That(EquipmentVariantRules.IsValid(Item("COMMON", new[] { Option("attack", -3) }), Policies()), Is.True); // 음수 옵션 허용
            Assert.That(EquipmentVariantRules.IsValid(Item("UNCOMMON", new[] { Option("attack", 0) }), Policies()), Is.True); // 명시된 영점 옵션 허용
            Assert.That(EquipmentVariantRules.IsValid(Item("RARE", new[] { Option("attack", 3), Option("resistance", -1) }), Policies()), Is.True); // 양수와 음수 동시 허용
            Assert.That(EquipmentVariantRules.IsValid(Item("LEGENDARY", new[] { Option("attack", 3) }), Policies()), Is.False); // 전설 옵션 수 부족 차단
        } // 검증 끝
        [Test] // 옵션 정의별 중복 허용 검증
        public void Options_UseEachDefinitionsDuplicatePolicy() // 중복 허용과 금지 확인
        { // 검증 시작
            Assert.That(EquipmentVariantRules.IsValid(Item("RARE", new[] { Option("resistance", 2), Option("resistance", 3) }), Policies()), Is.True); // 중복 허용 옵션 확인
            Assert.That(EquipmentVariantRules.IsValid(Item("RARE", new[] { Option("attack", 2), Option("attack", 3) }), Policies()), Is.False); // 중복 금지 옵션 차단
            Assert.That(EquipmentVariantRules.IsValid(Item("COMMON", new[] { Option("unknown", 2) }), Policies()), Is.False); // 미등록 옵션 차단
            Assert.That(EquipmentVariantRules.IsValid(Item("COMMON", new[] { Option("attack", 2) }), new[] { new EquipmentOptionPolicy { optionId = "attack", allowDuplicate = false }, new EquipmentOptionPolicy { optionId = "attack", allowDuplicate = true } }), Is.False); // 충돌하는 정책 차단
        } // 검증 끝
        [Test] // 저주 효과 공개와 기존 등급 옵션 검증
        public void Curse_ExposesSignedEffectsAndAllowsSingleGradeOption() // 저주 효과와 획득 전 공개 확인
        { // 검증 시작
            var item = Item("COMMON", new[] { Option("attack", 5) }); // 일반 등급 장비의 단일 기본 옵션
            item.cursed = true; // 명시된 저주 장비
            item.curseEffects = new[] { Option("resistance", -2), Option("resistance", 4) }; // 공개된 음수와 양수 효과
            Assert.That(EquipmentVariantRules.IsValid(item, Policies()), Is.True); // 별도 저주 효과와 등급 옵션 허용
            Assert.That(item.curseEffects[0].value, Is.EqualTo(-2)); // 획득 전 부정 효과 공개
            Assert.That(item.curseEffects[1].value, Is.EqualTo(4)); // 획득 전 긍정 효과 공개
            item.curseEffects = new EquipmentOptionData[0]; // 저주 효과 누락
            Assert.That(EquipmentVariantRules.IsValid(item, Policies()), Is.False); // 숨겨진 저주 효과 차단
            item.cursed = false; // 저주 없는 장비 표시
            item.curseEffects = new[] { Option("resistance", -2) }; // 상태와 불일치하는 저주 효과
            Assert.That(EquipmentVariantRules.IsValid(item, Policies()), Is.False); // 저주 상태와 효과 불일치 차단
        } // 검증 끝
        [Test] // 아이템별 정화 계획의 지정과 보호 검증
        public void Purification_RequiresExplicitMatchingItemPlanAndCopiesData() // 정화 종류와 결과를 지정한 원본 보존
        { // 검증 시작
            var item = Item("COMMON", new[] { Option("attack", 5) }); // 단일 옵션 장비
            item.cursed = true; // 저주 장비 표시
            item.curseEffects = new[] { Option("resistance", -2) }; // 공개된 저주 효과
            Assert.That(EquipmentVariantRules.TryGetPurification(item, Policies(), out _), Is.False); // 미정 정화 결과 차단
            item.purification = new EquipmentPurificationPlan { sourceDefinitionId = "sword-2", kind = "EQUIPMENT", resultId = "purified-sword", resultQuantity = 1 }; // 다른 아이템의 지정 계획
            Assert.That(EquipmentVariantRules.TryGetPurification(item, Policies(), out _), Is.False); // 다른 아이템 정화 결과 차단
            item.purification.sourceDefinitionId = "sword-1"; // 실제 장비의 계획 지정
            item.purification.resultEffects = new[] { Option("attack", 2) }; // 명시된 정화 결과 효과
            Assert.That(EquipmentVariantRules.TryGetPurification(item, Policies(), out var plan), Is.True); // 아이템별 정화 계획 조회
            Assert.That(plan.resultId, Is.EqualTo("purified-sword")); // 실제 지정 결과 확인
            plan.resultEffects[0].value = 99; // 조회 결과의 독립 수정
            Assert.That(item.purification.resultEffects[0].value, Is.EqualTo(2)); // 저장된 원본 계획 보존
            item.purification.kind = "unknown"; // 미등록 정화 결과 종류
            Assert.That(EquipmentVariantRules.TryGetPurification(item, Policies(), out _), Is.False); // 확인되지 않은 결과 차단
        } // 검증 끝
        [Test] // 저장과 장착 사이의 개별 장비 참조 보호 검증
        public void Copy_PreservesAllFieldsWithoutSharingNestedData() // 장비와 옵션 및 정화 계획의 심층 복사
        { // 검증 시작
            var item = Item("COMMON", new[] { Option("attack", 5) }); // 단일 옵션 장비
            item.cursed = true; // 저주 장비 설정
            item.curseEffects = new[] { Option("resistance", -2) }; // 공개된 저주 효과
            item.purification = new EquipmentPurificationPlan { sourceDefinitionId = "sword-1", kind = "EQUIPMENT", resultId = "purified-sword", resultQuantity = 1, resultEffects = new[] { Option("attack", 2) } }; // 아이템별 정화 계획
            var copy = EquipmentVariantRules.Copy(item); // 전체 장비의 독립 복사
            Assert.That(copy.instanceId, Is.EqualTo(item.instanceId)); // 개별 장비 식별자 보존
            Assert.That(copy.definitionId, Is.EqualTo(item.definitionId)); // 기본 장비 정의 보존
            Assert.That(EquipmentVariantRules.IsValid(copy, Policies()), Is.True); // 복사된 장비의 유효성 확인
            copy.options[0].value = 10; // 복사본의 일반 옵션 변경
            copy.curseEffects[0].value = -9; // 복사본의 저주 효과 변경
            copy.purification.resultEffects[0].value = 8; // 복사본의 정화 효과 변경
            Assert.That(item.options[0].value, Is.EqualTo(5)); // 원본 일반 옵션 보존
            Assert.That(item.curseEffects[0].value, Is.EqualTo(-2)); // 원본 저주 효과 보존
            Assert.That(item.purification.resultEffects[0].value, Is.EqualTo(2)); // 원본 정화 효과 보존
            Assert.That(EquipmentVariantRules.Copy(null), Is.Null); // 빈 장비의 안전한 복사
            item.options = new EquipmentOptionData[] { null }; // 손상된 옵션 원소
            item.curseEffects = null; // 누락된 저주 목록
            var corrupt = EquipmentVariantRules.Copy(item); // 유효하지 않은 원본의 안전한 복사
            Assert.That(corrupt.options[0], Is.Null); // 손상 원소의 안전한 보존
            Assert.That(corrupt.curseEffects, Is.Null); // 누락 목록의 안전한 보존
            Assert.That(EquipmentVariantRules.IsValid(corrupt, Policies()), Is.False); // 손상 복사본의 유효성 거절
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
