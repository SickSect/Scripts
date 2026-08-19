using System;
using UnityEngine;
using UnityEngine.InputSystem;
using R3;
using Core.DI;
using Core.Interaction.Interactables;
using Core.Carry;

namespace Core.Interaction
{
    public class MouseInteractor : MonoBehaviour
    {
        [Header("Настройки перетаскивания")]
        [SerializeField] private float _holdHeight = 1.5f; // Высота над землей (Y)
        [SerializeField] private float _holdSmoothSpeed = 20f;

        [Header("Слои")]
        [Tooltip("Слои, которые рейкаст должен ИГНОРИРОВАТЬ (например, DropZoneLayer).")]
        [SerializeField] private LayerMask _ignoreLayers = 0;

        private Camera _camera;
        private IInteractable _currentHovered;

        private ICarryable _heldObject;
        private Vector3 _holdVelocity;

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
            UpdateHeldObjectPosition();
        }

        private void UpdateHover()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(mousePos);

            // ВАЖНО: Передаем маску игнорирования в рейкаст
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~_ignoreLayers.value))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

                if (interactable != null)
                {
                    if (_currentHovered != interactable) SetHover(interactable);
                }
                else
                {
                    if (_currentHovered != null) ClearHover();
                }
            }
            else
            {
                if (_currentHovered != null) ClearHover();
            }
        }

        private void UpdateHeldObjectPosition()
        {
            if (_heldObject == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(mousePos);

            // Точка на высоте holdHeight от земли (плоскость Y = holdHeight)
            // Плоскость: нормаль (0,1,0), расстояние до начала координат = holdHeight
            Plane groundPlane = new Plane(Vector3.up, -_holdHeight);

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 targetPos = ray.GetPoint(enter);
                // Мгновенное перемещение или плавное? 
                // Для точного следования за курсором лучше мгновенно, либо очень быстро
                _heldObject.Transform.position = Vector3.SmoothDamp(
                    _heldObject.Transform.position,
                    targetPos,
                    ref _holdVelocity,
                    1f / _holdSmoothSpeed
                );
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
            if (_camera == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();

            // ЛОГИКА "ВЗЯЛ-БРОСИЛ"
            if (_heldObject != null)
            {
                DropObject();
                return;
            }

            Ray ray = _camera.ScreenPointToRay(mousePos);

            // Если мы пытаемся взять предмет (ЛКМ по объекту)
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                ICarryable holdable = hit.collider.GetComponentInParent<ICarryable>();

                if (holdable != null)
                {
                    // ПРОВЕРКА: Если предмет внутри DropZone, сообщаем зоне, что мы его забираем
                    var zones = Physics.OverlapSphere(holdable.Transform.position, 0.1f); // Ищем коллайдеры рядом
                    foreach (var zoneCol in zones)
                    {
                        var dropZone = zoneCol.GetComponent<DropZone>();
                        if (dropZone != null)
                        {
                            dropZone.ReleaseItemIfHeld(holdable);
                        }
                    }

                    // Теперь берем предмет
                    PickUpObject(holdable);
                    return;
                }
            }

            // Обычное взаимодействие
            if (_currentHovered != null)
            {
                _currentHovered.Interact(null);
                OnClick?.Invoke(_currentHovered);
            }
        }

        private void PickUpObject(ICarryable holdable)
        {
            _heldObject = holdable;
            if (holdable.Rigidbody != null)
            {
                holdable.Rigidbody.isKinematic = true;
                holdable.Rigidbody.angularVelocity = Vector3.zero;
            }
            holdable.OnPickUp();
            ClearHover();
        }

        private void DropObject()
        {
            if (_heldObject == null) return;
            var obj = _heldObject;

            if (obj.Rigidbody != null)
            {
                obj.Rigidbody.isKinematic = false;
            }

            obj.OnDrop();
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