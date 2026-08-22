using System;

namespace ProjectDelta.Domain
{
    // The live runtime state for the single active run. Created when a run
    // starts, discarded on game over, ending, or run abandonment (기획서 10.2).
    //
    // This is not the save shape - see ProjectDelta.Data.RunData for that.
    // RunContext is what gameplay code reads and writes during play; a later
    // SaveService flattens it into RunData when writing to disk.
    public sealed class RunContext
    {
        public static RunContext Current { get; private set; }

        public RunMetadata Metadata { get; }
        public PlayerRunState Player { get; }
        public DungeonRunState Dungeon { get; }
        public InventoryRunState Inventory { get; }
        public SkillRunState Skills { get; }
        public CharacterRunState Characters { get; }
        public EventRunState Events { get; }
        public BattleRunState Battle { get; }
        public RewardRunState Reward { get; }
        public RunStatistics Statistics { get; }

        private RunContext(string runId)
        {
            Metadata = new RunMetadata
            {
                RunId = runId,
                StartedAtIso8601 = DateTime.UtcNow.ToString("o")
            };

            Player = new PlayerRunState();
            Dungeon = new DungeonRunState();
            Inventory = new InventoryRunState();
            Skills = new SkillRunState();
            Characters = new CharacterRunState();
            Events = new EventRunState();
            Battle = new BattleRunState();
            Reward = new RewardRunState();
            Statistics = new RunStatistics();
        }

        // TODO: not yet called anywhere - wire this in once the new-game flow exists.
        public static RunContext Begin(string runId)
        {
            if (Current != null)
            {
                throw new InvalidOperationException("A run is already in progress. Call End() before starting a new one.");
            }

            Current = new RunContext(runId);
            return Current;
        }

        // TODO: not yet called anywhere - wire this in on game over / ending / run abandon.
        public static void End()
        {
            Current = null;
        }
    }

    [Serializable]
    public sealed class RunMetadata
    {
        public string RunId;
        public string StartedAtIso8601;
    }
}
