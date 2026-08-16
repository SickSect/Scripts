using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Computer
{
    /// <summary>
    /// Окно рабочего стола. Вешается на КОРЕНЬ окна (панель с рамкой и содержимым).
    ///
    /// Умеет три вещи и только их: подниматься наверх при клике, двигаться за
    /// заголовком, закрываться. Внешний вид полностью на стороне префаба —
    /// скрипт не создаёт и не красит ни одного элемента.
    ///
    /// Перетаскивание вынесено в WindowDragHandle на заголовке: иначе окно
    /// таскалось бы за любую точку содержимого.
    /// </summary>
    public class DesktopWindow : MonoBehaviour, IPointerDownHandler
    {
        [Header("Ссылки")]
        [Tooltip("Кнопка закрытия. Может быть пустой — тогда окно закрывается только из кода.")]
        [SerializeField] private UnityEngine.UI.Button _closeButton;

        [Header("Поведение")]
        [Tooltip("Держать окно в границах рабочего стола при перетаскивании.")]
        [SerializeField] private bool _clampToDesktop = true;

        [Tooltip("Сколько пикселей окна обязано остаться видимым при выходе за край.")]
        [SerializeField] private float _minVisible = 48f;

        private RectTransform _rect;
        private RectTransform _desktop;

        public RectTransform Rect
        {
            get
            {
                if (_rect == null) _rect = (RectTransform)transform;
                return _rect;
            }
        }

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _desktop = transform.parent as RectTransform;

            if (_closeButton != null)
                _closeButton.onClick.AddListener(CloseWindow);
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(CloseWindow);
        }

        /// <summary>Клик в любую точку окна поднимает его над остальными.</summary>
        public void OnPointerDown(PointerEventData eventData) => Focus();

        /// <summary>Поднять окно наверх. Порядок окон = порядок дочерних объектов.</summary>
        public void Focus() => transform.SetAsLastSibling();

        public void OpenWindow()
        {
            gameObject.SetActive(true);
            Focus();
        }

        public void CloseWindow() => gameObject.SetActive(false);

        /// <summary>Сдвинуть окно. Вызывается из WindowDragHandle.</summary>
        public void MoveBy(Vector2 delta)
        {
            Rect.anchoredPosition += delta;
            if (_clampToDesktop) Clamp();
        }

        private void Clamp()
        {
            if (_desktop == null) return;

            Vector2 half = _desktop.rect.size * 0.5f;
            Vector2 size = Rect.rect.size;
            Vector2 pos = Rect.anchoredPosition;

            // Пивот окна не обязан быть в центре — считаем от фактических краёв.
            float left = -half.x - size.x * Rect.pivot.x + _minVisible;
            float right = half.x + size.x * (1f - Rect.pivot.x) - _minVisible;
            float bottom = -half.y - size.y * Rect.pivot.y + _minVisible;
            float top = half.y + size.y * (1f - Rect.pivot.y) - _minVisible;

            pos.x = Mathf.Clamp(pos.x, left, right);
            pos.y = Mathf.Clamp(pos.y, bottom, top);

            Rect.anchoredPosition = pos;
        }
    }
}
