using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Player
{
    public class PointAndClickCamera : MonoBehaviour
    {
        [Header("Скорость движения")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _edgeScrollThreshold = 10f;
        [SerializeField] private float _edgeScrollSpeed = 8f;

        [Header("Зум")]
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _minZoom = 5f;
        [SerializeField] private float _maxZoom = 20f;

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
                _levelBounds = new Bounds(Vector3.zero, new Vector3(100, 1, 100));
            }
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
                Debug.Log("[PointAndClickCamera] Input привязан.");
            }
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();
        private void OnMoveCanceled(InputAction.CallbackContext ctx) => _moveInput = Vector2.zero;

        private void Update()
        {
            HandleMovement();
            HandleZoom();
        }

        private void HandleMovement()
        {
            Vector3 moveDir = Vector3.zero;

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                moveDir += new Vector3(_moveInput.x, 0, _moveInput.y);
            }

            Vector3 mousePos = Mouse.current.position.ReadValue();
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            if (mousePos.x < _edgeScrollThreshold) moveDir += Vector3.left;
            if (mousePos.x > screenWidth - _edgeScrollThreshold) moveDir += Vector3.right;
            if (mousePos.y < _edgeScrollThreshold) moveDir += Vector3.back;
            if (mousePos.y > screenHeight - _edgeScrollThreshold) moveDir += Vector3.forward;

            if (moveDir.sqrMagnitude > 0)
            {
                moveDir.Normalize();
                float speed = (moveDir.magnitude > 1.5f) ? _edgeScrollSpeed : _moveSpeed;
                Vector3 displacement = moveDir * (speed * Time.deltaTime);
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

                // Clamp без лишних логов
                float clampedZoom = Mathf.Clamp(newZoom, _minZoom, _maxZoom);

                if (clampedZoom != _cam.orthographicSize)
                {
                    _cam.orthographicSize = clampedZoom;
                }
            }
        }

        private void MoveWithBounds(Vector3 displacement)
        {
            Vector3 newPos = transform.position + displacement;
            newPos.x = Mathf.Clamp(newPos.x, _levelBounds.min.x, _levelBounds.max.x);
            newPos.z = Mathf.Clamp(newPos.z, _levelBounds.min.z, _levelBounds.max.z);
            newPos.y = transform.position.y;
            transform.position = newPos;
        }

        private void OnEnable()
        {
            if (_moveAction != null)
            {
                _moveAction.performed += OnMovePerformed;
                _moveAction.canceled += OnMoveCanceled;
            }
        }

        private void OnDisable()
        {
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