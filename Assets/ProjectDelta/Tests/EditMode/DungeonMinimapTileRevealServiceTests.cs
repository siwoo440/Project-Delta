using System.Collections.Generic; // 목록 검사 기능 사용
using NUnit.Framework; // Unity EditMode 테스트 기능 사용
using ProjectDelta.Domain; // 미니맵 타일 공개 규칙 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class DungeonMinimapTileRevealServiceTests // 미니맵 타일 공개 테스트
    {
        [Test] // 중앙 3x3 공개 테스트
        public void CollectAround_CenterTile_RevealsNineTiles() // 중앙 주변 8칸 포함 검사
        {
            IReadOnlyList<GridPosition> result = // 공개 타일 계산
                DungeonMinimapTileRevealService.CollectAround( // 공개 서비스 호출
                    new GridPosition(0, 0), // 플레이어 중앙 좌표
                    1, // 주변 한 칸 반경
                    -2, // 최소 X 범위
                    2, // 최대 X 범위
                    -2, // 최소 Z 범위
                    2); // 최대 Z 범위

            Assert.That(result.Count, Is.EqualTo(9)); // 3x3 타일 수 확인
            Assert.That(result, Does.Contain(new GridPosition(-1, -1))); // 좌하단 주변 칸 확인
            Assert.That(result, Does.Contain(new GridPosition(1, 1))); // 우상단 주변 칸 확인
        }

        [Test] // 방 모서리 범위 제한 테스트
        public void CollectAround_CornerTile_ClipsOutsideRoomBounds() // 방 밖 타일 제외 검사
        {
            IReadOnlyList<GridPosition> result = // 공개 타일 계산
                DungeonMinimapTileRevealService.CollectAround( // 공개 서비스 호출
                    new GridPosition(2, 2), // 우상단 모서리 좌표
                    1, // 주변 한 칸 반경
                    -2, // 최소 X 범위
                    2, // 최대 X 범위
                    -2, // 최소 Z 범위
                    2); // 최대 Z 범위

            Assert.That(result.Count, Is.EqualTo(4)); // 방 내부 네 칸만 공개 확인
            Assert.That(result, Does.Contain(new GridPosition(1, 1))); // 대각선 내부 칸 확인
            Assert.That(result, Does.Contain(new GridPosition(2, 2))); // 현재 칸 확인
            CollectionAssert.DoesNotContain(result, new GridPosition(3, 2)); // 방 밖 X 칸 제외 확인
            CollectionAssert.DoesNotContain(result, new GridPosition(2, 3)); // 방 밖 Z 칸 제외 확인
        }

        [Test] // 음수 반경 방어 테스트
        public void CollectAround_NegativeRadius_RevealsCurrentTileOnly() // 잘못된 반경 안전 처리 검사
        {
            IReadOnlyList<GridPosition> result = // 공개 타일 계산
                DungeonMinimapTileRevealService.CollectAround( // 공개 서비스 호출
                    new GridPosition(0, 0), // 현재 좌표
                    -1, // 잘못된 음수 반경
                    -2, // 최소 X 범위
                    2, // 최대 X 범위
                    -2, // 최소 Z 범위
                    2); // 최대 Z 범위

            Assert.That(result.Count, Is.EqualTo(1)); // 현재 칸 한 개 확인
            Assert.That(result[0], Is.EqualTo(GridPosition.Zero)); // 현재 칸 좌표 확인
        }
    }
}
