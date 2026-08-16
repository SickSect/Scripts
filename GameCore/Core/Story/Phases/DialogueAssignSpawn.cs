using Core.Common;
using UnityEngine;

namespace Core.Story.Phases
{
    /// <summary>
    /// Назначает диалог объекту, который УЖЕ стоит на сцене (телефон, стационарный NPC).
    /// Ничего не спавнит — находит DialogueTrigger с нужным slotId и подменяет ему диалог.
    ///
    /// Так один и тот же телефон в разных фазах ведёт разные разговоры.
    /// Если нужно именно создать объект — используй DialoguePhaseSpawn.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueAssignSpawn", menuName = "Core/Story/Phase Spawn/Dialogue (назначить существующему)")]
    public class DialogueAssignSpawn : PhaseSpawn
    {
        [Tooltip("Id слота — совпадает с DialogueTrigger._slotId на объекте сцены.")]
        [SerializeField] private string _slotId;

        [SerializeField] private Core.Story.Dialogue.Dialogue _dialogue;

        [Tooltip("Подсказка взаимодействия. Пусто = оставить как на объекте.")]
        [SerializeField] private string _prompt;

        [Tooltip("Если этот триггер выдан — не назначать (оставить прежний диалог). Пусто = назначать всегда.")]
        [SerializeField] private Core.Flags.TriggerDefinition _consumedTrigger;

        public override GameObject Spawn(PhaseSpawnContext context)
        {
            if (string.IsNullOrEmpty(_slotId) || _dialogue == null)
            {
                CoreLog.Debug("[DialogueAssignSpawn] не задан слот или диалог");
                return null;
            }

            // Ищем объект сцены с нужным слотом.
            var targets = Object.FindObjectsByType<Core.Story.Dialogue.DialogueTrigger>(
                FindObjectsInactive.Include);

            foreach (var t in targets)
            {
                if (t == null || t.SlotId != _slotId) continue;
                t.Configure(_dialogue, _prompt);
                CoreLog.Debug($"[DialogueAssignSpawn] слот '{_slotId}' ← {_dialogue.name}");
                return t.gameObject;
            }

            CoreLog.Debug($"[DialogueAssignSpawn] слот '{_slotId}' не найден на сцене");
            return null;
        }

        public override bool IsConsumed(PhaseSpawnContext context)
        {
            if (_consumedTrigger == null) return false;
            if (!context.Root.TryResolve<Core.Flags.FlagService>(out var flags)) return false;
            return flags.Has(_consumedTrigger);
        }
    }
}
