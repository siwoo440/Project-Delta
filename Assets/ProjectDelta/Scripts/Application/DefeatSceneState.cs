using System;

namespace ProjectDelta.Application
{
    public sealed class RunDefeatSummary
    {
        public BattleDefeatReason Reason { get; }
        public string AttackerInstanceId { get; }
        public string AttackerDefinitionId { get; }
        public int RoundNumber { get; }
        public int FloorNumber { get; }

        public bool HasAttacker =>
            !string.IsNullOrEmpty(
                AttackerInstanceId);

        public RunDefeatSummary(
            BattleDefeatReason reason,
            string attackerInstanceId,
            string attackerDefinitionId,
            int roundNumber,
            int floorNumber)
        {
            Reason =
                reason;

            AttackerInstanceId =
                attackerInstanceId;

            AttackerDefinitionId =
                attackerDefinitionId;

            RoundNumber =
                Math.Max(
                    1,
                    roundNumber);

            FloorNumber =
                Math.Max(
                    1,
                    floorNumber);
        }
    }

    public static class DefeatSceneState
    {
        public static RunDefeatSummary Current { get; private set; }

        public static RunDefeatSummary Capture(
            BattleDefeatRecord record,
            int floorNumber)
        {
            if (record == null)
            {
                Current =
                    null;

                return null;
            }

            Current =
                new RunDefeatSummary(
                    record.Reason,
                    record.AttackerInstanceId,
                    record.AttackerDefinitionId,
                    record.RoundNumber,
                    floorNumber);

            return Current;
        }

        public static void Clear()
        {
            Current =
                null;
        }
    }
}
