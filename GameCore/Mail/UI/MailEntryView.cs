using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Mail.UI
{
    /// <summary>
    /// Строка письма в списке. Префаб, который MailWindow плодит по числу писем.
    ///
    /// Ничего не знает про MailService — получает данные и колбэк снаружи.
    /// </summary>
    public class MailEntryView : MonoBehaviour, IPointerClickHandler
    {
        [Header("Тексты")]
        [SerializeField] private TMP_Text _sender;
        [SerializeField] private TMP_Text _subject;
        [SerializeField] private TMP_Text _date;

        [Header("Состояние")]
        [Tooltip("Точка рядом со строкой: письмо не прочитано.")]
        [SerializeField] private GameObject _unreadMark;

        [Tooltip("Подсветка выбранной строки.")]
        [SerializeField] private GameObject _selectedMark;

        [Tooltip("Значок важного письма.")]
        [SerializeField] private GameObject _importantMark;

        [Header("Цвета темы")]
        [SerializeField] private Color _unreadColor = Color.white;
        [SerializeField] private Color _readColor = new(0.65f, 0.65f, 0.65f, 1f);

        private MailMessage _message;
        private Action<MailMessage> _onClick;

        public MailMessage Message => _message;

        public void Bind(MailMessage message, bool isRead, Action<MailMessage> onClick)
        {
            _message = message;
            _onClick = onClick;

            if (_sender != null) _sender.text = message.sender;
            if (_subject != null) _subject.text = message.subject;
            if (_date != null) _date.text = message.date;

            if (_importantMark != null) _importantMark.SetActive(message.important);

            SetRead(isRead);
            SetSelected(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[MailEntry] клик по '{_message?.subject}'");
            _onClick?.Invoke(_message);
        }

        public void SetRead(bool isRead)
        {
            if (_unreadMark != null) _unreadMark.SetActive(!isRead);

            Color c = isRead ? _readColor : _unreadColor;

            if (_sender != null) _sender.color = c;
            if (_subject != null) _subject.color = c;
            if (_date != null) _date.color = c;
        }

        public void SetSelected(bool selected)
        {
            if (_selectedMark != null) _selectedMark.SetActive(selected);
        }

        //public void OnPointerClick(PointerEventData eventData) => _onClick?.Invoke(_message);
    }
}
