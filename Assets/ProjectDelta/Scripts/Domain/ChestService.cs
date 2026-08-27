namespace ProjectDelta.Domain
{
    public enum ChestActionFailureReason
    {
        None = 0,
        InvalidState = 1,

        // 이미 잠기지 않은 상자를 다시 열려고 했다.
        NotLocked = 2,

        // 열쇠가 없어 열지 못했다.
        NoKeys = 3,

        // 강제 개방은 상자당 1회만 허용된다.
        AlreadyForceOpened = 4,

        // 미믹 여부는 상자당 한 번만 판정한다(재판정 방지).
        AlreadyMimicResolved = 5,

        // 보상은 상자당 한 번만 지급한다(중복 획득 방지).
        AlreadyRewarded = 6
    }

    public sealed class ChestActionResult
    {
        public bool Success { get; private set; }

        public ChestActionFailureReason FailureReason { get; private set; }

        public static ChestActionResult Succeeded()
        {
            return new ChestActionResult
            {
                Success = true,
                FailureReason = ChestActionFailureReason.None
            };
        }

        public static ChestActionResult Failed(
            ChestActionFailureReason reason)
        {
            return new ChestActionResult
            {
                Success = false,
                FailureReason = reason
            };
        }
    }

    // 106일차: 잠긴 상자 개방, 강제 개방(1회 제한), 미믹 판정 확정, 보상 지급 확정
    // 규칙을 한 곳에서 처리한다. 미믹 여부 자체를 굴리는 무작위 판정은 Application
    // 계층(ChestMimicRollService)이 담당하고, 여기서는 그 결과를 확정·기록만 한다 -
    // 100일차 EquipmentService/EquipmentRollService와 같은 분리 원칙이다.
    public static class ChestService
    {
        public static ChestActionResult UnlockWithKey(
            ChestRunState chest,
            PlayerRunState player)
        {
            if (chest == null
                || player == null)
            {
                return ChestActionResult.Failed(
                    ChestActionFailureReason.InvalidState);
            }

            if (!chest.IsLocked)
            {
                return ChestActionResult.Failed(
                    ChestActionFailureReason.NotLocked);
            }

            if (player.KeyCount <= 0)
            {
                return ChestActionResult.Failed(
                    ChestActionFailureReason.NoKeys);
            }

            player.KeyCount -=
                1;

            chest.Unlock();

            return ChestActionResult.Succeeded();
        }

        // 강제 개방은 항상 성공하지만, 상자당 딱 한 번만 시도할 수 있다.
        public static ChestActionResult ForceOpen(
            ChestRunState chest)
        {
            if (chest == null)
            {
                return ChestActionResult.Failed(
                    ChestActionFailureReason.InvalidState);
            }

            // 강제 개방 이력은 잠금 상태보다 먼저 확인한다 - 이미 시도했던 상자라면
            // (그 사이 열쇠로 열렸더라도) 다시 강제 개방을 "시도할 수 있는 것처럼"
            // 보이지 않아야 한다.
            if (chest.ForceOpenAttempted)
            {
                return ChestActionResult.Failed(
                    ChestActionFailureReason.AlreadyForceOpened);
            }

            if (!chest.IsLocked)
            {
                return ChestActionResult.Failed(
                    ChestActionFailureReason.NotLocked);
            }

            chest.MarkForceOpenAttempted();

            chest.Unlock();

            return ChestActionResult.Succeeded();
        }

        public static ChestActionResult ResolveMimic(
            ChestRunState chest,
            bool isMimic)
        {
            if (chest == null)
            {
                return ChestActionResult.Failed(
                    ChestActionFailureReason.InvalidState);
            }

            if (chest.MimicResolved)
            {
                return ChestActionResult.Failed(
                    ChestActionFailureReason.AlreadyMimicResolved);
            }

            chest.ResolveMimic(
                isMimic);

            return ChestActionResult.Succeeded();
        }

        public static ChestActionResult GrantReward(
            ChestRunState chest)
        {
            if (chest == null)
            {
                return ChestActionResult.Failed(
                    ChestActionFailureReason.InvalidState);
            }

            if (!chest.MarkRewardGranted())
            {
                return ChestActionResult.Failed(
                    ChestActionFailureReason.AlreadyRewarded);
            }

            return ChestActionResult.Succeeded();
        }
    }
}
