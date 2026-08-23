using System; // Random 사용
using System.Collections.Generic; // 목록 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 생성이 끝난 던전 층 결과.
    public sealed class GeneratedDungeon // 생성된 던전 층 결과
    {
        public DungeonLayoutGraph Layout { get; } // 완성된 방-방 연결 그래프
        public RoomNode EntryRoom { get; } // 플레이어가 시작하는 방
        public RoomNode StairsRoom { get; } // 다음 층 계단을 놓을 방

        public GeneratedDungeon(DungeonLayoutGraph layout, RoomNode entryRoom, RoomNode stairsRoom) // 생성 결과 생성자
        {
            Layout = layout; // 그래프 저장
            EntryRoom = entryRoom; // 시작 방 저장
            StairsRoom = stairsRoom; // 계단 방 저장
        }
    }

    // DungeonLayoutGraph를 채우는 절차적 생성기.
    // 30일차부터 프론티어가 단순 방향이 아니라 RoomExit 자체를 보관하여 출구 좌표를 잃지 않는다.
    // 이번 일차에서는 기존 생성 규칙을 유지하며, 실제 프리팹 문 정렬·출구 위치 제약은 이후 일차에서 확장한다.
    public sealed class DungeonGenerator // 절차적 던전 층 생성기
    {
        private readonly Random random; // 생성에 쓰는 난수 발생기

        public DungeonGenerator(int seed) // 시드 기반 생성기 생성자
        {
            random = new Random(seed); // 시드로 난수 발생기 초기화
        }

        public GeneratedDungeon Generate(RoomTemplate entryTemplate, IReadOnlyList<RoomTemplate> roomPool, int targetRoomCount) // 던전 층 생성
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 빈 연결 그래프 생성
            RoomNode entryNode = graph.AddRoom(NewRoomId(entryTemplate, 0), entryTemplate.DefinitionId, GridPosition.Zero); // 원점에 시작 방 배치

            List<FrontierEntry> frontier = new List<FrontierEntry> // 아직 사용하지 않은 출구가 있는 방 목록
            {
                new FrontierEntry(entryNode, new List<RoomExit>(entryTemplate.Exits))
            };

            int createdCount = 1; // 지금까지 만든 방 개수
            int roomIdCounter = 1; // 다음 방 식별자 번호
            int maxAttempts = Math.Max(targetRoomCount * 20, 20); // 무한 루프 방지 최대 시도 횟수
            int attempts = 0; // 현재 시도 횟수

            while (createdCount < targetRoomCount && frontier.Count > 0 && attempts < maxAttempts) // 생성 반복
            {
                attempts++; // 시도 횟수 누적

                int frontierIndex = random.Next(frontier.Count); // 프론티어 무작위 선택
                FrontierEntry current = frontier[frontierIndex]; // 선택된 프론티어 조회

                if (current.RemainingExits.Count == 0) // 남은 출구가 없는지 확인
                {
                    frontier.RemoveAt(frontierIndex); // 프론티어에서 제거
                    continue; // 다음 시도로 이동
                }

                int exitIndex = random.Next(current.RemainingExits.Count); // 남은 출구 중 하나 선택
                RoomExit currentExit = current.RemainingExits[exitIndex]; // 선택된 실제 출구 조회
                current.RemainingExits.RemoveAt(exitIndex); // 현재 출구는 이번 시도로 소모

                GridPosition candidatePosition = current.Node.MacroCoordinate + GridMovement.GetDirectionDelta(currentExit.Direction); // 출구 방향의 인접 방 좌표 계산

                if (graph.TryGetRoomAt(candidatePosition, out _)) // 인접 좌표에 이미 방이 있는지 확인
                {
                    RemoveFrontierIfEmpty(frontier, frontierIndex, current); // 필요 시 프론티어 정리
                    continue; // 좌표 충돌이면 다음 시도로 이동
                }

                CardinalDirection neededDirection = RoomGridLayout.GetOpposite(currentExit.Direction); // 새 방이 가져야 할 반대 방향
                RoomTemplate nextTemplate = PickTemplateWithExit(roomPool, neededDirection); // 필요한 방향 출구를 가진 방 선택

                if (nextTemplate == null) // 연결 가능한 방이 없는지 확인
                {
                    RemoveFrontierIfEmpty(frontier, frontierIndex, current); // 필요 시 프론티어 정리
                    continue; // 다음 시도로 이동
                }

                RoomExit connectedExit = FindFirstExit(nextTemplate, neededDirection); // 새 방에서 실제로 연결에 사용할 출구 선택
                RoomNode newNode = graph.AddRoom(NewRoomId(nextTemplate, roomIdCounter), nextTemplate.DefinitionId, candidatePosition); // 새 방 노드 생성
                roomIdCounter++; // 다음 번호 준비
                graph.Connect(current.Node, currentExit.Direction, newNode); // 기존 그래프 연결 규칙으로 두 방 연결

                List<RoomExit> newRemaining = new List<RoomExit>(nextTemplate.Exits); // 새 방 출구 목록 복사
                newRemaining.Remove(connectedExit); // 이번 연결에 사용한 정확한 출구 하나만 제거

                if (newRemaining.Count > 0) // 새 방에 사용할 수 있는 출구가 남았는지 확인
                {
                    frontier.Add(new FrontierEntry(newNode, newRemaining)); // 새 방을 프론티어에 등록
                }

                createdCount++; // 생성된 방 개수 누적
                RemoveFrontierIfEmpty(frontier, frontierIndex, current); // 현재 방의 남은 출구가 없으면 정리
            }

            RoomNode stairsRoom = FindFurthestLeaf(graph, entryNode); // 가장 먼 막다른 방을 계단 방으로 선정
            return new GeneratedDungeon(graph, entryNode, stairsRoom); // 생성 결과 반환
        }

        private RoomTemplate PickTemplateWithExit(IReadOnlyList<RoomTemplate> roomPool, CardinalDirection requiredDirection) // 필요한 방향 출구를 가진 방 선택
        {
            List<RoomTemplate> candidates = new List<RoomTemplate>(); // 조건에 맞는 후보 목록

            if (roomPool == null) // 방 후보 목록 존재 확인
            {
                return null; // 후보 목록이 없으면 선택 불가
            }

            foreach (RoomTemplate template in roomPool) // 방 종류 후보 전체 반복
            {
                if (template != null && HasExitDirection(template, requiredDirection)) // 필요한 방향 출구 보유 확인
                {
                    candidates.Add(template); // 후보 목록에 추가
                }
            }

            if (candidates.Count == 0) // 후보가 없는지 확인
            {
                return null; // 선택 불가 반환
            }

            return candidates[random.Next(candidates.Count)]; // 후보 중 무작위 선택
        }

        private static bool HasExitDirection(RoomTemplate template, CardinalDirection direction) // 방향 출구 보유 확인
        {
            for (int i = 0; i < template.Exits.Count; i++) // 실제 출구 목록 전체 반복
            {
                if (template.Exits[i].Direction == direction) // 필요한 방향인지 확인
                {
                    return true; // 보유 확인 반환
                }
            }

            return false; // 미보유 반환
        }

        private static RoomExit FindFirstExit(RoomTemplate template, CardinalDirection direction) // 특정 방향의 첫 출구 조회
        {
            for (int i = 0; i < template.Exits.Count; i++) // 실제 출구 목록 전체 반복
            {
                RoomExit exit = template.Exits[i]; // 현재 출구 조회

                if (exit.Direction == direction) // 필요한 방향인지 확인
                {
                    return exit; // 실제 연결 출구 반환
                }
            }

            throw new InvalidOperationException($"RoomTemplate '{template.DefinitionId}'에 {direction} 출구가 없습니다."); // 방 선택 로직과 데이터 불일치
        }

        private static void RemoveFrontierIfEmpty(List<FrontierEntry> frontier, int frontierIndex, FrontierEntry current) // 빈 프론티어 정리
        {
            if (current.RemainingExits.Count == 0) // 현재 방의 출구를 모두 사용했는지 확인
            {
                frontier.RemoveAt(frontierIndex); // 프론티어에서 제거
            }
        }

        private static RoomNode FindFurthestLeaf(DungeonLayoutGraph graph, RoomNode entryNode) // 가장 먼 막다른 방 탐색
        {
            Dictionary<string, int> distanceByRoomId = new Dictionary<string, int> // 시작 방 거리 기록
            {
                { entryNode.RoomId, 0 }
            };

            Queue<RoomNode> queue = new Queue<RoomNode>(); // 너비 우선 탐색 대기열
            queue.Enqueue(entryNode); // 시작 방 등록

            RoomNode furthest = entryNode; // 현재 가장 먼 방
            int furthestDistance = 0; // 현재 가장 먼 거리

            while (queue.Count > 0) // 대기열이 빌 때까지 반복
            {
                RoomNode current = queue.Dequeue(); // 현재 방 조회
                int currentDistance = distanceByRoomId[current.RoomId]; // 현재 방 거리 조회

                foreach (KeyValuePair<CardinalDirection, RoomConnectionEdge> connection in current.Connections) // 연결된 방 전체 반복
                {
                    RoomNode neighbor = connection.Value.Neighbor; // 이웃 방 조회

                    if (distanceByRoomId.ContainsKey(neighbor.RoomId)) // 이미 방문한 방인지 확인
                    {
                        continue; // 재방문 생략
                    }

                    int neighborDistance = currentDistance + 1; // 이웃 방 거리 계산
                    distanceByRoomId[neighbor.RoomId] = neighborDistance; // 거리 기록
                    queue.Enqueue(neighbor); // 탐색 대기열에 등록

                    bool isLeaf = neighbor.Connections.Count == 1; // 막다른 방인지 확인

                    if (isLeaf && neighborDistance > furthestDistance) // 더 먼 막다른 방인지 확인
                    {
                        furthest = neighbor; // 가장 먼 방 갱신
                        furthestDistance = neighborDistance; // 가장 먼 거리 갱신
                    }
                }
            }

            return furthest; // 가장 먼 막다른 방 반환
        }

        private static string NewRoomId(RoomTemplate template, int index) // 방 식별자 생성
        {
            return $"{template.DefinitionId}_{index}"; // 정의 식별자와 번호 조합
        }

        private sealed class FrontierEntry // 아직 사용할 출구가 남은 방
        {
            public RoomNode Node { get; } // 대상 방 노드
            public List<RoomExit> RemainingExits { get; } // 아직 사용하지 않은 실제 출구 목록

            public FrontierEntry(RoomNode node, List<RoomExit> remainingExits) // 프론티어 항목 생성자
            {
                Node = node; // 방 노드 저장
                RemainingExits = remainingExits; // 남은 출구 저장
            }
        }
    }
}
