using System;
using System.Collections.Generic;
using Core.DI;
using Core.Input;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Story.Dialogue
{
    /// <summary>
    /// Моковый UI диалога: имя говорящего + текст реплики + кнопки выборов.
    /// Подписан на DialoguePlayer, перерисовывается при смене ноды.
    ///
    /// Без паузы (timeScale не трогаем), но переключает ввод на UI (нельзя ходить).
    /// Глобальный префаб (DontDestroyOnLoad), как пауза/инвентарь.
    ///
    /// Настройка префаба:
    ///  _panel        — корневая панель (вкл/выкл)
    ///  _speakerLabel — TMP имя говорящего
    ///  _textLabel    — TMP текст реплики
    ///  _choicesContainer — контейнер кнопок (Vertical Layout Group)
    ///  _choicePrefab — префаб DialogueChoiceView
    ///  _continueButton — кнопка «Далее» (для нод без выборов)
    /// </summary>
    public class DialogueUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _speakerLabel;
        [SerializeField] private TMP_Text _textLabel;
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private DialogueChoiceView _choicePrefab;
        [SerializeField] private Button _continueButton;

        private DialoguePlayer _player;
        private GameInput _input;
        private readonly List<DialogueChoiceView> _spawnedChoices = new();
        private IDisposable _nodeSub;
        private IDisposable _closedSub;

        private int _selected;                    // индекс выделенного ответа
        private UnityEngine.InputSystem.InputAction _navigateAction;
        private UnityEngine.InputSystem.InputAction _confirmAction;

        public void Bind(DIContainer root)
        {
            _player = root.Resolve<DialoguePlayer>();
            _input = root.Resolve<GameInput>();

            _panel.SetActive(false);

            // Реагируем на смену ноды.
            _nodeSub = _player.CurrentNode.Subscribe(OnNodeChanged);
            _closedSub = _player.Closed.Subscribe(_ => OnClosed());

            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnContinue);

            // Навигация WASD + подтверждение.
            _navigateAction = _input.UiNavigate;
            _confirmAction = _input.UiUse;
            if (_navigateAction != null) _navigateAction.performed += OnNavigate;
            if (_confirmAction != null) _confirmAction.performed += OnConfirm;
        }

        private void OnNavigate(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (!_panel.activeSelf) return;
            var dir = ctx.ReadValue<Vector2>();
            if (Mathf.Abs(dir.y) < 0.5f) return;

            int count = _spawnedChoices.Count;
            if (count == 0) return;

            _selected += dir.y > 0 ? -1 : 1;      // вверх = меньший индекс
            _selected = Mathf.Clamp(_selected, 0, count - 1);
            HighlightSelected();
        }

        private void OnConfirm(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (!_panel.activeSelf) return;

            if (_spawnedChoices.Count == 0)
            {
                OnContinue();                      // нода без ответов — закрыть
                return;
            }
            OnChoiceClicked(_selected);
        }

        private void HighlightSelected()
        {
            for (int i = 0; i < _spawnedChoices.Count; i++)
                if (_spawnedChoices[i] != null)
                    _spawnedChoices[i].SetSelected(i == _selected);
        }

        private void OnNodeChanged(DialogueNode node)
        {
            if (node == null) return; // закрытие обработает Closed

            // Первый показ — открыть панель, переключить ввод.
            if (!_panel.activeSelf)
            {
                _panel.SetActive(true);
                _input.SwitchToUI();
                SetPlayerLook(false);   // курсор виден, камера не крутится
            }

            if (_speakerLabel != null) _speakerLabel.text = node.speaker;
            if (_textLabel != null) _textLabel.text = node.text;

            RebuildChoices();
        }

        private void RebuildChoices()
        {
            // Чистим старые кнопки.
            foreach (var c in _spawnedChoices) if (c != null) Destroy(c.gameObject);
            _spawnedChoices.Clear();

            var choices = _player.VisibleChoices.Value;

            if (choices.Count == 0)
            {
                // Нода без выборов — показываем «Далее» (закроет диалог).
                if (_continueButton != null) _continueButton.gameObject.SetActive(true);
                return;
            }

            if (_continueButton != null) _continueButton.gameObject.SetActive(false);

            for (int i = 0; i < choices.Count; i++)
            {
                var view = Instantiate(_choicePrefab, _choicesContainer);
                view.Setup(i, choices[i].text, OnChoiceClicked);
                _spawnedChoices.Add(view);
            }

            _selected = 0;
            HighlightSelected();
        }

        private void OnChoiceClicked(int index) => _player.Choose(index);

        private void OnContinue() => _player.Continue();

        private void OnClosed()
        {
            _panel.SetActive(false);
            _input.SwitchToPlayer();  // вернуть управление игроку
            SetPlayerLook(true);      // вернуть камеру и залочить курсор
        }

        /// <summary>Включить/выключить обзор игрока (заодно лочит/разлочивает курсор).</summary>
        private void SetPlayerLook(bool enabled)
        {
            var look = UnityEngine.Object.FindAnyObjectByType<Core.Player.PlayerLook>();
            if (look != null) look.SetEnabled(enabled);

            // Зум камеры колесом тоже глушим на время диалога.
            var cam = UnityEngine.Object.FindAnyObjectByType<Core.Player.ThirdPersonCamera>();
            if (cam != null) cam.SetZoomEnabled(enabled);
        }

        private void OnDestroy()
        {
            _nodeSub?.Dispose();
            _closedSub?.Dispose();
            if (_navigateAction != null) _navigateAction.performed -= OnNavigate;
            if (_confirmAction != null) _confirmAction.performed -= OnConfirm;
        }
    }
}
