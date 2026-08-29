using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 110일차: 함정 방 판정(회피/피해 적용, 함정이 아닌 방 거부, 재판정 방지)을 검증한다.
    public sealed class RoomTrapServiceTests
    {
        private static RoomInstance CreateTrapRoom()
        {
            RoomInstance room =
                RoomInstance.Create(
                    "ROOM_TRAP",
                    "DEF_TRAP",
                    null);

            room.SetRoomType(
                RoomType.Trap);

            return room;
        }

        [Test]
        public void Trigger_NotAvoided_DealsDamageAndMarksTriggered()
        {
            RoomInstance room =
                CreateTrapRoom();

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.CurrentHp =
                50;

            RoomTrapResult result =
                RoomTrapService.Trigger(
                    room,
                    player,
                    false,
                    10);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                result.Avoided,
                Is.False);

            Assert.That(
                result.DamageDealt,
                Is.EqualTo(10));

            Assert.That(
                player.CurrentHp,
                Is.EqualTo(40));

            Assert.That(
                room.TrapTriggered,
                Is.True);
        }

        [Test]
        public void Trigger_Avoided_DealsNoDamage()
        {
            RoomInstance room =
                CreateTrapRoom();

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.CurrentHp =
                50;

            RoomTrapResult result =
                RoomTrapService.Trigger(
                    room,
                    player,
                    true,
                    10);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                result.Avoided,
                Is.True);

            Assert.That(
                result.DamageDealt,
                Is.EqualTo(0));

            Assert.That(
                player.CurrentHp,
                Is.EqualTo(50));
        }

        [Test]
        public void Trigger_DamageExceedsCurrentHp_ClampsToZero()
        {
            RoomInstance room =
                CreateTrapRoom();

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.CurrentHp =
                5;

            RoomTrapResult result =
                RoomTrapService.Trigger(
                    room,
                    player,
                    false,
                    999);

            Assert.That(
                player.CurrentHp,
                Is.EqualTo(0));

            Assert.That(
                result.DamageDealt,
                Is.EqualTo(5));
        }

        [Test]
        public void Trigger_SecondAttempt_FailsWithAlreadyTriggered()
        {
            RoomInstance room =
                CreateTrapRoom();

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            RoomTrapService.Trigger(
                room,
                player,
                false,
                10);

            RoomTrapResult secondAttempt =
                RoomTrapService.Trigger(
                    room,
                    player,
                    false,
                    10);

            Assert.That(
                secondAttempt.Success,
                Is.False);

            Assert.That(
                secondAttempt.FailureReason,
                Is.EqualTo(
                    RoomTrapFailureReason.AlreadyTriggered));

            // 두 번째 시도로 추가 피해를 입지 않아야 한다.
            Assert.That(
                player.CurrentHp,
                Is.EqualTo(
                    player.GetFinalStats().MaxHealth - 10));
        }

        [Test]
        public void Trigger_NonTrapRoom_FailsWithNotATrapRoom()
        {
            RoomInstance normalRoom =
                RoomInstance.Create(
                    "ROOM_NORMAL",
                    "DEF_NORMAL",
                    null);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            RoomTrapResult result =
                RoomTrapService.Trigger(
                    normalRoom,
                    player,
                    false,
                    10);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    RoomTrapFailureReason.NotATrapRoom));
        }

        [Test]
        public void Trigger_NullRoomOrPlayer_FailsWithInvalidState()
        {
            RoomInstance room =
                CreateTrapRoom();

            Assert.That(
                RoomTrapService.Trigger(
                    null,
                    PlayerRunState.CreateDefault(),
                    false,
                    10).FailureReason,
                Is.EqualTo(
                    RoomTrapFailureReason.InvalidState));

            Assert.That(
                RoomTrapService.Trigger(
                    room,
                    null,
                    false,
                    10).FailureReason,
                Is.EqualTo(
                    RoomTrapFailureReason.InvalidState));
        }

        [Test]
        public void SetRoomType_DefaultsToNormal()
        {
            RoomInstance room =
                RoomInstance.Create(
                    "ROOM_DEFAULT",
                    "DEF_DEFAULT",
                    null);

            Assert.That(
                room.RoomType,
                Is.EqualTo(
                    RoomType.Normal));
        }
    }
}
