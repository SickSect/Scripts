using Core.Common;
using UnityEngine;

namespace Core.Story.Phases
{
    /// <summary>
    /// Спавн диалога в якорь: префаб-носитель (телефон, NPC) + данные (Dialogue).
    /// Один префаб-форма, разные разговоры в разных фазах.
    ///
    /// IsConsumed: если задан _consumedTrigger (обычно completionTrigger диалога) и он выдан —
    /// объект не спавнится повторно. Пусто = спавнить всегда (например, телефон,
    /// с которым можно переговаривать).
    /// </summary>
    [CreateAssetMenu(fileName = "DialoguePhaseSpawn", menuName = "Core/Story/Phase Spawn/Dialogue")]
    public class DialoguePhaseSpawn : PhaseSpawn
    {
        [SerializeField] private Core.Story.Dialogue.DialogueTrigger _carrierPrefab;
        [SerializeField] private Core.Story.Dialogue.Dialogue _dialogue;
        [Tooltip("Подсказка взаимодействия. Пусто = из префаба.")]
        [SerializeField] private string _prompt;

        [Tooltip("Если этот триггер выдан — не спавнить повторно. Пусто = спавнить всегда.")]
        [SerializeField] private Core.Flags.TriggerDefinition _consumedTrigger;

        public override GameObject Spawn(PhaseSpawnContext context)
        {
            var anchor = context.GetAnchor(anchorId);
            if (anchor == null)
            {
                CoreLog.Debug($"[DialoguePhaseSpawn] якорь '{anchorId}' не найден");
                return null;
            }
            if (_carrierPrefab == null || _dialogue == null)
            {
                CoreLog.Debug("[DialoguePhaseSpawn] не задан префаб или диалог");
                return null;
            }

            var go = Object.Instantiate(_carrierPrefab, anchor.position, anchor.rotation);
            go.Configure(_dialogue, _prompt);
            CoreLog.Debug($"[DialoguePhaseSpawn] {_dialogue.name} → якорь '{anchorId}'");
            return go.gameObject;
        }

        public override bool IsConsumed(PhaseSpawnContext context)
        {
            if (_consumedTrigger == null) return false;
            if (!context.Root.TryResolve<Core.Flags.FlagService>(out var flags)) return false;
            return flags.Has(_consumedTrigger);
        }
    }
}