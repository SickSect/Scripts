using UnityEngine;

namespace Core.Audio
{
    /// <summary>
    /// Поверхность под ногами (ScriptableObject). Дерево, бетон, плитка, ковёр.
    /// Хранит не клипы, а ссылки на SoundDefinition — вариации, разброс высоты и
    /// защита от спама настраиваются там же, где и у любого другого звука.
    ///
    /// Именование: SUR_Wood, SUR_Concrete, SUR_Tile, SUR_Carpet.
    /// </summary>
    [CreateAssetMenu(fileName = "SUR_", menuName = "Core/Audio/Surface")]
    public class SurfaceDefinition : ScriptableObject
    {
        [Tooltip("Строка для логов. На логику не влияет.")]
        public string id;

        [Tooltip("Шаг при ходьбе. Внутри ассета — 4-6 вариантов клипа, иначе слышна повторяемость.")]
        public SoundDefinition footsteps;

        [Tooltip("Отдельный набор для бега. Пусто → берётся footsteps, погромче.")]
        public SoundDefinition footstepsRun;

        [Tooltip("Приземление. Пока не используется — задел под прыжок/падение.")]
        public SoundDefinition landing;

        public string DebugName => string.IsNullOrEmpty(id) ? name : id;
    }
}