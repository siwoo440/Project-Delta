using System; // Random·Array 기능 사용
using System.Collections.Generic; // 목록·사전·집합 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 생성이 끝난 던전 층 결과.
    public sealed class GeneratedDungeon // 생성된 던전 층 결과
    {
        private readonly List<RoomNode> mainPath; // 명시적으로 생성된 시작→계단 메인 경로

        public DungeonLayoutGraph Layout { get; } // 완성된 방-방 연결 그래프
        public RoomNode EntryRoom { get; } // 플레이어가 시작하는 방
        public RoomNode StairsRoom { get; } // 다음 층 계단을 놓을 방
        public IReadOnlyList<RoomNode> MainPath => mainPath; // 32일차: 시작 방부터 계단 방까지의 메인 경로
        public int TargetMainPathLength { get; } // 32일차: 이번 Seed에서 선택된 목표 메인 경로 방 수
        public bool UsesControlledMainPath => TargetMainPathLength > 0; // 32일차 설정 기반 생성 결과인지 확인
        public bool MainPathCompleted => !UsesControlledMainPath || mainPath.Count == TargetMainPathLength; // 목표 메인 경로 완성 여부
        public string FailureReason { get; } // 메인 경로 생성 실패 원인, 성공 시 null

        public GeneratedDungeon(DungeonLayoutGraph layout, RoomNode entryRoom, RoomNode stairsRoom) // 기존 생성 결과 생성자
            : this(layout, entryRoom, stairsRoom, Array.Empty<RoomNode>(), 0, null)
        {
        }

        public GeneratedDungeon(
            DungeonLayoutGraph layout,
            RoomNode entryRoom,
            RoomNode stairsRoom,
            IReadOnlyList<RoomNode> generatedMainPath,
            int targetMainPathLength,
            string failureReason) // 32일차 메인 경로 정보를 포함한 생성 결과 생성자
        {
            Layout = layout; // 그래프 저장
            EntryRoom = entryRoom; // 시작 방 저장
            StairsRoom = stairsRoom; // 계단 방 저장
            mainPath = generatedMainPath != null ? new List<RoomNode>(generatedMainPath) : new List<RoomNode>(); // 메인 경로 복사
            TargetMainPathLength = targetMainPathLength; // 목표 길이 저장
            FailureReason = failureReason; // 실패 원인 저장
        }
    }

    // DungeonLayoutGraph를 채우는 절차적 생성기.
    // 기존 Generate(targetRoomCount)는 이전 일차 호환용으로 유지한다.
    // 32일차부터 DungeonGenerationSettings 오버로드는 먼저 메인 경로를 계획하고 성공 후 그래프에 확정한다.
    public sealed class DungeonGenerator // 절차적 던전 층 생성기
    {
        private readonly Random random; // 생성에 쓰는 난수 발생기

        public DungeonGenerator(int seed) // 시드 기반 생성기 생성자
        {
            random = new Random(seed); // 시드로 난수 발생기 초기화
        }

        public GeneratedDungeon Generate(RoomTemplate entryTemplate, IReadOnlyList<RoomTemplate> roomPool, DungeonGenerationSettings settings) // 32일차 메인 경로 제어 생성
        {
            if (entryTemplate == null) // 시작 방 템플릿 존재 확인
            {
                throw new ArgumentNullException(nameof(entryTemplate)); // 시작 방 누락 차단
            }

            if (settings == null) // 생성 규칙 존재 확인
            {
                throw new ArgumentNullException(nameof(settings)); // 생성 규칙 누락 차단
            }

            int targetMainPathLength = random.Next(settings.MinMainPathLength, settings.MaxMainPathLength + 1); // 이번 Seed의 목표 메인 경로 길이 결정
            List<PlannedRoom> plannedPath = new List<PlannedRoom> // 그래프에 넣기 전 임시 메인 경로
            {
                new PlannedRoom(entryTemplate, GridPosition.Zero, null, null)
            };
            HashSet<GridPosition> occupiedCoordinates = new HashSet<GridPosition> // 임시 경로에서 이미 사용 중인 방 좌표
            {
                GridPosition.Zero
            };

            bool planned = TryExtendMainPath(plannedPath, occupiedCoordinates, roomPool, targetMainPathLength); // 정확한 목표 길이까지 경로 계획

            if (!planned) // 목표 길이 경로를 만들 수 없는지 확인
            {
                return CreateFailedMainPathResult(entryTemplate, targetMainPathLength); // 실패 상태를 명시한 결과 반환
            }

            return BuildDungeonFromMainPath(plannedPath, targetMainPathLength); // 성공한 계획만 실제 그래프로 확정
        }

        public GeneratedDungeon Generate(RoomTemplate entryTemplate, IReadOnlyList<RoomTemplate> roomPool, int targetRoomCount) // 기존 던전 층 생성
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

        private bool TryExtendMainPath(
            List<PlannedRoom> plannedPath,
            HashSet<GridPosition> occupiedCoordinates,
            IReadOnlyList<RoomTemplate> roomPool,
            int targetMainPathLength) // 목표 길이까지 충돌 없는 메인 경로 계획
        {
            if (plannedPath.Count >= targetMainPathLength) // 목표 길이에 도달했는지 확인
            {
                return true; // 메인 경로 계획 완료
            }

            PlannedRoom current = plannedPath[plannedPath.Count - 1]; // 현재 경로 끝 방
            List<RoomExit> outgoingExits = new List<RoomExit>(current.Template.Exits); // 현재 방 출구 후보 복사

            if (current.EntranceExit.HasValue) // 이전 방과 연결된 입구가 있는지 확인
            {
                outgoingExits.Remove(current.EntranceExit.Value); // 되돌아가는 데 사용된 정확한 입구 제외
            }

            Shuffle(outgoingExits); // Seed 기반 출구 순서 무작위화

            for (int exitIndex = 0; exitIndex < outgoingExits.Count; exitIndex++) // 현재 방의 가능한 출구 순회
            {
                RoomExit outgoingExit = outgoingExits[exitIndex]; // 이번에 사용할 출구
                GridPosition candidatePosition = current.Coordinate + GridMovement.GetDirectionDelta(outgoingExit.Direction); // 다음 방 좌표 계산

                if (occupiedCoordinates.Contains(candidatePosition)) // 이미 경로가 사용하는 좌표인지 확인
                {
                    continue; // 좌표 충돌이면 다른 출구 시도
                }

                CardinalDirection requiredEntranceDirection = RoomGridLayout.GetOpposite(outgoingExit.Direction); // 다음 방에 필요한 입구 방향
                List<RoomTemplate> templateCandidates = GetTemplatesWithExit(roomPool, requiredEntranceDirection); // 연결 가능한 방 종류 수집
                Shuffle(templateCandidates); // Seed 기반 방 종류 순서 무작위화

                for (int templateIndex = 0; templateIndex < templateCandidates.Count; templateIndex++) // 방 종류 후보 순회
                {
                    RoomTemplate template = templateCandidates[templateIndex]; // 현재 후보 방
                    List<RoomExit> entranceCandidates = GetExitsInDirection(template, requiredEntranceDirection); // 반대 방향 입구 후보 수집
                    Shuffle(entranceCandidates); // 같은 방향 출구가 여러 개면 Seed 기반 순서 적용

                    for (int entranceIndex = 0; entranceIndex < entranceCandidates.Count; entranceIndex++) // 실제 입구 후보 순회
                    {
                        RoomExit entranceExit = entranceCandidates[entranceIndex]; // 이번 연결에 사용할 새 방 입구

                        plannedPath.Add(new PlannedRoom(template, candidatePosition, entranceExit, outgoingExit.Direction)); // 임시 경로에 새 방 추가
                        occupiedCoordinates.Add(candidatePosition); // 임시 좌표 점유

                        if (TryExtendMainPath(plannedPath, occupiedCoordinates, roomPool, targetMainPathLength)) // 다음 방까지 재귀적으로 계획
                        {
                            return true; // 목표 길이 완성 시 성공 반환
                        }

                        occupiedCoordinates.Remove(candidatePosition); // 실패한 후보 좌표 되돌리기
                        plannedPath.RemoveAt(plannedPath.Count - 1); // 실패한 후보 방 되돌리기
                    }
                }
            }

            return false; // 현재 경로에서 목표 길이까지 확장 불가
        }

        private GeneratedDungeon BuildDungeonFromMainPath(List<PlannedRoom> plannedPath, int targetMainPathLength) // 계획된 메인 경로를 실제 그래프로 확정
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 새 던전 그래프 생성
            List<RoomNode> mainPathNodes = new List<RoomNode>(); // 확정된 메인 경로 노드 목록

            for (int i = 0; i < plannedPath.Count; i++) // 계획된 방을 순서대로 그래프에 등록
            {
                PlannedRoom plannedRoom = plannedPath[i]; // 현재 계획 방
                RoomNode node = graph.AddRoom(NewRoomId(plannedRoom.Template, i), plannedRoom.Template.DefinitionId, plannedRoom.Coordinate); // 실제 방 노드 생성
                mainPathNodes.Add(node); // 메인 경로 목록에 등록

                if (i > 0) // 시작 방 이후인지 확인
                {
                    CardinalDirection connectionDirection = plannedRoom.ConnectionDirectionFromPrevious.Value; // 이전 방에서 현재 방으로 향하는 방향
                    graph.Connect(mainPathNodes[i - 1], connectionDirection, node); // 메인 경로 앞뒤 방 연결
                }
            }

            RoomNode entryNode = mainPathNodes[0]; // 첫 방을 시작 방으로 확정
            RoomNode stairsRoom = mainPathNodes[mainPathNodes.Count - 1]; // 마지막 방을 계단 방으로 확정
            return new GeneratedDungeon(graph, entryNode, stairsRoom, mainPathNodes, targetMainPathLength, null); // 성공 결과 반환
        }

        private static GeneratedDungeon CreateFailedMainPathResult(RoomTemplate entryTemplate, int targetMainPathLength) // 메인 경로 계획 실패 결과 생성
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 실패 결과용 최소 그래프 생성
            RoomNode entryNode = graph.AddRoom(NewRoomId(entryTemplate, 0), entryTemplate.DefinitionId, GridPosition.Zero); // 시작 방만 등록
            List<RoomNode> partialMainPath = new List<RoomNode> { entryNode }; // 실패 상태에서 확정된 최소 경로
            string reason = $"목표 메인 경로 {targetMainPathLength}개 방을 연결할 수 없습니다."; // 실패 원인 기록
            return new GeneratedDungeon(graph, entryNode, entryNode, partialMainPath, targetMainPathLength, reason); // 재시도 가능한 실패 정보 반환
        }

        private RoomTemplate PickTemplateWithExit(IReadOnlyList<RoomTemplate> roomPool, CardinalDirection requiredDirection) // 필요한 방향 출구를 가진 방 선택
        {
            List<RoomTemplate> candidates = GetTemplatesWithExit(roomPool, requiredDirection); // 조건에 맞는 후보 목록 수집

            if (candidates.Count == 0) // 후보가 없는지 확인
            {
                return null; // 선택 불가 반환
            }

            return candidates[random.Next(candidates.Count)]; // 후보 중 무작위 선택
        }

        private static List<RoomTemplate> GetTemplatesWithExit(IReadOnlyList<RoomTemplate> roomPool, CardinalDirection requiredDirection) // 필요한 방향 출구를 가진 모든 방 수집
        {
            List<RoomTemplate> candidates = new List<RoomTemplate>(); // 조건에 맞는 후보 목록

            if (roomPool == null) // 방 후보 목록 존재 확인
            {
                return candidates; // 빈 후보 목록 반환
            }

            for (int i = 0; i < roomPool.Count; i++) // 방 종류 후보 전체 반복
            {
                RoomTemplate template = roomPool[i]; // 현재 방 종류

                if (template != null && HasExitDirection(template, requiredDirection)) // 필요한 방향 출구 보유 확인
                {
                    candidates.Add(template); // 후보 목록에 추가
                }
            }

            return candidates; // 수집된 후보 반환
        }

        private static List<RoomExit> GetExitsInDirection(RoomTemplate template, CardinalDirection direction) // 특정 방향의 실제 출구 전체 조회
        {
            List<RoomExit> exits = new List<RoomExit>(); // 같은 방향 출구 목록

            for (int i = 0; i < template.Exits.Count; i++) // 실제 출구 목록 전체 반복
            {
                RoomExit exit = template.Exits[i]; // 현재 출구 조회

                if (exit.Direction == direction) // 필요한 방향인지 확인
                {
                    exits.Add(exit); // 입구 후보 등록
                }
            }

            return exits; // 같은 방향 출구 목록 반환
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

        private void Shuffle<T>(List<T> items) // 현재 Seed의 Random을 이용해 목록 순서 섞기
        {
            for (int i = items.Count - 1; i > 0; i--) // 뒤에서부터 Fisher-Yates 셔플
            {
                int swapIndex = random.Next(i + 1); // 교환할 앞쪽 인덱스 선택
                T temp = items[i]; // 현재 항목 임시 저장
                items[i] = items[swapIndex]; // 선택 항목을 현재 위치로 이동
                items[swapIndex] = temp; // 현재 항목을 선택 위치로 이동
            }
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

        private sealed class PlannedRoom // 그래프 확정 전 메인 경로 방 하나
        {
            public RoomTemplate Template { get; } // 사용할 방 종류
            public GridPosition Coordinate { get; } // 던전 매크로 좌표
            public RoomExit? EntranceExit { get; } // 이전 방에서 들어온 실제 입구
            public CardinalDirection? ConnectionDirectionFromPrevious { get; } // 이전 방 기준 현재 방 방향

            public PlannedRoom(
                RoomTemplate template,
                GridPosition coordinate,
                RoomExit? entranceExit,
                CardinalDirection? connectionDirectionFromPrevious) // 계획 방 생성자
            {
                Template = template; // 방 종류 저장
                Coordinate = coordinate; // 좌표 저장
                EntranceExit = entranceExit; // 사용된 입구 저장
                ConnectionDirectionFromPrevious = connectionDirectionFromPrevious; // 이전 방 연결 방향 저장
            }
        }
    }
}
