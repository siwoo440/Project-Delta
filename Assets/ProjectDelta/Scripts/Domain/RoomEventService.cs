namespace ProjectDelta.Domain
{
    // 111일차: Event 방이 이벤트 화면을 띄워도 되는지 판정하고, 허용되면 한 번만
    // 발생하도록 표시까지 같이 처리한다. RoomInstance.MarkEventTriggered()는
    // internal이라 같은 Domain 어셈블리인 이 서비스를 통해서만 호출할 수 있다.
    public static class RoomEventService
    {
        public static bool TryMarkTriggered(
            RoomInstance room)
        {
            if (room == null
                || room.RoomType != RoomType.Event)
            {
                return false;
            }

            return room.MarkEventTriggered();
        }
    }
}
