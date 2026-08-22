using NUnit.Framework; // NUnit 테스트 기능 사용
using ProjectDelta.Domain; // 도메인 이동 규칙 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class GridMovementTests // 그리드 이동 규칙 테스트
    {
        [Test] // 시점 방향 변환 테스트
        public void GetFacingFromYaw_UsesNearestCardinalDirection() // 각도별 4방향 판정 검증
        {
            Assert.That(GridMovement.GetFacingFromYaw(0f), Is.EqualTo(CardinalDirection.North)); // 0도 북쪽 검증
            Assert.That(GridMovement.GetFacingFromYaw(44.9f), Is.EqualTo(CardinalDirection.North)); // 45도 직전 북쪽 검증
            Assert.That(GridMovement.GetFacingFromYaw(45f), Is.EqualTo(CardinalDirection.East)); // 45도 동쪽 검증
            Assert.That(GridMovement.GetFacingFromYaw(135f), Is.EqualTo(CardinalDirection.South)); // 135도 남쪽 검증
            Assert.That(GridMovement.GetFacingFromYaw(225f), Is.EqualTo(CardinalDirection.West)); // 225도 서쪽 검증
            Assert.That(GridMovement.GetFacingFromYaw(315f), Is.EqualTo(CardinalDirection.North)); // 315도 북쪽 검증
        }

        [Test] // 동쪽 전진 테스트
        public void Forward_WhenFacingEast_MovesPositiveX() // 동쪽 기준 W 이동 검증
        {
            GridPosition delta = GridMovement.GetMoveDelta(CardinalDirection.East, GridMoveInput.Forward); // 동쪽 전진 변화량 계산
            Assert.That(delta.X, Is.EqualTo(1)); // X 증가 검증
            Assert.That(delta.Z, Is.EqualTo(0)); // Z 유지 검증
        }

        [Test] // 동쪽 우측 이동 테스트
        public void Right_WhenFacingEast_MovesNegativeZ() // 동쪽 기준 D 이동 검증
        {
            GridPosition delta = GridMovement.GetMoveDelta(CardinalDirection.East, GridMoveInput.Right); // 동쪽 우측 변화량 계산
            Assert.That(delta.X, Is.EqualTo(0)); // X 유지 검증
            Assert.That(delta.Z, Is.EqualTo(-1)); // Z 감소 검증
        }

        [Test] // 방 경계 이동 거부 테스트
        public void TryGetTarget_RejectsPositionOutsideBounds() // 테스트 방 범위 밖 이동 검증
        {
            GridBounds bounds = new GridBounds(-2, 2, -2, 2); // 테스트 방 범위 생성
            GridPosition current = new GridPosition(2, 0); // 동쪽 끝 좌표 생성
            bool canMove = GridMovement.TryGetTarget(current, CardinalDirection.North, GridMoveInput.Right, bounds, out GridPosition target); // 동쪽 밖 이동 시도
            Assert.That(canMove, Is.False); // 이동 불가 검증
            Assert.That(target.X, Is.EqualTo(3)); // 계산된 목표 X 검증
            Assert.That(target.Z, Is.EqualTo(0)); // 계산된 목표 Z 검증
        }
    }
}
