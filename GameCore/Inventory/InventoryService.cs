using System.Collections.Generic;
using Core.DI;
using R3;
using UnityEngine;

namespace Core.Inventory
{
    /// <summary>
    /// Ядро инвентаря: хранение по ячейкам, добавление со стеком, использование
    /// (charges + эффект), выброс. Публикует Changed для UI.
    ///
    /// Ячейки — фиксированный массив размера capacity. Пустая ячейка = null.
    /// </summary>
    public class InventoryService
    {
        private readonly ItemDatabase _database;
        private readonly DIContainer _root;
        private ItemStack[] _slots;

        /// <summary>Дёргается при любом изменении инвентаря (для перерисовки UI).</summary>
        public Subject<Unit> Changed { get; } = new();

        public int Capacity => _slots.Length;
        public IReadOnlyList<ItemStack> Slots => _slots;

        public InventoryService(ItemDatabase database, DIContainer root)
        {
            _database = database;
            _root = root;
            _slots = new ItemStack[20];
        }

        // ---------- ЗАГРУЗКА / СОХРАНЕНИЕ ----------

        public void LoadFrom(InventoryData data)
        {
            _slots = new ItemStack[data.capacity];
            for (int i = 0; i < data.slots.Count && i < _slots.Length; i++)
            {
                var s = data.slots[i];
                if (string.IsNullOrEmpty(s.itemId)) continue;
                var def = _database.GetById(s.itemId);
                if (def == null) continue;
                _slots[i] = new ItemStack(def, s.count, s.charges);
            }
            Changed.OnNext(Unit.Default);
        }

        public void SaveInto(InventoryData data)
        {
            data.capacity = _slots.Length;
            data.slots = new List<InventoryData.SlotData>(_slots.Length);
            foreach (var slot in _slots)
            {
                if (slot == null || slot.IsEmpty)
                {
                    data.slots.Add(new InventoryData.SlotData { itemId = null });
                    continue;
                }
                data.slots.Add(new InventoryData.SlotData
                {
                    itemId = slot.Item.id,
                    count = slot.Count,
                    charges = slot.Charges
                });
            }
        }

        // ---------- ДОБАВЛЕНИЕ ----------

        /// <summary>
        /// Добавить count штук предмета. Возвращает сколько НЕ поместилось (0 = всё влезло).
        /// Сначала докладывает в существующие стеки, потом в пустые ячейки.
        /// </summary>
        public int Add(ItemDefinition item, int count = 1)
        {
            if (item == null || count <= 0) return count;

            // 1) доложить в существующие незаполненные стеки того же предмета
            for (int i = 0; i < _slots.Length && count > 0; i++)
            {
                var slot = _slots[i];
                if (slot == null || slot.Item != item || slot.IsFull) continue;
                int add = Mathf.Min(slot.FreeSpace, count);
                slot.Count += add;
                count -= add;
            }

            // 2) разложить остаток по пустым ячейкам
            for (int i = 0; i < _slots.Length && count > 0; i++)
            {
                if (_slots[i] != null && !_slots[i].IsEmpty) continue;
                int add = Mathf.Min(item.maxStack, count);
                _slots[i] = new ItemStack(item, add, item.maxCharges);
                count -= add;
            }

            Changed.OnNext(Unit.Default);
            return count; // остаток, что не влез
        }

        public bool Has(ItemDefinition item, int count = 1) => CountOf(item) >= count;

        /// <summary>Влезет ли count штук предмета (не меняя инвентарь)? Для проверки перед выдачей.</summary>
        public bool CanFit(ItemDefinition item, int count = 1)
        {
            if (item == null || count <= 0) return true;
            int remaining = count;

            // Место в существующих стеках.
            foreach (var slot in _slots)
            {
                if (slot == null || slot.Item != item || slot.IsFull) continue;
                remaining -= slot.FreeSpace;
                if (remaining <= 0) return true;
            }
            // Пустые ячейки.
            foreach (var slot in _slots)
            {
                if (slot != null && !slot.IsEmpty) continue;
                remaining -= item.maxStack;
                if (remaining <= 0) return true;
            }
            return remaining <= 0;
        }

        public int CountOf(ItemDefinition item)
        {
            int total = 0;
            foreach (var slot in _slots)
                if (slot != null && slot.Item == item) total += slot.Count;
            return total;
        }

        // ---------- ИСПОЛЬЗОВАНИЕ ----------

        /// <summary>
        /// Использовать предмет в ячейке index. Тратит charges верхнего экземпляра;
        /// когда charges кончились и предмет расходуемый — уменьшает стак.
        /// </summary>
        public bool Use(int index, GameObject player)
        {
            if (index < 0 || index >= _slots.Length) return false;
            var slot = _slots[index];
            if (slot == null || slot.IsEmpty) return false;
            if (!slot.Item.CanUse) return false;

            var ctx = new ItemUseContext(player, _root, this, slot.Item);
            bool ok = slot.Item.useEffect.Apply(ctx);
            if (!ok) return false; // эффект не сработал — не тратим

            // Тратим одно применение верхнего экземпляра.
            slot.Charges--;
            if (slot.Charges <= 0)
            {
                if (slot.Item.consumable)
                {
                    slot.Count--;                       // уничтожаем экземпляр
                    slot.Charges = slot.Item.maxCharges; // следующий экземпляр — полный
                    if (slot.Count <= 0) _slots[index] = null;
                }
                else
                {
                    slot.Charges = slot.Item.maxCharges; // не расходуемый — восстанавливаем
                }
            }

            Changed.OnNext(Unit.Default);
            return true;
        }

        // ---------- ВЫБРОС ----------

        public bool Drop(int index, int count = 1)
        {
            if (index < 0 || index >= _slots.Length) return false;
            var slot = _slots[index];
            if (slot == null || slot.IsEmpty || !slot.Item.droppable) return false;

            slot.Count -= Mathf.Min(count, slot.Count);
            if (slot.Count <= 0) _slots[index] = null;

            Changed.OnNext(Unit.Default);
            return true;
        }

        /// <summary>Убрать count штук предмета из инвентаря (для эффектов: списать патроны и т.п.).</summary>
        public bool Remove(ItemDefinition item, int count = 1)
        {
            if (!Has(item, count)) return false;
            for (int i = 0; i < _slots.Length && count > 0; i++)
            {
                var slot = _slots[i];
                if (slot == null || slot.Item != item) continue;
                int take = Mathf.Min(slot.Count, count);
                slot.Count -= take;
                count -= take;
                if (slot.Count <= 0) _slots[i] = null;
            }
            Changed.OnNext(Unit.Default);
            return true;
        }
    }
}
