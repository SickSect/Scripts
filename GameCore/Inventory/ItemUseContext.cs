using Core.DI;
using UnityEngine;

namespace Core.Inventory
{
    /// <summary>
    /// Контекст использования предмета: даёт эффекту доступ к игроку, контейнерам
    /// и к самому инвентарю (чтобы, например, зарядить оружие патронами или удалить предмет).
    /// </summary>
    public class ItemUseContext
    {
        public readonly GameObject Player;
        public readonly DIContainer Root;
        public readonly InventoryService Inventory;
        public readonly ItemDefinition Item;

        public ItemUseContext(GameObject player, DIContainer root, InventoryService inventory, ItemDefinition item)
        {
            Player = player;
            Root = root;
            Inventory = inventory;
            Item = item;
        }
    }
}
