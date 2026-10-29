using System; // 잘못된 층 범위 검사
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class FloorSeedRules // 회차 시드와 층별 고정 시드
    { // 클래스 시작
        public static int For(int seed, int floor) // 기존 1·2층과 호환되는 층별 시드
        { // 계산 시작
            if (floor < 1 || floor > 5) // 기획된 층 범위 검사
            { // 오류 시작
                throw new ArgumentOutOfRangeException(nameof(floor)); // 잘못된 층 거절
            } // 오류 끝
            return unchecked(seed + (floor - 1) * 7919); // 정수 넘침까지 재현되는 층별 시드
        } // 계산 끝
    } // 클래스 끝
} // 이름 공간 끝
