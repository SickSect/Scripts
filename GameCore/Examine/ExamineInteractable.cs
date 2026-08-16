using Core.Interaction;
using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// Мост Interact→Examine: по нажатию Interact (E) открывает меню ExaminePoint этого
    /// объекта. Вешать рядом с ExaminePoint на дверь/сложный объект. Простые объекты
    /// (подобрать ключ, щёлкнуть выключатель) продолжают жить на своих IInteractable.
    /// </summary>
    [RequireComponent(typeof(ExaminePoint))]
    public class ExamineInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Осмотреть";
        public string Prompt => _prompt;

        private ExaminePoint _point;
        private ExamineController _controller;

        private void Awake() => _point = GetComponent<ExaminePoint>();

        public void Interact(InteractionContext context)
        {
            if (_controller == null) _controller = FindAnyObjectByType<ExamineController>();
            if (_controller != null) _controller.Open(_point);
        }
    }
}