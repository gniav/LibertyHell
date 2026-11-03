using LibertyHell.Interaction;
using UnityEngine;

public sealed class TestInteractionTarget : MonoBehaviour, IInteractable
{
    public string ActionName => "Тестовое действие";
    public bool IsAvailable { get; set; } = true;
    public int InteractionCount { get; private set; }

    /// <summary>
    /// Запоминает вызов взаимодействия для проверки.
    /// </summary>
    /// <returns></returns>
    public bool Interact()
    {
        InteractionCount++;
        return true;
    }
}
