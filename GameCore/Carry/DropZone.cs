using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Core.Interaction;

namespace Core.Interaction.Interactables
{
    /// <summary>
    /// Зона для размещения переносимых объектов.
    /// Проверяет наличие объектов, реализующих интерфейс ICarryable, внутри своего коллайдера-триггера.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DropZone : MonoBehaviour
    {
        [Header("Настройки зоны")]
        [Tooltip("Список тегов или имен объектов, которые зона принимает (опционально). Если пусто - принимает любой ICarryable.")]
        [SerializeField] private string[] _acceptedTags;

        [Tooltip("Событие вызывается, когда в зоне появляется правильный предмет.")]
        [SerializeField] private UnityEvent<GameObject> _onItemPlaced;

        [Tooltip("Событие вызывается, когда из зоны убирают предмет.")]
        [SerializeField] private UnityEvent<GameObject> _onItemRemoved;

        [Tooltip("Нужно ли уничтожать предмет после успешной доставки?")]
        [SerializeField] private bool _destroyOnSuccess = false;

        private Collider _zoneCollider;
        private readonly List<ICarryable> _containedItems = new List<ICarryable>();

        private void Awake()
        {
            _zoneCollider = GetComponent<Collider>();

            if (!_zoneCollider.isTrigger)
            {
                Debug.LogWarning($"[DropZone] Коллайдер на объекте {name} не является триггером (Is Trigger = false). Зона не сможет детектировать предметы автоматически через OnTrigger. Установите Is Trigger = true.");
                // Мы можем работать и без триггера через ручной вызов CheckCompletion, но лучше исправить в редакторе.
            }
        }

        /// <summary>
        /// Вызывается, когда объект попадает в триггер зоны (например, упал при броске).
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            TryAddItem(other);
        }

        /// <summary>
        /// Вызывается, когда объект покидает триггер зоны (например, его подобрали обратно).
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            TryRemoveItem(other);
        }

        private void TryAddItem(Collider other)
        {
            // Ищем интерфейс ICarryable в иерархии объекта (вдруг он на родителе)
            ICarryable carryable = other.GetComponentInParent<ICarryable>();

            if (carryable != null)
            {
                // Проверка по тегам/именам, если настроено
                if (!IsAccepted(carryable.Transform.gameObject))
                {
                    return;
                }

                if (!_containedItems.Contains(carryable))
                {
                    _containedItems.Add(carryable);
                    Debug.Log($"[DropZone] Предмет добавлен: {carryable.Transform.name}. Всего предметов: {_containedItems.Count}");

                    _onItemPlaced?.Invoke(carryable.Transform.gameObject);

                    // Опционально: можно сразу проверять условие победы, если нужен ровно 1 предмет
                    // CheckCompletion(); 
                }
            }
        }

        private void TryRemoveItem(Collider other)
        {
            ICarryable carryable = other.GetComponentInParent<ICarryable>();

            if (carryable != null && _containedItems.Contains(carryable))
            {
                _containedItems.Remove(carryable);
                Debug.Log($"[DropZone] Предмет удален: {carryable.Transform.name}. Осталось: {_containedItems.Count}");

                _onItemRemoved?.Invoke(carryable.Transform.gameObject);
            }
        }

        /// <summary>
        /// Ручная проверка содержимого зоны.
        /// Полезно, если физика триггеров сработала некорректно или предмет телепортировали.
        /// </summary>
        public void CheckCompletion()
        {
            // Очищаем список от "мертвых" ссылок или предметов, которые физически уже не в зоне
            _containedItems.RemoveAll(item => item == null || !IsInsideZone(item.Transform));

            // Логика проверки условия победы/задачи
            if (_containedItems.Count > 0)
            {
                Debug.Log($"[DropZone] Проверка успешна. В зоне {_containedItems.Count} предметов.");

                if (_destroyOnSuccess)
                {
                    foreach (var item in _containedItems)
                    {
                        Destroy(item.Transform.gameObject);
                    }
                    _containedItems.Clear();
                }
            }
            else
            {
                Debug.Log("[DropZone] Зона пуста.");
            }
        }

        private bool IsAccepted(GameObject obj)
        {
            if (_acceptedTags == null || _acceptedTags.Length == 0)
                return true;

            foreach (string tag in _acceptedTags)
            {
                if (obj.CompareTag(tag) || obj.name.Contains(tag))
                    return true;
            }
            return false;
        }

        private bool IsInsideZone(Transform itemTransform)
        {
            if (_zoneCollider == null) return false;

            // Простая проверка через Bounds коллайдера
            Bounds bounds = _zoneCollider.bounds;
            return bounds.Contains(itemTransform.position);
        }

        // Отладка в редакторе
        private void OnDrawGizmosSelected()
        {
            if (_zoneCollider != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(_zoneCollider.bounds.center, _zoneCollider.bounds.size);
            }
        }
    }
}