using System;

namespace LibertyHell.GameLoop
{
    /// <summary>
    /// Хранит неизменяемый снимок параметров игрового запуска.
    /// </summary>
    public readonly struct RunParameters
    {
        public int StartingMoney { get; }
        public int StartingBlood { get; }
        public int MaxBlood { get; }
        public int MoneyGoal { get; }
        public float DurationSeconds { get; }
        public int BloodRegenAmount { get; }
        public float BloodRegenIntervalSeconds { get; }

        /// <summary>
        /// Создаёт набор параметров игрового запуска и проверяет их корректность.
        /// </summary>
        public RunParameters(int startingMoney, int startingBlood, int maxBlood, int moneyGoal, float durationSeconds,
            int bloodRegenAmount = 1, float bloodRegenIntervalSeconds = 5f)
        {

            if (startingMoney < 0)
                throw new ArgumentOutOfRangeException(nameof(startingMoney), "Invalid run parameters.");

            if (maxBlood <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxBlood), "Invalid run parameters.");

            if (startingBlood < 0 || startingBlood > maxBlood)
                throw new ArgumentOutOfRangeException(nameof(startingBlood), "Invalid run parameters.");

            if (moneyGoal <= 0)
                throw new ArgumentOutOfRangeException(nameof(moneyGoal), "Invalid run parameters.");

            if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds) || durationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Invalid run parameters.");

            if (bloodRegenAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(bloodRegenAmount), "Invalid run parameters.");

            if (float.IsNaN(bloodRegenIntervalSeconds) || float.IsInfinity(bloodRegenIntervalSeconds) || bloodRegenIntervalSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(bloodRegenIntervalSeconds), "Invalid run parameters.");

            StartingMoney = startingMoney;
            StartingBlood = startingBlood;
            MaxBlood = maxBlood;
            MoneyGoal = moneyGoal;
            DurationSeconds = durationSeconds;
            BloodRegenAmount = bloodRegenAmount;
            BloodRegenIntervalSeconds = bloodRegenIntervalSeconds;
        }
    }
}
