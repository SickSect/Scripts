using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Computer
{
    /// <summary>
    /// Ярлык на рабочем столе. Открывает приложение по двойному клику —
    /// как в системах того времени.
    ///
    /// Двойной клик считается вручную: мост монитора шлёт одиночные клики и
    /// не заполняет PointerEventData.clickCount.
    /// </summary>
    public class DesktopIcon : MonoBehaviour, IPointerClickHandler
    {
        [Header("Приложение")]
        [SerializeField] private DesktopAppDefinition _app;

        [Tooltip("Оболочка стола. Пусто — ищется вверх по иерархии.")]
        [SerializeField] private DesktopService _desktop;

        [Header("Вид")]
        [Tooltip("Куда подставить иконку из описания приложения. Можно оставить пустым.")]
        [SerializeField] private Image _iconImage;

        [Tooltip("Куда подставить подпись. Можно оставить пустым.")]
        [SerializeField] private TMP_Text _label;

        [Tooltip("Подсветка выделения. Включается по первому клику.")]
        [SerializeField] private GameObject _selection;

        [Header("Поведение")]
        [Tooltip("Открывать по одному клику вместо двойного.")]
        [SerializeField] private bool _singleClick = false;

        [SerializeField] private float _doubleClickTime = 0.4f;

        private float _lastClick = -10f;

        private void Awake()
        {
            if (_desktop == null) _desktop = GetComponentInParent<DesktopService>();

            if (_app != null)
            {
                if (_iconImage != null && _app.icon != null) _iconImage.sprite = _app.icon;
                if (_label != null) _label.text = _app.title;
            }

            Deselect();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_singleClick)
            {
                Open();
                return;
            }

            float now = Time.unscaledTime;

            if (now - _lastClick <= _doubleClickTime)
            {
                _lastClick = -10f;
                Deselect();
                Open();
                return;
            }

            _lastClick = now;
            Select();
        }

        private void Open()
        {
            if (_desktop == null)
            {
                Debug.LogError($"[DesktopIcon] '{name}': не найден DesktopService.");
                return;
            }

            if (_app == null)
            {
                Debug.LogWarning($"[DesktopIcon] '{name}': приложение не задано.");
                return;
            }

            _desktop.OpenApp(_app);
        }

        private void Select()
        {
            if (_selection != null) _selection.SetActive(true);
        }

        private void Deselect()
        {
            if (_selection != null) _selection.SetActive(false);
        }
    }
}
