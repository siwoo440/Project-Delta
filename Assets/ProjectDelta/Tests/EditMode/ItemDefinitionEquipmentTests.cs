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

        // 102일차: 방어구 무게 분류 / 장신구 역할 / 가방 등급이 설정한 값 그대로 노출되는지 확인한다.
        [Test]
        public void EquipmentClassification_ExposesConfiguredTags()
        {
            SetPrivateField(
                "armorWeightClass",
                ArmorWeightClass.Heavy);

            SetPrivateField(
                "accessoryRole",
                AccessoryRole.Evasion);

            SetPrivateField(
                "bagTier",
                BagTier.Large);

            Assert.That(
                definition.ArmorWeightClass,
                Is.EqualTo(
                    ArmorWeightClass.Heavy));

            Assert.That(
                definition.AccessoryRole,
                Is.EqualTo(
                    AccessoryRole.Evasion));

            Assert.That(
                definition.BagTier,
                Is.EqualTo(
                    BagTier.Large));
        }

        // 102일차: 분류 태그를 설정하지 않은 기존 에셋은 전부 None으로 유지되어야 한다.
        [Test]
        public void EquipmentClassification_DefaultsToNone()
        {
            Assert.That(
                definition.ArmorWeightClass,
                Is.EqualTo(
                    ArmorWeightClass.None));

            Assert.That(
                definition.AccessoryRole,
                Is.EqualTo(
                    AccessoryRole.None));

            Assert.That(
                definition.BagTier,
                Is.EqualTo(
                    BagTier.None));
        }

        // 103일차: 저주 장비 플래그가 설정한 값 그대로 노출되는지 확인한다.
        [Test]
        public void IsCursed_ExposesConfiguredValue()
        {
            SetPrivateField(
                "isCursed",
                true);

            Assert.That(
                definition.IsCursed,
                Is.True);
        }

        [Test]
        public void IsCursed_DefaultsToFalse()
        {
            Assert.That(
                definition.IsCursed,
                Is.False);
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
