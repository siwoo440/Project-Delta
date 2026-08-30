using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 114일차: 상인 NPC의 재고를 구성한다. 현재 프로젝트에 실제 ItemDefinition 자산이
    // 하나(ITEM_DAY80_TEST_DROP)뿐이라 지금은 그 하나로만 재고가 채워진다 - 나중에
    // 실제 아이템 자산이 늘어나면 여기 로직을 바꾸지 않아도 자동으로 재고에 포함된다.
    public static class NpcShopStockBuilder
    {
        public static List<ShopProductState> BuildDefaultStock()
        {
            List<ShopProductState> products =
                new List<ShopProductState>();

            ItemDefinition[] definitions =
                Resources.FindObjectsOfTypeAll<ItemDefinition>();

            for (int i = 0; i < definitions.Length; i++)
            {
                ItemDefinition definition =
                    definitions[i];

                if (definition == null
                    || string.IsNullOrEmpty(
                        definition.Id))
                {
                    continue;
                }

                ShopProductState product =
                    ShopInteractionService.CreateProduct(
                        definition);

                if (product != null)
                {
                    products.Add(
                        product);
                }
            }

            return products;
        }
    }
}
