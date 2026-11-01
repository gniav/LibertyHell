using LibertyHell.GameLoop;
using LibertyHell.SlotMachine;
using NUnit.Framework;

public class SlotSpinControllerTests
{
    private static RunState Run(int money = 10, int blood = 2, int goal = 100) =>
        new RunState(new RunParameters(money, blood, blood, goal, 20));

    private static SlotResult Result(int money, int blood = 0) =>
        new SlotResult(SlotSymbol.A, SlotSymbol.B, SlotSymbol.X, money, blood);

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
    public void TerminalRunCancelsPendingResult(bool timeout)
    {
        var run = Run();
        using var slot = new SlotSpinController(run, () => Result(100, 1));
        Assert.IsTrue(slot.TryInteract(out long id));
        if (timeout) run.Tick(20); else run.DamageBlood(2);
        int blood = run.Blood;
        float time = run.RemainingSeconds;
        Assert.AreEqual(RunStatus.Lost, run.Status);
        Assert.AreEqual(SlotInteractionState.Unavailable, slot.State);
        Assert.IsFalse(slot.PendingResult.HasValue);
        Assert.IsFalse(slot.TryCompleteSpin(id));
        Assert.IsFalse(slot.TryCompleteSpin(id));
        Assert.AreEqual(0, run.Money);
        Assert.AreEqual(blood, run.Blood);
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
    public void RunEndingDuringPaymentPreventsResultGeneration()
    {
        var run = Run();
        int generated = 0;
        using var slot = new SlotSpinController(run, () => { generated++; return Result(20); });
        run.Changed += state => { if (state.IsSpinInProgress) state.Tick(20); };
        Assert.IsFalse(slot.TryInteract(out _));
        Assert.AreEqual(0, generated);
        Assert.AreEqual(RunStatus.Lost, run.Status);
        Assert.IsFalse(slot.PendingResult.HasValue);
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
}
