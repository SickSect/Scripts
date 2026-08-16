using Core.Init;
using Core.State;
using R3;
using UnityEngine;

namespace Core.Boot
{
    /// <summary>
    /// Общий контракт bootstrap-а сцены: получает InitContext, инициализирует сцену
    /// и возвращает поток сигналов перехода наружу (в GameBootstrap).
    /// </summary>
    public abstract class SceneBootstrapBase : MonoBehaviour
    {
        public abstract Observable<SceneTransitionParameters> Initialize(InitContext ctx);
    }
}
