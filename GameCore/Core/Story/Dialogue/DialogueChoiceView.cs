using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Story.Dialogue
{
    /// <summary>
    /// Кнопка одного варианта ответа в диалоге. Сообщает контроллеру индекс при клике.
    /// Префаб: Button + TMP_Text.
    /// </summary>
    public class DialogueChoiceView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;

        public void Setup(int index, string text, Action<int> onClick)
        {
            if (_label != null) _label.text = text;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick?.Invoke(index));
        }

        /// <summary>Подсветка выделенного варианта (для навигации WASD).</summary>
        public void SetSelected(bool selected)
        {
            if (_label != null)
                _label.color = selected ? new Color(1f, 0.9f, 0.4f) : Color.white;

            if (_button != null)
            {
                var colors = _button.colors;
                colors.normalColor = selected ? new Color(0.35f, 0.35f, 0.2f) : Color.white;
                _button.colors = colors;
            }
        }
    }
}
