using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 조우 범위 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class EncounterRangeTests // 조우 범위 검증
    { // 클래스 시작
        [TestCase(0, 0, true)] // 같은 칸 검증
        [TestCase(1, 0, true)] // 오른쪽 한 칸 검증
        [TestCase(-1, -1, true)] // 대각선 한 칸 검증
        [TestCase(2, 0, false)] // 두 칸 거리 검증
        [TestCase(1, 2, false)] // 세로 두 칸 거리 검증
        public void Contains_UsesEightAdjacentCells(int x, int y, bool expected) // 기존 포착 범위 확인
        { // 검증 시작
            Assert.That(EncounterRange.Contains(x, y, 0, 0), Is.EqualTo(expected)); // 실제 포착 판정 확인
        } // 검증 끝
        [Test] // 좌표 범위 경계 검증
        public void Contains_DoesNotOverflowAtCoordinateLimits() // 큰 좌표 차이 검사
        { // 검증 시작
            Assert.That(EncounterRange.Contains(int.MinValue, 0, int.MaxValue, 0), Is.False); // 오버플로 방지 확인
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
