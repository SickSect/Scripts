using Core.Flags;
using Core.Init;
using Core.Interaction.Interactables;
using Core.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Story
{
    /// <summary>
    /// При заходе на сцену:
    ///  - уничтожает уже собранные WorldItemPickup (по сценовой метке);
    ///  - деактивирует условные объекты (ConditionalObject), чьё условие не выполнено;
    ///  - (Убрано) прокидывание FlagService в DropZone, так как теперь зоны работают через события UnityEvent.
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

            string sceneName = SceneManager.GetActiveScene().name;
            var condCtx = new ConditionContext(flags, inventory);

            // 1) Собранные предметы — убрать.
            foreach (var pickup in Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Include))
            {
                if (string.IsNullOrEmpty(pickup.UniqueId)) continue;

                if (flags.HasScene(sceneName, pickup.UniqueId))
                    Object.Destroy(pickup.gameObject);
            }

            // 2) Условные объекты — показать/скрыть по условию.
            foreach (var cond in Object.FindObjectsByType<ConditionalObject>(FindObjectsInactive.Include))
            {
                cond.gameObject.SetActive(cond.Condition == null || cond.Condition.Evaluate(condCtx));
            }

            // 3) Рабочие зоны (DropZone).
            // РАНЬШЕ: zone.BindFlags(flags);
            // ТЕПЕРЬ: Логика установки флага вынесена в инспектор Unity через UnityEvent.
            // Как настроить в редакторе:
            // 1. Выберите объект с компонентом DropZone.
            // 2. Найдите событие "On Item Placed" (или On Completion).
            // 3. Нажмите "+", перетащите любой объект со скриптом, умеющим ставить флаги (например, StoryFlagSetter).
            // 4. Выберите метод установки флага.
            //
            // Если вам критически нужно программное управление, создайте отдельный компонент-слушатель,
            // который подписывается на событие зоны в Awake и имеет доступ к FlagService.

            // Мы просто убеждаемся, что зоны активны и готовы к работе (физика работает автоматически).
            var zones = Object.FindObjectsByType<DropZone>(FindObjectsInactive.Include);
            if (zones.Length > 0)
            {
                Debug.Log($"[WorldObjectsInitStep] Найдено {zones.Length} зон доставки. Настройте UnityEvent для записи флагов в инспекторе.");
            }
        }
    }
}