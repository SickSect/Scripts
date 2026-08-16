using System;
using System.Collections.Generic;

namespace Core.Mail
{
    /// <summary>
    /// Сериализуемое состояние почты: какие письма дошли и какие из них прочитаны.
    ///
    /// Хранятся только id — сами тексты живут в ассетах MailMessage и в сохранение
    /// не попадают. Правка текста письма не ломает старые сейвы.
    /// </summary>
    [Serializable]
    public class MailData
    {
        /// <summary>Id пришедших писем, в порядке получения.</summary>
        public List<string> received = new();

        /// <summary>Id прочитанных писем.</summary>
        public List<string> read = new();

        public MailData Clone() => new MailData
        {
            received = new List<string>(received),
            read = new List<string>(read)
        };
    }
}
