using System;
using Core.Common;
using Core.DI;
using Core.Player;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Core.Inventory.UI
{
    /// <summary>
    /// UI инвентаря: сетка ячеек слева + описание выбранного предмета справа с кнопками
    /// Использовать/Выбросить. Открывается по Tab, ставит игру на паузу.
    ///
    /// Живёт как глобальный префаб (DontDestroyOnLoad), как пауза. Доступность гейтится
    /// (в меню выключен). Слушает InventoryService.Changed для перерисовки.
    ///
    /// Настройка префаба:
    ///  _panel           — корневая панель (вкл/выкл)
    ///  _slotsContainer  — контейнер сетки (Grid Layout Group)
    ///  _slotPrefab      — префаб InventorySlotView
    ///  _nameLabel, _descLabel, _iconImage — панель описания
    ///  _useButton, _dropButton — кнопки действий
    /// </summary>
    public class InventoryUIController : MonoBehaviour, Core.UI.Screens.IUIScreen
    {
        [Header("Панель")]
        [SerializeField] private GameObject _panel;

        [Header("Сетка")]
        [SerializeField] private Transform _slotsContainer;
        [SerializeField] private InventorySlotView _slotPrefab;
        [SerializeField] private int _columns = 5; // колонок в сетке (для WASD-навигации)

        [Header("Описание")]
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _descLabel;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _useButton;
        [SerializeField] private Button _dropButton;

        private DIContainer _root;
        private InventoryService _inventory;
        private InputAction _toggleAction;
        private GameObject _player;

        private Core.UI.Screens.UIScreenManager _screens;
        private InputAction _navigateAction;
        private InputAction _useAction;
        private InputAction _dropAction;

        /// <summary>Подключить экшены навигации инвентаря (карта UI). Вызывается из GameBootstrap.</summary>
        public void BindNavigation(InputAction navigate, InputAction use, InputAction drop)
        {
            _navigateAction = navigate;
            _useAction = use;
            _dropAction = drop;
            if (_useAction != null) _useAction.performed += _ => OnUse();
            if (_dropAction != null) _dropAction.performed += _ => OnDrop();
            if (_navigateAction != null) _navigateAction.performed += OnNavigate;
        }

        private void OnNavigate(InputAction.CallbackContext ctx)
        {
            if (!_isOpen) return;
            Vector2 dir = ctx.ReadValue<Vector2>();
            MoveSelection(dir);
        }

        private void MoveSelection(Vector2 dir)
        {
            int cols = Mathf.Max(1, _columns);
            int count = _views.Count;
            if (count == 0) return;

            int cur = _selected < 0 ? 0 : _selected;
            int row = cur / cols;
            int col = cur % cols;

            if (dir.x > 0.5f) col++;
            else if (dir.x < -0.5f) col--;
            else if (dir.y > 0.5f) row--; // вверх = меньший индекс
            else if (dir.y < -0.5f) row++;

            col = Mathf.Clamp(col, 0, cols - 1);
            int maxRow = (count - 1) / cols;
            row = Mathf.Clamp(row, 0, maxRow);

            int next = row * cols + col;
            if (next >= count) next = count - 1;
            Select(next);
        }

        private readonly System.Collections.Generic.List<InventorySlotView> _views = new();
        private int _selected = -1;
        private bool _isOpen;
        private bool _available;

        private IDisposable _changedSub;

        public void Bind(DIContainer root, InputAction toggleAction)
        {
            _root = root;
            _toggleAction = toggleAction;
            _toggleAction.performed += OnToggleKey;

            _screens = root.Resolve<Core.UI.Screens.UIScreenManager>();

            _useButton.onClick.AddListener(OnUse);
            _dropButton.onClick.AddListener(OnDrop);

            _panel.SetActive(false);
        }

        public void SetAvailable(bool available)
        {
            _available = available;
            if (!available && _isOpen) _screens.RequestClose(this);
        }

        private void OnToggleKey(InputAction.CallbackContext ctx)
        {
            if (!_available) return;
            _screens.RequestToggle(this);
        }

        public bool IsOpen => _isOpen;

        // --- IUIScreen: чистый показ/скрытие, timeScale и ввод делает менеджер ---

        public void OpenScreen()
        {
            if (!_root.TryResolve<InventoryService>(out _inventory))
            {
                CoreLog.Debug("[InventoryUI] InventoryService недоступен");
                return;
            }
            _player = FindPlayer();

            _isOpen = true;
            _panel.SetActive(true);

            BuildGrid();
            _changedSub = _inventory.Changed.Subscribe(_ => Refresh());
            Select(0);
            Refresh();
        }

        public void CloseScreen()
        {
            _isOpen = false;
            _panel.SetActive(false);
            _changedSub?.Dispose();
            _changedSub = null;
        }

        private void BuildGrid()
        {
            foreach (var v in _views) if (v != null) Destroy(v.gameObject);
            _views.Clear();

            for (int i = 0; i < _inventory.Capacity; i++)
            {
                var view = Instantiate(_slotPrefab, _slotsContainer);
                view.Setup(i, OnSlotClick);
                _views.Add(view);
            }
        }

        private void OnSlotClick(int index) => Select(index);

        private void Select(int index)
        {
            _selected = index;
            for (int i = 0; i < _views.Count; i++)
                _views[i].SetSelected(i == index);
            RenderDescription();
        }

        private void Refresh()
        {
            var slots = _inventory.Slots;
            for (int i = 0; i < _views.Count; i++)
                _views[i].Render(i < slots.Count ? slots[i] : null);
            RenderDescription();
        }

        private void RenderDescription()
        {
            ItemStack stack = null;
            if (_selected >= 0 && _selected < _inventory.Slots.Count)
                stack = _inventory.Slots[_selected];

            bool has = stack != null && !stack.IsEmpty;

            if (_nameLabel != null) _nameLabel.text = has ? stack.Item.displayName : "";
            if (_descLabel != null) _descLabel.text = has ? stack.Item.description : "";
            if (_iconImage != null)
            {
                _iconImage.enabled = has;
                if (has) _iconImage.sprite = stack.Item.icon;
            }

            if (_useButton != null) _useButton.interactable = has && stack.Item.CanUse;
            if (_dropButton != null) _dropButton.interactable = has && stack.Item.droppable;
        }

        private void OnUse()
        {
            if (_selected < 0) return;
            _inventory.Use(_selected, _player);
            // Refresh придёт через Changed.
        }

        private void OnDrop()
        {
            if (_selected < 0) return;
            _inventory.Drop(_selected, 1);
        }

        private GameObject FindPlayer()
        {
            var pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            return pm != null ? pm.gameObject : null;
        }

        private void OnDestroy()
        {
            if (_toggleAction != null) _toggleAction.performed -= OnToggleKey;
            _changedSub?.Dispose();
        }
    }
}
