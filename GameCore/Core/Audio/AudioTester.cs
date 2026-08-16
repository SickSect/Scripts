using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Audio.Debugging
{
    /// <summary>
    /// Отладка: проигрывает звук по нажатию клавиши. Нужен ровно для одного —
    /// проверить, что ассет звука настроен и сервис работает, до того как появились
    /// зоны и события. В финальной сцене этому компоненту делать нечего.
    ///
    /// Кладётся на любой объект сцены, привязывается автоматически из AudioInitStep.
    /// </summary>
    public class AudioTester : MonoBehaviour
    {
        public enum Where
        {
            /// <summary>По настройкам ассета: 2D — везде, 3D — в нулевой точке.</summary>
            AsIs,
            /// <summary>В точке этого объекта.</summary>
            AtThisObject,
            /// <summary>С привязкой к этому объекту (едет за ним).</summary>
            AttachedToThisObject,
        }

        [SerializeField] private SoundDefinition _sound;
        [SerializeField] private Key _key = Key.P;
        [SerializeField] private Where _where = Where.AtThisObject;

        [Tooltip("Для петель: вторым нажатием остановить (иначе каждый раз запускается новая).")]
        [SerializeField] private bool _toggleLoop = true;

        private AudioService _audio;
        private SoundHandle _handle;

        public void Bind(AudioService audio) => _audio = audio;

        private void Update()
        {
            if (_audio == null || _sound == null) return;

            var kb = Keyboard.current;
            if (kb == null) return;
            if (!kb[_key].wasPressedThisFrame) return;

            if (_toggleLoop && _sound.loop && _audio.IsAlive(_handle))
            {
                _audio.Stop(_handle);
                _handle = SoundHandle.None;
                return;
            }

            _handle = _where switch
            {
                Where.AtThisObject         => _audio.PlayAt(_sound, transform.position),
                Where.AttachedToThisObject => _audio.PlayAttached(_sound, transform),
                _                          => _audio.Play(_sound),
            };
        }
    }
}
