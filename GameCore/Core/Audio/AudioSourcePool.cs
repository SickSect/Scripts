using System.Collections.Generic;
using UnityEngine;

// Namespace SteamAudio НЕ подключается через using: в нём есть собственный Vector3
// для обмена с нативным SDK, и он конфликтует с UnityEngine.Vector3.
// Типы плагина указываются полным именем.

namespace Core.Audio
{
    /// <summary>
    /// Пул AudioSource. Создавать источник на каждый звук — дорого и приводит к
    /// мусору, поэтому они переиспользуются: отзвучал — вернулся в стек.
    ///
    /// Пул растёт по требованию до maxSources. Упёрлись в потолок — новый звук
    /// не запускается (лучше пропустить один шаг, чем засрать смеситель).
    /// </summary>
    public class AudioSourcePool
    {
        private readonly Transform _root;
        private readonly Stack<AudioSource> _free = new();
        private readonly int _maxSources;
        private int _created;

        public int Created => _created;
        public int FreeCount => _free.Count;

        public AudioSourcePool(Transform root, int prewarm, int maxSources)
        {
            _root = root;
            _maxSources = Mathf.Max(1, maxSources);

            for (int i = 0; i < prewarm && i < _maxSources; i++)
                _free.Push(Create());
        }

        private AudioSource Create()
        {
            var go = new GameObject($"AudioSource_{_created:00}");
            go.transform.SetParent(_root, false);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            src.spatialize = false; // включается на конкретный звук в AudioService

            _created++;
            return src;
        }

        /// <summary>
        /// Повесить на источник компонент Steam Audio, если его ещё нет.
        ///
        /// ПОЧЕМУ НЕ ПРИ СОЗДАНИИ ПУЛА: GameBootstrap стартует на BeforeSceneLoad,
        /// а SteamAudioManager инициализируется позже. SteamAudioSource.Awake сразу
        /// лезет в SteamAudioManager.Simulator — на этапе создания пула симулятора
        /// ещё нет, и каждый источник валится с NullReferenceException.
        ///
        /// Поэтому компонент добавляется лениво, при первом 3D-воспроизведении:
        /// к этому моменту плагин готов. 2D-звуки (шаги, UI) его не получают вовсе.
        /// </summary>
        public void EnsureSpatializer(AudioSource src)
        {
#if STEAMAUDIO_ENABLED
            if (src == null) return;
            if (src.GetComponent<SteamAudio.SteamAudioSource>() != null) return;

            if (SteamAudio.SteamAudioManager.Singleton == null)
            {
                if (!_warnedNoManager)
                {
                    _warnedNoManager = true;
                    Debug.LogWarning("[Audio] SteamAudioManager не инициализирован — " +
                                     "звук отыграет без окклюзии.");
                }
                return;
            }

            ConfigureSteamAudio(src.gameObject);
#endif
        }

#if STEAMAUDIO_ENABLED
        private bool _warnedNoManager;

        /// <summary>
        /// Параметры Steam Audio для источника. В инспекторе выставить их не на чем —
        /// источники создаются кодом.
        ///
        /// Компонент добавляется ПОСЛЕ AudioSource: его Awake ищет AudioSource на том же
        /// объекте, и обратный порядок оставил бы его без источника.
        /// </summary>
        private static void ConfigureSteamAudio(GameObject go)
        {
            var sa = go.AddComponent<SteamAudio.SteamAudioSource>();

            // HRTF: бинауральный рендер прямого звука. Цель — наушники.
            sa.directBinaural = true;

            // Затухание по расстоянию оставляем Unity: кривая приходит из SoundDefinition
            // (minDistance / maxDistance / rolloff). Включить и здесь — применить дважды.
            sa.distanceAttenuation = false;
            sa.airAbsorption = false;

            // Ради чего всё затевалось.
            // Occlusion — приглушение за преградой, Volumetric даёт частичное
            // перекрытие вместо щелчка «слышно / не слышно» на краю стены.
            sa.occlusion = true;
            sa.occlusionType = SteamAudio.OcclusionType.Volumetric;
            sa.occlusionRadius = 0.5f;
            sa.occlusionSamples = 16;

            // Transmission — прохождение сквозь материал. Без него стена глушит
            // звук насмерть, а нужен глухой бубнёж.
            sa.transmission = true;
            sa.transmissionType = SteamAudio.TransmissionType.FrequencyDependent;
            sa.maxTransmissionSurfaces = 1;

            // Отражения и проведение через проёмы требуют запекания probe batch.
            // Включим, когда планировка перестанет меняться.
            sa.reflections = false;
            sa.pathing = false;
        }
#endif

        /// <summary>Взять свободный источник. null = пул исчерпан.</summary>
        public AudioSource Rent()
        {
            if (_free.Count > 0) return _free.Pop();
            if (_created >= _maxSources) return null;
            return Create();
        }

        /// <summary>
        /// Подписать источник именем звука — в редакторе видно прямо в иерархии,
        /// что именно сейчас играет. Восемь одинаковых AudioSource_XX иначе
        /// приходится перебирать вручную при каждой отладке.
        /// </summary>
        public void Label(AudioSource src, string label)
        {
#if UNITY_EDITOR
            if (src == null) return;

            var baseName = src.gameObject.name;
            int bracket = baseName.IndexOf(" [");
            if (bracket >= 0) baseName = baseName.Substring(0, bracket);

            src.gameObject.name = string.IsNullOrEmpty(label) ? baseName : $"{baseName} [{label}]";
#endif
        }

        /// <summary>Вернуть источник в пул (сбрасывает все настройки).</summary>
        public void Return(AudioSource src)
        {
            if (src == null) return;

            Label(src, null);

            src.Stop();
            src.clip = null;
            src.outputAudioMixerGroup = null;
            src.loop = false;
            src.volume = 1f;
            src.pitch = 1f;
            src.spatialBlend = 0f;
            src.spatialize = false;
            src.transform.localPosition = UnityEngine.Vector3.zero;

            _free.Push(src);
        }
    }
}