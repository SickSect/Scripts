using Core.Init;
using Core.State;

namespace Core.Inventory
{
    /// <summary>
    /// Загружает инвентарь из снапшота при входе на игровую сцену и регистрирует
    /// контрибьютора сохранения. Сервис инвентаря создаётся один раз в root.
    ///
    /// Order 30 — после игрока/камеры (не критично, зависимостей от них нет).
    /// </summary>
    public class InventoryInitStep : IInitStep
    {
        public int Order => 30;

        public void Execute(InitContext ctx)
        {
            if (!ctx.Root.TryResolve<InventoryService>(out var inventory))
                return; // БД не настроена — инвентарь отключён

            ctx.State.inventory ??= new InventoryData();
            inventory.LoadFrom(ctx.State.inventory);
            Core.Player.ExamineServices.Inventory = inventory;

            var stateService = ctx.Root.Resolve<GameStateService>();
            stateService.RegisterContributor(new InventoryStateContributor(inventory));
        }
    }
}
