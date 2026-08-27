using System;
using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    public sealed class EquipmentRollResult
    {
        public EquipmentRarity Rarity { get; }

        public StatBlock Bonuses { get; }

        public EquipmentRollResult(
            EquipmentRarity rarity,
            StatBlock bonuses)
        {
            Rarity =
                rarity;

            Bonuses =
                bonuses
                ?? new StatBlock();
        }
    }

    // 100일차: 장착 시점에 등급을 판정하고, 등급 배율 + 스탯별 랜덤 옵션 변동폭을 적용한
    // 실제 보너스를 만든다. EquipmentService(Domain)는 무작위성을 갖지 않도록,
    // 무작위 판정은 Application 계층인 이 서비스에서만 수행한다.
    public static class EquipmentRollService
    {
        // 등급 배율을 적용한 뒤 스탯마다 추가로 흔드는 랜덤 옵션 변동폭 (±10%).
        private const double VarianceRatio = 0.1;

        public static EquipmentRollResult Roll(
            ItemDefinition definition,
            Random random = null)
        {
            if (definition == null)
            {
                return new EquipmentRollResult(
                    EquipmentRarity.Common,
                    new StatBlock());
            }

            Random rng =
                random
                ?? new Random();

            EquipmentRarity rarity =
                RollRarity(
                    rng);

            StatBlock rolledBonuses =
                ApplyRarityAndVariance(
                    definition.EquipmentStatBonuses,
                    rarity,
                    rng);

            return new EquipmentRollResult(
                rarity,
                rolledBonuses);
        }

        // 등급별 드랍 가중치(EquipmentRarityRules.GetDropWeight)에 따라 하나를 뽑는다.
        public static EquipmentRarity RollRarity(
            Random random)
        {
            Random rng =
                random
                ?? new Random();

            EquipmentRarity[] rarities =
                (EquipmentRarity[])Enum.GetValues(
                    typeof(EquipmentRarity));

            int totalWeight =
                0;

            foreach (EquipmentRarity rarity in rarities)
            {
                totalWeight +=
                    EquipmentRarityRules.GetDropWeight(
                        rarity);
            }

            if (totalWeight <= 0)
            {
                return EquipmentRarity.Common;
            }

            int roll =
                rng.Next(
                    totalWeight);

            int cumulative =
                0;

            foreach (EquipmentRarity rarity in rarities)
            {
                cumulative +=
                    EquipmentRarityRules.GetDropWeight(
                        rarity);

                if (roll < cumulative)
                {
                    return rarity;
                }
            }

            return EquipmentRarity.Common;
        }

        private static StatBlock ApplyRarityAndVariance(
            StatBlock baseBonuses,
            EquipmentRarity rarity,
            Random random)
        {
            StatBlock source =
                baseBonuses
                ?? new StatBlock();

            double multiplier =
                EquipmentRarityRules.GetStatMultiplier(
                    rarity);

            return new StatBlock
            {
                MaxHealth = ScaleStat(source.MaxHealth, multiplier, random),
                MaxMana = ScaleStat(source.MaxMana, multiplier, random),
                MaxStamina = ScaleStat(source.MaxStamina, multiplier, random),
                Attack = ScaleStat(source.Attack, multiplier, random),
                Defense = ScaleStat(source.Defense, multiplier, random),
                Speed = ScaleStat(source.Speed, multiplier, random),
                Charm = ScaleStat(source.Charm, multiplier, random),
                Evasion = ScaleStat(source.Evasion, multiplier, random),
                Resistance = ScaleStat(source.Resistance, multiplier, random)
            };
        }

        private static int ScaleStat(
            int baseValue,
            double multiplier,
            Random random)
        {
            if (baseValue == 0)
            {
                return 0;
            }

            double varianceFactor =
                1.0
                + ((random.NextDouble() * 2.0 - 1.0) * VarianceRatio);

            double scaled =
                baseValue
                * multiplier
                * varianceFactor;

            int rounded =
                (int)Math.Round(
                    scaled,
                    MidpointRounding.AwayFromZero);

            // 원래 값이 있던 스탯이 등급 배율/변동폭 때문에 0으로 깎이지 않도록 보장한다.
            return baseValue > 0
                ? Math.Max(1, rounded)
                : Math.Min(-1, rounded);
        }
    }
}
