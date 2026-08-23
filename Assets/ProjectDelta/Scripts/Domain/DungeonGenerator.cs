using System; // Random 사용
using System.Collections.Generic; // 목록 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 29일차: 생성이 끝난 던전 층 결과. 그래프 자체(28일차 DungeonLayoutGraph)와,
    // 시작 방·계단을 놓을 방을 함께 묶어서 돌려준다.
    public sealed class GeneratedDungeon // 생성된 던전 층 결과
    {
        public DungeonLayoutGraph Layout { get; } // 완성된 방-방 연결 그래프
        public RoomNode EntryRoom { get; } // 플레이어가 시작하는 방
        public RoomNode StairsRoom { get; } // 다음 층 계단을 놓을 방 (시작 방에서 가장 먼 막다른 방)

        public GeneratedDungeon(DungeonLayoutGraph layout, RoomNode entryRoom, RoomNode stairsRoom) // 생성 결과 생성자
        {
            Layout = layout; // 그래프 저장
            EntryRoom = entryRoom; // 시작 방 저장
            StairsRoom = stairsRoom; // 계단 방 저장
        }
    }

    // 29일차: DungeonLayoutGraph(28일차)를 실제로 채우는 절차적 생성기.
    //
    // 지금 있는 방 콘텐츠(미로 방 10종, TestRoom_A/B)는 전부 경계 출구가 하나뿐이라, 실제로
    // 만들어지는 던전은 "시작 방 + 막다른 방 몇 개"처럼 작게 나온다. 그래도 알고리즘 자체는
    // 방마다 출구가 몇 개든 상관없이 나뭇가지형(트리) 던전을 만들도록 일반화해뒀다 - 나중에
    // 출구 여러 개짜리 방 콘텐츠가 늘어나면 같은 코드가 자연스럽게 갈림길도 만든다.
    //
    // 순환(고리)이 있는 던전은 다루지 않는다 - 계단을 "막다른 방"에 두면 도달 가능성이
    // 트리 구조상 항상 보장되기 때문에(기획서 요구사항), 오늘은 트리로 충분하다.
    public sealed class DungeonGenerator // 절차적 던전 층 생성기
    {
        private readonly Random random; // 생성에 쓰는 난수 발생기

        public DungeonGenerator(int seed) // 시드 기반 생성기 생성자
        {
            random = new Random(seed); // 시드로 난수 발생기 초기화
        }

        // entryTemplate: 시작 방 종류. roomPool: 시작 방 밖에서 배치에 쓸 수 있는 방 종류 목록.
        // targetRoomCount: 시작 방을 포함해 만들고 싶은 총 방 개수 (콘텐츠 부족 등으로 못 채우면 그보다 적게 끝날 수 있음).
        public GeneratedDungeon Generate(RoomTemplate entryTemplate, IReadOnlyList<RoomTemplate> roomPool, int targetRoomCount) // 던전 층 생성
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 빈 연결 그래프 생성
            RoomNode entryNode = graph.AddRoom(NewRoomId(entryTemplate, 0), entryTemplate.DefinitionId, GridPosition.Zero); // 원점에 시작 방 배치

            // 프론티어: 아직 뻗어나갈 수 있는(출구가 남은) 방 목록.
            List<FrontierEntry> frontier = new List<FrontierEntry> { new FrontierEntry(entryNode, new List<CardinalDirection>(entryTemplate.ExitDirections)) }; // 시작 방을 프론티어에 등록

            int createdCount = 1; // 지금까지 만든 방 개수 (시작 방 포함)
            int roomIdCounter = 1; // 다음에 만들 방 이름에 쓸 번호
            int maxAttempts = System.Math.Max(targetRoomCount * 20, 20); // 무한 루프 방지용 최대 시도 횟수
            int attempts = 0; // 지금까지 시도한 횟수

            while (createdCount < targetRoomCount && frontier.Count > 0 && attempts < maxAttempts) // 목표 방 개수에 도달하거나 더 뻗어날 곳이 없을 때까지 반복
            {
                attempts++; // 시도 횟수 누적

                int frontierIndex = random.Next(frontier.Count); // 프론티어 중 무작위로 하나 선택
                FrontierEntry current = frontier[frontierIndex]; // 선택된 프론티어 항목 조회

                if (current.RemainingDirections.Count == 0) // 이미 출구를 다 썼는지 확인
                {
                    frontier.RemoveAt(frontierIndex); // 프론티어에서 제거
                    continue; // 다음 시도로 이동
                }

                int directionIndex = random.Next(current.RemainingDirections.Count); // 남은 출구 중 무작위로 하나 선택
                CardinalDirection direction = current.RemainingDirections[directionIndex]; // 선택된 출구 방향
                current.RemainingDirections.RemoveAt(directionIndex); // 이 방향은 이번 시도로 소모됨 (성공하든 실패하든 재사용 안 함)

                GridPosition candidatePosition = current.Node.MacroCoordinate + GridMovement.GetDirectionDelta(direction); // 그 방향으로 한 칸 이동한 좌표 계산

                if (graph.TryGetRoomAt(candidatePosition, out _)) // 그 자리에 이미 다른 방이 있는지 확인
                {
                    continue; // 자리 충돌, 이 시도는 포기하고 다음 시도로 이동
                }

                CardinalDirection neededDirection = RoomGridLayout.GetOpposite(direction); // 새 방이 반대쪽에 갖고 있어야 할 출구 방향
                RoomTemplate nextTemplate = PickTemplateWithExit(roomPool, neededDirection); // 그 방향 출구를 가진 방 종류 무작위 선택

                if (nextTemplate == null) // 맞는 방 종류를 못 찾았는지 확인
                {
                    continue; // 이 방향으로는 배치 불가, 다음 시도로 이동
                }

                RoomNode newNode = graph.AddRoom(NewRoomId(nextTemplate, roomIdCounter), nextTemplate.DefinitionId, candidatePosition); // 새 방 노드 생성
                roomIdCounter++; // 다음 이름 번호 준비
                graph.Connect(current.Node, direction, newNode); // 두 방 연결

                List<CardinalDirection> newRemaining = new List<CardinalDirection>(nextTemplate.ExitDirections); // 새 방의 출구 목록 복사
                newRemaining.Remove(neededDirection); // 지금 막 연결에 쓴 방향은 더 뻗어날 곳에서 제외

                if (newRemaining.Count > 0) // 새 방에 더 뻗어날 출구가 남아있는지 확인
                {
                    frontier.Add(new FrontierEntry(newNode, newRemaining)); // 새 방을 프론티어에 등록
                }

                createdCount++; // 생성된 방 개수 누적

                if (current.RemainingDirections.Count == 0) // 방금 뻗어난 방의 출구를 다 썼는지 확인
                {
                    frontier.RemoveAt(frontierIndex); // 프론티어에서 제거
                }
            }

            RoomNode stairsRoom = FindFurthestLeaf(graph, entryNode); // 시작 방에서 가장 먼 막다른 방을 계단 위치로 선정
            return new GeneratedDungeon(graph, entryNode, stairsRoom); // 생성 결과 반환
        }

        // 필요한 방향의 출구를 가진 방 종류 하나를 무작위로 고른다. 없으면 null.
        private RoomTemplate PickTemplateWithExit(IReadOnlyList<RoomTemplate> roomPool, CardinalDirection requiredDirection) // 조건에 맞는 방 종류 선택
        {
            List<RoomTemplate> candidates = new List<RoomTemplate>(); // 조건에 맞는 후보 목록

            foreach (RoomTemplate template in roomPool) // 방 종류 후보 전체 반복
            {
                if (HasExitDirection(template, requiredDirection)) // 필요한 방향 출구 보유 확인
                {
                    candidates.Add(template); // 후보 목록에 추가
                }
            }

            if (candidates.Count == 0) // 후보가 하나도 없는지 확인
            {
                return null; // 선택 불가 반환
            }

            return candidates[random.Next(candidates.Count)]; // 후보 중 무작위 선택 반환
        }

        // IReadOnlyList<T>에는 Contains가 없어서(List<T>/IList<T> 전용), 직접 순회로 확인한다.
        private static bool HasExitDirection(RoomTemplate template, CardinalDirection direction) // 방 종류의 출구 방향 보유 확인
        {
            for (int i = 0; i < template.ExitDirections.Count; i++) // 출구 방향 목록 전체 반복
            {
                if (template.ExitDirections[i] == direction) // 찾는 방향과 일치하는지 확인
                {
                    return true; // 보유 확인 반환
                }
            }

            return false; // 미보유 반환
        }

        // 시작 방에서부터 너비 우선 탐색으로 가장 먼 막다른 방(연결이 1개뿐인 방)을 찾는다.
        private static RoomNode FindFurthestLeaf(DungeonLayoutGraph graph, RoomNode entryNode) // 가장 먼 막다른 방 탐색
        {
            Dictionary<string, int> distanceByRoomId = new Dictionary<string, int> { { entryNode.RoomId, 0 } }; // 시작 방까지 거리(0)로 초기화
            Queue<RoomNode> queue = new Queue<RoomNode>(); // 너비 우선 탐색 대기열
            queue.Enqueue(entryNode); // 시작 방을 대기열에 등록

            RoomNode furthest = entryNode; // 지금까지 가장 먼 방 (기본값: 시작 방)
            int furthestDistance = 0; // 지금까지 가장 먼 거리

            while (queue.Count > 0) // 대기열이 빌 때까지 반복
            {
                RoomNode current = queue.Dequeue(); // 대기열에서 하나 꺼냄
                int currentDistance = distanceByRoomId[current.RoomId]; // 현재 방까지 거리 조회

                foreach (KeyValuePair<CardinalDirection, RoomConnectionEdge> connection in current.Connections) // 현재 방의 모든 연결 반복
                {
                    RoomNode neighbor = connection.Value.Neighbor; // 연결된 이웃 방 조회

                    if (distanceByRoomId.ContainsKey(neighbor.RoomId)) // 이미 방문한 방인지 확인
                    {
                        continue; // 재방문 생략
                    }

                    int neighborDistance = currentDistance + 1; // 이웃 방까지 거리 계산
                    distanceByRoomId[neighbor.RoomId] = neighborDistance; // 거리 기록
                    queue.Enqueue(neighbor); // 대기열에 등록

                    bool isLeaf = neighbor.Connections.Count == 1; // 연결이 하나뿐인 막다른 방인지 확인

                    if (isLeaf && neighborDistance > furthestDistance) // 막다른 방이면서 지금까지보다 더 먼지 확인
                    {
                        furthest = neighbor; // 가장 먼 막다른 방 갱신
                        furthestDistance = neighborDistance; // 가장 먼 거리 갱신
                    }
                }
            }

            return furthest; // 최종적으로 가장 먼 막다른 방 반환 (막다른 방이 없으면 시작 방 그대로)
        }

        private static string NewRoomId(RoomTemplate template, int index) // 방 식별자 생성 (같은 정의를 여러 번 써도 겹치지 않게)
        {
            return $"{template.DefinitionId}_{index}"; // 정의 식별자 + 번호 조합 반환
        }

        // 프론티어 한 항목: 아직 뻗어날 수 있는 방과, 그 방이 아직 안 쓴 출구 방향 목록.
        private sealed class FrontierEntry // 프론티어 항목
        {
            public RoomNode Node { get; } // 대상 방 노드
            public List<CardinalDirection> RemainingDirections { get; } // 아직 안 쓴 출구 방향 목록

            public FrontierEntry(RoomNode node, List<CardinalDirection> remainingDirections) // 프론티어 항목 생성자
            {
                Node = node; // 방 노드 저장
                RemainingDirections = remainingDirections; // 남은 출구 방향 목록 저장
            }
        }
    }
}
