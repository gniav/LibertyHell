using System;

namespace LibertyHell.GameLoop
{
    public enum RunStatus { Running, Paused, Won, Lost }

    // Plain game data, independent of scene objects and UI.
    public sealed class RunState
    {
        public int Money { get; private set; }
        public int Blood { get; private set; }
        private double remainingSeconds;
        private double regenerationProgress;
        public float RemainingSeconds => (float)remainingSeconds;
        public RunStatus Status { get; private set; }
        public RunParameters Parameters { get; private set; }
        public bool IsSpinInProgress { get; private set; }
        public event Action<RunState> Changed;

        public RunState(RunParameters parameters) => Reset(parameters);

        // Resource mutations and new spins are accepted only while Running.
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

        // Begin before paying; apply any payout before EndSpin checks the balance.
        public bool BeginSpin()
        {
            if (Status != RunStatus.Running || IsSpinInProgress)
                return false;
            IsSpinInProgress = true;
            Changed?.Invoke(this);
            return true;
        }

        // Also use this when cancelling, after applying any refund while Running.
        // If paused, the balance is checked on Resume. Terminal states clear the spin.
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

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (Status != RunStatus.Running || deltaTime == 0) return;

            double elapsed = Math.Min((double)deltaTime, remainingSeconds);
            remainingSeconds -= elapsed;
            double accumulated = regenerationProgress + elapsed;
            double intervals = Math.Floor(accumulated / Parameters.BloodRegenIntervalSeconds);
            regenerationProgress = accumulated % Parameters.BloodRegenIntervalSeconds;
            if (Parameters.BloodRegenAmount > 0 && intervals > 0)
            {
                double restored = intervals * Parameters.BloodRegenAmount;
                Blood += (int)Math.Min(restored, Parameters.MaxBlood - Blood);
            }
            EvaluateOutcome();
            Changed?.Invoke(this);
        }

        // Exhausted blood/time takes precedence over the goal, then zero money.
        private void EvaluateOutcome()
        {
            if (Status != RunStatus.Running) return;
            if (Blood == 0 || remainingSeconds <= 0) Status = RunStatus.Lost;
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
            remainingSeconds = parameters.DurationSeconds;
            regenerationProgress = 0;
            Status = RunStatus.Running;
            IsSpinInProgress = false;
            EvaluateOutcome();
            Changed?.Invoke(this);
        }
    }
}
