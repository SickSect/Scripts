using System;
using UnityEngine;
using UnityEngine.InputSystem;
using R3;
using Core.DI;
using Core.Interaction.Interactables;
using Core.Carry;

namespace Core.Interaction
{
    /// <summary>
    /// Обрабатывает клики мыши и перетаскивание объектов.
    /// </summary>
    public class MouseInteractor : MonoBehaviour
    {
        [Header("Настройки перетаскивания")]
        [Tooltip("Высота, на которой держится предмет относительно камеры")]
        [SerializeField] private float _holdHeightOffset = 2.0f;
        [Tooltip("С какой скоростью предмет следует за мышью (плавность)")]
        [SerializeField] private float _holdSmoothSpeed = 15f;

        private Camera _camera;
        private IInteractable _currentHovered;

        // Логика удержания предмета
        private ICarryable _heldObject; // Используем наш интерфейс ICarryable
        private Vector3 _holdVelocity;

        // События для UI
        public ReactiveProperty<string> HoveredPrompt { get; } = new(string.Empty);
        public event Action<IInteractable> OnHoverEnter;
        public event Action OnHoverExit;
        public event Action<IInteractable> OnClick;

        private InputAction _interactAction;

        private void Awake()
        {
            // Пытаемся найти камеру сразу. Если не нашли - попробуем в Update перед первым использованием.
            _camera = Camera.main;
        }

        public void Bind(InputAction interactAction, DIContainer container)
        {
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractPerformed;
            }

            _interactAction = interactAction;

            if (_interactAction != null)
            {
                _interactAction.performed += OnInteractPerformed;
                Debug.Log($"[MouseInteractor] Bind: {interactAction.name}, Enabled={interactAction.enabled}");
            }
        }

        private void Update()
        {
            // Если камеры нет, пытаемся найти её снова (вдруг она появилась)
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return; // Всё ещё нет - выходим
            }

            UpdateHover();
            UpdateHeldObject();
        }

        private void UpdateHover()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

                if (interactable != null)
                {
                    if (_currentHovered != interactable)
                    {
                        SetHover(interactable);
                    }
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

        private void UpdateHeldObject()
        {
            if (_heldObject == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(mousePos);

            // Точка, куда должен стремиться объект (на высоте holdHeightOffset от камеры вдоль луча)
            Vector3 targetPos = ray.GetPoint(_holdHeightOffset);

            // Плавное следование
            Vector3 newPos = Vector3.SmoothDamp(_heldObject.Transform.position, targetPos, ref _holdVelocity, 1f / _holdSmoothSpeed);

            _heldObject.Transform.position = newPos;
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
            // ВАЖНО: Проверка камеры перед использованием
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    Debug.LogWarning("[MouseInteractor] Клик без камеры! Действие пропущено.");
                    return;
                }
            }

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Debug.Log($"[CLICK] ScreenPos: {mousePos}");

            // 1. ЛОГИКА "БРОСИТЬ" (если уже держим объект)
            if (_heldObject != null)
            {
                DropObject();
                return;
            }

            // 2. ЛОГИКА "ВЗЯТЬ" (проверяем, не навели ли на переносимый объект)
            Ray ray = _camera.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                // Ищем интерфейс ICarryable (наш новый стандарт)
                ICarryable carryable = hit.collider.GetComponentInParent<ICarryable>();

                if (carryable != null)
                {
                    Debug.Log($"[PICKUP] Попытка взять: {carryable.Transform.name}");
                    PickUpObject(carryable);
                    return;
                }

                // 3. ЛОГИКА ОБЫЧНОГО ВЗАИМОДЕЙСТВИЯ (если не взяли предмет)
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    Debug.Log($"[INTERACT] Клик по: {interactable.GetType().Name}");
                    interactable.Interact();
                    OnClick?.Invoke(interactable);
                    return;
                }
            }

            Debug.Log("[CLICK] Пустой клик (ничего не задето или объект не интерактивный)");
        }

        private void PickUpObject(Carryable carryable)
        {
            _heldObject = carryable;

            if (carryable.Rigidbody != null)
            {
                carryable.Rigidbody.isKinematic = true;
                carryable.Rigidbody.velocity = Vector3.zero;
                carryable.Rigidbody.angularVelocity = Vector3.zero;
            }

            carryable.OnPickUp();
            Debug.Log($"[SYSTEM] Предмет взят: {carryable.Transform.name}");

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
            Debug.Log($"[SYSTEM] Предмет брошен: {obj.Transform.name}");

            _heldObject = null;
        }

        private void OnDestroy()
        {
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractPerformed;
            }
            HoveredPrompt.Dispose();

            if (_heldObject != null) DropObject();
        }
    }
}