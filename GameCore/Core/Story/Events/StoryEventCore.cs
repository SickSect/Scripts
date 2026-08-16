using System.Collections.Generic;
using Core.Common;
using Core.DI;
using Core.Flags;
using Core.Inventory;
using UnityEngine;

namespace Core.Story
{
    /// <summary>
    /// Ядро сюжетного события: список действий + условие + защита от повтора.
    /// Не MonoBehaviour — переиспользуется зоной и интерактивным событием.
    ///
    /// Fire() проверяет условие, выполняет все действия, ставит метку «событие сработало»
    /// (по eventId), чтобы не повторялось. Метка сценовая (переживает перезаход сцены).
    /// </summary>
    public class StoryEventCore
    {
        private readonly string _eventId;
        private readonly TriggerCondition _condition;
        private readonly List<StoryAction> _actions;
        private readonly bool _onceOnly;
        private readonly bool _persistOnce; // помнить срабатывание между заходами на сцену

        public StoryEventCore(string eventId, TriggerCondition condition,
                              List<StoryAction> actions, bool onceOnly, bool persistOnce)
        {
            _eventId = eventId;
            _condition = condition;
            _actions = actions;
            _onceOnly = onceOnly;
            _persistOnce = persistOnce;
        }

        private bool _firedThisSession;

        public bool TryFire(DIContainer root, GameObject player, Transform origin, MonoBehaviour runner)
        {
            root.TryResolve<FlagService>(out var flags);
            root.TryResolve<InventoryService>(out var inventory);

            // Защита от повтора.
            if (_onceOnly)
            {
                if (_firedThisSession) return false;
                if (_persistOnce && flags != null && !string.IsNullOrEmpty(_eventId)
                    && flags.HasScene(SceneName(), _eventId))
                    return false;
            }

            // Условие.
            if (_condition != null)
            {
                var ctx = new ConditionContext(flags, inventory);
                if (!_condition.Evaluate(ctx))
                {
                    CoreLog.Debug($"[StoryEvent] {_eventId}: условие не выполнено");
                    return false;
                }
            }

            // Выполнение действий.
            var actionCtx = new StoryActionContext(root, player, origin, runner);
            foreach (var action in _actions)
                if (action != null) action.Execute(actionCtx);

            _firedThisSession = true;
            if (_persistOnce && flags != null && !string.IsNullOrEmpty(_eventId))
                flags.SetScene(SceneName(), _eventId);

            CoreLog.Debug($"[StoryEvent] {_eventId}: сработало ({_actions.Count} действий)");
            return true;
        }

        private static string SceneName() =>
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
}
