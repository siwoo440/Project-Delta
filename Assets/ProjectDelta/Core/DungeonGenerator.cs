using System; // 난수와 구성 오류
using System.Collections.Generic; // 방과 경로 목록
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class DungeonGenerator // 시드 기반 1층 생성
    { // 클래스 시작
        private sealed class Cell // 생성 중인 방 좌표와 연결
        { // 클래스 시작
            public readonly int X; // 방 가로 좌표
            public readonly int Y; // 방 세로 좌표
            public readonly int Parent; // 연결된 이전 방 번호
            public Cell(int x, int y, int parent) // 생성 방 정보 보관
            { // 생성 시작
                X = x; // 가로 좌표 설정
                Y = y; // 세로 좌표 설정
                Parent = parent; // 이전 방 번호 설정
            } // 생성 끝
        } // 클래스 끝
        private static readonly MoveDirection[] Directions = // 방 사이의 네 방향
        { // 방향 목록 시작
            MoveDirection.Right, // 오른쪽 방향
            MoveDirection.Up, // 위쪽 방향
            MoveDirection.Left, // 왼쪽 방향
            MoveDirection.Down // 아래쪽 방향
        }; // 방향 목록 끝
        public static DungeonLayout Generate(int seed, int maxAttempts = 32) // 시드와 생성 시도 제한
        { // 생성 시작
            if (maxAttempts < 0) // 잘못된 재시도 제한 검사
            { // 음수 조건 시작
                throw new ArgumentOutOfRangeException(nameof(maxAttempts)); // 잘못된 제한 거절
            } // 음수 조건 끝
            for (var attempt = 0; attempt < maxAttempts; attempt++) // 제한된 생성 시도
            { // 생성 반복 시작
                var layout = TryGenerate(seed, attempt); // 현재 시도의 방 구조 생성
                if (layout != null && IsValid(layout)) // 생성 결과의 규칙 검사
                { // 유효한 결과 조건 시작
                    return layout; // 검증된 던전 전달
                } // 유효한 결과 조건 끝
            } // 생성 반복 끝
            var fallback = BuildFallback(); // 검증 가능한 고정 대체 구조
            if (!IsValid(fallback)) // 대체 구조의 실제 규칙 검사
            { // 대체 구조 오류 조건 시작
                throw new InvalidOperationException("Fallback dungeon does not satisfy the floor rules."); // 잘못된 대체 구조 차단
            } // 대체 구조 오류 조건 끝
            return fallback; // 검증된 대체 던전 전달
        } // 생성 끝
        public static DungeonLayout GenerateWithSecret(int seed) // 기존 구조에 시험 비밀방 한 곳 추가
        { // 생성 시작
            var baseLayout = Generate(seed); // 기존 생성 규칙의 기본 구조
            foreach (var room in baseLayout.Rooms) // 기존 방 순서 순회
            { // 후보 방 검사 시작
                if (room.Kind != RoomKind.Normal || room.IsSecret) // 일반 방 후보 검사
                { // 다른 방 조건 시작
                    continue; // 필수 방 제외
                } // 다른 방 조건 끝
                foreach (var direction in Directions) // 네 방향의 빈 좌표 검사
                { // 방향 검사 시작
                    var offset = Offset(direction); // 방향의 좌표 변화
                    var x = room.X + offset.Item1; // 비밀방 가로 좌표
                    var y = room.Y + offset.Item2; // 비밀방 세로 좌표
                    if (baseLayout.GetRoom(x, y) != null) // 기존 방의 좌표 중복 검사
                    { // 중복 조건 시작
                        continue; // 이미 사용하는 위치 제외
                    } // 중복 조건 끝
                    var opposite = Opposite(direction); // 비밀방 복귀 방향
                    var from = Approach(direction); // 일반 방의 비밀벽 접근 칸
                    var to = Approach(opposite); // 비밀방의 복귀 접근 칸
                    var rooms = new List<DungeonRoom>(baseLayout.Rooms); // 기존 방 정의 복사
                    var doors = new List<RoomDoor>(baseLayout.Doors); // 기존 문 정의 복사
                    rooms.Add(new DungeonRoom(x, y, true)); // 시험 비밀방 추가
                    doors.Add(new RoomDoor(room.X, room.Y, from.Item1, from.Item2, direction, x, y, to.Item1, to.Item2, true)); // 비밀방으로 가는 벽
                    doors.Add(new RoomDoor(x, y, to.Item1, to.Item2, opposite, room.X, room.Y, from.Item1, from.Item2, true)); // 비밀방에서 돌아가는 벽
                    return new DungeonLayout(rooms.ToArray(), doors.ToArray()); // 검증된 양방향 구조
                } // 방향 검사 끝
            } // 후보 방 검사 끝
            throw new InvalidOperationException("No normal room can host the secret room."); // 비밀방 배치 불가 안내
        } // 생성 끝
        public static DungeonLayout GenerateVersioned(int seed, int version) // 저장된 생성 규칙의 구조 복원
        { // 생성 시작
            if (version == 1) // 기존 생성 버전 검사
            { // 기존 버전 시작
                return Generate(seed); // 비밀방 없는 기존 구조
            } // 기존 버전 끝
            if (version == 2) // 비밀방 생성 버전 검사
            { // 새 버전 시작
                return GenerateWithSecret(seed); // 시험 비밀방 포함 구조
            } // 새 버전 끝
            throw new ArgumentOutOfRangeException(nameof(version)); // 알 수 없는 생성 규칙 거절
        } // 생성 끝
        private static DungeonLayout TryGenerate(int seed, int attempt) // 주 경로와 갈림길 생성
        { // 생성 시작
            var random = new Random(unchecked(seed ^ (attempt * 7919))); // 시드와 시도별 난수 생성
            var cells = new List<Cell>(); // 생성할 방 목록
            cells.Add(new Cell(0, 0, -1)); // 시작 방 배치
            var first = (int)((uint)seed % 4u); // 시드에 따른 첫 방향
            var turn = ((seed >> 2) & 1) == 0 ? 1 : 3; // 시드에 따른 다음 회전
            AddNeighbor(cells, 0, Directions[first]); // 첫 번째 주 경로
            AddNeighbor(cells, 1, Directions[(first + turn) % 4]); // 두 번째 주 경로
            for (var step = 2; step < 6; step++) // 최소 여섯 연결의 주 경로 생성
            { // 주 경로 반복 시작
                var options = AvailableDirections(cells, cells.Count - 1); // 마지막 방의 빈 이웃 조회
                if (options.Count == 0) // 주 경로 생성 실패 검사
                { // 막힘 조건 시작
                    return null; // 다음 생성 시도 요청
                } // 막힘 조건 끝
                AddNeighbor(cells, cells.Count - 1, options[random.Next(options.Count)]); // 빈 이웃으로 주 경로 연장
            } // 주 경로 반복 끝
            var stairs = cells[6]; // 주 경로 끝의 계단 방
            if (Math.Abs(stairs.X) + Math.Abs(stairs.Y) < 5) // 지도 좌표상의 계단 거리 검사
            { // 거리 부족 조건 시작
                return null; // 다른 경로로 재시도
            } // 거리 부족 조건 끝
            var roomCount = random.Next(11, 14); // 일반 방과 필수 방의 전체 수
            while (cells.Count < roomCount) // 목표 방 수까지 갈림길 생성
            { // 갈림길 반복 시작
                var candidates = new List<int>(); // 확장 가능한 방 번호 목록
                for (var index = 1; index < cells.Count; index++) // 시작 방을 제외한 방 순회
                { // 확장 후보 검사 시작
                    if (AvailableDirections(cells, index).Count > 0) // 빈 이웃 존재 검사
                    { // 확장 가능 조건 시작
                        candidates.Add(index); // 갈림길 후보 추가
                    } // 확장 가능 조건 끝
                } // 확장 후보 검사 끝
                if (candidates.Count == 0) // 추가 방 배치 실패 검사
                { // 후보 없음 조건 시작
                    return null; // 다른 구조로 재시도
                } // 후보 없음 조건 끝
                var parent = candidates[random.Next(candidates.Count)]; // 갈림길의 기존 방 선택
                var directions = AvailableDirections(cells, parent); // 선택한 방의 빈 이웃
                AddNeighbor(cells, parent, directions[random.Next(directions.Count)]); // 연결된 일반 방 추가
            } // 갈림길 반복 끝
            var elite = random.Next(1, cells.Count); // 상위 개체 방 후보 선택
            if (elite == 6) // 계단 방과 중복 검사
            { // 중복 조건 시작
                elite = 1; // 계단과 다른 방 선택
            } // 중복 조건 끝
            return BuildLayout(cells, 6, elite); // 방 종류와 양방향 출구 확정
        } // 생성 끝
        private static DungeonLayout BuildFallback() // 재시도 실패 시 고정 대체 던전
        { // 대체 생성 시작
            var cells = new List<Cell>(); // 대체 방 목록
            cells.Add(new Cell(0, 0, -1)); // 시작 방 배치
            AddNeighbor(cells, 0, MoveDirection.Right); // 첫 번째 주 경로
            for (var index = 1; index < 6; index++) // 충분히 먼 계단까지 경로 생성
            { // 주 경로 반복 시작
                AddNeighbor(cells, index, MoveDirection.Up); // 위쪽으로 주 경로 연장
            } // 주 경로 반복 끝
            for (var index = 1; index <= 4; index++) // 필수 방 외의 갈림길 생성
            { // 갈림길 반복 시작
                AddNeighbor(cells, index, MoveDirection.Right); // 오른쪽 가지 방 추가
            } // 갈림길 반복 끝
            return BuildLayout(cells, 6, 1); // 계단과 상위 개체 방 배치
        } // 대체 생성 끝
        private static DungeonLayout BuildLayout(List<Cell> cells, int stairs, int elite) // 방 데이터와 양방향 문 생성
        { // 구성 시작
            var rooms = new DungeonRoom[cells.Count]; // 순수 방 정의 배열
            var doors = new List<RoomDoor>(); // 양방향 출구 목록
            for (var index = 0; index < cells.Count; index++) // 생성된 방 순회
            { // 방 순회 시작
                var cell = cells[index]; // 현재 방 좌표 조회
                var kind = index == 0 ? RoomKind.Start : index == stairs ? RoomKind.Stairs : index == elite ? RoomKind.Elite : RoomKind.Normal; // 현재 방의 역할 선택
                rooms[index] = new DungeonRoom(cell.X, cell.Y, false, kind); // 방 좌표와 역할 저장
                if (cell.Parent < 0) // 시작 방의 이전 연결 검사
                { // 시작 방 조건 시작
                    continue; // 시작 방에는 이전 방 없음
                } // 시작 방 조건 끝
                var parent = cells[cell.Parent]; // 실제 연결된 이전 방 조회
                var direction = DirectionBetween(parent, cell); // 이전 방에서 현재 방으로 향하는 방향
                var opposite = Opposite(direction); // 현재 방에서 이전 방으로 향하는 방향
                var from = Approach(direction); // 이전 방의 문 앞 칸
                var to = Approach(opposite); // 현재 방의 문 앞 칸
                doors.Add(new RoomDoor(parent.X, parent.Y, from.Item1, from.Item2, direction, cell.X, cell.Y, to.Item1, to.Item2)); // 앞으로 이동하는 문
                doors.Add(new RoomDoor(cell.X, cell.Y, to.Item1, to.Item2, opposite, parent.X, parent.Y, from.Item1, from.Item2)); // 돌아가는 반대 문
            } // 방 순회 끝
            return new DungeonLayout(rooms, doors.ToArray()); // 실제 출구가 검증된 방 구조
        } // 구성 끝
        private static void AddNeighbor(List<Cell> cells, int parent, MoveDirection direction) // 기존 방 옆의 새 방 배치
        { // 배치 시작
            var source = cells[parent]; // 출발 방 조회
            var offset = Offset(direction); // 방향별 좌표 차이
            cells.Add(new Cell(source.X + offset.Item1, source.Y + offset.Item2, parent)); // 연결된 새 방 추가
        } // 배치 끝
        private static List<MoveDirection> AvailableDirections(List<Cell> cells, int parent) // 비어 있는 인접 위치 조회
        { // 조회 시작
            var available = new List<MoveDirection>(); // 이동 가능한 방향 목록
            foreach (var direction in Directions) // 네 방향 순회
            { // 방향 순회 시작
                var offset = Offset(direction); // 방향별 좌표 차이
                var x = cells[parent].X + offset.Item1; // 후보 가로 좌표
                var y = cells[parent].Y + offset.Item2; // 후보 세로 좌표
                var occupied = false; // 중복 방 여부 초기화
                foreach (var cell in cells) // 기존 방 좌표 순회
                { // 방 검사 시작
                    if (cell.X == x && cell.Y == y) // 좌표 중복 검사
                    { // 중복 조건 시작
                        occupied = true; // 이미 방이 존재하는 위치
                        break; // 추가 좌표 검사 종료
                    } // 중복 조건 끝
                } // 방 검사 끝
                if (!occupied) // 비어 있는 위치 검사
                { // 빈 위치 조건 시작
                    available.Add(direction); // 생성 가능한 방향 등록
                } // 빈 위치 조건 끝
            } // 방향 순회 끝
            return available; // 비어 있는 방향 목록 전달
        } // 조회 끝
        private static MoveDirection DirectionBetween(Cell from, Cell to) // 인접 방 사이 방향 계산
        { // 계산 시작
            if (to.X > from.X) // 오른쪽 이웃 검사
            { // 오른쪽 조건 시작
                return MoveDirection.Right; // 오른쪽 방향
            } // 오른쪽 조건 끝
            if (to.X < from.X) // 왼쪽 이웃 검사
            { // 왼쪽 조건 시작
                return MoveDirection.Left; // 왼쪽 방향
            } // 왼쪽 조건 끝
            return to.Y > from.Y ? MoveDirection.Up : MoveDirection.Down; // 위쪽 또는 아래쪽 방향
        } // 계산 끝
        private static MoveDirection Opposite(MoveDirection direction) // 출구의 반대 방향
        { // 변환 시작
            switch (direction) // 방향 선택
            { // 방향 선택 시작
                case MoveDirection.Right: // 오른쪽 방향
                    return MoveDirection.Left; // 왼쪽 반대 방향
                case MoveDirection.Up: // 위쪽 방향
                    return MoveDirection.Down; // 아래쪽 반대 방향
                case MoveDirection.Left: // 왼쪽 방향
                    return MoveDirection.Right; // 오른쪽 반대 방향
                default: // 아래쪽 방향
                    return MoveDirection.Up; // 위쪽 반대 방향
            } // 방향 선택 끝
        } // 변환 끝
        private static Tuple<int, int> Offset(MoveDirection direction) // 방 좌표의 방향별 변화
        { // 변환 시작
            switch (direction) // 방향 선택
            { // 방향 선택 시작
                case MoveDirection.Right: // 오른쪽 방향
                    return Tuple.Create(1, 0); // 가로 좌표 증가
                case MoveDirection.Up: // 위쪽 방향
                    return Tuple.Create(0, 1); // 세로 좌표 증가
                case MoveDirection.Left: // 왼쪽 방향
                    return Tuple.Create(-1, 0); // 가로 좌표 감소
                default: // 아래쪽 방향
                    return Tuple.Create(0, -1); // 세로 좌표 감소
            } // 방향 선택 끝
        } // 변환 끝
        private static Tuple<int, int> Approach(MoveDirection direction) // 방 내부의 문 앞 칸
        { // 조회 시작
            switch (direction) // 문 방향 선택
            { // 방향 선택 시작
                case MoveDirection.Right: // 오른쪽 문
                    return Tuple.Create(5, 0); // 오른쪽 문 앞 칸
                case MoveDirection.Up: // 위쪽 문
                    return Tuple.Create(2, 3); // 위쪽 문 앞 칸
                case MoveDirection.Left: // 왼쪽 문
                    return Tuple.Create(-5, 0); // 왼쪽 문 앞 칸
                default: // 아래쪽 문
                    return Tuple.Create(2, -3); // 아래쪽 문 앞 칸
            } // 방향 선택 끝
        } // 조회 끝
        public static bool IsValid(DungeonLayout layout) // 생성 결과의 독립적인 게임 규칙 검사
        { // 검사 시작
            if (layout == null || layout.Rooms.Count < 11 || layout.Rooms.Count > 13 || layout.Doors.Count != (layout.Rooms.Count - 1) * 2) // 방 수와 양방향 연결 수 검사
            { // 수량 불일치 조건 시작
                return false; // 잘못된 방 구조 거절
            } // 수량 불일치 조건 끝
            DungeonRoom start = null; // 유일한 시작 방
            DungeonRoom stairs = null; // 유일한 계단 방
            var normalCount = 0; // 일반 방 수
            var eliteCount = 0; // 상위 개체 방 수
            foreach (var room in layout.Rooms) // 전체 방 역할 순회
            { // 역할 검사 시작
                if (room.IsSecret) // 이번 범위 밖 비밀 방 검사
                { // 비밀 방 조건 시작
                    return false; // 비밀 방이 있는 구조 거절
                } // 비밀 방 조건 끝
                switch (room.Kind) // 방 역할 선택
                { // 역할 선택 시작
                    case RoomKind.Start: // 시작 방
                        if (start != null) // 시작 방 중복 검사
                        { // 중복 조건 시작
                            return false; // 중복 시작 방 거절
                        } // 중복 조건 끝
                        start = room; // 유일한 시작 방 보관
                        break; // 역할 검사 종료
                    case RoomKind.Stairs: // 계단 방
                        if (stairs != null) // 계단 방 중복 검사
                        { // 중복 조건 시작
                            return false; // 중복 계단 방 거절
                        } // 중복 조건 끝
                        stairs = room; // 유일한 계단 방 보관
                        break; // 역할 검사 종료
                    case RoomKind.Elite: // 상위 개체 방
                        eliteCount++; // 상위 개체 방 수 증가
                        break; // 역할 검사 종료
                    case RoomKind.Normal: // 일반 방
                        normalCount++; // 일반 방 수 증가
                        break; // 역할 검사 종료
                    default: // 잘못된 방 역할
                        return false; // 알 수 없는 방 역할 거절
                } // 역할 선택 끝
            } // 역할 검사 끝
            if (start == null || stairs == null || start.X != 0 || start.Y != 0 || eliteCount != 1 || normalCount < 8 || normalCount > 10 || Math.Abs((long)stairs.X - start.X) + Math.Abs((long)stairs.Y - start.Y) < 5) // 필수 방과 지도상 거리 검사
            { // 필수 조건 불일치 시작
                return false; // 잘못된 필수 방 구조 거절
            } // 필수 조건 불일치 끝
            var distances = new Dictionary<long, int>(); // 시작 방의 최단 거리 목록
            var pending = new Queue<DungeonRoom>(); // 도달 가능한 방 대기열
            distances.Add(DungeonLayout.Key(start.X, start.Y), 0); // 시작 방 거리 설정
            pending.Enqueue(start); // 시작 방 탐색 예약
            while (pending.Count > 0) // 연결된 방 순회
            { // 경로 검사 시작
                var room = pending.Dequeue(); // 다음 방문 방 조회
                foreach (var door in layout.Doors) // 방 출구 순회
                { // 출구 검사 시작
                    if (door.RoomX != room.X || door.RoomY != room.Y || door.IsSecret) // 현재 방의 일반 출구 검사
                    { // 다른 출구 조건 시작
                        continue; // 다른 방과 비밀 출구 제외
                    } // 다른 출구 조건 끝
                    var key = DungeonLayout.Key(door.TargetRoomX, door.TargetRoomY); // 도착 방 식별자
                    if (!distances.ContainsKey(key)) // 아직 도달하지 않은 방 검사
                    { // 새 방 조건 시작
                        distances.Add(key, distances[DungeonLayout.Key(room.X, room.Y)] + 1); // 최단 연결 거리 보관
                        pending.Enqueue(layout.GetRoom(door.TargetRoomX, door.TargetRoomY)); // 다음 방 탐색 예약
                    } // 새 방 조건 끝
                } // 출구 검사 끝
            } // 경로 검사 끝
            return distances.Count == layout.Rooms.Count && distances.TryGetValue(DungeonLayout.Key(stairs.X, stairs.Y), out var stairDistance) && stairDistance >= 5; // 전체 연결과 계단 최단 거리 확인
        } // 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
