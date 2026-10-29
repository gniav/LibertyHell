using System;

namespace LibertyHell.SlotMachine
{
    public sealed class SlotMachine
    {
        private readonly SlotRules rules;
        private readonly ISlotRandom random;
        private readonly int[] weights;
        private readonly int totalWeight;

        
        /// <summary>
        /// Создаёт автомат с заданными правилами и источником случайности.
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="random"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public SlotMachine(SlotRules rules, ISlotRandom random)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            weights = rules.CopyWeights();

            long total = 0;
            foreach (int weight in weights)
            {
                if (weight < 0)
                    throw new ArgumentOutOfRangeException(nameof(rules), "Weights cannot be negative.");

                total += weight;
            }

            if (total == 0 || total > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(rules), "The total weight must be positive and fit in an integer.");

            totalWeight = (int)total;
        }

        /// <summary>
        /// Выбирает три символа и возвращает результат вращения.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public SlotResult Spin() => Evaluate(ChooseSymbol(), ChooseSymbol(), ChooseSymbol());

        /// <summary>
        /// Определяет выплату и потерю крови для трёх символов.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="third"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public SlotResult Evaluate(SlotSymbol first, SlotSymbol second, SlotSymbol third)
        {
            Validate(first);
            Validate(second);
            Validate(third);

            if (first == second && second == third)
            {
                return first == SlotSymbol.H
                    ? new SlotResult(first, second, third, 0, rules.HTripleBloodLoss)
                    : new SlotResult(first, second, third, rules.TriplePayout(first), 0);
            }

            bool hasPair = IsPayingPair(first, second)
                || IsPayingPair(first, third)
                || IsPayingPair(second, third);

            return new SlotResult(first, second, third, hasPair ? rules.PairPayout : 0, 0);
        }

        /// <summary>
        /// Выбирает символ с учётом его веса.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private SlotSymbol ChooseSymbol()
        {
            int value = random.Next(totalWeight);
            if (value < 0 || value >= totalWeight)
                throw new InvalidOperationException("Random source returned a value outside the requested range.");

            for (int i = 0; i < weights.Length; i++)
            {
                if (value < weights[i])
                    return (SlotSymbol)i;

                value -= weights[i];
            }

            throw new InvalidOperationException("Invalid slot weights.");
        }

        /// <summary>
        /// Проверяет, образуют ли два символа оплачиваемую пару.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        private static bool IsPayingPair(SlotSymbol first, SlotSymbol second) =>
            first == second && first != SlotSymbol.H;

        /// <summary>
        /// Проверяет, что символ входит в список допустимых.
        /// </summary>
        /// <param name="symbol"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static void Validate(SlotSymbol symbol)
        {
            if (symbol < SlotSymbol.A || symbol > SlotSymbol.H)
                throw new ArgumentOutOfRangeException(nameof(symbol));
        }
    }
}
