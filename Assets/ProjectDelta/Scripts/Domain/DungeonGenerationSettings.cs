using System; // 예외 타입 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public sealed class DungeonGenerationSettings // 던전 한 층 생성 규칙
    {
        public int TargetRoomCount { get; } // 전체 목표 방 수
        public int MinMainPathLength { get; } // 시작 방과 계단 방을 포함한 메인 경로 최소 방 수
        public int MaxMainPathLength { get; } // 시작 방과 계단 방을 포함한 메인 경로 최대 방 수
        public double BranchChance { get; } // 메인 경로의 사용 가능한 출구에서 가지 생성을 시도할 확률
        public int MinBranchLength { get; } // 가지 하나의 최소 방 수
        public int MaxBranchLength { get; } // 가지 하나의 최대 방 수
        public double SpecialCandidateChance { get; } // 가지 끝 방을 특수 방 후보로 지정할 확률

        public DungeonGenerationSettings(
            int targetRoomCount,
            int minMainPathLength,
            int maxMainPathLength,
            double branchChance = 0.65d,
            int minBranchLength = 1,
            int maxBranchLength = 3,
            double specialCandidateChance = 0.30d) // 생성 규칙 생성자
        {
            if (targetRoomCount < 1) // 전체 목표 방 수 확인
            {
                throw new ArgumentOutOfRangeException(nameof(targetRoomCount), "전체 목표 방 수는 1 이상이어야 합니다."); // 잘못된 방 수 차단
            }

            if (minMainPathLength < 1) // 최소 메인 경로 길이 확인
            {
                throw new ArgumentOutOfRangeException(nameof(minMainPathLength), "메인 경로 최소 길이는 1 이상이어야 합니다."); // 잘못된 최소 길이 차단
            }

            if (maxMainPathLength < minMainPathLength) // 최소·최대 순서 확인
            {
                throw new ArgumentOutOfRangeException(nameof(maxMainPathLength), "메인 경로 최대 길이는 최소 길이 이상이어야 합니다."); // 역전된 범위 차단
            }

            if (maxMainPathLength > targetRoomCount) // 메인 경로가 전체 목표 방 수보다 긴지 확인
            {
                throw new ArgumentOutOfRangeException(nameof(maxMainPathLength), "메인 경로 최대 길이는 전체 목표 방 수를 넘을 수 없습니다."); // 전체 방 수 초과 차단
            }

            if (branchChance < 0d || branchChance > 1d) // 분기 확률 범위 확인
            {
                throw new ArgumentOutOfRangeException(nameof(branchChance), "분기 확률은 0~1 범위여야 합니다."); // 잘못된 확률 차단
            }

            if (minBranchLength < 1) // 가지 최소 길이 확인
            {
                throw new ArgumentOutOfRangeException(nameof(minBranchLength), "가지 최소 길이는 1 이상이어야 합니다."); // 잘못된 최소 길이 차단
            }

            if (maxBranchLength < minBranchLength) // 가지 최소·최대 순서 확인
            {
                throw new ArgumentOutOfRangeException(nameof(maxBranchLength), "가지 최대 길이는 최소 길이 이상이어야 합니다."); // 역전된 가지 길이 차단
            }

            if (specialCandidateChance < 0d || specialCandidateChance > 1d) // 특수 방 후보 확률 범위 확인
            {
                throw new ArgumentOutOfRangeException(nameof(specialCandidateChance), "특수 방 후보 확률은 0~1 범위여야 합니다."); // 잘못된 확률 차단
            }

            TargetRoomCount = targetRoomCount; // 전체 목표 방 수 저장
            MinMainPathLength = minMainPathLength; // 최소 메인 경로 길이 저장
            MaxMainPathLength = maxMainPathLength; // 최대 메인 경로 길이 저장
            BranchChance = branchChance; // 분기 확률 저장
            MinBranchLength = minBranchLength; // 가지 최소 길이 저장
            MaxBranchLength = maxBranchLength; // 가지 최대 길이 저장
            SpecialCandidateChance = specialCandidateChance; // 특수 방 후보 확률 저장
        }
    }
}
