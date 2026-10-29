using System; // 식별자 비교
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class MonsterDefinition // 층별 몬스터의 기본 식별 정보
    { // 클래스 시작
        public string Id { get; } // 기존 기획의 몬스터 식별자
        public string Name { get; } // 화면 표시 이름
        public bool IsElite { get; } // 상위 개체 여부
        private readonly int firstFloor; // 최초 등장 층
        private readonly int lastFloor; // 마지막 등장 층
        internal MonsterDefinition(string id, string name, bool elite, int first, int last) // 고정된 몬스터 정의
        { // 생성 시작
            Id = id; // 식별자 보관
            Name = name; // 표시 이름 보관
            IsElite = elite; // 상위 개체 여부 보관
            firstFloor = first; // 최초 등장 층 보관
            lastFloor = last; // 마지막 등장 층 보관
        } // 생성 끝
        public bool CanAppearOn(int floor) // 해당 층의 등장 가능 여부
        { // 검사 시작
            return floor >= firstFloor && floor <= lastFloor; // 기획서의 등장 범위 확인
        } // 검사 끝
    } // 클래스 끝
    public static class MonsterCatalog // 현재 1~2층 시험 대상 정의
    { // 클래스 시작
        private static readonly MonsterDefinition[] definitions = // 고정 몬스터 목록
        { // 목록 시작
            new MonsterDefinition("MON_SLIME", "슬라임", false, 1, 1), // 첫 층 일반 슬라임
            new MonsterDefinition("MON_GOBLIN", "고블린", false, 1, 2), // 첫 층과 두 번째 층 고블린
            new MonsterDefinition("MON_SLIME_QUEEN", "슬라임 퀸", true, 1, 1), // 첫 층 상위 개체
            new MonsterDefinition("MON_GOBLIN_QUEEN", "고블린 퀸", true, 2, 2) // 두 번째 층 상위 개체
        }; // 목록 끝
        public static MonsterDefinition Find(string id) // 기존 식별자의 몬스터 조회
        { // 조회 시작
            foreach (var definition in definitions) // 고정 대상 목록 순회
            { // 순회 시작
                if (string.Equals(definition.Id, id, StringComparison.Ordinal)) // 정확한 식별자 검사
                { // 일치 조건 시작
                    return definition; // 정의된 몬스터 전달
                } // 일치 조건 끝
            } // 순회 끝
            return null; // 등록되지 않은 대상
        } // 조회 끝
    } // 클래스 끝
} // 이름 공간 끝
