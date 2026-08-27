namespace ProjectDelta.Domain
{
    public enum RelicAcquisitionFailureReason
    {
        None = 0,
        InvalidState = 1,

        // 동일 ID 유물을 이미 보유하고 있다.
        AlreadyOwned = 2,

        // 보유 한도(기본 5개)에 도달했다.
        CapacityFull = 3
    }

    public sealed class RelicAcquisitionResult
    {
        public bool Success { get; private set; }

        public RelicAcquisitionFailureReason FailureReason { get; private set; }

        public RelicInstanceState Relic { get; private set; }

        public static RelicAcquisitionResult Succeeded(
            RelicInstanceState relic)
        {
            return new RelicAcquisitionResult
            {
                Success = true,
                FailureReason = RelicAcquisitionFailureReason.None,
                Relic = relic
            };
        }

        public static RelicAcquisitionResult Failed(
            RelicAcquisitionFailureReason reason)
        {
            return new RelicAcquisitionResult
            {
                Success = false,
                FailureReason = reason
            };
        }
    }

    // 104일차: 유물 획득 규칙(중복 금지·최대 보유 수)을 한 곳에서 처리한다.
    // 장비와 달리 인벤토리를 거치지 않고 획득 즉시 RelicRunState에 등록된다.
    public static class RelicService
    {
        public static RelicAcquisitionResult Acquire(
            RelicRunState relics,
            string relicId,
            string displayName,
            bool isCursed)
        {
            if (relics == null
                || string.IsNullOrEmpty(
                    relicId))
            {
                return RelicAcquisitionResult.Failed(
                    RelicAcquisitionFailureReason.InvalidState);
            }

            if (relics.HasRelic(
                    relicId))
            {
                return RelicAcquisitionResult.Failed(
                    RelicAcquisitionFailureReason.AlreadyOwned);
            }

            if (relics.IsFull)
            {
                return RelicAcquisitionResult.Failed(
                    RelicAcquisitionFailureReason.CapacityFull);
            }

            RelicInstanceState instance =
                new RelicInstanceState(
                    relicId,
                    displayName,
                    isCursed);

            relics.AddRelic(
                instance);

            return RelicAcquisitionResult.Succeeded(
                instance);
        }
    }
}
