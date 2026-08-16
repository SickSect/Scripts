using System.Collections.Generic;
using UnityEngine;

namespace Core.Computer
{
    /// <summary>
    /// Оболочка рабочего стола: открывает окна приложений, следит за тем,
    /// чтобы одно приложение не плодило копии, и поднимает уже открытое.
    ///
    /// Вешается на объект Desktop — контейнер окон внутри MonitorCanvas.
    /// Окна создаются из префабов приложения, порядок = порядок дочерних объектов.
    /// </summary>
    public class DesktopService : MonoBehaviour
    {
        [Header("Приложения")]
        [Tooltip("Все приложения, доступные на этом компьютере.")]
        [SerializeField] private List<DesktopAppDefinition> _apps = new();

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private readonly Dictionary<string, DesktopWindow> _open = new();
        private RectTransform _desktop;

        private void Awake() => _desktop = (RectTransform)transform;

        /// <summary>Найти описание приложения по id. Null, если такого нет.</summary>
        public DesktopAppDefinition FindApp(string id)
        {
            for (int i = 0; i < _apps.Count; i++)
                if (_apps[i] != null && _apps[i].id == id)
                    return _apps[i];

            return null;
        }

        /// <summary>
        /// Открыть приложение по id. Если окно уже открыто — просто поднимает его наверх.
        /// Возвращает окно или null, если приложение не найдено.
        /// </summary>
        public DesktopWindow OpenApp(string id)
        {
            var app = FindApp(id);

            if (app == null)
            {
                Debug.LogWarning($"[Desktop] приложение '{id}' не зарегистрировано.");
                return null;
            }

            return OpenApp(app);
        }

        /// <summary>Открыть приложение по описанию.</summary>
        public DesktopWindow OpenApp(DesktopAppDefinition app)
        {
            if (app == null) return null;

            if (app.windowPrefab == null)
            {
                Debug.LogError($"[Desktop] у приложения '{app.id}' не задан префаб окна.");
                return null;
            }

            // Уже открыто — поднимаем, а не создаём второе.
            if (!app.allowMultipleWindows &&
                _open.TryGetValue(app.id, out var existing) && existing != null)
            {
                existing.OpenWindow();
                if (_debugLog) Debug.Log($"[Desktop] '{app.id}' уже открыто — поднято наверх.");
                return existing;
            }

            var window = Instantiate(app.windowPrefab, _desktop);
            window.name = $"Window_{app.id}";
            window.Rect.anchoredPosition = app.spawnOffset;

            // Слой канваса монитора: иначе окно не попадёт в RenderTexture.
            SetLayerRecursive(window.gameObject, gameObject.layer);

            window.OpenWindow();

            if (!app.allowMultipleWindows) _open[app.id] = window;

            if (_debugLog) Debug.Log($"[Desktop] открыто '{app.id}'.");
            return window;
        }

        /// <summary>Закрыть окно приложения, если оно открыто.</summary>
        public void CloseApp(string id)
        {
            if (!_open.TryGetValue(id, out var window) || window == null) return;

            window.CloseWindow();
            if (_debugLog) Debug.Log($"[Desktop] закрыто '{id}'.");
        }

        /// <summary>Открыто ли сейчас окно приложения.</summary>
        public bool IsOpen(string id) =>
            _open.TryGetValue(id, out var w) && w != null && w.gameObject.activeSelf;

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;

            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
        }
    }
}
