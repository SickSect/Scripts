using System.Collections.Generic;
using Core.Flags;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Carry
{
    /// <summary>
    /// Рабочая зона. Ловит отпущенный предмет своим триггером и ставит его ровно
    /// в центр — целиться не нужно, достаточно задеть.
    ///
    /// Поймав предмет, выкладывает его содержимое: посылка раскрывается уликой,
    /// документом, ключевым текстом. Забрал коробку обратно — содержимое исчезает.
    ///
    /// Нужен коллайдер с включённым Is Trigger. Размер триггера — это и есть
    /// «зона притяжения», делай его щедрым.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DropZone : MonoBehaviour
    {
        [Header("Что принимает")]
        [Tooltip("Ключ. Должен совпасть с zoneKey у Carryable.")]
        [SerializeField] private string _acceptKey = "workdesk";

        [Header("Куда ставить")]
        [Tooltip("Точка, куда встанет предмет. Пусто — этот объект.")]
        [SerializeField] private Transform _snapAnchor;

        [Tooltip("Точки, куда лягут предметы из посылки: первый в первую, второй во вторую. " +
                 "Расставь их на столе как хочешь — наклон и поворот тоже берутся отсюда. " +
                 "Пусто — всё ляжет в точку предмета.")]
        [SerializeField] private Transform[] _contentAnchors;

        [Tooltip("Разброс для предметов, которым не хватило якоря, в метрах.")]
        [SerializeField] private float _overflowSpread = 0.12f;

        [Header("Подсветка")]
        [Tooltip("Включается, пока игрок держит подходящий предмет.")]
        [SerializeField] private GameObject _highlight;

        [Header("Метка")]
        [Tooltip("Ставится при первом приёме. Через неё реагируют фазы и условия.")]
        [SerializeField] private TriggerDefinition _acceptedTrigger;

        [Header("События")]
        public UnityEvent onAccepted;
        public UnityEvent onVacated;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private readonly List<GameObject> _spawned = new();
        private static readonly Collider[] _hits = new Collider[16];

        private PlayerCarry _carry;
        private FlagService _flags;

        public string AcceptKey => _acceptKey;
        public bool IsOccupied { get; private set; }

        /// <summary>Куда встаёт предмет.</summary>
        public Transform SnapAnchor => _snapAnchor != null ? _snapAnchor : transform;

        private void Awake()
        {
            if (_highlight != null) _highlight.SetActive(false);
        }

        /// <summary>Прокидывается один раз, чтобы зона могла ставить метку.</summary>
        public void BindFlags(FlagService flags) => _flags = flags;

        private void Update()
        {
            if (_highlight == null) return;

            if (_carry == null) _carry = FindAnyObjectByType<PlayerCarry>();

            bool ready = !IsOccupied
                         && _carry != null
                         && _carry.IsCarrying
                         && _carry.Current.ZoneKey == _acceptKey;

            if (_highlight.activeSelf != ready) _highlight.SetActive(ready);
        }

        // Ловим предмет, который в зону влетел (бросок с расстояния).
        private void OnTriggerEnter(Collider other)
        {
            var item = other.GetComponentInParent<Carryable>();
            if (item != null) TryAccept(item);
        }

        public bool CanAccept(Carryable item)
            => !IsOccupied
               && item != null
               && !item.IsHeld
               && item.CurrentZone == null
               && !string.IsNullOrEmpty(_acceptKey)
               && item.ZoneKey == _acceptKey;

        /// <summary>Принять предмет, если он подходит. Возвращает, случилось ли.</summary>
        public bool TryAccept(Carryable item)
        {
            if (!CanAccept(item)) return false;

            item.OnSnapped(this, SnapAnchor);

            var prefabs = item.ContentsPrefabs;

            if (prefabs != null)
            {
                for (int i = 0; i < prefabs.Length; i++)
                {
                    if (prefabs[i] == null) continue;
                    _spawned.Add(SpawnContent(prefabs[i], i));
                }
            }

            IsOccupied = true;
            if (_highlight != null) _highlight.SetActive(false);

            if (_acceptedTrigger != null && _flags != null) _flags.Set(_acceptedTrigger);

            onAccepted?.Invoke();

            if (_debugLog)
                Debug.Log($"[DropZone] '{name}' принял '{item.name}', выложено {_spawned.Count}");

            return true;
        }

        /// <summary>
        /// Разложить один предмет содержимого. Якоря разбираются по порядку;
        /// когда они кончаются, остаток раскладывается по кругу вокруг последнего,
        /// чтобы предметы не слипались в одной точке.
        /// </summary>
        private GameObject SpawnContent(GameObject prefab, int index)
        {
            bool hasAnchors = _contentAnchors != null && _contentAnchors.Length > 0;

            if (!hasAnchors)
                return Instantiate(prefab, SnapAnchor.position, SnapAnchor.rotation, SnapAnchor);

            if (index < _contentAnchors.Length && _contentAnchors[index] != null)
            {
                var a = _contentAnchors[index];
                return Instantiate(prefab, a.position, a.rotation, a);
            }

            // Якорей не хватило — раскладываем по кругу вокруг последнего.
            Transform last = _contentAnchors[^1] != null ? _contentAnchors[^1] : SnapAnchor;

            int overflow = index - _contentAnchors.Length + 1;
            float angle = overflow * 137.5f * Mathf.Deg2Rad;   // золотой угол: точки не совпадают
            Vector3 offset = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            Debug.LogWarning($"[DropZone] '{name}': якорей меньше, чем предметов " +
                             $"({_contentAnchors.Length}). '{prefab.name}' положен рядом.");

            return Instantiate(prefab, last.position + offset * _overflowSpread, last.rotation, last);
        }

        /// <summary>Предмет забрали обратно: содержимое уезжает вместе с ним.</summary>
        public void Vacate()
        {
            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i] != null) Destroy(_spawned[i]);

            _spawned.Clear();
            IsOccupied = false;

            onVacated?.Invoke();

            if (_debugLog) Debug.Log($"[DropZone] '{name}' освобождена");
        }

        /// <summary>
        /// Поиск зоны вокруг только что отпущенного предмета.
        /// Нужен потому, что OnTriggerEnter не сработает, если предмет
        /// уже находился внутри триггера в момент отпускания.
        /// </summary>
        public static bool TryCaptureAt(Carryable item)
        {
            if (item == null) return false;

            int count = Physics.OverlapSphereNonAlloc(
                item.transform.position,
                item.ApproxRadius + 0.15f,
                _hits,
                ~0,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                var zone = _hits[i].GetComponentInParent<DropZone>();
                if (zone != null && zone.TryAccept(item)) return true;
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied ? Color.gray : new Color(0.4f, 1f, 0.5f, 0.9f);
            Gizmos.DrawWireCube(SnapAnchor.position, Vector3.one * 0.2f);

            if (_contentAnchors == null) return;

            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.9f);

            for (int i = 0; i < _contentAnchors.Length; i++)
            {
                if (_contentAnchors[i] == null) continue;

                Gizmos.DrawWireSphere(_contentAnchors[i].position, 0.05f);
                Gizmos.DrawRay(_contentAnchors[i].position, _contentAnchors[i].up * 0.1f);
            }
        }
#endif
    }
}