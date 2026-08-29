using System;
using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data; // RoomDefinition 사용
using ProjectDelta.Domain; // 생성 그래프·Seed·통로 사용
using DungeonRunState = ProjectDelta.Domain.DungeonRunState; // Data/Domain 동명 타입 충돌 방지
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation
{
    public sealed class DungeonFloorController : MonoBehaviour
    {
        [Header("기존 자리표시자 방식 - 절차 생성 비활성 시 사용")]
        [SerializeField] private RoomView[] nextFloorRoomPrefabs;
        [SerializeField] private Vector3 floorOrigin = new Vector3(0f, 0f, 200f);
        [SerializeField] private Vector3 floorSpacing = new Vector3(200f, 0f, 0f);

        [Header("36일차 절차 생성 배치")]
        [SerializeField] private bool useProceduralGeneration;
        [SerializeField] private bool generateFirstFloorOnStart;
        [SerializeField] private DungeonRoomPrefabBinding[] roomBindings;
        [SerializeField] private PlayerGridMovementController playerController;
        [SerializeField] private Vector3 proceduralFloorOrigin = Vector3.zero;
        [SerializeField] private float roomWorldSize = 10f;

        [Header("생성 규칙")]
        // 110일차: 새 게임을 시작할 때마다 다른 던전이 나오도록 baseSeed를 무작위로 다시 뽑는다.
        // 끄면(QA 재현·밸런스 검증용) 아래 baseSeed 값을 그대로 고정해서 쓴다.
        [SerializeField] private bool randomizeSeedEachRun = true;
        [SerializeField] private int baseSeed = 3600;
        [SerializeField] private int maxGenerationAttempts = 10;
        [SerializeField] private int targetRoomCount = 8;
        [SerializeField] private int minMainPathLength = 6;
        [SerializeField] private int maxMainPathLength = 6;
        [SerializeField, Range(0f, 1f)] private float branchChance = 1f;
        [SerializeField] private int minBranchLength = 1;
        [SerializeField] private int maxBranchLength = 1;
        [SerializeField, Range(0f, 1f)] private float specialCandidateChance = 0.30f;
        [SerializeField, Range(0f, 1f)] private float loopChance = 0f;

        [Header("40일차 인카운터 배치")]
        [SerializeField] private EncounterDefinition defaultMonsterEncounter;

        // 78일차: 던전 전체가 defaultMonsterEncounter 하나만 쓰던 구조를 확장한다 - 이 배열에
        // 넣은 인카운터도 defaultMonsterEncounter와 함께 층마다(EncounterDefinition.IsAllowedOnFloor
        // 기준으로) 방 배정 대상이 된다.
        [SerializeField] private EncounterDefinition[] additionalFloorEncounters =
            new EncounterDefinition[0];

        private DungeonRunState dungeonState;
        private RoomView spawnedRoomView; // 기존 자리표시자 호환
        private Transform generatedFloorRoot;
        private DungeonGenerationRunResult currentGeneration;
        private DungeonEncounterLayout currentEncounterLayout =
            new DungeonEncounterLayout();
        private bool awakeCompleted;

        private readonly Dictionary<string, RoomView> spawnedRooms =
            new Dictionary<string, RoomView>();

        private readonly Dictionary<string, ExplorationMonsterMarker> spawnedMonsters =
            new Dictionary<string, ExplorationMonsterMarker>();

        private readonly Dictionary<string, DungeonRoomPrefabBinding> bindingsByDefinition =
            new Dictionary<string, DungeonRoomPrefabBinding>();

        public GeneratedDungeon CurrentDungeon => currentGeneration?.Dungeon;
        public int CurrentSuccessfulSeed => currentGeneration != null ? currentGeneration.SuccessfulSeed : 0;
        public IReadOnlyDictionary<string, RoomView> SpawnedRooms => spawnedRooms;
        public IReadOnlyDictionary<string, ExplorationMonsterMarker> SpawnedMonsters => spawnedMonsters;
        public DungeonEncounterLayout CurrentEncounterLayout => currentEncounterLayout;

        // 76일차: 몬스터 그룹 구성(RoomEncounterAssignment.MonsterDefinitionIds)은 ID 문자열만
        // 들고 있으므로, 실제 전투를 만들 때 그 ID로 MonsterDefinition 에셋을 다시 찾아야 한다.
        // 78일차: 던전이 defaultMonsterEncounter 하나가 아니라 여러 EncounterDefinition을
        // 쓰게 되면서, 이 층에 설정된 인카운터 전체(기본 몬스터 + 추가 후보 풀)를 뒤진다 -
        // 인카운터 종류가 더 늘어나면 DataRepository 기반 조회로 교체한다
        // (47~54일차 주석에 이미 예정돼 있던 방향).
        public bool TryFindMonsterDefinition(
            string monsterDefinitionId,
            out MonsterDefinition monsterDefinition)
        {
            monsterDefinition = null;

            if (string.IsNullOrEmpty(monsterDefinitionId))
            {
                return false;
            }

            List<EncounterDefinition> encounters =
                CollectFloorEncounters();

            for (int encounterIndex = 0; encounterIndex < encounters.Count; encounterIndex++)
            {
                EncounterDefinition encounter =
                    encounters[encounterIndex];

                if (encounter.Monster != null
                    && encounter.Monster.Id == monsterDefinitionId)
                {
                    monsterDefinition =
                        encounter.Monster;

                    return true;
                }

                EncounterMonsterEntry[] pool =
                    encounter.AdditionalMonsterPool;

                if (pool == null)
                {
                    continue;
                }

                for (int poolIndex = 0; poolIndex < pool.Length; poolIndex++)
                {
                    if (pool[poolIndex] != null
                        && pool[poolIndex].Monster != null
                        && pool[poolIndex].Monster.Id == monsterDefinitionId)
                    {
                        monsterDefinition =
                            pool[poolIndex].Monster;

                        return true;
                    }
                }
            }

            return false;
        }

        private void Awake()
        {
            GetDungeonState();

            if (useProceduralGeneration)
            {
                // 110일차: 불러오기(RestoreAndPlaceCurrentFloor)는 저장된 실제 Seed를
                // 그대로 쓰고 baseSeed를 전혀 참조하지 않으므로, 여기서 새로 뽑아도
                // 이어하기에는 영향이 없다 - 새 게임을 생성할 때만 실제로 쓰인다.
                if (randomizeSeedEachRun)
                {
                    baseSeed = new System.Random().Next(
                        int.MinValue,
                        int.MaxValue);
                }

                RemovePreExistingSceneRooms(); // 기존 테스트 맵을 제거하고 생성 던전만 사용
            }

            awakeCompleted = true;
        }

        private void Start()
        {
            if (!useProceduralGeneration || !generateFirstFloorOnStart)
            {
                return;
            }

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerGridMovementController>();
            }

            if (generatedFloorRoot != null)
            {
                return;
            }

            DungeonRunState state = GetDungeonState();

            if (state.TryGetGeneratedFloor(
                    out GeneratedDungeon restoredDungeon,
                    out int restoredSeed))
            {
                RestoreAndPlaceCurrentFloor(
                    restoredDungeon,
                    restoredSeed,
                    playerController);
                return;
            }

            GenerateAndPlaceCurrentFloor(playerController, true);
        }

        private DungeonRunState GetDungeonState()
        {
            if (dungeonState == null)
            {
                dungeonState = RunContext.Current != null
                    ? RunContext.Current.Dungeon
                    : new DungeonRunState();
            }

            return dungeonState;
        }

        public bool TryDescend(PlayerGridMovementController movementController)
        {
            if (movementController == null)
            {
                return false;
            }

            if (useProceduralGeneration)
            {
                GetDungeonState().AdvanceFloor();
                return GenerateAndPlaceCurrentFloor(movementController, true);
            }

            if (nextFloorRoomPrefabs == null || nextFloorRoomPrefabs.Length == 0)
            {
                Debug.LogWarning(
                    "[Project Delta] 다음 층 방 프리팹이 지정되지 않아 계단 이동을 처리할 수 없습니다.",
                    this);
                return false;
            }

            GetDungeonState().AdvanceFloor();
            RoomView newRoomView = SpawnLegacyRoomForCurrentFloor();
            movementController.EnterRoom(newRoomView, GridPosition.Zero, CardinalDirection.North);
            return true;
        }

        public void EnsureCurrentFloorRoomExists()
        {
            if (GetDungeonState().CurrentFloor <= 1)
            {
                return;
            }

            if (useProceduralGeneration)
            {
                if (!awakeCompleted || generatedFloorRoot != null)
                {
                    return;
                }

                DungeonRunState state = GetDungeonState();

                if (state.TryGetGeneratedFloor(
                        out GeneratedDungeon restoredDungeon,
                        out int restoredSeed))
                {
                    RestoreAndPlaceCurrentFloor(
                        restoredDungeon,
                        restoredSeed,
                        null);
                }
                else
                {
                    GenerateAndPlaceCurrentFloor(
                        null,
                        false);
                }

                return;
            }

            if (spawnedRoomView != null)
            {
                return;
            }

            if (nextFloorRoomPrefabs == null || nextFloorRoomPrefabs.Length == 0)
            {
                return;
            }

            SpawnLegacyRoomForCurrentFloor();
        }

        public bool GenerateAndPlaceCurrentFloor(
            PlayerGridMovementController movementController,
            bool movePlayerToEntry)
        {
            if (!BuildBindingLookup(out DungeonRoomPrefabBinding entryBinding))
            {
                Debug.LogError(
                    "[Project Delta] 절차 생성 RoomDefinition/RoomView 바인딩이 올바르지 않습니다.",
                    this);
                return false;
            }

            List<RoomTemplate> roomPool = new List<RoomTemplate>();

            for (int i = 0; i < roomBindings.Length; i++)
            {
                DungeonRoomPrefabBinding binding = roomBindings[i];

                if (binding != null && binding.IsValid && binding.IncludeInGenerationPool)
                {
                    roomPool.Add(binding.Definition.ToRoomTemplate());
                }
            }

            if (roomPool.Count == 0)
            {
                Debug.LogError("[Project Delta] 절차 생성 방 풀이 비어 있습니다.", this);
                return false;
            }

            DungeonGenerationSettings settings;

            try
            {
                settings = new DungeonGenerationSettings(
                    targetRoomCount,
                    minMainPathLength,
                    maxMainPathLength,
                    branchChance,
                    minBranchLength,
                    maxBranchLength,
                    specialCandidateChance,
                    loopChance);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Project Delta] 던전 생성 설정 오류: {exception.Message}", this);
                return false;
            }

            int floor = GetDungeonState().CurrentFloor;
            int requestedSeed = unchecked(baseSeed + ((floor - 1) * 1000));

            DungeonGenerationRunResult run = new DungeonGenerationService().GenerateWithRetry(
                entryBinding.Definition.ToRoomTemplate(),
                roomPool,
                settings,
                requestedSeed,
                maxGenerationAttempts);

            if (!run.Success || run.Dungeon == null)
            {
                LogGenerationFailure(run);
                return false;
            }

            ClearGeneratedFloor();

            GameObject rootObject = new GameObject($"GeneratedFloor_{floor}_Seed_{run.SuccessfulSeed}");
            rootObject.transform.SetParent(null, false); // Player를 따라가지 않도록 Scene 최상위 월드 루트로 유지
            generatedFloorRoot = rootObject.transform;
            currentGeneration = run;

            if (!InstantiateRooms(run.Dungeon))
            {
                ClearGeneratedFloor();
                return false;
            }

            if (!ConnectGeneratedDoors(run.Dungeon))
            {
                ClearGeneratedFloor();
                return false;
            }

            PlaceRuntimeStairs(run.Dungeon);

            GetDungeonState().SetGeneratedFloor(
                run.Dungeon,
                run.SuccessfulSeed);

            BuildEncounterLayout(
                run.Dungeon,
                run.SuccessfulSeed,
                floor);

            if (movePlayerToEntry && movementController != null)
            {
                if (!spawnedRooms.TryGetValue(run.Dungeon.EntryRoom.RoomId, out RoomView entryRoomView))
                {
                    Debug.LogError("[Project Delta] 생성된 Entry RoomView를 찾을 수 없습니다.", this);
                    return false;
                }

                Vector3 playerPosition = movementController.transform.position;
                Vector3 entryWorldPosition = entryRoomView.transform.position;

                movementController.transform.position = new Vector3(
                    entryWorldPosition.x,
                    playerPosition.y,
                    entryWorldPosition.z); // 기존 테스트 맵에서 새 방까지 미끄러져 이동하지 않고 즉시 시작 위치로 이동

                movementController.EnterRoom(
                    entryRoomView,
                    GridPosition.Zero,
                    CardinalDirection.North);
            }

            Debug.Log(
                $"[Project Delta] {floor}층 절차 던전 배치 완료 / Seed {run.SuccessfulSeed} / Rooms {run.Dungeon.Layout.AllRooms.Count}",
                this);

            return true;
        }

        private bool RestoreAndPlaceCurrentFloor(
            GeneratedDungeon dungeon,
            int savedSeed,
            PlayerGridMovementController movementController)
        {
            if (dungeon == null)
            {
                Debug.LogError(
                    "[Project Delta] 복원할 GeneratedDungeon이 없습니다.",
                    this);
                return false;
            }

            if (!BuildBindingLookup(out _))
            {
                Debug.LogError(
                    "[Project Delta] 저장 던전 복원에 필요한 RoomDefinition/RoomView 바인딩이 올바르지 않습니다.",
                    this);
                return false;
            }

            ClearGeneratedFloor();

            int floor =
                GetDungeonState().CurrentFloor;

            GameObject rootObject =
                new GameObject(
                    $"RestoredFloor_{floor}_Seed_{savedSeed}");

            rootObject.transform.SetParent(
                null,
                false);

            generatedFloorRoot =
                rootObject.transform;

            currentGeneration =
                new DungeonGenerationRunResult(
                    true,
                    dungeon,
                    savedSeed,
                    savedSeed,
                    Array.Empty<DungeonGenerationAttemptLog>(),
                    null);

            if (!InstantiateRooms(dungeon))
            {
                ClearGeneratedFloor();
                return false;
            }

            if (!ConnectGeneratedDoors(dungeon))
            {
                ClearGeneratedFloor();
                return false;
            }

            PlaceRuntimeStairs(dungeon);

            BuildEncounterLayout(
                dungeon,
                savedSeed,
                floor);

            if (movementController != null)
            {
                MovePlayerToSavedRoom(
                    dungeon,
                    movementController);
            }

            // 모든 RoomPassageController가 pending RoomRunState를 적용한 뒤 비운다.
            // 다음 층에서 같은 RoomId가 재사용돼도 이전 층 상태가 섞이지 않는다.
            DungeonSaveMapper.ClearPendingRestore();

            Debug.Log(
                $"[Project Delta] {floor}층 저장 던전 복원 완료 / Seed {savedSeed} / Rooms {dungeon.Layout.AllRooms.Count}",
                this);

            return true;
        }

        private void MovePlayerToSavedRoom(
            GeneratedDungeon dungeon,
            PlayerGridMovementController movementController)
        {
            if (movementController == null
                || movementController.PlayerState == null)
            {
                return;
            }

            string savedRoomId =
                movementController.PlayerState.CurrentRoomId;

            GridPosition savedGridPosition =
                movementController.PlayerState.CurrentGridPosition;

            RoomView targetRoomView = null;

            if (!string.IsNullOrEmpty(savedRoomId))
            {
                spawnedRooms.TryGetValue(
                    savedRoomId,
                    out targetRoomView);
            }

            if (targetRoomView == null
                && dungeon.EntryRoom != null)
            {
                spawnedRooms.TryGetValue(
                    dungeon.EntryRoom.RoomId,
                    out targetRoomView);

                savedGridPosition =
                    GridPosition.Zero;
            }

            if (targetRoomView == null)
            {
                Debug.LogError(
                    "[Project Delta] 저장된 현재 방 RoomView를 찾을 수 없습니다.",
                    this);
                return;
            }

            float restoredCellSize =
                CalculateRestoredCellSize(
                    targetRoomView);

            Vector3 localPosition =
                new Vector3(
                    savedGridPosition.X * restoredCellSize,
                    0f,
                    savedGridPosition.Z * restoredCellSize);

            Vector3 targetWorldPosition =
                targetRoomView.transform.TransformPoint(
                    localPosition);

            targetWorldPosition.y =
                movementController.transform.position.y;

            // EnterRoom의 보간이 다른 방에서 시작하지 않도록 저장 위치에 먼저 배치한다.
            movementController.transform.position =
                targetWorldPosition;

            movementController.EnterRoom(
                targetRoomView,
                savedGridPosition,
                CardinalDirection.North);
        }

        private float CalculateRestoredCellSize(
            RoomView roomView)
        {
            RoomDefinition definition =
                roomView != null
                && roomView.PassageController != null
                    ? roomView.PassageController.RoomDefinition
                    : null;

            if (definition == null
                || definition.Width <= 0)
            {
                return 2f;
            }

            return roomWorldSize / definition.Width;
        }

        private void RemovePreExistingSceneRooms()
        {
            RoomView[] existingRooms = FindObjectsByType<RoomView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < existingRooms.Length; i++)
            {
                RoomView roomView = existingRooms[i];

                if (roomView == null)
                {
                    continue;
                }

                roomView.gameObject.SetActive(false); // Destroy가 프레임 끝에 처리되기 전에도 기존 맵이 보이지 않게 함
                Destroy(roomView.gameObject);
            }
        }

        private bool BuildBindingLookup(out DungeonRoomPrefabBinding entryBinding)
        {
            bindingsByDefinition.Clear();
            entryBinding = null;

            if (roomBindings == null || roomBindings.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < roomBindings.Length; i++)
            {
                DungeonRoomPrefabBinding binding = roomBindings[i];

                if (binding == null || !binding.IsValid)
                {
                    continue;
                }

                string definitionId = binding.Definition.Id;

                if (string.IsNullOrEmpty(definitionId))
                {
                    continue;
                }

                bindingsByDefinition[definitionId] = binding;

                if (binding.UseAsEntry)
                {
                    if (entryBinding != null)
                    {
                        Debug.LogError("[Project Delta] 시작 방 바인딩은 하나만 지정해야 합니다.", this);
                        return false;
                    }

                    entryBinding = binding;
                }
            }

            return entryBinding != null && bindingsByDefinition.Count > 0;
        }

        private bool InstantiateRooms(GeneratedDungeon dungeon)
        {
            spawnedRooms.Clear();

            foreach (RoomNode node in dungeon.Layout.AllRooms)
            {
                if (!bindingsByDefinition.TryGetValue(
                        node.DefinitionId,
                        out DungeonRoomPrefabBinding binding))
                {
                    Debug.LogError(
                        $"[Project Delta] DefinitionId '{node.DefinitionId}'에 대응하는 RoomView 프리팹이 없습니다.",
                        this);
                    return false;
                }

                Vector3 worldPosition = CalculateRoomWorldPosition(
                    node.MacroCoordinate,
                    proceduralFloorOrigin,
                    roomWorldSize);

                RoomView roomView = Instantiate(
                    binding.Prefab,
                    worldPosition,
                    Quaternion.identity,
                    generatedFloorRoot);

                roomView.name = $"Room_{node.RoomId}";

                if (roomView.PassageController == null)
                {
                    Debug.LogError($"[Project Delta] {roomView.name}에 RoomPassageController가 없습니다.", roomView);
                    return false;
                }

                roomView.PassageController.ConfigureRuntime(
                    node.RoomId,
                    binding.Definition,
                    true);

                PrepareGeneratedRoomVisuals(roomView); // 생성 방 그리드와 천장 준비
                roomView.RefreshMarkers();
                spawnedRooms[node.RoomId] = roomView;
            }

            return true;
        }

        private void PrepareGeneratedRoomVisuals(RoomView roomView)
        {
            if (roomView == null)
            {
                return;
            }

            GridFloorGuideController guide = roomView.GetComponent<GridFloorGuideController>();

            if (guide == null)
            {
                guide = roomView.gameObject.AddComponent<GridFloorGuideController>();
            }

            guide.SetGuideVisible(true);
            CreateGeneratedCeiling(roomView);
        }

        private void CreateGeneratedCeiling(RoomView roomView)
        {
            if (roomView.transform.Find("Ceiling") != null)
            {
                return;
            }

            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(roomView.transform, false);
            ceiling.transform.localPosition = new Vector3(0f, 2.55f, 0f);
            ceiling.transform.localRotation = Quaternion.identity;
            ceiling.transform.localScale = new Vector3(roomWorldSize, 0.1f, roomWorldSize);

            Renderer ceilingRenderer = ceiling.GetComponent<Renderer>();
            Renderer floorRenderer = FindChildRenderer(roomView, "Floor");

            if (ceilingRenderer != null && floorRenderer != null)
            {
                ceilingRenderer.sharedMaterial = floorRenderer.sharedMaterial;
            }

            Collider ceilingCollider = ceiling.GetComponent<Collider>();

            if (ceilingCollider != null)
            {
                Destroy(ceilingCollider);
            }
        }

        private static Renderer FindChildRenderer(RoomView roomView, string objectName)
        {
            foreach (Renderer renderer in roomView.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null && renderer.gameObject.name == objectName)
                {
                    return renderer;
                }
            }

            return null;
        }

        private bool ConnectGeneratedDoors(GeneratedDungeon dungeon)
        {
            HashSet<string> connectedPairs = new HashSet<string>();

            foreach (RoomNode node in dungeon.Layout.AllRooms)
            {
                foreach (KeyValuePair<CardinalDirection, RoomConnectionEdge> pair in node.Connections)
                {
                    RoomConnectionEdge edge = pair.Value;

                    if (edge == null || edge.Neighbor == null)
                    {
                        return false;
                    }

                    string pairKey = BuildPairKey(node.RoomId, edge.Neighbor.RoomId);

                    if (!connectedPairs.Add(pairKey))
                    {
                        continue;
                    }

                    if (!edge.HasExactExitPair)
                    {
                        Debug.LogError(
                            $"[Project Delta] {node.RoomId} 연결에 정확한 RoomExit 쌍이 없습니다.",
                            this);
                        return false;
                    }

                    if (!spawnedRooms.TryGetValue(node.RoomId, out RoomView fromView)
                        || !spawnedRooms.TryGetValue(edge.Neighbor.RoomId, out RoomView toView))
                    {
                        return false;
                    }

                    RoomExit fromExit = edge.LocalExit.Value;
                    RoomExit toExit = edge.NeighborExit.Value;

                    if (fromView.FindExitMarker(fromExit) == null
                        || toView.FindExitMarker(toExit) == null)
                    {
                        Debug.LogError(
                            $"[Project Delta] 실제 프리팹 출구 마커가 그래프와 일치하지 않습니다. {node.RoomId} {fromExit} <-> {edge.Neighbor.RoomId} {toExit}",
                            this);
                        return false;
                    }

                    GridPassage sharedDoor = GridPassage.CreateDoor(edge.IsLocked);

                    if (!fromView.PassageController.SetGeneratedDoorPassage(fromExit, sharedDoor)
                        || !toView.PassageController.SetGeneratedDoorPassage(toExit, sharedDoor))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void PlaceRuntimeStairs(GeneratedDungeon dungeon)
        {
            if (dungeon.StairsRoom == null
                || !spawnedRooms.TryGetValue(dungeon.StairsRoom.RoomId, out RoomView stairsRoomView))
            {
                Debug.LogError("[Project Delta] 계단 방 RoomView를 찾을 수 없습니다.", this);
                return;
            }

            GameObject stairsObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stairsObject.name = "Runtime_Stairs";
            stairsObject.transform.SetParent(stairsRoomView.transform, false);
            stairsObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            stairsObject.transform.localScale = new Vector3(1.2f, 1f, 1.2f);

            Collider stairsCollider = stairsObject.GetComponent<Collider>();

            if (stairsCollider != null)
            {
                Destroy(stairsCollider);
            }

            RoomContentMarker marker = stairsObject.AddComponent<RoomContentMarker>();
            marker.Configure(RoomContentType.Stairs, GridPosition.Zero);
            stairsRoomView.RefreshMarkers();
        }

        // 이후 Player 경계 이동 연결에서 그대로 사용할 수 있는 그래프 전환 조회 API
        public bool TryGetGeneratedDestination(
            string currentRoomId,
            GridPosition currentPosition,
            CardinalDirection direction,
            out RoomView destinationRoom,
            out GridPosition destinationEntryPosition)
        {
            destinationRoom = null;
            destinationEntryPosition = GridPosition.Zero;

            GeneratedDungeon dungeon = CurrentDungeon;

            if (dungeon == null
                || !dungeon.Layout.TryGetRoom(currentRoomId, out RoomNode currentNode)
                || !currentNode.TryGetConnection(direction, out RoomConnectionEdge edge)
                || edge == null
                || !edge.HasExactExitPair)
            {
                return false;
            }

            RoomExit localExit = edge.LocalExit.Value;
            RoomExit neighborExit = edge.NeighborExit.Value;

            if (currentPosition != localExit.LocalPosition)
            {
                return false;
            }

            if (!spawnedRooms.TryGetValue(edge.Neighbor.RoomId, out destinationRoom))
            {
                destinationRoom = null;
                return false;
            }

            destinationEntryPosition = neighborExit.LocalPosition;
            return true;
        }

        public static Vector3 CalculateRoomWorldPosition(
            GridPosition macroCoordinate,
            Vector3 origin,
            float worldSize)
        {
            return origin + new Vector3(
                macroCoordinate.X * worldSize,
                0f,
                macroCoordinate.Z * worldSize);
        }

        // 78일차: floor를 받아 이 층에서 실제로 허용된 인카운터만(EncounterDefinition.
        // IsAllowedOnFloor) 배정 대상으로 쓴다.
        private void BuildEncounterLayout(
            GeneratedDungeon dungeon,
            int seed,
            int floor)
        {
            List<EncounterDefinition> encounters =
                CollectFloorEncounters();

            RoomEncounterPlacementService service =
                new RoomEncounterPlacementService();

            currentEncounterLayout =
                service.BuildForFloor(
                    dungeon,
                    seed,
                    floor,
                    encounters);

            if (encounters.Count == 0)
            {
                Debug.LogWarning(
                    "[Project Delta] 40일차 EncounterDefinition이 지정되지 않아 몬스터 방 배정을 건너뜁니다.",
                    this);
                return;
            }

            Debug.Log(
                $"[Project Delta] 78일차 Encounter 배치 완료 / Floor {floor} / Seed {seed} / MonsterRooms {currentEncounterLayout.Count}",
                this);

            SpawnEncounterMonsters(
                dungeon,
                seed);
        }

        // 78일차: defaultMonsterEncounter(항상 포함)와 additionalFloorEncounters를 하나로 합친다.
        private List<EncounterDefinition> CollectFloorEncounters()
        {
            List<EncounterDefinition> encounters =
                new List<EncounterDefinition>();

            if (defaultMonsterEncounter != null)
            {
                encounters.Add(
                    defaultMonsterEncounter);
            }

            if (additionalFloorEncounters != null)
            {
                for (int index = 0; index < additionalFloorEncounters.Length; index++)
                {
                    if (additionalFloorEncounters[index] != null)
                    {
                        encounters.Add(
                            additionalFloorEncounters[index]);
                    }
                }
            }

            return encounters;
        }

        private void SpawnEncounterMonsters(
            GeneratedDungeon dungeon,
            int seed)
        {
            spawnedMonsters.Clear();

            if (dungeon == null
                || currentEncounterLayout == null
                || currentEncounterLayout.Count == 0)
            {
                return;
            }

            MonsterSpawnPositionService spawnPositionService =
                new MonsterSpawnPositionService();

            foreach (RoomEncounterAssignment assignment
                     in currentEncounterLayout.Assignments)
            {
                if (assignment == null
                    || assignment.ContentType != RoomContentType.Monster)
                {
                    continue;
                }

                if (!spawnedRooms.TryGetValue(
                        assignment.RoomId,
                        out RoomView roomView)
                    || roomView == null
                    || roomView.PassageController == null)
                {
                    Debug.LogWarning(
                        $"[Project Delta] 41일차 Monster RoomView를 찾을 수 없습니다. RoomId={assignment.RoomId}",
                        this);
                    continue;
                }

                RoomDefinition definition =
                    roomView.PassageController.RoomDefinition;

                if (definition == null)
                {
                    Debug.LogWarning(
                        $"[Project Delta] 41일차 Monster 방의 RoomDefinition이 없습니다. RoomId={assignment.RoomId}",
                        roomView);
                    continue;
                }

                if (!dungeon.Layout.TryGetRoom(
                        assignment.RoomId,
                        out RoomNode roomNode))
                {
                    continue;
                }

                List<RoomExit> connectedExits =
                    CollectConnectedExits(
                        roomNode);

                List<GridPosition> occupiedPositions =
                    CollectOccupiedContentPositions(
                        roomView);

                if (!spawnPositionService.TryChoosePosition(
                        definition.MinX,
                        definition.MaxX,
                        definition.MinZ,
                        definition.MaxZ,
                        connectedExits,
                        occupiedPositions,
                        seed,
                        assignment.RoomId,
                        assignment.MonsterDefinitionId,
                        out GridPosition spawnPosition))
                {
                    Debug.LogWarning(
                        $"[Project Delta] 41일차 Monster 스폰 가능한 칸이 없습니다. RoomId={assignment.RoomId}",
                        roomView);
                    continue;
                }

                ExplorationMonsterMarker monster =
                    CreateRuntimeMonster(
                        roomView,
                        definition,
                        assignment,
                        spawnPosition);

                if (monster == null)
                {
                    continue;
                }

                spawnedMonsters[assignment.RoomId] =
                    monster;

                roomView.RefreshMarkers();
            }

            Debug.Log(
                $"[Project Delta] 41일차 정지형 테스트 몬스터 배치 완료 / Spawned {spawnedMonsters.Count}",
                this);
        }

        private static List<RoomExit> CollectConnectedExits(
            RoomNode roomNode)
        {
            List<RoomExit> exits =
                new List<RoomExit>();

            if (roomNode == null)
            {
                return exits;
            }

            foreach (RoomConnectionEdge edge
                     in roomNode.Connections.Values)
            {
                if (edge != null
                    && edge.LocalExit.HasValue)
                {
                    exits.Add(
                        edge.LocalExit.Value);
                }
            }

            return exits;
        }

        private static List<GridPosition> CollectOccupiedContentPositions(
            RoomView roomView)
        {
            List<GridPosition> occupied =
                new List<GridPosition>();

            if (roomView == null)
            {
                return occupied;
            }

            foreach (RoomContentMarker marker
                     in roomView.GetComponentsInChildren<RoomContentMarker>(true))
            {
                if (marker != null)
                {
                    occupied.Add(
                        marker.GridPosition);
                }
            }

            return occupied;
        }

        private ExplorationMonsterMarker CreateRuntimeMonster(
            RoomView roomView,
            RoomDefinition definition,
            RoomEncounterAssignment assignment,
            GridPosition spawnPosition)
        {
            if (roomView == null
                || definition == null
                || assignment == null)
            {
                return null;
            }

            float cellSizeX =
                definition.Width > 0
                    ? roomWorldSize / definition.Width
                    : 2f;

            float cellSizeZ =
                definition.Height > 0
                    ? roomWorldSize / definition.Height
                    : 2f;

            GameObject monsterObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule);

            monsterObject.name =
                $"Monster_{assignment.MonsterDefinitionId}_{assignment.RoomId}";

            monsterObject.transform.SetParent(
                roomView.transform,
                false);

            monsterObject.transform.localPosition =
                new Vector3(
                    spawnPosition.X * cellSizeX,
                    0.75f,
                    spawnPosition.Z * cellSizeZ);

            monsterObject.transform.localRotation =
                Quaternion.identity;

            monsterObject.transform.localScale =
                new Vector3(
                    0.65f,
                    0.75f,
                    0.65f);

            Collider monsterCollider =
                monsterObject.GetComponent<Collider>();

            if (monsterCollider != null)
            {
                // 42일차의 GridPosition 접촉 판정을 위해 플레이어 이동을 물리적으로 막지 않는다.
                monsterCollider.isTrigger = true;
            }

            ExplorationMonsterMarker monsterMarker =
                monsterObject.AddComponent<ExplorationMonsterMarker>();

            monsterMarker.Configure(
                assignment.RoomId,
                assignment.MonsterDefinitionId,
                spawnPosition,
                assignment.MonsterDefinitionIds); // 76일차: 실제 전투에 쓸 그룹 전체 구성

            RoomContentMarker contentMarker =
                monsterObject.AddComponent<RoomContentMarker>();

            contentMarker.Configure(
                RoomContentType.Monster,
                spawnPosition);

            return monsterMarker;
        }

        private void ClearGeneratedFloor()
        {
            spawnedRooms.Clear();
            spawnedMonsters.Clear();
            currentGeneration = null;
            currentEncounterLayout =
                new DungeonEncounterLayout();

            if (generatedFloorRoot == null)
            {
                return;
            }

            Destroy(generatedFloorRoot.gameObject);
            generatedFloorRoot = null;
        }

        private RoomView SpawnLegacyRoomForCurrentFloor()
        {
            int floor = GetDungeonState().CurrentFloor;
            RoomView prefab = nextFloorRoomPrefabs[(floor - 1) % nextFloorRoomPrefabs.Length];
            Vector3 spawnPosition = floorOrigin + (floorSpacing * (floor - 1));
            RoomView newRoomView = Instantiate(prefab, spawnPosition, Quaternion.identity);

            if (spawnedRoomView != null)
            {
                Destroy(spawnedRoomView.gameObject);
            }

            spawnedRoomView = newRoomView;
            return newRoomView;
        }

        private static string BuildPairKey(string first, string second)
        {
            return string.CompareOrdinal(first, second) < 0
                ? $"{first}<->{second}"
                : $"{second}<->{first}";
        }

        private void LogGenerationFailure(DungeonGenerationRunResult run)
        {
            if (run == null)
            {
                Debug.LogError("[Project Delta] DungeonGenerationRunResult가 null입니다.", this);
                return;
            }

            Debug.LogError(
                $"[Project Delta] 절차 던전 생성 실패 / RequestedSeed {run.RequestedSeed} / Attempts {run.AttemptCount}",
                this);

            for (int i = 0; i < run.Attempts.Count; i++)
            {
                DungeonGenerationAttemptLog attempt = run.Attempts[i];

                for (int issueIndex = 0; issueIndex < attempt.Issues.Count; issueIndex++)
                {
                    Debug.LogError(
                        $"[Project Delta] Attempt {attempt.AttemptNumber} / Seed {attempt.Seed} / {attempt.Issues[issueIndex]}",
                        this);
                }
            }
        }
    }
}
