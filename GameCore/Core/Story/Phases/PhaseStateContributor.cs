using Core.State;

namespace Core.Story.Phases
{
    public class PhaseStateContributor : IStateContributor
    {
        private readonly PhaseService _phases;
        public PhaseStateContributor(PhaseService phases) => _phases = phases;

        public void CaptureInto(GameStateData state)
        {
            state.phase ??= new PhaseData();
            _phases.SaveInto(state.phase);
        }
    }
}
