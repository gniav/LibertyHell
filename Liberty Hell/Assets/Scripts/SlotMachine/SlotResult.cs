namespace LibertyHell.SlotMachine
{
    public readonly struct SlotResult
    {
        public SlotSymbol First { get; }
        public SlotSymbol Second { get; }
        public SlotSymbol Third { get; }
        public int MoneyPayout { get; }
        public int BloodLoss { get; }

        /// <summary>
        /// Создаёт результат вращения с символами, выплатой и потерей крови.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="third"></param>
        /// <param name="moneyPayout"></param>
        /// <param name="bloodLoss"></param>
        public SlotResult(SlotSymbol first, SlotSymbol second, SlotSymbol third, int moneyPayout, int bloodLoss)
        {
            First = first;
            Second = second;
            Third = third;
            MoneyPayout = moneyPayout;
            BloodLoss = bloodLoss;
        }
    }
}
