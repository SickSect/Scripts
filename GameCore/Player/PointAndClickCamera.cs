using UnityEngine;
using UnityEngine.InputSystem;
using Core.Input;

namespace Core.Player
{
    /// <summary>
    /// Простая камера для режима point-and-click: следует за позицией мыши на уровне,
    /// позволяет перемещаться по уровню клавишами WASD/стрелками или двигая мышью у краёв экрана.
    /// 
    /// Вешается на основную камеру сцены. Игрок не спавнится — камера управляется напрямую.
    /// Использует новую систему ввода Unity (Input System) с событиями performed/canceled.
    /// </summary>
    public class PointAndClickCamera : MonoBehaviour
    {
        [Header("Перемещение клавишами")]
        [SerializeField] private float _moveSpeed = 10f;
        [SerializeField] private bool _allowKeyboardMove = true;

        [Header("Перемещение к краям экрана")]
        [SerializeField] private bool _allowEdgeScroll = true;
        [SerializeField] private float _edgeThickness = 20f;
        [SerializeField] private float _edgeScrollSpeed = 8f;

        [Header("Ограничения камеры")]
        [SerializeField] private bool _useBounds = false;
        [SerializeField] private Vector2 _minBounds = new Vector2(-50f, -50f);
        [SerializeField] private Vector2 _maxBounds = new Vector2(50f, 50f);

        [Header("Зум колесом")]
        [SerializeField] private bool _allowZoom = true;
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _minSize = 5f;
        [SerializeField] private float _maxSize = 30f;

        private Camera _camera;
        private Vector3 _velocity;
        
        // Новая система ввода
        private InputAction _moveAction;
        private Vector2 _moveInput;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                Debug.LogError("[PointAndClickCamera] Камера не найдена на этом GameObject!");
            }
            
            // Инициализация действия движения, если оно еще не назначено через BindInput
            if (_moveAction == null)
            {
                // Попытка найти стандартное действие "Move" в дефолтном ассете
                var playerInput = GetComponent<PlayerInput>();
                if (playerInput != null)
                {
                    _moveAction = playerInput.actions["Move"];
                    SetupInputListeners();
                }
            }
        }

        /// <summary>
        /// Привязка действия движения из новой системы ввода Unity.
        /// Вызывать после инициализации Input Actions.
        /// </summary>
        public void BindInput(InputAction moveAction)
        {
            // Отписываемся от старого действия, если было
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMovePerformed;
                _moveAction.canceled -= OnMoveCanceled;
            }

            _moveAction = moveAction;
            SetupInputListeners();
            
            // Если действие уже активно, считываем текущее значение
            if (_moveAction != null && _moveAction.enabled)
            {
                _moveInput = _moveAction.ReadValue<Vector2>();
            }
        }

        private void SetupInputListeners()
        {
            if (_moveAction == null) return;
            
            _moveAction.performed += OnMovePerformed;
            _moveAction.canceled += OnMoveCanceled;
        }

        // Обработка нажатия и изменения значения
        private void OnMovePerformed(InputAction.CallbackContext ctx) 
        {
            _moveInput = ctx.ReadValue<Vector2>();
        }

        // Обработка отпускания клавиш
        private void OnMoveCanceled(InputAction.CallbackContext ctx) 
        {
            _moveInput = Vector2.zero;
        }

        private void Update()
        {
            HandleKeyboardMove();
            HandleEdgeScroll();
            HandleZoom();
            ApplyBounds();
        }

        private void HandleKeyboardMove()
        {
            if (!_allowKeyboardMove) return;

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                // _moveInput.x = влево/вправо, _moveInput.y = вверх/вниз
                Vector3 move = new Vector3(_moveInput.x, _moveInput.y, 0);
                transform.position += move.normalized * _moveSpeed * Time.deltaTime;
            }
        }

        private void HandleEdgeScroll()
        {
            if (!_allowEdgeScroll) return;

            Vector3 move = Vector3.zero;
            var mousePos = Mouse.current.position.ReadValue();
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;

            if (mousePos.x < _edgeThickness) move += Vector3.left;
            if (mousePos.x > screenWidth - _edgeThickness) move += Vector3.right;
            if (mousePos.y < _edgeThickness) move += Vector3.down;
            if (mousePos.y > screenHeight - _edgeThickness) move += Vector3.up;

            if (move.sqrMagnitude > 0)
            {
                transform.position += move.normalized * _edgeScrollSpeed * Time.deltaTime;
            }
        }

        private void HandleZoom()
        {
            if (!_allowZoom || _camera.orthographic == false) return;

            var scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
            
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float newSize = _camera.orthographicSize - scroll * _zoomSpeed;
                _camera.orthographicSize = Mathf.Clamp(newSize, _minSize, _maxSize);
            }
        }

        private void ApplyBounds()
        {
            if (!_useBounds) return;

            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, _minBounds.x, _maxBounds.x);
            pos.y = Mathf.Clamp(pos.y, _minBounds.y, _maxBounds.y);
            transform.position = pos;
        }

        private void OnEnable()
        {
            // Восстанавливаем подписку при активации
            if (_moveAction != null)
            {
                SetupInputListeners();
            }
        }

        private void OnDisable()
        {
            // Важно отключать подписку при дезактивации объекта
            if (_moveAction != null)
            {
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
    }
}
