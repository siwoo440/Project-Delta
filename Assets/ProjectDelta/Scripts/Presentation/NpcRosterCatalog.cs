using ProjectDelta.Data;

namespace ProjectDelta.Presentation
{
    // 133일차: 114일차부터 NpcRuntimeBootstrapController 안에만 있던 역할 목록을 꺼내서
    // CgGalleryController도 같은 데이터를 쓸 수 있게 한다 - NPC는 아직 정식 정의 에셋이
    // 없고(런타임에만 생성) 이 역할 4개가 사실상 "지금 존재하는 NPC 전체"다.
    public struct NpcRoleConfig
    {
        public string Id;
        public string DisplayName;
        public NpcServiceType ServiceTypes;

        public NpcRoleConfig(
            string id,
            string displayName,
            NpcServiceType serviceTypes)
        {
            Id = id;
            DisplayName = displayName;
            ServiceTypes = serviceTypes;
        }
    }

    public static class NpcRosterCatalog
    {
        public static readonly NpcRoleConfig[] RoleConfigs =
        {
            new NpcRoleConfig(
                "NPC_MERCHANT_TEST",
                "상인",
                NpcServiceType.Trade),
            new NpcRoleConfig(
                "NPC_HEALER_TEST",
                "치료사",
                NpcServiceType.Healing),
            new NpcRoleConfig(
                "NPC_GUIDE_TEST",
                "지도사",
                NpcServiceType.MapInformation
                | NpcServiceType.ExplorationInformation),
            new NpcRoleConfig(
                "NPC_TREASURE_HUNTER_TEST",
                "보물사냥꾼",
                NpcServiceType.RelicTrade
                | NpcServiceType.RelicResearch)
        };
    }
}
