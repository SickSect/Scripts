using Core.Init;
using Core.State;

namespace Core.Story.Phases
{
    /// <summary>
    /// Загружает состояние фаз из снапшота, активирует текущую фазу на сцене (спавн наполнения),
    /// регистрирует контрибьютора сохранения.
    ///
    /// Order 9 — после флагов/статов/событий (фаза спавнит предметы/события, использует метки).
    /// </summary>
    public class PhaseInitStep : IInitStep
    {
        public int Order => 9;

        public void Execute(InitContext ctx)
        {
            if (!ctx.Root.TryResolve<PhaseService>(out var phases)) return;

            ctx.State.phase ??= new PhaseData();
            phases.LoadFrom(ctx.State.phase);

            // Активировать текущую фазу на этой сцене (спавн предметов/событий в якоря).
            phases.ActivateCurrentOnScene();

            var stateService = ctx.Root.Resolve<GameStateService>();
            stateService.RegisterContributor(new PhaseStateContributor(phases));
        }
    }
}
