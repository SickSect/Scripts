using UnityEngine;

namespace Core.DI
{
    /// <summary>
    /// Пустой MonoBehaviour, живущий через DontDestroyOnLoad.
    /// Нужен, чтобы запускать корутины из не-MonoBehaviour кода (GameBootstrap).
    /// </summary>
    public class Coroutines : MonoBehaviour { }
}
