namespace ProjectDelta.Domain
{
    // 114일차: 보물사냥꾼 NPC의 유물 서비스 - 저주 유물 제거(골드 지불)와
    // 유물 희생(골드 획득) 두 가지를 다룬다. RelicRunState.RemoveRelic이
    // internal이라 같은 Domain 어셈블리인 이 서비스를 통해서만 호출할 수 있다.
    public static class NpcRelicService
    {
        // 저주 유물만 제거할 수 있다 - 저주 없는 유물은 대상이 아니다.
        public static NpcServiceActionResult RemoveCursedRelic(
            RelicRunState relics,
            PlayerRunState player,
            string relicId,
            int goldCost)
        {
            if (relics == null
                || player == null
                || string.IsNullOrEmpty(
                    relicId))
            {
                return NpcServiceActionResult.Failed(
                    NpcServiceFailureReason.InvalidState);
            }

            if (!relics.HasRelic(
                    relicId))
            {
                return NpcServiceActionResult.Failed(
                    NpcServiceFailureReason.RelicNotFound);
            }

            if (!IsCursed(
                    relics,
                    relicId))
            {
                return NpcServiceActionResult.Failed(
                    NpcServiceFailureReason.RelicNotCursed);
            }

            if (!GoldService.TrySpend(
                    player,
                    goldCost))
            {
                return NpcServiceActionResult.Failed(
                    NpcServiceFailureReason.NotEnoughGold);
            }

            relics.RemoveRelic(
                relicId);

            return NpcServiceActionResult.Succeeded(
                -goldCost);
        }

        // 유물 종류와 무관하게 내주고 골드를 받는다("희생").
        public static NpcServiceActionResult SacrificeRelic(
            RelicRunState relics,
            PlayerRunState player,
            string relicId,
            int goldReward)
        {
            if (relics == null
                || player == null
                || string.IsNullOrEmpty(
                    relicId))
            {
                return NpcServiceActionResult.Failed(
                    NpcServiceFailureReason.InvalidState);
            }

            if (!relics.HasRelic(
                    relicId))
            {
                return NpcServiceActionResult.Failed(
                    NpcServiceFailureReason.RelicNotFound);
            }

            relics.RemoveRelic(
                relicId);

            int earned =
                GoldService.Earn(
                    player,
                    goldReward);

            return NpcServiceActionResult.Succeeded(
                earned);
        }

        private static bool IsCursed(
            RelicRunState relics,
            string relicId)
        {
            foreach (RelicInstanceState relic in relics.Relics)
            {
                if (relic != null
                    && relic.RelicId == relicId)
                {
                    return relic.IsCursed;
                }
            }

            return false;
        }
    }
}
