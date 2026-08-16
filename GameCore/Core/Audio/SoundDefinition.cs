using UnityEngine;

namespace Core.Audio
{
    /// <summary>
    /// Единица звука в игре (ScriptableObject). Один ассет = один звук, а не один файл:
    /// внутри может лежать несколько вариантов клипа, играется случайный.
    ///
    /// Всё, что умеет звучать (зоны, действия, шаги, UI), ссылается на этот ассет,
    /// а не на AudioClip напрямую — так настройка звука лежит в одном месте.
    ///
    /// Именование ассетов: SND_&lt;категория&gt;_&lt;что&gt; — SND_SFX_DoorCreak, SND_AMB_StudioRoom.
    /// </summary>
    [CreateAssetMenu(fileName = "SND_", menuName = "Core/Audio/Sound")]
    public class SoundDefinition : ScriptableObject
    {
        [Header("Базовое")]
        [Tooltip("Строка для логов и дебага. На логику не влияет.")]
        public string id;

        [Tooltip("Варианты клипа. Играется случайный, подряд один и тот же не повторяется.")]
        public AudioClip[] clips;

        public AudioCategory category = AudioCategory.Sfx;

        [Header("Громкость и высота")]
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Разброс громкости ±. 0.1 → каждый раз 0.9-1.1 от базовой.")]
        [Range(0f, 0.5f)] public float volumeJitter = 0f;

        [Range(0.1f, 3f)] public float pitch = 1f;

        [Tooltip("Разброс высоты ±. Главное средство против «пулемёта»: для шагов 0.08-0.12.")]
        [Range(0f, 0.5f)] public float pitchJitter = 0f;

        [Header("Пространство")]
        [Tooltip("0 = 2D (без расстояния и панорамы), 1 = полное 3D. " +
                 "Всё, что стоит в мире — 1. Всё, что издаёт сам игрок — 0: " +
                 "слушатель сидит на камере, и 3D сделало бы громкость шагов " +
                 "заложником зума и подтягивания камеры к стенам.")]
        [Range(0f, 1f)] public float spatialBlend = 0f;

        [Tooltip("Ближе этого расстояния громкость не растёт — радиус «источника». Работает при spatialBlend > 0.")]
        public float minDistance = 1f;

        [Tooltip("Дальше этого не слышно.")]
        public float maxDistance = 20f;

        public AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;

        [Header("Поведение")]
        [Tooltip("Петля. Обязательно для фонов, запрещено для one-shot.")]
        public bool loop = false;

        [Tooltip("Секунд на нарастание. Для петель 1-3, иначе слышен щелчок включения.")]
        public float fadeIn = 0f;

        [Tooltip("Секунд на затухание при остановке.")]
        public float fadeOut = 0f;

        [Tooltip("Минимум секунд между повторными запусками. Защита от спама.")]
        public float cooldown = 0f;

        [Tooltip("Сколько экземпляров звучит одновременно. Следующий вытесняет самый старый.")]
        public int maxInstances = 4;

        [Tooltip("Играть при Time.timeScale = 0. Обязательно для всех UI-звуков, иначе меню немое.")]
        public bool ignorePause = false;

        // Последний сыгранный индекс — чтобы не повторять клип подряд.
        // NonSerialized: это рантайм-состояние, в ассет не пишется.
        [System.NonSerialized] private int _lastIndex = -1;

        public bool HasClips => clips != null && clips.Length > 0;

        public string DebugName => string.IsNullOrEmpty(id) ? name : id;

        /// <summary>Случайный клип, но не тот же самый, что был в прошлый раз.</summary>
        public AudioClip PickClip()
        {
            if (!HasClips) return null;
            if (clips.Length == 1) return clips[0];

            int i = Random.Range(0, clips.Length);
            if (i == _lastIndex) i = (i + 1) % clips.Length;
            _lastIndex = i;
            return clips[i];
        }

        public float RollVolume() =>
            Mathf.Clamp01(volume + Random.Range(-volumeJitter, volumeJitter));

        public float RollPitch() =>
            Mathf.Max(0.01f, pitch + Random.Range(-pitchJitter, pitchJitter));
    }
}