using Core.Common;
using UnityEngine;

namespace Core.Story.Phases
{
    /// <summary>
    /// Спавн события в якорь: просто инстанцирует префаб события (StoryEventZone,
    /// StoryEventInteractable или любой объект) в точку якоря.
    /// </summary>
    [CreateAssetMenu(fileName = "EventPhaseSpawn", menuName = "Core/Story/Phase Spawn/Event")]
    public class EventPhaseSpawn : PhaseSpawn
    {
        [SerializeField] private GameObject _eventPrefab;
        [Tooltip("Триггер «уже получено»: если он есть, событие не спавнится повторно. " +
                 "Напр. для кофе — триггер has_coffee. Пусто = спавнить всегда.")]
        [SerializeField] private Core.Flags.TriggerDefinition _consumedTrigger;

        public override GameObject Spawn(PhaseSpawnContext context)
        {
            var anchor = context.GetAnchor(anchorId);
            if (anchor == null)
            {
                CoreLog.Debug($"[EventPhaseSpawn] якорь '{anchorId}' не найден");
                return null;
            }
            if (_eventPrefab == null) return null;

            var go = Object.Instantiate(_eventPrefab, anchor.position, anchor.rotation);
            CoreLog.Debug($"[EventPhaseSpawn] {_eventPrefab.name} → якорь '{anchorId}'");
            return go;
        }

        /// <summary>Событие получено, если выдан _consumedTrigger (напр. взял кофе).</summary>
        public override bool IsConsumed(PhaseSpawnContext context)
        {
            if (_consumedTrigger == null) return false;
            if (!context.Root.TryResolve<Core.Flags.FlagService>(out var flags)) return false;
            return flags.Has(_consumedTrigger);
        }
    }
}
