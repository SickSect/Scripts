using System;
using System.Collections.Generic;
using Core.Flags;
using Core.Inventory;
using UnityEngine;

namespace Core.Story
{
    /// <summary>
    /// Условие доступности (ScriptableObject) с поддержкой И/ИЛИ/НЕ.
    ///
    /// Устроено так, чтобы не городить дерево в инспекторе:
    ///  - mode: All (И — нужны все пункты) или Any (ИЛИ — достаточно одного);
    ///  - каждый пункт (Requirement) — это триггер ИЛИ предмет, с флагом negate (НЕ);
    ///  - для вложенности пункт может ссылаться на другое TriggerCondition (под-условие).
    ///
    /// Примеры:
    ///  "нужен ключ И включён генератор" → mode=All, [предмет ключ, триггер генератор].
    ///  "есть отмычка ИЛИ ключ"          → mode=Any, [предмет отмычка, предмет ключ].
    ///  "НЕ заперто"                      → пункт с триггером "locked" и negate=true.
    /// </summary>
    [CreateAssetMenu(fileName = "Condition", menuName = "Core/Story/Condition")]
    public class TriggerCondition : ScriptableObject
    {
        public enum Mode { All, Any }

        [Serializable]
        public struct Requirement
        {
            [Tooltip("Инвертировать: пункт выполнен, если условие НЕ выполнено.")]
            public bool negate;

            [Header("Один из вариантов (заполни ровно один):")]
            public TriggerDefinition trigger; // требуется наличие триггера
            public ItemDefinition item;        // требуется наличие предмета
            public int itemCount;              // сколько штук (для item), 0/1 = 1
            public TriggerCondition subCondition; // вложенное условие
        }

        public Mode mode = Mode.All;
        public List<Requirement> requirements = new();

        public bool Evaluate(ConditionContext ctx)
        {
            if (requirements == null || requirements.Count == 0) return true; // нет условий = открыто

            foreach (var req in requirements)
            {
                bool ok = EvaluateOne(req, ctx);
                if (req.negate) ok = !ok;

                if (mode == Mode.All && !ok) return false; // И: любой невыполненный → false
                if (mode == Mode.Any && ok) return true;   // ИЛИ: любой выполненный → true
            }

            return mode == Mode.All; // All: все прошли → true; Any: ни один → false
        }

        private static bool EvaluateOne(Requirement req, ConditionContext ctx)
        {
            if (req.subCondition != null)
                return req.subCondition.Evaluate(ctx);

            if (req.trigger != null)
                return ctx.Flags != null && ctx.Flags.Has(req.trigger);

            if (req.item != null)
                return ctx.Inventory != null && ctx.Inventory.Has(req.item, Mathf.Max(1, req.itemCount));

            return true; // пустой пункт игнорируем как выполненный
        }
    }
}
