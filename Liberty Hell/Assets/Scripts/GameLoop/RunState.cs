using System;
using LibertyHell.Items;
using LibertyHell.Shop;
using LibertyHell.SlotMachine;

namespace LibertyHell.GameLoop
{
    public enum RunStatus { Running, Paused, Won, Lost }

    // Plain game data, independent of scene objects and UI.
    public sealed class RunState : IShopEconomy
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
        public ItemInventory Inventory { get; } = new ItemInventory();
        public ShopService Shop { get; }
        private long revision;
        private bool committingPurchase;
        private bool paidSpin;

        public RunState(RunParameters parameters)
        {
            Shop = new ShopService(this, Inventory);
            Reset(parameters);
        }

        private void Publish() { revision++; Changed?.Invoke(this); }
        internal void NotifySpinItemChanges() => Publish();

        ShopEconomySnapshot IShopEconomy.Read() => new ShopEconomySnapshot(revision, Money, Blood,
            Parameters.MaxBlood, SlotSpinController.Stake, Status == RunStatus.Running && !IsSpinInProgress && !committingPurchase);

        bool IShopEconomy.TryCommit(long expectedRevision, int money, int blood, Action grant)
        {
            if (committingPurchase || expectedRevision != revision || Status != RunStatus.Running ||
                IsSpinInProgress || money < 0 || blood <= 0 || blood > Parameters.MaxBlood || grant == null)
                return false;
            committingPurchase = true;
            try { grant(); Money = money; Blood = blood; Publish(); }
            finally { committingPurchase = false; }
            return true;
        }

        // Resource mutations and new spins are accepted only while Running.
        public bool CanSpendMoney(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            return !committingPurchase && Status == RunStatus.Running && amount <= Money;
        }

        public bool TrySpendMoney(int amount)
        {
            if (!CanSpendMoney(amount))
                return false;

            Money -= amount;
            EvaluateOutcome();
            Publish();
            return true;
        }

        public bool TryAddMoney(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (committingPurchase || Status != RunStatus.Running || amount > int.MaxValue - Money)
                return false;
            Money += amount;
            EvaluateOutcome();
            Publish();
            return true;
        }

        // Payment and the deferred balance check become visible together.
        public bool TryBeginPaidSpin(int stake)
        {
            if (stake <= 0) throw new ArgumentOutOfRangeException(nameof(stake));
            if (IsSpinInProgress || !CanSpendMoney(stake)) return false;
            IsSpinInProgress = true;
            paidSpin = true;
            Money -= stake;
            Publish();
            return true;
        }

        // Apply all effects before deciding the outcome or notifying observers.
        public bool TryCompletePaidSpin(int payout, int bloodLoss, int nextStake)
        {
            if (payout < 0) throw new ArgumentOutOfRangeException(nameof(payout));
            if (bloodLoss < 0) throw new ArgumentOutOfRangeException(nameof(bloodLoss));
            if (nextStake <= 0) throw new ArgumentOutOfRangeException(nameof(nextStake));
            if (committingPurchase || Status != RunStatus.Running || !IsSpinInProgress || payout > int.MaxValue - Money)
                return false;

            Money += payout;
            Blood = Math.Max(0, Blood - bloodLoss);
            IsSpinInProgress = false;
            paidSpin = false;
            if (Money >= Parameters.MoneyGoal) Status = RunStatus.Won;
            else if (Blood == 0) Status = RunStatus.Lost;
            else if (remainingSeconds <= 0) Status = RunStatus.Lost;
            else if (Money < nextStake) Status = RunStatus.Lost;
            Publish();
            return true;
        }

        // Begin before paying; apply any payout before EndSpin checks the balance.
        public bool BeginSpin()
        {
            if (committingPurchase || Status != RunStatus.Running || IsSpinInProgress)
                return false;
            IsSpinInProgress = true;
            Publish();
            return true;
        }

        // Also use this when cancelling, after applying any refund while Running.
        // If paused, the balance is checked on Resume. Terminal states clear the spin.
        public bool EndSpin()
        {
            if (committingPurchase || !IsSpinInProgress)
                return false;
            IsSpinInProgress = false;
            paidSpin = false;
            EvaluateOutcome();
            Publish();
            return true;
        }

        public bool Pause()
        {
            if (committingPurchase || Status != RunStatus.Running) return false;
            Status = RunStatus.Paused;
            Publish();
            return true;
        }

        public bool Resume()
        {
            if (committingPurchase || Status != RunStatus.Paused) return false;
            Status = RunStatus.Running;
            EvaluateOutcome();
            Publish();
            return true;
        }

        public bool DamageBlood(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (committingPurchase || Status != RunStatus.Running) return false;
            Blood = Math.Max(0, Blood - amount);
            EvaluateOutcome();
            Publish();
            return true;
        }

        public bool RestoreBlood(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (committingPurchase || Status != RunStatus.Running) return false;
            Blood += Math.Min(amount, Parameters.MaxBlood - Blood);
            Publish();
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (committingPurchase || Status != RunStatus.Running || deltaTime == 0) return;

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
            Publish();
        }

        // Exhausted blood/time takes precedence over the goal, then zero money.
        private void EvaluateOutcome()
        {
            if (Status != RunStatus.Running) return;
            if (paidSpin && IsSpinInProgress) return;
            if (Blood == 0 || remainingSeconds <= 0) Status = RunStatus.Lost;
            else if (Money >= Parameters.MoneyGoal) Status = RunStatus.Won;
            else if (!IsSpinInProgress && Money == 0) Status = RunStatus.Lost;
            if (Status == RunStatus.Lost || Status == RunStatus.Won) IsSpinInProgress = false;
        }

        public void Reset(RunParameters parameters)
        {
            if (committingPurchase) throw new InvalidOperationException("Cannot reset during a purchase.");
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
            paidSpin = false;
            Inventory.ResetForNewRun();
            EvaluateOutcome();
            Publish();
        }
    }
}
