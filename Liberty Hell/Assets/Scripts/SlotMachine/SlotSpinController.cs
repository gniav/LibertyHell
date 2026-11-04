using System;
using LibertyHell.GameLoop;
using LibertyHell.Items;

namespace LibertyHell.SlotMachine
{
    public enum SlotInteractionState { Ready, Spinning, Applying, Unavailable }

    // Main-thread coordinator; RunState owns resources and terminal outcomes.
    public sealed class SlotSpinController : IDisposable
    {
        public const int Stake = 10;
        private readonly RunState run;
        private readonly Func<SlotResult> generateResult;
        private readonly SlotMachine machine;
        private ItemEffects spinEffects;
        private SlotInteractionState phase = SlotInteractionState.Ready;
        private SlotResult? pendingResult;
        private long nextSpinId;
        private long activeSpinId;
        private bool disposed;

        public SlotSpinController(RunState run, SlotMachine machine)
            : this(run, machine == null ? throw new ArgumentNullException(nameof(machine)) : (Func<SlotResult>)machine.Spin)
        { this.machine = machine; }

        public SlotSpinController(RunState run, Func<SlotResult> generateResult)
        {
            this.run = run ?? throw new ArgumentNullException(nameof(run));
            this.generateResult = generateResult ?? throw new ArgumentNullException(nameof(generateResult));
            run.Changed += OnRunChanged;
        }

        public SlotInteractionState State => disposed || run.Status != RunStatus.Running
            ? SlotInteractionState.Unavailable : phase;
        public bool CanInteract
        {
            get
            {
                var effects = run.Inventory.CurrentEffects;
                return State == SlotInteractionState.Ready && !run.IsSpinInProgress &&
                    (machine != null || (!effects.HasLure && !effects.GuaranteedXXX)) &&
                    run.CanSpendMoney(effects.GetEffectiveBetCost(Stake));
            }
        }
        public SlotResult? PendingResult => phase == SlotInteractionState.Spinning ? pendingResult : null;

        public bool TryInteract(out long spinId)
        {
            spinId = 0;
            if (!CanInteract) return false;
            phase = SlotInteractionState.Applying; // Blocks reentry during payment and generation.
            int price = run.Inventory.CurrentEffects.GetEffectiveBetCost(Stake);
            if (!run.TryBeginPaidSpin(price))
            {
                phase = SlotInteractionState.Ready;
                return false;
            }
            if (disposed || (run.Status != RunStatus.Running && run.Status != RunStatus.Paused) || !run.IsSpinInProgress)
            {
                phase = SlotInteractionState.Ready;
                if (run.IsSpinInProgress) run.EndSpin();
                return false;
            }

            try
            {
                spinEffects = run.Inventory.ConfirmSpinStarted(Guid.NewGuid());
                if (spinEffects.GuaranteedXXX)
                {
                    run.NotifySpinItemChanges();
                    if (disposed || (run.Status != RunStatus.Running && run.Status != RunStatus.Paused) || !run.IsSpinInProgress)
                    {
                        phase = SlotInteractionState.Ready;
                        if (run.IsSpinInProgress) run.EndSpin();
                        return false;
                    }
                }
                var result = machine != null ? machine.Spin(spinEffects) : generateResult();
                if (result.MoneyPayout < 0 || result.BloodLoss < 0)
                    throw new InvalidOperationException("The spin result contains negative effects.");
                if (disposed || (run.Status != RunStatus.Running && run.Status != RunStatus.Paused) || !run.IsSpinInProgress)
                {
                    phase = SlotInteractionState.Ready;
                    if (run.IsSpinInProgress) run.EndSpin();
                    return false;
                }
                pendingResult = result;
                activeSpinId = ++nextSpinId;
                spinId = activeSpinId;
                phase = SlotInteractionState.Spinning;
                return true;
            }
            catch
            {
                phase = SlotInteractionState.Ready;
                if (run.IsSpinInProgress) run.EndSpin();
                throw;
            }
        }

        public bool TryCompleteSpin(long spinId) => TryCompleteSpin(spinId, out _);

        public bool TryCompleteSpin(long spinId, out AppliedSpinResult appliedResult)
        {
            appliedResult = default;
            if (disposed || phase != SlotInteractionState.Spinning || spinId != activeSpinId ||
                run.Status != RunStatus.Running || !run.IsSpinInProgress || !pendingResult.HasValue)
                return false;
            var result = pendingResult.Value;
            int bloodLoss = spinEffects.GetEffectiveLosingBloodLoss(result.BloodLoss);
            int bloodLost = Math.Min(run.Blood, bloodLoss);
            phase = SlotInteractionState.Applying;
            if (!run.TryCompletePaidSpin(result.MoneyPayout, bloodLoss,
                run.Inventory.CurrentEffects.GetEffectiveBetCost(Stake)))
            {
                phase = SlotInteractionState.Spinning;
                return false;
            }
            appliedResult = new AppliedSpinResult(result, bloodLost);
            pendingResult = null;
            activeSpinId = 0;
            phase = SlotInteractionState.Ready;
            return true;
        }

        private void OnRunChanged(RunState state)
        {
            if (phase == SlotInteractionState.Spinning && !state.IsSpinInProgress)
            {
                pendingResult = null;
                activeSpinId = 0;
                phase = SlotInteractionState.Ready;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            run.Changed -= OnRunChanged;
            pendingResult = null;
            activeSpinId = 0;
            if (phase == SlotInteractionState.Spinning && run.IsSpinInProgress) run.EndSpin();
            phase = SlotInteractionState.Ready;
        }
    }
}
