using LibertyHell.GameLoop;
using LibertyHell.SlotMachine;
using NUnit.Framework;
using LibertyHell.Items;

public class SlotSpinControllerTests
{
    private sealed class FixedRandom : ISlotRandom
    {
        public int Value, Calls, LastMax;
        public int Next(int exclusiveMax) { Calls++; LastMax = exclusiveMax; return Value; }
    }
    private static LibertyHell.SlotMachine.SlotMachine Machine(ISlotRandom random) =>
        new LibertyHell.SlotMachine.SlotMachine(new SlotRules(new[] { 40, 30, 12, 12, 6 },
            new[] { 30, 50, 100, 150 }, 15, 20), random);

    [Test]
    public void AllItemsApplyInOrderAndGuaranteeIsUsedOnlyOnce()
    {
        var run = new RunState(new RunParameters(14, 200, 200, 1000, 20));
        foreach (var item in new[] { ItemId.Lure, ItemId.Discount, ItemId.Protection, ItemId.Guarantee })
            Assert.IsTrue(run.Shop.TryPurchase(item).Success);
        var random = new FixedRandom { Value = 223 };
        using var slot = new SlotSpinController(run, Machine(random));
        Assert.IsTrue(slot.TryInteract(out long first));
        Assert.AreEqual(7, run.Money);
        Assert.AreEqual(0, random.Calls);
        Assert.IsFalse(run.Inventory.HasPendingGuarantee);
        Assert.AreEqual(SlotSymbol.X, slot.PendingResult.Value.First);
        Assert.AreEqual(100, slot.PendingResult.Value.MoneyPayout);
        Assert.IsTrue(slot.TryCompleteSpin(first));
        Assert.AreEqual(107, run.Money);
        Assert.IsFalse(slot.TryCompleteSpin(first));
        Assert.IsTrue(slot.TryInteract(out long second));
        Assert.AreEqual(224, random.LastMax);
        Assert.AreEqual(3, random.Calls);
        Assert.AreEqual(SlotSymbol.H, slot.PendingResult.Value.First);
        int blood = run.Blood;
        Assert.IsTrue(slot.TryCompleteSpin(second));
        Assert.AreEqual(blood - 10, run.Blood);
        Assert.IsTrue(run.DamageBlood(3));
        Assert.AreEqual(blood - 13, run.Blood);
    }

    [Test]
    public void RejectedStartDoesNotConsumeGuaranteeOrChooseSymbols()
    {
        var run = new RunState(new RunParameters(6, 100, 100, 1000, 20));
        Assert.IsTrue(run.Shop.TryPurchase(ItemId.Discount).Success);
        Assert.IsTrue(run.Shop.TryPurchase(ItemId.Guarantee).Success);
        var random = new FixedRandom();
        using var slot = new SlotSpinController(run, Machine(random));
        Assert.IsFalse(slot.TryInteract(out _));
        Assert.IsTrue(run.Inventory.HasPendingGuarantee);
        Assert.AreEqual(0, random.Calls);
        Assert.IsTrue(run.TryAddMoney(1));
        Assert.IsTrue(slot.TryInteract(out long id));
        Assert.AreEqual(0, run.Money);
        Assert.IsFalse(slot.TryInteract(out _));
        Assert.IsTrue(slot.TryCompleteSpin(id));
    }

    [Test]
    public void LegacyResultDelegateCannotBypassGuarantee()
    {
        var run = new RunState(new RunParameters(20, 100, 100, 1000, 20));
        Assert.IsTrue(run.Shop.TryPurchase(ItemId.Guarantee).Success);
        int calls = 0;
        using var slot = new SlotSpinController(run, () => { calls++; return Result(0); });
        Assert.IsFalse(slot.CanInteract);
        Assert.IsFalse(slot.TryInteract(out _));
        Assert.AreEqual(20, run.Money);
        Assert.IsTrue(run.Inventory.HasPendingGuarantee);
        Assert.AreEqual(0, calls);
    }

    [Test]
    public void LureChangesWeightedSelectionWithoutChangingBaseSampling()
    {
        var random = new FixedRandom { Value = 82 };
        var machine = Machine(random);
        Assert.AreEqual(SlotSymbol.Y, machine.Spin().First);
        Assert.AreEqual(100, random.LastMax);
        var run = new RunState(new RunParameters(20, 100, 100, 1000, 20));
        Assert.IsTrue(run.Shop.TryPurchase(ItemId.Lure).Success);
        random.Value = 165;
        using var slot = new SlotSpinController(run, machine);
        Assert.IsTrue(slot.TryInteract(out _));
        Assert.AreEqual(224, random.LastMax);
        Assert.AreEqual(SlotSymbol.X, slot.PendingResult.Value.First);
    }

