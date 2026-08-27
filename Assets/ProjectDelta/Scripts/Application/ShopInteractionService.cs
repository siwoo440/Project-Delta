using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 105일차: 상점 UI가 ItemDefinition을 직접 ShopService 인자로 풀어 쓰지 않도록
    // 한곳에 모은다. 실제 규칙 판단은 여전히 ShopService(Domain)가 담당한다.
    public static class ShopInteractionService
    {
        public static ShopActionResult Sell(
            InventoryRunState inventory,
            PlayerRunState player,
            int slotIndex,
            ItemDefinition definition)
        {
            if (definition == null)
            {
                return ShopActionResult.Failed(
                    ShopActionFailureReason.ItemNotSellable);
            }

            return ShopService.Sell(
                inventory,
                player,
                slotIndex,
                definition.Category,
                definition.BasePrice);
        }

        // 상점 재고를 채울 때 ItemDefinition의 정가를 그대로 쓰거나,
        // overridePrice(0 이상)로 이번 층 한정 가격을 지정할 수 있다.
        public static ShopProductState CreateProduct(
            ItemDefinition definition,
            int overridePrice = -1)
        {
            if (definition == null)
            {
                return null;
            }

            int price =
                overridePrice >= 0
                    ? overridePrice
                    : definition.BasePrice;

            return new ShopProductState(
                definition.Id,
                definition.DisplayName,
                price,
                definition.MaxStackSize);
        }
    }
}
