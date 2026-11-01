using UnityEngine;

namespace LibertyHell.Interaction
{
    public sealed class InteractionDemoTarget : MonoBehaviour, IInteractable
    {
        [SerializeField] private string actionName = "Проверить взаимодействие";
        [SerializeField] private bool available = true;

        public string ActionName => actionName;
        public bool IsAvailable => available;
        public int InteractionCount { get; private set; }

        /// <summary>
        /// Записывает в консоль факт взаимодействия с тестовым объектом.
        /// </summary>
        /// <returns></returns>
        public bool Interact()
        {
            if (!available)
                return false;

            InteractionCount++;
            Debug.Log($"Взаимодействие с {name}: {InteractionCount}", this);
            return true;
        }
    }
}
