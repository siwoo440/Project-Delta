using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 111일차: Event 방 트리거 판정(허용/재판정 방지/비Event 방 거부)을 검증한다.
    public sealed class RoomEventServiceTests
    {
        private static RoomInstance CreateEventRoom()
        {
            RoomInstance room =
                RoomInstance.Create(
                    "ROOM_EVENT",
                    "DEF_EVENT",
                    null);

            room.SetRoomType(
                RoomType.Event);

            return room;
        }

        [Test]
        public void TryMarkTriggered_EventRoom_FirstCall_ReturnsTrue()
        {
            RoomInstance room =
                CreateEventRoom();

            Assert.That(
                RoomEventService.TryMarkTriggered(
                    room),
                Is.True);

            Assert.That(
                room.EventTriggered,
                Is.True);
        }

        [Test]
        public void TryMarkTriggered_SecondCall_ReturnsFalse()
        {
            RoomInstance room =
                CreateEventRoom();

            RoomEventService.TryMarkTriggered(
                room);

            Assert.That(
                RoomEventService.TryMarkTriggered(
                    room),
                Is.False);
        }

        [Test]
        public void TryMarkTriggered_NonEventRoom_ReturnsFalse()
        {
            RoomInstance normalRoom =
                RoomInstance.Create(
                    "ROOM_NORMAL",
                    "DEF_NORMAL",
                    null);

            Assert.That(
                RoomEventService.TryMarkTriggered(
                    normalRoom),
                Is.False);

            Assert.That(
                normalRoom.EventTriggered,
                Is.False);
        }

        [Test]
        public void TryMarkTriggered_NullRoom_ReturnsFalse()
        {
            Assert.That(
                RoomEventService.TryMarkTriggered(
                    null),
                Is.False);
        }
    }
}
