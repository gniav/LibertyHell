using UnityEngine;

namespace LibertyHell.GameLoop
{
    /// <summary>
    /// Связывает настройки забега с игровой сценой, сздает RunState для текущих статов.
    /// обновляет каждый кадр и предоставляет возможность сбросить забег, выводит текущие показатели.
    /// </summary>
    public sealed class RunBootstrap : MonoBehaviour
    {
        [SerializeField] private RunSettings settings;
        public RunState State { get; private set; }

        /// <summary>При запуске объекта проверяет настройки и создаёт начальное состояние забега.</summary>
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
        /// <summary>Обновляет время, прошедшее с предыдущего кадра.</summary>
        private void Update()
        {
            State?.Tick(Time.deltaTime);
        }

        /// <summary>Сбрасывает текущий забег к начальным значениям.</summary>
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

        /// <summary>Выводит основные показатели и статус текущего забега в Юнити.</summary>
        private void LogState() => Debug.Log(
            $"Run: money={State.Money}, blood={State.Blood}/{State.Parameters.MaxBlood}, " +
            $"goal={State.Parameters.MoneyGoal}, time={State.RemainingSeconds}s, status={State.Status}", this);
    }
}
