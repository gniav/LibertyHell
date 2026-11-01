using System;
using LibertyHell.Items;
using LibertyHell.SlotMachine;
using NUnit.Framework;

public class ItemEffectsTests
{
    [Test]
    public void DefaultsAndConfirmationAreStable()
    {
        var inventory = new ItemInventory();
        var id = Guid.NewGuid();
        var effects = inventory.ConfirmSpinStarted(id);
        Assert.IsFalse(effects.GuaranteedXXX);
        Assert.AreEqual(1f, effects.GetSymbolWeightMultiplier(SlotSymbol.X));
        Assert.AreEqual(5, effects.GetEffectiveBetCost(5));
        Assert.AreEqual(3, effects.GetEffectiveLosingBloodLoss(3));
        Assert.AreEqual(effects.GuaranteedXXX, inventory.ConfirmSpinStarted(id).GuaranteedXXX);
        Assert.Throws<ArgumentException>(() => inventory.ConfirmSpinStarted(Guid.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => effects.GetEffectiveLosingBloodLoss(-1));
        inventory.ResetForNewRun();
        inventory.ResetForNewRun();
    }

    [Test]
    public void CatalogHasTheFiveFixedPrices()
    {
        Assert.AreEqual(5, ItemDefinition.All.Count);
        Assert.AreEqual(20, ItemDefinition.Find(ItemId.Lure).Price);
        Assert.AreEqual(30, ItemDefinition.Find(ItemId.Guarantee).Price);
        Assert.AreEqual(ItemCurrency.Money, ItemDefinition.Find(ItemId.BloodPack).Currency);
        Assert.AreEqual(25, ItemDefinition.Find(ItemId.BloodPack).Price);
        Assert.AreEqual(25, ItemDefinition.Find(ItemId.Discount).Price);
        Assert.AreEqual(25, ItemDefinition.Find(ItemId.Protection).Price);
        Assert.IsNull(ItemDefinition.Find((ItemId)999));
    }
}
