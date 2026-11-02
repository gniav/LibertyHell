using LibertyHell.SlotMachine;
using LibertyHell.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameUIDemoControls : MonoBehaviour
{
    [SerializeField] private GameUI gameUI;

    /// <summary>
    /// Открывает или закрывает магазин клавишей M.
    /// </summary>
    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.mKey.wasPressedThisFrame)
            return;

        if (gameUI.IsShopOpen) gameUI.CloseShop();
        else gameUI.OpenShop();
    }

    /// <summary>
    /// Показывает кнопки проверки магазина и сообщения о результате.
    /// </summary>
    private void OnGUI()
    {
        if (gameUI.IsModalOpen)
            return;

        GUILayout.BeginArea(new Rect(20, 160, Mathf.Min(300, Screen.width - 40), 70), GUI.skin.box);
        GUILayout.Label("M — магазин, Escape — пауза");
        if (GUILayout.Button("Показать результат"))
            gameUI.ShowSpinResult(new SlotResult(SlotSymbol.A, SlotSymbol.B, SlotSymbol.A, 15, 0));
        GUILayout.EndArea();
    }
}
