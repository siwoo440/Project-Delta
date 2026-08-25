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

            Player = PlayerRunState.CreateDefault(); // 54일차: 기획서 6.1 기본 능력치로 시작
            Dungeon = new DungeonRunState();
            Inventory = new InventoryRunState();
            Skills = new SkillRunState();
            Characters = new CharacterRunState();
            Events = new EventRunState();
            Battle = new BattleRunState();
            Reward = new RewardRunState();
            Statistics = new RunStatistics();
        }

        // 24일차: ApplicationFlow.StartNewGame()/ContinueGame()에서 호출한다.
        public static RunContext Begin(string runId)
        {
            if (Current != null)
            {
                throw new InvalidOperationException("A run is already in progress. Call End() before starting a new one.");
            }

            Current = new RunContext(runId);
            return Current;
        }

        // 24일차: ApplicationFlow.ReturnToTitle()에서 런 포기 시 호출한다.
        // TODO: 게임 오버·엔딩 시에도 호출해야 한다 (해당 시스템이 생기는 일차에 연결).
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