    [Test]
    public void PaymentPrecedesConsumptionAndResetDiscardsResultAndItems()
    {
        var parameters = new RunParameters(20, 100, 100, 1000, 20);
        var run = new RunState(parameters);
        Assert.IsTrue(run.Shop.TryPurchase(ItemId.Guarantee).Success);
        bool observedPayment = false;
        bool observedConsumption = false;
        run.Changed += state =>
        {
            if (!state.IsSpinInProgress) return;
            if (observedPayment) { observedConsumption = !state.Inventory.HasPendingGuarantee; return; }
            observedPayment = true;
            Assert.AreEqual(10, state.Money);
            Assert.IsTrue(state.Inventory.HasPendingGuarantee);
            Assert.IsFalse(state.Shop.TryPurchase(ItemId.Lure).Success);
        };
        using var slot = new SlotSpinController(run, Machine(new FixedRandom()));
        Assert.IsTrue(slot.TryInteract(out long oldId));
        Assert.IsTrue(observedPayment);
        Assert.IsTrue(observedConsumption);
        Assert.IsFalse(run.Inventory.HasPendingGuarantee);
        run.Reset(parameters);
        Assert.IsFalse(slot.PendingResult.HasValue);
        Assert.IsFalse(slot.TryCompleteSpin(oldId));
        Assert.IsTrue(slot.TryInteract(out long newId));
        Assert.IsTrue(slot.TryCompleteSpin(newId));
        Assert.AreEqual(40, run.Money);
    }
    private static RunState Run(int money = 10, int blood = 2, int goal = 100) =>
        new RunState(new RunParameters(money, blood, blood, goal, 20));

    private static SlotResult Result(int money, int blood = 0) =>
        new SlotResult(SlotSymbol.A, SlotSymbol.B, SlotSymbol.X, money, blood);

    [TestCase(false, 100, 20)]
    [TestCase(true, 100, 10)]
    [TestCase(false, 5, 5)]
    [TestCase(true, 30, 5)]
    public void CompletedResultReportsActualBloodLost(bool protection, int startingBlood, int expectedLoss)
    {
        var run = new RunState(new RunParameters(100, startingBlood, 100, 1000, 20));
        if (protection) Assert.IsTrue(run.Shop.TryPurchase(ItemId.Protection).Success);
        using var slot = new SlotSpinController(run, () => Result(0, 20));
        Assert.IsTrue(slot.TryInteract(out long spinId));
        int bloodBefore = run.Blood;
        Assert.IsFalse(slot.TryCompleteSpin(spinId + 1, out _));
        Assert.IsTrue(slot.TryCompleteSpin(spinId, out var applied));
        Assert.AreEqual(20, applied.Result.BloodLoss);
        Assert.AreEqual(expectedLoss, applied.BloodLost);
        Assert.AreEqual(expectedLoss, bloodBefore - run.Blood);
        Assert.IsFalse(slot.TryCompleteSpin(spinId, out _));
    }

    [Test]
    public void PaidSpinGeneratesOnceAndAppliesOnlyOnCompletion()
    {
        var run = Run(20);
        int generated = 0;
        using var slot = new SlotSpinController(run, () => { generated++; return Result(15); });
        Assert.IsTrue(slot.CanInteract);
        Assert.IsTrue(slot.TryInteract(out long id));
        Assert.AreEqual(10, run.Money);
        Assert.AreEqual(1, generated);
        Assert.AreEqual(SlotInteractionState.Spinning, slot.State);
        Assert.IsFalse(slot.CanInteract);
        Assert.IsFalse(slot.TryInteract(out _));
        Assert.AreEqual(10, run.Money);
        Assert.IsTrue(slot.TryCompleteSpin(id));
        Assert.AreEqual(25, run.Money);
        Assert.IsFalse(slot.PendingResult.HasValue);
        Assert.IsFalse(slot.TryCompleteSpin(id));
        Assert.IsTrue(slot.CanInteract);
        Assert.IsTrue(slot.TryInteract(out long next));
        Assert.IsFalse(slot.TryCompleteSpin(id));
        Assert.IsTrue(slot.TryCompleteSpin(next));
        Assert.AreEqual(30, run.Money);
        Assert.AreEqual(2, generated);
    }

    [Test]
    public void InsufficientFundsDoNotStartOrGenerate()
    {
        var run = Run(9);
        int generated = 0;
        using var slot = new SlotSpinController(run, () => { generated++; return Result(20); });
        Assert.IsFalse(slot.CanInteract);
        Assert.IsFalse(slot.TryInteract(out _));
        Assert.AreEqual(9, run.Money);
        Assert.AreEqual(0, generated);
        Assert.IsFalse(run.IsSpinInProgress);
    }

