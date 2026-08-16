using System.Collections;
using Core.DI;
using Core.Init;
using Core.Input;
using Core.Save;
using Core.SceneLoader;
using Core.Signals;
using Core.State;
using R3;
using UnityEngine;

namespace Core.Boot
{
    /// <summary>
    /// Единственная точка входа в игру. Стартует автоматически до загрузки первой сцены,
    /// поднимает root-контейнер и ядровые сервисы, затем грузит сцену меню.
    ///
    /// Дальше вся навигация построена на сигналах перехода (SceneTransitionParameters),
    /// которые bootstrap каждой сцены возвращает наружу как Observable.
    ///
    /// Нажал Play → отработал AutoStart → загрузилась MainMenuScene. Всё.
    /// </summary>
    public class GameBootstrap
    {
        private static GameBootstrap _instance;

        private Coroutines _coroutines;
        private DIContainer _root;
        private SceneLoader.SceneLoader _sceneLoader;
        private JsonSaveProvider _saveProvider;
        private GameStateService _stateService;

        private SceneBootstrapBase _currentSceneBootstrap;
        private CompositeDisposable _sceneSubs = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoStart()
        {
            if (_instance != null)
            {
                Debug.LogWarning("[GameBootstrap] Уже запущен, пропускаем.");
                return;
            }

            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            _instance = new GameBootstrap();
            _instance.Run();
        }

        private GameBootstrap()
        {
            _coroutines = new GameObject("[COROUTINES]").AddComponent<Coroutines>();
            Object.DontDestroyOnLoad(_coroutines.gameObject);

            _root = new DIContainer();
            _root.RegisterInstance(_coroutines);
            InitCoreSystems();
        }

        /// <summary>Регистрация ядровых систем и сигналов в root-контейнере.</summary>
        private void InitCoreSystems()
        {
            var sceneGraph = Resources.Load<SceneGraph>("Core/SceneGraph");

            _sceneLoader = new SceneLoader.SceneLoader(sceneGraph);
            _saveProvider = new JsonSaveProvider();
            _stateService = new GameStateService(_saveProvider);

            _root.RegisterInstance(_sceneLoader);
            _root.RegisterInstance(_saveProvider);
            _root.RegisterInstance(_stateService);

            // Ввод: обёртка над MainInputSystem с переключением схем Player/UI.
            var input = new GameInput();
            _root.RegisterInstance(input);

            // Звук: единая точка воспроизведения (пул источников, громкости, фейды).
            // Регистрируется рано — на него опираются UI, зоны, игрок.
            var audio = new Core.Audio.AudioService();
            _root.RegisterInstance(audio);

            RegisterCoreSignals();

            // Арбитр UI-экранов (пауза/инвентарь): один открыт за раз, timeScale+ввод централизованы.
            var uiManager = new Core.UI.Screens.UIScreenManager(
                _root.Resolve<Subject<Unit>>(CoreSignals.PAUSE_GAME),
                _root.Resolve<Subject<Unit>>(CoreSignals.CONTINUE_GAME));
            _root.RegisterInstance(uiManager);

            // Глобальная пауза: один префаб на всю игру (DontDestroyOnLoad).
            var pausePrefab = Resources.Load<Core.UI.PauseController>("Core/PauseController");
            if (pausePrefab != null)
            {
                var pause = Object.Instantiate(pausePrefab);
                Object.DontDestroyOnLoad(pause.gameObject);
                pause.Bind(_root, input.PauseAction);
                _root.RegisterInstance(pause);
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] Resources/Core/PauseController не найден — пауза недоступна.");
            }

            // Глобальный UI инвентаря (как пауза).
            var invPrefab = Resources.Load<Core.Inventory.UI.InventoryUIController>("Core/InventoryUI");
            if (invPrefab != null)
            {
                var invUI = Object.Instantiate(invPrefab);
                Object.DontDestroyOnLoad(invUI.gameObject);
                invUI.Bind(_root, input.InventoryAction);
                invUI.BindNavigation(input.UiNavigate, input.UiUse, input.UiDrop);
                _root.RegisterInstance(invUI);
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] Resources/Core/InventoryUI не найден — UI инвентаря недоступен.");
            }

