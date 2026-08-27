using System;
using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    // 100일차: 등급 판정과 랜덤 옵션 스탯 굴림을 검증한다.
    // System.Random의 내부 구현에 의존하지 않도록, 정확한 값 대신
    // 통계적/구조적 불변식(범위, 분포 경향, null 처리)을 확인한다.
    public sealed class EquipmentRollServiceTests
    {
        [Test]
        public void Roll_NullDefinition_ReturnsCommonWithEmptyBonuses()
        {
            EquipmentRollResult result =
                EquipmentRollService.Roll(
                    null,
                    new System.Random(1));

            Assert.That(
                result.Rarity,
                Is.EqualTo(
                    EquipmentRarity.Common));

            Assert.That(
                result.Bonuses.Attack,
                Is.EqualTo(0));
        }

        [Test]
        public void RollRarity_ManyTrials_OnlyReturnsDefinedRarities()
        {
            System.Random random =
                new System.Random(
                    12345);

            for (int trial = 0;
                 trial < 1000;
                 trial++)
            {
                EquipmentRarity rarity =
                    EquipmentRollService.RollRarity(
                        random);

                Assert.That(
                    Enum.IsDefined(
                        typeof(EquipmentRarity),
                        rarity),
                    Is.True);
            }
        }

        [Test]
        public void RollRarity_ManyTrials_CommonIsMoreFrequentThanLegendary()
        {
            // 가중치가 100 : 3으로 크게 차이나므로, 충분한 시행에서는
            // 항상 일반(Common)이 전설(Legendary)보다 많이 나와야 한다.
            System.Random random =
                new System.Random(
                    2026);

            int commonCount =
                0;

            int legendaryCount =
                0;

            for (int trial = 0;
                 trial < 5000;
                 trial++)
            {
                EquipmentRarity rarity =
                    EquipmentRollService.RollRarity(
                        random);

                if (rarity == EquipmentRarity.Common)
                {
                    commonCount++;
                }
                else if (rarity == EquipmentRarity.Legendary)
                {
                    legendaryCount++;
                }
            }

            Assert.That(
                commonCount,
                Is.GreaterThan(
                    legendaryCount));
        }

        [Test]
        public void Roll_ManyTrials_ScaledStatsStayWithinRarityBounds()
        {
            ItemDefinition definition =
                CreateEquipmentDefinition(
                    new StatBlock
                    {
                        Attack = 20,
                        Defense = 0
                    });

            System.Random random =
                new System.Random(
                    777);

            try
            {
                for (int trial = 0;
                     trial < 500;
                     trial++)
                {
                    EquipmentRollResult result =
                        EquipmentRollService.Roll(
                            definition,
                            random);

                    double multiplier =
                        EquipmentRarityRules.GetStatMultiplier(
                            result.Rarity);

                    // 등급 배율 ± 10% 랜덤 옵션 변동폭 + 반올림 오차 여유.
                    double minExpected =
                        (20 * multiplier * 0.9) - 1.0;

                    double maxExpected =
                        (20 * multiplier * 1.1) + 1.0;

                    Assert.That(
                        result.Bonuses.Attack,
                        Is.GreaterThanOrEqualTo(
                            minExpected));

                    Assert.That(
                        result.Bonuses.Attack,
                        Is.LessThanOrEqualTo(
                            maxExpected));

                    // 기본값이 0인 스탯은 등급/변동폭과 무관하게 항상 0을 유지해야 한다.
                    Assert.That(
                        result.Bonuses.Defense,
                        Is.EqualTo(0));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    definition);
            }
        }

        private static ItemDefinition CreateEquipmentDefinition(
            StatBlock equipmentBonuses)
        {
            ItemDefinition definition =
                ScriptableObject.CreateInstance<ItemDefinition>();

            SetPrivateField(
                definition,
                "category",
                ItemCategory.Equipment);

            SetPrivateField(
                definition,
                "equipmentSlot",
                EquipmentSlotType.Weapon);

            SetPrivateField(
                definition,
                "equipmentStatBonuses",
                equipmentBonuses);

            return definition;
        }

        private static void SetPrivateField(
            ItemDefinition definition,
            string fieldName,
            object value)
        {
            FieldInfo field =
                typeof(ItemDefinition).GetField(
                    fieldName,
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"Private field not found: {fieldName}");

            field.SetValue(
                definition,
                value);
        }
    }
}
