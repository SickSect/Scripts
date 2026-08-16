using System.Collections.Generic;
using Core.Interaction;
using UnityEngine;

namespace Core.Story.Events
{
    /// <summary>
    /// Интерактивное событие: игрок нажал Interact, глядя на объект → срабатывают действия.
    /// Для событий, требующих активного действия игрока (осмотреть, нажать, поговорить).
    ///
    /// Использует систему взаимодействия (LookTarget + PlayerInteractor), не нужен bind:
    /// root приходит через InteractionContext.
    /// </summary>
    public class StoryEventInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _eventId;
        [SerializeField] private string _prompt = "Осмотреть";
        [SerializeField] private TriggerCondition _condition;
        [SerializeField] private List<StoryAction> _actions = new();
        [SerializeField] private bool _onceOnly = true;
        [SerializeField] private bool _persistOnce = true;

        private StoryEventCore _core;

        public string Prompt => _prompt;

        private void EnsureCore()
        {
            _core ??= new StoryEventCore(_eventId, _condition, _actions, _onceOnly, _persistOnce);
        }

        public void Interact(InteractionContext context)
        {
            EnsureCore();
            // runner — сам объект (MonoBehaviour), для длительных действий (скример).
            _core.TryFire(context.Root, context.Player, transform, this);
        }
    }
}
