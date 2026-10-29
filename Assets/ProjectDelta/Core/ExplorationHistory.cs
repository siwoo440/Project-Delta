using System; // 빈 기록 검사
using System.Collections.Generic; // 회차 기록 목록
using System.Collections.ObjectModel; // 외부 변경 차단 목록
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class ExplorationHistory // 현재 회차의 전체 탐험 기록
    { // 클래스 시작
        private readonly List<string> entries = new List<string>(); // 시간순 탐험 사건 목록
        private readonly ReadOnlyCollection<string> readOnlyEntries; // 외부 읽기 전용 목록
        public ExplorationHistory() // 빈 회차 기록 생성
        { // 생성 시작
            readOnlyEntries = entries.AsReadOnly(); // 내부 목록의 읽기 전용 보기
        } // 생성 끝
        public IReadOnlyList<string> Entries => readOnlyEntries; // 시간순 기록 조회
        public bool Add(string message) // 실제 탐험 사건 기록
        { // 추가 시작
            if (string.IsNullOrWhiteSpace(message)) // 빈 기록 검사
            { // 빈 기록 조건 시작
                return false; // 의미 없는 기록 제외
            } // 빈 기록 조건 끝
            entries.Add(message); // 실제 사건을 시간순 보관
            return true; // 유효한 기록 추가 확인
        } // 추가 끝
    } // 클래스 끝
} // 이름 공간 끝
