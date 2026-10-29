using System;

namespace LibertyHell.SlotMachine
{
    public sealed class SlotRules
    {
        private readonly int[] weights;
        private readonly int[] triplePayouts;

        public int PairPayout { get; }
        public int HTripleBloodLoss { get; }

        /// <summary>
        /// Создаёт независимый набор весов и выплат.
        /// </summary>
        /// <param name="weights"></param>
        /// <param name="triplePayouts"></param>
        /// <param name="pairPayout"></param>
        /// <param name="hTripleBloodLoss"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public SlotRules(int[] weights, int[] triplePayouts, int pairPayout, int hTripleBloodLoss)
        {
            if (weights == null || weights.Length != 5)
                throw new ArgumentException("Provide five symbol weights.", nameof(weights));
            if (triplePayouts == null || triplePayouts.Length != 4)
                throw new ArgumentException("Provide payouts for A, B, X and Y.", nameof(triplePayouts));

            foreach (int payout in triplePayouts)
            {
                if (payout < 0)
                    throw new ArgumentOutOfRangeException(nameof(triplePayouts));
            }

            if (pairPayout < 0)
                throw new ArgumentOutOfRangeException(nameof(pairPayout));
            if (hTripleBloodLoss < 0)
                throw new ArgumentOutOfRangeException(nameof(hTripleBloodLoss));

            this.weights = (int[])weights.Clone();
            this.triplePayouts = (int[])triplePayouts.Clone();
            PairPayout = pairPayout;
            HTripleBloodLoss = hTripleBloodLoss;
        }

        /// <summary>
        /// Возвращает копию весов символов.
        /// </summary>
        /// <returns></returns>
        internal int[] CopyWeights() => (int[])weights.Clone();
        /// <summary>
        /// Возвращает выплату за три одинаковых символа.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        internal int TriplePayout(SlotSymbol symbol) => triplePayouts[(int)symbol];
    }
}
