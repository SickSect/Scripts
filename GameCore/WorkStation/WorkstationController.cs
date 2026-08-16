using System;
using Core.Computer;
using Core.Interaction;
using Core.Player;
using Core.UI.Screens;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Core.Workstation
{
    /// <summary>
    /// Рабочее место: монитор, улики, ящик с инструментами.
    ///
    /// Игрок садится один раз (E), дальше переключает виды, уводя курсор
    /// к краю экрана. Это делает стол единым пространством работы, а не
    /// набором отдельных точек, к каждой из которых надо подходить.
    ///
    /// Виды — упорядоченная полоса. Края полосы это края: зацикливания нет,
    /// иначе игрок теряет ощущение, где он находится.
    ///
    /// Esc выходит из-за стола целиком.
    /// </summary>
    public class WorkstationController : MonoBehaviour, IInteractable, IUIScreen
    {
        [Serializable]
        public class View
        {
            [Tooltip("Для отладки и подсказок.")]
            public string title = "Вид";

            [Tooltip("Куда встаёт камера.")]
            public FocusPoint point;

            [Tooltip("Компоненты, работающие только на этом виде: мост кликов монитора, " +
                     "обработчик улик и т.п. Включаются на входе, выключаются на выходе.")]
            public MonoBehaviour[] activeComponents;

            [Tooltip("Точка подключения для косметики: выдвинуть ящик, зажечь подсветку.")]
            public UnityEvent onEnter;

            public UnityEvent onExit;
        }

        [Header("Виды, слева направо")]
        [SerializeField] private View[] _views;

        [Tooltip("С какого вида начинается работа.")]
        [SerializeField] private int _startIndex = 0;

        [Header("Вход")]
        [SerializeField] private string _prompt = "Сесть за стол";

        [Header("Навигация курсором")]
        [Tooltip("Полоса у края экрана в пикселях, которая считается краем.")]
        [SerializeField] private float _edgeMargin = 60f;

        [Tooltip("Сколько курсор должен пробыть у края, чтобы вид сменился. " +
                 "Защита от случайных переходов при работе с интерфейсом.")]
        [SerializeField] private float _edgeDwell = 0.25f;

        [Header("Ссылки")]
        [Tooltip("Стек фокусов. Пусто — ищется в сцене.")]
        [SerializeField] private CameraFocusService _focus;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private UIScreenManager _screens;
        private GameObject _player;

        private int _index = -1;
        private float _edgeTimer;
        private int _edgeDirection;

        // Навигация «взведена» только когда курсор побывал вне краевых полос.
        // Иначе она срабатывает сразу при посадке за стол: курсор до этого был
        // заблокирован в центре, и после разблокировки оказывается у края.
        private bool _edgeArmed;

        public string Prompt => _prompt;

        public bool IsOpen { get; private set; }

        /// <summary>Рабочее место диегетично: мир вокруг продолжает жить.</summary>
        public bool PausesGame => false;

        /// <summary>Текущий вид или null.</summary>
        public View Current => IsOpen && _index >= 0 && _index < _views.Length ? _views[_index] : null;

        private void Awake()
        {
            if (_focus == null) _focus = FindAnyObjectByType<CameraFocusService>();

            // Все виды выключены, пока за стол не сели.
            for (int i = 0; i < _views.Length; i++) SetComponentsEnabled(_views[i], false);
        }

        public void Interact(InteractionContext context)
        {
            if (IsOpen || context.Player == null) return;

            if (_focus == null || _views == null || _views.Length == 0)
            {
                Debug.LogError($"[Workstation] '{name}': не задан CameraFocusService или виды.");
                return;
            }

            _player = context.Player;

            _screens = null;
            if (context.Root != null) context.Root.TryResolve(out _screens);

            if (_screens != null) _screens.RequestOpen(this);
            else OpenScreen();
        }

        /// <summary>Встать из-за стола. Публичный — вешается на кнопку в интерфейсе.</summary>
        public void RequestExit()
        {
            if (!IsOpen) return;

            if (_screens != null) _screens.RequestClose(this);
            else CloseScreen();
        }

        public void OpenScreen()
        {
            IsOpen = true;
            _index = Mathf.Clamp(_startIndex, 0, _views.Length - 1);

            _focus.Push(_views[_index].point, _player);
            EnterView(_index);

            _edgeTimer = 0f;
            _edgeDirection = 0;
            _edgeArmed = false;

            if (_debugLog) Debug.Log($"[Workstation] сел за стол, вид '{_views[_index].title}'");
        }

        public void CloseScreen()
        {
            if (_index >= 0 && _index < _views.Length) ExitView(_index);

            IsOpen = false;
            _index = -1;

            _focus.PopAll();

            if (_debugLog) Debug.Log("[Workstation] встал из-за стола");
        }

        private void Update()
        {
            if (!IsOpen) return;

            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                RequestExit();
                return;
            }

            // Пока камера едет, ввод не принимаем — иначе виды проскакивают пачкой.
            if (_focus.IsMoving)
            {
                _edgeTimer = 0f;
                _edgeDirection = 0;
                _edgeArmed = false;
                return;
            }

            UpdateEdgeNavigation();
        }

        private void UpdateEdgeNavigation()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            float x = mouse.position.ReadValue().x;

            int direction = 0;
            if (x <= _edgeMargin) direction = -1;
            else if (x >= Screen.width - _edgeMargin) direction = 1;

            // Курсор в середине — навигация взводится.
            if (direction == 0) _edgeArmed = true;

            if (!_edgeArmed) return;

            // У края нет соседнего вида — не копим время впустую.
            if (direction != 0 && !CanMove(direction)) direction = 0;

            if (direction != _edgeDirection)
            {
                _edgeDirection = direction;
                _edgeTimer = 0f;
                return;
            }

            if (direction == 0) return;

            _edgeTimer += Time.unscaledDeltaTime;
            if (_edgeTimer < _edgeDwell) return;

            Move(direction);

            _edgeTimer = 0f;
            _edgeDirection = 0;

            // Следующий переход — только после возврата курсора в середину,
            // иначе виды пролистываются пачкой, пока курсор лежит у края.
            _edgeArmed = false;
        }

        private bool CanMove(int direction)
        {
            int next = _index + direction;
            return next >= 0 && next < _views.Length;
        }

        /// <summary>Перейти к соседнему виду. Публичный — можно повесить на стрелки в UI.</summary>
        public void Move(int direction)
        {
            if (!IsOpen || !CanMove(direction)) return;

            int next = _index + direction;

            ExitView(_index);
            _index = next;

            // Заменяем верх стека, а не наращиваем: виды одного уровня,
            // а не вложенные друг в друга.
            _focus.Replace(_views[_index].point, _player);

            EnterView(_index);

            if (_debugLog) Debug.Log($"[Workstation] вид → '{_views[_index].title}'");
        }

        /// <summary>Перейти к виду по индексу. Для кода и кнопок.</summary>
        public void GoTo(int index)
        {
            if (!IsOpen || index < 0 || index >= _views.Length || index == _index) return;

            Move(index - _index > 0 ? 1 : -1);
        }

        private void EnterView(int i)
        {
            SetComponentsEnabled(_views[i], true);
            _views[i].onEnter?.Invoke();
        }

        private void ExitView(int i)
        {
            SetComponentsEnabled(_views[i], false);
            _views[i].onExit?.Invoke();
        }

        private static void SetComponentsEnabled(View view, bool value)
        {
            if (view?.activeComponents == null) return;

            for (int i = 0; i < view.activeComponents.Length; i++)
                if (view.activeComponents[i] != null) view.activeComponents[i].enabled = value;
        }
    }
}