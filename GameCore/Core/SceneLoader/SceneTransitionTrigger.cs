using Core.Common;
using Core.Interaction;
using Core.Signals;
using Core.State;
using R3;
using UnityEngine;

namespace Core.SceneLoader
{
    /// <summary>
    /// "Дверь": объект на сцене, при взаимодействии переводит игрока на другую сцену
    /// и ставит его на точку спавна с targetSpawnId.
    ///
    /// Нужен коллайдер (чтобы рейкаст LookTarget его ловил) и слой из маски LookTarget.
    /// </summary>
    public class SceneTransitionTrigger : MonoBehaviour, IInteractable
    {
        [Header("Куда ведёт")]
        [SerializeField] private string _targetScene;
        [SerializeField] private int _targetSpawnId;

        [Header("Подсказка")]
        [SerializeField] private string _prompt = "Войти";

        [Header("Условие (опционально)")]
        [SerializeField] private Core.Story.TriggerCondition _condition; // напр. нужен ключ
        [SerializeField] private string _lockedPrompt = "Заперто";

        [Header("Выдать триггер при переходе (опционально)")]
        [Tooltip("Метка, что игрок покинул сцену через эту дверь. Напр. чтобы закрыть " +
                 "сюжетный диалог, который нельзя пройти после ухода.")]
        [SerializeField] private Core.Flags.TriggerDefinition _giveTriggerOnTransition;

        public string Prompt => _prompt;

        public void Interact(InteractionContext context)
        {
            // Проверка условия (ключ/триггер), если задано.
            if (_condition != null)
            {
                context.Root.TryResolve<Core.Flags.FlagService>(out var flags);
                context.Root.TryResolve<Core.Inventory.InventoryService>(out var inv);
                var condCtx = new Core.Story.ConditionContext(flags, inv);
                if (!_condition.Evaluate(condCtx))
                {
                    CoreLog.Debug($"[Transition] {_lockedPrompt}: условие не выполнено");
                    return;
                }
            }

            // Метим уход со сцены (закрывает сюжетные ветки, недоступные после перехода).
            if (_giveTriggerOnTransition != null
                && context.Root.TryResolve<Core.Flags.FlagService>(out var flagsOut))
            {
                flagsOut.Set(_giveTriggerOnTransition);
                CoreLog.Debug($"[Transition] выдан триггер {_giveTriggerOnTransition.id}");
            }

            var stateService = context.Root.Resolve<GameStateService>();
            var transition = context.Root.Resolve<Subject<SceneTransitionParameters>>(CoreSignals.TRANSITION);

            // Обновляем состояние: новая сцена + точка входа.
            // PlayerInitStep на новой сцене спавнит игрока по state.spawnId.
            var state = stateService.Runtime;
            state.sceneName = _targetScene;
            state.spawnId = _targetSpawnId;

            CoreLog.Debug($"[Transition] переход на '{_targetScene}', spawnId={_targetSpawnId}");

            transition.OnNext(new SceneTransitionParameters
            {
                nextSceneName = _targetScene,
                nextSpawnId = _targetSpawnId
            });
        }
    }
}