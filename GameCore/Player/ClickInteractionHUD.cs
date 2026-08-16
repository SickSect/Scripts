using Core.Interaction;
using R3;
using System;
using TMPro;
using UnityEngine;

namespace Core.UI.HUD
{
    /// <summary>
    /// HUD взаимодействия для режима point-and-click: показывает подсказку о действии
    /// при наведении курсора на интерактивный объект.
    /// 
    /// Подписан на MouseInteractor.HoveredPrompt — когда под курсором появляется
    /// IInteractable, показывает подсказку с текстом действия.
    /// 
    /// В отличие от InteractionHUD, не имеет кроссхейра и работает от позиции курсора мыши.
    /// </summary>
    public class ClickInteractionHUD : MonoBehaviour
    {
        [Header("Хинт взаимодействия (появляется на IInteractable)")]
        [SerializeField] private RectTransform _hintRoot;
        [SerializeField] private TMP_Text _hintLabel;
        [SerializeField] private Vector2 _offset = new Vector2(15f, 15f);

        [Header("Отладка")]
        [SerializeField] private bool _debug = false;

        private MouseInteractor _mouseInteractor;
        private IDisposable _sub;
        private Camera _camera;

        private void Awake()
        {
            if (_hintRoot != null) _hintRoot.gameObject.SetActive(false);
            
            _camera = Camera.main;
            
            if (_debug)
            {
                if (_hintRoot == null) Debug.LogWarning("[ClickInteractionHUD] _hintRoot НЕ назначен");
                if (_hintLabel == null) Debug.LogWarning("[ClickInteractionHUD] _hintLabel НЕ назначен");
            }
        }

        /// <summary>Привязать источник наведения (MouseInteractor).</summary>
        public void SetInteractor(MouseInteractor interactor)
        {
            if (_debug) Debug.Log($"[ClickInteractionHUD] SetInteractor вызван: {(interactor != null ? interactor.name : "NULL")}");

            _sub?.Dispose();
            _mouseInteractor = interactor;

            if (_mouseInteractor == null)
            {
                if (_debug) Debug.LogWarning("[ClickInteractionHUD] SetInteractor получил NULL — подписки нет");
                return;
            }

            _sub = _mouseInteractor.HoveredPrompt.Subscribe(OnPromptChanged);
            if (_debug) Debug.Log("[ClickInteractionHUD] Подписка на HoveredPrompt оформлена");
        }

        private void OnPromptChanged(string prompt)
        {
            if (_debug) Debug.Log($"[ClickInteractionHUD] промпт изменился: '{prompt}'");

            if (string.IsNullOrEmpty(prompt))
            {
                HideHint();
                return;
            }

            ShowHint(prompt);
        }

        private void ShowHint(string prompt)
        {
            if (_hintLabel != null) _hintLabel.text = prompt;
            if (_hintRoot != null)
            {
                _hintRoot.gameObject.SetActive(true);
                UpdatePosition();
            }

            if (_debug && _hintRoot == null)
                Debug.LogWarning("[ClickInteractionHUD] нашёл интерактабельный объект, но _hintRoot == null");
        }

        private void HideHint()
        {
            if (_hintRoot != null) _hintRoot.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_hintRoot != null && _hintRoot.gameObject.activeSelf)
            {
                UpdatePosition();
            }
        }

        private void UpdatePosition()
        {
            if (_camera == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _hintRoot.parent as RectTransform,
                    Input.mousePosition,
                    _camera,
                    out Vector2 localPoint))
            {
                return;
            }

            _hintRoot.anchoredPosition = localPoint + _offset;
        }

        private void OnDestroy() => _sub?.Dispose();
    }
}
