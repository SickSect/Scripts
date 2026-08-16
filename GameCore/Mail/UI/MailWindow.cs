using System.Collections.Generic;
using Core.DI;
using R3;
using TMPro;
using UnityEngine;

namespace Core.Mail.UI
{
    /// <summary>
    /// Окно почты: список слева, текст письма справа.
    ///
    /// Письмо помечается прочитанным в момент открытия конкретного письма,
    /// а не при входе в почту — иначе счётчик непрочитанного врёт.
    ///
    /// Сервис берётся из статической ссылки на root-контейнер, потому что окно
    /// создаётся DesktopService в рантайме и через инспектор его не связать.
    /// </summary>
    public class MailWindow : MonoBehaviour
    {
        [Header("Список")]
        [Tooltip("Родитель строк. Обычно Content внутри ScrollRect.")]
        [SerializeField] private Transform _listRoot;

        [SerializeField] private MailEntryView _entryPrefab;

        [Header("Чтение")]
        [SerializeField] private TMP_Text _readerSender;
        [SerializeField] private TMP_Text _readerSubject;
        [SerializeField] private TMP_Text _readerDate;
        [SerializeField] private TMP_Text _readerBody;

        [Tooltip("Панель чтения. Скрыта, пока письмо не выбрано.")]
        [SerializeField] private GameObject _readerRoot;

        [Tooltip("Заглушка «письмо не выбрано».")]
        [SerializeField] private GameObject _emptyHint;

        private readonly List<MailEntryView> _entries = new();
        private readonly CompositeDisposable _disposables = new();

        private MailService _mail;
        private MailMessage _current;

        private void OnEnable()
        {
            _mail = MailUIBridge.Service;

            if (_mail == null)
            {
                Debug.LogError("[MailWindow] MailService недоступен. Проверь MailUIBridge.");
                return;
            }

            Rebuild();
            ShowReader(null);

            _mail.Changed
                 .Subscribe(_ => Rebuild())
                 .AddTo(_disposables);
        }

        private void OnDisable() => _disposables.Clear();

        private void Rebuild()
        {
            if (_listRoot == null || _entryPrefab == null) return;

            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i] != null) Destroy(_entries[i].gameObject);

            _entries.Clear();

            foreach (var message in _mail.Inbox())
            {
                var entry = Instantiate(_entryPrefab, _listRoot);
                entry.Bind(message, _mail.IsRead(message), Open);
                entry.SetSelected(message == _current);
                _entries.Add(entry);
            }
        }

        /// <summary>Открыть письмо. Здесь и только здесь оно становится прочитанным.</summary>
        public void Open(MailMessage message)
        {
            if (message == null) return;

            _current = message;
            _mail.MarkRead(message);

            ShowReader(message);

            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e == null) continue;

                e.SetSelected(e.Message == message);
                if (e.Message == message) e.SetRead(true);
            }
        }

        private void ShowReader(MailMessage message)
        {
            bool has = message != null;

            if (_readerRoot != null) _readerRoot.SetActive(has);
            if (_emptyHint != null) _emptyHint.SetActive(!has);

            if (!has) return;

            if (_readerSender != null) _readerSender.text = message.sender;
            if (_readerSubject != null) _readerSubject.text = message.subject;
            if (_readerDate != null) _readerDate.text = message.date;
            if (_readerBody != null) _readerBody.text = message.body;
        }
    }
}
