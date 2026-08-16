using System.Collections.Generic;
using Core.Flags;
using UnityEngine;

namespace Core.Stats
{
    /// <summary>
    /// Держит все статы игрока (Health, Stamina, Sanity...), тикает авто-изменение
    /// и реагирует на достижение нуля (выдаёт onZeroTrigger через FlagService).
    ///
    /// Не MonoBehaviour — Tick вызывается снаружи (StatsTicker) каждый кадр.
    /// Статы описаны набором StatDefinition (из StatDatabase).
    /// </summary>
    public class StatsService
    {
        private readonly Dictionary<string, Stat> _stats = new();
        private readonly FlagService _flags;
        private readonly HashSet<string> _zeroFired = new(); // чтобы onZero не спамил каждый кадр

        public IReadOnlyDictionary<string, Stat> Stats => _stats;

        public StatsService(IEnumerable<StatDefinition> definitions, FlagService flags)
        {
            _flags = flags;
            foreach (var def in definitions)
            {
                if (def == null || string.IsNullOrEmpty(def.id)) continue;
                _stats[def.id] = new Stat(def, def.startValue);
            }
        }

        public Stat Get(StatDefinition def) => def != null ? Get(def.id) : null;

        public Stat Get(string id) => _stats.TryGetValue(id, out var s) ? s : null;

        /// <summary>Изменить стат на delta (для эффектов). true — если стат найден.</summary>
        public bool Modify(StatDefinition def, float delta)
        {
            var stat = Get(def);
            if (stat == null) return false;
            stat.Modify(delta);
            return true;
        }

        /// <summary>Тик авто-изменения. dt — прошедшее время (обычно Time.deltaTime).</summary>
        public void Tick(float dt)
        {
            foreach (var stat in _stats.Values)
            {
                if (stat.Definition.ratePerSecond != 0f)
                    stat.Modify(stat.Definition.ratePerSecond * dt);

                // Реакция на ноль — один раз, пока стат в нуле.
                if (stat.IsZero)
                {
                    if (_zeroFired.Add(stat.Definition.id))
                        OnStatZero(stat);
                }
                else
                {
                    _zeroFired.Remove(stat.Definition.id); // восстановился — снова можно сработать
                }
            }
        }

        private void OnStatZero(Stat stat)
        {
            Core.Common.CoreLog.Debug($"[Stats] {stat.Definition.id} достиг нуля");
            if (stat.Definition.onZeroTrigger != null && _flags != null)
                _flags.Set(stat.Definition.onZeroTrigger);
        }

        // ---------- сохранение/загрузка ----------

        public void LoadFrom(StatsData data)
        {
            foreach (var e in data.stats)
            {
                var stat = Get(e.id);
                if (stat != null) stat.SetValue(e.value);
            }
            _zeroFired.Clear();
        }

        public void SaveInto(StatsData data)
        {
            data.stats = new List<StatsData.Entry>(_stats.Count);
            foreach (var kv in _stats)
                data.stats.Add(new StatsData.Entry { id = kv.Key, value = kv.Value.Value.Value });
        }
    }
}
