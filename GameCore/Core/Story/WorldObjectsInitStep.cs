using Core.Carry;
using Core.Flags;
using Core.Init;
using Core.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Story
{
    /// <summary>
    /// При заходе на сцену:
    ///  - уничтожает уже собранные WorldItemPickup (по сценовой метке);
    ///  - деактивирует условные объекты (ConditionalObject), чьё условие не выполнено;
    ///  - прокидывает FlagService в рабочие зоны (DropZone), чтобы они умели ставить метки.
    ///
    /// Order 6 — после FlagInitStep (метки загружены).
    /// </summary>
    public class WorldObjectsInitStep : IInitStep
    {
        public int Order => 6;

        public void Execute(InitContext ctx)
        {
            if (!ctx.Root.TryResolve<FlagService>(out var flags)) return;
            ctx.Root.TryResolve<InventoryService>(out var inventory);

            string scene = SceneManager.GetActiveScene().name;
            var condCtx = new ConditionContext(flags, inventory);

            // 1) Собранные предметы — убрать.
            foreach (var pickup in Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Include))
            {
                if (string.IsNullOrEmpty(pickup.UniqueId)) continue;

                if (flags.HasScene(scene, pickup.UniqueId))
                    Object.Destroy(pickup.gameObject);
            }

            // 2) Условные объекты — показать/скрыть по условию.
            foreach (var cond in Object.FindObjectsByType<ConditionalObject>(FindObjectsInactive.Include))
            {
                cond.gameObject.SetActive(cond.Condition == null || cond.Condition.Evaluate(condCtx));
            }

            // 3) Рабочие зоны — выдать доступ к меткам.
            // Своего контекста у DropZone нет: она ловит предметы триггером,
            // а не через Interact, поэтому FlagService прокидывается здесь.
            foreach (var zone in Object.FindObjectsByType<DropZone>(FindObjectsInactive.Include))
            {
                zone.BindFlags(flags);
            }
        }
    }
}
