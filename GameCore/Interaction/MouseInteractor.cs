using System;
using UnityEngine;
using UnityEngine.InputSystem;
using R3;
using Core.DI;
using Core.Interaction.Interactables;
using Core.Common;

namespace Core.Interaction
{
    /// <summary>
    /// Обрабатывает клики мыши и перетаскивание объектов.
    /// ЛОГИКА: 
    /// 1. Подбор: Отключаем коллайдер предмета -> Зоны его игнорируют.
    /// 2. Бросок: Включаем коллайдер -> Если внутри зоны, срабатывает OnTriggerEnter (один раз).
    /// </summary>
    public class MouseInteractor : MonoBehaviour
    {
        [Header("Настройки перетаскивания")]
        [Tooltip("Высота над землей (Y), на которой держится предмет.")]
        [SerializeField] private float _holdHeight = 1.5f;

        [Tooltip("Скорость следования предмета за мышью.")]
        [SerializeField] private float _holdSmoothSpeed = 25f;

        [Header("Слои")]
        [Tooltip("Слои, которые рейкаст должен ИГНОРИРОВАТЬ (например, DropZoneLayer).")]
        [SerializeField] private LayerMask _ignoreLayers = 0;

        [Header("Отладка")]
        [Tooltip("Рисовать луч отладки в редакторе?")]
        [SerializeField] private bool _debugRaycast = true;

        private Camera _camera;
        private IInteractable _currentHovered;

        private Vector3 _holdVelocity;
        private Collider _heldColliderCache;

        public ReactiveProperty<string> HoveredPrompt { get; } = new(string.Empty);
        public event Action<IInteractable> OnHoverEnter;
        public event Action OnHoverExit;
        public event Action<IInteractable> OnClick;

        private InputAction _interactAction;

        private Boolean _isHeldItem = false;
        private Carryable _takenItem;

        private void Awake()
        {
            _camera = Camera.main;
        }

        public void Bind(InputAction interactAction, DIContainer container)
        {
            if (_interactAction != null)
                _interactAction.performed -= OnInteractPerformed;

            _interactAction = interactAction;

            if (_interactAction != null)
            {
                _interactAction.performed += OnInteractPerformed;
                CoreLog.Debug($"[MouseInteractor] Привязан к действию: {interactAction.name}");
            }
        }

        private void Update()
        {
            if (_camera == null)
            {
                if (_currentHovered != null) ClearHover();
                return;
            }

            // Обновляем подсветку только если ничего не держим
            if (_takenItem == null)
            {
                UpdateHover();
            }
        }

        private void FixedUpdate()
        {
            // Если держим объект — двигаем его за мышью каждый кадр
            if (_takenItem != null)
            {
                MoveObjectWithMouse();
            }
        }

        private void UpdateHover()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(mousePos);

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
            Vector2 mousePos = Mouse.current.position.value;

            Ray ray = _camera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Carryable carryable = hit.collider.GetComponentInParent<Carryable>();
                if (carryable == null)
                {
                    Debug.Log("[OnInteractPerformed] нет предмета взаимодействия");
                    return;
                }
                Debug.Log("[OnInteractPerformed] взаимодействуем с предметом " + carryable.name);

                if (carryable.isPickupble())
                {
                    Debug.Log("[OnInteractPerformed] мы можем поднять предмет" + carryable.name);
                    if (_isHeldItem)
                    {
                        _isHeldItem = false; // переводим состояние в "бросили объект и руки пусты"
                        DropObject();
                    }
                    else if (!_isHeldItem)
                    {
                        _isHeldItem = true; // переводим состояние в "мы держим объект"
                        Carryable takenItem = hit.collider.GetComponent<Carryable>(); // берем сам объект
                        if (carryable != null)
                        {
                            Debug.Log("[OnInteractPerformed] Поднимаем " + carryable.name);
                            PickUpObject(takenItem); // поднимаем объект
                        }
                    }
                }
                else
                {
                    Debug.Log("[OnInteractPerformed] мы НЕ можем поднять предмет" + carryable.name);
                }
            }
            else
            {
                Debug.Log("[OnInteractPerformed] ничего не произошло, предмета нет");
            }

        }

        private void PickUpObject(Carryable carryable)
        {
            Debug.Log("[OnInteractPerformed] PickUpObject " + carryable.name);
            _takenItem = carryable;
            if (_takenItem != null)
            {
                carryable.Rigidbody.isKinematic = true; // Главное: отключаем симуляцию
                carryable.Rigidbody.linearVelocity = Vector3.zero; // зануляем скорость
                carryable.Rigidbody.angularVelocity = Vector3.zero;
            }
            ClearHover();
            carryable.OnPickUp();
        }

        private void DropObject()
        {
            if (_takenItem == null) return;
            Carryable carryable = _takenItem as Carryable;
            Debug.Log("[OnInteractPerformed] DropObject " + carryable.name);
            // ВКЛЮЧАЕМ ФИЗИКУ ОБРАТНО
            if (carryable.Rigidbody != null)
                carryable.Rigidbody.isKinematic = false; // Включаем симуляцию (предмет упадет)
            // Визуальный эффект
            carryable.OnDrop();
            _takenItem = null;
            _heldColliderCache = null;
        }

        private void MoveObjectWithMouse()
        {
            Vector2 mousePos = Mouse.current.position.value;
            Ray ray = _camera.ScreenPointToRay(mousePos);
            Plane groundPlane = new Plane(Vector3.up, -_holdHeight);
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 targetPos = ray.GetPoint(enter);
                _takenItem.Transform.position = Vector3.SmoothDamp(
                _takenItem.Transform.position,
                targetPos,
                ref _holdVelocity,
                0.1f // Время сглаживания (меньше = быстрее/жестче)
            );
            }
        }

        private void OnDestroy()
        {
            if (_interactAction != null)
                _interactAction.performed -= OnInteractPerformed;

            HoveredPrompt.Dispose();

            if (_takenItem != null)
                DropObject();
        }

        // Визуализация луча в редакторе постоянно, если включена отладка
        private void OnDrawGizmos()
        {
            if (!_debugRaycast || _camera == null) return;

            Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width / 2, Screen.height / 2);
            Ray ray = _camera.ScreenPointToRay(mousePos);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * 50f);
        }
    }
}