using Core.Common;
using Core.Flags;
using UnityEngine;

namespace Core.Story.Actions
{
    /// <summary>
    /// Действие: выдать сюжетный триггер. Простейшее событие-флаг («вошёл в зону — засчитано»).
    /// </summary>
    [CreateAssetMenu(fileName = "GiveTriggerAction", menuName = "Core/Story/Actions/Give Trigger")]
    public class GiveTriggerAction : StoryAction
    {
        [SerializeField] private TriggerDefinition _trigger;

        public override void Execute(StoryActionContext context)
        {
            if (context.Root.TryResolve<FlagService>(out var flags))
            {
                flags.Set(_trigger);
                CoreLog.Debug($"[GiveTriggerAction] выдан триггер {_trigger.id}");
            }
        }
    }
}
