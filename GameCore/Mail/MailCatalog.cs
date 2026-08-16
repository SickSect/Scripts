using System.Collections.Generic;
using UnityEngine;

namespace Core.Mail
{
    /// <summary>
    /// Реестр всех писем игры. Лежит в Resources/Core/MailCatalog.
    /// Нужен, чтобы по id из сохранения найти текст письма.
    /// </summary>
    [CreateAssetMenu(fileName = "MailCatalog", menuName = "Core/Computer/Mail Catalog")]
    public class MailCatalog : ScriptableObject
    {
        public List<MailMessage> messages = new();
    }
}
