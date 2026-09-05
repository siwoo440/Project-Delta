using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 131일차: RunContext 여러 곳에 흩어진 값들을 MainEndingRule이 바로 쓸 수 있는
    // MainEndingConditions로 모은다 - Domain의 규칙 자체는 RunContext를 몰라도 되게 한다.
    public static class MainEndingConditionsBuilder
    {
        public static MainEndingConditions Build(
            RunContext context)
        {
            MainEndingConditions conditions =
                new MainEndingConditions();

            if (context == null)
            {
                return conditions;
            }

            conditions.BossOutcome =
                context.Battle.BossOutcome;

            conditions.Choice =
                context.Battle.FinalChoice;

            conditions.EquippedAndRelicCount =
                context.Equipment.EquippedCount
                + context.Relics.Relics.Count;

            conditions.CursedItemCount =
                context.Equipment.CursedEquippedCount()
                + CountCursedRelics(
                    context.Relics);

            StatBlock finalStats =
                context.Player.GetFinalStats();

            conditions.HpRatio =
                finalStats.MaxHealth > 0
                    ? context.Player.CurrentHp / (float)finalStats.MaxHealth
                    : 0f;

            conditions.StaminaRatio =
                finalStats.MaxStamina > 0
                    ? context.Player.CurrentStamina / (float)finalStats.MaxStamina
                    : 0f;

            // 5층은 마지막 층이라 AdvanceFloor로 넘어가는 일이 없어서, 지금까지 층을
            // 떠나며 기록해둔 개수(최대 4)에 "지금 있는 5층"의 실시간 탐색 여부를 더한다.
            conditions.FloorExplorationComplete =
                context.Statistics.FullyExploredFloorCount
                >= (FloorThemeSchedule.FloorCount - 1)
                && context.Dungeon.IsCurrentFloorFullyExplored();

            conditions.MonsterDexComplete =
                context.Statistics.HasFullMonsterDex;

            conditions.IndividualEndingConditionsMetCount =
                context.Statistics.IndividualEndingConditionsMetCount;

            conditions.AllRelationshipsMaxed =
                context.Statistics.HasAllRelationshipsMaxed;

            return conditions;
        }

        private static int CountCursedRelics(
            RelicRunState relics)
        {
            int count =
                0;

            foreach (RelicInstanceState relic in relics.Relics)
            {
                if (relic != null
                    && relic.IsCursed)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
