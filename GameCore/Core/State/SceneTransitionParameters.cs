namespace Core.State
{
    /// <summary>
    /// Пакет данных, который передаётся МЕЖДУ сценами: и внутрь bootstrap-а сцены
    /// (что загружать), и наружу из него (какой переход запросил игрок).
    /// Это единственный канал передачи данных при смене сцен.
    /// </summary>
    public class SceneTransitionParameters
    {
        /// <summary>Тег сигнала перехода (см. Core.Signals.CoreSignals).</summary>
        public string signal;

        /// <summary>Текущий снапшот состояния игры (переносится между сценами).</summary>
        public GameStateData gameState;

        /// <summary>Куда переходим.</summary>
        public string nextSceneName;

        /// <summary>На какой точке спавна появиться на следующей сцене.</summary>
        public int nextSpawnId;

        /// <summary>Слот сохранения (для NEW/LOAD из меню).</summary>
        public int saveSlotId;
    }
}
