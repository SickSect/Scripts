using System.Collections.Generic;
using Core.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Core.Computer
{
    /// <summary>
    /// Мост кликов для экрана компьютера.
    ///
    /// Интерфейс живёт на канвасе за пределами уровня и рендерится камерой в
    /// RenderTexture, натянутую на меш экрана. Клики переводятся так:
    ///
    ///   курсор мыши → луч из камеры игрока → попадание в MeshCollider экрана
    ///   → hit.textureCoord (UV) → UV * размер RT = экранная точка канваса
    ///   → GraphicRaycaster → обычные события UI
    ///
    /// Проброшены: enter/exit, down/up, click, begin/drag/end drag, drop, scroll.
    /// Внутри канваса работают Button, ScrollRect, Slider, InputField без правок.
    ///
    /// Компонент включается и выключается WorkstationController: он живёт, только
    /// пока игрок смотрит на монитор. Входом за рабочее место и движением камеры
    /// занимается станция, а не этот скрипт.
    ///
    /// ВАЖНО: на экране должен стоять MeshCollider. BoxCollider не заполняет
    /// textureCoord и молча возвращает (0,0) — все клики уедут в угол.
    /// </summary>
    public class MonitorScreen : MonoBehaviour
    {
        [Header("Экран")]
        [Tooltip("MeshCollider плоскости экрана. Пусто — берётся с этого объекта.")]
        [SerializeField] private Collider _screenCollider;

        [Tooltip("RenderTexture, в которую рендерится интерфейс.")]
        [SerializeField] private RenderTexture _renderTexture;

        [Header("Интерфейс")]
        [Tooltip("GraphicRaycaster канваса с интерфейсом (MonitorCanvas).")]
        [SerializeField] private GraphicRaycaster _raycaster;

        [Header("Ввод")]
        [Tooltip("Смещение курсора в пикселях RT, после которого начинается перетаскивание.")]
        [SerializeField] private float _dragThreshold = 8f;

        [Tooltip("Если к моменту отпускания курсор ближе этого к точке нажатия — " +
                 "засчитываем клик, даже если было перетаскивание.")]
        [SerializeField] private float _clickTolerance = 12f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private readonly List<RaycastResult> _results = new();

        private Camera _viewCamera;

        private PointerEventData _pointer;
        private GameObject _hovered;
        private GameObject _pressed;
        private GameObject _clickTarget;
        private GameObject _dragging;
        private bool _dragStarted;
        private Vector2 _prevPos;



        private void Awake()
        {
            if (_screenCollider == null) _screenCollider = GetComponent<Collider>();

            if (_screenCollider is not MeshCollider)
            {
                Debug.LogError($"[MonitorScreen] На '{name}' нужен MeshCollider. " +
                               "BoxCollider не заполняет textureCoord — клики работать не будут.");
            }
        }

        private void OnEnable()
        {
            // Камеру ищем лениво в LateUpdate: игрок спавнится init-шагом позже,
            // чем включается этот компонент, и в OnEnable её ещё нет.
            _viewCamera = null;

            _pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left
            };

            _prevPos = Vector2.zero;

            if (_debugLog) Debug.Log("[MonitorScreen] мост кликов включён.");
        }

        private void OnDisable()
        {
            CancelDrag();
            ClearHover();

            if (_debugLog) Debug.Log("[MonitorScreen] мост кликов выключен.");
        }

        private void LateUpdate()
        {
            if (_pointer == null) return;

            if (_viewCamera == null)
            {
                _viewCamera = ResolveCamera();
                if (_viewCamera == null) return;

                if (_debugLog) Debug.Log($"[MonitorScreen] камера: {_viewCamera.name}");
            }

            UpdatePointer();
        }

        /// <summary>
        /// Камера игрока. Ищется по компоненту, а не по тегу MainCamera:
        /// тег легко потерять при перенастройке префаба, и тогда мост молча
        /// перестаёт работать — без единой ошибки в консоли.
        /// </summary>
        private Camera ResolveCamera()
        {
            var fps = FindAnyObjectByType<FirstPersonCamera>();
            if (fps != null && fps.Camera != null) return fps.Camera;

            var main = Camera.main;
            if (main != null) return main;

            if (_debugLog)
                Debug.LogWarning("[MonitorScreen] камера игрока не найдена: " +
                                 "нет ни FirstPersonCamera в сцене, ни камеры с тегом MainCamera.");

            return null;
        }

        /// <summary>Явно задать камеру-источник луча, если автопоиск не подходит.</summary>
        public void SetCamera(Camera camera) => _viewCamera = camera;

        private void UpdatePointer()
        {
            if (_raycaster == null || _renderTexture == null || _viewCamera == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Ray ray = _viewCamera.ScreenPointToRay(mouse.position.ReadValue());

            bool onScreen = _screenCollider.Raycast(ray, out RaycastHit hit, 100f);

            // Курсор ушёл с экрана: перетаскивание продолжаем (как в настоящей ОС),
            // но подсветку снимаем.
            if (!onScreen)
            {
                if (_dragStarted) DragTo(_pointer.position);
                else ClearHover();

                if (mouse.leftButton.wasReleasedThisFrame) EndPress(null, _pointer.position);
                return;
            }

            Vector2 uv = hit.textureCoord;
            Vector2 pos = new(uv.x * _renderTexture.width, uv.y * _renderTexture.height);

            _pointer.delta = _prevPos == Vector2.zero ? Vector2.zero : pos - _prevPos;
            _prevPos = pos;
            _pointer.position = pos;

            _results.Clear();
            _raycaster.Raycast(_pointer, _results);

            RaycastResult top = _results.Count > 0 ? _results[0] : default;
            GameObject target = _results.Count > 0 ? top.gameObject : null;

            _pointer.pointerCurrentRaycast = top;

            if (_debugLog && mouse.leftButton.wasPressedThisFrame)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append($"[MonitorScreen] нажатие в RT({pos.x:F0},{pos.y:F0}), попаданий: {_results.Count}");

                for (int i = 0; i < _results.Count && i < 5; i++)
                    sb.Append($"\n  {i}: {_results[i].gameObject.name}");

                Debug.Log(sb.ToString());
            }

            HandleHover(target);

            if (mouse.leftButton.wasPressedThisFrame) BeginPress(target, top);
            else if (mouse.leftButton.isPressed) ContinuePress(pos);

            if (mouse.leftButton.wasReleasedThisFrame) EndPress(target, pos);

            Vector2 scroll = mouse.scroll.ReadValue();
            if (scroll.sqrMagnitude > 0.01f && target != null)
            {
                _pointer.scrollDelta = scroll;
                ExecuteEvents.ExecuteHierarchy(target, _pointer, ExecuteEvents.scrollHandler);
            }
        }

        private void HandleHover(GameObject target)
        {
            if (target == _hovered) return;

            if (_hovered != null)
                ExecuteEvents.ExecuteHierarchy(_hovered, _pointer, ExecuteEvents.pointerExitHandler);

            _hovered = target;
            _pointer.pointerEnter = target;

            if (_hovered != null)
                ExecuteEvents.ExecuteHierarchy(_hovered, _pointer, ExecuteEvents.pointerEnterHandler);
        }

        private void BeginPress(GameObject target, RaycastResult top)
        {
            if (target == null) return;

            _pointer.pressPosition = _pointer.position;
            _pointer.pointerPressRaycast = top;
            _pointer.eligibleForClick = true;
            _pointer.dragging = false;
            _dragStarted = false;

            // Down и Click ищутся раздельно. ExecuteHierarchy для Down может уйти
            // вверх к ScrollRect (он реализует IPointerDownHandler), а обработчик
            // клика при этом остаётся на самом элементе — сравнивать их нельзя.
            _pressed = ExecuteEvents.ExecuteHierarchy(target, _pointer, ExecuteEvents.pointerDownHandler);
            _clickTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);

            _pointer.pointerPress = _pressed != null ? _pressed : _clickTarget;
            _dragging = ExecuteEvents.GetEventHandler<IDragHandler>(target);
            _pointer.pointerDrag = _dragging;
        }

        private void ContinuePress(Vector2 pos)
        {
            if (_dragging == null) return;

            if (!_dragStarted)
            {
                if ((pos - _pointer.pressPosition).magnitude < _dragThreshold) return;

                _dragStarted = true;
                _pointer.dragging = true;
                _pointer.eligibleForClick = false;

                ExecuteEvents.Execute(_dragging, _pointer, ExecuteEvents.beginDragHandler);
            }

            DragTo(pos);
        }

        private void DragTo(Vector2 pos)
        {
            if (_dragging == null) return;

            _pointer.position = pos;
            ExecuteEvents.Execute(_dragging, _pointer, ExecuteEvents.dragHandler);
        }

        private void EndPress(GameObject target, Vector2 releasePos)
        {
            if (_pressed != null)
                ExecuteEvents.Execute(_pressed, _pointer, ExecuteEvents.pointerUpHandler);

            if (_dragStarted && _dragging != null)
                ExecuteEvents.Execute(_dragging, _pointer, ExecuteEvents.endDragHandler);

            // Курсор почти не сдвинулся — это клик, даже если ScrollRect успел
            // перехватить нажатие как перетаскивание списка. Иначе элементы
            // внутри прокручиваемых областей вообще не нажимаются.
            bool nearPress = (releasePos - _pointer.pressPosition).magnitude <= _clickTolerance;

            if (nearPress && _clickTarget != null && target != null &&
                ExecuteEvents.GetEventHandler<IPointerClickHandler>(target) == _clickTarget)
            {
                ExecuteEvents.Execute(_clickTarget, _pointer, ExecuteEvents.pointerClickHandler);
            }
            else if (_dragStarted && target != null)
            {
                ExecuteEvents.ExecuteHierarchy(target, _pointer, ExecuteEvents.dropHandler);
            }

            _pressed = null;
            _clickTarget = null;
            _dragging = null;
            _dragStarted = false;
            _pointer.pointerPress = null;
            _pointer.pointerDrag = null;
            _pointer.dragging = false;
        }

        private void CancelDrag()
        {
            if (_dragStarted && _dragging != null)
                ExecuteEvents.Execute(_dragging, _pointer, ExecuteEvents.endDragHandler);

            _pressed = null;
            _clickTarget = null;
            _dragging = null;
            _dragStarted = false;
        }

        private void ClearHover()
        {
            if (_hovered != null && _pointer != null)
                ExecuteEvents.ExecuteHierarchy(_hovered, _pointer, ExecuteEvents.pointerExitHandler);

            _hovered = null;
        }
    }
}