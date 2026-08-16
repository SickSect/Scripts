using System.Collections.Generic;
using System.IO;
using Core.State;
using UnityEngine;

namespace Core.Save
{
    /// <summary>
    /// Система сохранения/загрузки. Пишет снапшот GameStateData в JSON по слотам.
    /// Файлы: {persistentDataPath}/GameSaves/slot_{id}.json
    /// </summary>
    public class JsonSaveProvider
    {
        private const string SAVE_FOLDER = "GameSaves";

        private string SaveDir => Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        private string PathForSlot(int slotId) => Path.Combine(SaveDir, $"slot_{slotId}.json");

        public void Save(int slotId, GameStateData state)
        {
            state.currentSaveSlotId = slotId;
            state.timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            Directory.CreateDirectory(SaveDir);
            string json = JsonUtility.ToJson(state, prettyPrint: true);
            File.WriteAllText(PathForSlot(slotId), json);
        }

        public bool HasSave(int slotId) => File.Exists(PathForSlot(slotId));

        public GameStateData Load(int slotId)
        {
            string path = PathForSlot(slotId);
            if (!File.Exists(path)) return null;
            return JsonUtility.FromJson<GameStateData>(File.ReadAllText(path));
        }

        public void Delete(int slotId)
        {
            string path = PathForSlot(slotId);
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>Список id занятых слотов.</summary>
        public int[] GetExistingSlots()
        {
            if (!Directory.Exists(SaveDir)) return System.Array.Empty<int>();

            var slots = new List<int>();
            foreach (var file in Directory.GetFiles(SaveDir, "slot_*.json"))
            {
                string name = Path.GetFileNameWithoutExtension(file); // "slot_3"
                if (name.StartsWith("slot_") && int.TryParse(name.Substring(5), out int id))
                    slots.Add(id);
            }
            return slots.ToArray();
        }

        /// <summary>Краткая инфа по всем сейвам для списка загрузки (отсортировано по времени, свежие сверху).</summary>
        public List<SaveSlotInfo> GetAllSlotInfos()
        {
            var result = new List<SaveSlotInfo>();
            foreach (var id in GetExistingSlots())
            {
                var state = Load(id);
                if (state == null) continue;
                result.Add(new SaveSlotInfo
                {
                    SlotId = id,
                    Timestamp = state.timestamp,
                    SceneName = state.sceneName
                });
            }
            result.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            return result;
        }
    }
}
