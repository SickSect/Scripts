using Core.Init;
using Core.State;
using UnityEngine;

namespace Core.Stats
{
    /// <summary>
    /// Загружает статы из снапшота, создаёт тикер (авто-изменение) и регистрирует
    /// контрибьютора сохранения. Order 7 — после флагов (onZero выдаёт триггеры).
    /// </summary>
    public class StatsInitStep : IInitStep
    {
        public int Order => 7;

        private static StatsTicker _ticker; // один на игру

        public void Execute(InitContext ctx)
        {
            if (!ctx.Root.TryResolve<StatsService>(out var stats)) return;

            ctx.State.stats ??= new StatsData();
            // Если снапшот пуст (новая игра) — статы уже на startValue из конструктора сервиса.
            if (ctx.State.stats.stats.Count > 0)
                stats.LoadFrom(ctx.State.stats);

            // Тикер создаём один раз, глобально.
            if (_ticker == null)
            {
                var go = new GameObject("[STATS_TICKER]");
                Object.DontDestroyOnLoad(go);
                _ticker = go.AddComponent<StatsTicker>();
                _ticker.Init(stats);
            }

            var stateService = ctx.Root.Resolve<GameStateService>();
            stateService.RegisterContributor(new StatsStateContributor(stats));
        }
    }
}
