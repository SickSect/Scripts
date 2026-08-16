using Core.Common;
using Core.Flags;
using Core.Interaction;
using UnityEngine;

namespace Core.Story
{
    /// <summary>
    /// Выдаёт сюжетный триггер при взаимодействии (рубильник, кнопка, записка, «взять кофе»).
    /// Проверка наличия защищает от повторной выдачи (FlagService.Set).
    ///
    /// Опционально: условие для срабатывания (_condition) и удаление объекта после выдачи
    /// (_destroyAfter — «взял кофе, кружки больше нет»).
    /// Нужен коллайдер на слое LookTarget.
    /// </summary>
    public class TriggerGiver : MonoBehaviour, IInteractable
    {
        [SerializeField] private TriggerDefinition _trigger;
        [SerializeField] private string _prompt = "Активировать";
        [SerializeField] private TriggerCondition _condition; // опционально
        [SerializeField] private bool _onceOnly = true;       // после выдачи больше не интерактивен
        [Tooltip("Удалить объект со сцены после выдачи триггера.")]
        [SerializeField] private bool _destroyAfter = false;

        public string Prompt => _prompt;

        public void Interact(InteractionContext context)
        {
            if (_trigger == null)
            {
                CoreLog.Debug("[TriggerGiver] не назначен триггер (_trigger пустой)");
                return;
            }
            if (!context.Root.TryResolve<FlagService>(out var flags)) return;

            if (_onceOnly && flags.Has(_trigger))
            {
                CoreLog.Debug($"[TriggerGiver] {_trigger.id} уже выдан");
                return;
            }

            if (_condition != null)
            {
                context.Root.TryResolve<Inventory.InventoryService>(out var inv);
                var ctx = new ConditionContext(flags, inv);
                if (!_condition.Evaluate(ctx))
                {
                    CoreLog.Debug($"[TriggerGiver] условие не выполнено для {_trigger.id}");
                    return;
                }
            }

            flags.Set(_trigger);
            CoreLog.Debug($"[TriggerGiver] выдан триггер {_trigger.id}");

            if (_destroyAfter) Destroy(gameObject);
        }
    }
}