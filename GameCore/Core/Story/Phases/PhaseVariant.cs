using System.Collections.Generic;
using Core.Flags;
using UnityEngine;

namespace Core.Story.Phases
{
    /// <summary>
    /// Вариант фазы шага (ScriptableObject). Шаги идут по порядку (1,2,3,4), но у каждого
    /// шага может быть несколько вариантов — какой активируется, решает activationCondition
    /// (по набранным триггерам). Так «путь зависит от выбора игрока».
    ///
    /// При активации фаза спавнит своё наполнение (spawns) в якоря сцены и выставляет метку.
    /// </summary>
    [CreateAssetMenu(fileName = "PhaseVariant", menuName = "Core/Story/Phase Variant")]
    public class PhaseVariant : ScriptableObject
    {
        public enum TimeOfDay { Morning, Day, Evening, Night }

        [Header("Идентификация")]
        [Tooltip("Номер шага (1,2,3,4). Фазы идут по возрастанию шага.")]
        public int step;
        [Tooltip("Уникальный id варианта (напр. '2B').")]
        public string variantId;

        [Header("Выбор варианта")]
        [Tooltip("Условие, при котором выбирается ЭТОТ вариант шага. Пусто = подходит всегда " +
                 "(ставь такой вариант последним как fallback).")]
        public TriggerCondition activationCondition;

        [Header("Наполнение (спавнится при старте фазы)")]
        public List<PhaseSpawn> spawns = new();

        [Header("Эффекты активации")]
        [Tooltip("Метка «эта фаза активна» — для условий/квестов/визуала. Опционально.")]
        public TriggerDefinition onActivateTrigger;

        [Tooltip("Время суток — для визуала день/ночь.")]
        public TimeOfDay timeOfDay = TimeOfDay.Day;
    }
}
