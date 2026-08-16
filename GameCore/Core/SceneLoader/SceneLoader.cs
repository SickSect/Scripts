using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.SceneLoader
{
    /// <summary>
    /// Загружает сцены по имени (через SceneGraph) и умеет спавнить объект
    /// на нужной точке спавна текущей сцены.
    /// </summary>
    public class SceneLoader
    {
        public SceneGraph graph { get; }

        private readonly Dictionary<int, Transform> _spawnsById = new();
        private readonly Dictionary<string, Transform> _spawnsByName = new();

        public SceneLoader(SceneGraph graph) => this.graph = graph;

        public SceneNode FindNode(string sceneName)
        {
            if (graph?.scenes == null) return null;
            return graph.scenes.FirstOrDefault(n => n != null && n.sceneName == sceneName);
        }

        public IEnumerator LoadSceneAsync(string sceneName)
        {
            var node = FindNode(sceneName);
            if (node == null || !IsInBuild(node.sceneName))
            {
                Debug.LogError($"[SceneLoader] Сцена не найдена или не в Build Settings: {sceneName}");
                yield break;
            }

            if (SceneManager.GetSceneByName(node.sceneName).isLoaded)
                yield break; // уже загружена

            yield return SceneManager.LoadSceneAsync(node.sceneName, LoadSceneMode.Single);
        }

        /// <summary>Найти на текущей сцене корневой объект "Spawns" и собрать все точки спавна.</summary>
        public void BindSpawnsOnScene(GameObject spawnsRoot)
        {
            _spawnsById.Clear();
            _spawnsByName.Clear();
            if (spawnsRoot == null)
            {
                Debug.LogWarning("[SceneLoader] Корень Spawns не найден на сцене.");
                return;
            }

            foreach (var spawn in spawnsRoot.GetComponentsInChildren<Spawn>(includeInactive: true))
            {
                if (spawn.spawnInfo == null) continue;
                _spawnsById[spawn.spawnInfo.spawnId] = spawn.transform;
                _spawnsByName[spawn.spawnInfo.spawnName] = spawn.transform;
            }
        }

        public GameObject SpawnAt(GameObject prefab, int spawnId)
        {
            if (!_spawnsById.TryGetValue(spawnId, out var t))
            {
                Debug.LogError($"[SceneLoader] Spawn id={spawnId} не найден. Спавню в (0,0).");
                return Object.Instantiate(prefab);
            }
            Debug.Log($"[SceneLoader] точка id={spawnId} мировая позиция={t.position}");
            return Object.Instantiate(prefab, t.position, t.rotation);
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static bool IsInBuild(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                if (SceneUtility.GetScenePathByBuildIndex(i).EndsWith(sceneName + ".unity"))
                    return true;
            return false;
        }
    }
}
