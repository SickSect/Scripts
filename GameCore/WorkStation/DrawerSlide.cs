using UnityEngine;
using UnityEngine.Events;

namespace Core.Workstation
{
    /// <summary>
    /// Выдвижной ящик стола.
    ///
    /// Двигает сам объект по локальной оси. Содержимое едет вместе с ним,
    /// потому что лежит внутри — отдельного кода для инструментов не нужно.
    ///
    /// Вызывается из UnityEvent'ов вида станции: onEnter → Open, onExit → Close.
    /// Ни о какой станции сам при этом не знает и годится для любого ящика в игре.
    /// </summary>
    public class DrawerSlide : MonoBehaviour
    {
        [Header("Ход")]
        [Tooltip("Направление выдвижения в ЛОКАЛЬНЫХ координатах ящика. " +
                 "Обычно (0,0,-1) или (0,0,1) — смотря как повёрнут стол.")]
        [SerializeField] private Vector3 _axis = new(0f, 0f, -1f);

        [Tooltip("На сколько метров выдвигается.")]
        [SerializeField] private float _distance = 0.35f;

        [Tooltip("Сколько секунд занимает ход.")]
        [SerializeField] private float _duration = 0.45f;

        [Header("Звук")]
        [Tooltip("Точка, откуда звучит ящик. Пусто — сам объект.")]
        [SerializeField] private Transform _soundOrigin;

        [Header("События")]
        [Tooltip("Сработает, когда ящик полностью открыт — для подсветки инструментов.")]
        public UnityEvent onOpened;

        public UnityEvent onClosed;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private Vector3 _closedPos;
        private Vector3 _openPos;

        private float _t;          // 0 — закрыт, 1 — открыт
        private int _direction;    // -1 закрывается, +1 открывается, 0 покой

        public bool IsOpen => _t >= 1f;
        public bool IsMoving => _direction != 0;

        private void Awake()
        {
            _closedPos = transform.localPosition;
            _openPos = _closedPos + _axis.normalized * _distance;
        }

        /// <summary>Выдвинуть ящик. Вешается на onEnter вида станции.</summary>
        public void Open()
        {
            if (_t >= 1f && _direction == 0) return;

            _direction = 1;
            if (_debugLog) Debug.Log($"[Drawer] '{name}' открывается");
        }

        /// <summary>Задвинуть ящик. Вешается на onExit вида станции.</summary>
        public void Close()
        {
            if (_t <= 0f && _direction == 0) return;

            _direction = -1;
            if (_debugLog) Debug.Log($"[Drawer] '{name}' закрывается");
        }

        /// <summary>Открыть или закрыть — для кнопок и интерактивных ручек.</summary>
        public void Toggle()
        {
            if (_direction != 0) { _direction = -_direction; return; }

            if (IsOpen) Close();
            else Open();
        }

        /// <summary>Мгновенно задвинуть без анимации. Для сброса состояния.</summary>
        public void SnapClosed()
        {
            _t = 0f;
            _direction = 0;
            transform.localPosition = _closedPos;
        }

        private void Update()
        {
            if (_direction == 0) return;

            // unscaled: рабочее место не ставит игру на паузу, но так ящик
            // не зависнет, если пауза откроется поверх.
            float step = _duration <= 0f ? 1f : Time.unscaledDeltaTime / _duration;

            _t = Mathf.Clamp01(_t + step * _direction);

            // Плавный вход и выход — ящик не дёргается на старте и не бьётся в конце.
            float e = Mathf.SmoothStep(0f, 1f, _t);
            transform.localPosition = Vector3.Lerp(_closedPos, _openPos, e);

            if (_t > 0f && _t < 1f) return;

            bool opened = _t >= 1f;
            _direction = 0;

            if (opened) onOpened?.Invoke();
            else onClosed?.Invoke();

            if (_debugLog) Debug.Log($"[Drawer] '{name}' {(opened ? "открыт" : "закрыт")}");
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 from = Application.isPlaying
                ? transform.parent != null ? transform.parent.TransformPoint(_closedPos) : _closedPos
                : transform.position;

            Vector3 dir = transform.TransformDirection(_axis.normalized);

            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.9f);
            Gizmos.DrawLine(from, from + dir * _distance);
            Gizmos.DrawWireSphere(from + dir * _distance, 0.03f);
        }
#endif
    }
}
