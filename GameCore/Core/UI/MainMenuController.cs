using System.Collections.Generic;
using Core.DI;
using Core.Save;
using Core.Signals;
using Core.State;
using R3;
using UnityEngine;

namespace Core.UI
{
    /// <summary>
    /// Контроллер главного меню. Лежит на сцене MainMenuScene.
    /// Строит кнопки динамически из списка MenuItem через MenuBuilder и пушит
    /// нажатия в root-сигналы, которые слушает MainMenuBootstrap → GameBootstrap.
    ///
    /// Настройка в инспекторе: перетащи префаб кнопки (_buttonPrefab) и контейнер
    /// (_buttonsContainer, обычно объект с VerticalLayoutGroup внутри Canvas).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private MenuButtonView _buttonPrefab;
        [SerializeField] private Transform _buttonsContainer;
        [SerializeField] private SaveSlotPanel _slotPanel; // панель выбора слота (опционально)

        private MenuBuilder _builder;

        private Subject<SceneTransitionParameters> _newGame;
        private Subject<SceneTransitionParameters> _loadGame;
        private Subject<SceneTransitionParameters> _exitGame;
        private JsonSaveProvider _saveProvider;

        /// <summary>Вызывается из шага инициализации меню.</summary>
        public void Bind(DIContainer root)
        {
            _newGame  = root.Resolve<Subject<SceneTransitionParameters>>(CoreSignals.NEW_GAME);
            _loadGame = root.Resolve<Subject<SceneTransitionParameters>>(CoreSignals.LOAD_GAME);
            _exitGame = root.Resolve<Subject<SceneTransitionParameters>>(CoreSignals.EXIT_GAME);
            _saveProvider = root.Resolve<JsonSaveProvider>();

            if (_slotPanel != null) _slotPanel.Init(_saveProvider);

            _builder = new MenuBuilder(_buttonPrefab, _buttonsContainer);
            BuildMenu();
        }

        private void BuildMenu()
        {
            bool hasSaves = _saveProvider.GetExistingSlots().Length > 0;

            var items = new List<MenuItem>
            {
                new MenuItem("Новая игра", OnNewGame),
                // "Загрузить" пока не реализована — делаем неактивной, если сейвов нет.
                new MenuItem("Загрузить", OnLoadGame, interactable: hasSaves),
                // "Настройки" пока заглушка — неактивна.
                new MenuItem("Настройки", null, interactable: false),
                new MenuItem("Выход", OnExit),
            };

            _builder.Build(items);
        }

        private void OnNewGame()
        {
            // Пушим пустые параметры — GameBootstrap сам соберёт дефолтный стейт.
            _newGame.OnNext(new SceneTransitionParameters());
        }

        private void OnLoadGame()
        {
            if (_slotPanel != null)
            {
                // Показываем выбор слота; по выбору — грузим.
                _slotPanel.ShowForLoad(
                    onPick: slot => _loadGame.OnNext(new SceneTransitionParameters { saveSlotId = slot }),
                    onBack: () => { });
            }
            else
            {
                // Фолбэк без панели: грузим самый свежий слот.
                var infos = _saveProvider.GetAllSlotInfos();
                if (infos.Count == 0) return;
                _loadGame.OnNext(new SceneTransitionParameters { saveSlotId = infos[0].SlotId });
            }
        }

        private void OnExit()
        {
            _exitGame.OnNext(new SceneTransitionParameters());
        }

        private void OnDestroy()
        {
            _builder?.Clear();
        }
    }
}
