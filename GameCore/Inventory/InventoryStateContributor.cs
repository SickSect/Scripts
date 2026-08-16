using Core.State;

namespace Core.Inventory
{
    /// <summary>
    /// Записывает состояние инвентаря в снапшот при сохранении.
    /// </summary>
    public class InventoryStateContributor : IStateContributor
    {
        private readonly InventoryService _inventory;

        public InventoryStateContributor(InventoryService inventory) => _inventory = inventory;

        public void CaptureInto(GameStateData state)
        {
            state.inventory ??= new InventoryData();
            _inventory.SaveInto(state.inventory);
        }
    }
}
