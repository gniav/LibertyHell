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
    public void InvalidParametersCannotResetState()
    {
        var state = new RunState(new RunParameters(1, 1, 2, 10, 20));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Reset(default));
        Assert.AreEqual(1, state.Money);
        Assert.AreEqual(20f, state.RemainingSeconds);
    }
}
