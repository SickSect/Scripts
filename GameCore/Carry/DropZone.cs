using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Core.Interaction;
using Core.Interaction.Interactables;

namespace Core.Carry
{
    [RequireComponent(typeof(Collider))]
    public class DropZone : MonoBehaviour
    {
        [Header("Настройки зоны")]
        [Tooltip("Уникальный ID зоны.")]
        [SerializeField] private string _zoneId = "";

        [Tooltip("Точка примагничивания.")]
        [SerializeField] private Transform _dropTargetPoint;

        [Header("События")]
        public UnityEvent<List<ScriptableObject>> OnDataDelivered;

        private Collider _zoneCollider;
        private readonly List<ScriptableObject> _storedData = new List<ScriptableObject>();

        // Храним ссылки на объекты, а не их ID. Это надёжнее и не зависит от устаревших методов.
        private readonly HashSet<Carryable> _activeCarryables = new HashSet<Carryable>();

        private Carryable _heldItemInZone;
        private Rigidbody _heldRbCache;

        private void Awake()
        {
            _zoneCollider = GetComponent<Collider>();
            if (!_zoneCollider.isTrigger)
                Debug.LogWarning($"[DropZone] Коллайдер на {name} НЕ является триггером!");

            if (_dropTargetPoint == null)
            {
                var pointObj = new GameObject($"{name}_DropPoint");
                pointObj.transform.SetParent(transform);
                pointObj.transform.localPosition = Vector3.zero;
                _dropTargetPoint = pointObj.transform;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
        }

        // OnTriggerExit намеренно пустой. Выход проверяется детерминированно в FixedUpdate.
        private void OnTriggerExit(Collider other)
        {
        }
    }
}