using Core.Init;
using Core.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Player
{
    /// <summary>
    /// Инициализирует систему взаимодействия через клик мышкой для режима point-and-click.
    /// 
    /// Создаёт/находит MouseInteractor на сцене, биндит ввод (клик мыши) и связывает
    /// с ClickInteractionHUD для отображения подсказок.
    /// 
    /// Используется вместо PlayerInitStep когда игрок не спавнится, а управление идёт
    /// напрямую камерой и мышью.
    /// </summary>
    public class ClickInteractionInitStep : IInitStep
    {
        public int Order => 15;

        private readonly InputAction _clickAction;

        public ClickInteractionInitStep(InputAction clickAction)
        {
            _clickAction = clickAction;
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

            // Биндим клик
            interactor.Bind(_clickAction, ctx.Root);
            Debug.Log("[ClickInteractionInitStep] MouseInteractor привязан к клику");

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
