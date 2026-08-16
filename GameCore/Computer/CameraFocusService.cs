using System.Collections.Generic;
using Core.Player;
using Core.UI.HUD;
using UnityEngine;

namespace Core.Computer
{
    /// <summary>
    /// Стек фокусов камеры. Одна точка входа для монитора, ящика, инструментов —
    /// иначе движение камеры копируется в каждый из них.
    ///
    /// Стек даёт уровни вложенности: стол → монитор → инструмент. Выход (Esc)
    /// поднимает ровно на уровень вверх, а не выбрасывает сразу в комнату.
    /// Когда стек пустеет, управление возвращается FirstPersonCamera.
    ///
    /// Вешается на любой объект сцены в единственном экземпляре.
    /// </summary>
    public class CameraFocusService : MonoBehaviour
    {
        [SerializeField] private bool _debugLog = false;

        private readonly List<FocusPoint> _stack = new();

        private FirstPersonCamera _fpsCamera;
        private PlayerLook _look;
        private Camera _camera;
        private LookTarget _lookTarget;
        private InteractionHUD _hud;

        private Vector3 _fromPos;
        private Quaternion _fromRot;
        private float _fromFov;

        private Vector3 _targetPos;
        private Quaternion _targetRot;
        private float _targetFov;

        private float _t;
        private float _duration;
        private bool _animating;

        // Поза камеры до входа в первый фокус — точка возврата в свободный обзор.
        private Vector3 _restPos;
        private Quaternion _restRot;
        private float _restFov;

        /// <summary>Есть ли активный фокус (камера не у игрока в руках).</summary>
        public bool IsFocused => _stack.Count > 0;

        /// <summary>Глубина стека: 0 — свободное движение, 1 — стол, 2 — узел.</summary>
        public int Depth => _stack.Count;

        /// <summary>Едет ли камера прямо сейчас. Пока едет, ввод лучше не принимать.</summary>
        public bool IsMoving => _animating;

        /// <summary>Верхняя точка стека или null.</summary>
        public FocusPoint Top => _stack.Count > 0 ? _stack[^1] : null;

        /// <summary>
        /// Войти в фокус. Первый вызов забирает камеру у игрока,
        /// последующие складываются в стек.
        /// </summary>
        public bool Push(FocusPoint point, GameObject player)
        {
            if (point == null)
            {
                Debug.LogError("[Focus] Push с пустой точкой — проверь поле Point у вида.");
                return false;
            }

            if (_stack.Count == 0 && !Capture(player)) return false;

            _stack.Add(point);
            BeginMove(point.Position, point.Rotation, point.FieldOfView, point.MoveTime);

            if (_debugLog) Debug.Log($"[Focus] → '{point.Title}' (глубина {_stack.Count})");
            return true;
        }

        /// <summary>
        /// Заменить верхнюю точку стека, не меняя глубину.
        ///
        /// Нужно для соседних видов одного уровня: рабочее место переключается
        /// между монитором, уликами и ящиком. Через Pop+Push это делать нельзя —
        /// на глубине 1 Pop опустошит стек, камера уедет к игроку и отдаст
        /// управление раньше, чем придёт следующий Push.
        /// </summary>
        public bool Replace(FocusPoint point, GameObject player)
        {
            if (point == null)
            {
                Debug.LogError("[Focus] Replace с пустой точкой — проверь поле Point у вида.");
                return false;
            }

            if (_stack.Count == 0) return Push(point, player);

            var previous = _stack[^1];
            _stack[^1] = point;

            BeginMove(point.Position, point.Rotation, point.FieldOfView, point.MoveTime);

            if (_debugLog) Debug.Log($"[Focus] '{previous.Title}' → '{point.Title}' (глубина {_stack.Count})");
            return true;
        }

        /// <summary>Подняться на уровень вверх. Пустой стек — вернуть камеру игроку.</summary>
        public void Pop()
        {
            if (_stack.Count == 0) return;

            var left = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);

            if (_stack.Count > 0)
            {
                var back = _stack[^1];
                BeginMove(back.Position, back.Rotation, back.FieldOfView, back.MoveTime);
                if (_debugLog) Debug.Log($"[Focus] ← из '{left.Title}' в '{back.Title}'");
            }
            else
            {
                BeginMove(_restPos, _restRot, _restFov, left.MoveTime);
                if (_debugLog) Debug.Log($"[Focus] ← из '{left.Title}' в свободный обзор");
            }
        }

        /// <summary>Сбросить весь стек и вернуть камеру игроку.</summary>
        public void PopAll()
        {
            if (_stack.Count == 0) return;

            float time = _stack[^1].MoveTime;
            _stack.Clear();
            BeginMove(_restPos, _restRot, _restFov, time);
        }

        private bool Capture(GameObject player)
        {
            if (player == null) return false;

            _fpsCamera = player.GetComponentInChildren<FirstPersonCamera>(true);
            _look = player.GetComponentInParent<PlayerLook>();
            _lookTarget = player.GetComponentInChildren<LookTarget>(true);
            _camera = _fpsCamera != null ? _fpsCamera.Camera : Camera.main;

            if (_hud == null) _hud = FindAnyObjectByType<InteractionHUD>();

            if (_camera == null)
            {
                Debug.LogError("[Focus] камера игрока не найдена.");
                return false;
            }

            _restPos = _camera.transform.position;
            _restRot = _camera.transform.rotation;
            _restFov = _camera.fieldOfView;

            if (_fpsCamera != null) _fpsCamera.enabled = false;
            if (_look != null) _look.SetEnabled(false);

            // Прицел и подсказка «нажми E» в крупном плане мешают: игрок
            // работает мышью, а рейкаст взгляда всё равно смотрит в стол.
            if (_lookTarget != null) _lookTarget.enabled = false;
            if (_hud != null) _hud.SetVisible(false);

            return true;
        }

        private void Release()
        {
            if (_fpsCamera != null) _fpsCamera.enabled = true;
            if (_look != null) _look.SetEnabled(true);

            if (_lookTarget != null) _lookTarget.enabled = true;
            if (_hud != null) _hud.SetVisible(true);
        }

        private void BeginMove(Vector3 pos, Quaternion rot, float fov, float time)
        {
            if (_camera == null) return;

            _fromPos = _camera.transform.position;
            _fromRot = _camera.transform.rotation;
            _fromFov = _camera.fieldOfView;

            _targetPos = pos;
            _targetRot = rot;
            _targetFov = fov > 0f ? fov : _restFov;

            _duration = Mathf.Max(0f, time);
            _t = 0f;
            _animating = true;
        }

        private void LateUpdate()
        {
            if (!_animating || _camera == null) return;

            _t = _duration <= 0f ? 1f : Mathf.Min(1f, _t + Time.unscaledDeltaTime / _duration);
            float e = Mathf.SmoothStep(0f, 1f, _t);

            _camera.transform.SetPositionAndRotation(
                Vector3.Lerp(_fromPos, _targetPos, e),
                Quaternion.Slerp(_fromRot, _targetRot, e));

            _camera.fieldOfView = Mathf.Lerp(_fromFov, _targetFov, e);

            if (_t < 1f) return;

            _animating = false;

            // Стек опустел и камера доехала — отдаём управление игроку.
            if (_stack.Count == 0) Release();
        }
    }
}