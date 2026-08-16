using UnityEngine;

namespace Core.SceneLoader
{
    [CreateAssetMenu(fileName = "SpawnNode", menuName = "Core/Scene System/Spawn Node")]
    public class SpawnNode : ScriptableObject
    {
        public string spawnName;
        public int spawnId;
    }
}
