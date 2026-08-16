using UnityEngine;

namespace Core.Interaction
{
    /// <summary>
    /// Спрайт подсветки: сам разворачивается к камере, задаёт размер и смещение
    /// в плоскости спрайта. Заменяет Billboard — вешать вместо него, не рядом.
    /// </summary>
    [ExecuteAlways]
    public class SpriteFit : MonoBehaviour
    {
        [Header("Размер")]
        [SerializeField] private float _size = 1f;

        [Header("Смещение в плоскости спрайта")]
        [Tooltip("X — вправо, Y — вверх. Правит несовпадение pivot и центра рисунка.")]
        [SerializeField] private Vector2 _offset = Vector2.zero;

        [Tooltip("Сдвиг к камере. Убирает врезание спрайта в модель.")]
        [SerializeField] private float _towardCamera = 0.05f;

        [Header("Разворот")]
        [Tooltip("Не наклоняться вверх-вниз за камерой.")]
        [SerializeField] private bool _lockY = true;

        private Transform _cam;

        private void LateUpdate()
        {
            if (_cam == null)
            {
                var c = Camera.main;
                if (c == null) return;
                _cam = c.transform;
            }

            Vector3 center = transform.parent != null ? transform.parent.position : transform.position;

            Vector3 dir = _cam.position - center;
            if (_lockY) dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            Quaternion rot = Quaternion.LookRotation(-dir);
            transform.rotation = rot;
            transform.localScale = Vector3.one * _size;

            Vector3 right = rot * Vector3.right;
            Vector3 up = rot * Vector3.up;
            Vector3 forward = rot * Vector3.forward;

            transform.position = center
                                 + right * _offset.x
                                 + up * _offset.y
                                 - forward * _towardCamera;
        }
    }
}