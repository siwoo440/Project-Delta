namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 111일차: Event 방이 이벤트 화면을 띄워도 되는지 판정하고, 허용되면 한 번만
    // 발생하도록 표시까지 같이 처리한다. RoomInstance.MarkEventTriggered()는
    // internal이라 같은 Domain 어셈블리인 이 서비스를 통해서만 호출할 수 있다.
    public static class RoomEventService // 이벤트 방 발동 판정 서비스
    {
        public static bool TryMarkTriggered( // 이벤트를 발동 가능하면 발동 처리까지 시도
            RoomInstance room) // 판정 대상 방
        {
            if (room == null // 방이 없거나
                || room.RoomType != RoomType.Event) // 이벤트 방이 아니면
            {
                return false; // 발동 불가
            }

            return room.MarkEventTriggered(); // 이미 발동됐으면 false, 아니면 발동 표시 후 true
        }
    }
}
