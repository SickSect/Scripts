using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Core.Player
{
    /// <summary>
    /// HUD «Изучить»: реплика (печатная машинка), кнопки выбора «на плечах» с навигацией
    /// клавиатурой/геймпадом и подсветкой, подсказка «Q — Изучить». Выбор: клик, цифры 1..9,
    /// стрелки+Enter, или крестовина+A на геймпаде.
    /// </summary>
    public class ExamineHudView : MonoBehaviour
    {
        [Header("Реплика")]
        [SerializeField] private CanvasGroup _lineGroup;
        [SerializeField] private TMP_Text _lineText;
        [SerializeField] private TMP_Text _speakerText;
        [SerializeField] private float _charsPerSecond = 45f;

        [Header("Выбор («плечи»)")]
        [SerializeField] private Button _choiceButtonPrefab;
        [SerializeField] private RectTransform _leftColumn;
        [SerializeField] private RectTransform _rightColumn;
        [SerializeField] private float _selectedScale = 1.08f;

        [Header("Подсказка (Q — Изучить)")]
        [SerializeField] private CanvasGroup _promptGroup;
        [SerializeField] private TMP_Text _promptText;

        private readonly List<Button> _spawned = new();
        private Coroutine _typing;
        private int _selected = -1;

        private void Awake()
        {
            SetGroup(_lineGroup, 0f);
            SetGroup(_promptGroup, 0f);
        }

        // ---- Подсказка ----
        public void ShowPrompt(string text)
        {
            if (_promptText != null) _promptText.text = text;
            SetGroup(_promptGroup, 1f);
        }
        public void HidePrompt() => SetGroup(_promptGroup, 0f);

        // ---- Реплика (печатная машинка) ----
        public void ShowLine(string speaker, string text, float holdSeconds)
        {
            if (_speakerText != null) _speakerText.text = speaker ?? "";
            SetGroup(_lineGroup, 1f);
            if (_typing != null) StopCoroutine(_typing);
            _typing = StartCoroutine(Typewriter(text ?? ""));
        }

        public void HideLine()
        {
            if (_typing != null) { StopCoroutine(_typing); _typing = null; }
            SetGroup(_lineGroup, 0f);
        }

        private IEnumerator Typewriter(string text)
        {
            if (_lineText == null) yield break;
            _lineText.text = text;
            _lineText.maxVisibleCharacters = 0;
            _lineText.ForceMeshUpdate();
            int total = _lineText.textInfo.characterCount;

            float cps = Mathf.Max(1f, _charsPerSecond), shown = 0f;
            while (shown < total)
            {
                shown += Time.unscaledDeltaTime * cps;
                _lineText.maxVisibleCharacters = Mathf.Clamp((int)shown, 0, total);
                yield return null;
            }
            _lineText.maxVisibleCharacters = total;
            _typing = null;
        }

        // ---- Выбор ----
        public void ShowChoices(IReadOnlyList<string> labels, Action<int> onChosen)
        {
            HideChoices();
            if (_choiceButtonPrefab == null || labels == null) return;

            for (int i = 0; i < labels.Count; i++)
            {
                RectTransform column = (i % 2 == 0) ? _leftColumn : _rightColumn;
                if (column == null) column = _leftColumn != null ? _leftColumn : _rightColumn;

                Button b = Instantiate(_choiceButtonPrefab, column != null ? column : transform);
                b.gameObject.SetActive(true);
                var label = b.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = labels[i];

                int index = i;
                b.onClick.AddListener(() => onChosen?.Invoke(index));
                AddHover(b, () => _selected = index);   // навёл мышью — подсветить
                _spawned.Add(b);
            }
            _selected = _spawned.Count > 0 ? 0 : -1;
        }

        public void HideChoices()
        {
            foreach (var b in _spawned) if (b != null) Destroy(b.gameObject);
            _spawned.Clear();
            _selected = -1;
        }

        private void Update()
        {
            if (_spawned.Count == 0) return;
            HandleNavigation();
            HandleConfirmAndDigits();
            UpdateHighlight();
        }

        private void HandleNavigation()
        {
            int move = 0;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.downArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) move++;
                if (kb.upArrowKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) move--;
            }
            var gp = Gamepad.current;
            if (gp != null)
            {
                if (gp.dpad.down.wasPressedThisFrame || gp.dpad.right.wasPressedThisFrame) move++;
                if (gp.dpad.up.wasPressedThisFrame || gp.dpad.left.wasPressedThisFrame) move--;
            }
            if (move != 0 && _selected >= 0)
                _selected = (_selected + move + _spawned.Count) % _spawned.Count;
        }

        private void HandleConfirmAndDigits()
        {
            var kb = Keyboard.current;
            var gp = Gamepad.current;

            bool confirm = kb != null && (kb.enterKey.wasPressedThisFrame ||
                                          kb.numpadEnterKey.wasPressedThisFrame ||
                                          kb.spaceKey.wasPressedThisFrame);
            if (gp != null && gp.buttonSouth.wasPressedThisFrame) confirm = true;

            if (confirm && _selected >= 0 && _selected < _spawned.Count)
            {
                if (_spawned[_selected] != null) _spawned[_selected].onClick.Invoke();
                return;
            }

            if (kb != null)
                for (int i = 0; i < _spawned.Count && i < 9; i++)
                {
                    var key = kb[(Key)((int)Key.Digit1 + i)];
                    if (key != null && key.wasPressedThisFrame)
                    {
                        if (_spawned[i] != null) _spawned[i].onClick.Invoke();
                        break;
                    }
                }
        }

        private void UpdateHighlight()
        {
            float k = 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime);
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] == null) continue;
                float target = (i == _selected) ? _selectedScale : 1f;
                var t = _spawned[i].transform;
                t.localScale = Vector3.Lerp(t.localScale, Vector3.one * target, k);
            }
        }

        private void AddHover(Button b, Action onEnter)
        {
            var trigger = b.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ => onEnter());
            trigger.triggers.Add(entry);
        }

        private static void SetGroup(CanvasGroup g, float a)
        {
            if (g == null) return;
            g.alpha = a;
            g.blocksRaycasts = a > 0.5f;
        }
    }
}