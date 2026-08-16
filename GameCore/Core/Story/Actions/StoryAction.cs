using UnityEngine;

namespace Core.Story
{
    /// <summary>
    /// Базовое сюжетное действие (ScriptableObject). Что происходит, когда событие срабатывает:
    /// выдать триггер, показать скример, заспавнить объект, (позже) запустить диалог/катсцену.
    ///
    /// Новый вид действия = новый наследник StoryAction. Событие и сервис не меняются —
    /// тот же паттерн, что ItemEffect у предметов.
    ///
    /// Действия могут быть мгновенными (выдать флаг) или длительными (скример на 2 сек,
    /// диалог) — для длительных используется корутина через ActionRunner в контексте.
    /// </summary>
    public abstract class StoryAction : ScriptableObject
    {
        public abstract void Execute(StoryActionContext context);
    }
}
