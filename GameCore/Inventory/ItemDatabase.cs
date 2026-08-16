using System.Collections.Generic;
using UnityEngine;

namespace Core.Inventory
{
    /// <summary>
    /// Реестр всех предметов игры. Нужен, чтобы по id (из сейва) находить ItemDefinition-ассет,
    /// так как сами SO в JSON не сериализуются — в снапшоте лежит только id + количество.
    ///
    /// Лежит в Resources/Core/ItemDatabase. Заполняется вручную (перетаскиваешь все ItemDefinition).
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Core/Inventory/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemDefinition> items = new();

        private Dictionary<string, ItemDefinition> _byId;

        public ItemDefinition GetById(string id)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<string, ItemDefinition>();
                foreach (var item in items)
                {
                    if (item == null || string.IsNullOrEmpty(item.id)) continue;
                    if (!_byId.ContainsKey(item.id)) _byId[item.id] = item;
                    else Debug.LogWarning($"[ItemDatabase] дубль id '{item.id}'");
                }
            }
            _byId.TryGetValue(id, out var def);
            if (def == null) Debug.LogError($"[ItemDatabase] предмет с id '{id}' не найден");
            return def;
        }
    }
}
