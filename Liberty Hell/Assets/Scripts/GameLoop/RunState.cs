using System;

namespace LibertyHell.GameLoop
{
    public enum RunStatus { Running }

    // Plain game data, independent of scene objects and UI.
    public sealed class RunState
    {
        public int Money { get; private set; }
        public int Blood { get; private set; }
        public float RemainingSeconds { get; private set; }
        public RunStatus Status { get; private set; }
        public RunParameters Parameters { get; private set; }
        public event Action<RunState> Changed;

        public RunState(RunParameters parameters) => Reset(parameters);

        public void Reset(RunParameters parameters)
        {
            // A default struct bypasses its constructor, so validate here too.
            parameters = new RunParameters(parameters.StartingMoney, parameters.StartingBlood,
                parameters.MaxBlood, parameters.MoneyGoal, parameters.DurationSeconds);
            Parameters = parameters;
            Money = parameters.StartingMoney;
            Blood = parameters.StartingBlood;
            RemainingSeconds = parameters.DurationSeconds;
            Status = RunStatus.Running;
            Changed?.Invoke(this);
        }
    }
}
