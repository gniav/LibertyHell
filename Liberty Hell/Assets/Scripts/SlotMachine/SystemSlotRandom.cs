using System;

namespace LibertyHell.SlotMachine
{
    public sealed class SystemSlotRandom : ISlotRandom
    {
        private readonly Random random;

        /// <summary>
        /// Создаёт источник случайных чисел.
        /// </summary>
        public SystemSlotRandom() : this(new Random()) { }

        /// <summary>
        /// Создаёт источник на основе переданного генератора.
        /// </summary>
        /// <param name="random"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SystemSlotRandom(Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// Возвращает случайное число от нуля до указанной границы, не включая её.
        /// </summary>
        /// <param name="exclusiveMax"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public int Next(int exclusiveMax) => random.Next(exclusiveMax);
    }
}
