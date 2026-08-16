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

            // Спавним на точке с сохранённым spawnId из состояния.
            var go = loader.SpawnAt(_playerPrefab.gameObject, ctx.State.spawnId);

            var movement = go.GetComponent<PlayerMovement>();
            movement.BindInput(_moveAction);

            // Выносливость: если есть система статов и указан стат — привязываем к движению.
            if (_staminaStat != null && ctx.Root.TryResolve<Core.Stats.StatsService>(out var stats))
                movement.BindStamina(stats.Get(_staminaStat));

            // Взгляд мышью (если компонент есть на префабе).
            var look = go.GetComponent<PlayerLook>();
            if (look != null) look.BindInput(_lookAction);

            // Взаимодействие через рейкаст (двери, предметы).
            var interactor = go.GetComponent<Core.Interaction.PlayerInteractor>();
            if (interactor != null) interactor.Bind(_interactAction, ctx.Root);

            stateService.RegisterContributor(new PlayerStateContributor(movement));

            ctx.Scene.RegisterInstance(movement);
            if (look != null) ctx.Scene.RegisterInstance(look);

            // HUD взаимодействия: связываем с LookTarget игрока прямо здесь
            // (как было раньше — без отдельного init-шага).
            var lookTarget = go.GetComponentInChildren<Core.Player.LookTarget>();
            if (lookTarget != null)
            {
                var hud = UnityEngine.Object.FindAnyObjectByType<Core.UI.HUD.InteractionHUD>(FindObjectsInactive.Include);
                if (hud != null) hud.SetTarget(lookTarget);
                else Debug.LogWarning("[PlayerInitStep] InteractionHUD в сцене не найден.");
            }
            else
            {
                Debug.LogWarning("[PlayerInitStep] LookTarget на игроке не найден — хинт HUD работать не будет.");
            }
        }
    }
}