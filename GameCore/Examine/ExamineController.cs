using System.Collections;
using System.Collections.Generic;
using Core.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace Core.Player
{
    /// <summary>
    /// Селектор игровых катсцен + кинематография (авто fade/леттербокс, индикатор шанса,
    /// пропуск, подсказка, наезд URP Volume). Меню можно открыть по Q (прицел) или снаружи
    /// через Open() — например, из ExamineInteractable по нажатию E (дверь). Выбор может
    /// требовать предмет из инвентаря; действие без катсцены проходит без затемнения.
    /// </summary>
    public class ExamineController : MonoBehaviour
    {
        [Header("Поиск объекта под прицелом")]
        [SerializeField] private float _range = 3f;
        [Tooltip("Слой(и) интерактивных объектов. Убери отсюда слой игрока.")]
        [SerializeField] private LayerMask _interactMask = ~0;

        [Header("Кинематография")]
        [SerializeField] private float _fadeTime = 0.25f;
        [SerializeField, Range(0f, 0.2f)] private float _letterbox = 0.11f;
        [Tooltip("Необязательный URP Volume — плавно наезжает на время изучения.")]
        [SerializeField] private Volume _examineVolume;

        [Header("Подсказка")]
        [SerializeField] private string _promptLabel = "Q — Изучить";

        [Header("Прочее")]
        [SerializeField] private float _menuBeat = 0.35f;
        [SerializeField] private bool _freeCursorInMenu = true;
        [SerializeField] private bool _debug = true;

        private Camera _cam;
        private Camera _gameplayCam;
        private PlayerMovement _player;
        private PlayerLook _look;
        private Rigidbody _playerRb;
        private Renderer[] _playerRenderers;
        private ExamineHudView _hud;
        private ExamineOverlay _overlay;

        private readonly Dictionary<GameObject, bool> _hidden = new();
        private readonly List<Renderer> _hiddenRenderers = new();

        private ExaminePoint _current;
        private bool _busy;

        public ExaminePoint Current => _current;

        private void Update()
        {
            if (_busy) return;

            _current = FindTarget();
            bool qReady = _current != null && _current.RespondToQ;
            UpdatePrompt(qReady);

            var kb = Keyboard.current;
            if (qReady && kb != null && kb.qKey.wasPressedThisFrame)
                StartCoroutine(Run(_current));
        }

        /// <summary>Открыть меню объекта снаружи (напр. из ExamineInteractable по E).</summary>
        public void Open(ExaminePoint point)
        {
            if (_busy || point == null) return;
            _current = point;
            StartCoroutine(Run(point));
        }

        private void UpdatePrompt(bool show)
        {
            if (_hud == null) _hud = FindAnyObjectByType<ExamineHudView>();
            if (_hud == null) return;
            if (show) _hud.ShowPrompt(_promptLabel);
            else _hud.HidePrompt();
        }

        private ExaminePoint FindTarget()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return null;

            var t = _cam.transform;
            if (Physics.Raycast(t.position, t.forward, out RaycastHit hit,
                                _range, _interactMask, QueryTriggerInteraction.Ignore))
                return hit.collider.GetComponentInParent<ExaminePoint>();
            return null;
        }

        private IEnumerator Run(ExaminePoint point)
        {
            if (!ResolveDeps()) yield break;
            _busy = true;
            _hud.HidePrompt();

            LockGameplay(true);
            yield return _overlay.FadeTo(1f, _fadeTime);
            HidePlayer();
            HideObjects(point.HideDuringThinking);
            ActivateScene(point.ThinkingScene);          // пусто = меню поверх текущего вида
            StartCoroutine(FadeVolume(1f, 0.4f));
            yield return _overlay.FadeTo(0f, _fadeTime);
            StartCoroutine(_overlay.LetterboxTo(_letterbox, 0.35f));

            if (!string.IsNullOrEmpty(point.ExamineLine))
                _hud.ShowLine(point.Speaker, point.ExamineLine, 999f);
            yield return new WaitForSeconds(_menuBeat);

            bool exit = false;
            while (!exit)
            {
                var slots = new List<int>();
                var labels = new List<string>();
                for (int i = 0; i < point.Choices.Count; i++)
                {
                    var ch = point.Choices[i];
                    if (point.IsConsumed(i)) continue;
                    if (!MeetsRequirement(ch)) continue;     // нет нужного предмета → не показываем
                    slots.Add(i);
                    labels.Add(ch.Label + ChanceLabel(ch));
                }
                if (labels.Count == 0) break;

                int picked = -1;
                _hud.ShowChoices(labels, i => picked = i);
                while (picked < 0)
                {
                    var kb = Keyboard.current;
                    if (kb != null && kb.escapeKey.wasPressedThisFrame) { picked = -2; break; }
                    yield return null;
                }
                _hud.HideChoices();

                if (picked < 0 || picked >= slots.Count) { exit = true; break; }

                var choice = point.Choices[slots[picked]];
                if (choice.IsLeave) { exit = true; break; }

                // Списываем предмет за использование (если задано).
                if (choice.ConsumeItem && choice.RequiredItem != null)
                    ExamineServices.Inventory?.Remove(choice.RequiredItem, Mathf.Max(1, choice.RequiredCount));

                bool success = choice.SuccessChance >= 1f
                    || (choice.SuccessChance > 0f && Random.value < choice.SuccessChance);
                if (_debug) Debug.Log($"[Examine] «{choice.Label}»: {(success ? "УСПЕХ" : "ПРОВАЛ")}");

                GameObject scene = success ? choice.SuccessScene : choice.FailScene;
                float dur = success ? choice.SuccessDuration : choice.FailDuration;

                if (scene != null)     // с катсценой
                {
                    _hud.HideLine();
                    yield return PlayCutscene(point.ThinkingScene, scene, dur,
                                              point.HideDuringCutscene, choice.HideDuringCutscene);
                }

                if (success)
                {
                    ApplyAll(choice.EnableOnSuccess, true);
                    ApplyAll(choice.DisableOnSuccess, false);
                    Forget(choice.EnableOnSuccess);
                    Forget(choice.DisableOnSuccess);
                    if (choice.ConsumeOnSuccess) point.MarkConsumed(slots[picked]);
                }

                if (scene != null) RestoreHidden();

                if (choice.ExitAfterCutscene) { exit = true; break; }

                if (scene != null)     // после катсцены — вернуться в раздумье
                {
                    HideObjects(point.HideDuringThinking);
                    ActivateScene(point.ThinkingScene);
                    yield return _overlay.FadeTo(0f, _fadeTime);
                    _hud.ShowLine(point.Speaker, point.ExamineLine, 999f);
                }
                // без катсцены реплика и вид остаются — меню просто перерисуется
            }

            _hud.HideLine();
            yield return _overlay.FadeTo(1f, _fadeTime);
            DeactivateScenes(point);
            RestoreHidden();
            ShowPlayer();
            _overlay.SetLetterbox(0f);
            StartCoroutine(FadeVolume(0f, 0.4f));
            yield return _overlay.FadeTo(0f, _fadeTime);
            LockGameplay(false);
            _busy = false;
        }

        private bool MeetsRequirement(ExaminePoint.Choice c)
        {
            if (c.RequiredItem == null) return true;
            var inv = ExamineServices.Inventory;
            if (inv == null)
            {
                Debug.LogWarning("[Examine] Инвентарь не привязан (ExamineServices.Inventory) — пункт с предметом скрыт. Добавь строку в InventoryInitStep.");
                return false;
            }
            return inv.Has(c.RequiredItem, Mathf.Max(1, c.RequiredCount));
        }

        private IEnumerator PlayCutscene(GameObject thinking, GameObject scene, float dur,
                                         GameObject[] hideA, GameObject[] hideB)
        {
            yield return _overlay.FadeTo(1f, _fadeTime);
            SetActiveSafe(thinking, false);

            RestoreHidden();
            HideObjects(hideA);
            HideObjects(hideB);

            ActivateScene(scene);
            yield return _overlay.FadeTo(0f, _fadeTime);
            yield return WaitOrSkip(dur);
            yield return _overlay.FadeTo(1f, _fadeTime);
            scene.SetActive(false);
        }

        private IEnumerator WaitOrSkip(float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                if (SkipPressed()) yield break;
                t += Time.deltaTime;
                yield return null;
            }
        }

        private static bool SkipPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame ||
                               kb.numpadEnterKey.wasPressedThisFrame ||
                               kb.spaceKey.wasPressedThisFrame)) return true;
            var gp = Gamepad.current;
            return gp != null && gp.buttonSouth.wasPressedThisFrame;
        }

        private static string ChanceLabel(ExaminePoint.Choice c)
        {
            if (c.IsLeave || c.SuccessChance >= 1f || c.SuccessChance <= 0f) return "";
            return $"  <color=#FFD24A>{Mathf.RoundToInt(c.SuccessChance * 100f)}%</color>";
        }

        private IEnumerator FadeVolume(float target, float time)
        {
            if (_examineVolume == null) yield break;
            float start = _examineVolume.weight, t = 0f;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                _examineVolume.weight = Mathf.Lerp(start, target, t / time);
                yield return null;
            }
            _examineVolume.weight = target;
        }

        private void ActivateScene(GameObject scene)
        {
            SetActiveSafe(scene, true);
            if (scene == null) return;

            var cam = scene.GetComponentInChildren<Camera>(true);
            if (cam == null)
            {
                Debug.LogError($"[Examine] В сцене '{scene.name}' НЕТ камеры! Добавь Camera внутрь неё.", scene);
                return;
            }
            if (_gameplayCam != null) cam.depth = _gameplayCam.depth + 10f;
            if (cam.clearFlags == CameraClearFlags.Depth || cam.clearFlags == CameraClearFlags.Nothing)
                cam.clearFlags = CameraClearFlags.Skybox;
        }

        // --- Скрытие ---
        private void HideObjects(GameObject[] list)
        {
            if (list == null) return;
            foreach (var go in list)
                if (go != null && !_hidden.ContainsKey(go)) { _hidden[go] = go.activeSelf; go.SetActive(false); }
        }
        private void Forget(GameObject[] list)
        {
            if (list == null) return;
            foreach (var go in list) if (go != null) _hidden.Remove(go);
        }
        private void RestoreHidden()
        {
            foreach (var kv in _hidden) if (kv.Key != null) kv.Key.SetActive(kv.Value);
            _hidden.Clear();
        }

        private void HidePlayer()
        {
            _hiddenRenderers.Clear();
            if (_playerRenderers == null) return;
            foreach (var r in _playerRenderers)
                if (r != null && r.enabled) { _hiddenRenderers.Add(r); r.enabled = false; }
        }
        private void ShowPlayer()
        {
            foreach (var r in _hiddenRenderers) if (r != null) r.enabled = true;
            _hiddenRenderers.Clear();
        }

        private void LockGameplay(bool locked)
        {
            if (_look != null) _look.SetEnabled(!locked);
            if (_player != null) _player.enabled = !locked;
            if (locked && _playerRb != null) _playerRb.linearVelocity = Vector3.zero;

            if (_freeCursorInMenu)
            {
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }
        }

        private static void ApplyAll(GameObject[] list, bool on)
        {
            if (list == null) return;
            foreach (var go in list) SetActiveSafe(go, on);
        }
        private static void SetActiveSafe(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }
        private void DeactivateScenes(ExaminePoint point)
        {
            SetActiveSafe(point.ThinkingScene, false);
            foreach (var c in point.Choices)
            {
                SetActiveSafe(c.SuccessScene, false);
                SetActiveSafe(c.FailScene, false);
            }
        }

        private bool ResolveDeps()
        {
            if (_player == null) _player = FindAnyObjectByType<PlayerMovement>();
            if (_player == null) return false;

            if (_look == null) _look = _player.GetComponent<PlayerLook>();
            if (_playerRb == null) _playerRb = _player.GetComponent<Rigidbody>();
            if (_playerRenderers == null) _playerRenderers = _player.GetComponentsInChildren<Renderer>(true);
            if (_cam == null) _cam = Camera.main;
            if (_gameplayCam == null) _gameplayCam = _cam;
            if (_overlay == null) _overlay = ExamineOverlay.Create();
            if (_examineVolume != null) _examineVolume.weight = 0f;

            if (_hud == null) _hud = FindAnyObjectByType<ExamineHudView>();
            if (_hud == null) { Debug.LogWarning("[Examine] Нет ExamineHudView на сцене."); return false; }
            return true;
        }
    }
}