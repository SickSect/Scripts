using System.Collections.Generic;
using Core.Common;
using Core.DI;
using Core.Flags;
using Core.Inventory;
using R3;
using UnityEngine;

namespace Core.Story.Dialogue
{
    /// <summary>
    /// Логика прохождения диалога по узлам.
    ///
    /// Speech-узел показывается как реплика; доступные варианты — это Choice-узлы
    /// из его nextIds, прошедшие фильтр по condition.
    /// Choice-узел при выборе выдаёт свои награды и ведёт к своему nextIds[0].
    ///
    /// Ветка закрывается собственным триггером (условие на узле-выборе), диалог целиком
    /// остаётся доступен — базовые ветки работают всегда.
    /// </summary>
    public class DialoguePlayer
    {
        private readonly DIContainer _root;

        /// <summary>Текущая реплика (Speech). null = диалог закрыт.</summary>
        public ReactiveProperty<DialogueNode> CurrentNode { get; } = new(null);

        /// <summary>Доступные варианты (Choice-узлы) после фильтра по условиям.</summary>
        public ReactiveProperty<List<DialogueNode>> VisibleChoices { get; } = new(new());

        public Subject<Unit> Closed { get; } = new();

        private Dialogue _dialogue;
        private bool _active;

        public bool IsActive => _active;

        public DialoguePlayer(DIContainer root) => _root = root;

        public void StartDialogue(Dialogue dialogue)
        {
            if (dialogue == null || _active) return;
            _dialogue = dialogue;
            _active = true;
            GoToNode(dialogue.startNodeId);
        }

        /// <summary>Игрок выбрал вариант (индекс в VisibleChoices).</summary>
        public void Choose(int visibleIndex)
        {
            var choices = VisibleChoices.Value;
            if (visibleIndex < 0 || visibleIndex >= choices.Count) return;

            var choice = choices[visibleIndex];

            // Награды узла-выбора (если есть) с проверкой места.
            if (!ApplyRewards(choice)) return;

            // Куда ведёт выбор.
            if (choice.returnToStart) { GoToNode(_dialogue.startNodeId); return; }
            if (choice.nextIds.Count == 0) { Close(completed: true); return; }
            GoToNode(choice.nextIds[0]);
        }

        /// <summary>Продолжить с реплики без выборов («Далее»).</summary>
        public void Continue()
        {
            var node = CurrentNode.Value;
            if (node == null) { Close(completed: true); return; }

            if (node.returnToStart) { GoToNode(_dialogue.startNodeId); return; }
            if (node.nextIds.Count == 0) { Close(completed: true); return; }
            GoToNode(node.nextIds[0]);
        }

        // ---------- переходы ----------

        private void GoToNode(string nodeId)
        {
            var node = _dialogue.GetNode(nodeId);
            if (node == null)
            {
                CoreLog.Debug($"[Dialogue] узел '{nodeId}' не найден — завершаем");
                Close(completed: true);
                return;
            }

            // Награды реплики выдаются при её показе.
            if (node.kind == DialogueNode.NodeKind.Speech && !ApplyRewards(node)) return;

            // ВАЖНО: сначала выборы, потом нода — UI читает VisibleChoices в обработчике.
            VisibleChoices.Value = CollectChoices(node);
            CurrentNode.Value = node;
        }

        /// <summary>Собрать доступные Choice-узлы, на которые ссылается реплика.</summary>
        private List<DialogueNode> CollectChoices(DialogueNode node)
        {
            var result = new List<DialogueNode>();
            if (node.kind != DialogueNode.NodeKind.Speech) return result;

            _root.TryResolve<FlagService>(out var flags);
            _root.TryResolve<InventoryService>(out var inventory);
            var condCtx = new ConditionContext(flags, inventory);

            foreach (var id in node.nextIds)
            {
                var next = _dialogue.GetNode(id);
                if (next == null || next.kind != DialogueNode.NodeKind.Choice) continue;
                if (next.condition == null || next.condition.Evaluate(condCtx))
                    result.Add(next);
            }
            return result;
        }

        // ---------- награды ----------

        /// <summary>Выдать триггер/предмет узла. false — если места нет (переход на отказ).</summary>
        private bool ApplyRewards(DialogueNode node)
        {
            _root.TryResolve<FlagService>(out var flags);
            _root.TryResolve<InventoryService>(out var inventory);

            if (node.giveItem != null)
            {
                bool fits = inventory != null && inventory.CanFit(node.giveItem, node.giveItemCount);
                if (!fits)
                {
                    CoreLog.Debug("[Dialogue] нет места — переход на реплику-отказ");
                    if (!string.IsNullOrEmpty(node.nextIfNoSpace)) GoToNode(node.nextIfNoSpace);
                    else Close(completed: false);
                    return false;
                }
                inventory.Add(node.giveItem, node.giveItemCount);
            }

            if (node.giveTrigger != null && flags != null)
            {
                flags.Set(node.giveTrigger);
                CoreLog.Debug($"[Dialogue] выдан триггер {node.giveTrigger.id}");
            }

            return true;
        }

        // ---------- закрытие ----------

        public void Close(bool completed)
        {
            if (!_active) return;
            _active = false;

            if (completed && _dialogue.completionTrigger != null
                && _root.TryResolve<FlagService>(out var flags))
            {
                flags.Set(_dialogue.completionTrigger);
                CoreLog.Debug($"[Dialogue] завершён, триггер {_dialogue.completionTrigger.id}");
            }

            CurrentNode.Value = null;
            VisibleChoices.Value = new();
            _dialogue = null;
            Closed.OnNext(Unit.Default);
        }
    }
}
