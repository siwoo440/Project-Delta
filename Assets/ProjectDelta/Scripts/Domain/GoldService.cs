using System;

namespace ProjectDelta.Domain
{
    // 105일차: 골드 획득·소비를 한 곳에서 처리해 전투 보상·이벤트·상점이 항상
    // 같은 API로 같은 화폐 값(PlayerRunState.Gold)을 다루게 한다.
    public static class GoldService
    {
        // int.MaxValue를 넘지 않도록 포화시키며 실제로 늘어난 양을 반환한다.
        // (기존 BattleRewardPayoutService.ApplyDropGold의 계산 방식을 그대로 옮겼다.)
        public static int Earn(
            PlayerRunState player,
            int amount)
        {
            if (player == null
                || amount <= 0)
            {
                return 0;
            }

            int before =
                Math.Max(
                    0,
                    player.Gold);

            long after =
                (long)before
                + amount;

            player.Gold =
                after >= int.MaxValue
                    ? int.MaxValue
                    : (int)after;

            return player.Gold
                - before;
        }

        // 보유 골드가 부족하면 상태를 바꾸지 않고 실패한다.
        public static bool TrySpend(
            PlayerRunState player,
            int amount)
        {
            if (player == null
                || amount < 0)
            {
                return false;
            }

            if (amount == 0)
            {
                return true;
            }

            if (player.Gold < amount)
            {
                return false;
            }

            player.Gold -=
                amount;

            return true;
        }
    }
}
