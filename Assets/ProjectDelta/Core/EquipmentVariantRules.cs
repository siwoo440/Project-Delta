using System; // 독립 데이터 직렬화
using System.Collections.Generic; // 옵션 정책과 중복 검사
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    [Serializable] // Unity 저장 형식 지원
    public sealed class EquipmentOptionData // 저장 가능한 개별 능력 효과
    { // 클래스 시작
        public string optionId; // 옵션 정의 식별자
        public int value; // 양수 또는 음수 효과 값
    } // 클래스 끝
    [Serializable] // 옵션 정책의 독립 저장 형식
    public sealed class EquipmentOptionPolicy // 옵션별 중복 허용 정책
    { // 클래스 시작
        public string optionId; // 옵션 정의 식별자
        public bool allowDuplicate; // 같은 목록 안의 중복 허용 여부
    } // 클래스 끝
    [Serializable] // 아이템별 정화 계획의 저장 형식
    public sealed class EquipmentPurificationPlan // 명시된 저주 제거 결과
    { // 클래스 시작
        public string sourceDefinitionId; // 이 계획이 적용되는 장비 정의
        public string kind; // 정화 또는 교환 및 보상 결과 종류
        public string resultId; // 명시적으로 지정된 결과 식별자
        public int resultQuantity; // 명시적으로 지정된 결과 수량
        public EquipmentOptionData[] resultEffects; // 지정된 결과 효과 목록
    } // 클래스 끝
    [Serializable] // 개별 장비의 회차 저장 형식
    public sealed class EquipmentVariantData // 등급과 공개 저주를 포함한 장비 인스턴스
    { // 클래스 시작
        public string instanceId; // 같은 종류를 구분하는 개별 식별자
        public string definitionId; // 기존 기본 장비 정의 식별자
        public string grade; // 일반과 고급 및 희귀와 전설 등급
        public EquipmentOptionData[] options; // 등급별 추가 옵션 목록
        public bool cursed; // 저주 장비 여부
        public EquipmentOptionData[] curseEffects; // 획득 전에 공개되는 저주 효과
        public EquipmentPurificationPlan purification; // 미정일 수 있는 아이템별 정화 결과
    } // 클래스 끝
    public static class EquipmentVariantRules // 장비 등급과 공개 효과 검사
    { // 클래스 시작
        public const string Common = "COMMON"; // 일반 등급 식별자
        public const string Uncommon = "UNCOMMON"; // 고급 등급 식별자
        public const string Rare = "RARE"; // 희귀 등급 식별자
        public const string Legendary = "LEGENDARY"; // 전설 등급 식별자
        public const string Purify = "PURIFY"; // 일반 장비로 정화
        public const string ReduceEffects = "REDUCE_EFFECTS"; // 긍정과 부정 효과 감소
        public const string ExchangeEquipment = "EQUIPMENT"; // 지정 장비 교환
        public const string ExchangeRelic = "RELIC"; // 지정 유물 교환
        public const string DestroyForReward = "DESTROY_REWARD"; // 소멸 후 지정 보상
        public static int RequiredOptionCount(string grade) // 기획된 등급별 추가 옵션 수
        { // 조회 시작
            switch (grade) // 저장된 등급 구분
            { // 등급 선택 시작
                case Common: return 1; // 일반 등급 한 옵션
                case Uncommon: return 1; // 고급 등급 한 옵션
                case Rare: return 2; // 희귀 등급 두 옵션
                case Legendary: return 2; // 전설 등급 두 옵션
                default: return -1; // 미등록 등급의 조회 실패
            } // 등급 선택 끝
        } // 조회 끝
        public static EquipmentVariantData Copy(EquipmentVariantData item) // 저장과 장착 사이의 개별 장비 심층 복사
        { // 복사 시작
            if (item == null) // 빈 장비 검사
            { // 빈 장비 시작
                return null; // 장비가 없는 상태 보존
            } // 빈 장비 끝
            return new EquipmentVariantData { instanceId = item.instanceId, definitionId = item.definitionId, grade = item.grade, options = CopyOptions(item.options), cursed = item.cursed, curseEffects = CopyOptions(item.curseEffects), purification = CopyPlan(item.purification) }; // 모든 저장 필드와 효과의 독립 복사
        } // 복사 끝
        public static bool IsValid(EquipmentVariantData item, EquipmentOptionPolicy[] policies) // 개별 장비와 공개 효과 검사
        { // 검증 시작
            if (item == null || !ValidId(item.instanceId) || !ValidId(item.definitionId)) // 개별 장비와 기본 정의 검사
            { // 식별 오류 시작
                return false; // 누락된 장비 식별자 차단
            } // 식별 오류 끝
            var count = RequiredOptionCount(item.grade); // 기획된 등급별 옵션 수
            if (count < 0 || item.options == null || item.options.Length != count || item.curseEffects == null || (item.cursed != (item.curseEffects.Length > 0))) // 등급 수량과 공개 저주 상태 검사
            { // 상태 오류 시작
                return false; // 숨겨진 저주와 잘못된 옵션 수 차단
            } // 상태 오류 끝
            if (!TryPolicies(policies, out var definitions) || !ValidateOptions(item.options, definitions) || !ValidateOptions(item.curseEffects, definitions)) // 옵션별 중복 정책과 공개 효과 검사
            { // 옵션 오류 시작
                return false; // 미등록 옵션과 금지된 중복 차단
            } // 옵션 오류 끝
            if (item.purification != null && (!item.cursed || !ValidatePlan(item.definitionId, item.purification, definitions))) // 명시된 아이템별 정화 계획 검사
            { // 정화 계획 오류 시작
                return false; // 다른 장비와 잘못된 계획 차단
            } // 정화 계획 오류 끝
            return true; // 유효한 개별 장비 상태
        } // 검증 끝
        public static bool TryGetPurification(EquipmentVariantData item, EquipmentOptionPolicy[] policies, out EquipmentPurificationPlan plan) // 명시된 아이템별 정화 결과 조회
        { // 조회 시작
            plan = null; // 기본 정화 결과 없음
            if (!IsValid(item, policies) || !item.cursed || item.purification == null) // 유효한 저주와 지정 계획 검사
            { // 조회 거절 시작
                return false; // 미정 결과와 저주 없는 장비 차단
            } // 조회 거절 끝
            plan = CopyPlan(item.purification); // 검증된 정화 계획의 독립 복사
            return true; // 지정된 결과 조회 성공
        } // 조회 끝
        private static EquipmentPurificationPlan CopyPlan(EquipmentPurificationPlan source) // 아이템별 정화 계획 복사
        { // 복사 시작
            if (source == null) // 미지정 정화 계획 검사
            { // 미지정 계획 시작
                return null; // 계획이 없는 상태 보존
            } // 미지정 계획 끝
            return new EquipmentPurificationPlan { sourceDefinitionId = source.sourceDefinitionId, kind = source.kind, resultId = source.resultId, resultQuantity = source.resultQuantity, resultEffects = CopyOptions(source.resultEffects) }; // 정화 계획과 결과 효과의 독립 복사
        } // 복사 끝
        private static EquipmentOptionData[] CopyOptions(EquipmentOptionData[] source) // 손상 상태도 보존하는 효과 목록 복사
        { // 복사 시작
            if (source == null) // 누락된 효과 목록 검사
            { // 누락 목록 시작
                return null; // 원본 누락 상태 보존
            } // 누락 목록 끝
            var result = new EquipmentOptionData[source.Length]; // 효과별 독립 배열
            for (var index = 0; index < source.Length; index++) // 원본 효과 순회
            { // 효과 복사 시작
                var option = source[index]; // 원본 효과 조회
                result[index] = option == null ? null : new EquipmentOptionData { optionId = option.optionId, value = option.value }; // 누락 상태 또는 독립 효과 보관
            } // 효과 복사 끝
            return result; // 복사한 효과 목록 전달
        } // 복사 끝
        private static bool TryPolicies(EquipmentOptionPolicy[] policies, out Dictionary<string, bool> definitions) // 개별 옵션 정책의 고유성 검사
        { // 정책 검사 시작
            definitions = new Dictionary<string, bool>(StringComparer.Ordinal); // 대소문자를 구분하는 옵션 정책
            if (policies == null) // 옵션 정책 목록 검사
            { // 누락 조건 시작
                return false; // 미지정 정책 차단
            } // 누락 조건 끝
            foreach (var policy in policies) // 제공된 정책 순회
            { // 정책 검사 시작
                if (policy == null || !ValidId(policy.optionId) || definitions.ContainsKey(policy.optionId)) // 누락과 중복 정책 검사
                { // 정책 오류 시작
                    return false; // 모호한 정책과 미등록 식별자 차단
                } // 정책 오류 끝
                definitions.Add(policy.optionId, policy.allowDuplicate); // 명시된 중복 여부 보관
            } // 정책 검사 끝
            return true; // 유효한 옵션 정책 구성
        } // 정책 검사 끝
        private static bool ValidateOptions(EquipmentOptionData[] options, Dictionary<string, bool> definitions) // 같은 목록의 옵션과 중복 검사
        { // 옵션 검사 시작
            if (options == null) // 필수 옵션 목록 검사
            { // 목록 오류 시작
                return false; // 누락된 효과 목록 차단
            } // 목록 오류 끝
            var seen = new HashSet<string>(StringComparer.Ordinal); // 같은 목록의 옵션 식별자
            foreach (var option in options) // 표시할 효과 순회
            { // 효과 검사 시작
                if (option == null || !ValidId(option.optionId) || !definitions.TryGetValue(option.optionId, out var allowDuplicate) || (!seen.Add(option.optionId) && !allowDuplicate)) // 옵션 정의와 중복 허용 검사
                { // 효과 오류 시작
                    return false; // 누락과 허용되지 않은 중복 차단
                } // 효과 오류 끝
            } // 효과 검사 끝
            return true; // 양수와 음수를 포함한 유효 효과 목록
        } // 옵션 검사 끝
        private static bool ValidatePlan(string definitionId, EquipmentPurificationPlan plan, Dictionary<string, bool> definitions) // 지정된 정화 결과 검사
        { // 계획 검사 시작
            if (plan.sourceDefinitionId != definitionId || !ValidId(plan.resultId) || plan.resultQuantity <= 0) // 해당 장비와 명시된 결과 검사
            { // 계획 오류 시작
                return false; // 다른 장비와 미지정 결과 차단
            } // 계획 오류 끝
            if (plan.kind != Purify && plan.kind != ReduceEffects && plan.kind != ExchangeEquipment && plan.kind != ExchangeRelic && plan.kind != DestroyForReward) // 확인된 결과 종류 검사
            { // 종류 오류 시작
                return false; // 미등록 정화 결과 차단
            } // 종류 오류 끝
            return plan.resultEffects == null || ValidateOptions(plan.resultEffects, definitions); // 명시된 결과 효과와 중복 정책 검사
        } // 계획 검사 끝
        private static bool ValidId(string id) // 저장 식별자의 기본 유효성 검사
        { // 식별 검사 시작
            return !string.IsNullOrWhiteSpace(id) && id.Length <= 128; // 비어 있지 않은 제한 길이 확인
        } // 식별 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
