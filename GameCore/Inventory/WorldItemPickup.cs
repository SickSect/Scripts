using Core.Common;
using Core.Flags;
using Core.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Inventory
{
    /// <summary>
    /// Предмет, лежащий в мире. Навёл прицел + Interact → предмет уходит в инвентарь.
    ///
    /// Не респавнится: при подборе ставится сценовая метка (scene/uniqueId), а
    /// WorldObjectsInitStep при заходе на сцену уничтожает уже собранные.
    /// Поэтому _uniqueId должен быть уникален в пределах сцены.
    ///
    /// Нужен коллайдер на слое, который ловит LookTarget (как двери).
    /// </summary>
    public class WorldItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition _item;
        [SerializeField] private int _count = 1;
        [SerializeField] private string _prompt = "Взять";

        [Header("Персистентность")]
        [Tooltip("Уникальный id в пределах сцены (чтобы не респавнить собранное).")]
        [SerializeField] private string _uniqueId;

        public string Prompt => _prompt;
        public string UniqueId => _uniqueId;

        /// <summary>Задать содержимое при спавне из фазы (префаб один, данные разные).</summary>
        public void Configure(ItemDefinition item, int count, string uniqueId)
        {
            _item = item;
            _count = count;
            if (!string.IsNullOrEmpty(uniqueId)) _uniqueId = uniqueId;
        }

        public void Interact(InteractionContext context)
        {
            if (!context.Root.TryResolve<InventoryService>(out var inventory))
            {
                CoreLog.Debug("[Pickup] InventoryService недоступен (нет ItemDatabase?)");
                return;
            }

            int leftover = inventory.Add(_item, _count);

            if (leftover <= 0)
            {
                CoreLog.Debug($"[Pickup] взял {_item.displayName} x{_count}");
                MarkCollected(context);
                Destroy(gameObject);
            }
            else if (leftover < _count)
            {
                CoreLog.Debug($"[Pickup] взял часть {_item.displayName}, осталось {leftover}");
                _count = leftover; // остаток лежит дальше — метку не ставим
            }
            else
            {
                CoreLog.Debug($"[Pickup] инвентарь полон, {_item.displayName} не взят");
            }
        }

        private void MarkCollected(InteractionContext context)
        {
            if (string.IsNullOrEmpty(_uniqueId)) return; // без id не персистим
            if (context.Root.TryResolve<FlagService>(out var flags))
                flags.SetScene(SceneManager.GetActiveScene().name, _uniqueId);
        }
    }
}
