using Core.Init;
using Core.Signals;
using Core.State;
using R3;
using UnityEngine;

namespace Core.Boot
{
    /// <summary>
    /// Bootstrap сцены меню. Инициализирует то, что нужно меню (через Initializer),
    /// и возвращает объединённый поток сигналов "новая игра / загрузка / выход".
    ///
    /// Кнопки меню должны пушить в соответствующие Subject-сигналы из root-контейнера.
    /// Здесь ядро НЕ знает про конкретный UI — это зона механик.
    /// </summary>
    public class MainMenuBootstrap : SceneBootstrapBase
    {
        public override Observable<SceneTransitionParameters> Initialize(InitContext ctx)
        {
            // Пауза в меню не нужна.
            if (ctx.Root.TryResolve<Core.UI.PauseController>(out var pause))
                pause.SetAvailable(false);
            if (ctx.Root.TryResolve<Core.Inventory.UI.InventoryUIController>(out var invUI))
                invUI.SetAvailable(false);

            // Расширяемая инициализация меню.
            // Добавляй шаги под свои нужды (например, привязка UI меню к сигналам).
            var initializer = new Initializer();
            initializer.Add(new MainMenuInitStep());
            initializer.Run(ctx);

            var newGame  = ctx.Root.Resolve<Subject<SceneTransitionParameters>>(CoreSignals.NEW_GAME);
            var loadGame = ctx.Root.Resolve<Subject<SceneTransitionParameters>>(CoreSignals.LOAD_GAME);
            var exitGame = ctx.Root.Resolve<Subject<SceneTransitionParameters>>(CoreSignals.EXIT_GAME);

            // Проставляем тег сигнала, чтобы GameBootstrap понял намерение.
            var newGameStream  = newGame.Select(p  => Tag(p, CoreSignals.NEW_GAME));
            var loadGameStream = loadGame.Select(p => Tag(p, CoreSignals.LOAD_GAME));
            var exitGameStream = exitGame.Select(p => Tag(p, CoreSignals.EXIT_GAME));

            return newGameStream.Merge(loadGameStream).Merge(exitGameStream);
        }

        private static SceneTransitionParameters Tag(SceneTransitionParameters p, string signal)
        {
            p ??= new SceneTransitionParameters();
            p.signal = signal;
            return p;
        }
    }
}
