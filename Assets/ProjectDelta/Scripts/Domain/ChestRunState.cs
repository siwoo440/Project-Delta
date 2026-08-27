namespace ProjectDelta.Domain
{
    // 106일차: 상자 한 개(등급·잠금·강제개방·미믹·보상 지급 여부)의 런타임 상태.
    // 기존 25일차 ChestContentMarker/RoomInstance의 "남은 아이템 목록" 저장과는
    // 별개로, 이번 일차에서 새로 추가하는 등급·잠금·미믹 판정만 담당한다.
    // 실제 씬 상자(ChestContentMarker)와의 연결, 저장 데이터 편입은 이후 통합 작업이다.
    public sealed class ChestRunState
    {
        public ChestRarity Rarity { get; }

        public bool IsLocked { get; private set; }

        // 강제 개방은 성공 여부와 무관하게 상자당 1회만 시도할 수 있다.
        public bool ForceOpenAttempted { get; private set; }

        public bool MimicResolved { get; private set; }

        public bool IsMimic { get; private set; }

        public bool RewardGranted { get; private set; }

        public ChestRunState(
            ChestRarity rarity,
            bool isLocked)
        {
            Rarity =
                rarity;

            IsLocked =
                isLocked;
        }

        internal void Unlock()
        {
            IsLocked =
                false;
        }

        internal bool MarkForceOpenAttempted()
        {
            if (ForceOpenAttempted)
            {
                return false;
            }

            ForceOpenAttempted =
                true;

            return true;
        }

        internal void ResolveMimic(
            bool isMimic)
        {
            MimicResolved =
                true;

            IsMimic =
                isMimic;
        }

        internal bool MarkRewardGranted()
        {
            if (RewardGranted)
            {
                return false;
            }

            RewardGranted =
                true;

            return true;
        }
    }
}
