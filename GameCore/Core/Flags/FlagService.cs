using System.Collections.Generic;
using R3;

namespace Core.Flags
{
    /// <summary>
    /// Сервис меток состояния. Единый механизм для сюжетных триггеров (глобальных)
    /// и сценовых меток (собранные предметы, открытые двери).
    ///
    /// Глобальная метка:  ключ = triggerId.
    /// Сценовая метка:     ключ = "{scene}/{localId}".
    ///
    /// Проверка наличия защищает от дублей (повторный триггер не добавляется).
    /// Changed дёргается при изменении набора — для реактивных условий/UI.
    /// </summary>
    public class FlagService
    {
        private readonly HashSet<string> _set = new();

        /// <summary>Дёргается при установке/снятии любой метки.</summary>
        public Subject<Unit> Changed { get; } = new();

        // ---------- загрузка/сохранение ----------

        public void LoadFrom(FlagStore store)
        {
            _set.Clear();
            foreach (var f in store.flags) _set.Add(f);
            Changed.OnNext(Unit.Default);
        }

        public void SaveInto(FlagStore store)
        {
            store.flags = new List<string>(_set);
        }

        // ---------- глобальные (триггеры) ----------

        public bool Has(TriggerDefinition trigger)
            => trigger != null && _set.Contains(trigger.id);

        public void Set(TriggerDefinition trigger)
        {
            if (trigger == null || string.IsNullOrEmpty(trigger.id)) return;
            if (_set.Add(trigger.id)) Changed.OnNext(Unit.Default); // Add=false если уже был
        }

        public void Clear(TriggerDefinition trigger)
        {
            if (trigger != null && _set.Remove(trigger.id)) Changed.OnNext(Unit.Default);
        }

        // ---------- сценовые метки (собранные предметы и т.п.) ----------

        public static string SceneKey(string scene, string localId) => $"{scene}/{localId}";

        public bool HasScene(string scene, string localId)
            => _set.Contains(SceneKey(scene, localId));

        public void SetScene(string scene, string localId)
        {
            if (_set.Add(SceneKey(scene, localId))) Changed.OnNext(Unit.Default);
        }

        // ---------- прямой доступ по строке (для условий) ----------

        public bool HasRaw(string key) => !string.IsNullOrEmpty(key) && _set.Contains(key);
    }
}
