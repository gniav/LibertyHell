using System;
using LibertyHell.Items;
using LibertyHell.Shop;
using LibertyHell.SlotMachine;
using NUnit.Framework;

public class ShopServiceTests
{
    private sealed class Economy : IShopEconomy
    {
        public int Money = 100, Blood = 100, Max = 200, Bet = 10;
        public bool Available = true, RejectCommit;
        public long Revision;
        public ShopEconomySnapshot Read() => new ShopEconomySnapshot(Revision, Money, Blood, Max, Bet, Available);
        public bool TryCommit(long revision, int money, int blood, Action grant)
        {
            if (RejectCommit || revision != Revision) return false;
            Money = money; Blood = blood; Revision++; grant(); return true;
        }
    }
    private Economy economy;
    private ItemInventory inventory;
    private ShopService shop;
    [SetUp] public void SetUp() { economy = new Economy(); inventory = new ItemInventory(); shop = new ShopService(economy, inventory); }

    [TestCase(ItemId.Lure, 20)] [TestCase(ItemId.Guarantee, 30)]
    [TestCase(ItemId.Discount, 25)] [TestCase(ItemId.Protection, 25)]
    public void BloodPurchaseRequiresPositiveRemainder(ItemId id, int price)
    {
        foreach (var amount in new[] { price - 1, price })
        {
            economy.Blood = amount;
            Assert.AreEqual(PurchaseCode.InsufficientBlood, shop.CanPurchase(id).Code);
            Assert.AreEqual(PurchaseCode.InsufficientBlood, shop.TryPurchase(id).Code);
            Assert.AreEqual(amount, economy.Blood);
            Assert.AreEqual(0, economy.Revision);
        }
        economy.Blood = price + 1;
        Assert.IsTrue(shop.TryPurchase(id).Success);
        Assert.AreEqual(1, economy.Blood);
        Assert.AreEqual(1, economy.Revision);
    }

    [Test]
    public void LureDiscountAndProtectionArePerRunAndDoNotStack()
    {
        economy.Blood = 200;
        Assert.IsTrue(shop.TryPurchase(ItemId.Lure).Success);
        Assert.IsTrue(shop.TryPurchase(ItemId.Discount).Success);
        Assert.IsTrue(shop.TryPurchase(ItemId.Protection).Success);
        var effects = inventory.CurrentEffects;
        Assert.AreEqual(15f, effects.GetEffectiveWeight(SlotSymbol.X, 10));
        Assert.AreEqual(15f, effects.GetEffectiveWeight(SlotSymbol.Y, 10));
        Assert.AreEqual(10f, effects.GetEffectiveWeight(SlotSymbol.A, 10));
        Assert.AreEqual(15f, inventory.CurrentEffects.GetEffectiveWeight(SlotSymbol.X, 10));
        Assert.AreEqual(7, effects.GetEffectiveBetCost(10));
        Assert.AreEqual(1, effects.GetEffectiveBetCost(1));
        Assert.AreEqual((int)(((long)int.MaxValue * 7 + 5) / 10), effects.GetEffectiveBetCost(int.MaxValue));
        Assert.AreEqual(2, effects.GetEffectiveLosingBloodLoss(5));
        foreach (var id in new[] { ItemId.Lure, ItemId.Discount, ItemId.Protection })
            Assert.AreEqual(PurchaseCode.AlreadyPurchased, shop.TryPurchase(id).Code);
        int blood = economy.Blood;
        inventory.ResetForNewRun(); inventory.ResetForNewRun();
        Assert.AreEqual(blood, economy.Blood);
        Assert.AreEqual(1f, inventory.CurrentEffects.GetSymbolWeightMultiplier(SlotSymbol.X));
        Assert.AreEqual(10, inventory.CurrentEffects.GetEffectiveBetCost(10));
        Assert.AreEqual(5, inventory.CurrentEffects.GetEffectiveLosingBloodLoss(5));
        Assert.IsTrue(shop.TryPurchase(ItemId.Lure).Success);
    }

    [TestCase(0, 1)] [TestCase(1, 1)] [TestCase(2, 1)]
    [TestCase(3, 2)] [TestCase(5, 4)] [TestCase(10, 7)] [TestCase(15, 11)]
    public void DiscountedBetRoundsToNearestAwayFromZeroWithMinimumOne(int original, int expected)
    {
        Assert.IsTrue(shop.TryPurchase(ItemId.Discount).Success);
        Assert.AreEqual(expected, inventory.CurrentEffects.GetEffectiveBetCost(original));
    }

