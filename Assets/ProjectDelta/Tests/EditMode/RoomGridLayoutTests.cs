using NUnit.Framework; // NUnit 테스트 기능 사용
using ProjectDelta.Domain; // 도메인 통로 규칙 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class RoomGridLayoutTests // 방 통로 규칙 테스트
    {
        [Test] // 벽 양방향 차단 테스트
        public void Wall_BlocksBothDirections() // 벽이 양쪽 칸에서 이동을 막는지 확인
        {
            RoomGridLayout layout = new RoomGridLayout(); // 테스트 방 통로 데이터 생성
            GridPassage wall = GridPassage.CreateWall(); // 테스트 벽 생성
            layout.SetPassage(new GridPosition(0, 0), CardinalDirection.East, wall); // 동쪽 벽 등록
            Assert.IsFalse(layout.CanPass(new GridPosition(0, 0), CardinalDirection.East)); // 현재 칸 동쪽 차단 확인
            Assert.IsFalse(layout.CanPass(new GridPosition(1, 0), CardinalDirection.West)); // 인접 칸 서쪽 차단 확인
        }

        [Test] // 일반 문 열기 테스트
        public void UnlockedDoor_OpensWithoutKey() // 잠기지 않은 문이 열쇠 없이 열리는지 확인
        {
            PlayerRunState player = new PlayerRunState(); // 테스트 플레이어 생성
            GridPassage door = GridPassage.CreateDoor(false); // 일반 닫힌 문 생성
            DoorOpenResult result = door.TryOpenDoor(player); // 문 열기 시도
            Assert.AreEqual(DoorOpenResult.Opened, result); // 문 열기 성공 확인
            Assert.IsTrue(door.CanPass()); // 열린 문 통과 가능 확인
            Assert.AreEqual(0, player.KeyCount); // 열쇠 미소모 확인
        }

        [Test] // 열쇠 없는 잠긴 문 테스트
        public void LockedDoor_WithNoKey_RemainsClosed() // 열쇠가 없으면 잠긴 문이 열리지 않는지 확인
        {
            PlayerRunState player = new PlayerRunState(); // 열쇠 없는 테스트 플레이어 생성
            GridPassage door = GridPassage.CreateDoor(true); // 잠긴 문 생성
            DoorOpenResult result = door.TryOpenDoor(player); // 문 열기 시도
            Assert.AreEqual(DoorOpenResult.LockedNoKey, result); // 열쇠 부족 결과 확인
            Assert.IsFalse(door.CanPass()); // 닫힌 문 통과 불가 확인
            Assert.IsTrue(door.IsLocked); // 잠금 상태 유지 확인
        }

        [Test] // 열쇠 보유 잠긴 문 테스트
        public void LockedDoor_WithKey_ConsumesOneAndOpens() // 열쇠 한 개를 소모하고 잠긴 문이 열리는지 확인
        {
            PlayerRunState player = new PlayerRunState(); // 테스트 플레이어 생성
            player.KeyCount = 2; // 테스트 열쇠 두 개 지급
            GridPassage door = GridPassage.CreateDoor(true); // 잠긴 문 생성
            DoorOpenResult result = door.TryOpenDoor(player); // 문 열기 시도
            Assert.AreEqual(DoorOpenResult.Opened, result); // 문 열기 성공 확인
            Assert.AreEqual(1, player.KeyCount); // 열쇠 한 개 소모 확인
            Assert.IsFalse(door.IsLocked); // 잠금 해제 확인
            Assert.IsTrue(door.CanPass()); // 열린 문 통과 가능 확인
        }

        [Test] // 닫힌 문 이동 차단 테스트
        public void ClosedDoor_BlocksMovementUntilOpened() // 문이 열리기 전까지 통과가 막히는지 확인
        {
            RoomGridLayout layout = new RoomGridLayout(); // 테스트 방 통로 데이터 생성
            GridPassage door = GridPassage.CreateDoor(false); // 일반 닫힌 문 생성
            layout.SetPassage(new GridPosition(0, 0), CardinalDirection.North, door); // 북쪽 문 등록
            Assert.IsFalse(layout.CanPass(new GridPosition(0, 0), CardinalDirection.North)); // 닫힌 문 차단 확인
            door.TryOpenDoor(new PlayerRunState()); // 일반 문 열기
            Assert.IsTrue(layout.CanPass(new GridPosition(0, 0), CardinalDirection.North)); // 열린 문 통과 확인
        }

        [Test] // 기본 열린 통로 테스트
        public void UnregisteredPassage_IsOpenByDefault() // 별도 장애물이 없는 내부 통로가 열려 있는지 확인
        {
            RoomGridLayout layout = new RoomGridLayout(); // 빈 테스트 방 통로 데이터 생성
            Assert.IsTrue(layout.CanPass(new GridPosition(0, 0), CardinalDirection.South)); // 기본 통로 허용 확인
        }
    }
}
