using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Init
{
    /// <summary>
    /// Прогоняет набор шагов инициализации по порядку (Order).
    /// Чтобы добавить новую механику в инициализацию сцены — достаточно зарегистрировать
    /// её IInitStep через Add(...), не трогая сам bootstrap.
    /// </summary>
    public class Initializer
    {
        private readonly List<IInitStep> _steps = new();

        public Initializer Add(IInitStep step)
        {
            if (step != null) _steps.Add(step);
            return this;
        }

        public Initializer AddRange(IEnumerable<IInitStep> steps)
        {
            foreach (var s in steps) Add(s);
            return this;
        }

        public void Run(InitContext ctx)
        {
            foreach (var step in _steps.OrderBy(s => s.Order))
            {
#if UNITY_EDITOR
                Debug.Log($"[Initializer] → {step.GetType().Name} (order {step.Order})");
#endif
                step.Execute(ctx);
            }
        }
    }
}
