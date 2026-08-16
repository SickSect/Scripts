using UnityEngine;

namespace Core.Inventory
{
    /// <summary>
    /// Базовый эффект предмета (что происходит при использовании). ScriptableObject —
    /// наследники задают конкретное поведение: лечение, ремонт, зарядка оружия и т.д.
    ///
    /// Новый тип действия = новый наследник ItemEffect. Инвентарь и предмет не меняются.
    ///
    /// Возвращаемое значение Apply — сработал ли эффект (успех). Если false (например,
    /// здоровье уже полное) — применение НЕ тратится, charges не списывается.
    /// </summary>
    public abstract class ItemEffect : ScriptableObject
    {
        /// <summary>Применить эффект. Возвращает true, если эффект реально сработал.</summary>
        public abstract bool Apply(ItemUseContext context);
    }
}
