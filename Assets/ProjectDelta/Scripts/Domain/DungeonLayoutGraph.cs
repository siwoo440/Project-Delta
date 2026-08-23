using System; // 예외 타입 사용
using System.Collections.Generic; // 사전 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 28일차: 던전 한 층의 방-방 연결을 나타내는 그래프.
    // RoomGridLayout(방 하나 내부 칸 단위 통로)과는 스케일이 다르다 - 여기서는 "방 하나 = 노드 하나".
    // 방 내부 모양(칸 크기, 문 배치)은 계속 RoomDefinition/RoomGridLayout이 담당하고,
    // 이 그래프는 그 방들을 던전 전체 격자에서 어떻게 배치·연결했는지만 다룬다.
    public sealed class RoomConnectionEdge // 노드 하나가 한쪽 방향으로 가진 연결 정보
    {
        public RoomNode Neighbor { get; } // 그 방향으로 연결된 옆 방
        public bool IsLocked { get; } // 잠긴 문으로 연결되었는지 여부

        public RoomConnectionEdge(RoomNode neighbor, bool isLocked) // 연결 정보 생성자
        {
            Neighbor = neighbor; // 옆 방 저장
            IsLocked = isLocked; // 잠김 여부 저장
        }
    }

    public sealed class RoomNode // 던전 그래프 안 방 하나
    {
        public string RoomId { get; } // 방 식별자 (RoomInstance.RoomId와 대응)
        public string DefinitionId { get; } // 원본 RoomDefinition의 Id

        // 지금은 방 하나가 던전 격자 한 칸만 차지한다고 가정한다.
        // TODO: 여러 칸을 차지하는 방 모양이 생기면 이 필드 하나로는 부족해진다. 그때는
        // MacroCoordinate 대신 OccupiedCoordinates(IReadOnlyList<GridPosition>) 형태로 바꾸고,
        // DungeonLayoutGraph의 좌표별 조회(nodesByCoordinate)도 여러 칸을 등록하도록 손보면 된다.
        public GridPosition MacroCoordinate { get; } // 던전 격자 안 이 방의 좌표

        private readonly Dictionary<CardinalDirection, RoomConnectionEdge> connections = new Dictionary<CardinalDirection, RoomConnectionEdge>(); // 방향별 연결 정보

        public IReadOnlyDictionary<CardinalDirection, RoomConnectionEdge> Connections => connections; // 전체 연결 목록 공개

        public RoomNode(string roomId, string definitionId, GridPosition macroCoordinate) // 방 노드 생성자
        {
            RoomId = roomId; // 방 식별자 저장
            DefinitionId = definitionId; // 정의 식별자 저장
            MacroCoordinate = macroCoordinate; // 던전 격자 좌표 저장
        }

        public bool TryGetConnection(CardinalDirection direction, out RoomConnectionEdge edge) // 방향별 연결 조회
        {
            return connections.TryGetValue(direction, out edge); // 연결 정보 조회 결과 반환
        }

        internal void SetConnection(CardinalDirection direction, RoomConnectionEdge edge) // 방향별 연결 설정 (DungeonLayoutGraph.Connect 전용)
        {
            connections[direction] = edge; // 연결 정보 저장
        }
    }

    public sealed class DungeonLayoutGraph // 던전 한 층의 방-방 연결 그래프
    {
        private readonly Dictionary<string, RoomNode> nodesByRoomId = new Dictionary<string, RoomNode>(); // 식별자별 방 노드
        private readonly Dictionary<GridPosition, RoomNode> nodesByCoordinate = new Dictionary<GridPosition, RoomNode>(); // 좌표별 방 노드

        public IReadOnlyCollection<RoomNode> AllRooms => nodesByRoomId.Values; // 전체 방 노드 공개

        // 새 방 노드를 그래프에 등록한다. 같은 좌표에 이미 방이 있으면 예외를 던진다.
        public RoomNode AddRoom(string roomId, string definitionId, GridPosition macroCoordinate) // 방 노드 추가
        {
            if (string.IsNullOrEmpty(roomId)) // 방 식별자 존재 확인
            {
                throw new ArgumentException("roomId는 비어있을 수 없습니다.", nameof(roomId)); // 식별자 누락 예외
            }

            if (nodesByCoordinate.ContainsKey(macroCoordinate)) // 좌표 중복 확인
            {
                throw new InvalidOperationException($"좌표 {macroCoordinate}에는 이미 다른 방이 있습니다."); // 좌표 중복 예외
            }

            RoomNode node = new RoomNode(roomId, definitionId, macroCoordinate); // 새 방 노드 생성
            nodesByRoomId[roomId] = node; // 식별자 기준 등록
            nodesByCoordinate[macroCoordinate] = node; // 좌표 기준 등록
            return node; // 생성된 노드 반환
        }

        public bool TryGetRoom(string roomId, out RoomNode node) // 식별자로 방 노드 조회
        {
            return nodesByRoomId.TryGetValue(roomId, out node); // 조회 결과 반환
        }

        public bool TryGetRoomAt(GridPosition macroCoordinate, out RoomNode node) // 좌표로 방 노드 조회
        {
            return nodesByCoordinate.TryGetValue(macroCoordinate, out node); // 조회 결과 반환
        }

        // 두 방을 양방향으로 연결한다. direction은 from 기준 방향이고, to에는 자동으로 반대 방향에 연결된다.
        public void Connect(RoomNode from, CardinalDirection direction, RoomNode to, bool isLocked = false) // 두 방 노드 연결
        {
            if (from == null) // from 노드 존재 확인
            {
                throw new ArgumentNullException(nameof(from)); // from 누락 예외
            }

            if (to == null) // to 노드 존재 확인
            {
                throw new ArgumentNullException(nameof(to)); // to 누락 예외
            }

            CardinalDirection opposite = RoomGridLayout.GetOpposite(direction); // 반대 방향 계산 (기존 로직 재사용)
            from.SetConnection(direction, new RoomConnectionEdge(to, isLocked)); // from -> to 방향 연결
            to.SetConnection(opposite, new RoomConnectionEdge(from, isLocked)); // to -> from 반대 방향 연결
        }
    }
}
