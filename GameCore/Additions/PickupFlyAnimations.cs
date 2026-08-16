using System;
using UnityEngine;

namespace Core.Inventory
{
    /// <summary>
    /// Процедурная анимация подбора: объект летит к цели с ускорением, подкручивается,
    /// уменьшается и самоуничтожается. Полностью опциональна: отсутствие компонента на
    /// объекте ничего не ломает (WorldItemPickup просто делает Destroy).
    ///
    /// Устойчива к: пропаже цели в полёте, паузе (timeScale=0), кривым параметрам.
    /// Не требует ассетов/аниматоров.
    /// </summary>
    public class PickupFlyAnimation : MonoBehaviour
    {
        private Transform _target;
        private float _duration = 0.45f;
        private float _spinSpeed;
        private float _arcHeight;
        private Vector3 _startPos;
        private Vector3 _startScale;
        private float _t;
        private Action _onDone;
        private bool _playing;

        public void Play(Transform target, float duration = 0.45f, float arcHeight = 1f,
                         float spinSpeed = 720f, Action onDone = null)
        {
            _target = target;
            _duration = Mathf.Max(0.05f, duration);   // защита от нуля/отрицательного
            _arcHeight = Mathf.Max(0f, arcHeight);
            _spinSpeed = spinSpeed;
            _onDone = onDone;

            _startPos = transform.position;
            _startScale = transform.localScale;
            if (_startScale == Vector3.zero) _startScale = Vector3.one; // защита от нулевого масштаба

            _t = 0f;
            _playing = true;

            // Если цели нет с самого начала — не тянем, сразу финиш.
            if (_target == null) Finish();
        }

        private void Update()
        {
            if (!_playing) return;

            // Цель могла быть уничтожена в полёте (напр. игрок деспавнился) — мягко финишируем.
            if (_target == null) { Finish(); return; }

            // unscaledDeltaTime — чтобы полёт шёл даже если игра на паузе (timeScale=0).
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;

            _t += dt / _duration;
            float k = Mathf.Clamp01(_t);
            float ease = k * k;   // ease-in: разгон к концу

            Vector3 flat = Vector3.Lerp(_startPos, _target.position, ease);
            float arc = Mathf.Sin(k * Mathf.PI) * _arcHeight;
            transform.position = flat + Vector3.up * arc;

            transform.Rotate((Vector3.up + Vector3.right).normalized,
                             _spinSpeed * dt, Space.Self);

            transform.localScale = _startScale * Mathf.Max(0f, 1f - ease);

            if (k >= 1f) Finish();
        }

        private void Finish()
        {
            if (!_playing && _onDone == null) { SafeDestroy(); return; }
            _playing = false;

            try { _onDone?.Invoke(); }
            catch (Exception) { /* колбэк не должен мешать уничтожению */ }

            SafeDestroy();
        }

        private void SafeDestroy()
        {
            if (this != null && gameObject != null)
                Destroy(gameObject);
        }
    }
}