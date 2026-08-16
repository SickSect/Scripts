using Core.Interaction;
using Core.Player;
using R3;
using System;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Core.UI.HUD
{
    /// <summary>
    /// HUD взаимодействия: кроссхейр по центру + хинт «нажми E» с текстом Prompt
    /// объекта под прицелом. Подписан на LookTarget.Target — когда под прицелом
    /// появляется IInteractable, показывает подсказку.
    ///
    /// Ссылку на LookTarget получает из InitStep (SetTarget), т.к. игрок спавнится
    /// в рантайме. Живёт как сценовый/глобальный HUD.
    ///
    /// Прячется целиком через SetVisible — это зовёт CameraFocusService, когда
    /// игрок садится за компьютер или уходит в любой другой крупный план.
    ///
    /// ДИАГНОСТИКА: включи _debug — в консоль пойдут логи о привязке и смене цели,
    /// чтобы понять, где рвётся цепочка (не зовут SetTarget / прилетает null /
    /// не назначены _hintRoot/_hintLabel).
    /// </summary>
    public class InteractionHUD : MonoBehaviour
    {
        [Header("Кроссхейр")]
        [SerializeField] private GameObject _crosshair;

        [Header("Хинт взаимодействия (появляется на IInteractable)")]
        [SerializeField] private GameObject _hintRoot;
        [SerializeField] private TMP_Text _hintLabel;
        [SerializeField] private string _keyName = "E";

        [Header("Отладка")]
        [SerializeField] private bool _debug = true;

        private LookTarget _lookTarget;
        private IDisposable _sub;

        private bool _visible = true;
        private bool _hintWanted;

        private void Awake()
        {
            if (_crosshair != null) _crosshair.SetActive(true);
            if (_hintRoot != null) _hintRoot.SetActive(false);

            // Сразу подсветим, если забыли назначить ссылки в инспекторе.
            if (_debug)
            {
                if (_hintRoot == null) Debug.LogWarning("[HUD] _hintRoot НЕ назначен — хинт не покажется.");
                if (_hintLabel == null) Debug.LogWarning("[HUD] _hintLabel НЕ назначен — текст хинта не выведется.");
                if (_crosshair == null) Debug.LogWarning("[HUD] _crosshair НЕ назначен.");
            }
        }

        /// <summary>
        /// Показать или скрыть весь HUD. В крупном плане прицел и подсказка
        /// не нужны: игрок работает мышью, а не смотрит по сторонам.
        /// </summary>
        public void SetVisible(bool visible)
        {
            _visible = visible;
            Apply();

            if (_debug) Debug.Log($"[HUD] видимость: {visible}");
        }

        /// <summary>Привязать источник взгляда (из InitStep при спавне игрока).</summary>
        public void SetTarget(LookTarget lookTarget)
        {
            if (_debug) Debug.Log($"[HUD] SetTarget вызван: {(lookTarget != null ? lookTarget.name : "NULL")}");

            _sub?.Dispose();
            _lookTarget = lookTarget;

            if (_lookTarget == null)
            {
                if (_debug) Debug.LogWarning("[HUD] SetTarget получил NULL — подписки нет, хинт работать не будет.");
                return;
            }

            // Реагируем на смену объекта под прицелом.
            _sub = _lookTarget.Target.Subscribe(OnTargetChanged);
            if (_debug) Debug.Log("[HUD] Подписка на LookTarget.Target оформлена.");
        }

        private void OnTargetChanged(GameObject go)
        {
            var interactable = go != null ? go.GetComponentInParent<IInteractable>() : null;

            if (_debug)
            {
                string goName = go != null ? go.name : "null";
                string hasInter = interactable != null ? "ЕСТЬ IInteractable" : "нет IInteractable";
                string prompt = interactable != null ? $"prompt='{interactable.Prompt}'" : "";
                Debug.Log($"[HUD] цель под прицелом: {goName} → {hasInter} {prompt}");
            }

            if (interactable == null || string.IsNullOrEmpty(interactable.Prompt))
            {
                _hintWanted = false;
                Apply();
                return;
            }

            if (_hintLabel != null) _hintLabel.text = $"[{_keyName}] {interactable.Prompt}";

            _hintWanted = true;
            Apply();

            if (_hintRoot == null && _debug)
                Debug.LogWarning("[HUD] нашёл интерактабельный объект, но _hintRoot == null — показать нечего.");
        }

        private void Apply()
        {
            if (_crosshair != null) _crosshair.SetActive(_visible);
            if (_hintRoot != null) _hintRoot.SetActive(_visible && _hintWanted);
        }

        private void OnDestroy() => _sub?.Dispose();
    }
}
