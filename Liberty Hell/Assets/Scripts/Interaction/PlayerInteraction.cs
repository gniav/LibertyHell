using UnityEngine;
using UnityEngine.InputSystem;

namespace LibertyHell.Interaction
{
    [RequireComponent(typeof(Camera), typeof(PlayerLook))]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private Camera playerCamera;
        private PlayerLook playerLook;
        private bool hasFocus = true;
        private CursorLockMode previousLockMode;
        private bool previousCursorVisible;

        public IInteractable CurrentTarget { get; private set; }
        public bool InputEnabled { get; private set; } = true;

        /// <summary>
        /// Находит камеру и компонент обзора.
        /// </summary>
        private void Awake()
        {
            playerCamera = GetComponent<Camera>();
            playerLook = GetComponent<PlayerLook>();
        }

        /// <summary>
        /// Сохраняет состояние курсора и включает управление.
        /// </summary>
        private void OnEnable()
        {
            previousLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            UpdateControls();
        }

        /// <summary>
        /// Находит объект под прицелом и обрабатывает нажатие E.
        /// </summary>
        private void Update()
        {
            if (!CanUseInput())
                return;

            RefreshTarget();

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                TryInteract();
        }

        /// <summary>
        /// Включает или отключает управление для паузы или магазина.
        /// </summary>
        /// <param name="enabled"></param>
        public void SetInputEnabled(bool enabled)
        {
            InputEnabled = enabled;

            if (!enabled)
                CurrentTarget = null;

            if (isActiveAndEnabled)
                UpdateControls();
        }

        /// <summary>
        /// Находит ближайший объект под прицелом.
        /// </summary>
        public void RefreshTarget()
        {
            CurrentTarget = null;

            if (!CanUseInput() || playerCamera == null)
                return;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Collide))
                return;

            foreach (MonoBehaviour component in hit.collider.GetComponentsInParent<MonoBehaviour>())
            {
                IInteractable target = component as IInteractable;
                if (target != null && component.isActiveAndEnabled)
                {
                    CurrentTarget = target;
                    return;
                }
            }
        }

        /// <summary>
        /// Выполняет доступное действие объекта под прицелом.
        /// </summary>
        /// <returns></returns>
        public bool TryInteract()
        {
            if (!CanUseInput())
                return false;

            MonoBehaviour target = CurrentTarget as MonoBehaviour;
            if (target == null || !target.isActiveAndEnabled || !CurrentTarget.IsAvailable)
                return false;

            return CurrentTarget.Interact();
        }

        /// <summary>
        /// Отключает управление при потере фокуса окна.
        /// </summary>
        /// <param name="focused"></param>
        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;

            if (!focused)
                CurrentTarget = null;

            if (isActiveAndEnabled)
                UpdateControls();
        }

        /// <summary>
        /// Отключает обзор и восстанавливает прежнее состояние курсора.
        /// </summary>
        private void OnDisable()
        {
            CurrentTarget = null;
            playerLook.InputEnabled = false;
            Cursor.lockState = previousLockMode;
            Cursor.visible = previousCursorVisible;
        }

        /// <summary>
        /// Проверяет, можно ли сейчас управлять игроком.
        /// </summary>
        /// <returns></returns>
        private bool CanUseInput()
        {
            return InputEnabled && hasFocus && isActiveAndEnabled;
        }

        /// <summary>
        /// Обновляет обзор и курсор в зависимости от доступности управления.
        /// </summary>
        private void UpdateControls()
        {
            bool canUseInput = CanUseInput();
            playerLook.InputEnabled = canUseInput;

            if (canUseInput)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
