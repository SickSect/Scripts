using Core.DI;
using UnityEngine;

namespace Core.Story
{
    /// <summary>
    /// Контекст выполнения сюжетного действия: доступ к контейнерам, игроку и точке,
    /// в которой действие сработало (для спавна объектов, привязки скримера и т.д.).
    /// </summary>
    public class StoryActionContext
    {
        public readonly DIContainer Root;
        public readonly GameObject Player;
        public readonly Transform Origin; // точка события (якорь/зона), может быть null

        /// <summary>Раннер корутин (для длительных действий: скример, задержки).</summary>
        public MonoBehaviour CoroutineRunner => _runner;
        private readonly MonoBehaviour _runner;

        public StoryActionContext(DIContainer root, GameObject player, Transform origin, MonoBehaviour runner)
        {
            Root = root;
            Player = player;
            Origin = origin;
            _runner = runner;
        }
    }
}

