using System.Collections.Generic;
using System.Linq;
using Core.Flags;
using UnityEngine;

namespace Core.Story.Dialogue
{
    /// <summary>
    /// Диалог как граф узлов (ScriptableObject).
    ///
    /// Узлы двух типов (см. DialogueNode): реплики говорящего и варианты игрока.
    /// Стартовый узел — обычно Speech («Слушаю?») со списком nextIds на узлы-выборы.
    ///
    /// Ветка блокируется собственным триггером: узел в конце ветки выдаёт триггер,
    /// а первый узел-выбор ветки имеет условие «НЕТ этого триггера».
    /// Диалог целиком не блокируется — базовые ветки остаются доступны всегда.
    /// </summary>
    [CreateAssetMenu(fileName = "Dialogue", menuName = "Core/Story/Dialogue")]
    public class Dialogue : ScriptableObject
    {
        [Tooltip("id стартового узла.")]
        public string startNodeId;

        public List<DialogueNode> nodes = new();

        [Tooltip("Опционально: триггер, выдаваемый при первом полном прохождении диалога. " +
                 "Для блокировки ОТДЕЛЬНЫХ веток используй триггеры на узлах, а не это поле.")]
        public TriggerDefinition completionTrigger;

        public DialogueNode GetNode(string id)
            => string.IsNullOrEmpty(id) ? null : nodes.FirstOrDefault(n => n != null && n.id == id);

        public DialogueNode StartNode => GetNode(startNodeId);
    }
}
