namespace Core.Interaction
{
    /// <summary>
    /// Объект, который умеет подсвечиваться при наведении прицела.
    /// Включает/выключает свою подсветку сам, ничего не зная про игрока.
    /// </summary>
    public interface IHighlightable
    {
        void SetHighlight(bool on);
    }
}
