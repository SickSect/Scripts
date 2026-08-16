using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Player
{
    /// <summary>
    /// Движение игрока (3D, XZ) на СКОРОСТИ Rigidbody (velocity-based) — плавно, без рывков.
    ///
    /// ПОЧЕМУ velocity, а не MovePosition:
    ///   Тело НЕ кинематическое (есть гравитация). MovePosition рассчитан на кинематические
    ///   тела и на обычном теле каждый физ-шаг «телепортирует» капсулу, конфликтуя с решателем
    ///   столкновений и гравитацией — при перемещении это даёт микро-дёрганье (у стены его нет,
    ///   т.к. движение гасится). Управление скоростью двигает тело непрерывно, а PhysX сам
    ///   скользит вдоль стен — ручная проекция по нормалям стен больше не нужна.
    ///
    /// Направление движения — ОТ КАМЕРЫ (yaw из PlayerLook), тело доворачивается к движению.
    ///
    /// ВЫНОСЛИВОСТЬ (опционально, из PlayerInitStep): спринт тратит выносливость, иначе она
    /// восстанавливается. При нуле спринт блокируется до порога. Нет стата — спринт свободен.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Скорости (units/sec)")]
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _sprintSpeed = 6f;

        [Header("Плавность (units/sec^2)")]
        [SerializeField] private float _acceleration = 45f;
        [SerializeField] private float _deceleration = 55f;

        [Header("Поворот тела к направлению движения")]
        [Tooltip("Скорость доворота тела к вектору движения (град/сек).")]
        [SerializeField] private float _turnSpeed = 720f;

        [Header("Выносливость (в секунду)")]
        [SerializeField] private float _sprintStaminaDrain = 15f;
        [SerializeField] private float _staminaRegen = 10f;
        [Tooltip("Мин. выносливость, чтобы снова начать спринт после обнуления.")]
        [SerializeField] private float _staminaSprintThreshold = 10f;
        [Tooltip("Логировать текущую выносливость в консоль (при изменении на единицу).")]
        [SerializeField] private bool _logStamina = false;

        private Rigidbody _rb;
        private Vector2 _moveInput;
        private Vector3 _velocity;      // горизонтальная скорость (y всегда 0)
        private bool _sprinting;
        private InputAction _moveAction;

        // Источник направления камеры (для камеро-зависимого движения).
        private PlayerLook _look;

        // Стат выносливости (опционально; привязывается из PlayerInitStep).
        private Core.Stats.Stat _stamina;
        private bool _staminaExhausted; // true, пока не восстановились до порога
        private int _lastLoggedStamina = -1;

        public ReactiveProperty<Vector3> Position { get; } = new(Vector3.zero);

        public float CurrentSpeed01 =>
            _sprintSpeed <= 0f ? 0f : Mathf.Clamp01(_velocity.magnitude / _sprintSpeed);

        public Vector3 WorldVelocity => _velocity;
        public bool IsSprinting => _sprinting;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            _rb.freezeRotation = true;
            // Скоростью по X/Z рулим сами (свои разгон/торможение), поэтому линейное затухание
            // не нужно — иначе PhysX «съедает» скорость и персонаж не добирает до целевой.
            _rb.linearDamping = 0f;

            _look = GetComponent<PlayerLook>();
            Position.Value = transform.position;
        }

        public void BindInput(InputAction moveAction)
        {
            _moveAction = moveAction;
            _moveAction.performed += OnMove;
            _moveAction.canceled += OnMoveCanceled;
        }

        /// <summary>Привязать стат выносливости (из PlayerInitStep). null = спринт без ограничений.</summary>
        public void BindStamina(Core.Stats.Stat stamina) => _stamina = stamina;

        private void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();
        private void OnMoveCanceled(InputAction.CallbackContext ctx) => _moveInput = Vector2.zero;

        private void Update()
        {
            var kb = Keyboard.current;
            bool wantSprint = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
            bool moving = _moveInput.sqrMagnitude > 0.01f;

            _sprinting = wantSprint && moving && HasStaminaToSprint();

            UpdateStamina(dt: Time.deltaTime);
            Position.Value = transform.position;
        }

        private bool HasStaminaToSprint()
        {
            if (_stamina == null) return true;      // нет системы выносливости — спринт свободен
            if (_staminaExhausted) return false;    // ждём восстановления до порога
            return !_stamina.IsZero;
        }

        private void UpdateStamina(float dt)
        {
            if (_stamina == null) return;

            if (_sprinting)
            {
                _stamina.Modify(-_sprintStaminaDrain * dt);
                if (_stamina.IsZero) _staminaExhausted = true; // выдохлись — блок до порога
            }
            else
            {
                _stamina.Modify(_staminaRegen * dt);
                if (_staminaExhausted && _stamina.Value.Value >= _staminaSprintThreshold)
                    _staminaExhausted = false; // восстановились — снова можно спринтовать
            }

            if (_logStamina)
            {
                int cur = Mathf.RoundToInt(_stamina.Value.Value);
                if (cur != _lastLoggedStamina)
                {
                    _lastLoggedStamina = cur;
                    Core.Common.CoreLog.Debug($"[Stamina] {cur}/{Mathf.RoundToInt(_stamina.Max)}" +
                                              (_staminaExhausted ? " (выдохся)" : ""));
                }
            }
        }

        private void FixedUpdate()
        {
            // Направление ОТ КАМЕРЫ (по yaw), а не от тела — иначе связка «Mafia 1».
            float yaw = _look != null ? _look.Yaw : transform.eulerAngles.y;
            Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 dir = yawRot * (Vector3.right * _moveInput.x + Vector3.forward * _moveInput.y);
            dir.y = 0f;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            // Тело плавно доворачивается в сторону движения (не в сторону камеры).
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion faceMove = Quaternion.LookRotation(dir, Vector3.up);
                _rb.MoveRotation(Quaternion.RotateTowards(
                    _rb.rotation, faceMove, _turnSpeed * Time.fixedDeltaTime));
            }

            // Плавный разгон/торможение горизонтальной скорости.
            float targetSpeed = _sprinting ? _sprintSpeed : _walkSpeed;
            Vector3 targetVel = dir * targetSpeed;
            float rate = dir.sqrMagnitude > 0.01f ? _acceleration : _deceleration;
            _velocity = Vector3.MoveTowards(_velocity, targetVel, rate * Time.fixedDeltaTime);

            // Рулим только X/Z, вертикаль (гравитацию/падение) оставляем физике.
            // PhysX сам скользит капсулой вдоль стен — ручная проекция по нормалям не нужна.
            Vector3 v = _rb.linearVelocity;
            v.x = _velocity.x;
            v.z = _velocity.z;
            _rb.linearVelocity = v;
        }

        public void Teleport(Vector3 worldPos)
        {
            transform.position = worldPos;
            if (_rb != null)
            {
                _rb.position = worldPos;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            _velocity = Vector3.zero;
            Position.Value = worldPos;
        }

        private void OnDestroy()
        {
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMove;
                _moveAction.canceled -= OnMoveCanceled;
            }
            Position.Dispose();
        }
    }
}