using System;
using System.Collections.Generic;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 113일차: 상자처럼 한 칸을 점유하는 콘텐츠가 방의 이동 가능 영역을 끊지 않는 위치만 고른다.
    public sealed class RoomBlockingPlacementService
    {
        private static readonly CardinalDirection[] Directions =
        {
            CardinalDirection.North,
            CardinalDirection.East,
            CardinalDirection.South,
            CardinalDirection.West
        };

        public bool TryChoosePosition(
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            IEnumerable<RoomExit> connectedExits,
            IEnumerable<GridPosition> occupiedPositions,
            Func<GridPosition, CardinalDirection, bool> canPass,
            int dungeonSeed,
            string roomId,
            string contentKey,
            out GridPosition position)
        {
            MonsterSpawnPositionService basePlacement =
                new MonsterSpawnPositionService();

            IReadOnlyList<GridPosition> candidates =
                basePlacement.BuildCandidates(
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
                    contentKey);

            int startIndex =
                Math.Min(
                    candidates.Count - 1,
                    (int)(roll * candidates.Count));

            for (int offset = 0; offset < candidates.Count; offset++)
            {
                int index =
                    (startIndex + offset) % candidates.Count;

                GridPosition candidate =
                    candidates[index];

                if (PreservesTraversableArea(
                        minX,
                        maxX,
                        minZ,
                        maxZ,
                        candidate,
                        canPass))
                {
                    position = candidate;
                    return true;
                }
            }

            position = GridPosition.Zero;
            return false;
        }

        public bool PreservesTraversableArea(
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            GridPosition blockedPosition,
            Func<GridPosition, CardinalDirection, bool> canPass)
        {
            if (!IsInside(
                    blockedPosition,
                    minX,
                    maxX,
                    minZ,
                    maxZ))
            {
                return false;
            }

            if (!TryFindAnchor(
                    minX,
                    maxX,
                    minZ,
                    maxZ,
                    blockedPosition,
                    out GridPosition anchor))
            {
                return false;
            }

            HashSet<GridPosition> baseline =
                CollectReachable(
                    minX,
                    maxX,
                    minZ,
                    maxZ,
                    anchor,
                    null,
                    canPass);

            if (!baseline.Contains(blockedPosition))
            {
                return true;
            }

            HashSet<GridPosition> afterPlacement =
                CollectReachable(
                    minX,
                    maxX,
                    minZ,
                    maxZ,
                    anchor,
                    blockedPosition,
                    canPass);

            return afterPlacement.Count
                == baseline.Count - 1;
        }

        private static HashSet<GridPosition> CollectReachable(
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            GridPosition start,
            GridPosition? blockedPosition,
            Func<GridPosition, CardinalDirection, bool> canPass)
        {
            HashSet<GridPosition> visited =
                new HashSet<GridPosition>();

            if (blockedPosition.HasValue
                && start == blockedPosition.Value)
            {
                return visited;
            }

            Queue<GridPosition> queue =
                new Queue<GridPosition>();

            visited.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                GridPosition current =
                    queue.Dequeue();

                for (int directionIndex = 0;
                     directionIndex < Directions.Length;
                     directionIndex++)
                {
                    CardinalDirection direction =
                        Directions[directionIndex];

                    if (canPass != null
                        && !canPass(current, direction))
                    {
                        continue;
                    }

                    GridPosition delta =
                        GridMovement.GetDirectionDelta(direction);

                    GridPosition next =
                        new GridPosition(
                            current.X + delta.X,
                            current.Z + delta.Z);

                    if (!IsInside(
                            next,
                            minX,
                            maxX,
                            minZ,
                            maxZ))
                    {
                        continue;
                    }

                    if (blockedPosition.HasValue
                        && next == blockedPosition.Value)
                    {
                        continue;
                    }

                    if (visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return visited;
        }

        private static bool TryFindAnchor(
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            GridPosition blockedPosition,
            out GridPosition anchor)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    GridPosition candidate =
                        new GridPosition(x, z);

                    if (candidate != blockedPosition)
                    {
                        anchor = candidate;
                        return true;
                    }
                }
            }

            anchor = GridPosition.Zero;
            return false;
        }

        private static bool IsInside(
            GridPosition position,
            int minX,
            int maxX,
            int minZ,
            int maxZ)
        {
            return position.X >= minX
                && position.X <= maxX
                && position.Z >= minZ
                && position.Z <= maxZ;
        }
    }
}