    [Test]
    public void GuaranteeIsConsumedOnlyByAcceptedUniqueSpin()
    {
        economy.Blood = 200;
        Assert.IsTrue(shop.TryPurchase(ItemId.Guarantee).Success);
        Assert.AreEqual(PurchaseCode.GuaranteePending, shop.TryPurchase(ItemId.Guarantee).Code);
        Assert.IsTrue(inventory.CurrentEffects.GuaranteedXXX);
        Assert.IsTrue(inventory.HasPendingGuarantee);
        var id = Guid.NewGuid();
        var first = inventory.ConfirmSpinStarted(id);
        Assert.IsTrue(first.GuaranteedXXX);
        Assert.IsFalse(inventory.HasPendingGuarantee);
        Assert.IsTrue(shop.TryPurchase(ItemId.Guarantee).Success);
        Assert.IsTrue(inventory.ConfirmSpinStarted(id).GuaranteedXXX);
        Assert.IsTrue(inventory.HasPendingGuarantee);
        Assert.IsTrue(inventory.ConfirmSpinStarted(Guid.NewGuid()).GuaranteedXXX);
        Assert.IsFalse(inventory.ConfirmSpinStarted(Guid.NewGuid()).GuaranteedXXX);
        Assert.IsTrue(first.GuaranteedXXX);
        Assert.IsTrue(shop.TryPurchase(ItemId.Guarantee).Success);
        inventory.ResetForNewRun();
        Assert.IsFalse(inventory.HasPendingGuarantee);
    }

    [Test]
    public void BloodPackValidatesFullMoneyReserveAndCapsHealing()
    {
        economy.Blood = economy.Max;
        Assert.AreEqual(PurchaseCode.BloodFull, shop.TryPurchase(ItemId.BloodPack).Code);
        economy.Blood = 100; economy.Money = 24;
        Assert.AreEqual(PurchaseCode.InsufficientMoney, shop.TryPurchase(ItemId.BloodPack).Code);
        economy.Money = 34;
        Assert.AreEqual(PurchaseCode.BetReserveRequired, shop.TryPurchase(ItemId.BloodPack).Code);
        Assert.AreEqual(100, economy.Blood); Assert.AreEqual(34, economy.Money);
        economy.Money = 35;
        Assert.IsTrue(shop.TryPurchase(ItemId.BloodPack).Success);
        Assert.AreEqual(135, economy.Blood); Assert.AreEqual(10, economy.Money);
        economy.Money = 100; economy.Blood = 180;
        Assert.IsTrue(shop.TryPurchase(ItemId.BloodPack).Success);
        Assert.AreEqual(200, economy.Blood);
        Assert.AreEqual(PurchaseCode.BloodFull, shop.TryPurchase(ItemId.BloodPack).Code);
    }

    [Test]
    public void DiscountChangesReserveAndPackCanBeBoughtRepeatedly()
    {
        economy.Money = 32; economy.Blood = 150;
        Assert.IsTrue(shop.TryPurchase(ItemId.Discount).Success);
        Assert.IsTrue(shop.TryPurchase(ItemId.BloodPack).Success);
        Assert.AreEqual(7, economy.Money); Assert.AreEqual(160, economy.Blood);
        economy.Money = 32;
        Assert.IsTrue(shop.TryPurchase(ItemId.BloodPack).Success);
        Assert.AreEqual(195, economy.Blood);
    }

    [Test]
    public void RefusalsAndFailedCommitNeverGrantOrCharge()
    {
        Assert.AreEqual(PurchaseCode.UnknownItem, shop.TryPurchase((ItemId)900).Code);
        economy.Available = false;
        Assert.AreEqual(PurchaseCode.InvalidState, shop.TryPurchase(ItemId.Lure).Code);
        economy.Available = true; economy.RejectCommit = true;
        var result = shop.TryPurchase(ItemId.Lure);
        Assert.AreEqual(PurchaseCode.InvalidState, result.Code);
        Assert.IsNotEmpty(result.Message);
        Assert.AreEqual(100, economy.Blood);
        Assert.IsFalse(inventory.CurrentEffects.HasLure);
        economy.RejectCommit = false;
        Assert.IsTrue(shop.TryPurchase(ItemId.Lure).Success);
    }
}
