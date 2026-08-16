using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Core.Mail
{
    /// <summary>
    /// Почтовый ящик игрока. Живёт в root и переживает смену сцен.
    ///
    /// Фазы и события только доставляют письмо (Receive). Прочитанным оно
    /// становится в момент, когда игрок его действительно открыл (MarkRead) —
    /// поэтому счётчик непрочитанного честный, а не «обнулился при входе в почту».
    /// </summary>
    public class MailService
    {
        private readonly Dictionary<string, MailMessage> _catalog = new();

        public MailData Data { get; private set; } = new();

        /// <summary>Число непрочитанных. На него подписан флажок у иконки почты.</summary>
        public ReactiveProperty<int> UnreadCount { get; } = new(0);

        /// <summary>Пришло новое письмо. UI перестраивает список.</summary>
        public Subject<MailMessage> Received { get; } = new();

        /// <summary>Состав ящика изменился: пришло, прочитано, загружено.</summary>
        public Subject<Unit> Changed { get; } = new();

        public MailService(IEnumerable<MailMessage> catalog)
        {
            if (catalog == null) return;

            foreach (var m in catalog)
            {
                if (m == null || string.IsNullOrEmpty(m.id)) continue;

                if (_catalog.ContainsKey(m.id))
                {
                    Debug.LogWarning($"[Mail] дубликат id '{m.id}' в каталоге писем.");
                    continue;
                }

                _catalog[m.id] = m;
            }
        }

        public void LoadFrom(MailData data)
        {
            Data = data?.Clone() ?? new MailData();
            Recount();
            Changed.OnNext(Unit.Default);
        }

        public void SaveInto(MailData data)
        {
            data.received = new List<string>(Data.received);
            data.read = new List<string>(Data.read);
        }

        /// <summary>Письмо по id. Null, если такого нет в каталоге.</summary>
        public MailMessage Find(string id)
            => !string.IsNullOrEmpty(id) && _catalog.TryGetValue(id, out var m) ? m : null;

        /// <summary>Все дошедшие письма, новые первыми.</summary>
        public IEnumerable<MailMessage> Inbox()
        {
            for (int i = Data.received.Count - 1; i >= 0; i--)
            {
                var m = Find(Data.received[i]);
                if (m != null) yield return m;
            }
        }

        public bool IsRead(string id) => Data.read.Contains(id);
        public bool IsRead(MailMessage m) => m != null && IsRead(m.id);

        /// <summary>Доставить письмо. Повторная доставка игнорируется.</summary>
        public bool Receive(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            if (!_catalog.ContainsKey(id))
            {
                Debug.LogWarning($"[Mail] письмо '{id}' не найдено в каталоге — доставка пропущена.");
                return false;
            }

            if (Data.received.Contains(id)) return false;

            Data.received.Add(id);
            Recount();

            var msg = _catalog[id];
            Received.OnNext(msg);
            Changed.OnNext(Unit.Default);

            Debug.Log($"[Mail] получено письмо '{msg.subject}'. Непрочитанных: {UnreadCount.Value}");
            return true;
        }

        public bool Receive(MailMessage message) => message != null && Receive(message.id);

        /// <summary>Пометить прочитанным. Вызывается, когда игрок открыл письмо.</summary>
        public bool MarkRead(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (!Data.received.Contains(id)) return false;
            if (Data.read.Contains(id)) return false;

            Data.read.Add(id);
            Recount();
            Changed.OnNext(Unit.Default);

            Debug.Log($"[Mail] прочитано '{id}'. Непрочитанных: {UnreadCount.Value}");
            return true;
        }

        public bool MarkRead(MailMessage m) => m != null && MarkRead(m.id);

        private void Recount()
        {
            int unread = 0;

            for (int i = 0; i < Data.received.Count; i++)
                if (!Data.read.Contains(Data.received[i])) unread++;

            UnreadCount.Value = unread;
        }
    }
}
