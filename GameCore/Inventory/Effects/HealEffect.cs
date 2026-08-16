using Core.Common;
using Core.Stats;
using UnityEngine;

namespace Core.Inventory.Effects
{
    /// <summary>
    /// Лечение: восстанавливает указанный стат (по умолчанию — здоровье).
    /// Оставлен для совместимости; для новых предметов используй ModifyStatEffect.
    /// </summary>
    [CreateAssetMenu(fileName = "HealEffect", menuName = "Core/Inventory/Effects/Heal")]
    public class HealEffect : ItemEffect
    {
        [SerializeField] private StatDefinition _stat;
        [SerializeField] private int _amount = 25;

        public override bool Apply(ItemUseContext context)
        {
            if (!context.Root.TryResolve<StatsService>(out var stats)) return false;
            var stat = stats.Get(_stat);
            if (stat == null) return false;
            if (stat.IsFull) return false;

            stat.Modify(_amount);
            CoreLog.Debug($"[HealEffect] +{_amount} {_stat.id} → {stat.Value.Value}");
            return true;
        }
    }
}
