namespace Core.State
{
    /// <summary>
    /// Механика, которая хочет попасть в снапшот сохранения, реализует этот интерфейс
    /// и регистрируется в GameStateService. При сборке снапшота каждый контрибьютор
    /// записывает своё актуальное состояние в GameStateData.
    ///
    /// Пример: PlayerService берёт текущее здоровье/деньги/инвентарь из рантайма и
    /// кладёт в state.player.
    /// </summary>
    public interface IStateContributor
    {
        void CaptureInto(GameStateData state);
    }
}
