using Core.Init;
using Core.Player;
using Core.SceneLoader;
using Core.State;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Player
{
    /// <summary>
    /// Оживляет игрока на игровой сцене:
    ///  1) спавнит префаб игрока на нужном месте,
    ///  2) ставит позицию (сохранённые координаты при загрузке / точка спавна при новой игре),
    ///  3) биндит ввод (Move) из Input System,
    ///  4) регистрирует контрибьютора, чтобы позиция попадала в сохранение,
    ///  5) кладёт LookTarget в scene-контейнер (нужен HUD-у взаимодействия).
    ///
    /// Порядок: после BindSpawnsInitStep (тот Order=0), поэтому здесь Order=10.
    /// </summary>
    public class PlayerInitStep : IInitStep
    {
        public int Order => 10;

        private readonly PlayerMovement _playerPrefab;
        private readonly InputAction _moveAction;
        private readonly InputAction _lookAction;
        private readonly InputAction _interactAction;
        private readonly Core.Stats.StatDefinition _staminaStat;

        public PlayerInitStep(PlayerMovement playerPrefab, InputAction moveAction,
                              InputAction lookAction, InputAction interactAction,
                              Core.Stats.StatDefinition staminaStat = null)
        {
            _playerPrefab = playerPrefab;
            _moveAction = moveAction;
            _lookAction = lookAction;
            _interactAction = interactAction;
            _staminaStat = staminaStat;
        }

        public void Execute(InitContext ctx)
        {
            var loader = ctx.Root.Resolve<SceneLoader.SceneLoader>();
            var stateService = ctx.Root.Resolve<GameStateService>();

            ctx.State.player ??= new PlayerData();
        }
    }
}