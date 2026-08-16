using Core.Common;
using UnityEngine;

namespace Core.Story.Actions
{
    /// <summary>
    /// Действие: заспавнить префаб в точке события (Origin) или в позиции игрока.
    /// Универсальный спавн объекта — для декораций, эффектов, объектов сюжета.
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnPrefabAction", menuName = "Core/Story/Actions/Spawn Prefab")]
    public class SpawnPrefabAction : StoryAction
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private bool _atPlayer = false; // спавнить у игрока, а не в Origin

        public override void Execute(StoryActionContext context)
        {
            if (_prefab == null) return;

            Vector3 pos;
            Quaternion rot;
            if (_atPlayer && context.Player != null)
            {
                pos = context.Player.transform.position;
                rot = context.Player.transform.rotation;
            }
            else if (context.Origin != null)
            {
                pos = context.Origin.position;
                rot = context.Origin.rotation;
            }
            else
            {
                pos = Vector3.zero;
                rot = Quaternion.identity;
            }

            Object.Instantiate(_prefab, pos, rot);
            CoreLog.Debug($"[SpawnPrefabAction] заспавнен {_prefab.name}");
        }
    }
}
