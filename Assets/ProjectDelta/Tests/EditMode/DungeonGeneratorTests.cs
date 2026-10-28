using System; // 난수와 수학 도구
using System.Collections.Generic; // 경로와 방 집합
using System.Linq; // 방 종류와 구조 비교
using NUnit.Framework; // 검증 도구
using ProjectDelta.Core; // 던전 생성 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class DungeonGeneratorTests // 1층 방 생성 검증
    { // 클래스 시작
        [Test] // 개별 검증 표시
        public void ManySeeds_CreateValidReachableFloor() // 여러 시드의 방 수와 경로 검증
        { // 검증 시작
            for (var seed = 0; seed < 200; seed++) // 서로 다른 시드 순회
            { // 시드 순회 시작
                AssertValid(DungeonGenerator.Generate(seed)); // 생성된 각 층의 규칙 확인
            } // 시드 순회 끝
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void SameSeed_RecreatesRoomsDoorsAndRoles() // 같은 시드의 완전한 구조 재현
        { // 검증 시작
            var first = Signature(DungeonGenerator.Generate(12345)); // 첫 번째 생성 결과
            var second = Signature(DungeonGenerator.Generate(12345)); // 같은 시드의 두 번째 결과
            Assert.That(first, Is.EqualTo(second)); // 방과 문과 역할 재현 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void DifferentSeeds_ProduceDifferentLayouts() // 시드에 따른 배치 변화
        { // 검증 시작
            var signatures = new HashSet<string>(); // 서로 다른 구조 보관
            for (var seed = 0; seed < 20; seed++) // 여러 시드 순회
            { // 시드 순회 시작
                signatures.Add(Signature(DungeonGenerator.Generate(seed))); // 실제 구조 기록
            } // 시드 순회 끝
            Assert.That(signatures.Count, Is.GreaterThan(1)); // 고정 구조 반복 방지
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void ZeroAttempts_UsesValidDeterministicFallback() // 시도 제한 시 대체 구조 검증
        { // 검증 시작
            var fallback = DungeonGenerator.Generate(17, 0); // 생성 시도 없이 대체 구조 요청
            AssertValid(fallback); // 대체 구조의 모든 규칙 확인
            Assert.That(Signature(fallback), Is.EqualTo(Signature(DungeonGenerator.Generate(17, 0)))); // 대체 구조 재현 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void NegativeAttemptLimit_IsRejected() // 잘못된 재시도 제한 차단
        { // 검증 시작
            Assert.Throws<ArgumentOutOfRangeException>(() => DungeonGenerator.Generate(0, -1)); // 음수 제한 거절
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void ZeroSeed_HasKnownInitialRouteForIntegrationTests() // 실제 입력 시험의 재현 가능한 첫 연결
        { // 검증 시작
            var floor = DungeonGenerator.Generate(0); // 검증용 시드의 던전
            Assert.That(floor.HasConnection(0, 0, 1, 0), Is.True); // 첫 번째 오른쪽 문 연결
            Assert.That(floor.HasConnection(1, 0, 1, 1), Is.True); // 두 번째 방의 위쪽 문 연결
            Assert.That(floor.Doors.Count(door => door.RoomX == 0 && door.RoomY == 0), Is.EqualTo(1)); // 시작 방의 단일 출구
        } // 검증 끝
        private static void AssertValid(DungeonLayout floor) // 층 구조의 게임 규칙 검증
        { // 검증 시작
            Assert.That(floor, Is.Not.Null); // 실제 구조 생성 확인
            Assert.That(floor.Rooms.Count, Is.InRange(11, 13)); // 일반 방과 세 필수 방 합계
            Assert.That(floor.Rooms.Count(room => room.Kind == RoomKind.Normal), Is.InRange(8, 10)); // 일반 방 수
            Assert.That(floor.Rooms.Count(room => room.Kind == RoomKind.Start), Is.EqualTo(1)); // 시작 방 한 개
            Assert.That(floor.Rooms.Count(room => room.Kind == RoomKind.Stairs), Is.EqualTo(1)); // 계단 방 한 개
            Assert.That(floor.Rooms.Count(room => room.Kind == RoomKind.Elite), Is.EqualTo(1)); // 상위 개체 방 한 개
            var start = floor.Rooms.Single(room => room.Kind == RoomKind.Start); // 시작 방 조회
            var stairs = floor.Rooms.Single(room => room.Kind == RoomKind.Stairs); // 계단 방 조회
            Assert.That(start.X, Is.EqualTo(0)); // 시작 방 가로 기준
            Assert.That(start.Y, Is.EqualTo(0)); // 시작 방 세로 기준
            Assert.That(Math.Abs(stairs.X - start.X) + Math.Abs(stairs.Y - start.Y), Is.GreaterThanOrEqualTo(5)); // 지도상 계단 거리
            var distances = new Dictionary<string, int>(); // 시작 방으로부터의 최단 거리
            var queue = new Queue<DungeonRoom>(); // 확인할 방 대기열
            distances[Key(start.X, start.Y)] = 0; // 시작 방 거리 설정
            queue.Enqueue(start); // 시작 방 탐색 등록
            while (queue.Count > 0) // 도달 가능한 방 순회
            { // 경로 탐색 시작
                var room = queue.Dequeue(); // 다음 방 조회
                foreach (var door in floor.Doors) // 실제 출구 순회
                { // 출구 검사 시작
                    if (door.RoomX != room.X || door.RoomY != room.Y) // 현재 방의 출구 검사
                    { // 다른 방 조건 시작
                        continue; // 현재 방과 관계없는 출구 제외
                    } // 다른 방 조건 끝
                    var key = Key(door.TargetRoomX, door.TargetRoomY); // 목적 방 식별자
                    if (!distances.ContainsKey(key)) // 아직 방문하지 않은 방 검사
                    { // 새 방 조건 시작
                        distances.Add(key, distances[Key(room.X, room.Y)] + 1); // 최단 연결 거리 설정
                        queue.Enqueue(floor.GetRoom(door.TargetRoomX, door.TargetRoomY)); // 목적 방 탐색 예약
                    } // 새 방 조건 끝
                } // 출구 검사 끝
            } // 경로 탐색 끝
            Assert.That(distances.Count, Is.EqualTo(floor.Rooms.Count)); // 모든 방의 도달 가능성
            Assert.That(distances[Key(stairs.X, stairs.Y)], Is.GreaterThanOrEqualTo(5)); // 계단의 최단 경로 거리
            Assert.That(floor.Doors.Count, Is.EqualTo((floor.Rooms.Count - 1) * 2)); // 반복 경로 없는 양방향 연결
        } // 검증 끝
        private static string Key(int x, int y) // 방 좌표의 검증용 식별자
        { // 변환 시작
            return x + "," + y; // 좌표 문자열 반환
        } // 변환 끝
        private static string Signature(DungeonLayout floor) // 방과 출구의 재현 비교
        { // 비교 준비 시작
            Assert.That(floor, Is.Not.Null); // 실제 생성 결과 확인
            var rooms = floor.Rooms.Select(room => room.X + "," + room.Y + ":" + room.Kind).OrderBy(value => value); // 좌표와 역할 정렬
            var doors = floor.Doors.Select(door => door.RoomX + "," + door.RoomY + ":" + door.Direction + ":" + door.TargetRoomX + "," + door.TargetRoomY).OrderBy(value => value); // 출구 연결 정렬
            return string.Join("|", rooms) + ";" + string.Join("|", doors); // 구조 비교 문자열
        } // 비교 준비 끝
    } // 클래스 끝
} // 이름 공간 끝
