using System;
using Core.Player;
using R3;
using UnityEngine;

namespace Core.Interaction
{
    /// <summary>
    /// Единая точка подсветки: слушает LookTarget.Target, гасит прошлый объект
    /// и зажигает новый. Вешается на игрока рядом с LookTarget и PlayerInteractor.
    /// </summary>
    [RequireComponent(typeof(LookTarget))]
    public class LookHighlighter : MonoBehaviour
    {
        [SerializeField] private bool _logs = true;

        private LookTarget _lookTarget;
        private IHighlightable _current;
        private IDisposable _sub;

        private void Awake()
        {
            _lookTarget = GetComponent<LookTarget>();

            int count = FindObjectsByType<LookTarget>(FindObjectsSortMode.None).Length;
            if (_logs)
                UnityEngine.Debug.Log($"[Highlight] старт на объекте '{name}'. " +
                                      $"LookTarget в сцене: {count}");
        }

        private void OnEnable()
        {
            _sub = _lookTarget.Target.Subscribe(t =>
            {
                if (_logs)
                    UnityEngine.Debug.Log($"[Highlight] цель: {(t == null ? "null" : t.name)}");

                var next = t == null ? null : t.GetComponentInParent<IHighlightable>();

                if (_logs && t != null && next == null)
                    UnityEngine.Debug.Log($"[Highlight] на '{t.name}' и родителях нет IHighlightable");

                Apply(next);
            });
        }

        private void OnDisable()
        {
            _sub?.Dispose();
            _sub = null;
            Apply(null);
        }

        private void Apply(IHighlightable next)
        {
            if (ReferenceEquals(_current, next)) return;

            _current?.SetHighlight(false);
            _current = next;
            _current?.SetHighlight(true);

            if (_logs)
                UnityEngine.Debug.Log($"[Highlight] подсветка -> {(next == null ? "выкл" : "вкл")}");
        }
    }
}