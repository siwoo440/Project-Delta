using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    // 105일차: 상점 구매/판매 규칙(골드 검사, 인벤토리 공간 검증, 판매가 50%)을 검증한다.
    public sealed class ShopServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            InventoryRunState.MaxStackResolver =
                null;
        }

        [TearDown]
        public void TearDown()
        {
            InventoryRunState.MaxStackResolver =
                null;
        }

        [Test]
        public void Buy_SufficientGoldAndSpace_DeductsGoldAndAddsItem()
        {
            ShopRunState shop =
                new ShopRunState();

            shop.SetProducts(
                new[]
                {
                    new ShopProductState(
                        "POTION",
                        "포션",
                        20,
                        5)
                });

            InventoryRunState inventory =
                new InventoryRunState();

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold =
                50;

            ShopActionResult result =
                ShopService.Buy(
                    shop,
                    inventory,
                    player,
                    0);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                result.GoldChange,
                Is.EqualTo(-20));

            Assert.That(
                player.Gold,
                Is.EqualTo(30));

            Assert.That(
                inventory.Slots[0].ItemId,
                Is.EqualTo("POTION"));
        }

        [Test]
        public void Buy_NotEnoughGold_FailsWithoutMutation()
        {
            ShopRunState shop =
                new ShopRunState();

            shop.SetProducts(
                new[]
                {
                    new ShopProductState(
                        "POTION",
                        "포션",
                        20,
                        5)
                });

            InventoryRunState inventory =
                new InventoryRunState();

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold =
                5;

            ShopActionResult result =
                ShopService.Buy(
                    shop,
                    inventory,
                    player,
                    0);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ShopActionFailureReason.NotEnoughGold));

            Assert.That(
                player.Gold,
                Is.EqualTo(5));

            Assert.That(
                inventory.Slots[0].IsEmpty,
                Is.True);
        }

        [Test]
        public void Buy_InventoryFull_RefundsGoldAndFails()
        {
            ShopRunState shop =
                new ShopRunState();

            shop.SetProducts(
                new[]
                {
                    new ShopProductState(
                        "NEW_ITEM",
                        "새 아이템",
                        15,
                        1)
                });

            InventoryRunState inventory =
                new InventoryRunState();

            // 인벤토리를 서로 다른 아이템으로 가득 채운다.
            for (int index = 0;
                 index < inventory.Capacity;
                 index++)
            {
                inventory.TryAdd(
                    $"FILLER_{index}",
                    $"채움 {index}",
                    1,
                    1,
                    out _);
            }

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold =
                100;

            ShopActionResult result =
                ShopService.Buy(
                    shop,
                    inventory,
                    player,
                    0);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ShopActionFailureReason.InventoryFull));

            // 지출했던 골드가 그대로 환불되어야 한다.
            Assert.That(
                player.Gold,
                Is.EqualTo(100));
        }

        [Test]
        public void Buy_InvalidProductIndex_Fails()
        {
            ShopRunState shop =
                new ShopRunState();

            InventoryRunState inventory =
                new InventoryRunState();

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            ShopActionResult result =
                ShopService.Buy(
                    shop,
                    inventory,
                    player,
                    0);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ShopActionFailureReason.InvalidProduct));
        }

        [Test]
        public void Sell_SellableItem_PaysHalfOfBasePriceAndRemovesOne()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "OLD_SWORD",
                "낡은 검",
                1,
                1,
                out int slotIndex);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.Gold =
                0;

            ShopActionResult result =
                ShopService.Sell(
                    inventory,
                    player,
                    slotIndex,
                    ItemCategory.Equipment,
                    100);

            Assert.That(
                result.Success,
                Is.True);

            Assert.That(
                result.GoldChange,
                Is.EqualTo(50));

            Assert.That(
                player.Gold,
                Is.EqualTo(50));

            Assert.That(
                inventory.Slots[slotIndex].IsEmpty,
                Is.True);
        }

        [Test]
        public void Sell_OddBasePrice_RoundsDown()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "TRINKET",
                "장신구",
                1,
                1,
                out int slotIndex);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            ShopActionResult result =
                ShopService.Sell(
                    inventory,
                    player,
                    slotIndex,
                    ItemCategory.Treasure,
                    31);

            Assert.That(
                result.GoldChange,
                Is.EqualTo(15));
        }

        [Test]
        public void Sell_NotSellableCategory_FailsWithoutMutation()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "KEY",
                "열쇠",
                1,
                1,
                out int slotIndex);

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            ShopActionResult result =
                ShopService.Sell(
                    inventory,
                    player,
                    slotIndex,
                    ItemCategory.KeyItem,
                    50);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ShopActionFailureReason.ItemNotSellable));

            Assert.That(
                inventory.Slots[slotIndex].ItemId,
                Is.EqualTo("KEY"));
        }

        [Test]
        public void Sell_EmptySlot_FailsWithInvalidSlot()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            PlayerRunState player =
                PlayerRunState.CreateDefault();

            ShopActionResult result =
                ShopService.Sell(
                    inventory,
                    player,
                    0,
                    ItemCategory.Equipment,
                    100);

            Assert.That(
                result.Success,
                Is.False);

            Assert.That(
                result.FailureReason,
                Is.EqualTo(
                    ShopActionFailureReason.InvalidSlot));
        }
    }
}
