using UnityEngine;

namespace Core.Inventory
{
    /// <summary>
    /// Определение предмета (ScriptableObject). Один ассет на тип предмета: вода, патрон, ключ.
    /// Экземпляры в инвентаре ссылаются на него по id.
    ///
    /// Две независимые оси количества:
    ///   - maxStack: сколько ОДИНАКОВЫХ предметов помещается в одну ячейку (патроны: 10);
    ///   - maxCharges: сколько раз можно использовать ОДИН экземпляр (вода: 3 глотка).
    /// </summary>
    [CreateAssetMenu(fileName = "Item", menuName = "Core/Inventory/Item")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Идентификация")]
        [Tooltip("Уникальный стабильный id для сохранений. Не менять после релиза.")]
        public string id;

        [Header("Отображение")]
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Стак (штук в ячейке)")]
        [Min(1)] public int maxStack = 1;

        [Header("Применения (износ одного экземпляра)")]
        [Tooltip("Сколько раз можно использовать один экземпляр. 0 = нельзя использовать (напр. ключ-условие).")]
        [Min(0)] public int maxCharges = 1;

        [Tooltip("Уничтожается ли, когда применения кончились.")]
        public bool consumable = true;

        [Header("Поведение")]
        [Tooltip("Эффект при использовании. Пусто = предмет нельзя использовать напрямую (ключ, патрон-как-ресурс).")]
        public ItemEffect useEffect;

        [Tooltip("Можно ли выбросить.")]
        public bool droppable = true;

        [Tooltip("Ключевой предмет (условие для взаимодействий). Обычно не используется/не выбрасывается.")]
        public bool isKeyItem = false;

        public bool CanUse => useEffect != null && maxCharges > 0;
    }
}
