using R3;
using TMPro;
using UnityEngine;

namespace Core.Mail.UI
{
    /// <summary>
    /// Флажок непрочитанного у иконки почты на рабочем столе.
    /// Вешается на объект-иконку, а не на сам флажок: скрипт прячет флажок через
    /// SetActive(false), и на выключенном объекте подписка бы оборвалась.
    ///
    /// Сервис появляется в MailUIBridge на Order 31, а иконка активна с самого
    /// начала сцены — поэтому подписка оформляется не в OnEnable, а по факту
    /// появления сервиса.
    /// </summary>
    public class MailUnreadBadge : MonoBehaviour
    {
        [Tooltip("Что показывать при наличии непрочитанного. Отдельный дочерний объект.")]
        [SerializeField] private GameObject _badgeRoot;

        [Tooltip("Число непрочитанных. Можно оставить пустым, если нужна просто точка.")]
        [SerializeField] private TMP_Text _counter;

        [Tooltip("Прятать счётчик, когда письмо одно.")]
        [SerializeField] private bool _hideCounterWhenSingle = true;

        [SerializeField] private bool _debugLog = false;

        private readonly CompositeDisposable _disposables = new();
        private MailService _mail;

        private void OnEnable()
        {
            Apply(0);
            TryBind();
        }

        private void OnDisable()
        {
            _disposables.Clear();
            _mail = null;
        }

        private void Update()
        {
            if (_mail == null) TryBind();
        }

        private void TryBind()
        {
            var service = MailUIBridge.Service;
            if (service == null) return;

            _mail = service;

            _mail.UnreadCount
                 .Subscribe(Apply)
                 .AddTo(_disposables);

            if (_debugLog)
                Debug.Log($"[MailBadge] подписан, непрочитанных: {_mail.UnreadCount.Value}");
        }

        private void Apply(int unread)
        {
            bool show = unread > 0;

            if (_badgeRoot != null && _badgeRoot != gameObject)
                _badgeRoot.SetActive(show);

            if (_counter == null) return;

            _counter.text = unread.ToString();
            _counter.gameObject.SetActive(show && !(_hideCounterWhenSingle && unread == 1));
        }
    }
}