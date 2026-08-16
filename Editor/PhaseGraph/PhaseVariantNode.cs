using System.Collections.Generic;
using System.Text;
using Core.Flags;
using Core.Story;
using Core.Story.Phases;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PhaseGraphEditor
{
    /// <summary>
    /// Редактируемая нода варианта фазы: шаг, id, время суток, условие активации,
    /// выдаваемый триггер и список спавнов с возможностью создавать новые прямо здесь.
    /// </summary>
    public class PhaseVariantNode : Node
    {
        public PhaseVariant Data { get; }
        public Port TriggerInPort { get; private set; }
        public Port TriggerOutPort { get; private set; }

        /// <summary>Просит холст перестроить эту ноду (после добавления/удаления спавна).</summary>
        public System.Action<PhaseVariantNode> OnNeedRebuild;

        private readonly PhaseGraph _graph;
        private const float FieldWidth = 300f;

        public PhaseVariantNode(PhaseVariant data, PhaseGraph graph)
        {
            Data = data;
            _graph = graph;
            title = $"Шаг {data.step} — {(string.IsNullOrEmpty(data.variantId) ? "?" : data.variantId)}";

            BuildPorts();
            BuildBody();

            RefreshExpandedState();
            RefreshPorts();
            style.width = FieldWidth + 40;
        }

        private void BuildPorts()
        {
            TriggerInPort = InstantiatePort(Orientation.Horizontal, Direction.Input,
                                            Port.Capacity.Multi, typeof(bool));
            TriggerInPort.portName = "условия";
            inputContainer.Add(TriggerInPort);

            TriggerOutPort = InstantiatePort(Orientation.Horizontal, Direction.Output,
                                             Port.Capacity.Multi, typeof(bool));
            TriggerOutPort.portName = "выдаёт";
            outputContainer.Add(TriggerOutPort);
        }

        private void BuildBody()
        {
            var body = new VisualElement();
            body.style.paddingLeft = 8;
            body.style.paddingRight = 8;
            body.style.paddingTop = 6;
            body.style.paddingBottom = 6;

            // --- шапка ---
            body.Add(Caption("Шаг"));
            var stepField = new IntegerField { value = Data.step };
            stepField.style.width = FieldWidth;
            stepField.RegisterValueChangedCallback(e =>
            {
                Data.step = e.newValue;
                title = $"Шаг {Data.step} — {Data.variantId}";
                Save();
            });
            body.Add(stepField);

            body.Add(Caption("ID варианта"));
            body.Add(Text(Data.variantId, v =>
            {
                Data.variantId = v;
                title = $"Шаг {Data.step} — {v}";
                Save();
            }));

            body.Add(Caption("Время суток"));
            var todField = new EnumField(Data.timeOfDay);
            todField.style.width = FieldWidth;
            todField.RegisterValueChangedCallback(e =>
            {
                Data.timeOfDay = (PhaseVariant.TimeOfDay)e.newValue;
                Save();
            });
            body.Add(todField);

            // --- условие активации ---
            body.Add(Caption("УСЛОВИЕ АКТИВАЦИИ", bold: true));
            var condRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var condField = Obj<TriggerCondition>(Data.activationCondition, v =>
            {
                Data.activationCondition = v;
                Save();
                OnNeedRebuild?.Invoke(this);
            });
            condField.style.width = FieldWidth - 60;
            condRow.Add(condField);

            var newCondBtn = new Button(() =>
            {
                Data.activationCondition = PhaseAssetFactory.CreateCondition(
                    _graph, $"Cond_{Data.step}{Data.variantId}");
                Save();
                OnNeedRebuild?.Invoke(this);
            })
            { text = "+" };
            newCondBtn.style.width = 26;
            newCondBtn.tooltip = "Создать новое условие";
            condRow.Add(newCondBtn);
            body.Add(condRow);

            body.Add(Label(DescribeCondition(Data.activationCondition), 10,
                           new Color(0.75f, 0.85f, 1f), wrap: true));

            // --- выдаваемый триггер ---
            body.Add(Caption("ВЫДАЁТ ТРИГГЕР", bold: true));
            var trigRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var trigField = Obj<TriggerDefinition>(Data.onActivateTrigger, v =>
            {
                Data.onActivateTrigger = v;
                Save();
                OnNeedRebuild?.Invoke(this);
            });
            trigField.style.width = FieldWidth - 60;
            trigRow.Add(trigField);

            var newTrigBtn = new Button(() =>
            {
                Data.onActivateTrigger = PhaseAssetFactory.CreateTrigger(
                    _graph, $"phase_{Data.step}{Data.variantId}");
                Save();
                OnNeedRebuild?.Invoke(this);
            })
            { text = "+" };
            newTrigBtn.style.width = 26;
            newTrigBtn.tooltip = "Создать новый триггер";
            trigRow.Add(newTrigBtn);
            body.Add(trigRow);

            // --- спавны ---
            body.Add(Caption($"СПАВНЫ ({Data.spawns.Count})", bold: true));
            for (int i = 0; i < Data.spawns.Count; i++)
                body.Add(BuildSpawnRow(i));

            body.Add(BuildAddSpawnButtons());

            // --- сервис ---
            var ping = new Button(() => { Selection.activeObject = Data; EditorGUIUtility.PingObject(Data); })
            { text = "Показать ассет" };
            ping.style.marginTop = 6;
            ping.style.fontSize = 10;
            body.Add(ping);

            mainContainer.Add(body);
        }

        private VisualElement BuildSpawnRow(int index)
        {
            var spawn = Data.spawns[index];

            var box = new VisualElement();
            box.style.marginTop = 4;
            box.style.paddingTop = 3;
            box.style.paddingBottom = 3;
            box.style.paddingLeft = 4;
            box.style.paddingRight = 4;
            box.style.backgroundColor = new Color(0.17f, 0.19f, 0.17f);

            string typeName = spawn != null ? spawn.GetType().Name.Replace("PhaseSpawn", "") : "?";
            box.Add(Label($"{index}. {typeName}", 10, new Color(0.7f, 1f, 0.7f)));

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var field = Obj<PhaseSpawn>(spawn, v =>
            {
                Data.spawns[index] = v;
                Save();
            });
            field.style.width = FieldWidth - 60;
            row.Add(field);

            int captured = index;
            var del = new Button(() =>
            {
                Data.spawns.RemoveAt(captured);
                Save();
                OnNeedRebuild?.Invoke(this);
            })
            { text = "×" };
            del.style.width = 26;
            del.tooltip = "Убрать из фазы (ассет не удаляется)";
            row.Add(del);

            box.Add(row);

            // Якорь — сразу видно и правится.
            if (spawn != null)
            {
                box.Add(Caption("Якорь (anchorId)"));
                box.Add(Text(spawn.anchorId, v =>
                {
                    spawn.anchorId = v;
                    EditorUtility.SetDirty(spawn);
                    AssetDatabase.SaveAssets();
                }));

                var editBtn = new Button(() => { Selection.activeObject = spawn; EditorGUIUtility.PingObject(spawn); })
                { text = "Настроить содержимое" };
                editBtn.style.fontSize = 9;
                box.Add(editBtn);
            }

            return box;
        }

        private VisualElement BuildAddSpawnButtons()
        {
            var wrap = new VisualElement();
            wrap.style.marginTop = 6;

            wrap.Add(Caption("Добавить спавн:"));

            var row1 = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            row1.Add(AddSpawnButton<ItemPhaseSpawn>("Предмет"));
            row1.Add(AddSpawnButton<EventPhaseSpawn>("Событие"));
            wrap.Add(row1);

            var row2 = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            row2.Add(AddSpawnButton<DialoguePhaseSpawn>("Диалог (спавн)"));
            row2.Add(AddSpawnButton<DialogueAssignSpawn>("Диалог (назначить)"));
            wrap.Add(row2);

            return wrap;
        }

        private Button AddSpawnButton<T>(string label) where T : PhaseSpawn
        {
            var btn = new Button(() =>
            {
                var spawn = PhaseAssetFactory.CreateSpawn<T>(
                    _graph, $"{typeof(T).Name}_{Data.step}{Data.variantId}");
                Data.spawns.Add(spawn);
                Save();
                OnNeedRebuild?.Invoke(this);
            })
            { text = label };
            btn.style.fontSize = 9;
            btn.style.flexGrow = 1;
            return btn;
        }

        private void Save()
        {
            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets();
        }

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

        private static Label Label(string text, int size, Color color, bool wrap = false)
        {
            var l = new Label(text);
            l.style.fontSize = size;
            l.style.color = color;
            if (wrap) l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        private static TextField Text(string value, System.Action<string> onChange)
        {
            var f = new TextField { value = value };
            f.style.width = FieldWidth;
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

        // ---------- описание условия ----------

        public static string DescribeCondition(TriggerCondition cond)
        {
            if (cond == null) return "— всегда (fallback) —";
            if (cond.requirements == null || cond.requirements.Count == 0) return "— пустое —";

            var sb = new StringBuilder();
            string joiner = cond.mode == TriggerCondition.Mode.All ? " И " : " ИЛИ ";
            for (int i = 0; i < cond.requirements.Count; i++)
            {
                var r = cond.requirements[i];
                if (i > 0) sb.Append(joiner);
                if (r.negate) sb.Append("НЕ ");

                if (r.subCondition != null) sb.Append($"({r.subCondition.name})");
                else if (r.trigger != null) sb.Append(r.trigger.id);
                else if (r.item != null) sb.Append($"предмет {r.item.displayName}");
                else sb.Append("?");
            }
            return sb.ToString();
        }

        public IEnumerable<TriggerDefinition> GetConditionTriggers()
        {
            var result = new List<TriggerDefinition>();
            Collect(Data.activationCondition, result, 0);
            return result;
        }

        private static void Collect(TriggerCondition cond, List<TriggerDefinition> into, int depth)
        {
            if (cond == null || depth > 5) return;
            foreach (var r in cond.requirements)
            {
                if (r.trigger != null && !into.Contains(r.trigger)) into.Add(r.trigger);
                if (r.subCondition != null) Collect(r.subCondition, into, depth + 1);
            }
        }
    }
}
