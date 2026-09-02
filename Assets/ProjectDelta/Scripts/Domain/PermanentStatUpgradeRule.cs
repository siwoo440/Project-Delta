using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    // 126일차: 기억의 조각으로 사는 영구 능력치 강화 - 공격/방어/최대체력 3종만 우선 지원한다.
    // ProfileData(Data)를 직접 참조하지 않고 평범한 Dictionary<string,int>만 받아서
    // Domain의 "제로 의존성" 원칙(RoomTypeRules·MonsterTierRules와 같은 이유)을 지킨다.
    public static class PermanentStatUpgradeRule
    {
        public const string Attack = "Attack";
        public const string Defense = "Defense";
        public const string MaxHealth = "MaxHealth";

        public static readonly string[] UpgradableStatIds =
        {
            Attack,
            Defense,
            MaxHealth
        };

        public const int MaxLevel = 10;

        private const int BaseCost = 5;

        public static int GetLevel(
            IReadOnlyDictionary<string, int> levels,
            string statId)
        {
            if (levels == null
                || !levels.TryGetValue(
                    statId,
                    out int level))
            {
                return 0;
            }

            return level < 0
                ? 0
                : level;
        }

        // 다음 레벨을 사는 데 드는 비용 - 레벨이 오를수록 비싸진다.
        public static int GetNextLevelCost(
            int currentLevel)
        {
            return BaseCost
                * (currentLevel + 1);
        }

        public static int GetEffectPerLevel(
            string statId)
        {
            switch (statId)
            {
                case MaxHealth:
                    return 10;

                default:
                    return 2;
            }
        }

        public static bool TryGetUpgradeCost(
            IReadOnlyDictionary<string, int> levels,
            string statId,
            out int cost)
        {
            int currentLevel =
                GetLevel(
                    levels,
                    statId);

            if (currentLevel >= MaxLevel)
            {
                cost = 0;
                return false;
            }

            cost =
                GetNextLevelCost(
                    currentLevel);

            return true;
        }

        // 런 시작 시 GetFinalStats()에 그대로 더해질 보너스 StatBlock을 만든다.
        public static StatBlock BuildBonusStats(
            IReadOnlyDictionary<string, int> levels)
        {
            StatBlock bonus =
                new StatBlock();

            if (levels == null)
            {
                return bonus;
            }

            for (int index = 0;
                 index < UpgradableStatIds.Length;
                 index++)
            {
                string statId =
                    UpgradableStatIds[index];

                int level =
                    GetLevel(
                        levels,
                        statId);

                if (level <= 0)
                {
                    continue;
                }

                int amount =
                    level
                    * GetEffectPerLevel(
                        statId);

                switch (statId)
                {
                    case Attack:
                        bonus.Attack +=
                            amount;
                        break;

                    case Defense:
                        bonus.Defense +=
                            amount;
                        break;

                    case MaxHealth:
                        bonus.MaxHealth +=
                            amount;
                        break;
                }
            }

            return bonus;
        }
    }
}
