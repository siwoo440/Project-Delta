using System;
using System.Collections.Generic;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 41일차: Monster Encounter가 배정된 방 내부에서 실제 스폰 칸 하나를 고른다.
    public sealed class MonsterSpawnPositionService
    {
        public IReadOnlyList<GridPosition> BuildCandidates(
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            IEnumerable<RoomExit> connectedExits,
            IEnumerable<GridPosition> occupiedPositions)
        {
            if (minX > maxX)
            {
                throw new ArgumentException(
                    "minX는 maxX보다 클 수 없습니다.");
            }

            if (minZ > maxZ)
            {
                throw new ArgumentException(
                    "minZ는 maxZ보다 클 수 없습니다.");
            }

            HashSet<GridPosition> reserved =
                new HashSet<GridPosition>();

            if (occupiedPositions != null)
            {
                foreach (GridPosition occupied
                         in occupiedPositions)
                {
                    reserved.Add(occupied);
                }
            }

            if (connectedExits != null)
            {
                foreach (RoomExit exit
                         in connectedExits)
                {
                    reserved.Add(
                        exit.LocalPosition);

                    GridPosition inwardDelta =
                        GridMovement.GetDirectionDelta(
                            RoomGridLayout.GetOpposite(
                                exit.Direction));

                    GridPosition insideSafetyPosition =
                        new GridPosition(
                            exit.LocalPosition.X
                            + inwardDelta.X,
                            exit.LocalPosition.Z
                            + inwardDelta.Z);

                    if (insideSafetyPosition.X >= minX
                        && insideSafetyPosition.X <= maxX
                        && insideSafetyPosition.Z >= minZ
                        && insideSafetyPosition.Z <= maxZ)
                    {
                        reserved.Add(
                            insideSafetyPosition);
                    }
                }
            }

            List<GridPosition> candidates =
                new List<GridPosition>();

            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    GridPosition position =
                        new GridPosition(x, z);

                    if (!reserved.Contains(position))
                    {
                        candidates.Add(position);
                    }
                }
            }

            return candidates;
        }

        public bool TryChoosePosition(
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            IEnumerable<RoomExit> connectedExits,
            IEnumerable<GridPosition> occupiedPositions,
            int dungeonSeed,
            string roomId,
            string monsterDefinitionId,
            out GridPosition position)
        {
            IReadOnlyList<GridPosition> candidates =
                BuildCandidates(
                    minX,
                    maxX,
                    minZ,
                    maxZ,
                    connectedExits,
                    occupiedPositions);

            if (candidates.Count == 0)
            {
                position = GridPosition.Zero;
                return false;
            }

            float roll =
                RoomEncounterPlacementService.CalculateStableRoll(
                    dungeonSeed,
                    roomId,
                    $"{monsterDefinitionId}:SPAWN");

            int index =
                Math.Min(
                    candidates.Count - 1,
                    (int)(roll * candidates.Count));

            position =
                candidates[index];

            return true;
        }
    }
}
