using System;
using Core.Player;

namespace Core.State
{
    /// <summary>
    /// Полный снапшот состояния игры. Это ровно то, что сериализуется в файл сохранения
    /// и восстанавливается при загрузке. Механики (инвентарь, время, квесты...) добавляют
    /// сюда свои [Serializable]-поля-контейнеры и обслуживают их клонирование в Clone().
    ///
    /// Правила:
    ///  - все поля публичные и [Serializable] (для JsonUtility);
    ///  - никаких ссылок на MonoBehaviour / сцену / рантайм-объекты;
    ///  - Clone() обязан делать ГЛУБОКУЮ копию, иначе runtime-снапшот будет мутировать origin.
    /// </summary>
    [Serializable]
    public class GameStateData
    {
        // --- SYSTEM ---
        public int currentSaveSlotId;   // в какой слот сохранён этот стейт
        public long timestamp;          // время последнего сохранения (Unix seconds)
        public string sceneName;        // на какой сцене находится игрок
        public int spawnId;             // на какой точке спавна появиться

        // --- MECHANICS DATA (точки расширения) ---
        public PlayerData player = new();
        public Core.Inventory.InventoryData inventory = new();
        public Core.Flags.FlagStore flags = new();
        public Core.Stats.StatsData stats = new();
        public Core.Story.Phases.PhaseData phase = new();
        public Core.Mail.MailData mail = new();
        //   public GameTimeData time = new();
        //   public QuestData quests = new();
        // и не забудь скопировать их в Clone().

        /// <summary>Базовое состояние для новой игры.</summary>
        public static GameStateData CreateDefault(int slotId, string firstGameSceneName, int firstSpawnId)
        {
            return new GameStateData
            {
                currentSaveSlotId = slotId,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                sceneName = firstGameSceneName,
                spawnId   = firstSpawnId,
                // Позиция игрока при новой игре не задана — PlayerInitStep поставит его
                // на точку спавна (firstSpawnId), а не по сохранённым координатам.
                player = new PlayerData()
                // time   = new GameTimeData { day = 1, hour = 6, minute = 0 },
            };
        }

        /// <summary>Глубокая копия снапшота.</summary>
        public GameStateData Clone()
        {
            return new GameStateData
            {
                currentSaveSlotId = currentSaveSlotId,
                timestamp = timestamp,
                sceneName = sceneName,
                spawnId   = spawnId,
                player = player?.Clone(),
                inventory = inventory?.Clone(),
                flags = flags?.Clone(),
                stats = stats?.Clone(),
                phase = phase?.Clone(),
                mail = mail?.Clone()
                // time   = time?.Clone(),
                // quests = quests?.Clone(),
            };
        }
    }
}
