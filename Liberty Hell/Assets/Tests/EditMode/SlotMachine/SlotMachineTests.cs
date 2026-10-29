using System;
using LibertyHell.SlotMachine;
using NUnit.Framework;
using Machine = LibertyHell.SlotMachine.SlotMachine;

public class SlotMachineTests
{
    private sealed class SequenceRandom : ISlotRandom
    {
        private readonly int[] values;
        private int index;

        /// <summary>
        /// Создаёт источник с заранее заданной последовательностью чисел.
        /// </summary>
        /// <param name="values"></param>
        public SequenceRandom(params int[] values) => this.values = values;

        /// <summary>
        /// Возвращает следующее число из последовательности.
        /// </summary>
        /// <param name="exclusiveMax"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public int Next(int exclusiveMax) => values[index++];
    }
    /// <summary>
    /// Создаёт стандартные правила для тестов.
    /// </summary>
    /// <returns></returns>
    private static SlotRules Defaults() => new SlotRules(new[] { 40, 30, 12, 12, 6 }, new[] { 30, 50, 100, 150 }, 15, 20);
    /// <summary>
    /// Создаёт автомат для проверки готовых комбинаций.
    /// </summary>
    /// <returns></returns>
    private static Machine Create() => new Machine(Defaults(), new SequenceRandom());

    /// <summary>
    /// Проверяет выплату за три одинаковых символа без дополнительной выплаты за пару.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="payout"></param>
    [TestCase(SlotSymbol.A, 30)]
    [TestCase(SlotSymbol.B, 50)]
    [TestCase(SlotSymbol.X, 100)]
    [TestCase(SlotSymbol.Y, 150)]
    public void TriplePaysOnce(SlotSymbol symbol, int payout)
    {
        var result = Create().Evaluate(symbol, symbol, symbol);
        Assert.AreEqual(payout, result.MoneyPayout);
        Assert.AreEqual(0, result.BloodLoss);
    }
    /// <summary>
    /// Проверяет потерю крови без денежной выплаты за три символа H.
    /// </summary>
    [Test]
    public void TripleHOnlyLosesBlood()
    {
        var result = Create().Evaluate(SlotSymbol.H, SlotSymbol.H, SlotSymbol.H);
        Assert.AreEqual(0, result.MoneyPayout);
        Assert.AreEqual(20, result.BloodLoss);
    }
    /// <summary>
    /// Проверяет выплату за пару одинаковых символов, отличных от H.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    [TestCase(SlotSymbol.A, SlotSymbol.A, SlotSymbol.H)]
    [TestCase(SlotSymbol.H, SlotSymbol.B, SlotSymbol.B)]
    [TestCase(SlotSymbol.X, SlotSymbol.H, SlotSymbol.X)]
    [TestCase(SlotSymbol.Y, SlotSymbol.Y, SlotSymbol.A)]
    public void PairPays(SlotSymbol a, SlotSymbol b, SlotSymbol c) => Assert.AreEqual(15, Create().Evaluate(a, b, c).MoneyPayout);
    /// <summary>
    /// Проверяет отсутствие выплаты и потери крови для невыигрышной комбинации.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    [TestCase(SlotSymbol.H, SlotSymbol.H, SlotSymbol.A)]
    [TestCase(SlotSymbol.A, SlotSymbol.B, SlotSymbol.X)]
    public void NoWinningCombination(SlotSymbol a, SlotSymbol b, SlotSymbol c)
    {
        var result = Create().Evaluate(a, b, c);
        Assert.AreEqual(0, result.MoneyPayout);
        Assert.AreEqual(0, result.BloodLoss);
    }
    /// <summary>
    /// Проверяет границы весов и возможность повторного выбора символа.
    /// </summary>
    [Test]
    public void SpinUsesWeightBoundariesAndSelectsWithReplacement()
    {
        var machine = new Machine(Defaults(), new SequenceRandom(39, 40, 99, 70, 82, 94));
        var first = machine.Spin();
        var second = machine.Spin();
        Assert.AreEqual(SlotSymbol.A, first.First);
        Assert.AreEqual(SlotSymbol.B, first.Second);
        Assert.AreEqual(SlotSymbol.H, first.Third);
        Assert.AreEqual(SlotSymbol.X, second.First);
        Assert.AreEqual(SlotSymbol.Y, second.Second);
        Assert.AreEqual(SlotSymbol.H, second.Third);
        var repeat = new Machine(Defaults(), new SequenceRandom(0, 0, 0)).Spin();
        Assert.AreEqual(30, repeat.MoneyPayout);
    }
    /// <summary>
    /// Проверяет настраиваемые выплаты и независимость правил от исходных массивов.
    /// </summary>
    [Test]
    public void RulesAreSnapshotsAndConfigurable()
    {
        var weights = new[] { 0, 0, 0, 1, 0 };
        var payouts = new[] { 30, 50, 100, 77 };
        var rules = new SlotRules(weights, payouts, 9, 11);
        weights[3] = 0;
        payouts[3] = 0;
        var machine = new Machine(rules, new SequenceRandom(0, 0, 0));
        Assert.AreEqual(77, machine.Spin().MoneyPayout);
        Assert.AreEqual(9, machine.Evaluate(SlotSymbol.A, SlotSymbol.A, SlotSymbol.H).MoneyPayout);
        Assert.AreEqual(11, machine.Evaluate(SlotSymbol.H, SlotSymbol.H, SlotSymbol.H).BloodLoss);
    }
    /// <summary>
    /// Проверяет отклонение недопустимых весов и значений источника случайности.
    /// </summary>
    [Test]
    public void InvalidWeightsAndRandomValuesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Machine(new SlotRules(new[] { 0, 0, 0, 0, 0 }, new[] { 1, 1, 1, 1 }, 1, 1), new SequenceRandom()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Machine(new SlotRules(new[] { -1, 1, 1, 1, 1 }, new[] { 1, 1, 1, 1 }, 1, 1), new SequenceRandom()));
        Assert.Throws<InvalidOperationException>(() => new Machine(Defaults(), new SequenceRandom(100)).Spin());
    }
}
