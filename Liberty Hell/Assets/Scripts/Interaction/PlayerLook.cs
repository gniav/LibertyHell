using UnityEngine;
using UnityEngine.InputSystem;

namespace LibertyHell.Interaction
{
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.1f;
        [SerializeField, Range(-89f, 0f)] private float minPitch = -80f;
        [SerializeField, Range(0f, 89f)] private float maxPitch = 80f;

        private float yaw;
        private float pitch;

        public bool InputEnabled { get; set; } = true;

        /// <summary>
        /// Запоминает начальное направление камеры.
        /// </summary>
        private void Awake()
        {
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = Mathf.DeltaAngle(0f, angles.x);
        }

        /// <summary>
        /// Передаёт движение мыши для поворота камеры.
        /// </summary>
        private void Update()
        {
            if (Mouse.current != null)
                ApplyLook(Mouse.current.delta.ReadValue());
        }

        /// <summary>
        /// Поворачивает камеру и ограничивает вертикальный угол.
        /// </summary>
        /// <param name="delta"></param>
        public void ApplyLook(Vector2 delta)
        {
            if (!InputEnabled || !isActiveAndEnabled)
                return;

            yaw += delta.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
