using System.Collections.Generic; // 공개 타일 목록 생성 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public static class DungeonMinimapTileRevealService // 미니맵 타일 공개 규칙
    {
        public static IReadOnlyList<GridPosition> CollectAround( // 현재 칸 주변 공개 좌표 계산
            GridPosition center, // 플레이어 기준 좌표
            int radius, // 공개 반경
            int minX, // 방 최소 X
            int maxX, // 방 최대 X
            int minZ, // 방 최소 Z
            int maxZ) // 방 최대 Z
        {
            int safeRadius = // 안전한 공개 반경 계산
                radius < 0 // 음수 반경 여부 확인
                    ? 0 // 음수 반경 0 보정
                    : radius; // 정상 반경 유지

            List<GridPosition> result = // 공개 좌표 목록 생성
                new List<GridPosition>(); // 빈 목록 초기화

            for (int z = center.Z - safeRadius; // 최소 Z부터 반복
                 z <= center.Z + safeRadius; // 최대 Z까지 반복
                 z++) // Z 증가
            {
                for (int x = center.X - safeRadius; // 최소 X부터 반복
                     x <= center.X + safeRadius; // 최대 X까지 반복
                     x++) // X 증가
                {
                    if (x < minX // 왼쪽 방 밖 여부 확인
                        || x > maxX // 오른쪽 방 밖 여부 확인
                        || z < minZ // 아래쪽 방 밖 여부 확인
                        || z > maxZ) // 위쪽 방 밖 여부 확인
                    {
                        continue; // 방 밖 좌표 제외
                    }

                    result.Add( // 공개 좌표 추가
                        new GridPosition( // 그리드 좌표 생성
                            x, // X 좌표 저장
                            z)); // Z 좌표 저장
                }
            }

            return result; // 공개 좌표 목록 반환
        }
    }
}
