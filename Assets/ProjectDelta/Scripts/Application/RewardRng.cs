using System;

namespace ProjectDelta.Application
{
    // 80일차: 전투 명중·피해용 CombatRng와 분리된 보상 전용 난수 발생원.
    public sealed class RewardRng : IRandomSource
    {
        private readonly Random random;

        public RewardRng(
            int seed)
        {
            random =
                new Random(seed);
        }

        public RewardRng()
            : this(
                Environment.TickCount)
        {
        }

        public int NextInt(
            int minInclusive,
            int maxExclusive)
        {
            return random.Next(
                minInclusive,
                maxExclusive);
        }
    }
}
