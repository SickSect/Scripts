using R3;
using UnityEngine;

namespace Core.Stats
{
    /// <summary>
    /// Рантайм-значение одного стата. Текущее значение — ReactiveProperty (для UI-баров).
    /// Логику авто-изменения и реакции на ноль ведёт StatsService.
    /// </summary>
    public class Stat
    {
        public StatDefinition Definition { get; }

        /// <summary>Текущее значение (0..max). Подписывайся для UI.</summary>
        public ReactiveProperty<float> Value { get; }

        public float Max => Definition.max;
        public float Normalized => Definition.max <= 0 ? 0 : Value.Value / Definition.max;
        public bool IsZero => Value.Value <= 0f;
        public bool IsFull => Value.Value >= Definition.max;

        public Stat(StatDefinition def, float value)
        {
            Definition = def;
            Value = new ReactiveProperty<float>(Mathf.Clamp(value, 0f, def.max));
        }

        /// <summary>Изменить на delta (может быть отрицательным). Клампится в [0, max].</summary>
        public void Modify(float delta)
        {
            Value.Value = Mathf.Clamp(Value.Value + delta, 0f, Definition.max);
        }

        public void SetValue(float v)
        {
            Value.Value = Mathf.Clamp(v, 0f, Definition.max);
        }
    }
}
