using Core.Init;
using Core.State;

namespace Core.Mail
{
    /// <summary>
    /// Загружает состояние почты из снапшота и регистрирует контрибьютора сохранения.
    ///
    /// Order 8 — до PhaseInitStep (9), потому что фаза при активации может
    /// доставить письмо, и ящик к этому моменту должен быть загружен.
    /// </summary>
    public class MailInitStep : IInitStep
    {
        public int Order => 8;

        public void Execute(InitContext ctx)
        {
            if (!ctx.Root.TryResolve<MailService>(out var mail)) return;

            ctx.State.mail ??= new MailData();
            mail.LoadFrom(ctx.State.mail);

            var stateService = ctx.Root.Resolve<GameStateService>();
            stateService.RegisterContributor(new MailStateContributor(mail));
        }
    }
}
