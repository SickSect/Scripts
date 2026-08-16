using System.Collections.Generic;
using Core.Init;
using Core.Player;
using Core.Signals;
using Core.State;
using R3;
using UnityEngine;

namespace Core.Boot
{
    /// <summary>
    /// Bootstrap игровой сцены. Вся инициализация вынесена в Initializer/IInitStep:
    /// чтобы подключить новую механику, ты просто добавляешь её шаг в BuildInitializer()
    /// (или регистрируешь список шагов извне) — тело bootstrap-а трогать не нужно.
    /// 
    /// Поддерживает два режима:
    /// 1) Классический режим с игроком (ThirdPerson/FirstPerson) - спавнится префаб игрока
    /// 2) Point-and-click режим - игрок не спавнится, управление камерой и мышью напрямую
    /// 
    /// Наружу возвращает поток сигналов перехода (выход в меню / переход на сцену / выход).
    /// </summary>
    public class GameplayBootstrap : SceneBootstrapBase
    {
        [Header("Player (для классического режима)")]
        [SerializeField] private PlayerMovement _playerPrefab;
        [SerializeField] private Core.Stats.StatDefinition _staminaStat; // стат выносливости для движения
        
        [Header("Point-and-Click режим")]
        [Tooltip("Если true, игрок не спавнится, используется камера с управлением мышью")]
        [SerializeField] private bool _pointAndClickMode = false;
        [SerializeField] private Camera _pointAndClickCameraPrefab; // опционально, можно использовать камеру сцены

        // Объекты, которые этот bootstrap создал и должен убрать при уничтожении сцены.
        private readonly List<GameObject> _spawned = new();

        private Subject<SceneTransitionParameters> _exitToMenu;
        private Subject<SceneTransitionParameters> _transition;
        private Subject<SceneTransitionParameters> _exitGame;

        private GameStateService _stateService;
        private readonly CompositeDisposable _sceneDisposables = new();

        public override Observable<SceneTransitionParameters> Initialize(InitContext ctx)
        {
            _stateService = ctx.Root.Resolve<GameStateService>();
            _stateService.SetState(ctx.State); // загрузили снапшот в рантайм

            // Включаем паузу на игровой сцене.
            if (ctx.Root.TryResolve<Core.UI.PauseController>(out var pause))
                pause.SetAvailable(true);
            if (ctx.Root.TryResolve<Core.Inventory.UI.InventoryUIController>(out var invUI))
                invUI.SetAvailable(true);

            BindCoreSignals(ctx);

            // === РАСШИРЯЕМЫЙ МЕХАНИЗМ ИНИЦИАЛИЗАЦИИ ===
            BuildInitializer(ctx).Run(ctx);

            return BuildTransitionStream();
        }

        private Initializer BuildInitializer(InitContext ctx)
        {
            var initializer = new Initializer();

            // Ядровой шаг — привязка спавнов текущей сцены (нужен всем, кто спавнит объекты).
            initializer.Add(new BindSpawnsInitStep());

            // Метки состояния: загрузка + регистрация в сохранении (рано, Order 5).
            initializer.Add(new Core.Flags.FlagInitStep());
            // Статы (здоровье/выносливость/рассудок), Order 7.
            initializer.Add(new Core.Stats.StatsInitStep());
            // Собранные предметы / условные объекты (Order 6).
            initializer.Add(new Core.Story.WorldObjectsInitStep());
            // Зональные сюжетные события (Order 8).
            initializer.Add(new Core.Story.StoryEventInitStep());
            initializer.Add(new Core.Mail.MailInitStep());
            initializer.Add(new Core.Mail.UI.MailUIBridge());
            // Фазы: активация текущей фазы на сцене (спавн наполнения), Order 9.
            initializer.Add(new Core.Story.Phases.PhaseInitStep());

            // Выбор режима инициализации: классический игрок или point-and-click
            if (_pointAndClickMode)
            {
                // Point-and-click режим: без игрока, управление камерой и мышью
                Debug.Log("[GameplayBootstrap] Инициализация point-and-click режима");
                
                var input = ctx.Root.Resolve<Core.Input.GameInput>();
                
                // Инициализация взаимодействия через клик мыши
                initializer.Add(new ClickInteractionInitStep(input));
                
                // Камера для point-and-click (если назначена или нужно создать)
                if (_pointAndClickCameraPrefab != null)
                {
                    var cameraGo = Instantiate(_pointAndClickCameraPrefab.gameObject);
                    _spawned.Add(cameraGo);
                    DontDestroyOnLoad(cameraGo);
                }
            }
            else
            {
                // Классический режим с игроком
                Debug.Log("[GameplayBootstrap] Инициализация классического режима с игроком");
                
                var input = ctx.Root.Resolve<Core.Input.GameInput>();
                initializer.Add(new PlayerInitStep(_playerPrefab, input.Actions.Player.Move, input.Actions.Player.Look, input.Actions.Player.Interact, _staminaStat));

                // Камера: привязка Cinemachine к игроку (после спавна игрока).
                initializer.Add(new CameraInitStep());
            }

            // Инвентарь: загрузка из снапшота + регистрация в сохранении.
            initializer.Add(new Core.Inventory.InventoryInitStep());

            // Звук: раздача AudioService компонентам сцены (Order 11).
            initializer.Add(new Core.Audio.AudioInitStep());

            // Сюда механики добавляют свои шаги. Порядок задаётся через IInitStep.Order:
            // initializer.Add(new GameTimeInitStep());
            // initializer.Add(new GameUiInitStep());
            // ...

            return initializer;
        }

