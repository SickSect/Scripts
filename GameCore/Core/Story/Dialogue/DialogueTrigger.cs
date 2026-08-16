using Core.Common;
using Core.Interaction;
using UnityEngine;

namespace Core.Story.Dialogue
{
    /// <summary>
    /// Запуск диалога с объектом (телефон, NPC) по Interact.
    ///
    /// Диалог целиком НЕ блокируется — объект остаётся интерактивным всегда.
    /// Пройденные сюжетные ветки скрываются условиями на узлах-выборах внутри диалога,
    /// базовые ветки остаются доступны.
    ///
    /// Носитель диалога спавнится/назначается фазой (DialoguePhaseSpawn / DialogueAssignSpawn).
    /// </summary>
    public class DialogueTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private Dialogue _dialogue;
        [SerializeField] private string _prompt = "Поговорить";

        [Header("Слот для фазы (опционально)")]
        [Tooltip("Id слота. Фаза может назначить сюда диалог через DialogueAssignSpawn. " +
                 "Пусто = объект не управляется фазой, диалог задан вручную.")]
        [SerializeField] private string _slotId;

        public string SlotId => _slotId;
        public string Prompt => _prompt;

        /// <summary>Задать диалог при спавне/назначении из фазы.</summary>
        public void Configure(Dialogue dialogue, string prompt = null)
        {
            _dialogue = dialogue;
            if (!string.IsNullOrEmpty(prompt)) _prompt = prompt;
        }

        public void Interact(InteractionContext context)
        {
            if (_dialogue == null)
            {
                CoreLog.Debug("[DialogueTrigger] диалог не назначен");
                return;
            }
            if (context.Root.TryResolve<DialoguePlayer>(out var player))
                player.StartDialogue(_dialogue);
        }
    }
}
