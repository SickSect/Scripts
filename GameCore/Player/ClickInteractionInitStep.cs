using UnityEngine;
using Core.Init;
using Core.DI;
using Core.Player;
using Core.Interaction;
using Core.UI.HUD;
using Core.Input; // Пространство имен твоего GameInput

namespace Core.Player
{
    /// <summary>
    /// Инициализирует систему Point-and-Click взаимодействия.
    /// Получает GameInput из DI-контейнера, включает схему Player и настраивает камеру/интерактор.
    /// </summary>
    public class ClickInteractionInitStep : IInitStep
    {
        public int Order => 15;

        public void Execute(InitContext context)
        {
            var container = context.Root; // Используем Root контейнер для глобальных сервисов
            var sceneContainer = context.Scene;

            // 1. Получаем GameInput из DI
            // Убедись, что GameInput зарегистрирован в корневом контейнере при старте приложения!
            GameInput gameInput;
            try
            {
                gameInput = container.Resolve<GameInput>();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[ClickInteractionInitStep] НЕ УДАЛОСЬ получить GameInput из DI контейнера!");
                Debug.LogError("Убедитесь, что в GameplayBootstrap (или MainMenuBootstrap) вы сделали: container.RegisterSingleton<GameInput>(new GameInput());");
                Debug.LogException(e);
                return;
            }

            gameInput.SwitchToPlayer();

            // 3. Находим основную камеру
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                // Попытка найти любую камеру с тегом MainCamera, если Camera.main null
                mainCam = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
            }

            if (mainCam == null)
            {
                Debug.LogError("[ClickInteractionInitStep] Камера не найдена! Невозможно инициализировать управление.");
                return;
            }

            // 4. Настраиваем камеру (PointAndClickCamera)
            var cameraController = mainCam.GetComponent<PointAndClickCamera>();
            if (cameraController == null)
            {
                cameraController = mainCam.gameObject.AddComponent<PointAndClickCamera>();
                Debug.Log("[ClickInteractionInitStep] Добавлен компонент PointAndClickCamera.");
            }

            // Привязываем действие движения (WASD)
            var moveAction = gameInput.Actions.Player.Move;
            if (moveAction != null)
            {
                cameraController.BindInput(moveAction);
                Debug.Log($"[ClickInteractionInitStep] Камера привязана к действию Move.");
            }
            else
            {
                Debug.LogWarning("[ClickInteractionInitStep] Действие 'Player/Move' не найдено в Input Asset!");
            }

            // 5. Создаем и настраиваем MouseInteractor
            GameObject interactorObj = new GameObject("MouseInteractor");
            interactorObj.transform.SetParent(mainCam.transform);
            interactorObj.transform.localPosition = Vector3.zero;

            var interactor = interactorObj.AddComponent<MouseInteractor>();

            // Привязываем действие взаимодействия (ЛКМ)
            var interactAction = gameInput.Actions.Player.Interact;
            if (interactAction != null)
            {
                interactor.Bind(interactAction, container);
                Debug.Log($"[ClickInteractionInitStep] MouseInteractor привязан к действию Interact.");
            }
            else
            {
                Debug.LogError("[ClickInteractionInitStep] Действие 'Player/Interact' не найдено! Проверьте .inputactions файл.");
            }

            // 6. Подключаем HUD
            var hud = Object.FindObjectOfType<ClickInteractionHUD>();
            if (hud != null)
            {
                hud.SetInteractor(interactor);
                Debug.Log("[ClickInteractionInitStep] HUD успешно подключен.");
            }
            else
            {
                Debug.LogWarning("[ClickInteractionInitStep] ClickInteractionHUD не найден в сцене.");
            }
        }
    }
}