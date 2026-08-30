namespace ProjectDelta.Domain
{
    // 114일차: NPC 한 명(마커 하나)이 실제로 제공하는 서비스 상태 - 지금은
    // 상인의 재고(ShopRunState)만 담는다. 전역 싱글턴인 RunContext.Shop과 달리
    // NPC별로 독립된 인스턴스라 상인마다 재고가 다를 수 있다.
    // NPC GameObject는 같은 층 안에서는 파괴되지 않으므로(RoomView가 계속 살아있다)
    // 이 상태를 NpcContentMarker에 붙여두는 것만으로 "같은 층 재방문 시 재고·가격
    // 유지"가 저절로 만족된다 - 별도 저장 로직이 필요한 건 층 이동/불러오기뿐이며,
    // 그건 115일차 이후 범위다.
    public sealed class NpcServiceRunState
    {
        public ShopRunState Shop { get; } =
            new ShopRunState();
    }
}
