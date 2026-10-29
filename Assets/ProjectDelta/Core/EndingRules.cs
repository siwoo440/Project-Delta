using System; // 독립 읽기 전용 후보 배열
using System.Collections.Generic; // 시험 결말 후보 목록
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class EndingCandidateSet // 조건이 확인된 독립 결말 후보
    { // 클래스 시작
        public IReadOnlyList<string> ReturnCandidates { get; } // 현실 귀환 방향의 확인된 후보
        public IReadOnlyList<string> StayCandidates { get; } // 던전 잔류 방향의 확인된 후보
        public string AutomaticCandidate { get; } // 패배 또는 항복의 단일 결말
        internal EndingCandidateSet(string[] returns, string[] stays, string automatic) // 독립 후보 목록 생성
        { // 생성 시작
            ReturnCandidates = Array.AsReadOnly((string[])returns.Clone()); // 귀환 후보의 읽기 전용 복사
            StayCandidates = Array.AsReadOnly((string[])stays.Clone()); // 잔류 후보의 읽기 전용 복사
            AutomaticCandidate = automatic; // 선택 없는 결과의 결말
        } // 생성 끝
    } // 클래스 끝
    public static class EndingRules // 확인된 시험 결말 후보와 최초 보상
    { // 클래스 시작
        public const string ReturnBasic = "return-basic"; // 현실로의 귀환 식별자
        public const string ReturnHeroic = "return-heroic"; // 영웅의 귀환 식별자
        public const string ReturnEmptyHanded = "return-empty-handed"; // 빈손의 귀환 식별자
        public const string ReturnWounded = "return-wounded"; // 상처뿐인 귀환 식별자
        public const string StayBasic = "stay-basic"; // 던전의 왕 식별자
        public const string StayConquest = "stay-conquest"; // 정복의 왕 식별자
        public const string DemonServant = "demon-servant"; // 마왕의 종 식별자
        public static bool TryCandidates(int bossOutcome, int health, int maxHealth, int equippedCount, int relicCount, out EndingCandidateSet result) // 실제 마왕 결과의 확인된 후보 계산
        { // 계산 시작
            result = null; // 기본 계산 실패
            if (bossOutcome < 1 || bossOutcome > 3 || maxHealth <= 0 || health < 0 || health > maxHealth || equippedCount < 0 || equippedCount > 4 || relicCount < 0 || (bossOutcome == 2 ? health != 0 : health == 0)) // 결과와 실제 자원 및 장비 범위 검사
            { // 오류 시작
                return false; // 잘못된 승패와 소지 상태 차단
            } // 오류 끝
            if (bossOutcome == 2 || bossOutcome == 3) // 실제 마왕 패배 또는 항복
            { // 자동 결말 시작
                result = new EndingCandidateSet(new string[0], new string[0], DemonServant); // 선택 없는 동일 마왕 결과
                return true; // 자동 결말 후보 계산 성공
            } // 자동 결말 끝
            var returns = new List<string> { ReturnBasic, ReturnHeroic }; // 일반 승리의 기본 및 영웅 귀환
            if (equippedCount == 0 && relicCount == 0) // 장착 장비와 유물 부재 검사
            { // 빈손 후보 시작
                returns.Add(ReturnEmptyHanded); // 실제 빈손 귀환 추가
            } // 빈손 후보 끝
            if ((long)health * 5 <= maxHealth) // 체력이 최대의 이십 퍼센트 이하인지 검사
            { // 저체력 후보 시작
                returns.Add(ReturnWounded); // 확인된 체력 조건의 귀환 추가
            } // 저체력 후보 끝
            result = new EndingCandidateSet(returns.ToArray(), new[] { StayBasic, StayConquest }, null); // 확정 가능한 모든 방향 후보
            return true; // 시험 범위의 후보 계산 성공
        } // 계산 끝
        public static string DisplayName(string candidateId) // 확인된 결말의 실제 한글 이름
        { // 조회 시작
            switch (candidateId) // 결말 식별자 검사
            { // 선택 시작
                case ReturnBasic: return "현실로의 귀환"; // 기본 귀환 이름
                case ReturnHeroic: return "영웅의 귀환"; // 영웅 귀환 이름
                case ReturnEmptyHanded: return "빈손의 귀환"; // 빈손 귀환 이름
                case ReturnWounded: return "상처뿐인 귀환"; // 저체력 귀환 이름
                case StayBasic: return "던전의 왕"; // 기본 잔류 이름
                case StayConquest: return "정복의 왕"; // 정복 잔류 이름
                case DemonServant: return "마왕의 종"; // 마왕 패배와 항복 이름
                default: return null; // 확인되지 않은 식별자
            } // 선택 끝
        } // 조회 끝
        public static bool TryFirstUnlockShards(string candidateId, out int shards) // 기획서의 최초 해금 파편 조회
        { // 조회 시작
            shards = 0; // 기본 지급 없음
            switch (candidateId) // 확인된 결말 식별자 검사
            { // 결말 선택 시작
                case ReturnBasic: // 기본 현실 귀환
                case StayBasic: // 기본 던전 잔류
                case DemonServant: // 마왕의 종
                    shards = 5; // 기획의 기본 최초 해금 파편
                    return true; // 실제 보상 조회 성공
                case ReturnHeroic: // 일반 승리 귀환
                case ReturnEmptyHanded: // 빈손 귀환
                case ReturnWounded: // 저체력 귀환
                case StayConquest: // 일반 승리 잔류
                    shards = 10; // 기획의 최초 해금 파편
                    return true; // 실제 보상 조회 성공
                default: // 확인되지 않은 결말
                    return false; // 미확정 보상을 영으로 확정하는 처리 차단
            } // 결말 선택 끝
        } // 조회 끝
    } // 클래스 끝
} // 이름 공간 끝
