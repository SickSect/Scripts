using Core.Flags;
using Core.Inventory;
using Core.Story;
using Core.Story.Dialogue;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueGraphEditor
{
    /// <summary>
    /// Узел диалога в графе. Тип задаёт вид и цвет заголовка:
    ///  - Speech (реплика NPC) — синеватый
    ///  - Choice (вариант игрока) — зеленоватый
    ///
    /// Порты: слева вход, справа выход (связи = переходы, поле nextIds).
    /// </summary>
    public class DialogueGraphNode : Node
    {
        public DialogueNode Data { get; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        private readonly Dialogue _dialogue;

        /// <summary>Просит холст перерисовать ноду (после создания ассета).</summary>
        public System.Action<DialogueGraphNode> OnNeedRebuild;

        private const float NodeWidth = 320f;
        private const float FieldWidth = NodeWidth - 30f;

        public DialogueGraphNode(DialogueNode data, Dialogue dialogue = null)
        {
            Data = data;
            _dialogue = dialogue;
            RefreshTitle();

            BuildPorts();
            BuildBody();

            RefreshExpandedState();
            RefreshPorts();
            SetPosition(new Rect(data.graphPosition, new Vector2(NodeWidth, 200)));
            style.width = NodeWidth;
        }

        private void RefreshTitle()
        {
            bool isChoice = Data.kind == DialogueNode.NodeKind.Choice;
            title = $"{(isChoice ? "ОТВЕТ" : "РЕПЛИКА")}: {(string.IsNullOrEmpty(Data.id) ? "?" : Data.id)}";
            titleContainer.style.backgroundColor = isChoice
                ? new Color(0.18f, 0.32f, 0.20f)
                : new Color(0.18f, 0.24f, 0.35f);
        }

        private void BuildPorts()
        {
            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input,
                                        Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "вход";
            inputContainer.Add(InputPort);

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output,
                                         Port.Capacity.Multi, typeof(bool));
            OutputPort.portName = "далее";
            outputContainer.Add(OutputPort);
        }

        private void BuildBody()
        {
            var body = new VisualElement();
            body.style.paddingLeft = 8;
            body.style.paddingRight = 8;
            body.style.paddingTop = 6;
            body.style.paddingBottom = 6;

            // Тип узла.
            body.Add(Caption("Тип узла"));
            var kindField = new EnumField(Data.kind);
            kindField.style.width = FieldWidth;
            kindField.RegisterValueChangedCallback(e =>
            {
                Data.kind = (DialogueNode.NodeKind)e.newValue;
                RefreshTitle();
            });
            body.Add(kindField);

            body.Add(Caption("ID"));
            body.Add(Text(Data.id, v => { Data.id = v; RefreshTitle(); }));

            if (Data.kind == DialogueNode.NodeKind.Speech)
            {
                body.Add(Caption("Говорящий"));
                body.Add(Text(Data.speaker, v => Data.speaker = v));
            }

            body.Add(Caption(Data.kind == DialogueNode.NodeKind.Choice ? "Текст ответа" : "Реплика"));
            var textField = Text(Data.text, v => Data.text = v, multiline: true);
            textField.style.minHeight = 50;
            body.Add(textField);

            // Условие — только для выборов.
            if (Data.kind == DialogueNode.NodeKind.Choice)
            {
                body.Add(Caption("УСЛОВИЕ ПОКАЗА", bold: true));

                var condRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var condField = Obj<TriggerCondition>(Data.condition, v => Data.condition = v);
                condField.style.width = FieldWidth - 30;
                condRow.Add(condField);

                var newCond = new Button(() =>
                {
                    if (_dialogue == null) return;
                    Data.condition = DialogueAssetFactory.CreateCondition(_dialogue, Data.id);
                    OnNeedRebuild?.Invoke(this);
                })
                { text = "+" };
                newCond.style.width = 26;
                newCond.tooltip = "Создать пустое условие";
                condRow.Add(newCond);
                body.Add(condRow);

                // Блокировка ветки: накапливает триггеры в одно условие (НЕ A И НЕ B ...).
                body.Add(Caption("Блокировать веткой по триггеру"));
                var blockRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var blockField = new ObjectField
                {
                    objectType = typeof(TriggerDefinition),
                    allowSceneObjects = false
                };
                blockField.style.width = FieldWidth - 30;
                blockRow.Add(blockField);

                var makeBlock = new Button(() =>
                {
                    if (_dialogue == null) return;
                    var trig = blockField.value as TriggerDefinition;
                    if (trig == null)
                    {
                        Debug.LogWarning("[DialogueGraph] выбери триггер, по которому блокировать ветку");
                        return;
                    }
                    // Добавляем к существующему условию (накапливаем), а не пересоздаём.
                    Data.condition = DialogueAssetFactory.AddBlockTrigger(
                        _dialogue, Data.condition, trig, $"Block_{Data.id}");

                    // Сохраняем и сам диалог — иначе ссылка на условие потеряется при перестройке.
                    UnityEditor.EditorUtility.SetDirty(_dialogue);
                    UnityEditor.AssetDatabase.SaveAssets();

                    Debug.Log($"[DialogueGraph] блокировок в условии: " +
                              $"{(Data.condition != null ? Data.condition.requirements.Count : 0)}");

                    blockField.value = null;                 // очистим поле под следующий триггер
                    OnNeedRebuild?.Invoke(this);
                })
                { text = "⛔" };
                makeBlock.style.width = 26;
                makeBlock.tooltip = "Добавить блокировку «НЕТ этого триггера» к условию показа (накапливается)";
                blockRow.Add(makeBlock);
                body.Add(blockRow);

                body.Add(Hint("Выбирай триггеры и жми ⛔ по очереди —\nветка скрыта, если есть ЛЮБОЙ из них"));

                // Показать, какие триггеры уже блокируют ветку.
                body.Add(DescribeBlockers());
            }

            // Награды.
            body.Add(Caption("НАГРАДЫ УЗЛА", bold: true));
            body.Add(Caption("Выдать триггер"));
            var trigRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var trigField = Obj<TriggerDefinition>(Data.giveTrigger, v => Data.giveTrigger = v);
            trigField.style.width = FieldWidth - 30;
            trigRow.Add(trigField);

            var newTrig = new Button(() =>
            {
                if (_dialogue == null) return;
                Data.giveTrigger = DialogueAssetFactory.CreateTrigger(_dialogue, Data.id);
                OnNeedRebuild?.Invoke(this);
            })
            { text = "+" };
            newTrig.style.width = 26;
            newTrig.tooltip = "Создать новый триггер";
            trigRow.Add(newTrig);
            body.Add(trigRow);

            body.Add(Caption("Выдать предмет"));
            body.Add(Obj<ItemDefinition>(Data.giveItem, v => Data.giveItem = v));

            body.Add(Caption("Количество"));
            var count = new IntegerField { value = Data.giveItemCount };
            count.style.width = FieldWidth;
            count.RegisterValueChangedCallback(e => Data.giveItemCount = e.newValue);
            body.Add(count);

            body.Add(Caption("Если нет места → id узла"));
            body.Add(Text(Data.nextIfNoSpace, v => Data.nextIfNoSpace = v));

            // Возврат к старту.
            var ret = new Toggle("Вернуться к началу") { value = Data.returnToStart };
            ret.style.width = FieldWidth;
            ret.tooltip = "После этого узла диалог вернётся к стартовой реплике (для базовых веток)";
            ret.RegisterValueChangedCallback(e => Data.returnToStart = e.newValue);
            body.Add(ret);

            mainContainer.Add(body);
        }

        /// <summary>Строка со списком триггеров, которые сейчас блокируют ветку.</summary>
        private VisualElement DescribeBlockers()
        {
            var box = new VisualElement();
            if (Data.condition == null || Data.condition.requirements == null
                || Data.condition.requirements.Count == 0)
            {
                box.Add(Hint("блокировок нет"));
                return box;
            }

            var sb = new System.Text.StringBuilder("Скрыто если есть: ");
            bool first = true;
            foreach (var r in Data.condition.requirements)
            {
                if (r.trigger == null) continue;
                if (!first) sb.Append(", ");
                sb.Append(r.negate ? r.trigger.id : $"(без НЕ){r.trigger.id}");
                first = false;
            }
            box.Add(Hint(sb.ToString()));
            return box;
        }

        public void SavePosition() => Data.graphPosition = GetPosition().position;

        // ---------- хелперы ----------

        private static Label Caption(string text, bool bold = false)
        {
            var l = new Label(text);
            l.style.fontSize = 10;
            l.style.color = bold ? new Color(0.95f, 0.82f, 0.4f) : new Color(0.62f, 0.62f, 0.62f);
            l.style.marginTop = 3;
            if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold;
            return l;
        }

        private static Label Hint(string text)
        {
            var l = new Label(text);
            l.style.fontSize = 9;
            l.style.color = new Color(0.55f, 0.6f, 0.55f);
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        private static TextField Text(string value, System.Action<string> onChange, bool multiline = false)
        {
            var f = new TextField { value = value, multiline = multiline };
            f.style.width = FieldWidth;
            if (multiline) f.style.whiteSpace = WhiteSpace.Normal;
            f.RegisterValueChangedCallback(e => onChange(e.newValue));
            return f;
        }

        private static ObjectField Obj<T>(Object value, System.Action<T> onChange) where T : Object
        {
            var f = new ObjectField
            {
                objectType = typeof(T),
                allowSceneObjects = false,
                value = value
            };
            f.style.width = FieldWidth;
            f.RegisterValueChangedCallback(e => onChange(e.newValue as T));
            return f;
        }
    }
}