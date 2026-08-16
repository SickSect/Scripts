using Core.Init;

namespace Core.Mail.UI
{
    /// <summary>
    /// Мостик между DI-контейнером и интерфейсом почты.
    ///
    /// Окно и флажок создаются в рантайме (DesktopService плодит окна из префабов,
    /// иконки живут внутри RenderTexture-канваса), поэтому прокинуть сервис
    /// через инспектор нельзя, а InteractionContext до них не доходит.
    ///
    /// Order 31 — после InventoryInitStep (30), то есть в самом конце,
    /// когда MailService точно зарегистрирован.
    /// </summary>
    public class MailUIBridge : IInitStep
    {
        /// <summary>Сервис для UI-компонентов почты. Null до инициализации сцены.</summary>
        public static MailService Service { get; private set; }

        public int Order => 31;

        public void Execute(InitContext ctx)
        {
            Service = ctx.Root.TryResolve<MailService>(out var mail) ? mail : null;
        }
    }
}
