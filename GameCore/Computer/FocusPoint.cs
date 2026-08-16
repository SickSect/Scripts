using UnityEngine;

namespace Core.Computer
{
    /// <summary>
    /// Точка фокуса камеры: монитор, общий план стола, ящик, инструмент в руках.
    ///
    /// Сам объект и есть якорь — позиция и поворот трансформа задают,
    /// откуда и куда смотрит камера. Отдельной пустышки не нужно.
    /// </summary>
    public class FocusPoint : MonoBehaviour
    {
        [Header("Переход")]
        [Tooltip("Сколько камера едет сюда.")]
        [SerializeField] private float _moveTime = 0.35f;

        [Tooltip("Опциональный угол обзора. 0 — не менять.")]
        [SerializeField] private float _fieldOfView = 0f;

        [Header("Подпись")]
        [Tooltip("Для отладки и подсказок.")]
        [SerializeField] private string _title = "Фокус";

        public float MoveTime => _moveTime;
        public float FieldOfView => _fieldOfView;
        public string Title => _title;

        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.06f);
            Gizmos.DrawRay(transform.position, transform.forward * 0.35f);
        }
#endif
    }
}
