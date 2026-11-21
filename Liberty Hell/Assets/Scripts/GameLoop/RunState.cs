using System;
using LibertyHell.Items;
using LibertyHell.Shop;
using LibertyHell.SlotMachine;

namespace LibertyHell.GameLoop
{
    public enum RunStatus { Running, Paused, Won, Lost }

    /// <summary>
    /// Хранит основные данные игровой сессии независимо от объектов сцены и интерфейса.
    /// </summary>
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


        /// <summary>
        /// Создаёт состояние игровой сессии с заданными параметрами.
        /// </summary>
        /// <param name="parameters">Параметры новой игровой сессии.</param>
        public RunState(RunParameters parameters)
        {
            Shop = new ShopService(this, Inventory);
            Reset(parameters);
        }
        /// <summary>
        /// Увеличивает версию состояния и уведомляет подписчиков об изменениях.
        /// </summary>
        private void Publish() { revision++; Changed?.Invoke(this); }
        
        /// <summary>
        /// Уведомляет подписчиков об изменении предметов во время вращения.
        /// </summary>
        internal void NotifySpinItemChanges() => Publish();

        /// <summary>
        /// Возвращает снимок текущего состояния экономики для магазина.
        /// </summary>
        /// <returns>Снимок состояния экономики.</returns>
        ShopEconomySnapshot IShopEconomy.Read() => new ShopEconomySnapshot(revision, Money, Blood,
            Parameters.MaxBlood, SlotSpinController.Stake, Status == RunStatus.Running && !IsSpinInProgress && !committingPurchase);
 
        /// <summary>
        /// Проверяет возможность покупки и применяет её, если состояние экономики не изменилось.
        /// </summary>
        /// <param name="expectedRevision">Ожидаемая версия состояния.</param>
        /// <param name="money">Количество денег после покупки.</param>
        /// <param name="blood">Количество крови после покупки.</param>
        /// <param name="grant">Действие, выдающее купленный предмет.</param>
        /// <returns>True, если покупка успешно выполнена.</returns> 
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

        /// <summary>
        /// Проверяет, можно ли потратить указанное количество денег.
        /// </summary>
        /// <param name="amount">Количество денег для траты.</param>
        /// <returns>True, если денег достаточно и игровая сессия активна.</returns>
        public bool CanSpendMoney(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            return !committingPurchase && Status == RunStatus.Running && amount <= Money;
        }


        /// <summary>
        /// Списывает указанное количество денег, если это возможно.
        /// </summary>
        /// <param name="amount">Количество денег для списания.</param>
        /// <returns>True, если деньги успешно списаны.</returns>
        public bool TrySpendMoney(int amount)
        {
            if (!CanSpendMoney(amount))
                return false;

            Money -= amount;
            EvaluateOutcome();
            Publish();
            return true;
        }


        /// <summary>
        /// Добавляет деньги, если сессия находится в активном состоянии и не происходит переполнения.
        /// </summary>
        /// <param name="amount">Количество добавляемых денег.</param>
        /// <returns>True, если деньги успешно добавлены.</returns>
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

        /// <summary>
        /// Начинает платное вращение и списывает ставку.
        /// </summary>
        /// <param name="stake">Размер ставки.</param>
        /// <returns>True, если вращение успешно начато.</returns>
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

        /// <summary>
        /// Завершает платное вращение, начисляет выигрыш и применяет потерю крови.
        /// </summary>
        /// <param name="payout">Количество денег, полученных за вращение.</param>
        /// <param name="bloodLoss">Количество потерянной крови.</param>
        /// <param name="nextStake">Размер следующей ставки.</param>
        /// <returns>True, если вращение успешно завершено.</returns>
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

        /// <summary>
        /// Начинает обычное вращение без предварительного списания денег.
        /// </summary>
        /// <returns>True, если вращение успешно начато.</returns>
        public bool BeginSpin()
        {
            if (committingPurchase || Status != RunStatus.Running || IsSpinInProgress)
                return false;
            IsSpinInProgress = true;
            Publish();
            return true;
        }

        /// <summary>
        /// Завершает текущее вращение и проверяет условия окончания сессии.
        /// </summary>
        /// <returns>True, если вращение успешно завершено.</returns>
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

        /// <summary>
        /// Приостанавливает игровую сессию.
        /// </summary>
        /// <returns>True, если игровая сессия успешно приостановлена.</returns>
        public bool Pause()
        {
            if (committingPurchase || Status != RunStatus.Running) return false;
            Status = RunStatus.Paused;
            Publish();
            return true;
        }

        /// <summary>
        /// Возобновляет игровую сессию и проверяет условия её завершения.
        /// </summary>
        /// <returns>True, если игровая сессия успешно возобновлена.</returns>
        public bool Resume()
        {
            if (committingPurchase || Status != RunStatus.Paused) return false;
            Status = RunStatus.Running;
            EvaluateOutcome();
            Publish();
            return true;
        }

        /// <summary>
        /// Уменьшает количество крови и проверяет условия окончания игровой сессии.
        /// </summary>
        /// <param name="amount">Количество теряемой крови.</param>
        /// <returns>True, если урон успешно применён.</returns>
        public bool DamageBlood(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (committingPurchase || Status != RunStatus.Running) return false;
            Blood = Math.Max(0, Blood - amount);
            EvaluateOutcome();
            Publish();
            return true;
        }

        /// <summary>
        /// Восстанавливает кровь, не превышая её максимальное количество.
        /// </summary>
        /// <param name="amount">Количество восстанавливаемой крови.</param>
        /// <returns>True, если восстановление выполнено.</returns>
        public bool RestoreBlood(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (committingPurchase || Status != RunStatus.Running) return false;
            Blood += Math.Min(amount, Parameters.MaxBlood - Blood);
            Publish();
            return true;
        }

        /// <summary>
        /// Обновляет таймер игровой сессии и выполняет автоматическое восстановление крови.
        /// </summary>
        /// <param name="deltaTime">Время, прошедшее с предыдущего обновления.</param>
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

        /// <summary>
        /// Проверяет условия победы и поражения и обновляет статус игровой сессии.
        /// </summary>
        private void EvaluateOutcome()
        {
            if (Status != RunStatus.Running) return;
            if (paidSpin && IsSpinInProgress) return;
            if (Blood == 0 || remainingSeconds <= 0) Status = RunStatus.Lost;
            else if (Money >= Parameters.MoneyGoal) Status = RunStatus.Won;
            else if (!IsSpinInProgress && Money == 0) Status = RunStatus.Lost;
            if (Status == RunStatus.Lost || Status == RunStatus.Won) IsSpinInProgress = false;
        }

         /// <summary>
        /// Сбрасывает состояние игровой сессии и применяет новые параметры.
        /// </summary>
        /// <param name="parameters">Параметры новой сессии.</param>
        public void Reset(RunParameters parameters)
        {
            if (committingPurchase) throw new InvalidOperationException("Cannot reset during a purchase.");
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
