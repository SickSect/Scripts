using UnityEngine;

namespace Core.Mail
{
    /// <summary>
    /// Письмо (ScriptableObject). Один ассет на одно письмо.
    ///
    /// Само письмо — это только текст. Прочитано оно или нет, лежит не здесь,
    /// а в состоянии игрока (MailData): один и тот же ассет в разных сохранениях
    /// может быть и прочитан, и нет.
    /// </summary>
    [CreateAssetMenu(fileName = "MAIL_", menuName = "Core/Computer/Mail Message")]
    public class MailMessage : ScriptableObject
    {
        [Header("Идентификация")]
        [Tooltip("Уникальный стабильный id. Не менять после того, как письмо ушло в сохранения.")]
        public string id;

        [Header("Заголовок")]
        public string sender = "Отправитель";
        public string subject = "Без темы";

        [Tooltip("Дата в том виде, в каком её увидит игрок. Свободная строка.")]
        public string date = "";

        [Header("Текст")]
        [TextArea(6, 30)]
        public string body;

        [Header("Пометки")]
        [Tooltip("Показывать как важное — для писем от начальства и тревожных.")]
        public bool important = false;
    }
}
