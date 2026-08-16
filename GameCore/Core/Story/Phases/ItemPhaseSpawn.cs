using Core.Common;
using Core.Inventory;
using UnityEngine;

namespace Core.Story.Phases
{
    /// <summary>
    /// Спавн предмета в якорь: префаб WorldItemPickup + данные (ItemDefinition).
    /// Один префаб-форма, разные данные — то, что нужно (в фазе 2A аптечка, в 3B ключ).
    /// </summary>
    [CreateAssetMenu(fileName = "ItemPhaseSpawn", menuName = "Core/Story/Phase Spawn/Item")]
    public class ItemPhaseSpawn : PhaseSpawn
    {
        [SerializeField] private WorldItemPickup _pickupPrefab;
        [SerializeField] private ItemDefinition _item;
        [SerializeField] private int _count = 1;
        [Tooltip("Уникальный id для персистентности (не респавнить собранное). " +
                 "Пусто = возьмётся из префаба.")]
        [SerializeField] private string _uniqueId;

        public override GameObject Spawn(PhaseSpawnContext context)
        {
            var anchor = context.GetAnchor(anchorId);
            if (anchor == null)
            {
                CoreLog.Debug($"[ItemPhaseSpawn] якорь '{anchorId}' не найден на сцене");
                return null;
            }
            if (_pickupPrefab == null || _item == null)
            {
                CoreLog.Debug("[ItemPhaseSpawn] не задан префаб или предмет");
                return null;
            }

            var go = Object.Instantiate(_pickupPrefab, anchor.position, anchor.rotation);
            go.Configure(_item, _count, _uniqueId);
            CoreLog.Debug($"[ItemPhaseSpawn] {_item.displayName} → якорь '{anchorId}'");
            return go.gameObject;
        }

        /// <summary>Предмет получен, если стоит его сценовая метка (собран через WorldObjectsInitStep).</summary>
        public override bool IsConsumed(PhaseSpawnContext context)
        {
            if (string.IsNullOrEmpty(_uniqueId)) return false;
            if (!context.Root.TryResolve<Core.Flags.FlagService>(out var flags)) return false;
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return flags.HasScene(scene, _uniqueId);
        }
    }
}
