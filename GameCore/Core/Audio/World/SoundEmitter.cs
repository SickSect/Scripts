using Core.DI;
using Core.Flags;
using Core.Inventory;
using Core.Story;
using UnityEngine;

namespace Core.Audio
{
    /// <summary>
    /// Точечный источник звука в мире: холодильник, лампа, телевизор, кран.
    /// Вешается на объект, играет петлю с его позиции, громкость падает с
    /// расстоянием — коллайдер и зоны не нужны, всё делает 3D-затухание.
    ///
    /// Не путать с фоновыми зонами: там звук без места (тон комнаты, улица за
    /// окном), и им нужен кроссфейд по входу/выходу. Здесь место есть.
    ///
    /// Условие (опционально) проверяется один раз при привязке: холодильник
    /// молчит, пока не дали свет. Для включения/выключения по ходу игры есть
    /// публичные Play/Stop — их дёргает сюжетное действие.
    /// </summary>
    public class SoundEmitter : MonoBehaviour
    {
        [Header("Звук")]
        [Tooltip("SoundDefinition с loop = ✓ и spatialBlend = 1.")]
        [SerializeField] private SoundDefinition _sound;

        [Tooltip("Множитель поверх громкости ассета — подкрутить конкретный объект, не трогая звук.")]
        [Range(0f, 2f)] [SerializeField] private float _volumeScale = 1f;

        [Header("Поведение")]
        [Tooltip("Начать играть сразу при входе на сцену.")]
        [SerializeField] private bool _playOnStart = true;

        [Tooltip("Условие запуска. Проверяется один раз при привязке. Пусто = играть всегда.")]
        [SerializeField] private TriggerCondition _condition;

        [Tooltip("Ехать за объектом. Нужно, только если объект движется — статике лишний расход.")]
        [SerializeField] private bool _follow = false;

        private AudioService _audio;
        private DIContainer _root;
        private SoundHandle _handle;
        private bool _allowed = true;

        public bool IsPlaying => _audio != null && _audio.IsAlive(_handle);

        /// <summary>Вызывается из AudioInitStep.</summary>
        public void Bind(AudioService audio, DIContainer root)
        {
            _audio = audio;
            _root = root;
            _allowed = EvaluateCondition();

            if (_playOnStart && _allowed) Play();
        }

        public void Play()
        {
            if (_audio == null || _sound == null) return;
            if (IsPlaying) return; // защита от двойного запуска

            _handle = _follow
                ? _audio.PlayAttached(_sound, transform, _volumeScale)
                : _audio.PlayAt(_sound, transform.position, _volumeScale);
        }

        public void Stop(float fadeOverride = -1f)
        {
            if (_audio == null || !_handle.IsValid) return;
            _audio.Stop(_handle, fadeOverride);
            _handle = SoundHandle.None;
        }

        private bool EvaluateCondition()
        {
            if (_condition == null || _root == null) return true;

            _root.TryResolve<FlagService>(out var flags);
            _root.TryResolve<InventoryService>(out var inventory);
            return _condition.Evaluate(new ConditionContext(flags, inventory));
        }

        private void OnEnable()
        {
            if (_audio != null && _playOnStart && _allowed) Play();
        }

        // Объект выключили или сцена уходит — петля обязана замолчать.
        // Сервис живёт в root и переживает сцены, сама она не остановится.
        private void OnDisable() => Stop();

        private void OnDestroy() => Stop(0f);

        // Радиусы слышимости прямо в сцене — иначе 3D-звук настраивается вслепую.
        private void OnDrawGizmosSelected()
        {
            if (_sound == null || _sound.spatialBlend <= 0f) return;

            Gizmos.color = new Color(0.2f, 0.8f, 0.6f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, _sound.minDistance);

            Gizmos.color = new Color(0.2f, 0.8f, 0.6f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, _sound.maxDistance);
        }
    }
}