            // Диалоги: плеер (логика) в root + глобальный UI (как инвентарь).
            _root.RegisterInstance(new Core.Story.Dialogue.DialoguePlayer(_root));
            var dlgPrefab = Resources.Load<Core.Story.Dialogue.DialogueUIController>("Core/DialogueUI");
            if (dlgPrefab != null)
            {
                var dlgUI = Object.Instantiate(dlgPrefab);
                Object.DontDestroyOnLoad(dlgUI.gameObject);
                dlgUI.Bind(_root);
                _root.RegisterInstance(dlgUI);
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] Resources/Core/DialogueUI не найден — UI диалогов недоступен.");
            }

            // Механики регистрируют свои root-сервисы здесь, например:
            // Метки состояния (триггеры + сценовые метки) — один сервис на игру.
            var flagService = new Core.Flags.FlagService();
            _root.RegisterInstance(flagService);

            // Статы (здоровье/выносливость/рассудок) — из StatDatabase, зависят от флагов (onZero).
            var statDb = Resources.Load<Core.Stats.StatDatabase>("Core/StatDatabase");
            if (statDb != null)
                _root.RegisterInstance(new Core.Stats.StatsService(statDb.stats, flagService));
            else
                Debug.LogWarning("[GameBootstrap] Resources/Core/StatDatabase не найден — статы недоступны.");
            // Инвентарь: БД предметов из Resources + сервис (один на игру).
            var itemDb = Resources.Load<Core.Inventory.ItemDatabase>("Core/ItemDatabase");
            if (itemDb != null)
                _root.RegisterInstance(new Core.Inventory.InventoryService(itemDb, _root));
            else
                Debug.LogWarning("[GameBootstrap] Resources/Core/ItemDatabase не найден — инвентарь недоступен.");

            // Фазы (цикл дня — шаги с вариантами). Граф из Resources.
            var phaseGraph = Resources.Load<Core.Story.Phases.PhaseGraph>("Core/PhaseGraph");
            if (phaseGraph != null)
                _root.RegisterInstance(new Core.Story.Phases.PhaseService(phaseGraph, _root));
            else
                Debug.LogWarning("[GameBootstrap] Resources/Core/PhaseGraph не найден — фазы недоступны.");
            // Почта: каталог писем из Resources, состояние — в снапшоте.
            var mailCatalog = Resources.Load<Core.Mail.MailCatalog>("Mail/MailCatalog");
            if (mailCatalog != null)
                _root.RegisterInstance(new Core.Mail.MailService(mailCatalog.messages));
            else
                Debug.LogWarning("[GameBootstrap] Resources/Mail/MailCatalog не найден — почта недоступна.");
            // _root.RegisterInstance(new PlayerService());
            // _root.RegisterInstance(new GameTimeService());
        }

        private void RegisterCoreSignals()
        {
            _root.RegisterInstance(CoreSignals.NEW_GAME,      new Subject<SceneTransitionParameters>());
            _root.RegisterInstance(CoreSignals.LOAD_GAME,     new Subject<SceneTransitionParameters>());
            _root.RegisterInstance(CoreSignals.EXIT_GAME,     new Subject<SceneTransitionParameters>());
            _root.RegisterInstance(CoreSignals.EXIT_TO_MENU,  new Subject<SceneTransitionParameters>());
            _root.RegisterInstance(CoreSignals.TRANSITION,    new Subject<SceneTransitionParameters>());
            _root.RegisterInstance(CoreSignals.SAVE_GAME,     new Subject<Unit>());
            _root.RegisterInstance(CoreSignals.PAUSE_GAME,    new Subject<Unit>());
            _root.RegisterInstance(CoreSignals.CONTINUE_GAME, new Subject<Unit>());
        }

        private void Run()
        {
            _coroutines.StartCoroutine(LoadMenu());
        }

        // ---------------- MENU ----------------

        private IEnumerator LoadMenu(SceneTransitionParameters parameters = null)
        {
            parameters ??= new SceneTransitionParameters();

            _sceneSubs.Dispose();
            _sceneSubs = new CompositeDisposable();

            yield return _sceneLoader.LoadSceneAsync("MainMenuScene");

            var sceneContainer = new DIContainer(_root);
            var bootstrap = Object.FindAnyObjectByType<MainMenuBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError("[GameBootstrap] MainMenuBootstrap не найден на MainMenuScene!");
                yield break;
            }

            _currentSceneBootstrap = bootstrap;

            bootstrap.Initialize(new InitContext(_root, sceneContainer, parameters))
                .Subscribe(OnMenuSignal)
                .AddTo(_sceneSubs);
        }

        private void OnMenuSignal(SceneTransitionParameters p)
        {
            switch (p.signal)
            {
                case CoreSignals.NEW_GAME:
                {
                    int slotId = _saveProvider.GetExistingSlots().Length;
                    var firstSceneNode = _sceneLoader.graph.scenes.Length > 1 ? _sceneLoader.graph.scenes[1] : null;
                    string firstScene = firstSceneNode != null ? firstSceneNode.sceneName : null;

                    var state = GameStateData.CreateDefault(slotId, firstScene, firstSpawnId: 0);

                    p.gameState = state;
                    p.nextSceneName = state.sceneName;
                    p.nextSpawnId = state.spawnId;

                    _coroutines.StartCoroutine(LoadGame(p));
                    break;
                }
                case CoreSignals.LOAD_GAME:
                {
                    var state = _saveProvider.Load(p.saveSlotId);
                    if (state == null) { Debug.LogError($"[GameBootstrap] Слот {p.saveSlotId} пуст."); return; }

                    p.gameState = state;
                    p.nextSceneName = state.sceneName;
                    p.nextSpawnId = state.spawnId;

                    _coroutines.StartCoroutine(LoadGame(p));
                    break;
                }
                case CoreSignals.EXIT_GAME:
                    _sceneLoader.ExitGame();
                    break;
            }
        }

        // ---------------- GAME ----------------

        private IEnumerator LoadGame(SceneTransitionParameters parameters)
        {
            if (_currentSceneBootstrap is GameplayBootstrap oldGameplay && oldGameplay != null)
            {
                Object.Destroy(oldGameplay.gameObject);
            }

            _sceneSubs.Dispose();
            _sceneSubs = new CompositeDisposable();

            yield return _sceneLoader.LoadSceneAsync(parameters.nextSceneName);

            var sceneContainer = new DIContainer(_root);

            var prefab = Resources.Load<GameplayBootstrap>("Core/GameplayBootstrap");
            var bootstrap = Object.Instantiate(prefab);
            _currentSceneBootstrap = bootstrap;

            bootstrap.Initialize(new InitContext(_root, sceneContainer, parameters))
                .Subscribe(OnGameplaySignal)
                .AddTo(_sceneSubs);
        }

        private void OnGameplaySignal(SceneTransitionParameters p)
        {
            switch (p.signal)
            {
                case CoreSignals.EXIT_TO_MENU:
                    _coroutines.StartCoroutine(LoadMenu(p));
                    break;
                case CoreSignals.TRANSITION:
                    _coroutines.StartCoroutine(LoadGame(p));
                    break;
                case CoreSignals.EXIT_GAME:
                    _sceneLoader.ExitGame();
                    break;
            }
        }
    }
}
