using Core.Common;
using Core.Inventory;
using Core.Stats;
using UnityEngine;

namespace Core.Stats.Effects
{
    /// <summary>
    /// Эффект предмета: изменить стат на величину (лечение, восстановление выносливости,
    /// урон по рассудку и т.д.). Один эффект-класс на все статы — выбираешь стат и величину.
    ///
    /// Пример вода: stat=Stamina, amount=+30. Аптечка: stat=Health, amount=+25.
    /// Возвращает false, если стат уже полон (при положительном amount) — применение не тратится.
    /// </summary>
    [CreateAssetMenu(fileName = "ModifyStatEffect", menuName = "Core/Inventory/Effects/Modify Stat")]
    public class ModifyStatEffect : ItemEffect
    {
        [SerializeField] private StatDefinition _stat;
        [SerializeField] private float _amount = 25f;

        public override bool Apply(ItemUseContext context)
        {
            if (!context.Root.TryResolve<StatsService>(out var stats)) return false;

            var stat = stats.Get(_stat);
            if (stat == null) return false;

            // Не тратим, если восстанавливаем уже полный стат.
            if (_amount > 0 && stat.IsFull) return false;
            // Не тратим, если уроним уже нулевой.
            if (_amount < 0 && stat.IsZero) return false;

            stat.Modify(_amount);
            CoreLog.Debug($"[ModifyStatEffect] {_stat.id} {(_amount >= 0 ? "+" : "")}{_amount} → {stat.Value.Value}");
            return true;
        }
    }
}
