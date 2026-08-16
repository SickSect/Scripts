using System;
using System.Collections.Generic;
using Core.Common;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Audio
{
    /// <summary>
    /// Единственная точка воспроизведения звука в игре. Живёт в root-контейнере
    /// (регистрируется в GameBootstrap), переживает смену сцен.
    ///
    /// Отвечает за: пул источников, случайные вариации, фейды, громкости по
    /// категориям, приглушение (дак) и защиту от спама (cooldown / maxInstances).
    ///
    /// Никто, кроме этого сервиса, не должен звать AudioSource.Play напрямую —
    /// иначе звук пройдёт мимо громкостей и микшера.
    ///
    /// Всё время внутри — НЕмасштабированное: на паузе timeScale = 0, а звук живёт.
    /// </summary>
    public class AudioService : IDisposable
    {
        /// <summary>Один звучащий экземпляр звука.</summary>
        private class Voice
        {
            public int Id;
            public AudioSource Source;
            public SoundDefinition Def;
            public Transform Follow;      // за кем ехать (null = стоит на месте)
            public bool HadFollow;        // цель была задана при запуске (чтобы поймать её уничтожение)
            public bool Loop;

            public float BaseVolume;      // громкость ассета после разброса
            public float Scale = 1f;      // внешний множитель (зоны, дак конкретного голоса)
            public float ScaleTarget = 1f;
            public float ScaleSpeed;      // 0 = мгновенно

            public float Envelope;        // огибающая fade-in / fade-out, 0..1
            public float EnvelopeTarget = 1f;
            public float EnvelopeSpeed;

            public bool Stopping;
            public float StartedAt;
        }

        private const int PrewarmSources = 8;
        private const int MaxSources = 32;

        /// <summary>Потолок внешнего множителя громкости (итог всё равно клампится в 1).</summary>
        private const float MaxVolumeScale = 2f;

        private readonly GameObject _rootGo;
        private readonly Transform _root;
        private readonly AudioSourcePool _pool;
        private readonly AudioMixer _mixer;
        private readonly Dictionary<AudioCategory, AudioMixerGroup> _groups = new();

        private readonly List<Voice> _voices = new();
        private readonly Dictionary<int, Voice> _byId = new();
        private readonly Dictionary<SoundDefinition, float> _lastPlayTime = new();

        private readonly AudioCategory[] _categories;
        private readonly Dictionary<AudioCategory, float> _volumes = new();
        private readonly Dictionary<AudioCategory, float> _duck = new();
        private readonly Dictionary<AudioCategory, float> _duckTarget = new();
        private readonly Dictionary<AudioCategory, float> _duckSpeed = new();

        private int _nextId = 1;
        private float _master = 1f;

        public float Master => _master;

        public AudioService()
        {
            _categories = (AudioCategory[])Enum.GetValues(typeof(AudioCategory));

            _rootGo = new GameObject("[AUDIO]");
            UnityEngine.Object.DontDestroyOnLoad(_rootGo);
            _root = _rootGo.transform;

            _rootGo.AddComponent<AudioRunner>().Bind(this);

            _pool = new AudioSourcePool(_root, PrewarmSources, MaxSources);

            // Микшер опционален: без него громкости считаются множителями в коде.
            _mixer = Resources.Load<AudioMixer>("Core/AudioMixer");
            if (_mixer != null) MapMixerGroups();

            foreach (var c in _categories)
            {
                _volumes[c] = 1f;
                _duck[c] = 1f;
                _duckTarget[c] = 1f;
                _duckSpeed[c] = 0f;
            }

            AudioSettingsStore.Load(this);
        }

        // ---------------- микшер ----------------

        private static string GroupName(AudioCategory c) => c switch
        {
            AudioCategory.Music => "Music",
            AudioCategory.Ambient => "Ambient",
            AudioCategory.Sfx => "SFX",
            AudioCategory.UI => "UI",
            AudioCategory.Player => "Player",
            _ => "Master",
        };

        private void MapMixerGroups()
        {
            foreach (var c in _categories)
            {
                var found = _mixer.FindMatchingGroups(GroupName(c));
                if (found != null && found.Length > 0) _groups[c] = found[0];
                else CoreLog.Debug($"[Audio] в микшере нет группы {GroupName(c)} — категория пойдёт мимо микшера");
            }
        }

        // ---------------- воспроизведение ----------------

        /// <summary>Звук «сам по себе»: 2D, либо 3D в нулевой точке (для 3D используй PlayAt).</summary>
        public SoundHandle Play(SoundDefinition def, float volumeScale = 1f) =>
            PlayInternal(def, null, Vector3.zero, volumeScale);

        /// <summary>Звук в точке мира.</summary>
        public SoundHandle PlayAt(SoundDefinition def, Vector3 position, float volumeScale = 1f) =>
            PlayInternal(def, null, position, volumeScale);

        /// <summary>Звук, едущий за целью (гудящая лампа на движущемся объекте).</summary>
        public SoundHandle PlayAttached(SoundDefinition def, Transform follow, float volumeScale = 1f) =>
            PlayInternal(def, follow, follow != null ? follow.position : Vector3.zero, volumeScale);

        /// <param name="volumeScale">
        /// Множитель поверх громкости ассета: подкрутить конкретное срабатывание,
        /// не трогая сам SoundDefinition (бег громче ходьбы, зона тише базовой).
        /// Итоговая громкость всё равно ограничена единицей.
        /// </param>
        private SoundHandle PlayInternal(SoundDefinition def, Transform follow, Vector3 position, float volumeScale)
        {
            if (def == null) return SoundHandle.None;

            if (!def.HasClips)
            {
                CoreLog.Debug($"[Audio] {def.DebugName}: пустой список клипов");
                return SoundHandle.None;
            }

            // На паузе играет только то, что явно помечено ignorePause (UI).
            if (Time.timeScale <= 0f && !def.ignorePause)
                return SoundHandle.None;

            float now = Time.unscaledTime;

            if (def.cooldown > 0f && _lastPlayTime.TryGetValue(def, out var last)
                && now - last < def.cooldown)
                return SoundHandle.None;

            EnforceInstanceLimit(def);

            var src = _pool.Rent();
            if (src == null)
            {
                CoreLog.Debug($"[Audio] {def.DebugName}: пул источников исчерпан ({MaxSources})");
                return SoundHandle.None;
            }

            var clip = def.PickClip();
            if (clip == null)
            {
                _pool.Return(src);
                return SoundHandle.None;
            }

            src.clip = clip;
            src.loop = def.loop;
            src.pitch = def.RollPitch();
            src.spatialBlend = Mathf.Clamp01(def.spatialBlend);
            // Спатиализатор (Steam Audio) включаем только для 3D-звуков: гонять через
            // окклюзию шаги игрока и UI незачем, они и так звучат «изнутри».
            src.spatialize = def.spatialBlend > 0f;
            if (src.spatialize) _pool.EnsureSpatializer(src);
            _pool.Label(src, def.DebugName);
            src.rolloffMode = def.rolloff;
            src.minDistance = Mathf.Max(0.01f, def.minDistance);
            src.maxDistance = Mathf.Max(src.minDistance + 0.01f, def.maxDistance);
            src.transform.position = follow != null ? follow.position : position;
            src.outputAudioMixerGroup = _groups.TryGetValue(def.category, out var g) ? g : null;

            var voice = new Voice
            {
                Id = _nextId++,
                Source = src,
                Def = def,
                Follow = follow,
                HadFollow = follow != null,
                Loop = def.loop,
                BaseVolume = def.RollVolume(),
                Scale = Mathf.Clamp(volumeScale, 0f, MaxVolumeScale),
                ScaleTarget = Mathf.Clamp(volumeScale, 0f, MaxVolumeScale),
                Envelope = def.fadeIn > 0f ? 0f : 1f,
                EnvelopeTarget = 1f,
                EnvelopeSpeed = def.fadeIn > 0f ? 1f / def.fadeIn : 0f,
                StartedAt = now,
            };

            src.volume = Effective(voice);
            src.Play();

            _voices.Add(voice);
            _byId[voice.Id] = voice;
            _lastPlayTime[def] = now;

            CoreLog.Debug($"[Audio] {def.DebugName} → {def.category} @ {src.transform.position}");

            return new SoundHandle(voice.Id);
        }

        /// <summary>Если экземпляров этого звука уже максимум — глушим самый старый.</summary>
        private void EnforceInstanceLimit(SoundDefinition def)
        {
            int limit = Mathf.Max(1, def.maxInstances);

            int count = 0;
            Voice oldest = null;
            for (int i = 0; i < _voices.Count; i++)
            {
                var v = _voices[i];
                if (v.Def != def || v.Stopping) continue;
                count++;
                if (oldest == null || v.StartedAt < oldest.StartedAt) oldest = v;
            }

            if (count >= limit && oldest != null)
                StopVoice(oldest, 0f);
        }

        // ---------------- остановка ----------------

        /// <summary>Остановить конкретный голос. fadeOverride &lt; 0 = взять fadeOut из ассета.</summary>
        public void Stop(SoundHandle handle, float fadeOverride = -1f)
        {
            if (!handle.IsValid || !_byId.TryGetValue(handle.Id, out var voice)) return;
            StopVoice(voice, fadeOverride);
        }

        private void StopVoice(Voice voice, float fadeOverride)
        {
            float fade = fadeOverride >= 0f ? fadeOverride : voice.Def.fadeOut;

            voice.Stopping = true;
            voice.EnvelopeTarget = 0f;
            voice.EnvelopeSpeed = fade > 0f ? 1f / fade : 0f;

            if (fade <= 0f) voice.Envelope = 0f; // уберётся ближайшим тиком
        }

        /// <summary>Остановить всё в категории (например, при выходе в меню).</summary>
        public void StopAll(AudioCategory category, float fade = 0f)
        {
            for (int i = 0; i < _voices.Count; i++)
                if (_voices[i].Def.category == category)
                    StopVoice(_voices[i], fade);
        }

        public void StopEverything(float fade = 0f)
        {
            for (int i = 0; i < _voices.Count; i++)
                StopVoice(_voices[i], fade);
        }

        public bool IsAlive(SoundHandle handle) =>
            handle.IsValid && _byId.TryGetValue(handle.Id, out var v) && !v.Stopping;

        // ---------------- громкость ----------------

        public void SetMaster(float value01, bool persist = true)
        {
            _master = Mathf.Clamp01(value01);
            if (persist) AudioSettingsStore.SaveMaster(_master);
        }

        public void SetVolume(AudioCategory category, float value01, bool persist = true)
        {
            _volumes[category] = Mathf.Clamp01(value01);
            if (persist) AudioSettingsStore.SaveCategory(category, _volumes[category]);
        }

        public float GetVolume(AudioCategory category) =>
            _volumes.TryGetValue(category, out var v) ? v : 1f;

        /// <summary>Приглушить категорию до factor (0..1) за fade секунд. Петли не останавливаются.</summary>
        public void Duck(AudioCategory category, float factor, float fade = 0.2f)
        {
            _duckTarget[category] = Mathf.Clamp01(factor);
            _duckSpeed[category] = fade > 0f ? 1f / fade : 0f;
            if (fade <= 0f) _duck[category] = _duckTarget[category];
        }

        public void Unduck(AudioCategory category, float fade = 0.2f) => Duck(category, 1f, fade);

        /// <summary>Плавно изменить множитель конкретного голоса (используют фоновые зоны).</summary>
        public void FadeTo(SoundHandle handle, float scale, float time)
        {
            if (!handle.IsValid || !_byId.TryGetValue(handle.Id, out var voice)) return;

            voice.ScaleTarget = Mathf.Clamp(scale, 0f, MaxVolumeScale);
            voice.ScaleSpeed = time > 0f ? 1f / time : 0f;
            if (time <= 0f) voice.Scale = voice.ScaleTarget;
        }

        private float Effective(Voice v)
        {
            float category = _volumes.TryGetValue(v.Def.category, out var cv) ? cv : 1f;
            float duck = _duck.TryGetValue(v.Def.category, out var dv) ? dv : 1f;
            return Mathf.Clamp01(v.BaseVolume * v.Scale * v.Envelope * category * duck * _master);
        }

        // ---------------- тик ----------------

        public void Tick(float dt)
        {
            if (dt < 0f) dt = 0f;

            // Дак по категориям.
            for (int i = 0; i < _categories.Length; i++)
            {
                var c = _categories[i];
                float cur = _duck[c], target = _duckTarget[c];
                if (Mathf.Approximately(cur, target)) continue;

                _duck[c] = _duckSpeed[c] <= 0f
                    ? target
                    : Mathf.MoveTowards(cur, target, _duckSpeed[c] * dt);
            }

            // Голоса — с конца, потому что удаляем по индексу.
            for (int i = _voices.Count - 1; i >= 0; i--)
            {
                var v = _voices[i];

                if (v.Source == null) { Retire(i); continue; }

                if (!Mathf.Approximately(v.Envelope, v.EnvelopeTarget))
                    v.Envelope = v.EnvelopeSpeed <= 0f
                        ? v.EnvelopeTarget
                        : Mathf.MoveTowards(v.Envelope, v.EnvelopeTarget, v.EnvelopeSpeed * dt);

                if (!Mathf.Approximately(v.Scale, v.ScaleTarget))
                    v.Scale = v.ScaleSpeed <= 0f
                        ? v.ScaleTarget
                        : Mathf.MoveTowards(v.Scale, v.ScaleTarget, v.ScaleSpeed * dt);

                // Цель, за которой ехали, уничтожена — не оставляем висеть петлю.
                if (v.HadFollow && v.Follow == null)
                {
                    if (!v.Stopping) StopVoice(v, 0.1f);
                }
                else if (v.Follow != null)
                {
                    v.Source.transform.position = v.Follow.position;
                }

                v.Source.volume = Effective(v);

                // Затух до нуля при остановке — снимаем.
                if (v.Stopping && v.Envelope <= 0.0001f) { Retire(i); continue; }

                // One-shot доиграл — снимаем.
                if (!v.Loop && !v.Source.isPlaying) { Retire(i); continue; }
            }
        }

        private void Retire(int index)
        {
            var v = _voices[index];
            _voices.RemoveAt(index);
            _byId.Remove(v.Id);
            _pool.Return(v.Source);
        }

        // ---------------- очистка ----------------

        public void Dispose()
        {
            StopEverything(0f);
            _voices.Clear();
            _byId.Clear();

            if (_rootGo != null) UnityEngine.Object.Destroy(_rootGo);
        }
    }
}