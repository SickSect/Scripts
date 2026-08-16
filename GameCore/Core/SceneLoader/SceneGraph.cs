using UnityEngine;

namespace Core.SceneLoader
{
    /// <summary>
    /// Реестр всех сцен проекта. scenes[0] — обычно меню, scenes[1] — первая игровая сцена.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneGraph", menuName = "Core/Scene System/Scene Graph")]
    public class SceneGraph : ScriptableObject
    {
        public SceneNode[] scenes;
    }
}
