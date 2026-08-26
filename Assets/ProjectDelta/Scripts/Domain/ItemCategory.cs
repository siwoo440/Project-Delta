namespace ProjectDelta.Domain
{
    // 실제 게임에서 사용하는 아이템 7분류와 기존 에셋 마이그레이션용 미분류 상태다.
    public enum ItemCategory
    {
        // 기존 ItemDefinition 에셋이 자동으로 특정 종류가 되지 않도록 0번을 예약한다.
        Uncategorized = 0,

        // 소비 아이템이다.
        Consumable = 1,

        // 탐험 도구다.
        ExplorationTool = 2,

        // 중요 아이템이다.
        KeyItem = 3,

        // 보물이다.
        Treasure = 4,

        // 장비다.
        Equipment = 5,

        // 유물이다.
        Relic = 6,

        // 저주 아이템이다.
        Cursed = 7
    }
}
