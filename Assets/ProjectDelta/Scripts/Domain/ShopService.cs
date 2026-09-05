namespace ProjectDelta.Domain
{
    public enum ShopActionFailureReason
    {
        None = 0,
        InvalidState = 1,
        InvalidProduct = 2,
        NotEnoughGold = 3,
        InventoryFull = 4,
        ItemNotSellable = 5,
        InvalidSlot = 6
    }

    public sealed class ShopActionResult
    {
        public bool Success { get; private set; }

        public ShopActionFailureReason FailureReason { get; private set; }

        // 구매는 음수(지출), 판매는 양수(수입)로 반환한다.
        public int GoldChange { get; private set; }

        public static ShopActionResult Succeeded(
            int goldChange)
        {
            return new ShopActionResult
            {
                Success = true,
                FailureReason = ShopActionFailureReason.None,
                GoldChange = goldChange
            };
        }

        public static ShopActionResult Failed(
            ShopActionFailureReason reason)
        {
            return new ShopActionResult
            {
                Success = false,
                FailureReason = reason
            };
        }
    }

    // 105일차: 상점 구매/판매 규칙(보유 골드 검사, 인벤토리 공간 검증)을 한 곳에서 처리한다.
    public static class ShopService
    {
        // 130일차: 판매가 비율은 상점 강화(ShopUpgradeRule, 기본 50%~70%)에 따라 달라져서
        // Domain(ProfileData를 모름)이 값을 정할 수 없다 - Application(ApplicationFlow.
        // GetShopSellPriceRatio)이 계산해 호출자로 넘겨준다. 기본값은 강화 전 50%다.
        public const double DefaultSellPriceRatio = 0.5;

        public static ShopActionResult Buy(
            ShopRunState shop,
            InventoryRunState inventory,
            PlayerRunState player,
            int productIndex)
        {
            if (shop == null
                || inventory == null
                || player == null)
            {
                return ShopActionResult.Failed(
                    ShopActionFailureReason.InvalidState);
            }

            if (!shop.TryGetProduct(
                    productIndex,
                    out ShopProductState product))
            {
                return ShopActionResult.Failed(
                    ShopActionFailureReason.InvalidProduct);
            }

            if (!GoldService.TrySpend(
                    player,
                    product.Price))
            {
                return ShopActionResult.Failed(
                    ShopActionFailureReason.NotEnoughGold);
            }

            bool added =
                inventory.TryAdd(
                    product.ItemId,
                    product.DisplayName,
                    1,
                    product.MaxStackSize,
                    out _);

            if (!added)
            {
                // 인벤토리에 넣지 못했으면 지출한 골드를 그대로 되돌린다.
                GoldService.Earn(
                    player,
                    product.Price);

                return ShopActionResult.Failed(
                    ShopActionFailureReason.InventoryFull);
            }

            return ShopActionResult.Succeeded(
                -product.Price);
        }

        public static ShopActionResult Sell(
            InventoryRunState inventory,
            PlayerRunState player,
            int slotIndex,
            ItemCategory category,
            int basePrice,
            double sellPriceRatio = DefaultSellPriceRatio)
        {
            if (inventory == null
                || player == null)
            {
                return ShopActionResult.Failed(
                    ShopActionFailureReason.InvalidState);
            }

            if (!ItemCategoryRules.CanSell(
                    category))
            {
                return ShopActionResult.Failed(
                    ShopActionFailureReason.ItemNotSellable);
            }

            if (!inventory.TryGetSlot(
                    slotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                return ShopActionResult.Failed(
                    ShopActionFailureReason.InvalidSlot);
            }

            if (!inventory.TryRemoveQuantityAt(
                    slotIndex,
                    1,
                    out int removedQuantity)
                || removedQuantity != 1)
            {
                return ShopActionResult.Failed(
                    ShopActionFailureReason.InvalidSlot);
            }

            int sellPrice =
                (int)(basePrice
                * sellPriceRatio);

            int earned =
                GoldService.Earn(
                    player,
                    sellPrice);

            return ShopActionResult.Succeeded(
                earned);
        }
    }
}
