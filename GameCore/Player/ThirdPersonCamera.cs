using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Player
{
    /// <summary>
    /// AAA-камера от третьего лица целиком на коде. Мышь крутит орбиту (углы из PlayerLook),
    /// персонаж доворачивается сам. Обход препятствий — SphereCast. Зум — колесо.
    /// (Режим «фокуса» убран — крупные планы теперь делают катсцены со своими камерами.)
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Цель")]
        [SerializeField] private Transform _target;
        [SerializeField] private float _focusHeight = 1.6f;

        [Header("Дистанция и зум")]
        [SerializeField] private float _distance = 4f;
        [SerializeField] private float _minDistance = 0.5f;
        [SerializeField] private float _maxDistance = 6f;
        [SerializeField] private float _zoomStep = 0.5f;

        [Header("Смещение «через плечо»")]
        [SerializeField] private Vector2 _shoulder = new(0.6f, 0.1f);

        [Header("Сглаживание следования")]
        [SerializeField] private float _followSmoothTime = 0.06f;

        [Header("Обход препятствий")]
        [SerializeField] private LayerMask _collisionMask = ~0;
        [SerializeField] private float _collisionRadius = 0.25f;
        [SerializeField] private float _collisionBuffer = 0.2f;
        [SerializeField] private float _pullInSpeed = 30f;
        [SerializeField] private float _pushOutSpeed = 6f;

        private PlayerLook _look;
        private Vector3 _smoothFocus;
        private Vector3 _focusVel;
        private float _curDist;

        public float Yaw => _look != null ? _look.Yaw : transform.eulerAngles.y;

        public Vector3 PlanarForward
        {
            get
            {
                Vector3 f = Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward;
                f.y = 0f;
                return f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward;
            }
        }

        public Vector3 PlanarRight
        {
            get
            {
                Vector3 r = Quaternion.Euler(0f, Yaw, 0f) * Vector3.right;
                r.y = 0f;
                return r.sqrMagnitude > 1e-4f ? r.normalized : Vector3.right;
            }
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            _look = _target != null ? _target.GetComponent<PlayerLook>() : null;
            SnapToTarget();
        }

        private void Awake()
        {
            _curDist = _distance;
            if (_target != null && _look == null) _look = _target.GetComponent<PlayerLook>();
        }

        private void Start()
        {
            if (_target != null) SnapToTarget();
        }

        private void SnapToTarget()
        {
            if (_target == null) return;
            _smoothFocus = _target.position + Vector3.up * _focusHeight;
            _focusVel = Vector3.zero;
            _curDist = _distance;
            PositionCamera(Time.deltaTime, true);
        }

        /// <summary>Разрешить/запретить зум колесом (выключается на время диалога/меню).</summary>
        public void SetZoomEnabled(bool enabled) => _zoomEnabled = enabled;
        private bool _zoomEnabled = true;

        private void Update()
        {
            if (!_zoomEnabled) return;
            if (_zoomStep <= 0f) return;
            var mouse = Mouse.current;
            if (mouse == null) return;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                _distance = Mathf.Clamp(_distance - Mathf.Sign(scroll) * _zoomStep, _minDistance, _maxDistance);
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            PositionCamera(Time.deltaTime, false);
        }

        private void PositionCamera(float dt, bool snap)
        {
            if (dt <= 0f) dt = 0.0001f;

            Vector3 focus = _target.position + Vector3.up * _focusHeight;
            _smoothFocus = snap ? focus : Vector3.SmoothDamp(_smoothFocus, focus, ref _focusVel, _followSmoothTime);

            float yaw = _look != null ? _look.Yaw : 0f;
            float pitch = _look != null ? _look.Pitch : 10f;
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);

            Vector3 pivot = _smoothFocus + orbit * new Vector3(_shoulder.x, _shoulder.y, 0f);
            Vector3 dir = orbit * Vector3.back;
            float wanted = _distance;

            if (Physics.SphereCast(pivot, _collisionRadius, dir, out RaycastHit hit,
                                   _distance, _collisionMask, QueryTriggerInteraction.Ignore))
                wanted = Mathf.Max(_minDistance, hit.distance - _collisionBuffer);

            if (snap) _curDist = wanted;
            else
            {
                float speed = wanted < _curDist ? _pullInSpeed : _pushOutSpeed;
                _curDist = Mathf.Lerp(_curDist, wanted, 1f - Mathf.Exp(-speed * dt));
            }

            Vector3 pos = pivot + dir * _curDist;
            Vector3 toPivot = pivot - pos;
            Quaternion rot = toPivot.sqrMagnitude > 1e-6f
                ? Quaternion.LookRotation(toPivot.normalized, Vector3.up)
                : transform.rotation;
            transform.SetPositionAndRotation(pos, rot);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_target == null) return;
            Vector3 focus = _target.position + Vector3.up * _focusHeight;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(focus, 0.1f);
        }
#endif
    }
}