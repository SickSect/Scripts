using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Player
{
    /// <summary>
    /// Простая камера для режима point-and-click: следует за позицией мыши на уровне,
    /// позволяет перемещаться по уровню клавишами WASD/стрелками или двигая мышью у краёв экрана.
    /// 
    /// Вешается на основную камеру сцены. Игрок не спавнится — камера управляется напрямую.
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

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                Debug.LogError("[PointAndClickCamera] Камера не найдена на этом GameObject!");
            }
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

            Vector3 move = Vector3.zero;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrow.isPressed) move += Vector3.up;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrow.isPressed) move += Vector3.down;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrow.isPressed) move += Vector3.left;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrow.isPressed) move += Vector3.right;
            }

            if (move.sqrMagnitude > 0)
            {
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
    }
}
