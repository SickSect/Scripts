using Core.Common;
using UnityEngine;

namespace Core.Story.Phases
{
    /// <summary>
    /// Доставка письма при активации фазы. Якорь не используется —
    /// письмо попадает в ящик игрока, а не в точку сцены.
    ///
    /// Повторная доставка отсекается самим MailService: письмо с уже полученным
    /// id не добавится второй раз, поэтому переход между сценами ящик не засоряет.
    /// </summary>
    [CreateAssetMenu(fileName = "MailSpawn", menuName = "Core/Story/Phase Spawn/Mail")]
    public class MailPhaseSpawn : PhaseSpawn
    {
        [SerializeField] private Core.Mail.MailMessage _message;

        public override GameObject Spawn(PhaseSpawnContext context)
        {
            if (_message == null)
            {
                Debug.LogError($"[MailPhaseSpawn] '{name}': поле Message не заполнено.");
                return null;
            }

            if (string.IsNullOrEmpty(_message.id))
            {
                Debug.LogError($"[MailPhaseSpawn] '{name}': у письма '{_message.name}' пустой id.");
                return null;
            }

            if (!context.Root.TryResolve<Core.Mail.MailService>(out var mail))
            {
                Debug.LogError($"[MailPhaseSpawn] '{name}': MailService не зарегистрирован.");
                return null;
            }

            mail.Receive(_message);
            return null;
        }

        /// <summary>
        /// Письмо уже в ящике — доставлять нечего.
        ///
        /// Незаполненное поле НЕ считается доставленным: иначе ошибка настройки
        /// выглядит как штатный пропуск и не видна в консоли.
        /// </summary>
        public override bool IsConsumed(PhaseSpawnContext context)
        {
            if (_message == null || string.IsNullOrEmpty(_message.id)) return false;
            if (!context.Root.TryResolve<Core.Mail.MailService>(out var mail)) return false;

            return mail.Data.received.Contains(_message.id);
        }
    }
}