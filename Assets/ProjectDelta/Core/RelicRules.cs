using System; // 유물 식별자 비교
using System.Collections.Generic; // 유물 목록과 중복 검사
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class RelicDefinition // 시험 유물의 고정 정의
    { // 정의 클래스 시작
        public string Id { get; } // 안정적인 유물 식별자
        public string Name { get; } // 시험 유물 표시 이름
        public int AttackBonus { get; } // 시험 공격력 효과
        public int DefenseBonus { get; } // 시험 방어력 효과
        public int SpeedBonus { get; } // 시험 속도 효과
        internal RelicDefinition(string id, string name, int attack, int defense, int speed) // 시험 유물 정의 생성
        { // 생성 시작
            Id = id; // 유물 식별자 보관
            Name = name; // 시험 표시 이름 보관
            AttackBonus = attack; // 시험 공격력 보관
            DefenseBonus = defense; // 시험 방어력 보관
            SpeedBonus = speed; // 시험 속도 보관
        } // 생성 끝
    } // 정의 클래스 끝
    public static class TrialRelicCatalog // 정식 수치가 아닌 시험 유물 목록
    { // 목록 클래스 시작
        public const string AttackId = "trial-relic-attack"; // 시험 공격 유물 식별자
        public const string SpeedId = "trial-relic-speed"; // 시험 속도 유물 식별자
        private static readonly RelicDefinition[] definitions = // 획득과 교체 검증용 시험 정의
        { // 시험 목록 시작
            new RelicDefinition(AttackId, "시험 공격 유물", 2, 0, 0), // 임의 시험 공격력 2
            new RelicDefinition("trial-relic-defense", "시험 방어 유물", 0, 2, 0), // 임의 시험 방어력 2
            new RelicDefinition("trial-relic-amber", "시험 호박 유물", 1, 0, 0), // 임의 시험 공격력 1
            new RelicDefinition("trial-relic-stone", "시험 돌 유물", 0, 1, 0), // 임의 시험 방어력 1
            new RelicDefinition("trial-relic-iron", "시험 철 유물", 1, 1, 0), // 임의 시험 공격력과 방어력 1
            new RelicDefinition(SpeedId, "시험 속도 유물", 0, 0, 2) // 임의 시험 속도 2
        }; // 시험 목록 끝
        public static RelicDefinition Find(string id) // 등록된 시험 유물 조회
        { // 조회 시작
            foreach (var definition in definitions) // 시험 유물 순회
            { // 비교 시작
                if (string.Equals(definition.Id, id, StringComparison.Ordinal)) // 정확한 식별자 검사
                { // 일치 시작
                    return definition; // 등록 유물 전달
                } // 일치 끝
            } // 비교 끝
            return null; // 미등록 유물 거절
        } // 조회 끝
        public static string[] Ids() // 독립적인 시험 유물 식별자 목록
        { // 목록 생성 시작
            var ids = new string[definitions.Length]; // 독립 목록 생성
            for (var index = 0; index < ids.Length; index++) // 시험 정의 순회
            { // 식별자 복사 시작
                ids[index] = definitions[index].Id; // 시험 유물 식별자 복사
            } // 식별자 복사 끝
            return ids; // 외부 수정 가능한 독립 목록 전달
        } // 목록 생성 끝
    } // 목록 클래스 끝
    public sealed class RelicState // 회차 유물의 불변 보유 상태
    { // 상태 클래스 시작
        public const int Capacity = 5; // 영구 강화 없는 기본 보유 한도
        private readonly string[] ids; // 외부 변경을 차단한 보유 식별자
        private RelicState(string[] owned) // 검증된 유물 상태 생성
        { // 생성 시작
            ids = (string[])owned.Clone(); // 보유 목록의 독립 복사
        } // 생성 끝
        public static RelicState Empty => new RelicState(new string[0]); // 새 회차의 빈 유물
        public int Count => ids.Length; // 현재 보유 유물 수
        public bool Contains(string id) // 동일 유물 보유 여부
        { // 보유 검사 시작
            return Array.IndexOf(ids, id) >= 0; // 정확한 식별자 일치 여부
        } // 보유 검사 끝
        public string[] SaveIds() // 저장 가능한 독립 식별자 배열
        { // 저장 시작
            return (string[])ids.Clone(); // 현재 상태 보호를 위한 복사
        } // 저장 끝
        public static bool TryRestore(string[] saved, out RelicState state) // 저장 유물의 유효성 검증과 복원
        { // 복원 시작
            state = null; // 기본 실패 결과
            if (saved == null || saved.Length > Capacity) // 저장 목록과 기본 한도 검사
            { // 범위 오류 시작
                return false; // 누락과 초과 보유 차단
            } // 범위 오류 끝
            var seen = new HashSet<string>(StringComparer.Ordinal); // 보유 식별자 중복 검사
            foreach (var id in saved) // 저장된 유물 순회
            { // 유물 검사 시작
                if (TrialRelicCatalog.Find(id) == null || !seen.Add(id)) // 등록 여부와 중복 검사
                { // 유물 오류 시작
                    return false; // 미등록 또는 중복 유물 차단
                } // 유물 오류 끝
            } // 유물 검사 끝
            state = new RelicState(saved); // 검증된 독립 보유 상태
            return true; // 정상 복원 성공
        } // 복원 끝
        public bool TryAcquire(string id, out RelicState state) // 새 유물 획득과 즉시 효과 적용
        { // 획득 시작
            state = null; // 기본 실패 결과
            if (Count >= Capacity || Contains(id) || TrialRelicCatalog.Find(id) == null) // 한도와 중복 및 등록 검사
            { // 획득 오류 시작
                return false; // 기존 보유 상태 유지
            } // 획득 오류 끝
            var result = new string[Count + 1]; // 새 유물 포함 목록
            Array.Copy(ids, result, Count); // 기존 보유 목록 복사
            result[Count] = id; // 새 유물 추가
            state = new RelicState(result); // 독립적인 새 보유 상태
            return true; // 유물 획득 성공
        } // 획득 끝
        public bool TryReplace(string existingId, string incomingId, out RelicState state) // 명시된 유물 교체
        { // 교체 시작
            state = null; // 기본 실패 결과
            var index = Array.IndexOf(ids, existingId); // 확인한 기존 유물 위치
            if (index < 0 || Contains(incomingId) || TrialRelicCatalog.Find(incomingId) == null) // 기존 보유와 새 유물 검사
            { // 교체 오류 시작
                return false; // 기존 상태와 유물 보존
            } // 교체 오류 끝
            var result = SaveIds(); // 기존 목록의 독립 복사
            result[index] = incomingId; // 확인한 자리의 유물 교체
            state = new RelicState(result); // 독립적인 새 보유 상태
            return true; // 유물 교체 성공
        } // 교체 끝
        public int AttackBonus => SumEffect(0); // 현재 시험 공격력 효과 합계
        public int DefenseBonus => SumEffect(1); // 현재 시험 방어력 효과 합계
        public int SpeedBonus => SumEffect(2); // 현재 시험 속도 효과 합계
        private int SumEffect(int kind) // 보유 유물의 시험 효과 합산
        { // 합산 시작
            var total = 0; // 효과 합계 초기화
            foreach (var id in ids) // 보유 유물 순회
            { // 효과 합산 시작
                var relic = TrialRelicCatalog.Find(id); // 검증된 시험 유물 조회
                total += kind == 0 ? relic.AttackBonus : kind == 1 ? relic.DefenseBonus : relic.SpeedBonus; // 요청 능력치 효과 반영
            } // 효과 합산 끝
            return total; // 보유 효과 합계 전달
        } // 합산 끝
    } // 상태 클래스 끝
} // 이름 공간 끝
