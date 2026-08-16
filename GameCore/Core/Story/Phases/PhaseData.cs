using System;

namespace Core.Story.Phases
{
    /// <summary>
    /// Сериализуемое состояние фаз для снапшота: текущий шаг + id активного варианта + день.
    /// </summary>
    [Serializable]
    public class PhaseData
    {
        public int currentStep = 1;
        public string currentVariantId;  // какой вариант активен
        public int dayNumber = 1;

        public PhaseData Clone() => new PhaseData
        {
            currentStep = currentStep,
            currentVariantId = currentVariantId,
            dayNumber = dayNumber
        };
    }
}
