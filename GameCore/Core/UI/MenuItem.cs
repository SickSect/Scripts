using System;

namespace Core.UI
{
    /// <summary>
    /// Описание одного пункта меню: подпись, действие по клику и активность.
    /// Это чистые данные — не MonoBehaviour. Контроллер отдаёт список таких пунктов,
    /// MenuBuilder спавнит под каждый кнопку.
    /// </summary>
    public class MenuItem
    {
        public string Label;
        public Action OnClick;
        public bool Interactable;

        public MenuItem(string label, Action onClick, bool interactable = true)
        {
            Label = label;
            OnClick = onClick;
            Interactable = interactable;
        }
    }
}
