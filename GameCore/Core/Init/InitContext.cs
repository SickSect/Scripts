using Core.DI;
using Core.State;

namespace Core.Init
{
    /// <summary>
    /// Контекст, который получает каждый шаг инициализации.
    /// Через него шаг достаёт зависимости (DI) и данные снапшота, которые нужно применить к сцене.
    /// </summary>
    public class InitContext
    {
        /// <summary>Root-контейнер (переживает сцены): сервисы, сигналы, реестры.</summary>
        public readonly DIContainer Root;

        /// <summary>Scene-контейнер (создаётся заново на каждую сцену): сюда регистрируются пер-сценовые штуки.</summary>
        public readonly DIContainer Scene;

        /// <summary>Параметры перехода, с которыми пришли на эту сцену (снапшот, spawn и т.д.).</summary>
        public readonly SceneTransitionParameters Parameters;

        public GameStateData State => Parameters?.gameState;

        public InitContext(DIContainer root, DIContainer scene, SceneTransitionParameters parameters)
        {
            Root = root;
            Scene = scene;
            Parameters = parameters;
        }
    }
}
