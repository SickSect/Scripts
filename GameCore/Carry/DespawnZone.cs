using Core.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Carry
{
    /// <summary>
    /// Зона уничтожения: мусоропровод, мусорное ведро, окно.
    /// Всё, что сюда попадает (и реализует ICarryable), исчезает без следа.
    ///
    /// Нужен коллайдер с Is Trigger = true.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DespawnZone : MonoBehaviour
    {
        [Tooltip("Пусто — принимает любой объект с ICarryable. Заполнено — только объекты с определённым тегом.")]
        [SerializeField] private string _acceptTag = "";

        [Tooltip("Пауза перед исчезновением: предмет успевает провалиться внутрь/упасть.")]
        [SerializeField] private float _delay = 0.4f;

        [Tooltip("Событие вызывается при уничтожении предмета.")]
        public UnityEvent onDespawned;

        [SerializeField] private bool _debugLog = false;

        private void OnTriggerEnter(Collider other)
        {
            // Ищем интерфейс переносимого объекта
            ICarryable carryable = other.GetComponentInParent<ICarryable>();

            if (carryable == null)
            {
                // Если это не переносимый объект, игнорируем
                return;
            }

            GameObject itemObj = carryable.Transform.gameObject;

            // Проверка по тегу, если он задан
            if (!string.IsNullOrEmpty(_acceptTag))
            {
                if (!itemObj.CompareTag(_acceptTag))
                {
                    if (_debugLog) Debug.Log($"[DespawnZone] '{name}' отверг '{itemObj.name}' (тег не совпадает: {_acceptTag})");
                    return;
                }
            }

            // Дополнительная защита: если предмет всё ещё "в руке" (кинematic), 
            // то возможно он просто проходит сквозь зону пока его несут.
            // Но обычно MouseInteractor сам бросает предмет до попадания в зону, 
            // либо мы хотим удалить его даже если игрок сунул его туда прямо в руке.
            // Оставим простую логику: попал в триггер -> удаляется.

            if (carryable.Rigidbody != null && carryable.Rigidbody.isKinematic)
            {
                // Опционально: можно заставить выбросить предмет перед удалением
                // Но для зоны уничтожения это не критично.
                if (_debugLog) Debug.Log($"[DespawnZone] '{name}' поглощает предмет '{itemObj.name}' прямо из руки.");
            }

            if (_debugLog) Debug.Log($"[DespawnZone] '{name}' поглотил '{itemObj.name}'");

            onDespawned?.Invoke();

            // Уничтожаем объект с задержкой
            Destroy(itemObj, _delay);
        }
    }
}