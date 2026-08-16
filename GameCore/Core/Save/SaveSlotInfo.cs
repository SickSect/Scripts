namespace Core.Save
{
    /// <summary>
    /// Краткая инфо о сохранении для показа в списке слотов (без применения состояния).
    /// </summary>
    public struct SaveSlotInfo
    {
        public int SlotId;
        public long Timestamp;   // Unix seconds
        public string SceneName;

        public System.DateTime DateTimeLocal =>
            System.DateTimeOffset.FromUnixTimeSeconds(Timestamp).LocalDateTime;
    }
}
