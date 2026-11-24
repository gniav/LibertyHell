using System;
using LibertyHell.GameLoop;
using NUnit.Framework;

public class RunStateTests
{
    // Проверяем применение всех эффектов платного вращения перед определением результата.
    [Test]
    public void PaidSpinAppliesAllEffectsBeforeChoosingOutcome()
    {
        var state = new RunState(new RunParameters(10, 1, 1, 50, 20));
        Assert.IsTrue(state.TryBeginPaidSpin(10));
        Assert.AreEqual(0, state.Money);
        Assert.AreEqual(RunStatus.Running, state.Status);
        Assert.IsFalse(state.TryBeginPaidSpin(10));
        Assert.IsTrue(state.TryCompletePaidSpin(50, 1, 10));
        Assert.AreEqual(0, state.Blood);
        Assert.AreEqual(50, state.Money);
        Assert.AreEqual(RunStatus.Won, state.Status);
        Assert.IsFalse(state.TryCompletePaidSpin(50, 0, 10));
    }

    // Проверяем наличие денег для платного вращения и проверку следующей ставки.
    [Test]
    public void PaidSpinRequiresFundsAndChecksNextStakeAfterResult()
    {
        var state = new RunState(new RunParameters(15, 1, 1, 100, 20));
        Assert.IsTrue(state.TryBeginPaidSpin(10));
        Assert.IsTrue(state.TryCompletePaidSpin(0, 0, 10));
        Assert.AreEqual(RunStatus.Lost, state.Status);
        state.Reset(new RunParameters(9, 1, 1, 100, 20));
        Assert.IsFalse(state.TryBeginPaidSpin(10));
        Assert.AreEqual(9, state.Money);
        Assert.IsFalse(state.IsSpinInProgress);
    }

    // Проверяем обработку окончания времени и переполнения при платном вращении.
    [Test]
    public void PaidSpinDefersTimeoutAndRejectsOverflow()
    {
        var state = new RunState(new RunParameters(10, 1, 1, int.MaxValue, 20));
        Assert.IsTrue(state.TryBeginPaidSpin(10));
        state.Tick(20);
        Assert.AreEqual(RunStatus.Running, state.Status);
        Assert.IsTrue(state.TryCompletePaidSpin(1, 0, 10));
        Assert.AreEqual(RunStatus.Lost, state.Status);
        state.Reset(new RunParameters(20, 1, 1, int.MaxValue, 20));
        Assert.IsTrue(state.TryBeginPaidSpin(10));
        Assert.IsFalse(state.TryCompletePaidSpin(int.MaxValue, 0, 10));
        Assert.AreEqual(10, state.Money);
        Assert.IsTrue(state.IsSpinInProgress);
    }
    
    // Проверяем, что покупка применяется целиком, а устаревшее состояние отклоняется.
    [Test]
    public void PurchaseIsAtomicAndResetClearsInventory()
    {
        var state = new RunState(new RunParameters(100, 100, 100, 1000, 20));
        var economy = (LibertyHell.Shop.IShopEconomy)state;
        var old = economy.Read();
        int calls = 0;
        state.Changed += changed =>
        {
            calls++;
            Assert.AreEqual(75, changed.Blood);
            Assert.IsTrue(changed.Inventory.CurrentEffects.HasProtection);
            Assert.IsFalse(changed.TrySpendMoney(1));
        };
        Assert.IsTrue(state.Shop.TryPurchase(LibertyHell.Items.ItemId.Protection).Success);
        Assert.AreEqual(1, calls);
        Assert.IsFalse(economy.TryCommit(old.Revision, 1, 1, () => Assert.Fail("stale grant")));
    }

    // Проверяем очистку предметов игровой сессии после сброса.
    [Test]
    public void ResetClearsRunItems()
    {
        var parameters = new RunParameters(100, 100, 100, 1000, 20);
        var state = new RunState(parameters);
        Assert.IsTrue(state.Shop.TryPurchase(LibertyHell.Items.ItemId.Guarantee).Success);
        state.Reset(parameters);
        Assert.IsFalse(state.Inventory.HasPendingGuarantee);
    }

    // Проверяем начальные значения ресурсов, времени и цели игровой сессии.
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

    // Проверяем обновление всех значений состояния и однократное уведомление после сброса.
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

     // Проверяем, что уведомление об изменении денег происходит только после успешного списания.
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

