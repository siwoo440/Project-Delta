using System;
using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    public sealed class ShopProductState
    {
        public string ItemId { get; }

        public string DisplayName { get; }

        public int Price { get; }

        public int MaxStackSize { get; }

        public ShopProductState(
            string itemId,
            string displayName,
            int price,
            int maxStackSize)
        {
            ItemId =
                itemId
                ?? string.Empty;

            DisplayName =
                string.IsNullOrEmpty(
                    displayName)
                    ? ItemId
                    : displayName;

            Price =
                Math.Max(
                    0,
                    price);

            MaxStackSize =
                Math.Max(
                    1,
                    maxStackSize);
        }
    }

    // 105일차: 상품 목록은 층 진입 시 한 번만 정해져 고정된다 - 같은 층에서
    // 상점을 여러 번 드나들어도 SetProducts를 다시 호출하기 전까지는
    // 가격·구성이 바뀌지 않는다.
    public sealed class ShopRunState
    {
        private readonly List<ShopProductState> products =
            new List<ShopProductState>();

        public IReadOnlyList<ShopProductState> Products =>
            products;

        public void SetProducts(
            IEnumerable<ShopProductState> newProducts)
        {
            products.Clear();

            if (newProducts == null)
            {
                return;
            }

            foreach (ShopProductState product in newProducts)
            {
                if (product != null)
                {
                    products.Add(
                        product);
                }
            }
        }

        public bool TryGetProduct(
            int index,
            out ShopProductState product)
        {
            if (index < 0
                || index >= products.Count)
            {
                product = null;
                return false;
            }

            product =
                products[index];

            return true;
        }
    }
}
