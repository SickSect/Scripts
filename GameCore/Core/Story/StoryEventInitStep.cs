using Core.Init;
using Core.Story.Events;
using UnityEngine;

namespace Core.Story
{
    /// <summary>
    /// Биндит зональные события сцены (StoryEventZone), давая им root и раннер корутин.
    /// Интерактивные события (StoryEventInteractable) не нуждаются — получают root
    /// через InteractionContext при взаимодействии.
    ///
    /// Order 8 — после флагов/статов, объекты сцены уже есть.
    /// </summary>
    public class StoryEventInitStep : IInitStep
    {
        public int Order => 8;

        public void Execute(InitContext ctx)
        {
            // Раннер корутин — Coroutines-объект из root (DontDestroyOnLoad).
            MonoBehaviour runner = null;
            if (ctx.Root.TryResolve<Core.DI.Coroutines>(out var cor))
                runner = cor;

            foreach (var zone in Object.FindObjectsByType<StoryEventZone>(FindObjectsInactive.Include))
                zone.Bind(ctx.Root, runner);
        }
    }
}
