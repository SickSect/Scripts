using System;
using UnityEngine;
using UnityEngine.InputSystem;
using R3;
using Core.DI;
using Core.Common;

namespace Core.Interaction
{
    /// <summary>
    /// Обрабатывает клики мыши по интерактивным объектам.
    /// ТЕКУЩАЯ ВЕРСИЯ: С упором на отладку координат и механику "Взял-Бросил".
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
        private IHoldable _heldObject;
        private Vector3 _holdVelocity;

        // События для UI
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
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractPerformed;
            }

            _interactAction = interactAction;

            if (_interactAction != null)
            {
                _interactAction.performed += OnInteractPerformed;
                CoreLog.Debug($"[MouseInteractor] Bind: {interactAction.name}, Enabled={interactAction.enabled}");
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
            UpdateHeldObject();
        }

        private void UpdateHover()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(mousePos);

            // Лог для отладки рейкаста (можно закомментировать, если слишком много шума)
            // Debug.Log($"[Raycast] Pos: {mousePos}, Dir: {ray.direction}");

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                // Пробуем найти интерактивный объект
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

                // Если это не интерактивный объект, но мы держим что-то другое, просто игнорируем ховер
                // Но если мы хотим видеть подсказку только над интерактивными - ок.

                if (interactable != null)
                {
                    if (_currentHovered != interactable)
                    {
                        SetHover(interactable);
                    }
                }
                else
                {
                    if (_currentHovered != null)
                    {
                        ClearHover();
                    }
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

            // Получаем текущую позицию мыши в мире
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(mousePos);

            // Цель: точка на расстоянии _holdHeightOffset от камеры вдоль луча
            Vector3 targetPos = ray.GetPoint(_holdHeightOffset);

            // Плавное движение к цели (чтобы не дергалось)
            Vector3 newPos = Vector3.SmoothDamp(_heldObject.Transform.position, targetPos, ref _holdVelocity, 1f / _holdSmoothSpeed);

            _heldObject.Transform.position = newPos;

            // Важно: оставляем вращение как есть или сбрасываем, чтобы предмет не крутился wildly
            // Можно добавить выравнивание, если нужно: _heldObject.Transform.rotation = Quaternion.identity;
        }

        private void SetHover(IInteractable interactable)
        {
            _currentHovered = interactable;
            string prompt = (interactable as MonoBehaviour)?.gameObject.name ?? "Interact";
            HoveredPrompt.Value = prompt;
            OnHoverEnter?.Invoke(interactable);
            // CoreLog.Debug($"[HOVER] {prompt}");
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
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // ЛОГИКА "ВЗЯЛ-БРОСИЛ"
            if (_heldObject != null)
            {
                // Если уже держим предмет - бросаем его
                DropObject();
                return;
            }

            // Если не держим - пытаемся взять
            Ray ray = _camera.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                IHoldable holdable = hit.collider.GetComponentInParent<IHoldable>();
                if (holdable != null)
                {
                    CoreLog.Debug($"[PICKUP] Попытка взять: {holdable.Transform.name}");
                    PickUpObject(holdable);
                    return;
                }
            }

            // ЛОГИКА ОБЫЧНОГО ВЗАИМОДЕЙСТВИЯ (если не взяли предмет)
            if (_currentHovered != null)
            {
                CoreLog.Debug($"[INTERACT] Клик по: {_currentHovered.GetType().Name}");
                _currentHovered.Interact(new InteractionContext(this.gameObject, null));
                OnClick?.Invoke(_currentHovered);
            }
            else
            {
                CoreLog.Debug("[CLICK] Пустой клик (ничего не задето)");
            }
        }

        private void PickUpObject(IHoldable holdable)
        {
            _heldObject = holdable;

            // Отключаем физику, чтобы предмет не падал и не коллизился
            if (holdable.Rigidbody != null)
            {
                holdable.Rigidbody.isKinematic = true;
                holdable.Rigidbody.linearVelocity = Vector3.zero;
                holdable.Rigidbody.angularVelocity = Vector3.zero;
            }

            holdable.OnPickUp();
            CoreLog.Debug($"[SYSTEM] Предмет взят: {holdable.Transform.name}");

            // Сбрасываем ховер, чтобы не мелькала подсказка на предмете, который мы тащим
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
                // Можно добавить небольшой импульс вперед, если нужно "бросать"
                // obj.Rigidbody.velocity = _holdVelocity * 2f; 
            }

            obj.OnDrop();
            CoreLog.Debug($"[SYSTEM] Предмет брошен: {obj.Transform.name}");

            _heldObject = null;
        }

        private void OnDestroy()
        {
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractPerformed;
            }
            HoveredPrompt.Dispose();

            // Если игра выключается, а предмет в руке - бросаем его (на всякий случай)
            if (_heldObject != null) DropObject();
        }
    }
}