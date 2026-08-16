using UnityEngine;

namespace Core.Story
{
    /// <summary>
    /// Объект, который присутствует на сцене только при выполненном условии
    /// (проход открывается после триггера, предмет появляется по сюжету и т.д.).
    /// WorldObjectsInitStep деактивирует объект, если условие не выполнено.
    /// </summary>
    public interface IConditionalObject
    {
        TriggerCondition Condition { get; }
    }

    /// <summary>
    /// Готовый компонент: вешаешь на объект, задаёшь условие — объект будет активен
    /// только если условие выполнено на момент захода на сцену.
    /// </summary>
    public class ConditionalObject : MonoBehaviour, IConditionalObject
    {
        [SerializeField] private TriggerCondition _condition;
        public TriggerCondition Condition => _condition;
    }
}
