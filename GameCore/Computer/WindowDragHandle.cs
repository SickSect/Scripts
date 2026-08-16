using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Computer
{
    /// <summary>
    /// Заголовок окна. Вешается на полосу заголовка внутри окна.
    ///
    /// Сдвиг считается через RectTransformUtility, а не через eventData.delta:
    /// канвас монитора живёт в World Space и отрисован в RenderTexture, поэтому
    /// пиксели курсора и единицы канваса не совпадают один к одному.
    /// </summary>
    public class WindowDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {
        [Tooltip("Окно, которое двигает этот заголовок. Пусто — ищется вверх по иерархии.")]
        [SerializeField] private DesktopWindow _window;

        private RectTransform _desktop;
        private Vector2 _grabOffset;

        private void Awake()
        {
            if (_window == null) _window = GetComponentInParent<DesktopWindow>();

            if (_window == null)
            {
                Debug.LogError($"[WindowDragHandle] На '{name}' не найдено окно DesktopWindow.");
                return;
            }

            _desktop = _window.transform.parent as RectTransform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_window != null) _window.Focus();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_window == null || _desktop == null) return;

            if (TryGetLocal(eventData, out Vector2 local))
                _grabOffset = _window.Rect.anchoredPosition - local;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_window == null || _desktop == null) return;

            if (!TryGetLocal(eventData, out Vector2 local)) return;

            Vector2 target = local + _grabOffset;
            _window.MoveBy(target - _window.Rect.anchoredPosition);
        }

        private bool TryGetLocal(PointerEventData eventData, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _desktop, eventData.position, eventData.pressEventCamera, out local);
        }
    }
}