        private void BindCoreSignals(InitContext ctx)
        {
            _exitToMenu = ctx.Root.Resolve<Subject<SceneTransitionParameters>>(CoreSignals.EXIT_TO_MENU);
            _transition = ctx.Root.Resolve<Subject<SceneTransitionParameters>>(CoreSignals.TRANSITION);
            _exitGame   = ctx.Root.Resolve<Subject<SceneTransitionParameters>>(CoreSignals.EXIT_GAME);

            // Сохранение по сигналу: собираем актуальный снапшот и пишем в файл.
            var saveSignal = ctx.Root.Resolve<Subject<Unit>>(CoreSignals.SAVE_GAME);
            saveSignal.Subscribe(_ => _stateService.Save()).AddTo(_sceneDisposables);

            // Переключение схем ввода: пауза → UI, продолжение → Player.
            var input = ctx.Root.Resolve<Core.Input.GameInput>();
            var pause = ctx.Root.Resolve<Subject<Unit>>(CoreSignals.PAUSE_GAME);
            var cont  = ctx.Root.Resolve<Subject<Unit>>(CoreSignals.CONTINUE_GAME);
            pause.Subscribe(_ =>
            {
                input.SwitchToUI();
                // В point-and-click режиме PlayerLook не используется
                if (!_pointAndClickMode && ctx.Scene.TryResolve<Core.Player.PlayerLook>(out var look)) 
                    look.SetEnabled(false);
            }).AddTo(_sceneDisposables);
            cont.Subscribe(_ =>
            {
                input.SwitchToPlayer();
                // В point-and-click режиме PlayerLook не используется
                if (!_pointAndClickMode && ctx.Scene.TryResolve<Core.Player.PlayerLook>(out var look)) 
                    look.SetEnabled(true);
            }).AddTo(_sceneDisposables);
        }

        private Observable<SceneTransitionParameters> BuildTransitionStream()
        {
            var exitToMenu = _exitToMenu.Select(p =>
            {
                // При выходе в меню фиксируем актуальный снапшот, чтобы он уехал с параметрами.
                p ??= new SceneTransitionParameters();
                p.signal = CoreSignals.EXIT_TO_MENU;
                p.gameState = _stateService.Capture();
                return p;
            });

            var transition = _transition.Select(p =>
            {
                p ??= new SceneTransitionParameters();
                p.signal = CoreSignals.TRANSITION;
                p.gameState = _stateService.Capture();
                return p;
            });

            var exitGame = _exitGame.Select(p =>
            {
                p ??= new SceneTransitionParameters();
                p.signal = CoreSignals.EXIT_GAME;
                return p;
            });

            return exitToMenu.Merge(transition).Merge(exitGame);
        }

        /// <summary>Механики-шаги могут регистрировать здесь созданные объекты на очистку.</summary>
        public void TrackForCleanup(GameObject go)
        {
            if (go != null) _spawned.Add(go);
        }

        private void OnDestroy()
        {
            foreach (var go in _spawned)
                if (go != null) Destroy(go);
            _spawned.Clear();
            _sceneDisposables.Dispose();
        }
    }
}
