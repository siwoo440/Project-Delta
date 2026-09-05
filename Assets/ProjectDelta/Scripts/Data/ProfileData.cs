using System;
using System.Collections.Generic;

namespace ProjectDelta.Data
{
    // Persists across runs. No manual save/load or rollback to a past point (기획서 9.1).
    [Serializable]
    public sealed class ProfileData
    {
        public PermanentGrowth PermanentGrowth = new PermanentGrowth();
        public PermanentRecord PermanentRecord = new PermanentRecord();
        public LifetimeStats LifetimeStats = new LifetimeStats();
    }

    [Serializable]
    public sealed class PermanentGrowth
    {
        public int MemoryShards;
        public int TotalMemoryShardsEarned;

        // 126일차: 영구 능력치 강화 - PermanentStatUpgradeRule.UpgradableStatIds를 키로 쓴다.
        // 레벨이 오를수록 GetFinalStats()에 합산되는 보너스 스탯도, 다음 구매 비용도 커진다.
        public Dictionary<string, int> PermanentStatUpgradeLevels =
            new Dictionary<string, int>();

        // 127일차: 인벤토리 슬롯 영구 확장 레벨. InventorySlotUpgradeRule이 비용/효과를 계산한다.
        public int InventorySlotUpgradeLevel;

        // 128일차: 유물 보유량 영구 확장 레벨. RelicSlotUpgradeRule이 비용/효과를 계산한다.
        public int RelicSlotUpgradeLevel;

        // 130일차: 상점 강화 4종(기획서 6.6절). ShopUpgradeRule이 비용/효과를 계산한다.
        public int ShopDiscountLevel;
        public int ShopStockLevel;
        public int ShopRareChanceLevel;
        public int ShopSellBonusLevel;

        // TODO 6.6절 영구 강화 구현 시 추가: 탐험 강화
        // (영구 능력치 강화는 126일차, 인벤토리 확장은 127일차, 유물 확장은 128일차,
        // 상점 강화는 130일차에 완료)

        public int StartingGold;
        public List<string> StartingConsumableItemIds = new List<string>();
        public List<string> UnlockedSkillIds = new List<string>();
        public List<string> BonusStartingSkillCandidateIds = new List<string>();

        // 119일차: 별도 이벤트 전투 공통 행동 12종(EventBattleActionCatalog, 118일차)의
        // 영구 숙련도 - 행동 ID를 키로 쓴다. 실행 취소/롤백 없이(기획서 9.1) 오래 쓸수록 쌓인다.
        public Dictionary<string, EventBattleActionProficiencyRecord> EventBattleActionProficiency =
            new Dictionary<string, EventBattleActionProficiencyRecord>();
    }

    [Serializable]
    public sealed class EventBattleActionProficiencyRecord
    {
        public int Level = 1;
        public int Experience;
    }

    [Serializable]
    public sealed class PermanentRecord
    {
        public List<string> ObservedMonsterIds = new List<string>();
        public Dictionary<string, int> NpcAffinity = new Dictionary<string, int>();
        public List<string> NpcRelationshipEventIds = new List<string>();
        public List<string> UnlockedMainEndingIds = new List<string>();
        public List<string> UnlockedMonsterEndingIds = new List<string>();
        public List<string> UnlockedNpcEndingIds = new List<string>();
        public List<string> DefeatRecordIds = new List<string>();
        public List<string> UnlockedCgIds = new List<string>();
        public List<string> ReplayableEventIds = new List<string>();
        public List<string> UnlockedAchievementIds = new List<string>();
        public List<string> StoryFlags = new List<string>();
        public List<string> FirstDiscoveryIds = new List<string>();
    }

    [Serializable]
    public sealed class LifetimeStats
    {
        public float TotalPlaytimeSeconds;
        public int RunsCompleted;
        public int CharacterEndingsReached;
        public int GameOvers;
        public int RunsAbandoned;
        public int NormalBattleWins;
        public int AdultBattleWins;
        public int RoomsDiscovered;
        public int SecretRoomsFound;
        public int ChestsOpened;
        public int MonstersDefeated;
        public int MonstersSatisfiedAway;
        public int TotalMemoryShardsCollected;
    }
}
