using System.Collections.Generic;
using Core.DI;
using UnityEngine;

namespace Core.Story.Phases
{
    /// <summary>
    /// Контекст спавна фазы: доступ к контейнерам и к якорям текущей сцены (по anchorId).
    /// </summary>
    public class PhaseSpawnContext
    {
        public readonly DIContainer Root;
        private readonly Dictionary<string, Transform> _anchors;

        public PhaseSpawnContext(DIContainer root, Dictionary<string, Transform> anchors)
        {
            Root = root;
            _anchors = anchors;
        }

        /// <summary>Найти якорь по id. null — если на текущей сцене такого нет.</summary>
        public Transform GetAnchor(string anchorId)
        {
            if (string.IsNullOrEmpty(anchorId)) return null;
            return _anchors.TryGetValue(anchorId, out var t) ? t : null;
        }
    }
}
