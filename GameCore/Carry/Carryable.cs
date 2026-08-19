using System.Collections.Generic;
using UnityEngine;
using Core.Carry;
using System;

namespace Core.Interaction.Interactables
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Carryable : MonoBehaviour, ICarryable, ICarryData, IInteractable
    {
        [Header("Настройки зоны")]
        [Tooltip("ID зоны, куда нужно доставить этот предмет. Пусто = любая зона.")]
        [SerializeField] private string _targetZoneId = "";

        [Header("Визуал")]
        [SerializeField] private bool _useHighlight = true;
        [SerializeField] private Color _holdColor = Color.yellow;

        private Rigidbody _rb;
        private Collider _col;
        private Renderer[] _renderers;
        private Color[] _originalColors;

        // Состояние: держим ли мы предмет мышкой
        private bool _isHeldByMouse = false;

        public Transform Transform => transform;
        public Rigidbody Rigidbody => _rb;
        public string TargetZoneId => _targetZoneId;
        public bool IsHeld => _isHeldByMouse;

        [Header("Можно ли поднять объект")]
        [Tooltip("Влияет на возможность поднять предмет. если true, то поднимаем, если false - вызывается метод interract")]
        private Boolean isPickupable = true;

        public string Prompt => throw new System.NotImplementedException();

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
            _renderers = GetComponentsInChildren<Renderer>();

            if (_useHighlight && _renderers.Length > 0)
            {
                _originalColors = new Color[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                    _originalColors[i] = _renderers[i].material.color;
            }

            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public List<ScriptableObject> GetData()
        {
            // Заглушка для примера. Верни свой список SO.
            return new List<ScriptableObject>();
        }

        public void Interact(InteractionContext context)
        {
            
        }

        public void OnPickUp()
        {

        }

        public void OnDrop()
        {

        }

        public bool isPickupble()
        {
            return isPickupable;
        }
    }
}