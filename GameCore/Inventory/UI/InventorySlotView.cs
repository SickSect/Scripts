using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Inventory.UI
{
    /// <summary>
    /// Вью одной ячейки инвентаря (префаб). Показывает иконку и количество,
    /// сообщает контроллеру о клике по себе.
    ///
    /// На префабе: Button (клик), Image (иконка предмета), TMP_Text (количество).
    /// </summary>
    public class InventorySlotView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _countLabel;

        private int _index;
        private Action<int> _onClick;

        public void Setup(int index, Action<int> onClick)
        {
            _index = index;
            _onClick = onClick;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onClick?.Invoke(_index));
        }

        /// <summary>Обновить содержимое. stack=null — пустая ячейка.</summary>
        public void Render(ItemStack stack)
        {
            bool empty = stack == null || stack.IsEmpty;

            if (_icon != null)
            {
                _icon.enabled = !empty;
                if (!empty) _icon.sprite = stack.Item.icon;
            }

            if (_countLabel != null)
                _countLabel.text = (!empty && stack.Count > 1) ? stack.Count.ToString() : "";
        }

        public void SetSelected(bool selected)
        {
            // Простейшая подсветка выбранной ячейки — через цвет кнопки.
            if (_button != null)
            {
                var colors = _button.colors;
                colors.normalColor = selected ? new Color(1f, 1f, 0.6f) : Color.white;
                _button.colors = colors;
            }
        }
    }
}
