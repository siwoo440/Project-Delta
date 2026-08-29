namespace ProjectDelta.Domain
{
    // 110일차: 방 종류. 114~121일차(특수 방 단계)에 걸쳐 값이 계속 늘어날 예정이다.
    // Combat/Event는 자리만 만들어뒀다 - 기존 몬스터 조우(40일차)·이벤트(107~109일차)
    // 시스템이 아직 RoomType을 참조하지 않으므로, 실제로 굴려서 배정하는 건 Trap뿐이다.
    public enum RoomType
    {
        Normal = 0,
        Combat = 1,
        Event = 2,
        Trap = 3
    }

    public static class RoomTypeRules
    {
        public static string GetDisplayName(
            RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Combat:
                    return "전투";

                case RoomType.Event:
                    return "이벤트";

                case RoomType.Trap:
                    return "함정";

                default:
                    return "일반";
            }
        }
    }
}
