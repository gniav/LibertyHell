using UnityEngine;

namespace LibertyHell.SlotMachine
{
    [CreateAssetMenu(menuName = "Liberty Hell/Slot Settings", fileName = "SlotSettings")]
    public sealed class SlotSettings : ScriptableObject
    {
        [SerializeField, Min(0)] private int weightA = 40;
        [SerializeField, Min(0)] private int weightB = 30;
        [SerializeField, Min(0)] private int weightX = 12;
        [SerializeField, Min(0)] private int weightY = 12;
        [SerializeField, Min(0)] private int weightH = 6;

        [SerializeField, Min(0)] private int tripleA = 30;
        [SerializeField, Min(0)] private int tripleB = 50;
        [SerializeField, Min(0)] private int tripleX = 100;
        [SerializeField, Min(0)] private int tripleY = 150;
        [SerializeField, Min(0)] private int pairPayout = 15;
        [SerializeField, Min(0)] private int tripleHBloodLoss = 20;

        /// <summary>
        /// Создаёт снимок настроек автомата для игрового сеанса.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public SlotRules CreateRules() => new SlotRules(
            new[] { weightA, weightB, weightX, weightY, weightH },
            new[] { tripleA, tripleB, tripleX, tripleY },
            pairPayout,
            tripleHBloodLoss);
    }
}
