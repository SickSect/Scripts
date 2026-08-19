using System.Collections.Generic;
using UnityEngine;

namespace Core.Interaction
{
    public interface ICarryData
    {
        public string TargetZoneId { get; }
        List<ScriptableObject> GetData();
    }
}