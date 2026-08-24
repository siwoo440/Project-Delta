using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Encounter 범위 규칙 사용
using ProjectDelta.Domain; // GridPosition 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class EncounterRangeRuleTests
    {
        [Test]
        public void IsWithinRange_SamePosition_ReturnsTrue()
        {
            Assert.IsTrue(
                EncounterRangeRule.IsWithinRange(
                    GridPosition.Zero,
                    GridPosition.Zero)); // 같은 칸 Encounter 허용 확인
        }

        [TestCase(-1, -1)]
        [TestCase(0, -1)]
        [TestCase(1, -1)]
        [TestCase(-1, 0)]
        [TestCase(1, 0)]
        [TestCase(-1, 1)]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        public void IsWithinRange_EightAdjacentDirections_ReturnsTrue(
            int monsterX,
            int monsterZ)
        {
            Assert.IsTrue(
                EncounterRangeRule.IsWithinRange(
                    GridPosition.Zero,
                    new GridPosition(
                        monsterX,
                        monsterZ))); // 주변 8방향 1칸 포착 확인
        }

        [TestCase(-2, 0)]
        [TestCase(2, 0)]
        [TestCase(0, -2)]
        [TestCase(0, 2)]
        [TestCase(2, 2)]
        public void IsWithinRange_OutsideOneCell_ReturnsFalse(
            int monsterX,
            int monsterZ)
        {
            Assert.IsFalse(
                EncounterRangeRule.IsWithinRange(
                    GridPosition.Zero,
                    new GridPosition(
                        monsterX,
                        monsterZ))); // 1칸 초과 위치 거부 확인
        }

        [Test]
        public void IsWithinRange_NegativeCaptureRange_ReturnsFalse()
        {
            Assert.IsFalse(
                EncounterRangeRule.IsWithinRange(
                    GridPosition.Zero,
                    GridPosition.Zero,
                    -1)); // 음수 범위 거부 확인
        }
    }
}
