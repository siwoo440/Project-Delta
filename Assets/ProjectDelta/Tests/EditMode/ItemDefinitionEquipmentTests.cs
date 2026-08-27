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
