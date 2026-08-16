using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// Камера от первого лица. Углы берёт из <see cref="PlayerLook"/> — тот же источник,
    /// что и у <see cref="ThirdPersonCamera"/>. Благодаря этому движение остаётся
    /// камеро-зависимым, а PlayerMovement не требует ни одной правки.
    ///
    /// Тело НЕ вращает: PlayerMovement сам доворачивает капсулу к вектору движения,
    /// в первом лице этот доворот не виден. Прямой записи в трансформ Rigidbody нет —
    /// физика не рассинхронизируется.
    ///
    /// Вешается на объект с компонентом Camera внутри префаба игрока.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FirstPersonCamera : MonoBehaviour
    {
        [Header("Цель")]
        [Tooltip("Корень игрока. Пусто — берётся transform.root.")]
        [SerializeField] private Transform _target;

        [Tooltip("Высота глаз над корнем игрока.")]
        [SerializeField] private float _eyeHeight = 1.65f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private PlayerLook _look;
        private Camera _camera;

        /// <summary>Рендер-камера этого вида. Используется LookTarget как источник луча.</summary>
        public Camera Camera
        {
            get
            {
                if (_camera == null) _camera = GetComponent<Camera>();
                return _camera;
            }
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            if (_target == null) _target = transform.root;
            if (_target != null && _look == null) _look = _target.GetComponent<PlayerLook>();

            ApplyImmediate();
        }

        /// <summary>
        /// Привязка из CameraInitStep. Вызывается после спавна игрока.
        /// </summary>
        public void Bind(Transform target, PlayerLook look)
        {
            _target = target;
            _look = look != null
                ? look
                : (target != null ? target.GetComponent<PlayerLook>() : null);

            if (_debugLog)
                Debug.Log($"[FirstPersonCamera] привязана к {(_target != null ? _target.name : "null")}, " +
                          $"PlayerLook={(_look != null ? "есть" : "НЕТ")}");

            ApplyImmediate();
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            transform.position = _target.position + Vector3.up * _eyeHeight;

            if (_look != null)
                transform.rotation = Quaternion.Euler(_look.Pitch, _look.Yaw, 0f);
        }

        private void ApplyImmediate()
        {
            if (_target == null) return;

            transform.position = _target.position + Vector3.up * _eyeHeight;

            if (_look != null)
                transform.rotation = Quaternion.Euler(_look.Pitch, _look.Yaw, 0f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Transform t = _target != null ? _target : transform.root;
            if (t == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(t.position + Vector3.up * _eyeHeight, 0.08f);
        }
#endif
    }
}