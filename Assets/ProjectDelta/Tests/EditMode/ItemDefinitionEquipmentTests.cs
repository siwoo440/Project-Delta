using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class ItemDefinitionEquipmentTests
    {
        private ItemDefinition definition;

        [SetUp]
        public void SetUp()
        {
            definition =
                ScriptableObject.CreateInstance<ItemDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            if (definition != null)
            {
                Object.DestroyImmediate(
                    definition);
            }
        }

        [Test]
        public void EquipmentMetadata_ExposesSlotAndBaseStatBonuses()
        {
            StatBlock bonuses =
                new StatBlock
                {
                    Attack = 7,
                    Defense = 3,
                    MaxHealth = 12
                };

            SetPrivateField(
                "equipmentSlot",
                EquipmentSlotType.ChestArmor);

            SetPrivateField(
                "equipmentStatBonuses",
                bonuses);

            Assert.That(
                definition.EquipmentSlot,
                Is.EqualTo(
                    EquipmentSlotType.ChestArmor));

            Assert.That(
                definition.EquipmentStatBonuses,
                Is.SameAs(
                    bonuses));
        }

        // 101일차: 요구 조건 데이터가 노출되는지 확인한다.
        [Test]
        public void EquipmentRequirements_ExposesConfiguredThresholds()
        {
            StatBlock requirements =
                new StatBlock
                {
                    Attack = 30,
                    Speed = 20,
                    Charm = 10,
                    Resistance = 15
                };

            SetPrivateField(
                "equipmentRequirements",
                requirements);

            Assert.That(
                definition.EquipmentRequirements,
                Is.SameAs(
                    requirements));
        }

        // 101일차: 요구 조건을 설정하지 않으면 null이 아니라 빈 StatBlock을 반환해야 한다.
        [Test]
        public void EquipmentRequirements_DefaultsToEmptyStatBlock()
        {
            SetPrivateField(
                "equipmentRequirements",
                null);

            Assert.That(
                definition.EquipmentRequirements,
                Is.Not.Null);

            Assert.That(
                definition.EquipmentRequirements.Attack,
                Is.EqualTo(0));
        }

        [Test]
        public void EquipmentCategory_MaxStackSize_IsAlwaysOne()
        {
            SetPrivateField(
                "category",
                ItemCategory.Equipment);

            SetPrivateField(
                "maxStackSize",
                99);

            Assert.That(
                definition.MaxStackSize,
                Is.EqualTo(
                    1));
        }

        private void SetPrivateField(
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
