using System;
using UnityEngine;

namespace LibertyHell.SlotMachine
{
    public sealed class SlotPresentation : MonoBehaviour
    {
        [SerializeField] private TextMesh firstReel;
        [SerializeField] private TextMesh secondReel;
        [SerializeField] private TextMesh thirdReel;
        [SerializeField, Min(0.1f)] private float spinDuration = 1.5f;

        private SlotResult result;
        private float elapsedTime;
        private bool paused;
        private bool spinning;
        private const string Symbols = "ABXYH";

        public bool IsSpinning => spinning;
        public event Action SpinFinished;

        /// <summary>
        /// Запускает отображение заранее рассчитанного результата.
        /// </summary>
        /// <param name="spinResult"></param>
        /// <returns></returns>
        public bool StartSpin(SlotResult spinResult)
        {
            if (spinning || firstReel == null || secondReel == null || thirdReel == null)
                return false;

            result = spinResult;
            elapsedTime = 0f;
            paused = false;
            spinning = true;
            return true;
        }

        /// <summary>
        /// Приостанавливает или продолжает анимацию.
        /// </summary>
        /// <param name="isPaused"></param>
        public void SetPaused(bool isPaused)
        {
            paused = isPaused;
        }

        /// <summary>
        /// Отменяет анимацию без события завершения.
        /// </summary>
        public void CancelSpin()
        {
            spinning = false;
            paused = false;
        }

        /// <summary>
        /// Меняет временные символы и показывает результат по окончании вращения.
        /// </summary>
        private void Update()
        {
            if (!spinning || paused)
                return;

            elapsedTime += Time.deltaTime;
            if (elapsedTime >= spinDuration)
            {
                firstReel.text = result.First.ToString();
                secondReel.text = result.Second.ToString();
                thirdReel.text = result.Third.ToString();
                spinning = false;
                SpinFinished?.Invoke();
                return;
            }

            int frame = (int)(elapsedTime * 12f);
            firstReel.text = Symbols[frame % Symbols.Length].ToString();
            secondReel.text = Symbols[(frame + 1) % Symbols.Length].ToString();
            thirdReel.text = Symbols[(frame + 2) % Symbols.Length].ToString();
        }

        /// <summary>
        /// Отменяет незавершённое вращение при отключении объекта.
        /// </summary>
        private void OnDisable()
        {
            CancelSpin();
        }
    }
}
