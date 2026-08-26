using NUnit.Framework;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class InventoryAcquisitionServiceTests
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
            RunContext.End();

            InventoryRunState.MaxStackResolver =
                null;
        }

        [Test]
        public void Preview_DoesNotMutateInventory()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "POTION",
                "포션",
                4,
                5,
                out _);

            InventoryAcquisitionPlan plan =
                InventoryAcquisitionService.Preview(
                    inventory,
                    "POTION",
                    "포션",
                    3,
                    5);

            Assert.That(
                plan.AddableQuantity,
                Is.EqualTo(3));

            Assert.That(
                plan.RemainingQuantity,
                Is.EqualTo(0));

            Assert.That(
                inventory.Slots[0].Quantity,
                Is.EqualTo(4));

            Assert.That(
                inventory.Slots[1].IsEmpty,
                Is.True);
        }

        [Test]
        public void Preview_FullInventory_ReturnsOverflowWithoutMutation()
        {
            InventoryRunState inventory =
                CreateFullInventory();

            InventoryAcquisitionPlan plan =
                InventoryAcquisitionService.Preview(
                    inventory,
                    "NEW_ITEM",
                    "새 아이템",
                    1,
                    1);

            Assert.That(
                plan.AddableQuantity,
                Is.EqualTo(0));

            Assert.That(
                plan.RemainingQuantity,
                Is.EqualTo(1));

            Assert.That(
                plan.RequiresDecision,
                Is.True);

            Assert.That(
                inventory.Slots[0].ItemId,
                Is.EqualTo("ITEM_0"));
        }

        [Test]
        public void Cancel_DoesNotChangeInventory()
        {
            InventoryRunState inventory =
                CreateFullInventory();

            InventoryAcquisitionPlan plan =
                InventoryAcquisitionService.Preview(
                    inventory,
                    "NEW_ITEM",
                    "새 아이템",
                    1,
                    1);

            InventoryAcquisitionCommitResult result =
                InventoryAcquisitionService.CommitCancel(
                    plan);

            Assert.That(
                result.WasCancelled,
                Is.True);

            Assert.That(
                result.AddedQuantity,
                Is.EqualTo(0));

            Assert.That(
                inventory.Slots[0].ItemId,
                Is.EqualTo("ITEM_0"));
        }

        [Test]
        public void Leave_AddsOnlyWhatFits()
        {
            InventoryRunState inventory =
                CreateFullInventory();

            inventory.TryRemoveAt(
                0);

            inventory.TryAdd(
                "POTION",
                "포션",
                4,
                5,
                out _);

            InventoryAcquisitionPlan plan =
                InventoryAcquisitionService.Preview(
                    inventory,
                    "POTION",
                    "포션",
                    3,
                    5);

            InventoryAcquisitionCommitResult result =
                InventoryAcquisitionService.CommitLeave(
                    inventory,
                    plan);

            Assert.That(
                result.AddedQuantity,
                Is.EqualTo(1));

            Assert.That(
                result.RemainingQuantity,
                Is.EqualTo(2));

            Assert.That(
                inventory.Slots[0].Quantity,
                Is.EqualTo(5));
        }

        [Test]
        public void Replace_AllowedCategory_ReplacesChosenSlot()
        {
            InventoryRunState inventory =
                CreateFullInventory();

            InventoryAcquisitionPlan plan =
                InventoryAcquisitionService.Preview(
                    inventory,
                    "NEW_ITEM",
                    "새 아이템",
                    1,
                    1);

            InventoryAcquisitionCommitResult result =
                InventoryAcquisitionService.CommitReplace(
                    inventory,
                    plan,
                    3,
                    ItemCategory.Consumable);

            Assert.That(
                result.ReplacementSucceeded,
                Is.True);

            Assert.That(
                result.ReplacedSlotIndex,
                Is.EqualTo(3));

            Assert.That(
                inventory.Slots[3].ItemId,
                Is.EqualTo("NEW_ITEM"));

            Assert.That(
                result.RemainingQuantity,
                Is.EqualTo(0));
        }

        [Test]
        public void Replace_KeyItem_IsBlockedWithoutMutation()
        {
            InventoryRunState inventory =
                CreateFullInventory();

            string originalItemId =
                inventory.Slots[3].ItemId;

            InventoryAcquisitionPlan plan =
                InventoryAcquisitionService.Preview(
                    inventory,
                    "NEW_ITEM",
                    "새 아이템",
                    1,
                    1);

            InventoryAcquisitionCommitResult result =
                InventoryAcquisitionService.CommitReplace(
                    inventory,
                    plan,
                    3,
                    ItemCategory.KeyItem);

            Assert.That(
                result.ReplacementSucceeded,
                Is.False);

            Assert.That(
                inventory.Slots[3].ItemId,
                Is.EqualTo(originalItemId));

            Assert.That(
                result.AddedQuantity,
                Is.EqualTo(0));
        }

        [TestCase(ItemCategory.Relic)]
        [TestCase(ItemCategory.Cursed)]
        [TestCase(ItemCategory.Uncategorized)]
        public void Replace_ConditionalOrUnknownCategory_IsBlocked(
            ItemCategory category)
        {
            InventoryRunState inventory =
                CreateFullInventory();

            InventoryAcquisitionPlan plan =
                InventoryAcquisitionService.Preview(
                    inventory,
                    "NEW_ITEM",
                    "새 아이템",
                    1,
                    1);

            InventoryAcquisitionCommitResult result =
                InventoryAcquisitionService.CommitReplace(
                    inventory,
                    plan,
                    2,
                    category);

            Assert.That(
                result.ReplacementSucceeded,
                Is.False);

            Assert.That(
                inventory.Slots[2].ItemId,
                Is.EqualTo("ITEM_2"));
        }

        [Test]
        public void Replace_PreservesExistingStackGainBeforeUsingChosenSlot()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            inventory.TryAdd(
                "POTION",
                "포션",
                4,
                5,
                out _);

            for (int index = 1;
                 index < inventory.Capacity;
                 index++)
            {
                inventory.TryAdd(
                    $"ITEM_{index}",
                    $"아이템 {index}",
                    1,
                    1,
                    out _);
            }

            InventoryAcquisitionPlan plan =
                InventoryAcquisitionService.Preview(
                    inventory,
                    "POTION",
                    "포션",
                    3,
                    5);

            InventoryAcquisitionCommitResult result =
                InventoryAcquisitionService.CommitReplace(
                    inventory,
                    plan,
                    5,
                    ItemCategory.Treasure);

            Assert.That(
                inventory.Slots[0].Quantity,
                Is.EqualTo(5));

            Assert.That(
                inventory.Slots[5].ItemId,
                Is.EqualTo("POTION"));

            Assert.That(
                inventory.Slots[5].Quantity,
                Is.EqualTo(2));

            Assert.That(
                result.AddedQuantity,
                Is.EqualTo(3));
        }

        private static InventoryRunState CreateFullInventory()
        {
            InventoryRunState inventory =
                new InventoryRunState();

            for (int index = 0;
                 index < inventory.Capacity;
                 index++)
            {
                inventory.TryAdd(
                    $"ITEM_{index}",
                    $"아이템 {index}",
                    1,
                    1,
                    out _);
            }

            return inventory;
        }
    }
}
