namespace ProjectDelta.Domain
{
    public enum RoomTrapFailureReason
    {
        None = 0,
        InvalidState = 1,

        // 함정 방이 아닌 방에 함정 판정을 시도했다.
        NotATrapRoom = 2,

        // 이미 처리된 함정이다(재판정 방지).
        AlreadyTriggered = 3
    }

    public sealed class RoomTrapResult
    {
        public bool Success { get; private set; }

        public RoomTrapFailureReason FailureReason { get; private set; }

        public bool Avoided { get; private set; }

        public int DamageDealt { get; private set; }

        public static RoomTrapResult Succeeded(
            bool avoided,
            int damageDealt)
        {
            return new RoomTrapResult
            {
                Success = true,
                FailureReason = RoomTrapFailureReason.None,
                Avoided = avoided,
                DamageDealt = damageDealt
            };
        }

        public static RoomTrapResult Failed(
            RoomTrapFailureReason reason)
        {
            return new RoomTrapResult
            {
                Success = false,
                FailureReason = reason
            };
        }
    }

    // 110일차: 함정 방 판정을 한 곳에서 처리한다. 회피 여부·피해량은 이미 굴려진 값을
    // 그대로 받는다 - 무작위 판정 자체는 Application 계층(RoomTrapRollService)이
    // 담당한다(100일차 EquipmentService/EquipmentRollService와 같은 분리 원칙).
    public static class RoomTrapService
    {
        public static RoomTrapResult Trigger(
            RoomInstance room,
            PlayerRunState player,
            bool avoided,
            int damage)
        {
            if (room == null
                || player == null)
            {
                return RoomTrapResult.Failed(
                    RoomTrapFailureReason.InvalidState);
            }

            if (room.RoomType != RoomType.Trap)
            {
                return RoomTrapResult.Failed(
                    RoomTrapFailureReason.NotATrapRoom);
            }

            if (!room.MarkTrapTriggered())
            {
                return RoomTrapResult.Failed(
                    RoomTrapFailureReason.AlreadyTriggered);
            }

            int damageDealt =
                0;

            if (!avoided
                && damage > 0)
            {
                int before =
                    player.CurrentHp;

                int after =
                    before - damage;

                player.CurrentHp =
                    after < 0
                        ? 0
                        : after;

                damageDealt =
                    before
                    - player.CurrentHp;
            }

            return RoomTrapResult.Succeeded(
                avoided,
                damageDealt);
        }
    }
}
