using System;
using System.Collections.Generic;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Data
{
    [CreateAssetMenu(
        fileName = "ItemDefinition",
        menuName = "ProjectDelta/Data/Item Definition")]
    public sealed class ItemDefinition : DefinitionBase
    {
        [SerializeField]
        private string displayName;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        [TextArea(2, 4)]
        private string description;

        [SerializeField]
        private ItemCategory category =
            ItemCategory.Uncategorized;

        [SerializeField]
        [Min(1)]
        private int maxStackSize = 1;

        // 105일차: 상점 정가. 판매가는 ShopService가 이 값의 50%로 계산한다.
        [SerializeField]
        [Min(0)]
        private int basePrice;

        [Header("Equipment")]
        [SerializeField]
        private EquipmentSlotType equipmentSlot =
            EquipmentSlotType.Weapon;

        // 97일차에는 데이터만 보관하고 실제 최종 능력치 합산은 99일차에 연결한다.
        [SerializeField]
        private StatBlock equipmentStatBonuses =
            new StatBlock();

        // 101일차: 공격력·속도·매력·저항 요구치. 값이 0인 스탯은 요구 조건이 없는 것으로 취급한다.
        [SerializeField]
        private StatBlock equipmentRequirements =
            new StatBlock();

        // 102일차: 방어구(ChestArmor·Leggings·Boots)에만 의미 있는 무게 분류 태그.
        [SerializeField]
        private ArmorWeightClass armorWeightClass =
            ArmorWeightClass.None;

        // 102일차: 장신구(Accessory)에만 의미 있는 역할 분류 태그.
        [SerializeField]
        private AccessoryRole accessoryRole =
            AccessoryRole.None;

        // 102일차: 가방 등급. None이 아니면 BagExpansionService가 처리 가능한
        // 인벤토리 확장 아이템으로 취급한다 (6부위 장비 슬롯과는 별개).
        [SerializeField]
        private BagTier bagTier =
            BagTier.None;

        // 103일차: 저주 장비 여부. 저주 장비는 EquipmentStatBonuses에 강점(양수)과
        // 약점(음수)을 함께 담을 수 있으며, UI가 이 플래그를 보고 불리한 옵션까지
        // 전부 공개해야 한다. 별도의 장착 제한은 없다 - ItemCategory.Equipment로
        // 분류돼 있으면 일반 장비와 동일하게 장착·해제된다.
        [SerializeField]
        private bool isCursed;

        // 93일차: 아이템을 사용할 수 있는 상황.
        [SerializeField]
        private ItemUseContext useContext =
            ItemUseContext.Both;

        // 93일차: 하나의 아이템이 적용하는 실제 사용 효과 목록.
        [SerializeField]
        private ItemUseEffectDefinition[] useEffects =
            Array.Empty<ItemUseEffectDefinition>();

        public string DisplayName =>
            displayName;

        public Sprite Icon =>
            icon;

        public string Description =>
            description;

        public ItemCategory Category =>
            category;

        public int BasePrice =>
            Mathf.Max(
                0,
                basePrice);

        public int MaxStackSize =>
            category == ItemCategory.Equipment
                ? 1
                : Mathf.Max(
                    1,
                    maxStackSize);

        public EquipmentSlotType EquipmentSlot =>
            equipmentSlot;

        public StatBlock EquipmentStatBonuses =>
            equipmentStatBonuses
            ?? new StatBlock();

        public StatBlock EquipmentRequirements =>
            equipmentRequirements
            ?? new StatBlock();

        public ArmorWeightClass ArmorWeightClass =>
            armorWeightClass;

        public AccessoryRole AccessoryRole =>
            accessoryRole;

        public BagTier BagTier =>
            bagTier;

        public bool IsCursed =>
            isCursed;

        public ItemUseContext UseContext =>
            useContext;

        public IReadOnlyList<ItemUseEffectDefinition> UseEffects =>
            useEffects
            ?? Array.Empty<ItemUseEffectDefinition>();
    }
}
