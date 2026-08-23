using System; // 예외 타입 사용
using System.Collections.Generic; // 사전 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public sealed class RoomConnectionEdge // 노드 하나가 한쪽 방향으로 가진 연결 정보
    {
        public RoomNode Neighbor { get; } // 그 방향으로 연결된 옆 방
        public bool IsLocked { get; } // 잠긴 문으로 연결되었는지 여부
        public RoomExit? LocalExit { get; } // 현재 방에서 실제로 사용된 출구
        public RoomExit? NeighborExit { get; } // 이웃 방에서 실제로 사용된 출구
        public bool HasExactExitPair => LocalExit.HasValue && NeighborExit.HasValue; // 정확한 출구 쌍 보유 여부

        public RoomConnectionEdge(RoomNode neighbor, bool isLocked) // 이전 일차 호환 생성자
            : this(neighbor, isLocked, null, null)
        {
        }

        public RoomConnectionEdge(RoomNode neighbor, bool isLocked, RoomExit? localExit, RoomExit? neighborExit) // 정확한 출구 정보를 포함한 연결 생성자
        {
            Neighbor = neighbor; // 옆 방 저장
            IsLocked = isLocked; // 잠김 여부 저장
            LocalExit = localExit; // 현재 방 출구 저장
            NeighborExit = neighborExit; // 이웃 방 출구 저장
        }
    }

    public sealed class RoomNode // 던전 그래프 안 방 하나
    {
        public string RoomId { get; } // 방 식별자
        public string DefinitionId { get; } // 원본 RoomDefinition의 Id
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

        internal bool HasConnection(CardinalDirection direction) // 해당 방향 연결 존재 여부
        {
            return connections.ContainsKey(direction); // 방향 키 존재 결과 반환
        }

        internal void SetConnection(CardinalDirection direction, RoomConnectionEdge edge) // DungeonLayoutGraph 전용 연결 기록
        {
            connections[direction] = edge; // 방향별 연결 저장
        }
    }

    public sealed class DungeonLayoutGraph // 던전 한 층의 방-방 연결 그래프
    {
        private readonly Dictionary<string, RoomNode> nodesByRoomId = new Dictionary<string, RoomNode>(); // 식별자별 방 노드
        private readonly Dictionary<GridPosition, RoomNode> nodesByCoordinate = new Dictionary<GridPosition, RoomNode>(); // 좌표별 방 노드

        public IReadOnlyCollection<RoomNode> AllRooms => nodesByRoomId.Values; // 전체 방 노드 공개

        public RoomNode AddRoom(string roomId, string definitionId, GridPosition macroCoordinate) // 방 노드 추가
        {
            if (string.IsNullOrEmpty(roomId)) // 방 식별자 존재 확인
            {
                throw new ArgumentException("roomId는 비어있을 수 없습니다.", nameof(roomId)); // 식별자 누락 예외
            }

            if (nodesByRoomId.ContainsKey(roomId)) // 같은 방 ID 중복 확인
            {
                throw new InvalidOperationException($"방 ID '{roomId}'는 이미 존재합니다."); // 방 ID 중복 예외
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

        public bool TryConnect(RoomNode from, CardinalDirection direction, RoomNode to, bool isLocked = false) // 방향 정보만 사용한 안전한 연결 시도
        {
            if (!CanConnectNodes(from, direction, to)) // 기본 그래프 연결 규칙 확인
            {
                return false; // 규칙 위반 시 연결 거부
            }

            CardinalDirection opposite = RoomGridLayout.GetOpposite(direction); // 반대 방향 계산
            from.SetConnection(direction, new RoomConnectionEdge(to, isLocked)); // from -> to 연결
            to.SetConnection(opposite, new RoomConnectionEdge(from, isLocked)); // to -> from 연결
            return true; // 연결 성공
        }

        public bool TryConnect(RoomNode from, RoomExit fromExit, RoomNode to, RoomExit toExit, bool isLocked = false) // 정확한 출구 쌍을 사용한 안전한 연결 시도
        {
            if (!fromExit.CanConnectTo(toExit)) // 방향과 정렬 축이 일치하는지 확인
            {
                return false; // 물리적으로 맞지 않는 출구는 연결 거부
            }

            if (!CanConnectNodes(from, fromExit.Direction, to)) // 그래프 방향·좌표·중복 규칙 확인
            {
                return false; // 그래프 규칙 위반 시 연결 거부
            }

            CardinalDirection opposite = RoomGridLayout.GetOpposite(fromExit.Direction); // 반대 방향 계산

            if (opposite != toExit.Direction) // 목적지 출구 방향 재확인
            {
                return false; // 반대 방향이 아니면 연결 거부
            }

            from.SetConnection(
                fromExit.Direction,
                new RoomConnectionEdge(to, isLocked, fromExit, toExit)); // from 쪽에 정확한 출구 쌍 저장

            to.SetConnection(
                toExit.Direction,
                new RoomConnectionEdge(from, isLocked, toExit, fromExit)); // to 쪽에는 반대 관점으로 출구 쌍 저장

            return true; // 연결 성공
        }

        public void Connect(RoomNode from, CardinalDirection direction, RoomNode to, bool isLocked = false) // 이전 일차 호환 연결 API
        {
            if (!TryConnect(from, direction, to, isLocked)) // 안전한 연결 시도
            {
                throw new InvalidOperationException($"방 연결에 실패했습니다. {DescribeRoom(from)} --{direction}--> {DescribeRoom(to)}"); // 잘못된 연결을 조용히 덮어쓰지 않음
            }
        }

        public void Connect(RoomNode from, RoomExit fromExit, RoomNode to, RoomExit toExit, bool isLocked = false) // 정확한 출구 쌍 연결 API
        {
            if (!TryConnect(from, fromExit, to, toExit, isLocked)) // 정확한 출구 연결 시도
            {
                throw new InvalidOperationException($"출구 연결에 실패했습니다. {DescribeRoom(from)} [{fromExit}] <-> {DescribeRoom(to)} [{toExit}]"); // 잘못된 출구 연결 차단
            }
        }

        private static bool CanConnectNodes(RoomNode from, CardinalDirection direction, RoomNode to) // 공통 그래프 연결 규칙
        {
            if (from == null || to == null) // 노드 존재 확인
            {
                return false; // null 연결 거부
            }

            if (ReferenceEquals(from, to)) // 자기 자신 연결 확인
            {
                return false; // 자기 연결 거부
            }

            CardinalDirection opposite = RoomGridLayout.GetOpposite(direction); // 목적지 반대 방향 계산

            if (from.HasConnection(direction) || to.HasConnection(opposite)) // 양쪽 방향 사용 여부 확인
            {
                return false; // 기존 연결 덮어쓰기 방지
            }

            GridPosition expectedCoordinate = from.MacroCoordinate + GridMovement.GetDirectionDelta(direction); // 방향상 목적지 좌표 계산

            if (expectedCoordinate != to.MacroCoordinate) // 실제 인접 좌표인지 확인
            {
                return false; // 떨어져 있거나 잘못된 방향이면 연결 거부
            }

            return true; // 모든 공통 규칙 통과
        }

        private static string DescribeRoom(RoomNode room) // 예외 메시지용 방 설명
        {
            return room == null ? "null" : $"{room.RoomId}@{room.MacroCoordinate}"; // 방 ID와 좌표 반환
        }
    }
}
