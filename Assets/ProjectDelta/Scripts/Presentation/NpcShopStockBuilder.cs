using System;
using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 130일차: 기획서 6.5절 "상점 상품 및 가격 규칙" - 카테고리별 가격 범위 안에서 실제
    // 가격을 굴리고, 6.6절 "상점 강화" 수치(재고 증가·할인·희귀 상품 확률)를 반영한다.
    // 114일차의 "실제 자산이 하나뿐이라 전부 채운다"는 임시 구현을 대체한다.
    public static class NpcShopStockBuilder
    {
        // "희귀 상품"(고급 카테고리)으로 우선 배정할 후보 - 유물·저주 유물처럼 값비싼 분류.
        private static readonly ItemCategory[] RareCategories =
        {
            ItemCategory.Relic,
            ItemCategory.Cursed
        };

        // 129일차 이전과 같은 방식으로 씨앗 없이 호출해도 동작하도록 기본 오버로드를 남긴다.
        public static List<ShopProductState> BuildDefaultStock()
        {
            return BuildStock(
                Environment.TickCount,
                string.Empty);
        }

        public static List<ShopProductState> BuildStock(
            int seed,
            string npcId)
        {
            List<ShopProductState> products =
                new List<ShopProductState>();

            List<ItemDefinition> normalPool;
            List<ItemDefinition> rarePool;

            CollectPools(
                out normalPool,
                out rarePool);

            if (normalPool.Count == 0
                && rarePool.Count == 0)
            {
                return products;
            }

            ShopUpgradeSnapshot upgrade =
                ApplicationFlow.Current != null
                    ? ApplicationFlow.Current.GetShopUpgradeSnapshot()
                    : new ShopUpgradeSnapshot(0, 0, 0);

            int stockCount =
                ShopUpgradeRule.BaseStockCount
                + upgrade.BonusStockCount;

            int npcSeed =
                unchecked(seed + (npcId ?? string.Empty).GetHashCode());

            System.Random rng =
                new System.Random(npcSeed);

            double discountRatio =
                (100 - upgrade.DiscountPercent)
                / 100.0;

            for (int slotIndex = 0; slotIndex < stockCount; slotIndex++)
            {
                bool pickFromRarePool =
                    rarePool.Count > 0
                    && rng.Next(100) < upgrade.RareChancePercent;

                List<ItemDefinition> sourcePool =
                    pickFromRarePool
                        ? rarePool
                        : normalPool;

                if (sourcePool.Count == 0)
                {
                    sourcePool =
                        pickFromRarePool
                            ? normalPool
                            : rarePool;
                }

                if (sourcePool.Count == 0)
                {
                    continue;
                }

                ItemDefinition chosen =
                    sourcePool[rng.Next(sourcePool.Count)];

                ShopProductState product =
                    CreatePricedProduct(
                        chosen,
                        discountRatio,
                        rng);

                if (product != null)
                {
                    products.Add(
                        product);
                }
            }

            return products;
        }

        // 로드된 ItemDefinition 전체를 상점 판매 가능 여부(ItemPriceRangeRules)로 나눈다.
        private static void CollectPools(
            out List<ItemDefinition> normalPool,
            out List<ItemDefinition> rarePool)
        {
            normalPool = new List<ItemDefinition>();
            rarePool = new List<ItemDefinition>();

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

                bool isBag =
                    definition.BagTier != BagTier.None;

                if (!ItemPriceRangeRules.TryGetPriceRange(
                        definition.Category,
                        isBag,
                        out _,
                        out _))
                {
                    continue;
                }

                if (IsRareCategory(
                        definition.Category))
                {
                    rarePool.Add(
                        definition);
                }
                else
                {
                    normalPool.Add(
                        definition);
                }
            }
        }

        private static bool IsRareCategory(
            ItemCategory category)
        {
            for (int i = 0; i < RareCategories.Length; i++)
            {
                if (RareCategories[i] == category)
                {
                    return true;
                }
            }

            return false;
        }

        private static ShopProductState CreatePricedProduct(
            ItemDefinition definition,
            double discountRatio,
            System.Random rng)
        {
            bool isBag =
                definition.BagTier != BagTier.None;

            if (!ItemPriceRangeRules.TryGetPriceRange(
                    definition.Category,
                    isBag,
                    out int minPrice,
                    out int maxPrice))
            {
                return null;
            }

            int rolledPrice =
                minPrice
                + rng.Next(
                    maxPrice - minPrice + 1);

            int finalPrice =
                Mathf.Max(
                    0,
                    Mathf.RoundToInt(
                        (float)(rolledPrice * discountRatio)));

            return ShopInteractionService.CreateProduct(
                definition,
                finalPrice);
        }
    }
}
