using UnityEngine.InputSystem;

namespace Core.Input
{
    /// <summary>
    /// Обёртка над сгенерённым MainInputSystem. Держит инстанс ввода и переключает
    /// схемы: Player (геймплей) ↔ UI (меню/пауза). Регистрируется в root и живёт весь рантайм.
    ///
    /// Правило: обычный геймплей — схема Player; вошли в паузу/меню — схема UI.
    /// </summary>
    public class GameInput
    {
        public MainInputSystem Actions { get; }

        public enum Scheme { Player, UI }
        public Scheme Current { get; private set; }

        public GameInput()
        {
            Actions = new MainInputSystem();
            SwitchToPlayer();

            // Экшен паузы держим включённым всегда, независимо от схемы Player/UI,
            // чтобы одна клавиша и открывала, и закрывала паузу.
            PauseAction.Enable();
            InventoryAction.Enable();
        }

        /// <summary>
        /// Экшен паузы (всегда активен). Ожидается, что в карте Player есть экшен "Pause".
        /// Если назвал иначе — поменяй здесь.
        /// </summary>
        public InputAction PauseAction => Actions.Player.Pause;

        /// <summary>Экшен открытия инвентаря (всегда активен). Карта Player, экшен "Inventory".</summary>
        public InputAction InventoryAction => Actions.Player.Inventory;

        // --- Экшены навигации внутри инвентаря (карта UI) ---
        public InputAction UiNavigate => Actions.UI.Navigate;
        public InputAction UiUse => Actions.UI.Use;
        public InputAction UiDrop => Actions.UI.Drop;

        public void SwitchToPlayer()
        {
            Actions.UI.Disable();
            Actions.Player.Enable();
            Current = Scheme.Player;
            PauseAction.Enable(); // пауза остаётся активной всегда
            InventoryAction.Enable();
        }

        public void SwitchToUI()
        {
            Actions.Player.Disable();
            Actions.UI.Enable();
            Current = Scheme.UI;
            PauseAction.Enable(); // пауза остаётся активной всегда
            InventoryAction.Enable();
        }

        public void Dispose()
        {
            Actions.Disable();
            Actions.Dispose();
        }
    }
}
