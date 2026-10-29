using System; // 시험 식별자와 효과 합산
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class TrialEquipmentTraits // 확정되지 않은 장비 특성의 시험 규칙
    { // 클래스 시작
        public const string Attack = "ATTACK"; // 시험 공격력 옵션
        public const string Defense = "DEFENSE"; // 시험 방어력 옵션
        public const string Speed = "SPEED"; // 시험 속도 옵션
        public static EquipmentOptionPolicy[] Policies() // 시험 옵션별 중복 정책
        { // 정책 시작
            return new[] { new EquipmentOptionPolicy { optionId = Attack }, new EquipmentOptionPolicy { optionId = Defense }, new EquipmentOptionPolicy { optionId = Speed } }; // 시험용 중복 금지 옵션 목록
        } // 정책 끝
        public static bool IsValid(EquipmentVariantData item) // 지원하는 시험 개별 장비 검사
        { // 검사 시작
            if (item == null || EquipmentCatalog.Find(item.definitionId) == null || !Guid.TryParseExact(item.instanceId, "N", out _) || !EquipmentVariantRules.IsValid(item, Policies())) // 장비 정의와 식별자 및 옵션 정책 검사
            { // 오류 시작
                return false; // 미지원 시험 장비 차단
            } // 오류 끝
            foreach (var effect in item.options) // 기본 추가 옵션 순회
            { // 범위 검사 시작
                if (effect.value < -10 || effect.value > 10) // 시험 보정 범위 검사
                { // 범위 오류 시작
                    return false; // 지나친 시험 수치 차단
                } // 범위 오류 끝
            } // 범위 검사 끝
            foreach (var effect in item.curseEffects) // 공개 저주 효과 순회
            { // 범위 검사 시작
                if (effect.value < -10 || effect.value > 10) // 시험 보정 범위 검사
                { // 범위 오류 시작
                    return false; // 지나친 시험 수치 차단
                } // 범위 오류 끝
            } // 범위 검사 끝
            return true; // 지원하는 시험 장비 확인
        } // 검사 끝
        public static EquipmentVariantData Create(string definitionId, string grade, bool cursed) // 공개할 고유 시험 장비 생성
        { // 생성 시작
            var count = EquipmentVariantRules.RequiredOptionCount(grade); // 등급별 옵션 개수
            if (EquipmentCatalog.Find(definitionId) == null || count < 1) // 기본 장비와 등급 검사
            { // 생성 오류 시작
                return null; // 미등록 시험 장비 차단
            } // 생성 오류 끝
            var options = count == 1 ? new[] { new EquipmentOptionData { optionId = Attack, value = 2 } } : new[] { new EquipmentOptionData { optionId = Attack, value = 2 }, new EquipmentOptionData { optionId = Defense, value = 2 } }; // 시험용 추가 효과
            var curses = cursed ? new[] { new EquipmentOptionData { optionId = Speed, value = -2 } } : new EquipmentOptionData[0]; // 획득 전에 공개할 시험 저주
            return new EquipmentVariantData { instanceId = Guid.NewGuid().ToString("N"), definitionId = definitionId, grade = grade, options = options, cursed = cursed, curseEffects = curses, purification = cursed ? new EquipmentPurificationPlan { sourceDefinitionId = definitionId, kind = EquipmentVariantRules.Purify, resultId = definitionId, resultQuantity = 1 } : null }; // 시험 장비와 시험 정화 결과
        } // 생성 끝
        public static EquipmentVariantData CreateRandom(string definitionId, string grade, bool cursed, Random random) // 시험 확률로 개별 옵션 추첨
        { // 추첨 시작
            if (random == null) // 실제 난수 생성기 검사
            { // 난수 오류 시작
                return null; // 확인할 수 없는 후보 차단
            } // 난수 오류 끝
            var item = Create(definitionId, grade, cursed); // 기획 등급과 시험 저주 기본 후보
            if (item == null) // 유효한 장비 후보 검사
            { // 후보 오류 시작
                return null; // 잘못된 후보 차단
            } // 후보 오류 끝
            var ids = new[] { Attack, Defense, Speed }; // 미정 확률 대신 사용하는 시험 옵션 목록
            var first = random.Next(0, ids.Length); // 첫 옵션의 시험 균등 추첨
            item.options[0] = new EquipmentOptionData { optionId = ids[first], value = 2 }; // 결정된 첫 시험 옵션
            if (item.options.Length > 1) // 두 번째 옵션 필요 검사
            { // 두 번째 추첨 시작
                var second = random.Next(0, ids.Length - 1); // 첫 옵션을 제외한 시험 추첨
                if (second >= first) // 첫 옵션의 중복 회피 검사
                { // 중복 회피 시작
                    second++; // 선택하지 않은 실제 옵션 위치
                } // 중복 회피 끝
                item.options[1] = new EquipmentOptionData { optionId = ids[second], value = 2 }; // 결정된 두 번째 시험 옵션
            } // 두 번째 추첨 끝
            return item; // 저장할 고유 장비와 확정된 옵션 전달
        } // 추첨 끝
        public static string Describe(EquipmentVariantData item) // 획득과 장착 전 공개 문구
        { // 표시 시작
            if (!IsValid(item)) // 실제 시험 개별 장비 검사
            { // 오류 시작
                return "장비 정보 없음"; // 확인할 수 없는 장비 안내
            } // 오류 끝
            var grade = item.grade == EquipmentVariantRules.Common ? "일반" : item.grade == EquipmentVariantRules.Uncommon ? "고급" : item.grade == EquipmentVariantRules.Rare ? "희귀" : "전설"; // 기획 등급의 표시 이름
            var text = grade + (item.cursed ? " · 저주" : "") + " · 시험 옵션"; // 등급과 저주 공개
            foreach (var effect in item.options) // 일반 추가 옵션 순회
            { // 효과 표시 시작
                text += " · " + Label(effect); // 실제 옵션과 수치 표시
            } // 효과 표시 끝
            foreach (var effect in item.curseEffects) // 저주 효과 순회
            { // 저주 표시 시작
                text += " · " + Label(effect); // 실제 부정 효과와 수치 공개
            } // 저주 표시 끝
            return text; // 전체 시험 효과 안내
        } // 표시 끝
        private static string Label(EquipmentOptionData effect) // 시험 효과의 표시 이름
        { // 표시 시작
            var name = effect.optionId == Attack ? "공격" : effect.optionId == Defense ? "방어" : "속도"; // 효과별 능력치 이름
            return name + (effect.value >= 0 ? "+" : "") + effect.value; // 실제 부호와 수치
        } // 표시 끝
    } // 클래스 끝
} // 이름 공간 끝
