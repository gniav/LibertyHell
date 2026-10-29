using System;
using LibertyHell.GameLoop;
using NUnit.Framework;

public class RunStateTests
{
    [Test]
    public void InitializesFromSnapshot()
    {
        var state = new RunState(new RunParameters(42, 3, 8, 500, 90));
        Assert.AreEqual(42, state.Money);
        Assert.AreEqual(3, state.Blood);
        Assert.AreEqual(8, state.Parameters.MaxBlood);
        Assert.AreEqual(500, state.Parameters.MoneyGoal);
        Assert.AreEqual(90f, state.RemainingSeconds);
        Assert.AreEqual(RunStatus.Running, state.Status);
    }

    [Test]
    public void ResetReplacesSnapshotAndNotifiesOnceAfterAllValuesAreSet()
    {
        var state = new RunState(new RunParameters(1, 1, 2, 10, 20));
        int calls = 0;
        state.Changed += changed =>
        {
            calls++;
            Assert.AreSame(state, changed);
            Assert.AreEqual(7, changed.Money);
            Assert.AreEqual(4, changed.Blood);
            Assert.AreEqual(40f, changed.RemainingSeconds);
            Assert.AreEqual(100, changed.Parameters.MoneyGoal);
        };
        state.Reset(new RunParameters(7, 4, 6, 100, 40));
        Assert.AreEqual(1, calls);
    }

    [Test]
    public void SpendingMoneyNotifiesOnlyAfterSuccessfulPayment()
    {
        var state = new RunState(new RunParameters(10, 1, 2, 20, 30));
        int calls = 0;
        state.Changed += changed => { calls++; Assert.AreEqual(6, changed.Money); };

        Assert.IsTrue(state.TrySpendMoney(4));
        Assert.IsFalse(state.TrySpendMoney(7));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.TrySpendMoney(0));
        Assert.AreEqual(6, state.Money);
        Assert.AreEqual(1, calls);
    }

    [Test]
    public void InvalidParametersCannotResetState()
    {
        var state = new RunState(new RunParameters(1, 1, 2, 10, 20));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Reset(default));
        Assert.AreEqual(1, state.Money);
        Assert.AreEqual(20f, state.RemainingSeconds);
    }

    [Test]
    public void SpinDefersMoneyLossUntilResultOrCancellation()
    {
        var state = new RunState(new RunParameters(10, 1, 2, 100, 30));
        Assert.IsTrue(state.BeginSpin());
        Assert.IsFalse(state.BeginSpin());
        Assert.IsFalse(state.TrySpendMoney(11));
        Assert.IsTrue(state.IsSpinInProgress);
        Assert.IsTrue(state.TrySpendMoney(10));
        Assert.AreEqual(RunStatus.Running, state.Status);
        Assert.IsTrue(state.TryAddMoney(50));
        Assert.IsTrue(state.EndSpin());
        Assert.IsFalse(state.EndSpin());
        Assert.AreEqual(RunStatus.Running, state.Status);
        Assert.IsTrue(state.BeginSpin());
        Assert.IsTrue(state.TrySpendMoney(50));
        Assert.IsTrue(state.EndSpin());
        Assert.AreEqual(RunStatus.Lost, state.Status);
        Assert.IsFalse(state.Resume());
    }

    [Test]
    public void ResourcesAreBoundedAndTerminalStatesCannotBeChanged()
    {
        var state = new RunState(new RunParameters(10, 2, 3, int.MaxValue, 30));
        Assert.IsFalse(state.TryAddMoney(int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.TryAddMoney(-1));
        Assert.IsTrue(state.RestoreBlood(int.MaxValue));
        Assert.AreEqual(3, state.Blood);
        Assert.IsTrue(state.DamageBlood(int.MaxValue));
        Assert.AreEqual(0, state.Blood);
        Assert.AreEqual(RunStatus.Lost, state.Status);
        Assert.IsFalse(state.TryAddMoney(1));
        Assert.IsFalse(state.RestoreBlood(1));
        Assert.IsFalse(state.Pause());
    }

    [Test]
    public void GoalAndInitialConditionsHaveDeterministicPrecedence()
    {
        var state = new RunState(new RunParameters(5, 1, 1, 10, 30));
        Assert.IsTrue(state.TryAddMoney(5));
        Assert.AreEqual(RunStatus.Won, state.Status);
        Assert.IsFalse(state.TryAddMoney(1));
        state.Reset(new RunParameters(10, 0, 1, 10, 30));
        Assert.AreEqual(RunStatus.Lost, state.Status);
        state.Reset(new RunParameters(0, 1, 1, 10, 30));
        Assert.AreEqual(RunStatus.Lost, state.Status);
    }
}
