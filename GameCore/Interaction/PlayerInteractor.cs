using Core.Common;
using Core.DI;
using Core.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Interaction
{
    /// <summary>
    /// Обрабатывает взаимодействие: по нажатию Interact берёт объект под прицелом
    /// (из LookTarget) и, если на нём есть IInteractable, вызывает Interact().
    ///
    /// Единая точка для всех взаимодействий через рейкаст: двери, предметы и т.д.
    /// Вешается на игрока (рядом с LookTarget).
    /// </summary>
    [RequireComponent(typeof(LookTarget))]
    public class PlayerInteractor : MonoBehaviour
    {
        private LookTarget _lookTarget;
        private InputAction _interactAction;
        private DIContainer _root;

        private void Awake() => _lookTarget = GetComponent<LookTarget>();

        public void Bind(InputAction interactAction, DIContainer root)
        {
            _root = root;
            _interactAction = interactAction;
            _interactAction.performed += OnInteract;
            CoreLog.Debug($"[Interactor] привязан к экшену: {interactAction?.name}, enabled={interactAction?.enabled}");
        }

        private void OnInteract(InputAction.CallbackContext ctx)
        {

            UnityEngine.Debug.Log("[Interact] OnInteract вызван!");
            var target = _lookTarget.Target.Value;
            if (target == null)
            {
                CoreLog.Debug("[Interact] нажато, но под прицелом ничего нет");
                return;
            }

            // Ищем IInteractable на объекте или его родителях.
            var interactable = target.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                CoreLog.Debug($"[Interact] на объекте {target.name} нет IInteractable");
                return;
            }

            CoreLog.Debug($"[Interact] взаимодействие с {target.name} ({interactable.Prompt})");
            interactable.Interact(new InteractionContext(gameObject, _root));
        }

        private void OnDestroy()
        {
            if (_interactAction != null) _interactAction.performed -= OnInteract;
        }
    }
}
