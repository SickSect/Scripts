using System;
using System.Collections.Generic;

namespace Core.Inventory
{
    /// <summary>
    /// Сериализуемое состояние инвентаря для снапшота. Хранит только id + количество + charges
    /// по ячейкам (SO не сериализуются). Восстанавливается через ItemDatabase по id.
    /// </summary>
    [Serializable]
    public class InventoryData
    {
        public int capacity = 20; // число ячеек
        public List<SlotData> slots = new();

        [Serializable]
        public struct SlotData
        {
            public string itemId; // пусто = ячейка свободна
            public int count;
            public int charges;
        }

        public InventoryData Clone()
        {
            var copy = new InventoryData { capacity = capacity, slots = new List<SlotData>(slots.Count) };
            foreach (var s in slots) copy.slots.Add(s); // struct копируется по значению
            return copy;
        }
    }
}
