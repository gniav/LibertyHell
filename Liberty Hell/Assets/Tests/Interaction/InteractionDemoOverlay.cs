using LibertyHell.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InteractionDemoOverlay : MonoBehaviour
{
    [SerializeField] private PlayerInteraction player;

    /// <summary>
    /// Переключает управление и курсор для проверки паузы или открытого магазина.
    /// </summary>
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            player.SetInputEnabled(!player.InputEnabled);
    }

    /// <summary>
    /// Показывает подсказку тестовой сцены и доступное действие.
    /// </summary>
    private void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 600, 25), "Mouse: look | E: interact | Esc: release/lock cursor");
        string action = player.CurrentTarget == null ? "No target" : player.CurrentTarget.ActionName;
        GUI.Label(new Rect(20, 45, 600, 25), $"Target: {action}");
    }
}
