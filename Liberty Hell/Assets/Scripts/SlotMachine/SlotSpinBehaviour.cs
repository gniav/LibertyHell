using LibertyHell.GameLoop;
using UnityEngine;

namespace LibertyHell.SlotMachine
{
    public sealed class SlotSpinBehaviour : MonoBehaviour
    {
        [SerializeField] private RunBootstrap runBootstrap;
        [SerializeField] private SlotSettings settings;
        private SlotSpinController controller;

        public SlotInteractionState State => controller == null
            ? SlotInteractionState.Unavailable : controller.State;
        public bool CanInteract => controller != null && controller.CanInteract;
        public SlotResult? PendingResult => controller?.PendingResult;

        private void Start()
        {
            if (runBootstrap == null || runBootstrap.State == null || settings == null)
            {
                Debug.LogError("Assign an initialized Run Bootstrap and Slot Settings.", this);
                enabled = false;
                return;
            }
            controller = new SlotSpinController(runBootstrap.State,
                new SlotMachine(settings.CreateRules(), new SystemSlotRandom()));
        }

        public bool TryInteract(out long spinId)
        {
            spinId = 0;
            return controller != null && controller.TryInteract(out spinId);
        }

        public bool TryCompleteSpin(long spinId) => controller != null && controller.TryCompleteSpin(spinId);

        private void OnDestroy()
        {
            controller?.Dispose();
            controller = null;
        }
    }
}
