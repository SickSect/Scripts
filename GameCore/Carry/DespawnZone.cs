using UnityEngine;
using UnityEngine.Events;

namespace Core.Carry
{
    /// <summary>
    /// Зона уничтожения: мусоропровод, мусорное ведро, окно.
    /// Всё, что сюда попадает, исчезает без следа.
    ///
    /// Нужен коллайдер с Is Trigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DespawnZone : MonoBehaviour
    {
        [Tooltip("Пусто — принимает любой Carryable. Заполнено — только с этим ключом.")]
        [SerializeField] private string _acceptKey = "";

        [Tooltip("Пауза перед исчезновением: предмет успевает провалиться внутрь.")]
        [SerializeField] private float _delay = 0.4f;

        public UnityEvent onDespawned;

        [SerializeField] private bool _debugLog = false;

        private void OnTriggerEnter(Collider other)
        {
            var item = other.GetComponentInParent<Carryable>();

            if (item == null || item.IsHeld) return;
            if (!string.IsNullOrEmpty(_acceptKey) && item.ZoneKey != _acceptKey) return;

            // Предмет мог лежать в рабочей зоне — освободим её, иначе она
            // останется занятой навсегда.
            if (item.CurrentZone != null) item.CurrentZone.Vacate();

            if (_debugLog) Debug.Log($"[DespawnZone] '{name}' поглотил '{item.name}'");

            onDespawned?.Invoke();
            Destroy(item.gameObject, _delay);
        }
    }
}
