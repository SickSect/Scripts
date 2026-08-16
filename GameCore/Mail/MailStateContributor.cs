using Core.State;

namespace Core.Mail
{
    public class MailStateContributor : IStateContributor
    {
        private readonly MailService _mail;
        public MailStateContributor(MailService mail) => _mail = mail;

        public void CaptureInto(GameStateData state)
        {
            state.mail ??= new MailData();
            _mail.SaveInto(state.mail);
        }
    }
}
