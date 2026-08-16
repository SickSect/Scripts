using System.Collections.Generic;
using UnityEngine;

namespace Core.UI
{
    /// <summary>
    /// Переиспользуемый строитель меню: берёт префаб кнопки и список пунктов,
    /// спавнит кнопку под каждый пункт в заданный контейнер и умеет всё почистить.
    ///
    /// Один и тот же MenuBuilder годится для любого меню (главное, пауза, настройки) —
    /// меняется только список MenuItem, который ты в него передаёшь.
    /// </summary>
    public class MenuBuilder
    {
        private readonly MenuButtonView _buttonPrefab;
        private readonly Transform _container;
        private readonly List<GameObject> _spawned = new();

        public MenuBuilder(MenuButtonView buttonPrefab, Transform container)
        {
            _buttonPrefab = buttonPrefab;
            _container = container;
        }

        /// <summary>Пересобрать меню из списка пунктов (старые кнопки удаляются).</summary>
        public void Build(IEnumerable<MenuItem> items)
        {
            Clear();
            foreach (var item in items)
            {
                var view = Object.Instantiate(_buttonPrefab, _container);
                view.Bind(item);
                _spawned.Add(view.gameObject);
            }
        }

        /// <summary>Удалить все заспавненные кнопки.</summary>
        public void Clear()
        {
            foreach (var go in _spawned)
                if (go != null) Object.Destroy(go);
            _spawned.Clear();
        }
    }
}
