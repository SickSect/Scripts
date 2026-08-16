using UnityEngine;

namespace Core.SceneLoader
{
    [CreateAssetMenu(fileName = "SceneNode", menuName = "Core/Scene System/Scene Node")]
    public class SceneNode : ScriptableObject
    {
        public string sceneName;
        public bool isPersistent = false; // держать через DontDestroyOnLoad?
    }
}
