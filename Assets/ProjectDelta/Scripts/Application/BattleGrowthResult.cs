namespace ProjectDelta.Application
{
    public sealed class BattleGrowthResult
    {
        public int EarnedExperience { get; }
        public int PreviousLevel { get; }
        public int CurrentLevel { get; }
        public int PreviousExperience { get; }
        public int CurrentExperience { get; }
        public int GainedLevels { get; }
        public int GainedStatPoints { get; }

        public bool ReachedMaxLevel { get; }

        public BattleGrowthResult(
            int earnedExperience,
            int previousLevel,
            int currentLevel,
            int previousExperience,
            int currentExperience,
            int gainedLevels,
            int gainedStatPoints,
            bool reachedMaxLevel)
        {
            EarnedExperience =
                earnedExperience;

            PreviousLevel =
                previousLevel;

            CurrentLevel =
                currentLevel;

            PreviousExperience =
                previousExperience;

            CurrentExperience =
                currentExperience;

            GainedLevels =
                gainedLevels;

            GainedStatPoints =
                gainedStatPoints;

            ReachedMaxLevel =
                reachedMaxLevel;
        }
    }
}
