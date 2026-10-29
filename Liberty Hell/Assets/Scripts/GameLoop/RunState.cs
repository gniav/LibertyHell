using System;

namespace LibertyHell.GameLoop
{
    public enum RunStatus { Running, Paused, Won, Lost }

    // Plain game data, independent of scene objects and UI.
    public sealed class RunState
    {
        public int Money { get; private set; }
        public int Blood { get; private set; }
        public float RemainingSeconds { get; private set; }
        public RunStatus Status { get; private set; }
        public RunParameters Parameters { get; private set; }
        public bool IsSpinInProgress { get; private set; }
        public event Action<RunState> Changed;

        public RunState(RunParameters parameters) => Reset(parameters);

        public bool CanSpendMoney(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            return Status == RunStatus.Running && amount <= Money;
        }

        public bool TrySpendMoney(int amount)
        {
            if (!CanSpendMoney(amount))
                return false;

            Money -= amount;
            EvaluateOutcome();
            Changed?.Invoke(this);
            return true;
        }

        public bool TryAddMoney(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (Status != RunStatus.Running || amount > int.MaxValue - Money)
                return false;
            Money += amount;
            EvaluateOutcome();
            Changed?.Invoke(this);
            return true;
        }

        public bool BeginSpin()
        {
            if (Status != RunStatus.Running || IsSpinInProgress)
                return false;
            IsSpinInProgress = true;
            Changed?.Invoke(this);
            return true;
        }

        // Also use this when cancelling a spin, after applying any refund.
        public bool EndSpin()
        {
            if (!IsSpinInProgress)
                return false;
            IsSpinInProgress = false;
            EvaluateOutcome();
            Changed?.Invoke(this);
            return true;
        }

        public bool Pause()
        {
            if (Status != RunStatus.Running) return false;
            Status = RunStatus.Paused;
            Changed?.Invoke(this);
            return true;
        }

        public bool Resume()
        {
            if (Status != RunStatus.Paused) return false;
            Status = RunStatus.Running;
            EvaluateOutcome();
            Changed?.Invoke(this);
            return true;
        }

        public bool DamageBlood(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (Status != RunStatus.Running) return false;
            Blood = Math.Max(0, Blood - amount);
            EvaluateOutcome();
            Changed?.Invoke(this);
            return true;
        }

        public bool RestoreBlood(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (Status != RunStatus.Running) return false;
            Blood += Math.Min(amount, Parameters.MaxBlood - Blood);
            Changed?.Invoke(this);
            return true;
        }

        private void EvaluateOutcome()
        {
            if (Status != RunStatus.Running) return;
            if (Blood == 0 || RemainingSeconds <= 0) Status = RunStatus.Lost;
            else if (Money >= Parameters.MoneyGoal) Status = RunStatus.Won;
            else if (!IsSpinInProgress && Money == 0) Status = RunStatus.Lost;
            if (Status == RunStatus.Lost || Status == RunStatus.Won) IsSpinInProgress = false;
        }

        public void Reset(RunParameters parameters)
        {
            // A default struct bypasses its constructor, so validate here too.
            parameters = new RunParameters(parameters.StartingMoney, parameters.StartingBlood,
                parameters.MaxBlood, parameters.MoneyGoal, parameters.DurationSeconds,
                parameters.BloodRegenAmount, parameters.BloodRegenIntervalSeconds);
            Parameters = parameters;
            Money = parameters.StartingMoney;
            Blood = parameters.StartingBlood;
            RemainingSeconds = parameters.DurationSeconds;
            Status = RunStatus.Running;
            IsSpinInProgress = false;
            EvaluateOutcome();
            Changed?.Invoke(this);
        }
    }
}
