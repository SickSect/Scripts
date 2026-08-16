using System.Collections.Generic;
using UnityEngine;

namespace Core.Stats
{
    /// <summary>
    /// Реестр всех статов игры (Health, Stamina, Sanity). Лежит в Resources/Core/StatDatabase.
    /// Из него StatsService создаёт рантайм-статы, и по id восстанавливает из сейва.
    /// </summary>
    [CreateAssetMenu(fileName = "StatDatabase", menuName = "Core/Stats/Stat Database")]
    public class StatDatabase : ScriptableObject
    {
        public List<StatDefinition> stats = new();
    }
}
