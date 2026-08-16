using Core.SceneLoader;
using UnityEngine;

namespace Core.Init
{
    /// <summary>
    /// Ядровой шаг: находит на сцене корень "Spawns" и отдаёт точки спавна в SceneLoader.
    /// Выполняется одним из первых, потому что от спавнов зависят почти все механики,
    /// которые ставят объекты на сцену (игрок, NPC и т.д.).
    /// </summary>
    public class BindSpawnsInitStep : IInitStep
    {
        public int Order => 0;

        public void Execute(InitContext ctx)
        {
            var loader = ctx.Root.Resolve<SceneLoader.SceneLoader>();
            var spawnsRoot = GameObject.Find("Spawns");
            loader.BindSpawnsOnScene(spawnsRoot);
        }
    }
}
