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

        // 115일차: 적대로 전환된 NPC를 기존 몬스터 조우 파이프라인(TryBeginEncounterAtCurrentPosition)에
        // 그대로 태워보내기 위한 등록 통로 - 몬스터처럼 이 방의 "현재 조우 대상"이 된다.
        public void RegisterRuntimeMonsterMarker(
            string roomId,
            ExplorationMonsterMarker marker)
        {
            if (string.IsNullOrEmpty(roomId)
                || marker == null)
            {
                return;
            }

            spawnedMonsters[roomId] =
                marker;
        }

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

        // 123일차: "다음 회복 시점은 층 이동뿐이다"(기획서 3.6.2, 81일차 주석에 이미 있던 계획)를
        // 실제로 구현한다 - 층을 넘어갈 때 체력·마나·정력을 최대치로 채운다.
        public int CurrentFloorNumber =>
            GetDungeonState().CurrentFloor;

        public FloorTheme CurrentFloorTheme =>
            FloorThemeSchedule.GetTheme(
                baseSeed,
                GetDungeonState().CurrentFloor);

        private void RecoverPlayerOnFloorChange()
        {
            if (RunContext.Current == null)
            {
                return;
            }

            PlayerRunState player =
                RunContext.Current.Player;

            StatBlock finalStats =
                player.GetFinalStats();

            player.CurrentHp =
                Mathf.Max(
                    0,
                    finalStats.MaxHealth);

            player.CurrentMana =
                Mathf.Max(
                    0,
                    finalStats.MaxMana);

            player.CurrentStamina =
                Mathf.Max(
                    0,
                    finalStats.MaxStamina);

            Debug.Log(
                $"[Project Delta] 123일차 층 이동 회복 / HP {player.CurrentHp} MP {player.CurrentMana} 정력 {player.CurrentStamina}",
                this);
        }

        // 131일차: 주요 엔딩("완전한 탐험자의 귀환") 판정용 - 층을 떠나기 직전에만
        // 이 층의 모든 방을 다 봤는지 알 수 있다(AdvanceFloor가 곧바로 방 목록을 비운다).
        private void RecordFloorExplorationBeforeAdvancing()
        {
            if (RunContext.Current == null)
            {
                return;
            }

            if (GetDungeonState().IsCurrentFloorFullyExplored())
            {
                RunContext.Current.Statistics.FullyExploredFloorCount++;
            }
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

            // 124일차: 5층(마왕성)의 계단은 더 내려가는 용도가 아니라 "던전 클리어" 트리거다.
            // 이 계단은 보스 방이 Completed(마왕 처치)되기 전에는 애초에 나타나지 않으므로
            // (PlaceRuntimeStairs의 RoomType.Boss 게이팅), 여기 도달했다는 것 자체가 이미
            // 마왕을 쓰러뜨렸다는 뜻이다 - 곧바로 클리어 처리 후 로비로 돌려보낸다.
            if (GetDungeonState().CurrentFloor >= FloorThemeSchedule.FloorCount)
            {
                Debug.Log(
                    "[Project Delta] 124일차 던전 클리어 - 마왕 처치 후 로비로 복귀합니다.",
                    this);

                ApplicationFlow.Current?.ReturnToLobby();

                return true;
            }

            if (useProceduralGeneration)
            {
                RecordFloorExplorationBeforeAdvancing();

                GetDungeonState().AdvanceFloor();

                RecoverPlayerOnFloorChange();

                return GenerateAndPlaceCurrentFloor(movementController, true);
            }

            if (nextFloorRoomPrefabs == null || nextFloorRoomPrefabs.Length == 0)
            {
                Debug.LogWarning(
                    "[Project Delta] 다음 층 방 프리팹이 지정되지 않아 계단 이동을 처리할 수 없습니다.",
                    this);
                return false;
            }

            RecordFloorExplorationBeforeAdvancing();

            GetDungeonState().AdvanceFloor();
            RecoverPlayerOnFloorChange();
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

            ApplyBossRoomType(
                run.Dungeon); // 121일차: 몬스터를 채우기 전에 계단 방을 보스 방으로 확정한다.

            PlaceRuntimeStairs(run.Dungeon);

            GetDungeonState().SetGeneratedFloor(
                run.Dungeon,
                run.SuccessfulSeed);

            BuildEncounterLayout(
                run.Dungeon,
                run.SuccessfulSeed,
                floor);

            SpawnChests(
                run.Dungeon,
                run.SuccessfulSeed);

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

            ApplyBossRoomType(
                dungeon); // 121일차: 저장에서 복원할 때도 계단 방은 항상 보스 방이다.

            PlaceRuntimeStairs(dungeon);

            BuildEncounterLayout(
                dungeon,
                savedSeed,
                floor);

            SpawnChests(
                dungeon,
                savedSeed);

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

            if (stairsRoomView.transform.Find("Runtime_Stairs") != null)
            {
                return; // 이미 배치된 계단을 다시 만들지 않는다.
            }

            RoomInstance stairsRoomInstance =
                stairsRoomView.PassageController != null
                    ? stairsRoomView.PassageController.CurrentInstance
                    : null;

            // 121일차: 보스 방(계단 방)은 보스를 쓰러뜨리기(RoomInstance.Completed) 전까지
            // 계단을 감춘다 - NotifyRoomEncounterCompleted()가 쓰러뜨린 순간 이 메서드를 다시 부른다.
            if (stairsRoomInstance != null
                && stairsRoomInstance.RoomType == RoomType.Boss
                && !stairsRoomInstance.Completed)
            {
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

        // 121일차: 몬스터 조우가 완료(승리)됐다고 알려주는 진입점 - ExplorationMonsterEncounterController가
        // 보스(계단 방) 승리 직후 호출한다. 계단 방이 아니면 아무 일도 하지 않는다.
        public void NotifyRoomEncounterCompleted(
            string roomId)
        {
            if (string.IsNullOrEmpty(roomId)
                || CurrentDungeon?.StairsRoom == null
                || roomId != CurrentDungeon.StairsRoom.RoomId)
            {
                return;
            }

            PlaceRuntimeStairs(
                CurrentDungeon);

            Debug.Log(
                $"[Project Delta] 121일차 보스 격파 - 계단 공개 / RoomId={roomId}",
                this);
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
                    encounters,
                    CollectNonCombatRoomIds(
                        dungeon));

            if (encounters.Count == 0)
            {
                Debug.LogWarning(
                    "[Project Delta] EncounterDefinition 필드가 비어 있어 로드된 인카운터 자산으로 Combat 방 보장을 시도합니다.",
                    this);

                EnsureCombatRoomsHaveMonsters(
                    dungeon,
                    seed);

                EnsureBossRoomHasMonster(
                    dungeon,
                    seed); // 121일차: 계단 방(=보스 방)은 위 보장 로직에서 항상 제외되므로 따로 채운다.

                return;
            }

            Debug.Log(
                $"[Project Delta] 78일차 Encounter 배치 완료 / Floor {floor} / Seed {seed} / MonsterRooms {currentEncounterLayout.Count}",
                this);

            SpawnEncounterMonsters(
                dungeon,
                seed);

            EnsureCombatRoomsHaveMonsters(
                dungeon,
                seed);

            EnsureBossRoomHasMonster(
                dungeon,
                seed); // 121일차: 계단 방(=보스 방)은 위 보장 로직에서 항상 제외되므로 따로 채운다.
        }

        // 121일차: 계단 방을 보스 방으로 확정한다 - 몬스터를 채우기 전, 문 연결 직후 호출한다.
        // "보스가 있는 층은 보스방에 계단이 생긴다" 요구사항을 그대로 반영해, 지금은 모든 층의
        // 계단 방이 곧 보스 방이다(특정 층만 보스가 있게 하려면 이 메서드 하나만 조건을 걸면 된다).
        private void ApplyBossRoomType(
            GeneratedDungeon dungeon)
        {
            if (dungeon?.StairsRoom == null)
            {
                return;
            }

            if (!spawnedRooms.TryGetValue(
                    dungeon.StairsRoom.RoomId,
                    out RoomView stairsRoomView)
                || stairsRoomView.PassageController == null
                || stairsRoomView.PassageController.CurrentInstance == null)
            {
                return;
            }

            stairsRoomView.PassageController.CurrentInstance.SetRoomType(
                RoomType.Boss);
        }

        // 121일차: 계단 방(보스 방)에는 일반 Combat 방 보장 로직이 절대 몬스터를 채우지 않는다
        // (EnsureCombatRoomsHaveMonsters가 시작/계단 방을 명시적으로 제외한다) - 그래서 같은
        // 배치 절차(TryBuildCombatGuaranteeAssignment·MonsterSpawnPositionService·
        // CreateRuntimeMonster)를 이 방 하나에 대해서만 따로 실행한다.
        private void EnsureBossRoomHasMonster(
            GeneratedDungeon dungeon,
            int seed)
        {
            if (dungeon?.StairsRoom == null)
            {
                return;
            }

            RoomNode room =
                dungeon.StairsRoom;

            if (spawnedMonsters.ContainsKey(
                    room.RoomId))
            {
                return;
            }

            if (!spawnedRooms.TryGetValue(
                    room.RoomId,
                    out RoomView roomView)
                || roomView == null
                || roomView.PassageController == null)
            {
                return;
            }

            RoomInstance roomInstance =
                roomView.PassageController.CurrentInstance;

            // 이미 깬 보스 방(저장에서 복원)이면 다시 스폰하지 않는다.
            if (roomInstance == null
                || roomInstance.Completed)
            {
                return;
            }

            RoomDefinition definition =
                roomView.PassageController.RoomDefinition;

            if (definition == null)
            {
                return;
            }

            List<EncounterDefinition> fallbackEncounters =
                CollectBossEncounters();

            if (fallbackEncounters.Count == 0)
            {
                Debug.LogError(
                    "[Project Delta] 121일차 보스 방 몬스터 배치 실패: 사용할 EncounterDefinition이 하나도 없습니다.",
                    this);

                return;
            }

            if (!TryBuildCombatGuaranteeAssignment(
                    room.RoomId,
                    seed,
                    fallbackEncounters,
                    out RoomEncounterAssignment assignment))
            {
                Debug.LogError(
                    $"[Project Delta] 121일차 보스 방 몬스터 배치 실패: 유효한 몬스터 그룹 없음. RoomId={room.RoomId}",
                    roomView);

                return;
            }

            MonsterSpawnPositionService spawnPositionService =
                new MonsterSpawnPositionService();

            List<RoomExit> connectedExits =
                CollectConnectedExits(
                    room);

            List<GridPosition> occupiedPositions =
                CollectOccupiedContentPositions(
                    roomView);

            bool hasSpawnPosition =
                spawnPositionService.TryChoosePosition(
                    definition.MinX,
                    definition.MaxX,
                    definition.MinZ,
                    definition.MaxZ,
                    connectedExits,
                    occupiedPositions,
                    seed,
                    room.RoomId,
                    assignment.MonsterDefinitionId,
                    out GridPosition spawnPosition);

            if (!hasSpawnPosition)
            {
                hasSpawnPosition =
                    spawnPositionService.TryChoosePosition(
                        definition.MinX,
                        definition.MaxX,
                        definition.MinZ,
                        definition.MaxZ,
                        connectedExits,
                        null,
                        seed,
                        room.RoomId,
                        assignment.MonsterDefinitionId,
                        out spawnPosition);
            }

            if (!hasSpawnPosition)
            {
                spawnPosition =
                    ChooseEmergencyMonsterPosition(
                        definition,
                        connectedExits);

                Debug.LogWarning(
                    $"[Project Delta] 보스 방 빈 칸이 없어 비상 위치에 보스를 배치합니다. RoomId={room.RoomId} / Position={spawnPosition}",
                    roomView);
            }

            ExplorationMonsterMarker monster =
                CreateRuntimeMonster(
                    roomView,
                    definition,
                    assignment,
                    spawnPosition);

            if (monster == null)
            {
                return;
            }

            currentEncounterLayout.TryAdd(
                assignment);

            spawnedMonsters[room.RoomId] =
                monster;

            roomView.RefreshMarkers();

            Debug.Log(
                $"[Project Delta] 121일차 보스 방 몬스터 배치 완료 / RoomId={room.RoomId} / Monster={assignment.MonsterDefinitionId}",
                this);
        }

        // 112일차: RoomType.Combat 방(시작/계단 방 제외)은 인카운터 확률 굴림과 무관하게
        // 최소 1마리를 보장한다 - 위 SpawnEncounterMonsters가 확률상 아무것도 배치하지
        // 않고 지나간 Combat 방만 여기서 defaultMonsterEncounter로 채운다.
        private void EnsureCombatRoomsHaveMonsters(
            GeneratedDungeon dungeon,
            int seed)
        {
            if (dungeon?.Layout == null)
            {
                return;
            }

            DungeonRunState dungeonState =
                RunContext.Current?.Dungeon;

            if (dungeonState == null)
            {
                return;
            }

            List<EncounterDefinition> fallbackEncounters =
                CollectCombatGuaranteeEncounters();

            if (fallbackEncounters.Count == 0)
            {
                Debug.LogError(
                    "[Project Delta] Combat 방 최소 몬스터 보장 실패: 사용할 EncounterDefinition이 하나도 없습니다.",
                    this);
                return;
            }

            MonsterSpawnPositionService spawnPositionService =
                new MonsterSpawnPositionService();

            foreach (RoomNode room in dungeon.Layout.AllRooms)
            {
                if (room == null
                    || string.IsNullOrEmpty(room.RoomId)
                    || spawnedMonsters.ContainsKey(room.RoomId)
                    || (dungeon.EntryRoom != null && room.RoomId == dungeon.EntryRoom.RoomId)
                    || (dungeon.StairsRoom != null && room.RoomId == dungeon.StairsRoom.RoomId))
                {
                    continue;
                }

                if (!dungeonState.TryGetRoom(room.RoomId, out RoomInstance roomInstance)
                    || roomInstance.RoomType != RoomType.Combat)
                {
                    continue;
                }

                if (!spawnedRooms.TryGetValue(room.RoomId, out RoomView roomView)
                    || roomView == null
                    || roomView.PassageController == null)
                {
                    continue;
                }

                RoomDefinition definition =
                    roomView.PassageController.RoomDefinition;

                if (definition == null)
                {
                    continue;
                }

                if (!TryBuildCombatGuaranteeAssignment(
                        room.RoomId,
                        seed,
                        fallbackEncounters,
                        out RoomEncounterAssignment assignment))
                {
                    Debug.LogError(
                        $"[Project Delta] Combat 방 최소 몬스터 보장 실패: 유효한 몬스터 그룹 없음. RoomId={room.RoomId}",
                        roomView);
                    continue;
                }

                List<RoomExit> connectedExits =
                    CollectConnectedExits(
                        room);

                List<GridPosition> occupiedPositions =
                    CollectOccupiedContentPositions(
                        roomView);

                bool hasSpawnPosition =
                    spawnPositionService.TryChoosePosition(
                        definition.MinX,
                        definition.MaxX,
                        definition.MinZ,
                        definition.MaxZ,
                        connectedExits,
                        occupiedPositions,
                        seed,
                        room.RoomId,
                        assignment.MonsterDefinitionId,
                        out GridPosition spawnPosition);

                // 113일차: 다른 콘텐츠 때문에 빈 칸이 없으면 겹침을 허용해서라도 Combat 방 1마리를 우선 보장한다.
                if (!hasSpawnPosition)
                {
                    hasSpawnPosition =
                        spawnPositionService.TryChoosePosition(
                            definition.MinX,
                            definition.MaxX,
                            definition.MinZ,
                            definition.MaxZ,
                            connectedExits,
                            null,
                            seed,
                            room.RoomId,
                            assignment.MonsterDefinitionId,
                            out spawnPosition);
                }

                if (!hasSpawnPosition)
                {
                    spawnPosition =
                        ChooseEmergencyMonsterPosition(
                            definition,
                            connectedExits);

                    Debug.LogWarning(
                        $"[Project Delta] Combat 방 빈 칸이 없어 비상 위치에 최소 몬스터를 배치합니다. RoomId={room.RoomId} / Position={spawnPosition}",
                        roomView);
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

                currentEncounterLayout.TryAdd(
                    assignment);

                spawnedMonsters[room.RoomId] =
                    monster;

                roomView.RefreshMarkers();
            }
        }

        // 113일차: 기본 인카운터가 비어 있어도 추가 인카운터와 현재 로드된 인카운터 자산까지 후보로 사용한다.
        private List<EncounterDefinition> CollectCombatGuaranteeEncounters()
        {
            List<EncounterDefinition> encounters =
                CollectFloorEncounters();

            HashSet<EncounterDefinition> known =
                new HashSet<EncounterDefinition>(encounters);

            EncounterDefinition[] loaded =
                Resources.FindObjectsOfTypeAll<EncounterDefinition>();

            for (int index = 0; index < loaded.Length; index++)
            {
                EncounterDefinition encounter =
                    loaded[index];

                if (encounter != null
                    && known.Add(encounter))
                {
                    encounters.Add(encounter);
                }
            }

            return encounters;
        }

        // 122일차: "전용 방과 연결" - 지금 층 번호로 보스 로스터(Tier == Boss로 지정된
        // 몬스터들)를 순서대로 돌려가며 정한다. 골라낸 보스를 목록 맨 앞에 둬서
        // TryBuildCombatGuaranteeAssignment(첫 유효 항목을 쓴다)가 항상 그 보스를 쓰게 한다.
        // Boss 등급 몬스터가 하나도 없으면(콘텐츠 미비) 기존 전체 후보로 안전하게 되돌아간다.
        // 123일차: 5층(마지막 층)은 순환 로스터가 아니라 항상 이 몬스터(마왕)가 나온다.
        // 순환 로스터에는 절대 섞이지 않도록 별도로 빼둔다.
        // 132일차: 패배 기록 쪽(ApplicationFlow)과 같은 ID를 써야 해서 MainEndingRules로
        // 옮겨두고 여기서는 그 값을 그대로 참조한다.
        private const string FinalBossMonsterId = MainEndingRules.DemonLordMonsterId;

        private List<EncounterDefinition> CollectBossEncounters()
        {
            List<EncounterDefinition> all =
                CollectCombatGuaranteeEncounters();

            List<EncounterDefinition> bossOnly =
                new List<EncounterDefinition>();

            EncounterDefinition finalBossEncounter =
                null;

            for (int index = 0; index < all.Count; index++)
            {
                EncounterDefinition encounter =
                    all[index];

                if (encounter == null
                    || encounter.Monster == null
                    || encounter.Monster.Tier != MonsterTier.Boss)
                {
                    continue;
                }

                if (encounter.Monster.Id == FinalBossMonsterId)
                {
                    finalBossEncounter =
                        encounter;

                    continue;
                }

                bossOnly.Add(encounter);
            }

            int floor =
                GetDungeonState().CurrentFloor;

            if (floor >= FloorThemeSchedule.FloorCount)
            {
                if (finalBossEncounter != null)
                {
                    return new List<EncounterDefinition>
                    {
                        finalBossEncounter
                    };
                }

                Debug.LogWarning(
                    "[Project Delta] 123일차 5층 전용 마왕(MON_DEMON_LORD)을 찾지 못해 순환 보스로 대체합니다.",
                    this);
            }

            if (bossOnly.Count == 0)
            {
                return all;
            }

            int chosenIndex =
                ((floor - 1) % bossOnly.Count
                    + bossOnly.Count)
                % bossOnly.Count;

            List<EncounterDefinition> ordered =
                new List<EncounterDefinition>
                {
                    bossOnly[chosenIndex]
                };

            ordered.AddRange(
                bossOnly);

            return ordered;
        }

        private static bool TryBuildCombatGuaranteeAssignment(
            string roomId,
            int seed,
            IReadOnlyList<EncounterDefinition> encounters,
            out RoomEncounterAssignment assignment)
        {
            assignment = null;

            if (encounters == null)
            {
                return false;
            }

            for (int encounterIndex = 0;
                 encounterIndex < encounters.Count;
                 encounterIndex++)
            {
                EncounterDefinition encounter =
                    encounters[encounterIndex];

                if (encounter == null)
                {
                    continue;
                }

                MonsterGroupCompositionService.Result group =
                    MonsterGroupCompositionService.Build(
                        encounter,
                        seed,
                        roomId);

                if (group.Representative == null
                    || group.Slots.Count == 0)
                {
                    continue;
                }

                string[] monsterDefinitionIds =
                    new string[group.Slots.Count];

                for (int slotIndex = 0;
                     slotIndex < group.Slots.Count;
                     slotIndex++)
                {
                    monsterDefinitionIds[slotIndex] =
                        group.Slots[slotIndex].Id;
                }

                assignment =
                    new RoomEncounterAssignment(
                        roomId,
                        RoomContentType.Monster,
                        encounter.Id,
                        monsterDefinitionIds,
                        group.Representative.Id);

                return true;
            }

            return false;
        }

        private static GridPosition ChooseEmergencyMonsterPosition(
            RoomDefinition definition,
            IReadOnlyList<RoomExit> connectedExits)
        {
            GridPosition center =
                new GridPosition(
                    Mathf.Clamp(
                        0,
                        definition.MinX,
                        definition.MaxX),
                    Mathf.Clamp(
                        0,
                        definition.MinZ,
                        definition.MaxZ));

            HashSet<GridPosition> doorPositions =
                new HashSet<GridPosition>();

            if (connectedExits != null)
            {
                for (int exitIndex = 0;
                     exitIndex < connectedExits.Count;
                     exitIndex++)
                {
                    doorPositions.Add(
                        connectedExits[exitIndex].LocalPosition);
                }
            }

            if (!doorPositions.Contains(center))
            {
                return center;
            }

            for (int z = definition.MinZ;
                 z <= definition.MaxZ;
                 z++)
            {
                for (int x = definition.MinX;
                     x <= definition.MaxX;
                     x++)
                {
                    GridPosition candidate =
                        new GridPosition(x, z);

                    if (!doorPositions.Contains(candidate))
                    {
                        return candidate;
                    }
                }
            }

            return center;
        }

        // 111일차: RoomType.Combat이 아닌 방은 몬스터 조우 배정에서 제외한다.
        // RoomEncounterPlacementService는 그래프(RoomNode)만 알고 실제 RoomType은
        // 모르므로, 이미 배정된 excludedRoomIds 파라미터로 걸러준다.
        private List<string> CollectNonCombatRoomIds(GeneratedDungeon dungeon)
        {
            List<string> excluded = new List<string>();

            DungeonRunState dungeonState = RunContext.Current?.Dungeon;

            if (dungeonState == null || dungeon?.Layout == null)
            {
                return excluded;
            }

            foreach (RoomNode room in dungeon.Layout.AllRooms)
            {
                if (room == null || string.IsNullOrEmpty(room.RoomId))
                {
                    continue;
                }

                if (!dungeonState.TryGetRoom(room.RoomId, out RoomInstance roomInstance)
                    || roomInstance.RoomType != RoomType.Combat)
                {
                    excluded.Add(room.RoomId);
                }
            }

            return excluded;
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

        // 112일차: RoomType과 무관하게(전투 방 포함) 방마다 결정론적으로 상자를 배치한다.
        // 문 칸/문 바로 안쪽 칸(MonsterSpawnPositionService의 기존 안전 칸 계산을 그대로
        // 재사용)과 이미 다른 콘텐츠가 있는 칸은 피한다.
        private void SpawnChests(
            GeneratedDungeon dungeon,
            int seed)
        {
            if (dungeon == null)
            {
                return;
            }

            RoomChestPlacementService placementService =
                new RoomChestPlacementService();

            List<string> chestRoomIds =
                placementService.SelectRoomIds(
                    dungeon,
                    seed);

            // 113일차: 상자가 문 사이 이동 경로를 끊지 않는 후보만 선택한다.
            RoomBlockingPlacementService spawnPositionService =
                new RoomBlockingPlacementService();

            for (int i = 0; i < chestRoomIds.Count; i++)
            {
                string roomId =
                    chestRoomIds[i];

                if (!spawnedRooms.TryGetValue(
                        roomId,
                        out RoomView roomView)
                    || roomView == null
                    || roomView.PassageController == null)
                {
                    continue;
                }

                RoomDefinition definition =
                    roomView.PassageController.RoomDefinition;

                if (definition == null)
                {
                    continue;
                }

                if (!dungeon.Layout.TryGetRoom(
                        roomId,
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
                        roomView.PassageController.CanPass,
                        seed,
                        roomId,
                        "CHEST",
                        out GridPosition spawnPosition))
                {
                    Debug.LogWarning(
                        $"[Project Delta] 112일차 상자 배치 가능한 칸이 없습니다. RoomId={roomId}",
                        roomView);
                    continue;
                }

                CreateRuntimeChest(
                    roomView,
                    definition,
                    roomId,
                    spawnPosition);

                roomView.RefreshMarkers();
            }
        }

        private static readonly string[] PlaceholderChestLoot =
        {
            "ITEM_DAY80_TEST_DROP",
            "ITEM_DAY80_TEST_DROP"
        };

        private void CreateRuntimeChest(
            RoomView roomView,
            RoomDefinition definition,
            string roomId,
            GridPosition spawnPosition)
        {
            if (roomView == null
                || definition == null)
            {
                return;
            }

            float cellSizeX =
                definition.Width > 0
                    ? roomWorldSize / definition.Width
                    : 2f;

            float cellSizeZ =
                definition.Height > 0
                    ? roomWorldSize / definition.Height
                    : 2f;

            GameObject chestObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            chestObject.name =
                $"Chest_{roomId}";

            chestObject.transform.SetParent(
                roomView.transform,
                false);

            chestObject.transform.localPosition =
                new Vector3(
                    spawnPosition.X * cellSizeX,
                    0.4f,
                    spawnPosition.Z * cellSizeZ);

            chestObject.transform.localRotation =
                Quaternion.identity;

            chestObject.transform.localScale =
                new Vector3(
                    0.8f,
                    0.8f,
                    0.8f);

            Collider chestCollider =
                chestObject.GetComponent<Collider>();

            if (chestCollider != null)
            {
                chestCollider.isTrigger = true;
            }

            RoomContentMarker contentMarker =
                chestObject.AddComponent<RoomContentMarker>();

            contentMarker.Configure(
                RoomContentType.Chest,
                spawnPosition);

            // TODO: 실제 아이템 자산이 늘어나면 자리표시자 대신 진짜 루트 테이블로 교체한다.
            // 지금은 프로젝트에 존재하는 유일한 실제 아이템(ITEM_DAY80_TEST_DROP)을 사용한다.
            ChestContentMarker chestMarker =
                chestObject.AddComponent<ChestContentMarker>();

            chestMarker.Configure(
                PlaceholderChestLoot);
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
