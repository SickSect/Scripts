using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Player
{
    /// <summary>
    /// Ввод мыши для орбитальной камеры от 3-го лица.
    ///
    /// ВАЖНО: этот скрипт БОЛЬШЕ НЕ вращает игрока и не трогает камеру напрямую.
    /// Он только копит углы обзора (yaw/pitch), гасит отдачу и клеммит pitch.
    /// Позицию камеры по этим углам строит <see cref="ThirdPersonCamera"/>,
    /// а тело персонажа доворачивается к движению в <see cref="PlayerMovement"/>.
    /// Так достигается декуплинг «камера ≠ поворот персонажа».
    /// </summary>
    public class PlayerLook : MonoBehaviour
    {
        [Header("Чувствительность мыши")]
        [SerializeField] private float _sensitivity = 0.1f;
        [SerializeField] private bool _invertY = false;

        [Header("Ограничение вертикального угла")]
        [SerializeField] private float _minPitch = -35f;
        [SerializeField] private float _maxPitch = 70f;

        private InputAction _lookAction;
        private float _yaw, _pitch;
        private Vector2 _recoil;        // доп. отдача (гасится к нулю)
        private bool _enabled = true;

        /// <summary>Текущий горизонтальный угол камеры (град).</summary>
        public float Yaw => _yaw;

        /// <summary>Текущий вертикальный угол камеры (град).</summary>
        public float Pitch => _pitch;

        /// <summary>Скорость поворота по горизонтали за кадр (град) — для крена камеры.</summary>
        public float YawDelta { get; private set; }

        private void Awake()
        {
            _yaw = transform.eulerAngles.y;
            _pitch = 10f;
            LockCursor(true);
        }

        public void BindInput(InputAction lookAction) => _lookAction = lookAction;

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            LockCursor(enabled);
        }

        /// <summary>Толчок камеры (отдача, удар, взрыв). x=yaw, y=pitch, в градусах.</summary>
        public void AddRecoil(Vector2 kick) => _recoil += kick;

        private void Update()
        {
            if (!_enabled || _lookAction == null) { YawDelta = 0f; return; }

            Vector2 delta = _lookAction.ReadValue<Vector2>() * _sensitivity;

            // Отдача гасится экспоненциально.
            _recoil = Vector2.Lerp(_recoil, Vector2.zero, 1f - Mathf.Exp(-12f * Time.deltaTime));

            YawDelta = delta.x + _recoil.x;
            _yaw += YawDelta;

            float pitchDelta = (_invertY ? -delta.y : delta.y) + _recoil.y;
            _pitch -= pitchDelta;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}