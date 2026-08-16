using Core.UI;
using UnityEngine;

namespace Core.Init
{
    /// <summary>
    /// Шаг инициализации меню: находит MainMenuController на сцене и связывает его
    /// с root-контейнером (сигналы + провайдер сейвов). После этого меню строит кнопки.
    /// </summary>
    public class MainMenuInitStep : IInitStep
    {
        public int Order => 0;

        public void Execute(InitContext ctx)
        {
            var controller = Object.FindAnyObjectByType<MainMenuController>();
            if (controller == null)
            {
                Debug.LogError("[MainMenuInitStep] MainMenuController не найден на сцене меню!");
                return;
            }
            controller.Bind(ctx.Root);
        }
    }
}
