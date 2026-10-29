using System;

namespace LibertyHell.GameLoop
{
    // A value snapshot is independent of later changes to the settings asset.
    public readonly struct RunParameters
    {
        public int StartingMoney { get; }
        public int StartingBlood { get; }
        public int MaxBlood { get; }
        public int MoneyGoal { get; }
        public float DurationSeconds { get; }

        public RunParameters(int startingMoney, int startingBlood, int maxBlood, int moneyGoal, float durationSeconds)
        {
            if (startingMoney < 0 || maxBlood <= 0 || startingBlood < 0 || startingBlood > maxBlood ||
                moneyGoal <= 0 || float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds) || durationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(startingMoney), "Invalid run parameters.");
            StartingMoney = startingMoney;
            StartingBlood = startingBlood;
            MaxBlood = maxBlood;
            MoneyGoal = moneyGoal;
            DurationSeconds = durationSeconds;
        }
    }
}
