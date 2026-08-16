namespace Core.Inventory
{
    /// <summary>
    /// Рантайм-ячейка инвентаря: ссылка на предмет + сколько штук в стаке + текущие
    /// применения ВЕРХНЕГО экземпляра (для износа, как вода на 3 глотка).
    ///
    /// Пример вода (maxStack 1, maxCharges 3): count=1, charges тратятся при использовании,
    /// на 0 — экземпляр уничтожается.
    /// Пример патроны (maxStack 10, maxCharges 0): count=10, charges не используется.
    /// </summary>
    public class ItemStack
    {
        public ItemDefinition Item;
        public int Count;    // штук в стаке
        public int Charges;  // применения текущего (верхнего) экземпляра

        public ItemStack(ItemDefinition item, int count, int charges)
        {
            Item = item;
            Count = count;
            Charges = charges;
        }

        public bool IsEmpty => Item == null || Count <= 0;
        public bool IsFull => Item != null && Count >= Item.maxStack;
        public int FreeSpace => Item == null ? 0 : Item.maxStack - Count;
    }
}
