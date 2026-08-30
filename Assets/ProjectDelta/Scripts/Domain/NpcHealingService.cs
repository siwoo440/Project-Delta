namespace ProjectDelta.Domain
{
    public enum NpcServiceFailureReason
    {
        None = 0,
        InvalidState = 1,
        NotEnoughGold = 2,
        AlreadyFull = 3,
        RelicNotFound = 4,
        RelicNotCursed = 5
    }

    public sealed class NpcServiceActionResult
    {
        public bool Success { get; private set; }

        public NpcServiceFailureReason FailureReason { get; private set; }

        public int GoldChange { get; private set; }

        public static NpcServiceActionResult Succeeded(
            int goldChange)
        {
            return new NpcServiceActionResult
            {
                Success = true,
                FailureReason = NpcServiceFailureReason.None,
                GoldChange = goldChange
            };
        }

        public static NpcServiceActionResult Failed(
            NpcServiceFailureReason reason)
        {
            return new NpcServiceActionResult
            {
                Success = false,
                FailureReason = reason
            };
        }
    }

    // 114일차: 치료사 NPC의 회복 서비스 - 골드를 받고 체력·마나·정력을 최대치로 채운다.
    // ShopService와 같은 계층(Domain)에서 같은 규칙(GoldService)을 재사용한다.
    public static class NpcHealingService
    {
        public static NpcServiceActionResult Heal(
            PlayerRunState player,
            int goldCost)
        {
            if (player == null)
            {
                return NpcServiceActionResult.Failed(
                    NpcServiceFailureReason.InvalidState);
            }

            StatBlock finalStats =
                player.GetFinalStats();

            bool alreadyFull =
                player.CurrentHp >= finalStats.MaxHealth
                && player.CurrentMana >= finalStats.MaxMana
                && player.CurrentStamina >= finalStats.MaxStamina;

            if (alreadyFull)
            {
                return NpcServiceActionResult.Failed(
                    NpcServiceFailureReason.AlreadyFull);
            }

            if (!GoldService.TrySpend(
                    player,
                    goldCost))
            {
                return NpcServiceActionResult.Failed(
                    NpcServiceFailureReason.NotEnoughGold);
            }

            player.CurrentHp =
                finalStats.MaxHealth;

            player.CurrentMana =
                finalStats.MaxMana;

            player.CurrentStamina =
                finalStats.MaxStamina;

            return NpcServiceActionResult.Succeeded(
                -goldCost);
        }
    }
}
