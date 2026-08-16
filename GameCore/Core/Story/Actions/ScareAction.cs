using System.Collections;
using Core.Common;
using UnityEngine;

namespace Core.Story.Actions
{
    /// <summary>
    /// Действие-скример: спавнит пугающий объект (перед камерой или в точке), держит
    /// заданное время, играет звук, затем убирает. Длительное действие через корутину.
    /// </summary>
    [CreateAssetMenu(fileName = "ScareAction", menuName = "Core/Story/Actions/Scare")]
    public class ScareAction : StoryAction
    {
        [SerializeField] private GameObject _scarePrefab;
        [SerializeField] private AudioClip _sound;
        [SerializeField] private float _duration = 1.5f;
        [SerializeField] private bool _inFrontOfCamera = true;
        [SerializeField] private float _distanceFromCamera = 1.5f;

        public override void Execute(StoryActionContext context)
        {
            if (_scarePrefab == null || context.CoroutineRunner == null)
            {
                CoreLog.Debug("[ScareAction] нет префаба или раннера");
                return;
            }
            context.CoroutineRunner.StartCoroutine(Run(context));
        }

        private IEnumerator Run(StoryActionContext context)
        {
            var cam = Camera.main;
            Vector3 pos;
            Quaternion rot;

            if (_inFrontOfCamera && cam != null)
            {
                pos = cam.transform.position + cam.transform.forward * _distanceFromCamera;
                rot = Quaternion.LookRotation(cam.transform.position - pos); // лицом к камере
            }
            else if (context.Origin != null)
            {
                pos = context.Origin.position;
                rot = context.Origin.rotation;
            }
            else { pos = Vector3.zero; rot = Quaternion.identity; }

            var instance = Object.Instantiate(_scarePrefab, pos, rot);
            if (_sound != null)
                AudioSource.PlayClipAtPoint(_sound, pos);

            CoreLog.Debug("[ScareAction] БУ!");

            yield return new WaitForSeconds(_duration);

            if (instance != null) Object.Destroy(instance);
        }
    }
}
