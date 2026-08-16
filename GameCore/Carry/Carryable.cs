using Core.Interaction;
using UnityEngine;

namespace Core.Carry
{
    /// <summary>
    /// Предмет, который игрок несёт в руках, а не кладёт в инвентарь.
    ///
    /// Отпускается где угодно. Куда именно он «примагнитится», решает не предмет,
    /// а зона: DropZone принимает только совпадающий zoneKey. Пустой ключ —
    /// предмет не липнет никуда и просто падает.
    ///
    /// Мелочь, которая должна попадать в инвентарь, остаётся на WorldItemPickup —
    /// это независимые механики, не заменяющие друг друга.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Carryable : MonoBehaviour, IInteractable
    {
        [Header("Подсказка")]
        [SerializeField] private string _prompt = "Взять";

        [Header("Зона назначения")]
        [Tooltip("Ключ зоны. Должен совпасть с acceptKey у DropZone. Пусто — не липнет никуда.")]
        [SerializeField] private string _zoneKey = "";

        [Header("Поза в руках")]
        [SerializeField] private Vector3 _holdOffset = Vector3.zero;
        [SerializeField] private Vector3 _holdEuler = Vector3.zero;

        [Header("Содержимое")]
        [Tooltip("Что выкладывается в зоне. Для посылки — улика, документ, ключевой текст. " +
                 "Забрал коробку обратно — содержимое исчезает вместе с ней.")]
        [SerializeField] private GameObject[] _contentsPrefabs;

        private Rigidbody _rb;
        private Collider[] _colliders;
        private bool _wasKinematic;

        public string Prompt => _prompt;
        public string ZoneKey => _zoneKey;
        public Vector3 HoldOffset => _holdOffset;
        public Quaternion HoldRotation => Quaternion.Euler(_holdEuler);
        public GameObject[] ContentsPrefabs => _contentsPrefabs;

        public bool IsHeld { get; private set; }

        /// <summary>Зона, в которой предмет сейчас лежит. Null — на руках или на полу.</summary>
        public DropZone CurrentZone { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _colliders = GetComponentsInChildren<Collider>();
            _wasKinematic = _rb.isKinematic;
        }

        public void Interact(InteractionContext context)
        {
            if (IsHeld || context.Player == null) return;

            var carry = context.Player.GetComponentInChildren<PlayerCarry>(true);

            if (carry == null)
            {
                Debug.LogError("[Carryable] на игроке нет PlayerCarry.");
                return;
            }

            carry.Take(this);
        }

        /// <summary>Взято в руки. Если лежал в зоне — зона убирает выложенное содержимое.</summary>
        public void OnPickedUp(Transform holder)
        {
            if (CurrentZone != null)
            {
                CurrentZone.Vacate();
                CurrentZone = null;
            }

            IsHeld = true;

            // Скорости гасим ДО перевода в kinematic: на кинематическом теле
            // Unity ругается на любую попытку их задать.
            StopMotion();
            _rb.isKinematic = true;

            SetCollidersEnabled(false);
            transform.SetParent(holder, true);
        }

        /// <summary>Отпущено из рук: физика возвращается, предмет летит и падает.</summary>
        public void OnReleased()
        {
            IsHeld = false;
            transform.SetParent(null, true);

            SetCollidersEnabled(true);
            _rb.isKinematic = _wasKinematic;
        }

        /// <summary>Зона поймала предмет: ставим ровно в её якорь и замораживаем.</summary>
        public void OnSnapped(DropZone zone, Transform anchor)
        {
            IsHeld = false;
            CurrentZone = zone;

            StopMotion();
            _rb.isKinematic = true;

            transform.SetParent(anchor, false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            SetCollidersEnabled(true);
        }

        /// <summary>Габарит для поиска зоны при отпускании.</summary>
        public float ApproxRadius
        {
            get
            {
                for (int i = 0; i < _colliders.Length; i++)
                    if (_colliders[i] != null)
                        return _colliders[i].bounds.extents.magnitude;

                return 0.25f;
            }
        }

        /// <summary>Обнулить скорости. Работает только на некинематическом теле.</summary>
        private void StopMotion()
        {
            if (_rb.isKinematic) return;

            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        private void SetCollidersEnabled(bool value)
        {
            for (int i = 0; i < _colliders.Length; i++)
                if (_colliders[i] != null) _colliders[i].enabled = value;
        }
    }
}