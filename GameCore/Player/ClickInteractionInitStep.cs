using Core.Init;
using Core.Interaction;
using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// Инициализирует систему взаимодействия через клик мышкой для режима point-and-click.
    /// 
    /// Создаёт/находит MouseInteractor на сцене, биндит ввод (клик мыши) и связывает
    /// с ClickInteractionHUD для отображения подсказок.
    /// 
    /// Использует новую систему ввода GameInput вместо прямых InputAction.
    /// </summary>
    public class ClickInteractionInitStep : IInitStep
    {
        public int Order => 15;

        private readonly Core.Input.GameInput _gameInput;

        public ClickInteractionInitStep(Core.Input.GameInput gameInput)
        {
            _gameInput = gameInput;
        }

        public void Execute(InitContext ctx)
        {
            // Ищем или создаём MouseInteractor на сцене
            var interactor = Object.FindAnyObjectByType<MouseInteractor>();
            
            if (interactor == null)
            {
                Debug.Log("[ClickInteractionInitStep] MouseInteractor не найден, создаём новый GameObject");
                var go = new GameObject("MouseInteractor");
                interactor = go.AddComponent<MouseInteractor>();
            }

            // Биндим клик через новую систему ввода
            // Используем экшен Interact из карты Player
            var clickAction = _gameInput.Actions.Player.Interact;
            interactor.Bind(clickAction, ctx.Root);
            Debug.Log("[ClickInteractionInitStep] MouseInteractor привязан к клику через GameInput");

            // Связываем с HUD
            var hud = Object.FindAnyObjectByType<ClickInteractionHUD>(FindObjectsInactive.Include);
            if (hud != null)
            {
                hud.SetInteractor(interactor);
                Debug.Log("[ClickInteractionInitStep] ClickInteractionHUD подключён к MouseInteractor");
            }
            else
            {
                Debug.LogWarning("[ClickInteractionInitStep] ClickInteractionHUD не найден в сцене");
            }
        }
    }
}
