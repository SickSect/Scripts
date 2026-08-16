using System;
using System.Collections.Generic;
using Core.Flags;
using Core.Inventory;
using UnityEngine;

namespace Core.Story.Dialogue
{
    /// <summary>
    /// Узел диалога. Два типа:
    ///  - Speech: реплика говорящего (NPC). Ведёт к следующему узлу или к списку выборов.
    ///  - Choice: вариант ответа игрока. Показывается, если выполнено condition.
    ///
    /// Ветка «закрывается» так: последний узел ветки выдаёт триггер, а первый узел-выбор
    /// этой ветки имеет условие «НЕТ этого триггера». Пройдя ветку, игрок её больше не видит,
    /// остальные ветки остаются доступны.
    /// </summary>
    [Serializable]
    public class DialogueNode
    {
        public enum NodeKind { Speech, Choice }

        [Tooltip("Уникальный id узла в пределах диалога.")]
        public string id;

        [Tooltip("Тип узла: реплика говорящего или вариант игрока.")]
        public NodeKind kind = NodeKind.Speech;

        [Tooltip("Имя говорящего (для Speech).")]
        public string speaker;

        [Tooltip("Текст реплики или вариант ответа.")]
        [TextArea(2, 5)] public string text;

        [Header("Доступность (для Choice)")]
        [Tooltip("Вариант виден, только если условие выполнено. Пусто = виден всегда. " +
                 "Для блокировки пройденной ветки: условие «НЕТ триггера ветки».")]
        public TriggerCondition condition;

        [Header("Награды узла")]
        [Tooltip("Триггер, который выдаётся при прохождении этого узла.")]
        public TriggerDefinition giveTrigger;

        [Tooltip("Предмет, который выдаётся при прохождении узла (с проверкой места).")]
        public ItemDefinition giveItem;
        public int giveItemCount = 1;

        [Tooltip("Куда перейти, если предмет не влез. Пусто = прервать диалог.")]
        public string nextIfNoSpace;

        [Header("Переходы")]
        [Tooltip("id узлов, к которым ведёт этот узел. Для Speech: список доступных выборов " +
                 "или следующая реплика. Для Choice: обычно один переход к реплике-ответу.")]
        public List<string> nextIds = new();

        [Tooltip("Вернуться к стартовому узлу после этого узла (для базовых веток). " +
                 "Удобнее, чем тянуть связь через весь граф.")]
        public bool returnToStart;

        [Tooltip("Позиция узла в графовом редакторе (на игру не влияет).")]
        [HideInInspector] public Vector2 graphPosition;
    }
}
