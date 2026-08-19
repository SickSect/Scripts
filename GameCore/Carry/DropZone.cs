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
        private ICarryable _heldItemInZone;

        // Кэш Rigidbody предмета, чтобы не искать каждый кадр
        private Rigidbody _heldRbCache;

        private void Awake()
        {
            _zoneCollider = GetComponent<Collider>();

            // Важно: Коллайдер должен быть Триггером
            if (!_zoneCollider.isTrigger)
            {
                Debug.LogWarning($"[DropZone] Коллайдер на {name} НЕ является триггером! Установите Is Trigger = true.");
            }

            if (_dropTargetPoint == null)
            {
                GameObject pointObj = new GameObject($"{name}_DropPoint");
                pointObj.transform.SetParent(transform);
                pointObj.transform.localPosition = Vector3.zero;
                _dropTargetPoint = pointObj.transform;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Ищем переносимый объект
            ICarryable carryable = other.GetComponentInParent<ICarryable>();
            if (carryable == null) return;

            // 1. Проверка ID зоны
            if (!string.IsNullOrEmpty(_zoneId))
            {
                if (carryable is ICarryData dataCarrier)
                {
                    if (dataCarrier.TargetZoneId != _zoneId)
                    {
                        return; // ID не совпадает, игнорируем
                    }
                }
                else
                {
                    // Если у объекта нет данных, но есть Carryable, можно проверить через каст, 
                    // если Carryable сам хранит ZoneKey (зависит от вашей реализации Carryable)
                    // Для надежности предположим, что Carryable тоже реализует проверку или имеет поле
                    if (carryable is Carryable specificCarryable)
                    {
                        // Если в Carryable есть публичное свойство ZoneKey (добавьте его в Carryable если нет)
                        // Пока используем рефлексию или просто пропускаем, если строгой проверки нет
                        // ЛУЧШЕ: Добавьте свойство ZoneKey в интерфейс ICarryable или используйте ICarryData
                    }
                }
            }

            // 2. Извлечение данных
            List<ScriptableObject> itemData = new List<ScriptableObject>();
            if (carryable is ICarryData dataProvider)
            {
                var data = dataProvider.GetData();
                if (data != null) itemData.AddRange(data);
            }

            // 3. Примагничивание и отключение физики
            SnapToTarget(carryable);

            _heldItemInZone = carryable;
            _heldRbCache = carryable.Rigidbody;

            if (_heldRbCache != null)
            {
                
                _heldRbCache.linearVelocity = Vector3.zero;
                _heldRbCache.isKinematic = true; // ОТКЛЮЧАЕМ ФИЗИКУ (предмет висит)
                _heldRbCache.angularVelocity = Vector3.zero;
            }

            _storedData.AddRange(itemData);
            Debug.Log($"[DropZone] '{_zoneId}' принял объект. Данные: {itemData.Count}. Физика отключена.");

            OnDataDelivered?.Invoke(_storedData);
        }

        private void OnTriggerExit(Collider other)
        {
            ICarryable carryable = other.GetComponentInParent<ICarryable>();

            // Проверяем, тот ли это предмет, который держим
            if (carryable == _heldItemInZone)
            {
                ReleaseItem();
            }
        }

        private void ReleaseItem()
        {
            if (_heldItemInZone == null) return;

            Debug.Log($"[DropZone] Предмет покинул зону '{_zoneId}'. Возвращаем физику.");

            if (_heldRbCache != null)
            {
                _heldRbCache.isKinematic = false; // ВКЛЮЧАЕМ ФИЗИКУ (предмет падает)
            }

            _heldItemInZone = null;
            _heldRbCache = null;
            // Данные можно очистить или оставить, зависит от логики игры
            // _storedData.Clear(); 
        }

        private void SnapToTarget(ICarryable item)
        {
            if (item == null || item.Transform == null) return;

            item.Transform.position = _dropTargetPoint.position;
            item.Transform.rotation = _dropTargetPoint.rotation;
        }

        public List<ScriptableObject> GetStoredData() => new List<ScriptableObject>(_storedData);

        public void ClearData()
        {
            ReleaseItem();
            _storedData.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            if (_dropTargetPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_dropTargetPoint.position, 0.2f);

                Gizmos.color = new Color(0, 1, 1, 0.3f);
                if (_zoneCollider != null)
                    Gizmos.DrawWireCube(_zoneCollider.bounds.center, _zoneCollider.bounds.size);
            }
        }

        // Добавь этот метод в класс DropZone
        public void ReleaseItemIfHeld(ICarryable item)
        {
            if (_heldItemInZone == item)
            {
                Debug.Log($"[DropZone] Освобождаю предмет {item.Transform.name} из зоны {_zoneId}");
                _heldItemInZone = null;
                // Мы НЕ телепортируем предмет обратно, он останется там, где его взяли мышкой.
                // Физика включится в MouseInteractor при отпускании.
            }
        }
    }


    public interface ICarryData
    {
        string TargetZoneId { get; }
        List<ScriptableObject> GetData();
    }
}