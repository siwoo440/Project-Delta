using System; // 식별자 비교 규칙
using System.Collections.Generic; // 관찰한 대상 기록
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class ObservationJournal // 현재 회차의 1단계 관찰 기록
    { // 클래스 시작
        private readonly HashSet<string> observed = new HashSet<string>(StringComparer.Ordinal); // 관찰한 대상 식별자
        public bool Contains(string id) // 대상의 관찰 기록 조회
        { // 조회 시작
            return !string.IsNullOrWhiteSpace(id) && observed.Contains(id); // 유효한 대상의 기록 확인
        } // 조회 끝
        public bool TryRecord(string id) // 새 대상의 관찰 기록 추가
        { // 기록 시작
            return !string.IsNullOrWhiteSpace(id) && observed.Add(id); // 빈 식별자와 중복 기록 차단
        } // 기록 끝
    } // 클래스 끝
} // 이름 공간 끝