    // Проверяем, что некорректные параметры не изменяют текущее состояние.
    [Test]
    public void InvalidParametersCannotResetState()
    {
        var state = new RunState(new RunParameters(1, 1, 2, 10, 20));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Reset(default));
        Assert.AreEqual(1, state.Money);
        Assert.AreEqual(20f, state.RemainingSeconds);
    }

     // Проверяем списание денег во время вращения и завершение вращения.
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

    // Проверяем ограничения ресурсов и невозможность изменения завершённой игровой сессии.
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

    // Проверяем приоритет победы и поражения при достижении начальных условий.
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

     // Проверяем сохранение таймера и частичного восстановления крови при паузе.
    [Test]
    public void PausePreservesTimerAndPartialRegeneration()
    {
        var state = new RunState(new RunParameters(10, 5, 10, 100, 30));
        state.Tick(4);
        Assert.IsTrue(state.Pause());
        state.Tick(100);
        Assert.AreEqual(26, state.RemainingSeconds);
        Assert.AreEqual(5, state.Blood);
        Assert.IsTrue(state.Resume());
        state.Tick(1);
        Assert.AreEqual(6, state.Blood);
        Assert.AreEqual(25, state.RemainingSeconds);
    }

    // Проверяем, что результат не зависит от размера интервала обновления.
    [Test]
    public void MultipleIntervalsAndSmallTicksProduceSameResult()
    {
        var parameters = new RunParameters(10, 1, 10, 100, 60, 2, 5);
        var large = new RunState(parameters);
        var small = new RunState(parameters);
        large.Tick(16);
        for (int i = 0; i < 16; i++) small.Tick(1);
        Assert.AreEqual(7, large.Blood);
        Assert.AreEqual(large.Blood, small.Blood);
        Assert.AreEqual(large.RemainingSeconds, small.RemainingSeconds);
        large.Tick(10);
        Assert.AreEqual(10, large.Blood);
    }

    // Проверяем завершение игровой сессии по таймеру во время вращения и очистку прогресса после сброса.
    [Test]
    public void TimeoutDuringSpinStopsChangesAndResetClearsProgress()
    {
        var state = new RunState(new RunParameters(10, 1, 10, 100, 8));
        int calls = 0;
        state.Changed += _ => calls++;
        Assert.IsTrue(state.BeginSpin());
        Assert.IsTrue(state.TrySpendMoney(10));
        state.Tick(4);
        state.Tick(10);
        Assert.AreEqual(0, state.RemainingSeconds);
        Assert.AreEqual(2, state.Blood);
        Assert.AreEqual(RunStatus.Lost, state.Status);
        Assert.IsFalse(state.IsSpinInProgress);
        int finishedCalls = calls;
        state.Tick(1);
        Assert.IsFalse(state.TryAddMoney(50));
        Assert.IsFalse(state.EndSpin());
        Assert.AreEqual(finishedCalls, calls);
        state.Reset(new RunParameters(10, 1, 10, 100, 8));
        state.Tick(4);
        Assert.AreEqual(1, state.Blood);
        Assert.AreEqual(4, state.RemainingSeconds);
    }

    // Проверяем отклонение некорректных значений времени и параметров восстановления крови.
    [Test]
    public void TickAndRegenParametersRejectInvalidValues()
    {
        var state = new RunState(new RunParameters(1, 1, 1, 10, 20));
        state.Tick(0);
        Assert.AreEqual(20, state.RemainingSeconds);
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Tick(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Tick(float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Tick(float.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RunParameters(1, 1, 1, 10, 20, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RunParameters(1, 1, 1, 10, 20, -1, 5));
    }

    // Проверяем приоритет потери крови во время вращения и сброс состояния вращения.
    [Test]
    public void BloodLossDuringSpinTakesPriorityAndResetClearsSpin()
    {
        var parameters = new RunParameters(10, 1, 1, 100, 20);
        var state = new RunState(parameters);
        Assert.IsTrue(state.BeginSpin());
        Assert.IsTrue(state.TrySpendMoney(10));
        Assert.IsTrue(state.DamageBlood(1));
        Assert.AreEqual(RunStatus.Lost, state.Status);
        Assert.IsFalse(state.IsSpinInProgress);
        state.Reset(parameters);
        Assert.IsTrue(state.BeginSpin());
        state.Reset(parameters);
        Assert.IsFalse(state.IsSpinInProgress);
        Assert.IsTrue(state.BeginSpin());
    }

     // Проверяем завершение вращения после победы и невозможность повторного завершения.
    [Test]
    public void WinningPayoutEndsSpinAndCannotFinishTwice()
    {
        var state = new RunState(new RunParameters(10, 1, 1, 50, 20));
        Assert.IsTrue(state.BeginSpin());
        Assert.IsTrue(state.TrySpendMoney(10));
        Assert.IsTrue(state.TryAddMoney(50));
        Assert.AreEqual(RunStatus.Won, state.Status);
        Assert.IsFalse(state.IsSpinInProgress);
        Assert.IsFalse(state.EndSpin());
        Assert.IsFalse(state.CanSpendMoney(1));
    }

    // Проверяем проверку достаточности денег после отмены вращения на паузе.
    [Test]
    public void PausedCancellationChecksMoneyOnResume()
    {
        var state = new RunState(new RunParameters(10, 1, 1, 50, 20));
        Assert.IsTrue(state.BeginSpin());
        Assert.IsTrue(state.TrySpendMoney(10));
        Assert.IsTrue(state.Pause());
        Assert.IsTrue(state.EndSpin());
        Assert.AreEqual(RunStatus.Paused, state.Status);
        Assert.IsTrue(state.Resume());
        Assert.AreEqual(RunStatus.Lost, state.Status);
    }
}
