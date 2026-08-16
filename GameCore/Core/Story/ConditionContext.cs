using Core.Flags;
using Core.Inventory;

namespace Core.Story
{
    /// <summary>
    /// Контекст вычисления условия: доступ к меткам и инвентарю.
    /// </summary>
    public class ConditionContext
    {
        public readonly FlagService Flags;
        public readonly InventoryService Inventory;

        public ConditionContext(FlagService flags, InventoryService inventory)
        {
            Flags = flags;
            Inventory = inventory;
        }
    }
}
