using Core.Inventory;

namespace Core.Player
{
    /// <summary>
    /// Мостик к сервисам для системы изучения. InventoryService создаётся в DI —
    /// InventoryInitStep кладёт его сюда одной строкой, чтобы контроллер мог проверять
    /// наличие предметов, не завися от DI напрямую. Пусто = проверок по предметам нет.
    /// </summary>
    public static class ExamineServices
    {
        public static InventoryService Inventory;
    }
}