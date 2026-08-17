using UnityEngine;

namespace Core.Interaction
{
    /// <summary>
    /// Интерфейс для любого объекта, который можно брать и переносить.
    /// </summary>
    public interface ICarryable
    {
        Transform Transform { get; }
        Rigidbody Rigidbody { get; }

        /// <summary> Вызывается при захвате物体. Отключает физику. </summary>
        void OnPickUp();

        /// <summary> Вызывается при броске. Включает физику. </summary>
        void OnDrop();
    }
}