    [Test]
    public void ZeroBalanceWaitsForPayoutAndCanContinue()
    {
        var run = Run();
        using var slot = new SlotSpinController(run, () => Result(15));
        Assert.IsTrue(slot.TryInteract(out long id));
        Assert.AreEqual(0, run.Money);
        Assert.AreEqual(RunStatus.Running, run.Status);
        Assert.IsTrue(slot.TryCompleteSpin(id));
        Assert.AreEqual(15, run.Money);
        Assert.AreEqual(RunStatus.Running, run.Status);
        Assert.IsTrue(slot.CanInteract);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void FatalEventsDuringPaidSpinWaitForCompletion(bool timeout)
    {
        var run = Run();
        using var slot = new SlotSpinController(run, () => Result(100, 1));
        Assert.IsTrue(slot.TryInteract(out long id));
        if (timeout) run.Tick(20); else run.DamageBlood(2);
        int blood = run.Blood;
        float time = run.RemainingSeconds;
        Assert.AreEqual(RunStatus.Running, run.Status);
        Assert.AreEqual(SlotInteractionState.Spinning, slot.State);
        Assert.IsTrue(slot.PendingResult.HasValue);
        Assert.IsTrue(slot.TryCompleteSpin(id));
        Assert.IsFalse(slot.TryCompleteSpin(id));
        Assert.AreEqual(100, run.Money);
        Assert.AreEqual(System.Math.Max(0, blood - 1), run.Blood);
        Assert.AreEqual(time, run.RemainingSeconds);
    }

    [Test]
    public void FullResultWinsBeforeBloodLossOrInsufficientFunds()
    {
        var run = Run(10, 1, 15);
        using var slot = new SlotSpinController(run, () => Result(15, 1));
        Assert.IsTrue(slot.TryInteract(out long id));
        Assert.IsTrue(slot.TryCompleteSpin(id));
        Assert.AreEqual(0, run.Blood);
        Assert.AreEqual(RunStatus.Won, run.Status);
    }

    [Test]
    public void ObserversCannotReenterDuringPaymentOrCompletion()
    {
        var run = Run(20);
        using var slot = new SlotSpinController(run, () => Result(15));
        int attempts = 0;
        run.Changed += state => { attempts++; Assert.IsFalse(slot.TryInteract(out _)); };
        Assert.IsTrue(slot.TryInteract(out long id));
        Assert.IsTrue(slot.TryCompleteSpin(id));
        Assert.AreEqual(2, attempts);
        Assert.AreEqual(25, run.Money);
    }

    [Test]
    public void TimeoutDuringPaymentWaitsForResult()
    {
        var run = Run();
        int generated = 0;
        using var slot = new SlotSpinController(run, () => { generated++; return Result(20); });
        bool timedOut = false;
        run.Changed += state => { if (state.IsSpinInProgress && !timedOut) { timedOut = true; state.Tick(20); } };
        Assert.IsTrue(slot.TryInteract(out long id));
        Assert.AreEqual(1, generated);
        Assert.AreEqual(RunStatus.Running, run.Status);
        Assert.IsTrue(slot.TryCompleteSpin(id));
        Assert.AreEqual(RunStatus.Lost, run.Status);
    }

    [Test]
    public void PauseBlocksInteractionAndResetInvalidatesOldCompletion()
    {
        var run = Run(20);
        using var slot = new SlotSpinController(run, () => Result(15));
        Assert.IsTrue(slot.TryInteract(out long oldId));
        Assert.IsTrue(run.Pause());
        Assert.AreEqual(SlotInteractionState.Unavailable, slot.State);
        Assert.IsFalse(slot.TryCompleteSpin(oldId));
        Assert.IsTrue(run.Resume());
        Assert.AreEqual(SlotInteractionState.Spinning, slot.State);
        run.Reset(new RunParameters(20, 2, 2, 100, 20));
        Assert.IsFalse(slot.PendingResult.HasValue);
        Assert.IsTrue(slot.TryInteract(out long newId));
        Assert.IsFalse(slot.TryCompleteSpin(oldId));
        Assert.IsTrue(slot.TryCompleteSpin(newId));
        Assert.AreEqual(25, run.Money);
    }
    [Test]
    public void PauseDuringPaymentKeepsPaidSpinUntilResume()
    {
        var run = Run(20);
        using var slot = new SlotSpinController(run, () => Result(15));
        bool paused = false;
        run.Changed += state =>
        {
            if (state.IsSpinInProgress && !paused)
            {
                paused = true;
                Assert.IsTrue(state.Pause());
            }
        };
        Assert.IsTrue(slot.TryInteract(out long id));
        Assert.AreEqual(10, run.Money);
        Assert.AreEqual(RunStatus.Paused, run.Status);
        Assert.IsTrue(run.IsSpinInProgress);
        Assert.IsTrue(slot.PendingResult.HasValue);
        Assert.IsFalse(slot.TryCompleteSpin(id));
        Assert.IsTrue(run.Resume());
        Assert.IsTrue(slot.TryCompleteSpin(id));
        Assert.AreEqual(25, run.Money);
        Assert.IsFalse(slot.TryCompleteSpin(id));
    }

}
