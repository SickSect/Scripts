using System;
using System.Collections.Generic;
using Core.Save;
using UnityEngine;

namespace Core.UI
{
    /// <summary>
    /// Панель выбора слота сохранения. Переиспользуема: открывается для загрузки
    /// (выбрал слот → колбэк с id) или для сохранения (выбрал слот → перезаписать).
    ///
    /// Строит кнопки под слоты через MenuBuilder. Работает поверх меню/паузы.
    ///
    /// Настройка префаба: Panel (корень) с контейнером (_buttonsContainer) и
    /// префабом кнопки (_buttonPrefab). Кнопка "Назад" — необязательна, вешается кодом.
    /// </summary>
    public class SaveSlotPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private MenuButtonView _buttonPrefab;
        [SerializeField] private Transform _buttonsContainer;

        private MenuBuilder _builder;
        private JsonSaveProvider _saveProvider;

        public void Init(JsonSaveProvider saveProvider)
        {
            _saveProvider = saveProvider;
            _builder = new MenuBuilder(_buttonPrefab, _buttonsContainer);
            Hide();
        }

        /// <summary>Показать список существующих сейвов. onPick(slotId) — выбранный слот.</summary>
        public void ShowForLoad(Action<int> onPick, Action onBack = null)
        {
            var infos = _saveProvider.GetAllSlotInfos();
            var items = new List<MenuItem>();

            foreach (var info in infos)
            {
                int slot = info.SlotId; // копия для замыкания
                string label = $"Слот {slot} — {info.SceneName} ({info.DateTimeLocal:dd.MM HH:mm})";
                items.Add(new MenuItem(label, () => { Hide(); onPick?.Invoke(slot); }));
            }

            if (items.Count == 0)
                items.Add(new MenuItem("Нет сохранений", null, interactable: false));

            if (onBack != null)
                items.Add(new MenuItem("Назад", () => { Hide(); onBack(); }));

            _builder.Build(items);
            _root.SetActive(true);
        }

        public void Hide()
        {
            _builder?.Clear();
            _root.SetActive(false);
        }
    }
}
