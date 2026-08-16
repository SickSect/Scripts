using Core.State;

namespace Core.Flags
{
    public class FlagStateContributor : IStateContributor
    {
        private readonly FlagService _flags;
        public FlagStateContributor(FlagService flags) => _flags = flags;

        public void CaptureInto(GameStateData state)
        {
            state.flags ??= new FlagStore();
            _flags.SaveInto(state.flags);
        }
    }
}
