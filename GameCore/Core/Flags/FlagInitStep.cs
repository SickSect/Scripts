using Core.Init;
using Core.State;

namespace Core.Flags
{
    /// <summary>
    /// Загружает метки из снапшота и регистрирует контрибьютора. Order 5 — рано,
    /// до объектов сцены (собранные предметы/условные двери читают метки при своей инициализации).
    /// </summary>
    public class FlagInitStep : IInitStep
    {
        public int Order => 5;

        public void Execute(InitContext ctx)
        {
            if (!ctx.Root.TryResolve<FlagService>(out var flags)) return;

            ctx.State.flags ??= new FlagStore();
            flags.LoadFrom(ctx.State.flags);

            var stateService = ctx.Root.Resolve<GameStateService>();
            stateService.RegisterContributor(new FlagStateContributor(flags));
        }
    }
}
