using UnityEngine;
using Core.Interaction;

namespace Core.Interaction.Interactables
{
    /// <summary>
    /// Компонент, делающий объект переносимым.
    /// Работает в паре с MouseInteractor.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Carryable : MonoBehaviour, ICarryable
    {
        [Header("Визуал")]
        [Tooltip("Подсвечивать предмет при поднятии?")]
        [SerializeField] private bool _useHighlight = true;

        [Tooltip("Цвет подсветки при поднятии.")]
        [SerializeField] private Color _holdColor = Color.yellow;

        private Rigidbody _rb;
        private Renderer[] _renderers;
        private Color[] _originalColors;
        private bool _isHeld = false;

        // Реализация интерфейса
        public Transform Transform => transform;
        public Rigidbody Rigidbody => _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _renderers = GetComponentsInChildren<Renderer>();

            // Сохраняем оригинальные цвета материалов
            if (_useHighlight && _renderers.Length > 0)
            {
                _originalColors = new Color[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _originalColors[i] = _renderers[i].material.color;
                }
            }

            // Настройка физики для стабильности
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Важно: убедимся, что центр масс адекватный, чтобы коробка не кувыркалась странно
            _rb.centerOfMass = Vector3.zero;
        }

        /// <summary>
        /// Вызывается при поднятии (ЛКМ).
        /// Отключает симуляцию физики, чтобы MouseInteractor мог двигать объект через Transform.
        /// </summary>
        public void OnPickUp()
        {
            if (_isHeld) return;
            _isHeld = true;

            // Делаем кинематическим (физика игнорируется, двигаем руками)
            _rb.isKinematic = true;
            _rb.angularVelocity = Vector3.zero;
            _rb.linearVelocity = Vector3.zero;

            if (_useHighlight) ApplyColor(_holdColor);

            Debug.Log($"[Carryable] Поднят: {gameObject.name}");
        }

        /// <summary>
        /// Вызывается при броске (повторный ЛКМ).
        /// Включает симуляцию физики обратно. Объект упадет под действием гравитации.
        /// </summary>
        public void OnDrop()
        {
            if (!_isHeld) return;
            _isHeld = false;

            // Возвращаем управление физике
            _rb.isKinematic = false;
            // Гравитацию включает сам MouseInteractor (useGravity = true), 
            // но на всякий случай продублируем проверку здесь, если логика изменится.

            if (_useHighlight) RestoreColors();

            Debug.Log($"[Carryable] Брошен: {gameObject.name}");
        }

        #region Helpers
        private void ApplyColor(Color color)
        {
            foreach (var r in _renderers)
            {
                if (r != null) r.material.color = color;
            }
        }

        private void RestoreColors()
        {
            if (_originalColors == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null && i < _originalColors.Length)
                {
                    _renderers[i].material.color = _originalColors[i];
                }
            }
        }
        #endregion
    }
}