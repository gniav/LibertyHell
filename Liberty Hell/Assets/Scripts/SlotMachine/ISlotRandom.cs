namespace LibertyHell.SlotMachine
{
    public interface ISlotRandom
    {
        /// <summary>
        /// Возвращает случайное число от нуля до указанной границы, не включая её.
        /// </summary>
        /// <param name="exclusiveMax"></param>
        /// <returns></returns>
        int Next(int exclusiveMax);
    }
}
