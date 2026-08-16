using Core.Common;
using Core.Story.Phases;
using UnityEngine;

namespace Core.Story.Actions
{
    /// <summary>
    /// Действие: завершить текущую фазу и перейти к следующему шагу.
    /// Вешается на кровать («Лечь спать»), событие («напился и уснул»), дверь ухода с работы.
    ///
    /// Переход выбирает вариант следующего шага по условиям (что игрок успел сделать).
    /// Наполнение новой фазы заспавнится на текущей сцене (смены сцены нет).
    /// </summary>
    [CreateAssetMenu(fileName = "AdvancePhaseAction", menuName = "Core/Story/Actions/Advance Phase")]
    public class AdvancePhaseAction : StoryAction
    {
        public override void Execute(StoryActionContext context)
        {
            if (context.Root.TryResolve<PhaseService>(out var phases))
            {
                phases.Advance();
                CoreLog.Debug("[AdvancePhaseAction] переход к следующей фазе");
            }
        }
    }
}
