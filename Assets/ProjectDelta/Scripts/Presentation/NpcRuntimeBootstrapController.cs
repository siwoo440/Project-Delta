using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 113일차: 정식 NPC 배치 규칙을 만들기 전, 첫 번째 테스트 NPC 한 명을 절차 던전에 자동 배치했다.
    // 114일차: 서비스별로 하나씩(상인/치료사/지도사/보물사냥꾼) 총 4명을 배치하도록 확장한다 -
    // 실제 방 선정·서비스 데이터 소스는 아직 정식 콘텐츠가 아니라 역할별 런타임 정의다.
    public sealed class NpcRuntimeBootstrapController : MonoBehaviour
    {
        private struct NpcRoleConfig
        {
            public string Id;
            public string DisplayName;
            public NpcServiceType ServiceTypes;

            public NpcRoleConfig(
                string id,
                string displayName,
                NpcServiceType serviceTypes)
            {
                Id = id;
                DisplayName = displayName;
                ServiceTypes = serviceTypes;
            }
        }

        private static readonly NpcRoleConfig[] RoleConfigs =
        {
            new NpcRoleConfig(
                "NPC_MERCHANT_TEST",
                "상인",
                NpcServiceType.Trade),
            new NpcRoleConfig(
                "NPC_HEALER_TEST",
                "치료사",
                NpcServiceType.Healing),
            new NpcRoleConfig(
                "NPC_GUIDE_TEST",
                "지도사",
                NpcServiceType.MapInformation
                | NpcServiceType.ExplorationInformation),
            new NpcRoleConfig(
                "NPC_TREASURE_HUNTER_TEST",
                "보물사냥꾼",
                NpcServiceType.RelicTrade
                | NpcServiceType.RelicResearch)
        };

        private PlayerGridMovementController movementController;
        private DungeonFloorController floorController;
        private readonly List<NpcDefinition> runtimeDefinitions =
            new List<NpcDefinition>();

        private bool spawnCompleted;

        private void Awake()
        {
            movementController =
                GetComponent<PlayerGridMovementController>();

            floorController =
                FindFirstObjectByType<DungeonFloorController>();
        }

        private void Update()
        {
            if (spawnCompleted)
            {
                return;
            }

            if (floorController == null)
            {
                floorController =
                    FindFirstObjectByType<DungeonFloorController>();
            }

            if (floorController == null
                || floorController.CurrentDungeon == null
                || floorController.SpawnedRooms == null
                || floorController.SpawnedRooms.Count == 0)
            {
                return;
            }

            SpawnRoleNpcs();

            spawnCompleted =
                true;
        }

        private void SpawnRoleNpcs()
        {
            List<RoomView> candidates =
                CollectTargetRooms();

            int candidateIndex =
                0;

            for (int roleIndex = 0;
                 roleIndex < RoleConfigs.Length;
                 roleIndex++)
            {
                NpcRoleConfig role =
                    RoleConfigs[roleIndex];

                bool spawned =
                    false;

                // 이미 소진한 방부터 다시 훑되, 방마다 실제로 빈 칸이 있는지는
                // TryChooseSpawnPosition이 최종 판단한다 - 방이 모자라면 재사용한다.
                for (int attempt = 0;
                     attempt < candidates.Count
                     && !spawned;
                     attempt++)
                {
                    int index =
                        (candidateIndex + attempt)
                        % candidates.Count;

                    RoomView targetRoom =
                        candidates[index];

                    if (!TryChooseSpawnPosition(
                            targetRoom,
                            role.Id,
                            out GridPosition spawnPosition))
                    {
                        continue;
                    }

                    CreateRoleNpc(
                        targetRoom,
                        spawnPosition,
                        role);

                    candidateIndex =
                        index + 1;

                    spawned =
                        true;
                }
            }
        }

        private bool TryChooseSpawnPosition(
            RoomView targetRoom,
            string npcId,
            out GridPosition spawnPosition)
        {
            spawnPosition =
                GridPosition.Zero;

            if (targetRoom == null
                || targetRoom.PassageController == null
                || targetRoom.PassageController.RoomDefinition == null)
            {
                return false;
            }

            RoomDefinition definition =
                targetRoom.PassageController.RoomDefinition;

            List<RoomExit> exits =
                BuildRoomExits(
                    definition);

            List<GridPosition> occupied =
                CollectOccupiedPositions(
                    targetRoom);

            RoomBlockingPlacementService placementService =
                new RoomBlockingPlacementService();

            return placementService.TryChoosePosition(
                definition.MinX,
                definition.MaxX,
                definition.MinZ,
                definition.MaxZ,
                exits,
                occupied,
                targetRoom.PassageController.CanPass,
                floorController.CurrentSuccessfulSeed,
                targetRoom.PassageController.RoomId,
                npcId,
                out spawnPosition);
        }

        private void CreateRoleNpc(
            RoomView targetRoom,
            GridPosition spawnPosition,
            NpcRoleConfig role)
        {
            NpcDefinition npcDefinition =
                GetOrCreateRuntimeDefinition(
                    role);

            NpcRelationshipState relationship =
                NpcRelationshipRegistry.GetOrCreate(
                    npcDefinition.Id,
                    npcDefinition.InitialAffinity,
                    npcDefinition.StartsHostile);

            NpcServiceRunState serviceState =
                new NpcServiceRunState();

            if ((role.ServiceTypes
                    & NpcServiceType.Trade)
                != 0)
            {
                serviceState.Shop.SetProducts(
                    NpcShopStockBuilder.BuildDefaultStock());
            }

            GameObject npcObject =
                GameObject.CreatePrimitive(
                    PrimitiveType.Capsule);

            npcObject.name =
                $"Npc_{npcDefinition.Id}";

            npcObject.transform.SetParent(
                targetRoom.transform,
                false);

            float cellSize =
                movementController != null
                    ? movementController.CellSize
                    : 2f;

            npcObject.transform.localPosition =
                new Vector3(
                    spawnPosition.X * cellSize,
                    1f,
                    spawnPosition.Z * cellSize);

            npcObject.transform.localScale =
                new Vector3(
                    0.70f,
                    0.90f,
                    0.70f);

            Collider collider =
                npcObject.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(
                    collider);
            }

            Renderer npcRenderer =
                npcObject.GetComponent<Renderer>();

            if (npcRenderer != null)
            {
                npcRenderer.material.color =
                    GetRoleColor(
                        role.ServiceTypes);
            }

            RoomContentMarker roomMarker =
                npcObject.AddComponent<RoomContentMarker>();

            roomMarker.Configure(
                RoomContentType.NpcPoint,
                spawnPosition);

            NpcContentMarker npcMarker =
                npcObject.AddComponent<NpcContentMarker>();

            npcMarker.Configure(
                npcDefinition,
                relationship,
                serviceState);

            targetRoom.RefreshMarkers();

            Debug.Log(
                $"[Project Delta] 114일차 NPC 배치 / {npcDefinition.DisplayName} / Room={targetRoom.PassageController.RoomId} / Position={spawnPosition}",
                this);
        }

        private static Color GetRoleColor(
            NpcServiceType serviceTypes)
        {
            if ((serviceTypes & NpcServiceType.Trade) != 0)
            {
                return new Color(0.30f, 0.78f, 0.95f, 1f);
            }

            if ((serviceTypes & NpcServiceType.Healing) != 0)
            {
                return new Color(0.35f, 0.90f, 0.45f, 1f);
            }

            if ((serviceTypes
                    & (NpcServiceType.MapInformation
                        | NpcServiceType.ExplorationInformation))
                != 0)
            {
                return new Color(0.95f, 0.85f, 0.30f, 1f);
            }

            if ((serviceTypes
                    & (NpcServiceType.RelicTrade
                        | NpcServiceType.RelicResearch))
                != 0)
            {
                return new Color(0.75f, 0.35f, 0.90f, 1f);
            }

            return new Color(0.7f, 0.7f, 0.7f, 1f);
        }

        private List<RoomView> CollectTargetRooms()
        {
            List<RoomView> nonCombat =
                new List<RoomView>();

            List<RoomView> fallback =
                new List<RoomView>();

            string entryRoomId =
                floorController.CurrentDungeon.EntryRoom != null
                    ? floorController.CurrentDungeon.EntryRoom.RoomId
                    : string.Empty;

            string stairsRoomId =
                floorController.CurrentDungeon.StairsRoom != null
                    ? floorController.CurrentDungeon.StairsRoom.RoomId
                    : string.Empty;

            foreach (KeyValuePair<string, RoomView> pair
                     in floorController.SpawnedRooms)
            {
                if (pair.Value == null
                    || pair.Value.PassageController == null
                    || pair.Key == entryRoomId
                    || pair.Key == stairsRoomId)
                {
                    continue;
                }

                RoomInstance instance =
                    pair.Value.PassageController.CurrentInstance;

                if (instance != null
                    && instance.RoomType != RoomType.Combat)
                {
                    nonCombat.Add(
                        pair.Value);
                }
                else
                {
                    fallback.Add(
                        pair.Value);
                }
            }

            nonCombat.Sort(
                CompareRoomId);

            fallback.Sort(
                CompareRoomId);

            nonCombat.AddRange(
                fallback);

            return nonCombat;
        }

        private static int CompareRoomId(
            RoomView left,
            RoomView right)
        {
            string leftId =
                left != null
                && left.PassageController != null
                    ? left.PassageController.RoomId
                    : string.Empty;

            string rightId =
                right != null
                && right.PassageController != null
                    ? right.PassageController.RoomId
                    : string.Empty;

            return string.CompareOrdinal(
                leftId,
                rightId);
        }

        private static List<RoomExit> BuildRoomExits(
            RoomDefinition definition)
        {
            List<RoomExit> exits =
                new List<RoomExit>();

            if (definition == null)
            {
                return exits;
            }

            foreach (PassageEntry entry
                     in definition.GetExits())
            {
                exits.Add(
                    new RoomExit(
                        new GridPosition(
                            entry.X,
                            entry.Z),
                        entry.Direction));
            }

            return exits;
        }

        private static List<GridPosition> CollectOccupiedPositions(
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
                if (marker != null
                    && marker.gameObject.activeInHierarchy)
                {
                    occupied.Add(
                        marker.GridPosition);
                }
            }

            return occupied;
        }

        private NpcDefinition GetOrCreateRuntimeDefinition(
            NpcRoleConfig role)
        {
            for (int i = 0; i < runtimeDefinitions.Count; i++)
            {
                if (runtimeDefinitions[i] != null
                    && runtimeDefinitions[i].Id == role.Id)
                {
                    return runtimeDefinitions[i];
                }
            }

            NpcDefinition definition =
                ScriptableObject.CreateInstance<NpcDefinition>();

            definition.name =
                role.Id;

            definition.hideFlags =
                HideFlags.DontSave;

            definition.ConfigureRuntime(
                role.Id,
                role.DisplayName,
                role.ServiceTypes,
                NpcHostilityMode.CanBecomeHostile,
                0);

            runtimeDefinitions.Add(
                definition);

            return definition;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < runtimeDefinitions.Count; i++)
            {
                if (runtimeDefinitions[i] != null)
                {
                    Destroy(
                        runtimeDefinitions[i]);
                }
            }

            runtimeDefinitions.Clear();
        }
    }
}
