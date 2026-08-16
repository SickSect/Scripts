using UnityEngine;

namespace Core.SceneLoader
{
    /// <summary>
    /// Маркер точки спавна на сцене. Кладётся на пустой GameObject внутри объекта "Spawns".
    /// </summary>
    public class Spawn : MonoBehaviour
    {
        public SpawnNode spawnInfo;
    }
}
