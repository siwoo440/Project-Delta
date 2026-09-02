namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 실제 게임에서 사용하는 아이템 7분류와 기존 에셋 마이그레이션용 미분류 상태다.
    public enum ItemCategory // 아이템 분류
    {
        Uncategorized = 0, // 기존 ItemDefinition 에셋이 자동으로 특정 종류가 되지 않도록 예약한 미분류 값
        Consumable = 1, // 소비 아이템
        ExplorationTool = 2, // 탐험 도구
        KeyItem = 3, // 중요 아이템
        Treasure = 4, // 보물
        Equipment = 5, // 장비
        Relic = 6, // 유물
        Cursed = 7 // 저주 아이템
    }
}
