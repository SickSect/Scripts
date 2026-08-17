using System;
using UnityEngine;
using UnityEngine.InputSystem;
using R3;
using Core.DI;
using Core.Interaction.Interactables;

namespace Core.Interaction
{
    public class MouseInteractor : MonoBehaviour
    {
        [Header("Настройки переноса")]
        [Tooltip("Высота над землей (Y), на которой висит предмет при переноске.")]
        [SerializeField] private float _holdHeight = 1.5f;

        [Tooltip("Смещение по курсору (например, чуть выше центра).")]
        [SerializeField] private Vector2 _cursorOffset = Vector2.zero;

        private Camera _camera;
        private IInteractable _currentHovered;

        // Логика удержания
        private ICarryable _heldObject;

        public ReactiveProperty<string> HoveredPrompt { get; } = new(string.Empty);
        public event Action<IInteractable> OnHoverEnter;
        public event Action OnHoverExit;
        public event Action<IInteractable> OnClick;

        private InputAction _interactAction;

        private void Awake()
        {
            _camera = Camera.main;
        }

        public void Bind(InputAction interactAction, DIContainer container)
        {
            if (_interactAction != null) _interactAction.performed -= OnInteractPerformed;

            _interactAction = interactAction;
            if (_interactAction != null)
            {
                _interactAction.performed += OnInteractPerformed;
                _interactAction.Enable();
                Debug.Log($"[MouseInteractor] Bound to '{interactAction.name}'.");
            }
        }

        private void Update()
        {
            if (_camera == null)
            {
                if (_currentHovered != null) ClearHover();
                return;
            }

            UpdateHover();

            // Если держим предмет, обновляем его позицию ЖЕСТКО за мышью
            if (_heldObject != null)
            {
                UpdateHeldObjectPosition();
            }
        }

        private void UpdateHeldObjectPosition()
        {
            if (_heldObject == null || _heldObject.Transform == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue() + _cursorOffset;
            Ray ray = _camera.ScreenPointToRay(mousePos);

            // ВАЖНО: Мы хотим, чтобы предмет был на высоте _holdHeight.
            // Находим точку на луче, где Y == _holdHeight.
            // Формула: dist = (targetY - rayOriginY) / rayDirectionY
            float distanceToPlane = (_holdHeight - ray.origin.y) / ray.direction.y;

            if (distanceToPlane > 0)
            {
                Vector3 targetPos = ray.GetPoint(distanceToPlane);

                // МГНОВЕННОЕ перемещение без SmoothDamp для идеального следования
                _heldObject.Transform.position = targetPos;

                // Можно добавить легкий поворот к курсору, если нужно, но пока оставим как есть
            }
        }

        private void UpdateHover()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(mousePos);

            // Ищем пересечения только с объектами, которые НЕ являются тем, что мы сейчас держим
            int layerMask = ~_heldObject?.Transform.gameObject.layer ?? -1;

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, layerMask))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

                if (interactable != null)
                {
                    if (_currentHovered != interactable) SetHover(interactable);
                }
                else if (_currentHovered != null)
                {
                    ClearHover();
                }
            }
            else if (_currentHovered != null)
            {
                ClearHover();
            }
        }

        private void SetHover(IInteractable interactable)
        {
            _currentHovered = interactable;
            string prompt = (interactable as MonoBehaviour)?.gameObject.name ?? "Interact";
            HoveredPrompt.Value = prompt;
            OnHoverEnter?.Invoke(interactable);
        }

        private void ClearHover()
        {
            var old = _currentHovered;
            _currentHovered = null;
            HoveredPrompt.Value = string.Empty;
            OnHoverExit?.Invoke();
        }

        private void OnInteractPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            // Логика "Взял-Бросил"
            if (_heldObject != null)
            {
                DropObject();
                return;
            }

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                ICarryable carryable = hit.collider.GetComponentInParent<ICarryable>();
                if (carryable != null)
                {
                    PickUpObject(carryable);
                    return;
                }
            }

            // Обычное взаимодействие, если не взяли предмет
            if (_currentHovered != null)
            {
                _currentHovered.Interact(null);
                OnClick?.Invoke(_currentHovered);
            }
        }

        private void PickUpObject(ICarryable carryable)
        {
            _heldObject = carryable;

            // Отключаем физику, чтобы управлять трансформом напрямую
            if (carryable.Rigidbody != null)
            {
                carryable.Rigidbody.isKinematic = true;
                carryable.Rigidbody.linearVelocity = Vector3.zero;
                carryable.Rigidbody.angularVelocity = Vector3.zero;
                carryable.Rigidbody.useGravity = false; // Важно: отключаем гравитацию, чтобы не падал при движении
            }

            carryable.OnPickUp();
            Debug.Log($"[System] Предмет взят: {carryable.Transform.name}");
            ClearHover();
        }

        private void DropObject()
        {
            if (_heldObject == null) return;

            var obj = _heldObject;

            // Включаем физику обратно
            if (obj.Rigidbody != null)
            {
                obj.Rigidbody.isKinematic = false;
                obj.Rigidbody.useGravity = true;
                // Небольшой толчок вниз, чтобы гарантировать выход из коллизий, если застрял
                // obj.Rigidbody.AddForce(Vector3.down * 0.1f, ForceMode.VelocityChange); 
            }

            obj.OnDrop();
            Debug.Log($"[System] Предмет брошен: {obj.Transform.name}");

            _heldObject = null;
        }

        private void OnDestroy()
        {
            if (_interactAction != null) _interactAction.performed -= OnInteractPerformed;
            HoveredPrompt.Dispose();
            if (_heldObject != null) DropObject();
        }
    }
}