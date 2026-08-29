using System;

namespace ProjectDelta.Data
{
    // 113일차: 한 NPC가 여러 서비스를 함께 제공할 수 있으므로 Flags로 관리한다.
    [Flags]
    public enum NpcServiceType
    {
        None = 0,
        Trade = 1 << 0,
        Healing = 1 << 1,
        MapInformation = 1 << 2,
        RelicTrade = 1 << 3,
        RelicResearch = 1 << 4,
        ExplorationInformation = 1 << 5
    }
}
