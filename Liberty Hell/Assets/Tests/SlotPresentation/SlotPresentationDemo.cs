using LibertyHell.SlotMachine;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SlotPresentationDemo : MonoBehaviour
{
    [SerializeField] private SlotPresentation presentation;
    private bool paused;

    /// <summary>
    /// Запускает тестовую анимацию или переключает её паузу.
    /// </summary>
    private void Update()
    {
        if (Keyboard.current == null)
            return;
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (presentation.StartSpin(new SlotResult(SlotSymbol.X, SlotSymbol.Y, SlotSymbol.H, 0, 0)))
                paused = false;
        }
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            paused = !paused;
            presentation.SetPaused(paused);
        }
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            paused = false;
            presentation.CancelSpin();
        }
    }

    /// <summary>
    /// Показывает управление тестовой сценой.
    /// </summary>
    private void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 700, 30), "Space: spin | P: pause / resume | C: cancel");
    }
}
