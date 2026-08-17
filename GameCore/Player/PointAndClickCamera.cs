using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Player
{
    /// <summary>
    /// Камера для режима Point-and-Click с отладкой ввода и границ.
    /// </summary>
    public class PointAndClickCamera : MonoBehaviour
    {
        [Header("Скорость движения")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _edgeScrollThreshold = 20f; // Пикселей от края
        [SerializeField] private float _edgeScrollSpeed = 8f;

        [Header("Зум")]
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _minZoom = 5f;
        [SerializeField] private float _maxZoom = 20f;

        [Header("Границы уровня (XZ плоскость)")]
        [Tooltip("Камера не выйдет за эти границы по X и Z.")]
        [SerializeField] private Bounds _levelBounds = new Bounds(Vector3.zero, new Vector3(20, 0, 20));

        private Camera _cam;
        private Vector2 _moveInput;

        // Input Actions
        private InputAction _moveAction;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;

            if (_levelBounds.extents == Vector3.zero)
            {
                _levelBounds = new Bounds(transform.position, new Vector3(50, 1, 50));
                Debug.LogWarning("[PointAndClickCamera] Границы не заданы! Используется дефолт вокруг старта.");
            }

            Debug.Log($"[PointAndClickCamera] Инициализация. Границы: {_levelBounds}");
        }

        public void BindInput(InputAction moveAction)
        {
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMovePerformed;
                _moveAction.canceled -= OnMoveCanceled;
            }

            _moveAction = moveAction;

            if (_moveAction != null)
            {
                _moveAction.performed += OnMovePerformed;
                _moveAction.canceled += OnMoveCanceled;
                Debug.Log($"[PointAndClickCamera] Привязан Input Action: '{moveAction.name}'. Enabled: {moveAction.enabled}");

                // Если действие уже активно (например, было включено до бинда), считаем текущее значение
                if (moveAction.enabled)
                {
                    _moveInput = moveAction.ReadValue<Vector2>();
                }
            }
            else
            {
                Debug.LogError("[PointAndClickCamera] Input Action НЕ привязан (null)! Проверь Bootstrap.");
            }
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            _moveInput = ctx.ReadValue<Vector2>();
#if UNITY_EDITOR
            if (_moveInput != Vector2.zero)
                Debug.Log($"[DEBUG INPUT] Получено движение: {_moveInput}");
#endif
        }

        private void OnMoveCanceled(InputAction.CallbackContext ctx)
        {
            _moveInput = Vector2.zero;
#if UNITY_EDITOR
            Debug.Log("[DEBUG INPUT] Движение сброшено в 0");
#endif
        }

        private void Update()
        {
            // Отладка: если нажаты клавиши, но инпут пустой
#if UNITY_EDITOR
            if ((Keyboard.current.wKey.isPressed || Keyboard.current.sKey.isPressed ||
                 Keyboard.current.aKey.isPressed || Keyboard.current.dKey.isPressed) && _moveInput == Vector2.zero)
            {
                Debug.LogWarning("[DEBUG INPUT] Клавиши WASD нажаты, но Input Action молчит! Проверь маппинг в .inputactions и вызов Enable().");
            }
#endif

            HandleMovement();
            HandleZoom();
        }

        private void HandleMovement()
        {
            Vector3 moveDir = Vector3.zero;
            bool isEdgeScrolling = false;

            // 1. Движение от клавиатуры (WASD)
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                // В Input System Y обычно смотрит вверх, а в Unity Z вперед. 
                // Зависит от настроек Control Scheme, но обычно Move это (x, y) -> (Right, Up).
                // Нам нужно (x, z).
                moveDir += new Vector3(_moveInput.x, 0, _moveInput.y);

#if UNITY_EDITOR
                // Лог только один раз при начале движения, чтобы не спамить
                if (Time.frameCount % 60 == 0)
                    Debug.Log($"[DEBUG MOVE] WASD активны. Вектор: {moveDir}");
#endif
            }

            // 2. Движение от краев экрана (Edge Scrolling)
            Vector2 mousePos = Mouse.current.position.ReadValue();
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // Проверка на выход мыши за пределы окна (иногда бывает при полноэкранном режиме)
            if (mousePos.x < 0 || mousePos.x > screenWidth || mousePos.y < 0 || mousePos.y > screenHeight)
            {
                // Мышь вне окна, игнорируем edge scroll
            }
            else
            {
                if (mousePos.x < _edgeScrollThreshold)
                {
                    moveDir += Vector3.left;
                    isEdgeScrolling = true;
                }
                if (mousePos.x > screenWidth - _edgeScrollThreshold)
                {
                    moveDir += Vector3.right;
                    isEdgeScrolling = true;
                }
                if (mousePos.y < _edgeScrollThreshold)
                {
                    moveDir += Vector3.back;
                    isEdgeScrolling = true;
                }
                if (mousePos.y > screenHeight - _edgeScrollThreshold)
                {
                    moveDir += Vector3.forward;
                    isEdgeScrolling = true;
                }
            }

            if (moveDir.sqrMagnitude > 0.001f)
            {
                moveDir.Normalize();

                // Ускоряем движение у краев
                float currentSpeed = isEdgeScrolling ? _edgeScrollSpeed : _moveSpeed;

                Vector3 displacement = moveDir * (currentSpeed * Time.deltaTime);

#if UNITY_EDITOR
                if (isEdgeScrolling && Time.frameCount % 30 == 0)
                    Debug.Log($"[DEBUG EDGE] Скролл у края экрана. Дирекция: {moveDir}");
#endif

                MoveWithBounds(displacement);
            }
        }

        private void HandleZoom()
        {
            float scroll = Mouse.current.scroll.ReadValue().y;

            if (Mathf.Abs(scroll) > 0.1f)
            {
                float zoomAmount = -scroll * _zoomSpeed * Time.deltaTime;
                float newZoom = _cam.orthographicSize + zoomAmount;
                float clampedZoom = Mathf.Clamp(newZoom, _minZoom, _maxZoom);

                if (!Mathf.Approximately(_cam.orthographicSize, clampedZoom))
                {
                    _cam.orthographicSize = clampedZoom;
#if UNITY_EDITOR
                    Debug.Log($"[DEBUG ZOOM] Новый зум: {clampedZoom}");
#endif
                }
            }
        }

        private void MoveWithBounds(Vector3 displacement)
        {
            Vector3 newPos = transform.position + displacement;

            // Ограничение по X
            float minX = _levelBounds.min.x;
            float maxX = _levelBounds.max.x;

            // Ограничение по Z
            float minZ = _levelBounds.min.z;
            float maxZ = _levelBounds.max.z;

            float oldX = transform.position.x;
            float oldZ = transform.position.z;

            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
            newPos.z = Mathf.Clamp(newPos.z, minZ, maxZ);
            newPos.y = transform.position.y; // Сохраняем высоту

            // Лог, если уперлись в границу
#if UNITY_EDITOR
            if (!Mathf.Approximately(newPos.x, oldX + displacement.x) ||
                !Mathf.Approximately(newPos.z, oldZ + displacement.z))
            {
                Debug.Log($"[DEBUG BOUNDS] Уперлись в границу! Было ({oldX}, {oldZ}), стало ({newPos.x}, {newPos.z}). Границы: X[{minX}:{maxX}], Z[{minZ}:{maxZ}]");
            }
#endif

            transform.position = newPos;
        }

        private void OnEnable()
        {
            if (_moveAction != null)
            {
                _moveAction.Enable(); // Важно! Включаем действие
                _moveAction.performed += OnMovePerformed;
                _moveAction.canceled += OnMoveCanceled;
            }
        }

        private void OnDisable()
        {
            if (_moveAction != null)
            {
                _moveAction.Disable();
                _moveAction.performed -= OnMovePerformed;
                _moveAction.canceled -= OnMoveCanceled;
            }
        }

        private void OnDestroy()
        {
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMovePerformed;
                _moveAction.canceled -= OnMoveCanceled;
            }
        }

        // ВИЗУАЛИЗАЦИЯ ГРАНИЦ В РЕДАКТОРЕ
        private void OnDrawGizmosSelected()
        {
            // Рисуем желтый каркас границ
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(_levelBounds.center, Quaternion.identity, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, _levelBounds.size);

            // Подписываем
            Gizmos.color = Color.white;
            Gizmos.DrawLine(_levelBounds.center, _levelBounds.center + Vector3.up * 2);
        }
    }
}