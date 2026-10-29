using UnityEngine;

namespace LibertyHell.GameLoop
{
    public sealed class RunBootstrap : MonoBehaviour
    {
        [SerializeField] private RunSettings settings;
        public RunState State { get; private set; }

        private void Awake()
        {
            if (settings == null)
            {
                Debug.LogError("Assign Run Settings to the Run Bootstrap.", this);
                enabled = false;
                return;
            }
            State = new RunState(settings.CreateParameters());
            LogState();
        }

        [ContextMenu("Reset Run")]
        public void ResetRun()
        {
            if (State == null || settings == null)
            {
                Debug.LogWarning("Enter Play mode with Run Settings assigned before resetting.", this);
                return;
            }
            State.Reset(settings.CreateParameters());
            LogState();
        }

        private void LogState() => Debug.Log(
            $"Run: money={State.Money}, blood={State.Blood}/{State.Parameters.MaxBlood}, " +
            $"goal={State.Parameters.MoneyGoal}, time={State.RemainingSeconds}s, status={State.Status}", this);
    }
}
