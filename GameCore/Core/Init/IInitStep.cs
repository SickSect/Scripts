namespace Core.Init
{
    /// <summary>
    /// Один шаг инициализации сцены. Каждая механика (инвентарь, время, диалоги...)
    /// реализует свой шаг и добавляет его в Initializer — bootstrap при этом не меняется.
    ///
    /// Order задаёт порядок выполнения (меньше — раньше). Так шаги, зависящие от других
    /// (например, UI, которому нужен уже готовый PlayerService), ставятся позже.
    /// </summary>
    public interface IInitStep
    {
        int Order { get; }
        void Execute(InitContext ctx);
    }
}
