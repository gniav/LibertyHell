
using System;
using LibertyHell.Items;
using LibertyHell.SlotMachine;
using NUnit.Framework;

public class ItemEffectsAdditionalTests
{
    // GetSymbolWeightMultiplier

    [Test]
    public void GetSymbolWeightMultiplier_ReturnsOneWithoutLure()
    {
        var effects = new ItemInventory().CurrentEffects;

        Assert.AreEqual(1f, effects.GetSymbolWeightMultiplier(SlotSymbol.A));
        Assert.AreEqual(1f, effects.GetSymbolWeightMultiplier(SlotSymbol.B));
        Assert.AreEqual(1f, effects.GetSymbolWeightMultiplier(SlotSymbol.X));
        Assert.AreEqual(1f, effects.GetSymbolWeightMultiplier(SlotSymbol.Y));
        Assert.AreEqual(1f, effects.GetSymbolWeightMultiplier(SlotSymbol.H));
    }

    // GetEffectiveWeight

    [Test]
    public void GetEffectiveWeight_ReturnsBaseWeightWithoutLure()
    {
        var effects = new ItemInventory().CurrentEffects;

        Assert.AreEqual(10f, effects.GetEffectiveWeight(SlotSymbol.X, 10f));
        Assert.AreEqual(25f, effects.GetEffectiveWeight(SlotSymbol.Y, 25f));
        Assert.AreEqual(0f, effects.GetEffectiveWeight(SlotSymbol.A, 0f));
    }

    [Test]
    public void GetEffectiveWeight_ThrowsForInvalidWeight()
    {
        var effects = new ItemInventory().CurrentEffects;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => effects.GetEffectiveWeight(SlotSymbol.X, -1f));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => effects.GetEffectiveWeight(SlotSymbol.X, float.NaN));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => effects.GetEffectiveWeight(SlotSymbol.X, float.PositiveInfinity));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => effects.GetEffectiveWeight(SlotSymbol.X, float.NegativeInfinity));
    }

    // GetEffectiveBetCost

    [Test]
    public void GetEffectiveBetCost_ReturnsBaseCostWithoutDiscount()
    {
        var effects = new ItemInventory().CurrentEffects;

        Assert.AreEqual(5, effects.GetEffectiveBetCost(5));
        Assert.AreEqual(100, effects.GetEffectiveBetCost(100));
        Assert.AreEqual(1, effects.GetEffectiveBetCost(1));
    }

    [Test]
    public void GetEffectiveBetCost_ReturnsMinimumOne()
    {
        var effects = new ItemInventory().CurrentEffects;

        Assert.AreEqual(1, effects.GetEffectiveBetCost(0));
    }

    [Test]
    public void GetEffectiveBetCost_ThrowsForNegativeCost()
    {
        var effects = new ItemInventory().CurrentEffects;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => effects.GetEffectiveBetCost(-1));
    }

    [Test]
    public void GetEffectiveBetCost_HandlesMaximumIntValue()
    {
        var effects = new ItemInventory().CurrentEffects;

        Assert.AreEqual(
            int.MaxValue,
            effects.GetEffectiveBetCost(int.MaxValue));
    }

    // GetEffectiveLosingBloodLoss

    [Test]
    public void GetEffectiveLosingBloodLoss_ReturnsBaseLossWithoutProtection()
    {
        var effects = new ItemInventory().CurrentEffects;

        Assert.AreEqual(3, effects.GetEffectiveLosingBloodLoss(3));
        Assert.AreEqual(10, effects.GetEffectiveLosingBloodLoss(10));
        Assert.AreEqual(0, effects.GetEffectiveLosingBloodLoss(0));
    }

    [Test]
    public void GetEffectiveLosingBloodLoss_ThrowsForNegativeLoss()
    {
        var effects = new ItemInventory().CurrentEffects;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => effects.GetEffectiveLosingBloodLoss(-1));
    }

    // ConfirmSpinStarted

    [Test]
    public void ConfirmSpinStarted_CreatesSnapshot()
    {
        var inventory = new ItemInventory();

        var effects = inventory.ConfirmSpinStarted(Guid.NewGuid());

        Assert.IsFalse(effects.HasLure);
        Assert.IsFalse(effects.HasDiscount);
        Assert.IsFalse(effects.HasProtection);
        Assert.IsFalse(effects.GuaranteedXXX);
    }

    [Test]
    public void ConfirmSpinStarted_ReturnsSameSnapshotForRepeatedId()
    {
        var inventory = new ItemInventory();
        var id = Guid.NewGuid();

        var first = inventory.ConfirmSpinStarted(id);
        var second = inventory.ConfirmSpinStarted(id);

        Assert.AreEqual(first.HasLure, second.HasLure);
        Assert.AreEqual(first.HasDiscount, second.HasDiscount);
        Assert.AreEqual(first.HasProtection, second.HasProtection);
        Assert.AreEqual(first.GuaranteedXXX, second.GuaranteedXXX);
    }

    [Test]
    public void ConfirmSpinStarted_ThrowsForEmptyId()
    {
        var inventory = new ItemInventory();

        Assert.Throws<ArgumentException>(
            () => inventory.ConfirmSpinStarted(Guid.Empty));
    }

    // ResetForNewRun

    [Test]
    public void ResetForNewRun_ClearsSpinHistory()
    {
        var inventory = new ItemInventory();
        var id = Guid.NewGuid();

        inventory.ConfirmSpinStarted(id);
        inventory.ResetForNewRun();

        // После сброса тот же ID должен создать новый снимок.
        Assert.DoesNotThrow(
            () => inventory.ConfirmSpinStarted(id));
    }

    [Test]
    public void ResetForNewRun_CanBeCalledMultipleTimes()
    {
        var inventory = new ItemInventory();

        Assert.DoesNotThrow(() =>
        {
            inventory.ResetForNewRun();
            inventory.ResetForNewRun();
        });
    }
}
