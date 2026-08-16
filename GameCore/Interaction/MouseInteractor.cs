using Core.Common;
using Core.DI;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Interaction
{
    /// <summary>
    /// Обрабатывает взаимодействие через клик мышкой: по нажатию пускает рейкаст
    /// из камеры в точку курсора и, если попал на объект с IInteractable, вызывает Interact().
    ///
    /// Единая точка для всех взаимодействий через клик: двери, предметы и т.д.
    /// Вешается на камеру или отдельный GameObject на сцене.
    /// </summary>
    public class MouseInteractor : MonoBehaviour
    {
        [Header("Настройки рейкаста")]
        [SerializeField] private float _maxDistance = 100f;
        [SerializeField] private LayerMask _interactableMask = ~0;

        [Header("Курсор")]
        [SerializeField] private Texture2D _cursorTexture;
        [SerializeField] private Vector2 _cursorHotspot = Vector2.zero;

        private Camera _camera;
        private InputAction _clickAction;
        private DIContainer _root;
        private GameObject _hoveredObject;
        private IInteractable _hoveredInteractable;

        public ReactiveProperty<GameObject> HoveredObject { get; } = new(null);
        public ReactiveProperty<string> HoveredPrompt { get; } = new(null);

        private void Awake()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogError("[MouseInteractor] Camera.main не найдена!");
            }
        }

        public void Bind(InputAction clickAction, DIContainer root)
        {
            _root = root;
            _clickAction = clickAction;
            _clickAction.performed += OnClick;
            CoreLog.Debug($"[MouseInteractor] привязан к экшену: {clickAction?.name}, enabled={clickAction?.enabled}");
        }

        private void Update()
        {
            UpdateHover();
        }

        private void UpdateHover()
        {
            var ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            
            if (Physics.Raycast(ray, out var hit, _maxDistance, _interactableMask))
            {
                var go = hit.collider.gameObject;
                var interactable = go.GetComponentInParent<IInteractable>();

                if (interactable != null)
                {
                    if (_hoveredObject != go)
                    {
                        _hoveredObject = go;
                        _hoveredInteractable = interactable;
                        HoveredObject.Value = go;
                        HoveredPrompt.Value = interactable.Prompt;
                        CoreLog.Debug($"[MouseInteractor] наведение на {go.name} ({interactable.Prompt})");
                    }
                }
                else
                {
                    ClearHover();
                }
            }
            else
            {
                ClearHover();
            }
        }

        private void ClearHover()
        {
            if (_hoveredObject != null)
            {
                CoreLog.Debug($"[MouseInteractor] уход с {_hoveredObject.name}");
                _hoveredObject = null;
                _hoveredInteractable = null;
                HoveredObject.Value = null;
                HoveredPrompt.Value = null;
            }
        }

        private void OnClick(InputAction.CallbackContext ctx)
        {
            if (_hoveredInteractable == null)
            {
                CoreLog.Debug("[MouseInteractor] клик, но под курсором нет интерактивного объекта");
                return;
            }

            CoreLog.Debug($"[MouseInteractor] взаимодействие с {_hoveredObject.name} ({_hoveredInteractable.Prompt})");
            _hoveredInteractable.Interact(new InteractionContext(gameObject, _root));
        }

        private void OnDestroy()
        {
            if (_clickAction != null) _clickAction.performed -= OnClick;
            HoveredObject.Dispose();
            HoveredPrompt.Dispose();
        }

        private void OnEnable()
        {
            if (_cursorTexture != null)
            {
                Cursor.SetCursor(_cursorTexture, _cursorHotspot, CursorMode.Auto);
            }
        }

        private void OnDisable()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
