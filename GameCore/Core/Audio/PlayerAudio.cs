using Core.Common;
using Core.Player;
using UnityEngine;

namespace Core.Audio
{
    /// <summary>
    /// Звуки, идущие от игрока. Пока — шаги; дыхание и прочее добавляются сюда же.
    ///
    /// Шаги считаются ПРОЦЕДУРНО, по фактически пройденному расстоянию: прошёл
    /// stepDistance метров → шаг. Animation Events не используются, к анимации
    /// не привязано — темп шагов сам следует за скоростью и не рассыпается при
    /// смене клипа анимации.
    ///
    /// Расстояние берётся из смещения трансформа, а не из желаемой скорости
    /// движения: упёрся в стену — ноги на месте, шаги не идут.
    ///
    /// Вешается на префаб игрока, ссылку на сервис получает из AudioInitStep.
    /// </summary>
    public class PlayerAudio : MonoBehaviour
    {
        [Header("Ссылки (авто-поиск, если пусто)")]
        [SerializeField] private PlayerMovement _movement;

        [Header("Шаги")]
        [Tooltip("Метров на шаг при ходьбе. Меньше — чаще. Ориентир для человека: 1.6-2.0.")]
        [SerializeField] private float _stepDistanceWalk = 1.8f;

        [Tooltip("Метров на шаг при беге. Больше, чем при ходьбе — шаг длиннее.")]
        [SerializeField] private float _stepDistanceRun = 2.4f;

        [Tooltip("Насколько бег громче ходьбы. Работает, только если у поверхности нет отдельного набора для бега.")]
        [SerializeField] private float _runVolumeScale = 1.3f;

        [Tooltip("Ниже этой скорости (м/с) шаги не считаются вообще.")]
        [SerializeField] private float _minSpeed = 0.3f;

        [Header("Определение поверхности")]
        [Tooltip("Откуда бить луч вниз. Пусто → точка игрока + rayHeight. Ставь на уровень таза, не в ноги.")]
        [SerializeField] private Transform _rayOrigin;

        [SerializeField] private float _rayHeight = 1f;
        [SerializeField] private float _rayDistance = 1.5f;
        [SerializeField] private LayerMask _groundMask = ~0;

        [Tooltip("Что играть, если под ногами нет SurfaceTag.")]
        [SerializeField] private SurfaceDefinition _defaultSurface;

        [Header("Отладка")]
        [SerializeField] private bool _logSteps = false;

        private AudioService _audio;
        private Vector3 _lastPosition;
        private float _accumulated;
        private bool _wasMoving;

        // Диагностика: проблемы сообщаются всегда, но не чаще раза в LogThrottle секунд,
        // иначе при неверной настройке луча консоль зальёт на каждом шаге.
        private float _lastProblemLog = -99f;
        private bool _loggedFirstStep;
        private const float LogThrottle = 2f;

        /// <summary>Смещение больше этого за кадр — телепорт или спавн, а не ходьба.</summary>
        private const float TeleportThreshold = 2f;

        public void Bind(AudioService audio) => _audio = audio;

        private void Awake()
        {
            if (_movement == null) _movement = GetComponent<PlayerMovement>();
            if (_movement == null) _movement = GetComponentInParent<PlayerMovement>();
            _lastPosition = transform.position;
        }

        private void OnEnable() => _lastPosition = transform.position;

        private void Update()
        {
            if (_audio == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return; // пауза

            Vector3 position = transform.position;
            Vector3 delta = position - _lastPosition;
            delta.y = 0f;
            _lastPosition = position;

            float distance = delta.magnitude;

            // Телепорт (спавн, переход сцены) — не считаем за пройденный путь.
            if (distance > TeleportThreshold)
            {
                _accumulated = 0f;
                _wasMoving = false;
                return;
            }

            if (distance / dt < _minSpeed)
            {
                _wasMoving = false;
                return;
            }

            bool running = _movement != null && _movement.IsSprinting;
            float stepDistance = Mathf.Max(0.1f, running ? _stepDistanceRun : _stepDistanceWalk);

            // Тронулся с места — первый шаг не заставляем ждать полную дистанцию.
            if (!_wasMoving)
            {
                _accumulated = stepDistance * 0.6f;
                _wasMoving = true;
            }

            _accumulated += distance;
            if (_accumulated < stepDistance) return;

            _accumulated -= stepDistance;
            PlayStep(running);
        }

        private void PlayStep(bool running)
        {
            Vector3 origin = _rayOrigin != null
                ? _rayOrigin.position
                : transform.position + Vector3.up * _rayHeight;

            if (!Physics.Raycast(origin, Vector3.down, out var hit, _rayDistance,
                                 _groundMask, QueryTriggerInteraction.Ignore))
            {
                // Шаг посчитан, но пола под ним нет — это всегда ошибка настройки,
                // поэтому говорим об этом независимо от _logSteps.
                Problem($"под ногами нет пола. Луч из {origin}, длина {_rayDistance}, " +
                        $"маска {_groundMask.value}. Проверь Ground Mask и Ray Height/Distance.");
                return;
            }

            var surface = ResolveSurface(hit.collider);
            if (surface == null)
            {
                Problem($"поверхность не определена: под ногами '{hit.collider.name}', " +
                        "на нём и выше по иерархии нет SurfaceTag, Default Surface пуст.");
                return;
            }

            bool hasRunSet = running && surface.footstepsRun != null;
            var sound = hasRunSet ? surface.footstepsRun : surface.footsteps;
            if (sound == null) return;

            // Отдельного набора для бега нет — берём обычный, но громче.
            float scale = (running && !hasRunSet) ? _runVolumeScale : 1f;

            _audio.PlayAt(sound, hit.point, scale);

            // Первый успешный шаг отмечаем всегда — подтверждение, что цепочка собралась.
            if (!_loggedFirstStep)
            {
                _loggedFirstStep = true;
                Debug.Log($"[PlayerAudio] первый шаг: {surface.DebugName} по '{hit.collider.name}'");
            }
            else if (_logSteps)
            {
                CoreLog.Debug($"[PlayerAudio] шаг: {surface.DebugName}{(running ? " (бег)" : "")} по {hit.collider.name}");
            }
        }

        /// <summary>Сообщить о проблеме настройки, не чаще раза в LogThrottle секунд.</summary>
        private void Problem(string message)
        {
            if (Time.unscaledTime - _lastProblemLog < LogThrottle) return;
            _lastProblemLog = Time.unscaledTime;
            Debug.LogWarning($"[PlayerAudio] {message}");
        }

        /// <summary>Метка на коллайдере или выше по иерархии; не нашли — дефолт.</summary>
        private SurfaceDefinition ResolveSurface(Collider collider)
        {
            if (collider != null)
            {
                var tag = collider.GetComponentInParent<SurfaceTag>();
                if (tag != null && tag.Surface != null) return tag.Surface;
            }
            return _defaultSurface;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = _rayOrigin != null
                ? _rayOrigin.position
                : transform.position + Vector3.up * _rayHeight;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, origin + Vector3.down * _rayDistance);
            Gizmos.DrawWireSphere(origin + Vector3.down * _rayDistance, 0.05f);
        }
    }
}