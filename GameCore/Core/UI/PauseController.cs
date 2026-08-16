using System.Collections.Generic;
using Core.DI;
using Core.Signals;
using Core.State;
using Core.UI.Screens;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.UI
{
    /// <summary>
    /// Глобальное меню паузы. Реализует IUIScreen — открытие/закрытие и timeScale/ввод
    /// разруливает UIScreenManager (чтобы не конфликтовать с инвентарём).
    /// </summary>
    public class PauseController : MonoBehaviour, IUIScreen
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private MenuButtonView _buttonPrefab;
        [SerializeField] private Transform _buttonsContainer;

        private MenuBuilder _builder;
        private DIContainer _root;
        private UIScreenManager _screens;

        private Subject<Unit> _saveSignal;
        private Subject<SceneTransitionParameters> _exitToMenu;

        private InputAction _pauseAction;
        private bool _isPaused;
        private bool _available; // пауза работает только на игровых сценах

        public bool IsOpen => _isPaused;

        /// <summary>Включить/выключить доступность паузы (геймплей — вкл, меню — выкл).</summary>
        public void SetAvailable(bool available)
        {
            _available = available;
            if (!available && _isPaused) _screens.RequestClose(this);
        }

        /// <summary>Вызывается один раз из GameBootstrap при создании префаба.</summary>
        public void Bind(DIContainer root, InputAction pauseAction)
        {
            _root = root;
            _screens        = root.Resolve<UIScreenManager>();
            _exitToMenu     = root.Resolve<Subject<SceneTransitionParameters>>(CoreSignals.EXIT_TO_MENU);
            _saveSignal     = root.Resolve<Subject<Unit>>(CoreSignals.SAVE_GAME);

            _builder = new MenuBuilder(_buttonPrefab, _buttonsContainer);

            _pauseAction = pauseAction;
            _pauseAction.performed += OnPauseKey;

            _panel.SetActive(false);
        }

        private void OnPauseKey(InputAction.CallbackContext ctx)
        {
            if (!_available) return;

            // Если идёт диалог — Esc закрывает его (без прохождения), паузу не открываем.
            if (_root != null && _root.TryResolve<Core.Story.Dialogue.DialoguePlayer>(out var dialogue)
                && dialogue.IsActive)
            {
                dialogue.Close(completed: false);
                return;
            }

            _screens.RequestToggle(this);
        }

        // --- IUIScreen: только показ/скрытие, timeScale и ввод делает менеджер ---

        public void OpenScreen()
        {
            _isPaused = true;
            _panel.SetActive(true);
            BuildMenu();
        }

        public void CloseScreen()
        {
            _isPaused = false;
            _builder.Clear();
            _panel.SetActive(false);
        }

        private void BuildMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Продолжить", () => _screens.RequestClose(this)),
                new MenuItem("Сохранить", OnSave),
                new MenuItem("Загрузить", null, interactable: false),
                new MenuItem("Настройки", null, interactable: false),
                new MenuItem("Выйти", OnExitToMenu),
            };
            _builder.Build(items);
        }

        private void OnSave()
        {
            _saveSignal.OnNext(Unit.Default); // GameplayBootstrap слушает и вызывает Save()
            Core.Common.CoreLog.Debug("[Pause] сохранение запрошено");
        }

        private void OnExitToMenu()
        {
            // Закрываем экран через менеджер (вернёт timeScale/ввод), потом сигналим выход.
            _screens.RequestClose(this);
            _exitToMenu.OnNext(new SceneTransitionParameters());
        }

        private void OnDestroy()
        {
            if (_pauseAction != null) _pauseAction.performed -= OnPauseKey;
        }
    }
}
