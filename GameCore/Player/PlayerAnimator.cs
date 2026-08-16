using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// Мост движение → Animator. Пишет Speed (0..1) в Blend Tree. Когда игрок почти
    /// стоит, замораживает Animator (animator.speed=0), чтобы walk-клип застыл кадром
    /// вместо перебора ногами на месте — псевдо-idle без отдельного клипа.
    ///
    /// Вешается на модель (MorroMan), где Animator. Ссылки авто-ищутся в родителе.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("Ссылки (авто-поиск в родителе, если пусто)")]
        [SerializeField] private PlayerMovement _movement;

        [Header("Имя параметра скорости")]
        [SerializeField] private string _speedParam = "Speed";

        [Header("Сглаживание скорости")]
        [SerializeField] private float _speedDamp = 0.12f;

        [Header("Псевдо-idle: заморозка при остановке")]
        [Tooltip("Ниже этой скорости Animator замирает (walk застывает как стойка).")]
        [SerializeField] private float _idleThreshold = 0.05f;

        private Animator _animator;
        private int _speedHash;
        private bool _hasSpeed;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_movement == null) _movement = GetComponentInParent<PlayerMovement>();
            _speedHash = Animator.StringToHash(_speedParam);
            _hasSpeed = HasParam(_speedParam);
        }

        private bool HasParam(string name)
        {
            if (_animator == null || string.IsNullOrEmpty(name)) return false;
            foreach (var p in _animator.parameters)
                if (p.name == name) return true;
            return false;
        }

        private void Update()
        {
            if (_animator == null) return;
            float dt = Time.deltaTime;

            float speed = _movement != null ? _movement.CurrentSpeed01 : 0f;

            if (_hasSpeed)
                _animator.SetFloat(_speedHash, speed, _speedDamp, dt);

            // Псевдо-idle: почти стоим → замораживаем Animator (walk застывает).
            if (speed < _idleThreshold)
                _animator.speed = 0f;
            else
                _animator.speed = 1f;
        }
    }
}