using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Player
{
    public class PointAndClickCamera : MonoBehaviour
    {
        [Header("Скорость движения")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _edgeScrollThreshold = 15f;
        [SerializeField] private float _edgeScrollSpeed = 8f;

        [Header("Границы уровня")]
        [SerializeField] private Bounds _levelBounds = new Bounds(Vector3.zero, new Vector3(20, 0, 20));

        private Camera _cam;
        private Vector2 _moveInput;
        private InputAction _moveAction;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;

            if (_levelBounds.extents == Vector3.zero)
            {
                _levelBounds = new Bounds(transform.position, new Vector3(100, 1, 100));
            }
        }

        public void BindInput(InputAction moveAction)
        {
            if (_moveAction != null)
            {
                _moveAction.Disable();
                _moveAction.performed -= OnMove;
                _moveAction.canceled -= OnMoveCanceled;
            }

            _moveAction = moveAction;
            if (_moveAction != null)
            {
                _moveAction.performed += OnMove;
                _moveAction.canceled += OnMoveCanceled;
                _moveAction.Enable();
                Debug.Log($"[Camera] Action '{_moveAction.name}' enabled.");
                _moveInput = _moveAction.ReadValue<Vector2>();
            }
        }

        private void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();
        private void OnMoveCanceled(InputAction.CallbackContext ctx) => _moveInput = Vector2.zero;

        private void Update()
        {
            if (_moveAction == null || !_moveAction.enabled) return;

            // Читаем актуальное значение каждый кадр для плавности
            Vector2 currentVal = _moveAction.ReadValue<Vector2>();
            if (currentVal != _moveInput) _moveInput = currentVal;

            HandleMovement();
        }

        private void HandleMovement()
        {
            Vector3 moveDir = Vector3.zero;

            // 1. Движение от клавиатуры (WASD)
            // Важно: Мы берем направление вперед/вправо ОТ КАМЕРЫ, чтобы W всегда было "вверх по экрану"
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                // Получаем векторы направлений камеры (только горизонтальные)
                Vector3 forward = _cam.transform.forward;
                Vector3 right = _cam.transform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                // Складываем векторы: Y инпута (W/S) * Вперед + X инпута (A/D) * Вправо
                moveDir = (forward * _moveInput.y) + (right * _moveInput.x);
                moveDir.Normalize();
            }

            // 2. Движение от краев экрана (Edge Scrolling)
            Vector2 mousePos = Mouse.current.position.ReadValue();
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            if (mousePos.x < _edgeScrollThreshold) moveDir += Vector3.left; // Или правее относительно камеры? Обычно влево по экрану
            if (mousePos.x > screenWidth - _edgeScrollThreshold) moveDir += Vector3.right;
            if (mousePos.y < _edgeScrollThreshold) moveDir += Vector3.back; // Вниз экрана = назад
            if (mousePos.y > screenHeight - _edgeScrollThreshold) moveDir += Vector3.forward; // Вверх экрана = вперед

            // Нормализуем итоговое направление, если оно больше 1 (чтобы не бегал быстрее по диагонали)
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            if (moveDir.sqrMagnitude > 0.01f)
            {
                // Определяем скорость (быстрее у краев)
                bool isEdgeScrolling = (mousePos.x < _edgeScrollThreshold || mousePos.x > screenWidth - _edgeScrollThreshold ||
                                        mousePos.y < _edgeScrollThreshold || mousePos.y > screenHeight - _edgeScrollThreshold);

                float speed = isEdgeScrolling ? _edgeScrollSpeed : _moveSpeed;

                Vector3 displacement = moveDir * (speed * Time.deltaTime);
                MoveWithBounds(displacement);
            }
        }

        private void MoveWithBounds(Vector3 displacement)
        {
            Vector3 newPos = transform.position + displacement;
            newPos.x = Mathf.Clamp(newPos.x, _levelBounds.min.x, _levelBounds.max.x);
            newPos.z = Mathf.Clamp(newPos.z, _levelBounds.min.z, _levelBounds.max.z);
            newPos.y = transform.position.y; // Сохраняем высоту
            transform.position = newPos;
        }

        private void OnDestroy()
        {
            if (_moveAction != null)
            {
                _moveAction.Disable();
                _moveAction.performed -= OnMove;
                _moveAction.canceled -= OnMoveCanceled;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Bounds drawBounds = (_levelBounds.extents == Vector3.zero)
                ? new Bounds(transform.position, new Vector3(20, 1, 20))
                : _levelBounds;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(drawBounds.center, drawBounds.size);
        }
    }
}