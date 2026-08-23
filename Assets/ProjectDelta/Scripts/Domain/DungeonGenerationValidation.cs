using System; // 직렬화·문자열 기능 사용
using System.Collections.Generic; // 목록·집합·대기열 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public enum DungeonValidationCode // 생성 완료 후 검증 실패 종류
    {
        GeneratorReportedFailure, // 생성기 자체가 실패를 보고함
        RoomCountMismatch, // 목표 방 수 불일치
        MainPathIncomplete, // 목표 메인 경로 미완성
        MainPathLengthOutOfRange, // 메인 경로 길이 범위 위반
        MainPathEndpointMismatch, // MainPath 처음·끝이 Entry/Stairs와 불일치
        EntryOrStairsMissing, // 시작 방 또는 계단 방 누락
        EntryToStairsDistanceOutOfRange, // 실제 최단 거리가 설정 범위 위반
        DuplicateCoordinate, // 같은 MacroCoordinate 중복
        TooManyConnections, // 방 하나의 연결 수 제한 초과
        SelfConnection, // 자기 자신 연결
        NonAdjacentConnection, // 던전 격자상 인접하지 않은 방 연결
        MissingReciprocalConnection, // 반대편 양방향 Edge 누락
        MissingExactExitPair, // 정확한 RoomExit 연결 정보 누락
        InvalidExitPair, // 저장된 RoomExit 쌍 불일치
        DisconnectedRoom // Entry에서 도달할 수 없는 방 존재
    }

    [Serializable] // 실패 기록 저장 지원
    public sealed class DungeonValidationIssue // 검증 실패 하나
    {
        public DungeonValidationCode Code { get; } // 실패 종류
        public string Message { get; } // 사람이 읽을 수 있는 설명
        public string RoomId { get; } // 관련 방 ID, 없으면 null

        public DungeonValidationIssue(DungeonValidationCode code, string message, string roomId = null) // 실패 기록 생성자
        {
            Code = code; // 실패 종류 저장
            Message = message; // 설명 저장
            RoomId = roomId; // 관련 방 저장
        }

        public override string ToString() // 디버그 문자열
        {
            return string.IsNullOrEmpty(RoomId)
                ? $"{Code}: {Message}"
                : $"{Code} [{RoomId}]: {Message}"; // 방 ID 포함 여부에 따라 출력
        }
    }

    public sealed class DungeonValidationResult // 던전 전체 검증 결과
    {
        private readonly List<DungeonValidationIssue> issues; // 발견된 모든 문제

        public bool IsValid => issues.Count == 0; // 문제 없음 여부
        public IReadOnlyList<DungeonValidationIssue> Issues => issues; // 외부 읽기 전용 문제 목록
        public int EntryToStairsDistance { get; } // 실제 최단 이동 Edge 수, 계산 불가 시 -1

        public DungeonValidationResult(List<DungeonValidationIssue> issues, int entryToStairsDistance) // 검증 결과 생성자
        {
            this.issues = issues ?? new List<DungeonValidationIssue>(); // 문제 목록 보관
            EntryToStairsDistance = entryToStairsDistance; // 최단 거리 보관
        }
    }

    public sealed class DungeonGenerationValidator // 완성된 논리 던전의 최종 안전성 검사기
    {
        private const int MaxCardinalConnections = 4; // N/E/S/W 네 방향 최대 연결 수

        public DungeonValidationResult Validate(GeneratedDungeon dungeon, DungeonGenerationSettings settings) // 던전 전체 검증
        {
            List<DungeonValidationIssue> issues = new List<DungeonValidationIssue>(); // 발견 문제 목록

            if (dungeon == null) // 생성 결과 존재 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.GeneratorReportedFailure,
                    "GeneratedDungeon이 null입니다.")); // null 결과 기록
                return new DungeonValidationResult(issues, -1); // 추가 검사 불가
            }

            if (dungeon.Layout == null) // 그래프 존재 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.GeneratorReportedFailure,
                    "DungeonLayoutGraph가 null입니다.")); // 그래프 누락 기록
                return new DungeonValidationResult(issues, -1); // 추가 검사 불가
            }

            if (!string.IsNullOrEmpty(dungeon.FailureReason)) // 생성기 자체 실패 여부 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.GeneratorReportedFailure,
                    dungeon.FailureReason)); // 생성 실패 원인 전달
            }

            if (settings != null) // 설정 기반 제약 검사
            {
                ValidateRoomCount(dungeon, settings, issues); // 전체 방 수 검사
                ValidateMainPath(dungeon, settings, issues); // 메인 경로 길이·끝점 검사
            }

            ValidateCoordinatesAndConnections(dungeon, issues); // 좌표·Edge 무결성 검사
            ValidateReachability(dungeon, issues); // 전체 연결성 검사

            int distance = FindShortestDistance(dungeon.Layout, dungeon.EntryRoom, dungeon.StairsRoom); // 실제 Entry→Stairs 최단 거리 계산

            if (settings != null) // 거리 범위 검사
            {
                ValidateEntryToStairsDistance(distance, settings, issues); // 메인 진행 거리 제약 확인
            }

            return new DungeonValidationResult(issues, distance); // 전체 검증 결과 반환
        }

        private static void ValidateRoomCount(
            GeneratedDungeon dungeon,
            DungeonGenerationSettings settings,
            List<DungeonValidationIssue> issues) // 전체 목표 방 수 검사
        {
            int actualRoomCount = dungeon.Layout.AllRooms.Count; // 실제 방 수

            if (actualRoomCount != settings.TargetRoomCount) // 목표와 불일치하는지 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.RoomCountMismatch,
                    $"목표 방 수 {settings.TargetRoomCount}개와 실제 방 수 {actualRoomCount}개가 다릅니다.")); // 불일치 기록
            }
        }

        private static void ValidateMainPath(
            GeneratedDungeon dungeon,
            DungeonGenerationSettings settings,
            List<DungeonValidationIssue> issues) // 메인 경로 제약 검사
        {
            if (!dungeon.MainPathCompleted) // 생성기가 목표 메인 경로를 완성했는지 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.MainPathIncomplete,
                    $"목표 메인 경로 {dungeon.TargetMainPathLength}개 방을 완성하지 못했습니다.")); // 미완성 기록
            }

            int length = dungeon.MainPath.Count; // 시작·계단 포함 실제 메인 경로 방 수

            if (length < settings.MinMainPathLength || length > settings.MaxMainPathLength) // 설정 범위 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.MainPathLengthOutOfRange,
                    $"메인 경로 방 수 {length}개가 허용 범위 {settings.MinMainPathLength}~{settings.MaxMainPathLength}를 벗어났습니다.")); // 범위 위반 기록
            }

            if (dungeon.EntryRoom == null || dungeon.StairsRoom == null) // 시작·계단 존재 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.EntryOrStairsMissing,
                    "EntryRoom 또는 StairsRoom이 없습니다.")); // 누락 기록
                return; // 끝점 비교 불가
            }

            if (dungeon.MainPath.Count == 0
                || !ReferenceEquals(dungeon.MainPath[0], dungeon.EntryRoom)
                || !ReferenceEquals(dungeon.MainPath[dungeon.MainPath.Count - 1], dungeon.StairsRoom)) // 메인 경로 끝점 일치 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.MainPathEndpointMismatch,
                    "MainPath의 처음과 끝이 EntryRoom/StairsRoom과 일치하지 않습니다.")); // 끝점 불일치 기록
            }
        }

        private static void ValidateCoordinatesAndConnections(
            GeneratedDungeon dungeon,
            List<DungeonValidationIssue> issues) // 좌표와 연결 구조 검사
        {
            HashSet<GridPosition> coordinates = new HashSet<GridPosition>(); // 좌표 중복 검사용 집합

            foreach (RoomNode room in dungeon.Layout.AllRooms) // 전체 방 순회
            {
                if (!coordinates.Add(room.MacroCoordinate)) // 동일 좌표가 이미 존재하는지 확인
                {
                    issues.Add(new DungeonValidationIssue(
                        DungeonValidationCode.DuplicateCoordinate,
                        $"MacroCoordinate {room.MacroCoordinate}가 중복되었습니다.",
                        room.RoomId)); // 좌표 중복 기록
                }

                if (room.Connections.Count > MaxCardinalConnections) // 연결 수 제한 확인
                {
                    issues.Add(new DungeonValidationIssue(
                        DungeonValidationCode.TooManyConnections,
                        $"연결 수 {room.Connections.Count}개가 최대 {MaxCardinalConnections}개를 초과했습니다.",
                        room.RoomId)); // 연결 수 초과 기록
                }

                foreach (KeyValuePair<CardinalDirection, RoomConnectionEdge> pair in room.Connections) // 방향별 Edge 순회
                {
                    ValidateConnection(room, pair.Key, pair.Value, issues); // Edge 하나 검증
                }
            }
        }

        private static void ValidateConnection(
            RoomNode room,
            CardinalDirection direction,
            RoomConnectionEdge edge,
            List<DungeonValidationIssue> issues) // Edge 하나의 양방향·출구 정렬 검사
        {
            if (edge == null || edge.Neighbor == null) // 이웃 정보 존재 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.MissingReciprocalConnection,
                    $"{direction} 방향 Edge의 이웃 방 정보가 없습니다.",
                    room.RoomId)); // 잘못된 Edge 기록
                return;
            }

            if (ReferenceEquals(room, edge.Neighbor)) // 자기 연결 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.SelfConnection,
                    $"{direction} 방향이 자기 자신과 연결되어 있습니다.",
                    room.RoomId)); // 자기 연결 기록
            }

            GridPosition expectedCoordinate = room.MacroCoordinate + GridMovement.GetDirectionDelta(direction); // 정상 이웃 좌표 계산

            if (expectedCoordinate != edge.Neighbor.MacroCoordinate) // 실제 좌표와 비교
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.NonAdjacentConnection,
                    $"{direction} 방향 연결의 이웃 좌표가 {expectedCoordinate}가 아니라 {edge.Neighbor.MacroCoordinate}입니다.",
                    room.RoomId)); // 비인접 연결 기록
            }

            CardinalDirection opposite = RoomGridLayout.GetOpposite(direction); // 반대 방향 계산

            if (!edge.Neighbor.TryGetConnection(opposite, out RoomConnectionEdge reciprocal)
                || reciprocal == null
                || !ReferenceEquals(reciprocal.Neighbor, room)) // 반대쪽 Edge가 정확히 되돌아오는지 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.MissingReciprocalConnection,
                    $"{edge.Neighbor.RoomId}의 {opposite} 방향에 대응 Edge가 없습니다.",
                    room.RoomId)); // 양방향 불일치 기록
            }

            if (!edge.HasExactExitPair) // 34일차 이후 정확한 출구 쌍 보유 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.MissingExactExitPair,
                    $"{direction} 방향 연결에 실제 RoomExit 쌍이 저장되어 있지 않습니다.",
                    room.RoomId)); // 출구 정보 누락 기록
                return;
            }

            RoomExit localExit = edge.LocalExit.Value; // 현재 방 출구
            RoomExit neighborExit = edge.NeighborExit.Value; // 이웃 방 출구

            if (localExit.Direction != direction
                || neighborExit.Direction != opposite
                || !localExit.CanConnectTo(neighborExit)) // 방향·정렬 축 일치 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.InvalidExitPair,
                    $"저장된 출구 쌍 {localExit} <-> {neighborExit}이 그래프 방향 {direction}과 일치하지 않습니다.",
                    room.RoomId)); // 잘못된 출구 쌍 기록
            }
        }

        private static void ValidateReachability(
            GeneratedDungeon dungeon,
            List<DungeonValidationIssue> issues) // Entry에서 전체 방 도달 가능 여부 검사
        {
            if (dungeon.EntryRoom == null) // 시작 방 존재 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.EntryOrStairsMissing,
                    "EntryRoom이 없어 전체 연결성을 검사할 수 없습니다.")); // 시작 방 누락 기록
                return;
            }

            HashSet<string> visited = new HashSet<string>(); // 방문 방 ID
            Queue<RoomNode> queue = new Queue<RoomNode>(); // BFS 대기열
            visited.Add(dungeon.EntryRoom.RoomId); // 시작 방 방문 처리
            queue.Enqueue(dungeon.EntryRoom); // 시작 방 탐색 등록

            while (queue.Count > 0) // 모든 연결 탐색
            {
                RoomNode current = queue.Dequeue(); // 현재 방

                foreach (KeyValuePair<CardinalDirection, RoomConnectionEdge> pair in current.Connections) // 이웃 순회
                {
                    RoomNode neighbor = pair.Value?.Neighbor; // 이웃 방 조회

                    if (neighbor != null && visited.Add(neighbor.RoomId)) // 처음 방문한 이웃인지 확인
                    {
                        queue.Enqueue(neighbor); // 다음 탐색 등록
                    }
                }
            }

            foreach (RoomNode room in dungeon.Layout.AllRooms) // 전체 방과 방문 결과 비교
            {
                if (!visited.Contains(room.RoomId)) // Entry에서 도달하지 못한 방인지 확인
                {
                    issues.Add(new DungeonValidationIssue(
                        DungeonValidationCode.DisconnectedRoom,
                        "EntryRoom에서 도달할 수 없는 방입니다.",
                        room.RoomId)); // 단절 방 기록
                }
            }
        }

        private static void ValidateEntryToStairsDistance(
            int distance,
            DungeonGenerationSettings settings,
            List<DungeonValidationIssue> issues) // 실제 최단 진행 거리 검사
        {
            if (distance < 0) // 계단까지 도달 불가능한지 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.EntryToStairsDistanceOutOfRange,
                    "EntryRoom에서 StairsRoom까지 도달할 수 없습니다.")); // 거리 계산 실패 기록
                return;
            }

            int minEdges = Math.Max(0, settings.MinMainPathLength - 1); // 방 수를 이동 Edge 수로 변환
            int maxEdges = Math.Max(0, settings.MaxMainPathLength - 1); // 최대 이동 Edge 수 계산

            if (distance < minEdges || distance > maxEdges) // 루프 지름길까지 고려한 실제 최단 거리 범위 확인
            {
                issues.Add(new DungeonValidationIssue(
                    DungeonValidationCode.EntryToStairsDistanceOutOfRange,
                    $"Entry→Stairs 최단 거리 {distance}칸이 허용 범위 {minEdges}~{maxEdges}칸을 벗어났습니다.")); // 거리 위반 기록
            }
        }

        private static int FindShortestDistance(
            DungeonLayoutGraph graph,
            RoomNode start,
            RoomNode target) // 두 방 사이 최단 Edge 거리 계산
        {
            if (graph == null || start == null || target == null) // 계산 가능 여부 확인
            {
                return -1; // 계산 불가
            }

            if (ReferenceEquals(start, target)) // 같은 방인지 확인
            {
                return 0; // 이동 거리 없음
            }

            Queue<RoomNode> queue = new Queue<RoomNode>(); // BFS 대기열
            Dictionary<string, int> distances = new Dictionary<string, int> // 방 ID별 거리
            {
                { start.RoomId, 0 }
            };
            queue.Enqueue(start); // 시작 방 등록

            while (queue.Count > 0) // BFS 반복
            {
                RoomNode current = queue.Dequeue(); // 현재 방
                int currentDistance = distances[current.RoomId]; // 현재 거리

                foreach (KeyValuePair<CardinalDirection, RoomConnectionEdge> pair in current.Connections) // 이웃 순회
                {
                    RoomNode neighbor = pair.Value?.Neighbor; // 이웃 방

                    if (neighbor == null || distances.ContainsKey(neighbor.RoomId)) // 잘못된 Edge 또는 재방문 확인
                    {
                        continue; // 생략
                    }

                    int nextDistance = currentDistance + 1; // 이웃 거리 계산

                    if (ReferenceEquals(neighbor, target)) // 목표 방 도달 확인
                    {
                        return nextDistance; // 최단 거리 반환
                    }

                    distances[neighbor.RoomId] = nextDistance; // 거리 기록
                    queue.Enqueue(neighbor); // 탐색 등록
                }
            }

            return -1; // 목표 방에 도달하지 못함
        }
    }
}
