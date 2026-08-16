using System;
using System.Collections.Generic;

namespace Core.Stats
{
    /// <summary>
    /// Сериализуемое состояние статов для снапшота: id стата → текущее значение.
    /// </summary>
    [Serializable]
    public class StatsData
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public float value;
        }

        public List<Entry> stats = new();

        public StatsData Clone()
        {
            var copy = new StatsData { stats = new List<Entry>(stats.Count) };
            foreach (var e in stats) copy.stats.Add(e);
            return copy;
        }
    }
}
