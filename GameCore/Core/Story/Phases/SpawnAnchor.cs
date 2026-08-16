using UnityEngine;

namespace Core.Story.Phases
{
    /// <summary>
    /// Якорь-точка на сцене: место, куда фаза спавнит предмет/событие.
    /// Позиции не хардкодятся — расставляешь якоря на сцене, фаза ссылается на них по anchorId.
    ///
    /// anchorId уникален в пределах сцены. Один якорь в разных фазах может спавнить разное.
    /// </summary>
    public class SpawnAnchor : MonoBehaviour
    {
        [Tooltip("Уникальный id якоря в пределах сцены.")]
        public string anchorId;
    }
}
