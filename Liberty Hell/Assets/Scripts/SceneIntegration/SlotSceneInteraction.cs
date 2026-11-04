using LibertyHell.GameLoop;
using LibertyHell.Interaction;
using LibertyHell.SlotMachine;
using LibertyHell.UI;
using UnityEngine;

namespace LibertyHell.SceneIntegration
{
    public sealed class SlotSceneInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private RunBootstrap runBootstrap;
        [SerializeField] private SlotSpinBehaviour spin;
        [SerializeField] private SlotPresentation presentation;
        [SerializeField] private GameUI gameUI;

        private RunState run;
        private long activeSpinId;

        public string ActionName => "Вращать автомат";
        public bool IsAvailable => run != null && spin != null && spin.isActiveAndEnabled &&
            presentation != null && presentation.isActiveAndEnabled && gameUI != null && gameUI.isActiveAndEnabled &&
            !gameUI.IsModalOpen && !presentation.IsSpinning && spin.CanInteract;

        private void Start()
        {
            if (runBootstrap == null || runBootstrap.State == null || spin == null || presentation == null || gameUI == null)
            {
                Debug.LogError("Assign the run, spin, presentation and UI to the slot interaction.", this);
                enabled = false;
                return;
            }
            run = runBootstrap.State;
            run.Changed += OnRunChanged;
            presentation.SpinFinished += OnSpinFinished;
            gameUI.RunRestarted += presentation.ResetDisplay;
            OnRunChanged(run);
        }

        public bool Interact()
        {
            if (!isActiveAndEnabled || !IsAvailable || !spin.TryInteract(out long spinId)) return false;
            activeSpinId = spinId;
            if (!spin.PendingResult.HasValue || !presentation.StartSpin(spin.PendingResult.Value))
            {
                Debug.LogWarning("Slot animation could not start; applying the paid result immediately.", this);
                OnSpinFinished();
            }
            return true;
        }

        private void OnSpinFinished()
        {
            if (activeSpinId == 0) return;
            long spinId = activeSpinId;
            activeSpinId = 0;
            if (spin.TryCompleteSpin(spinId, out AppliedSpinResult result)) gameUI.ShowSpinResult(result);
            else Debug.LogError("The paid spin could not be completed.", this);
        }

        private void OnRunChanged(RunState state)
        {
            if (activeSpinId != 0 && !state.IsSpinInProgress)
            {
                activeSpinId = 0;
                presentation.CancelSpin();
            }
            presentation.SetPaused(state.Status == RunStatus.Paused);
            Time.timeScale = state.Status == RunStatus.Paused ? 0f : 1f;
        }

        private void OnDestroy()
        {
            if (run != null) run.Changed -= OnRunChanged;
            if (presentation != null) presentation.SpinFinished -= OnSpinFinished;
            if (gameUI != null && presentation != null) gameUI.RunRestarted -= presentation.ResetDisplay;
            Time.timeScale = 1f;
        }
    }
}
