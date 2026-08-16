namespace Core.Signals
{
    /// <summary>
    /// Теги ядровых сигналов перехода между сценами.
    /// Механики (инвентарь, время, диалоги и т.д.) добавляют свои теги отдельно.
    /// </summary>
    public static class CoreSignals
    {
        // Из меню
        public const string NEW_GAME  = "CORE_NEW_GAME";
        public const string LOAD_GAME = "CORE_LOAD_GAME";
        public const string EXIT_GAME = "CORE_EXIT_GAME";

        // Из геймплея
        public const string EXIT_TO_MENU = "CORE_EXIT_TO_MENU"; // выйти в меню
        public const string TRANSITION   = "CORE_TRANSITION";   // переход на другую игровую сцену
        public const string SAVE_GAME    = "CORE_SAVE_GAME";
        public const string PAUSE_GAME   = "CORE_PAUSE_GAME";
        public const string CONTINUE_GAME = "CORE_CONTINUE_GAME";
    }
}
