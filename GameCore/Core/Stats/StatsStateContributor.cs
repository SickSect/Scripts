using Core.State;

namespace Core.Stats
{
    public class StatsStateContributor : IStateContributor
    {
        private readonly StatsService _stats;
        public StatsStateContributor(StatsService stats) => _stats = stats;

        public void CaptureInto(GameStateData state)
        {
            state.stats ??= new StatsData();
            _stats.SaveInto(state.stats);
        }
    }
}
