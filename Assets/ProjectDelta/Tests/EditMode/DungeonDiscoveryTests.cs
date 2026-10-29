using System; // 잘못된 구성 예외
using NUnit.Framework; // 검증 도구
using ProjectDelta.Core; // 던전 지도 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class DungeonDiscoveryTests // 방 연결과 발견 정보 검증
    { // 클래스 시작
        [Test] // 현재 방 중심의 미니맵 범위 검증
        public void MiniMapWindow_ShowsOnlyFiveByFiveAroundCurrentRoom() // 다섯 칸 범위와 경계 확인
        { // 검증 시작
            Assert.That(MiniMapWindow.Contains(2, -2, 0, 0), Is.True); // 양쪽 끝 방 표시
            Assert.That(MiniMapWindow.Contains(3, 0, 0, 0), Is.False); // 오른쪽 범위 밖 제외
            Assert.That(MiniMapWindow.Contains(0, -3, 0, 0), Is.False); // 아래쪽 범위 밖 제외
            Assert.That(MiniMapWindow.Contains(3, 0, 1, 0), Is.True); // 현재 방 이동 후 새 범위 표시
            Assert.That(MiniMapWindow.Contains(int.MinValue, 0, int.MaxValue, 0), Is.False); // 좌표 차이의 정수 범위 보호
        } // 검증 끝
        private static DungeonLayout MakeLayout() // 시험 방과 비밀 방 구성
        { // 구성 시작
            var rooms = new[] // 방 정의 목록
            { // 목록 시작
                new DungeonRoom(0, 0), // 시작 방
                new DungeonRoom(1, 0), // 오른쪽 방
                new DungeonRoom(1, 1), // 위쪽 방
                new DungeonRoom(0, -1, true) // 시작 방 아래 비밀 방
            }; // 목록 끝
            var doors = new[] // 양방향 문 목록
            { // 목록 시작
                new RoomDoor(0, 0, 5, 0, MoveDirection.Right, 1, 0, -5, 0), // 첫 방 오른쪽 문
                new RoomDoor(1, 0, -5, 0, MoveDirection.Left, 0, 0, 5, 0), // 두 번째 방 왼쪽 문
                new RoomDoor(1, 0, 2, 3, MoveDirection.Up, 1, 1, 2, -3), // 두 번째 방 위쪽 문
                new RoomDoor(1, 1, 2, -3, MoveDirection.Down, 1, 0, 2, 3), // 세 번째 방 아래쪽 문
                new RoomDoor(0, 0, 1, -3, MoveDirection.Down, 0, -1, 1, 3), // 시작 방의 비밀 출구
                new RoomDoor(0, -1, 1, 3, MoveDirection.Up, 0, 0, 1, -3) // 비밀 방의 복귀 출구
            }; // 목록 끝
            return new DungeonLayout(rooms, doors); // 순수 방 구조 생성
        } // 구성 끝
        [Test] // 개별 검증 표시
        public void Start_RevealsOnlyVisitedAndConnectedNonsecretRooms() // 시작 지도 공개 범위
        { // 검증 시작
            var map = new DungeonDiscovery(MakeLayout(), 0, 0); // 새 탐험 지도
            Assert.That(map.GetVisibility(0, 0), Is.EqualTo(RoomVisibility.Visited)); // 시작 방 방문 확인
            Assert.That(map.GetVisibility(1, 0), Is.EqualTo(RoomVisibility.Connected)); // 연결만 확인한 방
            Assert.That(map.GetVisibility(1, 1), Is.EqualTo(RoomVisibility.Hidden)); // 아직 연결을 모르는 방
            Assert.That(map.GetVisibility(0, -1), Is.EqualTo(RoomVisibility.Hidden)); // 연결된 비밀 방 가림
            Assert.That(map.IsConnectionVisible(0, 0, 0, -1), Is.False); // 비밀 연결선 가림
            Assert.That(map.IsConnectionVisible(0, 0, 1, 0), Is.True); // 확인된 일반 연결선 표시
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void Visit_RevealsNeighborsAndRetainsPriorVisits() // 방 방문과 연결 공개
        { // 검증 시작
            var map = new DungeonDiscovery(MakeLayout(), 0, 0); // 새 탐험 지도
            Assert.That(map.TryVisit(1, 0), Is.True); // 연결된 방 방문
            Assert.That(map.CurrentX, Is.EqualTo(1)); // 현재 방 좌표 갱신
            Assert.That(map.GetVisibility(0, 0), Is.EqualTo(RoomVisibility.Visited)); // 기존 방문 유지
            Assert.That(map.GetVisibility(1, 0), Is.EqualTo(RoomVisibility.Visited)); // 새 방 방문 표시
            Assert.That(map.GetVisibility(1, 1), Is.EqualTo(RoomVisibility.Connected)); // 새 방의 연결 공개
            Assert.That(map.IsConnectionVisible(1, 0, 1, 1), Is.True); // 새 연결선 표시
            Assert.That(map.TryVisit(1, 1), Is.True); // 세 번째 방 방문
            Assert.That(map.CurrentY, Is.EqualTo(1)); // 새 방의 세로 좌표
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void NonAdjacentAndUnknownVisit_DoNotChangeMap() // 유효하지 않은 방문 차단
        { // 검증 시작
            var map = new DungeonDiscovery(MakeLayout(), 0, 0); // 새 탐험 지도
            Assert.That(map.TryVisit(1, 1), Is.False); // 직접 연결되지 않은 방 차단
            Assert.That(map.TryVisit(50, 50), Is.False); // 존재하지 않는 방 차단
            Assert.That(map.CurrentX, Is.EqualTo(0)); // 시작 방 위치 유지
            Assert.That(map.GetVisibility(1, 1), Is.EqualTo(RoomVisibility.Hidden)); // 미발견 방 유지
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void SecretRoom_VisibleOnlyAfterVisit() // 비밀 방과 비밀 연결 정보 보호
        { // 검증 시작
            var map = new DungeonDiscovery(MakeLayout(), 0, 0); // 새 탐험 지도
            Assert.That(map.GetVisibility(0, -1), Is.EqualTo(RoomVisibility.Hidden)); // 방문 전 비밀 방 가림
            Assert.That(map.TryVisit(0, -1), Is.True); // 실제 비밀 방 진입 모사
            Assert.That(map.GetVisibility(0, -1), Is.EqualTo(RoomVisibility.Visited)); // 방문 후 비밀 방 표시
            Assert.That(map.IsConnectionVisible(0, 0, 0, -1), Is.True); // 방문 후 연결 표시
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void NewDiscovery_ResetsPreviousVisits() // 새 게임의 방문 기록 초기화
        { // 검증 시작
            var layout = MakeLayout(); // 공통 고정 방 구조
            var previous = new DungeonDiscovery(layout, 0, 0); // 이전 탐험 기록
            Assert.That(previous.TryVisit(1, 0), Is.True); // 이전 게임의 두 번째 방 방문
            var fresh = new DungeonDiscovery(layout, 0, 0); // 새 게임 기록 생성
            Assert.That(fresh.GetVisibility(1, 0), Is.EqualTo(RoomVisibility.Connected)); // 두 번째 방의 방문 기록 초기화
            Assert.That(fresh.GetVisibility(1, 1), Is.EqualTo(RoomVisibility.Hidden)); // 세 번째 방 다시 가림
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void SecretConnectionBetweenOrdinaryRooms_StaysHiddenUntilTraversed() // 일반 방 사이의 미발견 비밀 연결 보호
        { // 검증 시작
            var rooms = new[] // 일반 방 세 개
            { // 방 목록 시작
                new DungeonRoom(0, 0), // 시작 방
                new DungeonRoom(1, 0), // 시작 방 오른쪽 방
                new DungeonRoom(0, 1) // 시작 방 위쪽 방
            }; // 방 목록 끝
            var doors = new[] // 일반 연결과 비밀 연결
            { // 출구 목록 시작
                new RoomDoor(0, 0, 5, 0, MoveDirection.Right, 1, 0, -5, 0), // 일반 오른쪽 출구
                new RoomDoor(1, 0, -5, 0, MoveDirection.Left, 0, 0, 5, 0), // 일반 왼쪽 출구
                new RoomDoor(0, 0, 0, 3, MoveDirection.Up, 0, 1, 0, -3, true), // 일반 방으로 향하는 비밀 출구
                new RoomDoor(0, 1, 0, -3, MoveDirection.Down, 0, 0, 0, 3, true) // 반대쪽 비밀 출구
            }; // 출구 목록 끝
            var map = new DungeonDiscovery(new DungeonLayout(rooms, doors), 0, 0); // 새 탐험 지도
            Assert.That(map.GetVisibility(0, 1), Is.EqualTo(RoomVisibility.Hidden)); // 비밀 연결 너머 방 가림
            Assert.That(map.IsConnectionVisible(0, 0, 0, 1), Is.False); // 비밀 연결선 가림
            Assert.That(map.TryVisit(0, 1), Is.True); // 실제 비밀 연결 통과 모사
            Assert.That(map.GetVisibility(0, 1), Is.EqualTo(RoomVisibility.Visited)); // 직접 방문한 방 표시
            Assert.That(map.IsConnectionVisible(0, 0, 0, 1), Is.True); // 통과하여 발견한 연결 표시
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void Layout_RejectsMismatchedSecretDirections() // 비밀 연결 양방향 불일치 차단
        { // 검증 시작
            var rooms = new[] // 두 일반 방
            { // 방 목록 시작
                new DungeonRoom(0, 0), // 첫 방
                new DungeonRoom(1, 0) // 두 번째 방
            }; // 방 목록 끝
            var doors = new[] // 서로 다른 비밀 여부의 출구
            { // 출구 목록 시작
                new RoomDoor(0, 0, 5, 0, MoveDirection.Right, 1, 0, -5, 0, true), // 비밀 출구
                new RoomDoor(1, 0, -5, 0, MoveDirection.Left, 0, 0, 5, 0) // 일반 복귀 출구
            }; // 출구 목록 끝
            Assert.Throws<ArgumentException>(() => new DungeonLayout(rooms, doors)); // 비밀 상태 불일치 거부
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void SecretConnection_RemainsHiddenWhenBothRoomsAreOtherwiseKnown() // 이미 알려진 방 사이의 비밀 통로 보호
        { // 검증 시작
            var rooms = new[] // 사각형 시험 방 목록
            { // 방 목록 시작
                new DungeonRoom(0, 0), // 시작 방
                new DungeonRoom(1, 0), // 오른쪽 방
                new DungeonRoom(0, 1), // 위쪽 방
                new DungeonRoom(1, 1) // 오른쪽 위 방
            }; // 방 목록 끝
            var doors = new[] // 일반 연결과 비밀 연결
            { // 출구 목록 시작
                new RoomDoor(0, 0, 5, 0, MoveDirection.Right, 1, 0, -5, 0), // 시작 방 오른쪽 문
                new RoomDoor(1, 0, -5, 0, MoveDirection.Left, 0, 0, 5, 0), // 오른쪽 방 복귀 문
                new RoomDoor(0, 0, 0, 3, MoveDirection.Up, 0, 1, 0, -3), // 시작 방 위쪽 문
                new RoomDoor(0, 1, 0, -3, MoveDirection.Down, 0, 0, 0, 3), // 위쪽 방 복귀 문
                new RoomDoor(1, 0, 0, 3, MoveDirection.Up, 1, 1, 0, -3), // 오른쪽 방 위쪽 문
                new RoomDoor(1, 1, 0, -3, MoveDirection.Down, 1, 0, 0, 3), // 오른쪽 위 방 복귀 문
                new RoomDoor(0, 1, 5, 0, MoveDirection.Right, 1, 1, -5, 0, true), // 위쪽 방의 비밀 문
                new RoomDoor(1, 1, -5, 0, MoveDirection.Left, 0, 1, 5, 0, true) // 비밀 문 반대편
            }; // 출구 목록 끝
            var map = new DungeonDiscovery(new DungeonLayout(rooms, doors), 0, 0); // 새 탐험 지도
            Assert.That(map.TryVisit(1, 0), Is.True); // 오른쪽 방 방문
            Assert.That(map.TryVisit(1, 1), Is.True); // 오른쪽 위 방 방문
            Assert.That(map.GetVisibility(0, 1), Is.EqualTo(RoomVisibility.Connected)); // 다른 일반 문으로 알게 된 방
            Assert.That(map.IsConnectionVisible(1, 1, 0, 1), Is.False); // 양쪽 방을 알아도 비밀 연결 가림
            Assert.That(map.TryVisit(0, 1), Is.True); // 비밀 연결 통과
            Assert.That(map.IsConnectionVisible(1, 1, 0, 1), Is.True); // 실제 통과 후 연결 표시
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void Layout_RejectsDuplicateRoomCoordinates() // 중복 방 좌표 차단
        { // 검증 시작
            var rooms = new[] // 중복된 시험 방 목록
            { // 목록 시작
                new DungeonRoom(0, 0), // 첫 방
                new DungeonRoom(0, 0) // 같은 좌표의 두 번째 방
            }; // 목록 끝
            Assert.Throws<ArgumentException>(() => new DungeonLayout(rooms, new RoomDoor[0])); // 중복 좌표 거부 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void Layout_RejectsMissingReverseDoor() // 양방향 연결 불일치 차단
        { // 검증 시작
            var rooms = new[] // 연결 시험 방 목록
            { // 목록 시작
                new DungeonRoom(0, 0), // 첫 방
                new DungeonRoom(1, 0) // 두 번째 방
            }; // 목록 끝
            var doors = new[] // 역방향 문이 없는 목록
            { // 목록 시작
                new RoomDoor(0, 0, 5, 0, MoveDirection.Right, 1, 0, -5, 0) // 한쪽 방향 문
            }; // 목록 끝
            Assert.Throws<ArgumentException>(() => new DungeonLayout(rooms, doors)); // 누락된 역방향 문 거부
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
