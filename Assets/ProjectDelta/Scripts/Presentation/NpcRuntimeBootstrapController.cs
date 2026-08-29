using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 113일차: 정식 NPC 배치 규칙을 만들기 전, 첫 번째 테스트 NPC 한 명을 절차 던전에 자동 배치한다.
    public sealed class NpcRuntimeBootstrapController : MonoBehaviour
    {
        private const string TestNpcId = "NPC_MERCHANT_TEST";

        private PlayerGridMovementController movementController;
        private DungeonFloorController floorController;
        private NpcDefinition runtimeDefinition;
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

            if (!TrySpawnTestNpc())
            {
                return;
            }

            spawnCompleted =
                true;
        }

        private bool TrySpawnTestNpc()
        {
            List<RoomView> candidates =
                CollectTargetRooms();

            for (int roomIndex = 0;
                 roomIndex < candidates.Count;
                 roomIndex++)
            {
                RoomView targetRoom =
                    candidates[roomIndex];

                if (!TryChooseSpawnPosition(
                        targetRoom,
                        out GridPosition spawnPosition))
                {
                    continue;
                }

                CreateTestNpc(
                    targetRoom,
                    spawnPosition);

                return true;
            }

            return false;
        }

        private bool TryChooseSpawnPosition(
            RoomView targetRoom,
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
                TestNpcId,
                out spawnPosition);
        }

        private void CreateTestNpc(
            RoomView targetRoom,
            GridPosition spawnPosition)
        {
            NpcDefinition npcDefinition =
                GetOrCreateRuntimeDefinition();

            NpcRelationshipState relationship =
                NpcRelationshipRegistry.GetOrCreate(
                    npcDefinition.Id,
                    npcDefinition.InitialAffinity,
                    npcDefinition.StartsHostile);

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
                    new Color(
                        0.30f,
                        0.78f,
                        0.95f,
                        1f);
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
                relationship);

            targetRoom.RefreshMarkers();

            Debug.Log(
                $"[Project Delta] 113일차 테스트 NPC 배치 / {npcDefinition.DisplayName} / Room={targetRoom.PassageController.RoomId} / Position={spawnPosition}",
                this);
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

        private NpcDefinition GetOrCreateRuntimeDefinition()
        {
            if (runtimeDefinition != null)
            {
                return runtimeDefinition;
            }

            runtimeDefinition =
                ScriptableObject.CreateInstance<NpcDefinition>();

            runtimeDefinition.name =
                TestNpcId;

            runtimeDefinition.hideFlags =
                HideFlags.DontSave;

            runtimeDefinition.ConfigureRuntime(
                TestNpcId,
                "상인",
                NpcServiceType.Trade,
                NpcHostilityMode.CanBecomeHostile,
                0);

            return runtimeDefinition;
        }

        private void OnDestroy()
        {
            if (runtimeDefinition != null)
            {
                Destroy(
                    runtimeDefinition);

                runtimeDefinition =
                    null;
            }
        }
    }
}
