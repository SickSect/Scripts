using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Story.Phases
{
    /// <summary>
    /// Реестр всех вариантов фаз. PhaseService из него выбирает, какой вариант активировать
    /// на каждом шаге. Лежит в Resources/Core/PhaseGraph.
    /// </summary>
    [CreateAssetMenu(fileName = "PhaseGraph", menuName = "Core/Story/Phase Graph")]
    public class PhaseGraph : ScriptableObject
    {
        public List<PhaseVariant> variants = new();

        /// <summary>Все варианты заданного шага (в порядке списка — важен для fallback).</summary>
        public IEnumerable<PhaseVariant> VariantsOfStep(int step)
            => variants.Where(v => v != null && v.step == step);

        public PhaseVariant FindById(int step, string variantId)
            => variants.FirstOrDefault(v => v != null && v.step == step && v.variantId == variantId);

        public int MaxStep => variants.Count == 0 ? 0 : variants.Max(v => v != null ? v.step : 0);
    }
}
