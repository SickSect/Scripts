using UnityEngine;

namespace Core.Flags
{
    /// <summary>
    /// Сюжетный триггер (ScriptableObject). Один ассет на флаг: "включил генератор",
    /// "узнал код", "поговорил с боссом". Перетаскивается в инспекторе — не строка.
    ///
    /// Триггеры глобальные: метка живёт на всю игру независимо от сцены.
    /// </summary>
    [CreateAssetMenu(fileName = "Trigger", menuName = "Core/Story/Trigger")]
    public class TriggerDefinition : ScriptableObject
    {
        [Tooltip("Уникальный стабильный id для сохранений. Не менять после релиза.")]
        public string id;

        [Tooltip("Человекочитаемое описание (для себя, в игре не показывается).")]
        [TextArea] public string editorNote;
    }
}
