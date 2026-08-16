using Core.DI;
using UnityEngine;

namespace Core.Interaction
{
    /// <summary>
    /// Контекст взаимодействия: даёт интерактабельному объекту доступ к тому, кто с ним
    /// взаимодействует, и к контейнерам (сигналы, состояние, сервисы).
    /// </summary>
    public class InteractionContext
    {
        public readonly GameObject Player;
        public readonly DIContainer Root;

        public InteractionContext(GameObject player, DIContainer root)
        {
            Player = player;
            Root = root;
        }
    }
}
