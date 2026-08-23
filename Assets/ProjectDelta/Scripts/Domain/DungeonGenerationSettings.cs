using System; // 예외 타입 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public sealed class DungeonGenerationSettings // 던전 한 층 생성 규칙
    {
        public int TargetRoomCount { get; } // 이후 가지 방까지 포함한 전체 목표 방 수
        public int MinMainPathLength { get; } // 시작 방과 계단 방을 포함한 메인 경로 최소 방 수
        public int MaxMainPathLength { get; } // 시작 방과 계단 방을 포함한 메인 경로 최대 방 수

        public DungeonGenerationSettings(int targetRoomCount, int minMainPathLength, int maxMainPathLength) // 생성 규칙 생성자
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

            TargetRoomCount = targetRoomCount; // 전체 목표 방 수 저장
            MinMainPathLength = minMainPathLength; // 최소 메인 경로 길이 저장
            MaxMainPathLength = maxMainPathLength; // 최대 메인 경로 길이 저장
        }
    }
}
