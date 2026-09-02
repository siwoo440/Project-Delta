namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 97일차: 플레이어가 동시에 착용할 수 있는 6개 장비 슬롯이다.
    public enum EquipmentSlotType // 장비 슬롯 부위
    {
        Weapon = 0, // 무기 슬롯
        Helmet = 1, // 투구 슬롯
        ChestArmor = 2, // 상의 갑옷 슬롯
        Leggings = 3, // 하의 슬롯
        Boots = 4, // 신발 슬롯
        Accessory = 5 // 장신구 슬롯
    }
}
