using System.Collections.Generic;
using UnityEngine;
using Core.Interaction;

namespace Core.Interaction.Interactables
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Carryable : MonoBehaviour, ICarryable, ICarryData
    {
        [Header("Визуал")]
        [SerializeField] private bool _useHighlight = true;
        [SerializeField] private Color _holdColor = Color.yellow;

        [Header("Логика зоны")]
        [Tooltip("ID зоны, в которую можно положить этот предмет. Пусто - в любую.")]
        [SerializeField] private string _targetZoneId = "";

        [Header("Данные (для примера)")]
        [Tooltip("Список данных, которые предмет передаст в зону.")]
        [SerializeField] private List<ScriptableObject> _containedData;

        private Rigidbody _rb;
        private Renderer[] _renderers;
        private Color[] _originalColors;
        private bool _isHeld = false;

        public Transform Transform => transform;
        public Rigidbody Rigidbody => _rb;

        // Реализация ICarryData
        public string TargetZoneId => _targetZoneId;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _renderers = GetComponentsInChildren<Renderer>();

            if (_useHighlight && _renderers.Length > 0)
            {
                _originalColors = new Color[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _originalColors[i] = _renderers[i].material.color;
                }
            }

            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public void OnPickUp()
        {
            if (_isHeld) return;
            _isHeld = true;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;

            if (_useHighlight) ApplyColor(_holdColor);
        }

        public void OnDrop()
        {
            if (!_isHeld) return;
            _isHeld = false;
            _rb.isKinematic = false;

            if (_useHighlight) RestoreColors();
        }

        // Реализация метода получения данных
        public List<ScriptableObject> GetData()
        {
            return _containedData;
        }

        #region Helpers
        private void ApplyColor(Color color)
        {
            foreach (var r in _renderers)
            {
                if (r != null) r.material.color = color;
            }
        }

        private void RestoreColors()
        {
            if (_originalColors == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null && i < _originalColors.Length)
                {
                    _renderers[i].material.color = _originalColors[i];
                }
            }
        }
        #endregion
    }
}