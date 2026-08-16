namespace Core.Interaction
{
    /// <summary>
    /// Всё, с чем игрок может взаимодействовать через прицел (двери, предметы, рычаги).
    /// PlayerInteractor вызывает Interact(), когда игрок нажал Interact, глядя на объект.
    ///
    /// Новый вид взаимодействия = новый компонент с этим интерфейсом. Интерактор не меняется.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Короткая подсказка ("Открыть", "Взять") — для UI-хинта. Может быть null.</summary>
        string Prompt { get; }

        /// <summary>Сработать. context — кто взаимодействует.</summary>
        void Interact(InteractionContext context);
    }
}
