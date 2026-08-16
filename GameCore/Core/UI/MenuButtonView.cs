using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI
{
    /// <summary>
    /// Компонент, который висит на ПРЕФАБЕ кнопки меню.
    /// Связывает конкретный MenuItem с реальными Button + текстом.
    ///
    /// На префабе должны быть: Button (тот же объект или дочерний) и TMP_Text (подпись).
    /// Перетащи их в поля в инспекторе префаба.
    /// </summary>
    public class MenuButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;

        public void Bind(MenuItem item)
        {
            if (_label != null) _label.text = item.Label;

            if (_button != null)
            {
                _button.interactable = item.Interactable;
                _button.onClick.RemoveAllListeners();
                if (item.OnClick != null)
                    _button.onClick.AddListener(() => item.OnClick());
            }
        }
    }
}
