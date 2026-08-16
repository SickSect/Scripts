using R3;
using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// Рейкаст из центра камеры вперёд (по кроссхейру). Публикует объект под прицелом.
    ///
    /// Источник камеры задаётся явно через <see cref="SetCamera"/> из CameraInitStep.
    /// Camera.main остаётся только аварийным запасным вариантом: при двух камерах с тегом
    /// MainCamera он возвращает произвольную из них, и луч уходит не оттуда, откуда смотришь.
    /// </summary>
    public class LookTarget : MonoBehaviour
    {
        [Header("Дальность взгляда")]
        [SerializeField] private float _maxDistance = 3f;

        [Header("Слой интерактивных объектов")]
        [SerializeField] private LayerMask _interactableMask = ~0;

        [Header("Debug")]
        [SerializeField] private bool _debugGizmo = true;
        [SerializeField] private bool _debugLog = false;

        private Camera _camera;
        private bool _cameraInjected;

        private Ray _lastRay;
        private bool _lastHit;
        private Vector3 _lastHitPoint;

        public ReactiveProperty<GameObject> Target { get; } = new(null);

        /// <summary>Камера, из которой сейчас строится луч. Null — источник ещё не найден.</summary>
        public Camera SourceCamera => _camera;

        /// <summary>
        /// Явно задать камеру-источник луча. Приоритетнее Camera.main.
        /// </summary>
        public void SetCamera(Camera camera)
        {
            if (camera == null)
            {
                Debug.LogWarning("[LookTarget] SetCamera(null) — остаёмся на Camera.main.");
                return;
            }

            _camera = camera;
            _cameraInjected = true;

            if (_debugLog)
                Debug.Log($"[LookTarget] источник луча: {camera.name}");
        }

        private void Update()
        {
            if (_camera == null)
            {
                if (_cameraInjected) return;

                _camera = Camera.main;
                if (_camera == null) return;
            }

            var ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            _lastRay = ray;

            if (Physics.Raycast(ray, out var hit, _maxDistance, _interactableMask))
            {
                _lastHit = true;
                _lastHitPoint = hit.point;
                Target.Value = hit.collider.gameObject;

                if (_debugLog)
                    Debug.Log($"[LookTarget] попал в {hit.collider.name} " +
                              $"(слой {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
            }
            else
            {
                _lastHit = false;
                Target.Value = null;
            }
        }

        private void OnDisable()
        {
            // Компонент выключают на время крупного плана. Цель надо погасить явно,
            // иначе HUD останется с последней подсказкой на экране.
            Target.Value = null;
        }

        private void OnDrawGizmos()
        {
            if (!_debugGizmo) return;

            var cam = _camera != null ? _camera : Camera.main;
            if (cam == null) return;

            var ray = Application.isPlaying
                ? _lastRay
                : cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            Gizmos.color = _lastHit ? Color.green : Color.red;
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * _maxDistance);

            if (_lastHit)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_lastHitPoint, 0.1f);
            }
        }

        private void OnDestroy() => Target.Dispose();
    }
}
