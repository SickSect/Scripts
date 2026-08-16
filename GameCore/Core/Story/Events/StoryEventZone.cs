using System.Collections.Generic;
using Core.DI;
using Core.Player;
using UnityEngine;

namespace Core.Story.Events
{
    /// <summary>
    /// Зональное событие: игрок вошёл в область (триггер-коллайдер) → срабатывают действия
    /// (если условие выполнено). «Попал в область — засчитано».
    ///
    /// Нужен Collider с Is Trigger = ✓. Биндится через StoryEventInitStep (получает root/runner).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class StoryEventZone : MonoBehaviour
    {
        [SerializeField] private string _eventId;
        [SerializeField] private TriggerCondition _condition;
        [SerializeField] private List<StoryAction> _actions = new();
        [SerializeField] private bool _onceOnly = true;
        [Tooltip("Помнить срабатывание между заходами на сцену (сохраняется).")]
        [SerializeField] private bool _persistOnce = true;

        private StoryEventCore _core;
        private DIContainer _root;
        private MonoBehaviour _runner;

        public void Bind(DIContainer root, MonoBehaviour runner)
        {
            _root = root;
            _runner = runner;
            _core = new StoryEventCore(_eventId, _condition, _actions, _onceOnly, _persistOnce);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_core == null) return;
            if (other.GetComponentInParent<PlayerMovement>() == null) return; // только игрок

            _core.TryFire(_root, other.gameObject, transform, _runner);
        }
    }
}
