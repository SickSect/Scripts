using Core.State;

namespace Core.Player
{
    /// <summary>
    /// Записывает актуальную позицию игрока в снапшот при сохранении.
    /// Читает последнее значение ReactiveProperty движения — без подписки на каждый кадр.
    /// Это эталонный способ подключать любую механику к сохранению.
    /// </summary>
    public class PlayerStateContributor : IStateContributor
    {
        private readonly PlayerMovement _movement;

        public PlayerStateContributor(PlayerMovement movement) => _movement = movement;

        public void CaptureInto(GameStateData state)
        {
            state.player ??= new PlayerData();
            state.player.Position = _movement.Position.Value;
        }
    }
}
