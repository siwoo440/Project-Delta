using System; // Serializable 기능 사용
using System.Collections.Generic; // 목록·사전 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    [Serializable] // 이후 실제 세이브 포맷으로 변환 가능한 순수 데이터
    public sealed class DungeonRoomSnapshot // 방 하나의 저장 데이터
    {
        public string RoomId; // 방 인스턴스 ID
        public string DefinitionId; // 원본 RoomDefinition ID
        public int MacroX; // 던전 격자 X
        public int MacroZ; // 던전 격자 Z
        public DungeonRoomRole Role; // 생성 단계 방 역할
    }

    [Serializable] // 연결 하나의 저장 데이터
    public sealed class DungeonConnectionSnapshot // 두 방 사이의 무방향 연결 하나
    {
        public string FromRoomId; // 기준 방 ID
        public string ToRoomId; // 이웃 방 ID
        public bool IsLocked; // 잠금 상태

        public int FromExitX; // 기준 방 출구 X
        public int FromExitZ; // 기준 방 출구 Z
        public CardinalDirection FromExitDirection; // 기준 방 출구 방향

        public int ToExitX; // 이웃 방 출구 X
        public int ToExitZ; // 이웃 방 출구 Z
        public CardinalDirection ToExitDirection; // 이웃 방 출구 방향
    }

    [Serializable] // 논리 던전 한 층 전체의 저장 데이터
    public sealed class DungeonLayoutSnapshot
    {
        public int Seed; // 이 레이아웃을 확정한 Seed
        public int TargetMainPathLength; // 생성 당시 목표 메인 경로 방 수
        public int TargetRoomCount; // 생성 당시 전체 목표 방 수
        public string EntryRoomId; // 시작 방 ID
        public string StairsRoomId; // 계단 방 ID
        public string FailureReason; // 일반적으로 성공 Snapshot에서는 null

        public List<DungeonRoomSnapshot> Rooms = new List<DungeonRoomSnapshot>(); // 전체 방 상태
        public List<DungeonConnectionSnapshot> Connections = new List<DungeonConnectionSnapshot>(); // 중복 제거된 전체 연결
        public List<string> MainPathRoomIds = new List<string>(); // 순서가 중요한 메인 경로
        public List<string> BranchRoomIds = new List<string>(); // 가지 방 목록
        public List<string> DeadEndCandidateRoomIds = new List<string>(); // 일반 막다른 후보
        public List<string> SpecialCandidateRoomIds = new List<string>(); // 특수 방 후보

        public static DungeonLayoutSnapshot Capture(GeneratedDungeon dungeon, int seed) // 생성 결과를 저장용 데이터로 변환
        {
            if (dungeon == null) // 생성 결과 확인
            {
                throw new ArgumentNullException(nameof(dungeon)); // null 차단
            }

            if (dungeon.Layout == null) // 그래프 확인
            {
                throw new InvalidOperationException("DungeonLayoutGraph가 없어 Snapshot을 만들 수 없습니다."); // 그래프 누락 차단
            }

            DungeonLayoutSnapshot snapshot = new DungeonLayoutSnapshot // 기본 메타데이터 기록
            {
                Seed = seed,
                TargetMainPathLength = dungeon.TargetMainPathLength,
                TargetRoomCount = dungeon.TargetRoomCount,
                EntryRoomId = dungeon.EntryRoom?.RoomId,
                StairsRoomId = dungeon.StairsRoom?.RoomId,
                FailureReason = dungeon.FailureReason
            };

            List<RoomNode> rooms = new List<RoomNode>(dungeon.Layout.AllRooms); // 전체 방 목록 복사
            rooms.Sort((left, right) => string.CompareOrdinal(left.RoomId, right.RoomId)); // 저장 순서 고정

            for (int i = 0; i < rooms.Count; i++) // 방 데이터 저장
            {
                RoomNode room = rooms[i]; // 현재 방
                DungeonRoomRole role = DungeonRoomRole.Branch; // 역할 미기록 방의 안전 기본값

                if (!dungeon.TryGetRoomRole(room, out role)) // 이전 방식 결과 등 역할이 없는지 확인
                {
                    role = DungeonRoomRole.Branch; // 일반 방 역할로 저장
                }

                snapshot.Rooms.Add(new DungeonRoomSnapshot
                {
                    RoomId = room.RoomId,
                    DefinitionId = room.DefinitionId,
                    MacroX = room.MacroCoordinate.X,
                    MacroZ = room.MacroCoordinate.Z,
                    Role = role
                }); // 방 저장 데이터 추가
            }

            CopyRoomIds(dungeon.MainPath, snapshot.MainPathRoomIds); // 메인 경로 순서 저장
            CopyRoomIds(dungeon.BranchRooms, snapshot.BranchRoomIds); // 가지 방 저장
            CopyRoomIds(dungeon.DeadEndCandidates, snapshot.DeadEndCandidateRoomIds); // 막다른 후보 저장
            CopyRoomIds(dungeon.SpecialRoomCandidates, snapshot.SpecialCandidateRoomIds); // 특수 후보 저장

            HashSet<string> savedPairs = new HashSet<string>(); // 양방향 Edge 중복 저장 방지

            for (int roomIndex = 0; roomIndex < rooms.Count; roomIndex++) // 모든 연결 순회
            {
                RoomNode room = rooms[roomIndex]; // 현재 방

                foreach (KeyValuePair<CardinalDirection, RoomConnectionEdge> pair in room.Connections) // 방향별 연결 순회
                {
                    RoomConnectionEdge edge = pair.Value; // 현재 Edge

                    if (edge == null || edge.Neighbor == null) // 잘못된 Edge 확인
                    {
                        throw new InvalidOperationException($"방 '{room.RoomId}'에 이웃 정보가 없는 Edge가 있습니다."); // Snapshot 생성 중단
                    }

                    string pairKey = BuildPairKey(room.RoomId, edge.Neighbor.RoomId); // 무방향 연결 키

                    if (!savedPairs.Add(pairKey)) // 반대편에서 이미 저장했는지 확인
                    {
                        continue; // 중복 저장 생략
                    }

                    if (!edge.HasExactExitPair) // 34일차 이후 연결 정보 보유 확인
                    {
                        throw new InvalidOperationException($"방 '{room.RoomId}' 연결에 정확한 RoomExit 쌍이 없습니다."); // 불완전 Snapshot 방지
                    }

                    RoomExit fromExit = edge.LocalExit.Value; // 현재 방 실제 출구
                    RoomExit toExit = edge.NeighborExit.Value; // 이웃 방 실제 출구

                    snapshot.Connections.Add(new DungeonConnectionSnapshot
                    {
                        FromRoomId = room.RoomId,
                        ToRoomId = edge.Neighbor.RoomId,
                        IsLocked = edge.IsLocked,
                        FromExitX = fromExit.LocalPosition.X,
                        FromExitZ = fromExit.LocalPosition.Z,
                        FromExitDirection = fromExit.Direction,
                        ToExitX = toExit.LocalPosition.X,
                        ToExitZ = toExit.LocalPosition.Z,
                        ToExitDirection = toExit.Direction
                    }); // 연결 저장
                }
            }

            snapshot.Connections.Sort((left, right) => // 저장 결과 비교와 디버깅을 위한 연결 순서 고정
            {
                int fromCompare = string.CompareOrdinal(left.FromRoomId, right.FromRoomId);

                if (fromCompare != 0)
                {
                    return fromCompare;
                }

                return string.CompareOrdinal(left.ToRoomId, right.ToRoomId);
            });

            return snapshot; // 완성 Snapshot 반환
        }

        public GeneratedDungeon Restore() // Snapshot에서 새로운 논리 던전 그래프 복원
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 새 그래프 생성
            Dictionary<string, RoomNode> nodes = new Dictionary<string, RoomNode>(); // ID별 복원 노드

            for (int i = 0; i < Rooms.Count; i++) // 전체 방 복원
            {
                DungeonRoomSnapshot roomData = Rooms[i]; // 저장 방 데이터

                if (roomData == null || string.IsNullOrEmpty(roomData.RoomId)) // 저장 데이터 유효성 확인
                {
                    throw new InvalidOperationException($"Rooms[{i}] 데이터가 올바르지 않습니다."); // 잘못된 저장 데이터 차단
                }

                RoomNode node = graph.AddRoom(
                    roomData.RoomId,
                    roomData.DefinitionId,
                    new GridPosition(roomData.MacroX, roomData.MacroZ)); // 동일 ID·정의·좌표로 방 복원
                nodes.Add(roomData.RoomId, node); // 조회 사전에 등록
            }

            for (int i = 0; i < Connections.Count; i++) // 전체 연결 복원
            {
                DungeonConnectionSnapshot connection = Connections[i]; // 현재 연결 데이터

                if (connection == null
                    || !nodes.TryGetValue(connection.FromRoomId, out RoomNode from)
                    || !nodes.TryGetValue(connection.ToRoomId, out RoomNode to)) // 양쪽 방 존재 확인
                {
                    throw new InvalidOperationException($"Connections[{i}]가 존재하지 않는 방을 참조합니다."); // 잘못된 참조 차단
                }

                RoomExit fromExit = new RoomExit(
                    new GridPosition(connection.FromExitX, connection.FromExitZ),
                    connection.FromExitDirection); // 기준 방 출구 복원

                RoomExit toExit = new RoomExit(
                    new GridPosition(connection.ToExitX, connection.ToExitZ),
                    connection.ToExitDirection); // 이웃 방 출구 복원

                graph.Connect(from, fromExit, to, toExit, connection.IsLocked); // 정확한 출구 쌍으로 양방향 Edge 복원
            }

            if (!nodes.TryGetValue(EntryRoomId, out RoomNode entryRoom)) // 시작 방 복원 확인
            {
                throw new InvalidOperationException($"EntryRoom '{EntryRoomId}'을 찾을 수 없습니다."); // 시작 방 누락 차단
            }

            if (!nodes.TryGetValue(StairsRoomId, out RoomNode stairsRoom)) // 계단 방 복원 확인
            {
                throw new InvalidOperationException($"StairsRoom '{StairsRoomId}'을 찾을 수 없습니다."); // 계단 방 누락 차단
            }

            List<RoomNode> mainPath = ResolveRoomIds(MainPathRoomIds, nodes); // 메인 경로 순서 복원
            List<RoomNode> branchRooms = ResolveRoomIds(BranchRoomIds, nodes); // 가지 목록 복원
            List<RoomNode> deadEnds = ResolveRoomIds(DeadEndCandidateRoomIds, nodes); // 막다른 후보 복원
            List<RoomNode> specialCandidates = ResolveRoomIds(SpecialCandidateRoomIds, nodes); // 특수 후보 복원

            return new GeneratedDungeon(
                graph,
                entryRoom,
                stairsRoom,
                mainPath,
                branchRooms,
                deadEnds,
                specialCandidates,
                TargetMainPathLength,
                TargetRoomCount,
                FailureReason); // 원래 생성 메타데이터까지 복원
        }

        private static void CopyRoomIds(IReadOnlyList<RoomNode> rooms, List<string> destination) // RoomNode 목록을 ID 목록으로 변환
        {
            if (rooms == null) // 원본 목록 확인
            {
                return; // 저장할 내용 없음
            }

            for (int i = 0; i < rooms.Count; i++) // 순서대로 ID 저장
            {
                if (rooms[i] != null) // null 방 제외
                {
                    destination.Add(rooms[i].RoomId); // 방 ID 추가
                }
            }
        }

        private static List<RoomNode> ResolveRoomIds(
            List<string> roomIds,
            Dictionary<string, RoomNode> nodes) // 저장된 ID 목록을 복원된 RoomNode 목록으로 변환
        {
            List<RoomNode> result = new List<RoomNode>(); // 복원 목록

            if (roomIds == null) // 저장 목록 확인
            {
                return result; // 빈 목록 반환
            }

            for (int i = 0; i < roomIds.Count; i++) // ID 순서 유지
            {
                string roomId = roomIds[i]; // 현재 ID

                if (!nodes.TryGetValue(roomId, out RoomNode node)) // 참조 방 존재 확인
                {
                    throw new InvalidOperationException($"저장된 방 목록이 존재하지 않는 RoomId '{roomId}'을 참조합니다."); // 잘못된 저장 데이터 차단
                }

                result.Add(node); // 복원 목록에 추가
            }

            return result; // 완성 목록 반환
        }

        private static string BuildPairKey(string first, string second) // 두 방 ID의 순서와 무관한 연결 키
        {
            return string.CompareOrdinal(first, second) < 0
                ? $"{first}<->{second}"
                : $"{second}<->{first}"; // 항상 사전순으로 키 생성
        }
    }
}
