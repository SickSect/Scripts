using UnityEngine;

namespace Core.Stats
{
    /// <summary>
    /// Описание стата (ScriptableObject). Один ассет на стат: Health, Stamina, Sanity.
    /// Механически все статы одинаковы (шкала с макс/тек), различаются только настройками.
    ///
    /// Автоизменение: ratePerSecond прибавляется к значению каждую секунду.
    ///  - выносливость: ratePerSecond отрицательный (падает), или положительный (регенерирует).
    ///  - здоровье: обычно 0 (меняется только событиями), либо лёгкая регенерация.
    /// </summary>
    [CreateAssetMenu(fileName = "Stat", menuName = "Core/Stats/Stat")]
    public class StatDefinition : ScriptableObject
    {
        [Tooltip("Уникальный стабильный id для сохранений.")]
        public string id;

        [Tooltip("Отображаемое имя (для UI).")]
        public string displayName;

        [Min(1)] public float max = 100f;
        public float startValue = 100f;

        [Header("Автоизменение (в секунду)")]
        [Tooltip("Сколько прибавляется каждую секунду. Отрицательное = стат падает со временем.")]
        public float ratePerSecond = 0f;

        [Header("При достижении нуля")]
        [Tooltip("Триггер, который выдаётся при обнулении стата (напр. смерть, безумие). Опционально.")]
        public Core.Flags.TriggerDefinition onZeroTrigger;
    }
}
