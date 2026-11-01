namespace LibertyHell.Interaction
{
    public interface IInteractable
    {
        string ActionName { get; }
        bool IsAvailable { get; }

        /// <summary>
        /// Выполняет действие и сообщает, удалось ли его начать.
        /// </summary>
        /// <returns></returns>
        bool Interact();
    }
}
