using Core.Common;
using Core.Flags;
using Core.Inventory;
using UnityEngine;

namespace Core.Story
{
    /// <summary>
    /// Эффект предмета: выдать сюжетный триггер при использовании.
    /// Так предмет (напр. записка, карта-ключ) влияет на сюжет, НЕ зная про триггеры напрямую —
    /// связь через эффект. Ровно то разделение, что закладывалось.
    /// </summary>
    [CreateAssetMenu(fileName = "GiveTriggerEffect", menuName = "Core/Inventory/Effects/Give Trigger")]
    public class GiveTriggerEffect : ItemEffect
    {
        [SerializeField] private TriggerDefinition _trigger;

        public override bool Apply(ItemUseContext context)
        {
            if (!context.Root.TryResolve<FlagService>(out var flags)) return false;
            flags.Set(_trigger);
            CoreLog.Debug($"[GiveTriggerEffect] выдан триггер {_trigger.id}");
            return true;
        }
    }
}
