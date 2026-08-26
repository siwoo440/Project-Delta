using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDelta.Data
{
    [Serializable]
    public sealed class BattleEncounterCheckpointData
    {
        public bool IsPending;
        public string RoomId;
        public string MonsterDefinitionId;
        public Vector2Int MonsterGridPosition;
        public List<string> MonsterGroupDefinitionIds =
            new List<string>();
    }

    public static class BattleEncounterCheckpointStore
    {
        private static BattleEncounterCheckpointData pending;

        public static bool HasPending =>
            pending != null
            && pending.IsPending;

        public static BattleEncounterCheckpointData Pending =>
            Clone(
                pending);

        public static void Capture(
            string roomId,
            string monsterDefinitionId,
            Vector2Int monsterGridPosition,
            IReadOnlyList<string> monsterGroupDefinitionIds)
        {
            if (string.IsNullOrEmpty(roomId)
                || string.IsNullOrEmpty(monsterDefinitionId))
            {
                Clear();
                return;
            }

            pending =
                new BattleEncounterCheckpointData
                {
                    IsPending = true,
                    RoomId = roomId,
                    MonsterDefinitionId = monsterDefinitionId,
                    MonsterGridPosition = monsterGridPosition
                };

            if (monsterGroupDefinitionIds != null)
            {
                for (int index = 0;
                     index < monsterGroupDefinitionIds.Count;
                     index++)
                {
                    string definitionId =
                        monsterGroupDefinitionIds[index];

                    if (!string.IsNullOrEmpty(definitionId))
                    {
                        pending.MonsterGroupDefinitionIds.Add(
                            definitionId);
                    }
                }
            }

            if (pending.MonsterGroupDefinitionIds.Count == 0)
            {
                pending.MonsterGroupDefinitionIds.Add(
                    monsterDefinitionId);
            }
        }

        public static void Restore(
            BattleEncounterCheckpointData saved)
        {
            if (saved == null
                || !saved.IsPending)
            {
                Clear();
                return;
            }

            Capture(
                saved.RoomId,
                saved.MonsterDefinitionId,
                saved.MonsterGridPosition,
                saved.MonsterGroupDefinitionIds);
        }

        public static void ApplyTo(
            RunData runData)
        {
            if (runData == null)
            {
                return;
            }

            runData.BattleEncounterCheckpoint =
                HasPending
                    ? Clone(pending)
                    : new BattleEncounterCheckpointData();
        }

        public static void Clear()
        {
            pending =
                null;
        }

        private static BattleEncounterCheckpointData Clone(
            BattleEncounterCheckpointData source)
        {
            if (source == null)
            {
                return null;
            }

            BattleEncounterCheckpointData copy =
                new BattleEncounterCheckpointData
                {
                    IsPending = source.IsPending,
                    RoomId = source.RoomId,
                    MonsterDefinitionId = source.MonsterDefinitionId,
                    MonsterGridPosition = source.MonsterGridPosition
                };

            if (source.MonsterGroupDefinitionIds != null)
            {
                copy.MonsterGroupDefinitionIds.AddRange(
                    source.MonsterGroupDefinitionIds);
            }

            return copy;
        }
    }
}
