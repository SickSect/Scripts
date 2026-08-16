using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// «Сочность» камеры поверх код-камеры ThirdPersonCamera.
    /// Вешается на объект самой камеры (Main Camera), который лежит ПОД TPS_Rig.
    ///
    /// Иерархия:
    ///   TPS_Rig (ThirdPersonCamera — задаёт мировую позу)
    ///     └── Main Camera (Camera + этот CameraJuice — локальные боб/тряска/крен + FOV)
    ///
    /// ThirdPersonCamera двигает РОДИТЕЛЯ (rig), а juice добавляет ЛОКАЛЬНЫЕ смещения
    /// на самой камере — они не конфликтуют и складываются автоматически.
    ///
    /// BOB / SHAKE: только при СПРИНТЕ (Shift + реальное движение). При ходьбе — тишина.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraJuice : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private PlayerLook _look;
        [SerializeField] private Camera _camera;

        [Header("FOV kick")]
        [SerializeField] private float _baseFov = 60f;
        [SerializeField] private float _sprintFovAdd = 6f;
        [SerializeField] private float _fovLerp = 5f;

        [Header("Headbob (мягкий) — ТОЛЬКО при спринте")]
        [SerializeField] private float _bobFrequency = 6f;      // шагов/сек
        [SerializeField] private float _bobAmplitude = 0.025f;  // вертикаль (м) — небольшая
        [SerializeField] private float _bobHorizontal = 0.015f;
        [SerializeField] private float _bobBlend = 6f;          // как плавно bob включается/гаснет

        [Header("Беговая тряска — ТОЛЬКО при спринте")]
        [Tooltip("Сила постоянной тряски во время бега.")]
        [SerializeField] private float _sprintShake = 0.35f;
        [Tooltip("Плавность нарастания/спада беговой тряски.")]
        [SerializeField] private float _sprintShakeBlend = 6f;
        [Tooltip("Частота шума тряски.")]
        [SerializeField] private float _shakeFreq = 25f;

        [Header("Lean / roll (крен)")]
        [SerializeField] private float _turnRoll = 0.8f;
        [SerializeField] private float _strafeRoll = 1.5f;
        [SerializeField] private float _rollLerp = 6f;

        [Header("Speed pitch")]
        [SerializeField] private float _speedPitch = 1f;

        [Header("Общее сглаживание")]
        [SerializeField] private float _posSmooth = 12f;        // сглаживание итоговой позиции
        [SerializeField] private float _rotSmooth = 12f;        // сглаживание итогового поворота

        [Header("Shake (импульсный, от AddShake)")]
        [SerializeField] private float _shakeDecay = 2.5f;

        private Vector3 _basePos;
        private float _bobTimer;
        private float _bobWeight;   // 0..1, плавно следует за спринтом
        private float _roll;
        private float _shake;       // импульсная тряска (AddShake)
        private float _runShake;    // беговая тряска (сглаженная)

        private Vector3 _smoothPos;
        private Quaternion _smoothRot = Quaternion.identity;

        private void Awake()
        {
            _basePos = transform.localPosition;
            _smoothPos = _basePos;

            if (!_camera) _camera = GetComponent<Camera>();
            if (!_movement) _movement = FindAnyObjectByType<PlayerMovement>();
            if (!_look) _look = FindAnyObjectByType<PlayerLook>();
        }

        public void SetCamera(Camera camera) => _camera = camera;
        public void SetPlayer(PlayerMovement movement, PlayerLook look)
        {
            _movement = movement;
            _look = look;
        }

        public void AddShake(float amount) => _shake = Mathf.Max(_shake, amount);

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Ссылки могли ещё не найтись — добираем лениво.
            if (_movement == null) _movement = FindAnyObjectByType<PlayerMovement>();
            if (_look == null) _look = FindAnyObjectByType<PlayerLook>();

            float speed01 = _movement ? _movement.CurrentSpeed01 : 0f;

            // Спринт-фактор: >0 только когда зажат Shift И реально движемся.
            float sprint = (_movement != null && _movement.IsSprinting) ? speed01 : 0f;

            // FOV kick (по скорости)
            if (_camera != null)
            {
                float targetFov = _baseFov + _sprintFovAdd * speed01;
                _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFov, _fovLerp * dt);
            }

            // Headbob — вес следует за СПРИНТОМ (при ходьбе = 0, покачивания нет).
            _bobWeight = Mathf.Lerp(_bobWeight, sprint, _bobBlend * dt);
            Vector3 bob = Vector3.zero;
            if (_bobWeight > 0.001f)
            {
                _bobTimer += dt * _bobFrequency * Mathf.Max(speed01, 0.3f) * Mathf.PI * 2f;
                float v = Mathf.Sin(_bobTimer);
                bob.y = -Mathf.Abs(v) * _bobAmplitude * _bobWeight;   // мягкий «шаг» вниз
                bob.x = Mathf.Sin(_bobTimer * 0.5f) * _bobHorizontal * _bobWeight;
            }
            else
            {
                _bobTimer = 0f;
            }

            // Roll
            float turn = _look ? _look.YawDelta : 0f;
            float strafe = 0f;
            if (_movement)
            {
                float mag = _movement.WorldVelocity.magnitude;
                if (mag > 0.01f)
                {
                    // локальная X скорости относительно ориентации камеры (rig)
                    Vector3 local = transform.parent != null
                        ? transform.parent.InverseTransformDirection(_movement.WorldVelocity)
                        : _movement.WorldVelocity;
                    strafe = Mathf.Clamp(local.x / mag, -1f, 1f);
                }
            }
            float targetRoll = -turn * _turnRoll - strafe * _strafeRoll;
            _roll = Mathf.Lerp(_roll, targetRoll, _rollLerp * dt);

            // Shake: беговая (только спринт) + импульсная (AddShake).
            _runShake = Mathf.Lerp(_runShake, sprint * _sprintShake, _sprintShakeBlend * dt);
            float totalShake = _shake + _runShake;

            Vector3 shakeOffset = Vector3.zero;
            float shakeRoll = 0f;
            if (totalShake > 0.001f)
            {
                float t = Time.time * _shakeFreq;
                shakeOffset.x = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * totalShake * 0.08f;
                shakeOffset.y = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * totalShake * 0.08f;
                shakeRoll = (Mathf.PerlinNoise(t, t) - 0.5f) * 2f * totalShake * 1.5f;
            }
            _shake = Mathf.MoveTowards(_shake, 0f, _shakeDecay * dt); // импульс затухает

            // Целевые значения (в ЛОКАЛЬНЫХ осях камеры — родитель уже спозиционирован кодом).
            Vector3 targetPos = _basePos + bob + shakeOffset;
            Quaternion targetRot = Quaternion.Euler(_speedPitch * speed01, 0f, _roll + shakeRoll);

            // Финальное сглаживание (кадронезависимое) — убирает любую резкость.
            float pl = 1f - Mathf.Exp(-_posSmooth * dt);
            float rl = 1f - Mathf.Exp(-_rotSmooth * dt);
            _smoothPos = Vector3.Lerp(_smoothPos, targetPos, pl);
            _smoothRot = Quaternion.Slerp(_smoothRot, targetRot, rl);

            transform.localPosition = _smoothPos;
            transform.localRotation = _smoothRot;
        }
    }
}