using System; // Random·Array 기능 사용
using System.Collections.Generic; // 목록·사전·집합 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public sealed class GeneratedDungeon // 생성된 던전 층 결과
    {
        private readonly List<RoomNode> mainPath; // 시작→계단 메인 경로
        private readonly List<RoomNode> branchRooms; // 메인 경로 밖 가지 방 전체
        private readonly List<RoomNode> deadEndCandidates; // 일반 막다른 방 후보
        private readonly List<RoomNode> specialRoomCandidates; // 특수 방 후보
        private readonly Dictionary<string, DungeonRoomRole> roomRoles; // 방 ID별 생성 역할

        public DungeonLayoutGraph Layout { get; } // 완성된 방-방 연결 그래프
        public RoomNode EntryRoom { get; } // 플레이어가 시작하는 방
        public RoomNode StairsRoom { get; } // 다음 층 계단을 놓을 방
        public IReadOnlyList<RoomNode> MainPath => mainPath; // 시작 방부터 계단 방까지의 메인 경로
        public IReadOnlyList<RoomNode> BranchRooms => branchRooms; // 가지 경로에 추가된 방 전체
        public IReadOnlyList<RoomNode> DeadEndCandidates => deadEndCandidates; // 일반 막다른 방 후보 목록
        public IReadOnlyList<RoomNode> SpecialRoomCandidates => specialRoomCandidates; // 특수 방 후보 목록
        public IReadOnlyDictionary<string, DungeonRoomRole> RoomRoles => roomRoles; // 방 역할 조회용 읽기 전용 사전
        public int TargetMainPathLength { get; } // 이번 Seed에서 선택된 목표 메인 경로 방 수
        public int TargetRoomCount { get; } // 설정 기반 생성의 전체 목표 방 수
        public bool UsesControlledMainPath => TargetMainPathLength > 0; // 설정 기반 생성 결과인지 확인
        public bool MainPathCompleted => !UsesControlledMainPath || mainPath.Count == TargetMainPathLength; // 목표 메인 경로 완성 여부
        public bool RoomCountTargetReached => TargetRoomCount <= 0 || Layout.AllRooms.Count == TargetRoomCount; // 전체 목표 방 수 도달 여부
        public string FailureReason { get; } // 생성 실패 원인, 성공 시 null

        public GeneratedDungeon(DungeonLayoutGraph layout, RoomNode entryRoom, RoomNode stairsRoom) // 이전 일차 호환 생성자
            : this(
                layout,
                entryRoom,
                stairsRoom,
                Array.Empty<RoomNode>(),
                Array.Empty<RoomNode>(),
                Array.Empty<RoomNode>(),
                Array.Empty<RoomNode>(),
                0,
                0,
                null)
        {
        }

        public GeneratedDungeon(
            DungeonLayoutGraph layout,
            RoomNode entryRoom,
            RoomNode stairsRoom,
            IReadOnlyList<RoomNode> generatedMainPath,
            int targetMainPathLength,
            string failureReason) // 32일차 호환 생성자
            : this(
                layout,
                entryRoom,
                stairsRoom,
                generatedMainPath,
                Array.Empty<RoomNode>(),
                Array.Empty<RoomNode>(),
                Array.Empty<RoomNode>(),
                targetMainPathLength,
                0,
                failureReason)
        {
        }

        public GeneratedDungeon(
            DungeonLayoutGraph layout,
            RoomNode entryRoom,
            RoomNode stairsRoom,
            IReadOnlyList<RoomNode> generatedMainPath,
            IReadOnlyList<RoomNode> generatedBranchRooms,
            IReadOnlyList<RoomNode> generatedDeadEnds,
            IReadOnlyList<RoomNode> generatedSpecialCandidates,
            int targetMainPathLength,
            int targetRoomCount,
            string failureReason) // 생성 결과 생성자
        {
            Layout = layout; // 그래프 저장
            EntryRoom = entryRoom; // 시작 방 저장
            StairsRoom = stairsRoom; // 계단 방 저장
            mainPath = generatedMainPath != null ? new List<RoomNode>(generatedMainPath) : new List<RoomNode>(); // 메인 경로 복사
            branchRooms = generatedBranchRooms != null ? new List<RoomNode>(generatedBranchRooms) : new List<RoomNode>(); // 가지 방 복사
            deadEndCandidates = generatedDeadEnds != null ? new List<RoomNode>(generatedDeadEnds) : new List<RoomNode>(); // 막다른 방 후보 복사
            specialRoomCandidates = generatedSpecialCandidates != null ? new List<RoomNode>(generatedSpecialCandidates) : new List<RoomNode>(); // 특수 방 후보 복사
            TargetMainPathLength = targetMainPathLength; // 목표 메인 경로 길이 저장
            TargetRoomCount = targetRoomCount; // 전체 목표 방 수 저장
            FailureReason = failureReason; // 실패 원인 저장
            roomRoles = BuildRoleMap(); // 생성 역할 사전 구축
        }

        public bool TryGetRoomRole(RoomNode room, out DungeonRoomRole role) // 방의 생성 역할 조회
        {
            if (room == null) // 방 존재 확인
            {
                role = default; // 기본값 지정
                return false; // 조회 실패
            }

            return roomRoles.TryGetValue(room.RoomId, out role); // 방 ID 기준 역할 조회
        }

        private Dictionary<string, DungeonRoomRole> BuildRoleMap() // 방 역할 사전 생성
        {
            Dictionary<string, DungeonRoomRole> roles = new Dictionary<string, DungeonRoomRole>(); // 새 역할 사전

            for (int i = 0; i < mainPath.Count; i++) // 메인 경로 전체 등록
            {
                roles[mainPath[i].RoomId] = DungeonRoomRole.MainPath; // 메인 경로 역할 지정
            }

            for (int i = 0; i < branchRooms.Count; i++) // 가지 방 전체 등록
            {
                roles[branchRooms[i].RoomId] = DungeonRoomRole.Branch; // 기본 가지 역할 지정
            }

            for (int i = 0; i < deadEndCandidates.Count; i++) // 일반 막다른 방 후보 등록
            {
                roles[deadEndCandidates[i].RoomId] = DungeonRoomRole.DeadEndCandidate; // 막다른 방 역할로 덮어쓰기
            }

            for (int i = 0; i < specialRoomCandidates.Count; i++) // 특수 방 후보 등록
            {
                roles[specialRoomCandidates[i].RoomId] = DungeonRoomRole.SpecialCandidate; // 특수 후보 역할로 덮어쓰기
            }

            return roles; // 완성된 역할 사전 반환
        }
    }

    public sealed class DungeonGenerator // 절차적 던전 층 생성기
    {
        private static readonly CardinalDirection[] LoopScanDirections =
        {
            CardinalDirection.North,
            CardinalDirection.East
        }; // 인접 관계를 한 번씩만 검사하기 위한 방향

        private readonly Random random; // 생성에 쓰는 난수 발생기

        public DungeonGenerator(int seed) // 시드 기반 생성기 생성자
        {
            random = new Random(seed); // 시드로 난수 발생기 초기화
        }

        public GeneratedDungeon Generate(RoomTemplate entryTemplate, IReadOnlyList<RoomTemplate> roomPool, DungeonGenerationSettings settings) // 설정 기반 던전 생성
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
                return CreateFailedMainPathResult(entryTemplate, targetMainPathLength, settings.TargetRoomCount); // 실패 상태 반환
            }

            return BuildDungeonWithBranches(plannedPath, roomPool, settings, targetMainPathLength); // 메인 경로·가지·루프 생성
        }

        public GeneratedDungeon Generate(RoomTemplate entryTemplate, IReadOnlyList<RoomTemplate> roomPool, int targetRoomCount) // 이전 일차 호환 던전 생성
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
                RoomTemplate nextTemplate = PickCompatibleTemplate(roomPool, currentExit); // 실제 출구 정렬까지 맞는 방 선택

                if (nextTemplate == null) // 연결 가능한 방이 없는지 확인
                {
                    RemoveFrontierIfEmpty(frontier, frontierIndex, current); // 필요 시 프론티어 정리
                    continue; // 다음 시도로 이동
                }

                RoomExit connectedExit = FindCompatibleExit(nextTemplate, currentExit); // 실제 연결에 사용할 정렬된 반대 출구 선택
                RoomNode newNode = graph.AddRoom(NewRoomId(nextTemplate, roomIdCounter), nextTemplate.DefinitionId, candidatePosition); // 새 방 노드 생성
                roomIdCounter++; // 다음 번호 준비
                graph.Connect(current.Node, currentExit, newNode, connectedExit); // 정확한 출구 쌍으로 연결

                List<RoomExit> newRemaining = new List<RoomExit>(nextTemplate.Exits); // 새 방 출구 목록 복사
                newRemaining.Remove(connectedExit); // 이번 연결에 사용한 정확한 출구 제거

                if (newRemaining.Count > 0) // 새 방에 사용할 수 있는 출구가 남았는지 확인
                {
                    frontier.Add(new FrontierEntry(newNode, newRemaining)); // 새 방을 프론티어에 등록
                }

                createdCount++; // 생성된 방 개수 누적
                RemoveFrontierIfEmpty(frontier, frontierIndex, current); // 현재 방의 남은 출구가 없으면 정리
            }

            RoomNode stairsRoom = FindFurthestLeaf(graph, entryNode); // 가장 먼 막다른 방을 계단 방으로 선정
            return new GeneratedDungeon(graph, entryNode, stairsRoom); // 기존 결과 반환
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
                outgoingExits.Remove(current.EntranceExit.Value); // 되돌아가는 정확한 입구 제외
            }

            Shuffle(outgoingExits); // Seed 기반 출구 순서 무작위화

            for (int exitIndex = 0; exitIndex < outgoingExits.Count; exitIndex++) // 가능한 출구 순회
            {
                RoomExit outgoingExit = outgoingExits[exitIndex]; // 이번 출구
                GridPosition candidatePosition = current.Coordinate + GridMovement.GetDirectionDelta(outgoingExit.Direction); // 다음 방 좌표 계산

                if (occupiedCoordinates.Contains(candidatePosition)) // 이미 사용 중인 좌표인지 확인
                {
                    continue; // 좌표 충돌이면 다른 출구 시도
                }

                CardinalDirection requiredEntranceDirection = RoomGridLayout.GetOpposite(outgoingExit.Direction); // 다음 방에 필요한 입구 방향
                List<RoomTemplate> templateCandidates = GetTemplatesWithExit(roomPool, requiredEntranceDirection); // 반대 방향 출구가 있는 방 수집
                Shuffle(templateCandidates); // Seed 기반 후보 순서 무작위화

                for (int templateIndex = 0; templateIndex < templateCandidates.Count; templateIndex++) // 방 종류 후보 순회
                {
                    RoomTemplate template = templateCandidates[templateIndex]; // 현재 후보
                    List<RoomExit> entranceCandidates = GetExitsInDirection(template, requiredEntranceDirection); // 실제 입구 후보 수집
                    Shuffle(entranceCandidates); // 같은 방향 출구 후보 순서 무작위화

                    for (int entranceIndex = 0; entranceIndex < entranceCandidates.Count; entranceIndex++) // 입구 후보 순회
                    {
                        RoomExit entranceExit = entranceCandidates[entranceIndex]; // 새 방에서 사용할 입구

                        if (!outgoingExit.CanConnectTo(entranceExit)) // 실제 출구 정렬 축 검사
                        {
                            continue; // 위치가 맞지 않는 출구는 사용하지 않음
                        }

                        plannedPath.Add(new PlannedRoom(template, candidatePosition, entranceExit, outgoingExit)); // 정확한 출구 쌍을 포함해 임시 경로 추가
                        occupiedCoordinates.Add(candidatePosition); // 임시 좌표 점유

                        if (TryExtendMainPath(plannedPath, occupiedCoordinates, roomPool, targetMainPathLength)) // 다음 단계 탐색
                        {
                            return true; // 목표 길이 완성
                        }

                        occupiedCoordinates.Remove(candidatePosition); // 실패한 좌표 되돌리기
                        plannedPath.RemoveAt(plannedPath.Count - 1); // 실패한 방 되돌리기
                    }
                }
            }

            return false; // 목표 길이까지 확장 불가
        }

        private GeneratedDungeon BuildDungeonWithBranches(
            List<PlannedRoom> plannedPath,
            IReadOnlyList<RoomTemplate> roomPool,
            DungeonGenerationSettings settings,
            int targetMainPathLength) // 메인 경로를 확정하고 가지·루프 추가
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 새 던전 그래프 생성
            List<RoomNode> mainPathNodes = new List<RoomNode>(); // 메인 경로 노드 목록
            List<RoomNode> branchRooms = new List<RoomNode>(); // 가지 방 목록
            List<RoomNode> deadEndCandidates = new List<RoomNode>(); // 일반 막다른 방 후보
            List<RoomNode> specialCandidates = new List<RoomNode>(); // 특수 방 후보
            Dictionary<string, RoomTemplate> templateByRoomId = new Dictionary<string, RoomTemplate>(); // 실제 방 노드와 템플릿 연결

            for (int i = 0; i < plannedPath.Count; i++) // 계획된 메인 경로를 실제 그래프로 등록
            {
                PlannedRoom plannedRoom = plannedPath[i]; // 현재 계획 방
                RoomNode node = graph.AddRoom(NewRoomId(plannedRoom.Template, i), plannedRoom.Template.DefinitionId, plannedRoom.Coordinate); // 방 노드 생성
                mainPathNodes.Add(node); // 메인 경로 등록
                templateByRoomId[node.RoomId] = plannedRoom.Template; // 템플릿 연결 기록

                if (i > 0) // 시작 방 이후인지 확인
                {
                    graph.Connect(
                        mainPathNodes[i - 1],
                        plannedRoom.ExitFromPreviousRoom.Value,
                        node,
                        plannedRoom.EntranceExit.Value); // 메인 경로 실제 출구 쌍 연결
                }
            }

            int roomIdCounter = mainPathNodes.Count; // 가지 방 ID 번호 시작
            GenerateBranches(
                graph,
                mainPathNodes,
                templateByRoomId,
                roomPool,
                settings,
                ref roomIdCounter,
                branchRooms,
                deadEndCandidates,
                specialCandidates); // 남은 목표 방 수만큼 가지 생성 시도

            GenerateLoops(graph, templateByRoomId, settings.LoopChance); // 인접한 미연결 방의 루프 연결 시도
            RevalidateEndCandidates(deadEndCandidates); // 루프로 막다른 상태가 사라진 일반 후보 제거
            RevalidateEndCandidates(specialCandidates); // 루프로 막다른 상태가 사라진 특수 후보 제거

            RoomNode entryNode = mainPathNodes[0]; // 첫 방을 시작 방으로 확정
            RoomNode stairsRoom = mainPathNodes[mainPathNodes.Count - 1]; // 메인 경로 마지막 방을 계단 방으로 유지
            return new GeneratedDungeon(
                graph,
                entryNode,
                stairsRoom,
                mainPathNodes,
                branchRooms,
                deadEndCandidates,
                specialCandidates,
                targetMainPathLength,
                settings.TargetRoomCount,
                null); // 전체 생성 결과 반환
        }

        private void GenerateBranches(
            DungeonLayoutGraph graph,
            List<RoomNode> mainPathNodes,
            Dictionary<string, RoomTemplate> templateByRoomId,
            IReadOnlyList<RoomTemplate> roomPool,
            DungeonGenerationSettings settings,
            ref int roomIdCounter,
            List<RoomNode> branchRooms,
            List<RoomNode> deadEndCandidates,
            List<RoomNode> specialCandidates) // 메인 경로 주변 가지 생성
        {
            if (graph.AllRooms.Count >= settings.TargetRoomCount) // 이미 전체 목표 방 수에 도달했는지 확인
            {
                return; // 가지 생성 불필요
            }

            List<BranchStartCandidate> starts = new List<BranchStartCandidate>(); // 메인 경로의 미사용 출구 후보

            for (int i = 0; i < mainPathNodes.Count - 1; i++) // 계단 방을 제외한 메인 경로 순회
            {
                RoomNode sourceNode = mainPathNodes[i]; // 가지 시작 후보 방
                RoomTemplate sourceTemplate = templateByRoomId[sourceNode.RoomId]; // 해당 방 템플릿
                List<RoomExit> unusedExits = GetUnusedExits(sourceNode, sourceTemplate); // 메인 경로에서 사용하지 않은 출구 수집

                for (int exitIndex = 0; exitIndex < unusedExits.Count; exitIndex++) // 미사용 출구 전체 등록
                {
                    starts.Add(new BranchStartCandidate(sourceNode, unusedExits[exitIndex])); // 가지 시작 후보 추가
                }
            }

            Shuffle(starts); // Seed 기반 가지 시작 순서 무작위화

            for (int startIndex = 0; startIndex < starts.Count; startIndex++) // 가지 시작 후보 순회
            {
                if (graph.AllRooms.Count >= settings.TargetRoomCount) // 목표 방 수 도달 확인
                {
                    break; // 추가 가지 생성 중단
                }

                BranchStartCandidate start = starts[startIndex]; // 현재 가지 시작 후보

                if (start.SourceNode.Connections.ContainsKey(start.SourceExit.Direction)) // 다른 연결이 이미 같은 방향을 사용했는지 확인
                {
                    continue; // 이미 사용된 방향이면 생략
                }

                if (random.NextDouble() > settings.BranchChance) // 분기 확률 판정
                {
                    continue; // 이번 출구에서 가지 생성 안 함
                }

                int remainingRooms = settings.TargetRoomCount - graph.AllRooms.Count; // 남은 전체 방 수 계산
                int maxLength = Math.Min(settings.MaxBranchLength, remainingRooms); // 현재 남은 방 수에 맞춰 최대 가지 길이 제한

                if (maxLength < settings.MinBranchLength) // 최소 가지 길이조차 만들 수 없는지 확인
                {
                    break; // 남은 방 수가 부족하면 종료
                }

                int desiredLength = random.Next(settings.MinBranchLength, maxLength + 1); // 이번 가지 목표 길이 결정
                HashSet<GridPosition> occupied = CollectOccupiedCoordinates(graph); // 현재 그래프 전체 좌표 수집
                List<PlannedRoom> plannedBranch = new List<PlannedRoom>(); // 그래프 확정 전 가지 계획

                bool branchPlanned = TryPlanBranch(
                    start.SourceNode.MacroCoordinate,
                    start.SourceExit,
                    roomPool,
                    desiredLength,
                    occupied,
                    plannedBranch); // 충돌 없는 가지 경로 계획

                if (!branchPlanned) // 해당 출구에서 원하는 가지를 만들 수 없는지 확인
                {
                    continue; // 다른 시작 후보로 이동
                }

                RoomNode previousNode = start.SourceNode; // 가지 연결 시작 방

                for (int branchIndex = 0; branchIndex < plannedBranch.Count; branchIndex++) // 계획된 가지를 실제 그래프에 확정
                {
                    PlannedRoom plannedRoom = plannedBranch[branchIndex]; // 현재 가지 방 계획
                    RoomNode newNode = graph.AddRoom(NewRoomId(plannedRoom.Template, roomIdCounter), plannedRoom.Template.DefinitionId, plannedRoom.Coordinate); // 가지 방 생성
                    roomIdCounter++; // 다음 방 ID 번호 증가
                    graph.Connect(
                        previousNode,
                        plannedRoom.ExitFromPreviousRoom.Value,
                        newNode,
                        plannedRoom.EntranceExit.Value); // 정확한 출구 쌍으로 가지 연결
                    branchRooms.Add(newNode); // 가지 방 목록 등록
                    templateByRoomId[newNode.RoomId] = plannedRoom.Template; // 템플릿 기록
                    previousNode = newNode; // 다음 연결 기준 갱신
                }

                ClassifyBranchEnd(previousNode, settings, deadEndCandidates, specialCandidates); // 가지 마지막 방 후보 역할 지정
            }
        }

        private bool TryPlanBranch(
            GridPosition sourceCoordinate,
            RoomExit sourceExit,
            IReadOnlyList<RoomTemplate> roomPool,
            int targetLength,
            HashSet<GridPosition> occupied,
            List<PlannedRoom> plannedBranch) // 하나의 가지 경로를 그래프 밖에서 계획
        {
            GridPosition candidatePosition = sourceCoordinate + GridMovement.GetDirectionDelta(sourceExit.Direction); // 다음 가지 방 좌표

            if (occupied.Contains(candidatePosition)) // 기존 메인·가지 방과 좌표 충돌 확인
            {
                return false; // 충돌이면 해당 경로 사용 불가
            }

            CardinalDirection requiredEntranceDirection = RoomGridLayout.GetOpposite(sourceExit.Direction); // 새 방에 필요한 입구 방향
            List<RoomTemplate> templateCandidates = GetTemplatesWithExit(roomPool, requiredEntranceDirection); // 연결 가능한 방 종류 수집
            Shuffle(templateCandidates); // Seed 기반 후보 순서 무작위화

            for (int templateIndex = 0; templateIndex < templateCandidates.Count; templateIndex++) // 방 후보 순회
            {
                RoomTemplate template = templateCandidates[templateIndex]; // 현재 방 후보
                List<RoomExit> entranceCandidates = GetExitsInDirection(template, requiredEntranceDirection); // 실제 입구 후보 수집
                Shuffle(entranceCandidates); // 입구 후보 순서 무작위화

                for (int entranceIndex = 0; entranceIndex < entranceCandidates.Count; entranceIndex++) // 입구 후보 순회
                {
                    RoomExit entranceExit = entranceCandidates[entranceIndex]; // 이번 입구

                    if (!sourceExit.CanConnectTo(entranceExit)) // 실제 출구 정렬 축 검사
                    {
                        continue; // 위치가 맞지 않는 출구는 사용하지 않음
                    }

                    PlannedRoom plannedRoom = new PlannedRoom(template, candidatePosition, entranceExit, sourceExit); // 정확한 출구 쌍을 보존한 가지 계획
                    plannedBranch.Add(plannedRoom); // 임시 가지에 추가
                    occupied.Add(candidatePosition); // 임시 좌표 점유

                    if (plannedBranch.Count >= targetLength) // 목표 가지 길이에 도달했는지 확인
                    {
                        return true; // 가지 계획 완료
                    }

                    List<RoomExit> outgoingExits = new List<RoomExit>(template.Exits); // 다음 방으로 나갈 출구 후보
                    outgoingExits.Remove(entranceExit); // 들어온 입구 제외
                    Shuffle(outgoingExits); // Seed 기반 순서 무작위화

                    for (int exitIndex = 0; exitIndex < outgoingExits.Count; exitIndex++) // 다음 출구 후보 순회
                    {
                        if (TryPlanBranch(
                            candidatePosition,
                            outgoingExits[exitIndex],
                            roomPool,
                            targetLength,
                            occupied,
                            plannedBranch)) // 다음 가지 방 재귀 계획
                        {
                            return true; // 목표 길이 완성
                        }
                    }

                    occupied.Remove(candidatePosition); // 실패한 방 좌표 되돌리기
                    plannedBranch.RemoveAt(plannedBranch.Count - 1); // 실패한 방 계획 되돌리기
                }
            }

            return false; // 목표 길이 가지 생성 불가
        }

        private void GenerateLoops(
            DungeonLayoutGraph graph,
            Dictionary<string, RoomTemplate> templateByRoomId,
            double loopChance) // 이미 존재하는 인접 방 사이에 선택적 루프 연결
        {
            if (loopChance <= 0d) // 루프 기능 비활성 확인
            {
                return; // 추가 연결 없음
            }

            List<RoomNode> rooms = new List<RoomNode>(graph.AllRooms); // 안정적인 순회를 위해 방 목록 복사
            rooms.Sort((left, right) => string.CompareOrdinal(left.RoomId, right.RoomId)); // 동일 Seed 재현을 위한 순서 고정

            for (int roomIndex = 0; roomIndex < rooms.Count; roomIndex++) // 전체 방 순회
            {
                RoomNode room = rooms[roomIndex]; // 현재 방

                if (!templateByRoomId.TryGetValue(room.RoomId, out RoomTemplate roomTemplate)) // 현재 방 템플릿 확인
                {
                    continue; // 템플릿 정보가 없으면 루프 검사 불가
                }

                for (int directionIndex = 0; directionIndex < LoopScanDirections.Length; directionIndex++) // North·East만 검사해 중복 탐색 방지
                {
                    CardinalDirection direction = LoopScanDirections[directionIndex]; // 현재 검사 방향

                    if (room.Connections.ContainsKey(direction)) // 이미 해당 방향에 연결이 있는지 확인
                    {
                        continue; // 기존 연결 보호
                    }

                    GridPosition neighborCoordinate = room.MacroCoordinate + GridMovement.GetDirectionDelta(direction); // 인접 좌표 계산

                    if (!graph.TryGetRoomAt(neighborCoordinate, out RoomNode neighbor)) // 실제 인접 방 존재 확인
                    {
                        continue; // 방이 없으면 루프 후보 아님
                    }

                    CardinalDirection opposite = RoomGridLayout.GetOpposite(direction); // 이웃 방 기준 반대 방향 계산

                    if (neighbor.Connections.ContainsKey(opposite)) // 이웃 방의 대응 방향 사용 여부 확인
                    {
                        continue; // 기존 연결 보호
                    }

                    if (!templateByRoomId.TryGetValue(neighbor.RoomId, out RoomTemplate neighborTemplate)) // 이웃 방 템플릿 확인
                    {
                        continue; // 템플릿 정보 없으면 연결 불가
                    }

                    if (!TryFindCompatibleExitPair(
                        roomTemplate,
                        direction,
                        neighborTemplate,
                        opposite,
                        out RoomExit roomExit,
                        out RoomExit neighborExit)) // 양쪽 실제 출구 위치 정렬 확인
                    {
                        continue; // 방향은 맞아도 문 위치가 어긋나면 연결 금지
                    }

                    if (random.NextDouble() > loopChance) // 루프 확률 판정
                    {
                        continue; // 이번 인접 관계는 연결하지 않음
                    }

                    graph.TryConnect(room, roomExit, neighbor, neighborExit); // 중복·좌표·출구 규칙을 다시 검증하며 안전하게 연결
                }
            }
        }

        private static bool TryFindCompatibleExitPair(
            RoomTemplate fromTemplate,
            CardinalDirection fromDirection,
            RoomTemplate toTemplate,
            CardinalDirection toDirection,
            out RoomExit fromExit,
            out RoomExit toExit) // 두 방의 실제 연결 가능한 출구 쌍 검색
        {
            for (int fromIndex = 0; fromIndex < fromTemplate.Exits.Count; fromIndex++) // 시작 방 출구 순회
            {
                RoomExit candidateFrom = fromTemplate.Exits[fromIndex]; // 시작 방 출구 후보

                if (candidateFrom.Direction != fromDirection) // 필요한 방향인지 확인
                {
                    continue; // 다른 방향 생략
                }

                for (int toIndex = 0; toIndex < toTemplate.Exits.Count; toIndex++) // 이웃 방 출구 순회
                {
                    RoomExit candidateTo = toTemplate.Exits[toIndex]; // 이웃 방 출구 후보

                    if (candidateTo.Direction != toDirection) // 필요한 반대 방향인지 확인
                    {
                        continue; // 다른 방향 생략
                    }

                    if (!candidateFrom.CanConnectTo(candidateTo)) // 실제 정렬 축 일치 확인
                    {
                        continue; // 문 위치가 맞지 않으면 생략
                    }

                    fromExit = candidateFrom; // 연결 가능한 시작 출구 반환
                    toExit = candidateTo; // 연결 가능한 이웃 출구 반환
                    return true; // 출구 쌍 검색 성공
                }
            }

            fromExit = default; // 실패 기본값
            toExit = default; // 실패 기본값
            return false; // 연결 가능한 출구 쌍 없음
        }

        private void ClassifyBranchEnd(
            RoomNode branchEnd,
            DungeonGenerationSettings settings,
            List<RoomNode> deadEndCandidates,
            List<RoomNode> specialCandidates) // 가지 마지막 방 역할 지정
        {
            if (branchEnd == null) // 가지 끝 방 존재 확인
            {
                return; // 지정할 방 없음
            }

            if (random.NextDouble() <= settings.SpecialCandidateChance) // 특수 방 후보 확률 판정
            {
                specialCandidates.Add(branchEnd); // 특수 방 후보 등록
                return; // 일반 막다른 후보에는 중복 등록하지 않음
            }

            deadEndCandidates.Add(branchEnd); // 일반 막다른 방 후보 등록
        }

        private static void RevalidateEndCandidates(List<RoomNode> candidates) // 루프 연결 이후 막다른 방 후보 재검사
        {
            for (int i = candidates.Count - 1; i >= 0; i--) // 뒤에서부터 안전하게 제거
            {
                RoomNode room = candidates[i]; // 현재 후보

                if (room == null || room.Connections.Count != 1) // 더 이상 막다른 방이 아닌지 확인
                {
                    candidates.RemoveAt(i); // 후보 목록에서 제거
                }
            }
        }

        private RoomTemplate PickCompatibleTemplate(IReadOnlyList<RoomTemplate> roomPool, RoomExit sourceExit) // 실제 출구 위치까지 맞는 방 선택
        {
            CardinalDirection requiredDirection = RoomGridLayout.GetOpposite(sourceExit.Direction); // 필요한 반대 방향
            List<RoomTemplate> candidates = GetTemplatesWithExit(roomPool, requiredDirection); // 우선 방향 후보 수집

            for (int i = candidates.Count - 1; i >= 0; i--) // 정렬 불가능한 템플릿 제거
            {
                if (!HasCompatibleExit(candidates[i], sourceExit)) // 실제 연결 가능한 출구 존재 확인
                {
                    candidates.RemoveAt(i); // 정렬 불가 후보 제거
                }
            }

            if (candidates.Count == 0) // 최종 후보가 없는지 확인
            {
                return null; // 선택 불가
            }

            return candidates[random.Next(candidates.Count)]; // 정렬 가능한 후보 중 무작위 선택
        }

        private static bool HasCompatibleExit(RoomTemplate template, RoomExit sourceExit) // 템플릿에 실제 연결 가능한 출구가 있는지 확인
        {
            for (int i = 0; i < template.Exits.Count; i++) // 출구 전체 순회
            {
                if (sourceExit.CanConnectTo(template.Exits[i])) // 방향과 정렬 축이 모두 맞는지 확인
                {
                    return true; // 호환 출구 존재
                }
            }

            return false; // 호환 출구 없음
        }

        private static RoomExit FindCompatibleExit(RoomTemplate template, RoomExit sourceExit) // 실제 연결 가능한 첫 출구 조회
        {
            for (int i = 0; i < template.Exits.Count; i++) // 출구 전체 순회
            {
                RoomExit exit = template.Exits[i]; // 현재 출구

                if (sourceExit.CanConnectTo(exit)) // 실제 정렬 가능 여부 확인
                {
                    return exit; // 호환 출구 반환
                }
            }

            throw new InvalidOperationException($"RoomTemplate '{template.DefinitionId}'에 {sourceExit}과 연결 가능한 출구가 없습니다."); // 선택 로직과 데이터 불일치
        }

        private static List<RoomExit> GetUnusedExits(RoomNode node, RoomTemplate template) // 그래프에서 아직 사용하지 않은 출구 조회
        {
            List<RoomExit> exits = new List<RoomExit>(); // 미사용 출구 목록

            for (int i = 0; i < template.Exits.Count; i++) // 템플릿 출구 전체 순회
            {
                RoomExit exit = template.Exits[i]; // 현재 출구

                if (!node.Connections.ContainsKey(exit.Direction)) // 해당 방향이 아직 연결되지 않았는지 확인
                {
                    exits.Add(exit); // 가지 시작 후보 등록
                }
            }

            return exits; // 미사용 출구 반환
        }

        private static HashSet<GridPosition> CollectOccupiedCoordinates(DungeonLayoutGraph graph) // 현재 그래프의 전체 방 좌표 수집
        {
            HashSet<GridPosition> occupied = new HashSet<GridPosition>(); // 좌표 집합 생성

            foreach (RoomNode room in graph.AllRooms) // 전체 방 순회
            {
                occupied.Add(room.MacroCoordinate); // 방 좌표 등록
            }

            return occupied; // 전체 점유 좌표 반환
        }

        private static GeneratedDungeon CreateFailedMainPathResult(RoomTemplate entryTemplate, int targetMainPathLength, int targetRoomCount) // 메인 경로 실패 결과 생성
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph(); // 실패 결과용 최소 그래프
            RoomNode entryNode = graph.AddRoom(NewRoomId(entryTemplate, 0), entryTemplate.DefinitionId, GridPosition.Zero); // 시작 방만 등록
            List<RoomNode> partialMainPath = new List<RoomNode> { entryNode }; // 최소 경로
            string reason = $"목표 메인 경로 {targetMainPathLength}개 방을 연결할 수 없습니다."; // 실패 원인
            return new GeneratedDungeon(
                graph,
                entryNode,
                entryNode,
                partialMainPath,
                Array.Empty<RoomNode>(),
                Array.Empty<RoomNode>(),
                Array.Empty<RoomNode>(),
                targetMainPathLength,
                targetRoomCount,
                reason); // 실패 결과 반환
        }

        private RoomTemplate PickTemplateWithExit(IReadOnlyList<RoomTemplate> roomPool, CardinalDirection requiredDirection) // 이전 생성 방식 호환용 방향 기반 방 선택
        {
            List<RoomTemplate> candidates = GetTemplatesWithExit(roomPool, requiredDirection); // 조건에 맞는 후보 수집

            if (candidates.Count == 0) // 후보가 없는지 확인
            {
                return null; // 선택 불가
            }

            return candidates[random.Next(candidates.Count)]; // 무작위 후보 반환
        }

        private static List<RoomTemplate> GetTemplatesWithExit(IReadOnlyList<RoomTemplate> roomPool, CardinalDirection requiredDirection) // 필요한 방향 출구를 가진 모든 방 수집
        {
            List<RoomTemplate> candidates = new List<RoomTemplate>(); // 후보 목록

            if (roomPool == null) // 방 후보 목록 존재 확인
            {
                return candidates; // 빈 목록 반환
            }

            for (int i = 0; i < roomPool.Count; i++) // 방 후보 전체 순회
            {
                RoomTemplate template = roomPool[i]; // 현재 후보

                if (template != null && HasExitDirection(template, requiredDirection)) // 필요한 방향 출구 보유 확인
                {
                    candidates.Add(template); // 후보 등록
                }
            }

            return candidates; // 후보 반환
        }

        private static List<RoomExit> GetExitsInDirection(RoomTemplate template, CardinalDirection direction) // 특정 방향 실제 출구 전체 조회
        {
            List<RoomExit> exits = new List<RoomExit>(); // 출구 목록

            for (int i = 0; i < template.Exits.Count; i++) // 실제 출구 전체 순회
            {
                RoomExit exit = template.Exits[i]; // 현재 출구

                if (exit.Direction == direction) // 필요한 방향인지 확인
                {
                    exits.Add(exit); // 후보 등록
                }
            }

            return exits; // 출구 후보 반환
        }

        private static bool HasExitDirection(RoomTemplate template, CardinalDirection direction) // 방향 출구 보유 확인
        {
            for (int i = 0; i < template.Exits.Count; i++) // 실제 출구 전체 순회
            {
                if (template.Exits[i].Direction == direction) // 필요한 방향인지 확인
                {
                    return true; // 보유 확인
                }
            }

            return false; // 미보유
        }

        private void Shuffle<T>(List<T> items) // 현재 Seed의 Random을 이용해 목록 순서 섞기
        {
            for (int i = items.Count - 1; i > 0; i--) // Fisher-Yates 셔플
            {
                int swapIndex = random.Next(i + 1); // 교환 위치 선택
                T temp = items[i]; // 현재 값 임시 저장
                items[i] = items[swapIndex]; // 선택 값 이동
                items[swapIndex] = temp; // 현재 값 이동
            }
        }

        private static void RemoveFrontierIfEmpty(List<FrontierEntry> frontier, int frontierIndex, FrontierEntry current) // 빈 프론티어 정리
        {
            if (current.RemainingExits.Count == 0) // 현재 방 출구를 모두 사용했는지 확인
            {
                frontier.RemoveAt(frontierIndex); // 프론티어 제거
            }
        }

        private static RoomNode FindFurthestLeaf(DungeonLayoutGraph graph, RoomNode entryNode) // 가장 먼 막다른 방 탐색
        {
            Dictionary<string, int> distanceByRoomId = new Dictionary<string, int> // 시작 방 거리 기록
            {
                { entryNode.RoomId, 0 }
            };

            Queue<RoomNode> queue = new Queue<RoomNode>(); // BFS 대기열
            queue.Enqueue(entryNode); // 시작 방 등록

            RoomNode furthest = entryNode; // 현재 가장 먼 방
            int furthestDistance = 0; // 현재 가장 먼 거리

            while (queue.Count > 0) // 대기열이 빌 때까지 반복
            {
                RoomNode current = queue.Dequeue(); // 현재 방
                int currentDistance = distanceByRoomId[current.RoomId]; // 현재 거리

                foreach (KeyValuePair<CardinalDirection, RoomConnectionEdge> connection in current.Connections) // 연결 전체 순회
                {
                    RoomNode neighbor = connection.Value.Neighbor; // 이웃 방

                    if (distanceByRoomId.ContainsKey(neighbor.RoomId)) // 이미 방문했는지 확인
                    {
                        continue; // 재방문 생략
                    }

                    int neighborDistance = currentDistance + 1; // 이웃 거리 계산
                    distanceByRoomId[neighbor.RoomId] = neighborDistance; // 거리 기록
                    queue.Enqueue(neighbor); // 탐색 등록

                    bool isLeaf = neighbor.Connections.Count == 1; // 막다른 방인지 확인

                    if (isLeaf && neighborDistance > furthestDistance) // 더 먼 막다른 방인지 확인
                    {
                        furthest = neighbor; // 가장 먼 방 갱신
                        furthestDistance = neighborDistance; // 거리 갱신
                    }
                }
            }

            return furthest; // 가장 먼 막다른 방 반환
        }

        private static string NewRoomId(RoomTemplate template, int index) // 방 식별자 생성
        {
            return $"{template.DefinitionId}_{index}"; // 정의 ID와 번호 조합
        }

        private sealed class FrontierEntry // 이전 방식 호환 프론티어 항목
        {
            public RoomNode Node { get; } // 대상 방 노드
            public List<RoomExit> RemainingExits { get; } // 아직 사용하지 않은 출구

            public FrontierEntry(RoomNode node, List<RoomExit> remainingExits) // 생성자
            {
                Node = node; // 방 노드 저장
                RemainingExits = remainingExits; // 남은 출구 저장
            }
        }

        private sealed class PlannedRoom // 그래프 확정 전 경로 방 하나
        {
            public RoomTemplate Template { get; } // 사용할 방 종류
            public GridPosition Coordinate { get; } // 던전 매크로 좌표
            public RoomExit? EntranceExit { get; } // 현재 방에서 이전 방과 연결된 실제 입구
            public RoomExit? ExitFromPreviousRoom { get; } // 이전 방에서 현재 방으로 나온 실제 출구

            public PlannedRoom(
                RoomTemplate template,
                GridPosition coordinate,
                RoomExit? entranceExit,
                RoomExit? exitFromPreviousRoom) // 계획 방 생성자
            {
                Template = template; // 방 종류 저장
                Coordinate = coordinate; // 좌표 저장
                EntranceExit = entranceExit; // 현재 방 입구 저장
                ExitFromPreviousRoom = exitFromPreviousRoom; // 이전 방 실제 출구 저장
            }
        }

        private sealed class BranchStartCandidate // 메인 경로의 가지 시작 후보
        {
            public RoomNode SourceNode { get; } // 가지가 시작될 메인 경로 방
            public RoomExit SourceExit { get; } // 사용할 미사용 출구

            public BranchStartCandidate(RoomNode sourceNode, RoomExit sourceExit) // 가지 시작 후보 생성자
            {
                SourceNode = sourceNode; // 시작 방 저장
                SourceExit = sourceExit; // 시작 출구 저장
            }
        }
    }
}
