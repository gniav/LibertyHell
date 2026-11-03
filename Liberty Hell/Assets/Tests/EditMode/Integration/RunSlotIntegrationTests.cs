using LibertyHell.GameLoop;
using LibertyHell.SlotMachine;
using NUnit.Framework;
using Machine = LibertyHell.SlotMachine.SlotMachine;

public class RunSlotIntegrationTests
{
    private sealed class FixedSequence : ISlotRandom
    {
        private readonly int[] values;
        private int position;

        /// <summary>
        /// Создаёт источник заранее заданных значений.
        /// </summary>
        /// <param name="values"></param>
        public FixedSequence(params int[] values)
        {
            this.values = values;
        }

        /// <summary>
        /// Возвращает следующее значение последовательности.
        /// </summary>
        /// <param name="exclusiveMax"></param>
        /// <returns></returns>
        public int Next(int exclusiveMax)
        {
            return values[position++];
        }
    }

    /// <summary>
    /// Проверяет списание ставки и применение результата настоящего автомата к забегу.
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <param name="third"></param>
    /// <param name="payout"></param>
    /// <param name="bloodLoss"></param>
    [TestCase(0, 0, 0, 30, 0)]
    [TestCase(40, 40, 40, 50, 0)]
    [TestCase(70, 70, 70, 100, 0)]
    [TestCase(82, 82, 82, 150, 0)]
    [TestCase(0, 40, 0, 15, 0)]
    [TestCase(94, 94, 94, 0, 20)]
    [TestCase(0, 40, 70, 0, 0)]
    public void SpinAppliesResultOnce(int first, int second, int third, int payout, int bloodLoss)
    {
        var run = new RunState(new RunParameters(100, 100, 100, 1000, 480));
        var rules = new SlotRules(new[] { 40, 30, 12, 12, 6 }, new[] { 30, 50, 100, 150 }, 15, 20);
        var machine = new Machine(rules, new FixedSequence(first, second, third));

        using (var controller = new SlotSpinController(run, machine))
        {
            Assert.IsTrue(controller.TryInteract(out long spinId));
            Assert.AreEqual(90, run.Money);
            Assert.IsFalse(controller.TryInteract(out _));
            Assert.IsTrue(controller.TryCompleteSpin(spinId));
            Assert.AreEqual(90 + payout, run.Money);
            Assert.AreEqual(100 - bloodLoss, run.Blood);
            Assert.IsFalse(controller.TryCompleteSpin(spinId));
        }
    }

    /// <summary>
    /// Проверяет выплату до завершения забега, если время закончилось во время вращения.
    /// </summary>
    [Test]
    public void ExpiredTimeWaitsForSpinResult()
    {
        var run = new RunState(new RunParameters(10, 100, 100, 1000, 1));
        var rules = new SlotRules(new[] { 40, 30, 12, 12, 6 }, new[] { 30, 50, 100, 150 }, 15, 20);
        var machine = new Machine(rules, new FixedSequence(82, 82, 82));

        using (var controller = new SlotSpinController(run, machine))
        {
            Assert.IsTrue(controller.TryInteract(out long spinId));
            run.Tick(1);
            Assert.AreEqual(RunStatus.Running, run.Status);
            Assert.AreEqual(0, run.Money);
            Assert.IsTrue(controller.TryCompleteSpin(spinId));
            Assert.AreEqual(150, run.Money);
            Assert.AreEqual(RunStatus.Lost, run.Status);
            Assert.IsFalse(controller.TryCompleteSpin(spinId));
        }
    }

    /// <summary>
    /// Проверяет выплату до завершения забега, если кровь закончилась во время вращения.
    /// </summary>
    [Test]
    public void ZeroBloodWaitsForSpinResult()
    {
        var run = new RunState(new RunParameters(10, 10, 10, 1000, 60));
        var rules = new SlotRules(new[] { 40, 30, 12, 12, 6 }, new[] { 30, 50, 100, 150 }, 15, 20);
        var machine = new Machine(rules, new FixedSequence(82, 82, 82));

        using (var controller = new SlotSpinController(run, machine))
        {
            Assert.IsTrue(controller.TryInteract(out long spinId));
            Assert.IsTrue(run.DamageBlood(10));
            Assert.AreEqual(RunStatus.Running, run.Status);
            Assert.AreEqual(0, run.Money);
            Assert.IsTrue(controller.TryCompleteSpin(spinId));
            Assert.AreEqual(150, run.Money);
            Assert.AreEqual(RunStatus.Lost, run.Status);
            Assert.IsFalse(controller.TryCompleteSpin(spinId));
        }
    }
}
