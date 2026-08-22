using NUnit.Framework; // NUnit 테스트 기능
using ProjectDelta.Domain; // 도메인 좌표 데이터

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class GridPositionTests // 그리드 좌표 테스트 모음
    {
        [Test] // 좌표 생성 테스트
        public void Constructor_StoresCoordinates() // 생성자 좌표 저장 검증
        {
            var position = new GridPosition(3, -2); // 테스트 좌표 생성

            Assert.That(position.X, Is.EqualTo(3)); // X 좌표 검증
            Assert.That(position.Z, Is.EqualTo(-2)); // Z 좌표 검증
        }

        [Test] // 플레이어 초기 좌표 테스트
        public void PlayerRunState_StartsAtGridOrigin() // 초기 원점 상태 검증
        {
            var player = new PlayerRunState(); // 플레이어 런타임 상태 생성

            Assert.That(player.CurrentGridPosition.X, Is.EqualTo(0)); // 초기 X 좌표 검증
            Assert.That(player.CurrentGridPosition.Z, Is.EqualTo(0)); // 초기 Z 좌표 검증
        }
    }
}
