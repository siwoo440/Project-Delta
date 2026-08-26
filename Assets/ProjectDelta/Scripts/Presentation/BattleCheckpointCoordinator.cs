using System;
using System.Collections;
using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectDelta.Presentation
{
    public sealed class BattleCheckpointCoordinator : MonoBehaviour
    {
        private const int RestoreAttemptLimit = 300;

        private ExplorationMonsterEncounterController encounterController;
        private DungeonFloorController floorController;
        private EncounterResult observedEncounterResult;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != SceneNames.Dungeon)
            {
                return;
            }

            if (FindFirstObjectByType<BattleCheckpointCoordinator>() != null)
            {
                return;
            }

            GameObject coordinatorObject =
                new GameObject(
                    "BattleCheckpointCoordinator");

            coordinatorObject.AddComponent<BattleCheckpointCoordinator>(); // 전투 체크포인트 관리자 생성
        }

        private IEnumerator Start()
        {
            yield return null; // 던전·몬스터 복원 완료 대기

            ResolveReferences();

            if (BattleEncounterCheckpointStore.HasPending)
            {
                yield return RestorePendingBattle();
            }
        }

        private void Update()
        {
            if (encounterController == null)
            {
                ResolveReferences();
            }

            if (encounterController == null)
            {
                return;
            }

            EncounterResult currentResult =
                encounterController.LastEncounterResult;

            if (ReferenceEquals(
                    currentResult,
                    observedEncounterResult))
            {
                return;
            }

            observedEncounterResult =
                currentResult;

            if (currentResult != null
                && currentResult.Outcome == EncounterOutcome.Escaped)
            {
                ApplicationFlow.Current?.SaveDungeonProgress(); // 도주 완료 후 자동 저장
            }
        }

        private IEnumerator RestorePendingBattle()
        {
            for (int attempt = 0;
                 attempt < RestoreAttemptLimit;
                 attempt++)
            {
                if (!BattleEncounterCheckpointStore.HasPending)
                {
                    yield break;
                }

                ResolveReferences();

                BattleEncounterCheckpointData checkpoint =
                    BattleEncounterCheckpointStore.Pending;

                if (CanRestore(
                        checkpoint,
                        out ExplorationMonsterMarker marker)
                    && !encounterController.IsEncounterActive
                    && encounterController.TryBeginEncounterAtCurrentPosition())
                {
                    EncounterCommandResult result =
                        encounterController.SelectBattleCommand();

                    if (result != null
                        && result.Accepted)
                    {
                        Debug.Log(
                            $"[Project Delta] 82일차 전투 체크포인트 복원 / Room {checkpoint.RoomId} / Monster {marker.MonsterDefinitionId}",
                            this);

                        yield break;
                    }
                }

                yield return null;
            }

            BattleEncounterCheckpointData failedCheckpoint =
                BattleEncounterCheckpointStore.Pending;

            Debug.LogWarning(
                $"[Project Delta] 82일차 전투 체크포인트 자동 복원 실패 / Room {failedCheckpoint?.RoomId} / Monster {failedCheckpoint?.MonsterDefinitionId}",
                this);
        }

        private void ResolveReferences()
        {
            if (encounterController == null)
            {
                encounterController =
                    FindFirstObjectByType<ExplorationMonsterEncounterController>();
            }

            if (floorController == null)
            {
                floorController =
                    FindFirstObjectByType<DungeonFloorController>();
            }
        }

        private bool CanRestore(
            BattleEncounterCheckpointData checkpoint,
            out ExplorationMonsterMarker marker)
        {
            marker =
                null;

            if (checkpoint == null
                || !checkpoint.IsPending
                || encounterController == null
                || floorController == null
                || !floorController.SpawnedMonsters.TryGetValue(
                    checkpoint.RoomId,
                    out marker)
                || marker == null
                || !marker.gameObject.activeInHierarchy
                || marker.IsRoomEncounterCompleted)
            {
                return false;
            }

            if (marker.MonsterDefinitionId != checkpoint.MonsterDefinitionId
                || marker.GridPosition.X != checkpoint.MonsterGridPosition.x
                || marker.GridPosition.Z != checkpoint.MonsterGridPosition.y)
            {
                return false;
            }

            return GroupsMatch(
                marker.MonsterGroupDefinitionIds,
                checkpoint.MonsterGroupDefinitionIds);
        }

        private static bool GroupsMatch(
            IReadOnlyList<string> current,
            IReadOnlyList<string> saved)
        {
            if (current == null
                || saved == null
                || current.Count != saved.Count)
            {
                return false;
            }

            for (int index = 0;
                 index < current.Count;
                 index++)
            {
                if (!string.Equals(
                        current[index],
                        saved[index],
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
