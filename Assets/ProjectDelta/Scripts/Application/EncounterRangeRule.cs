using System; // 절대 거리 계산 사용
using ProjectDelta.Domain; // GridPosition 사용

namespace ProjectDelta.Application // 애플리케이션 네임스페이스
{
    // 45일차: 플레이어와 몬스터의 GridPosition 기반 Encounter 포착 범위를 계산한다.
    public static class EncounterRangeRule
    {
        public const int DefaultCaptureRange = 1; // 기본 포착 범위 1칸

        public static bool IsWithinRange(
            GridPosition playerPosition,
            GridPosition monsterPosition,
            int captureRange = DefaultCaptureRange)
        {
            if (captureRange < 0) // 음수 범위 확인
            {
                return false; // 잘못된 범위 거부
            }

            long deltaX =
                Math.Abs((long)playerPosition.X - monsterPosition.X); // X축 거리 계산

            long deltaZ =
                Math.Abs((long)playerPosition.Z - monsterPosition.Z); // Z축 거리 계산

            return deltaX <= captureRange
                && deltaZ <= captureRange; // 상하좌우·대각선 포함 사각 1칸 범위 판정
        }
    }
}
