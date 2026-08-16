using Core.Flags;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace PhaseGraphEditor
{
    /// <summary>
    /// Нода триггера (метки). Компактная: id + порты.
    /// Вход — кто выдаёт этот триггер, выход — куда он ведёт (в условия фаз).
    /// </summary>
    public class TriggerNode : Node
    {
        public TriggerDefinition Data { get; }
        public Port InPort { get; private set; }   // кто выдаёт
        public Port OutPort { get; private set; }  // куда используется

        public TriggerNode(TriggerDefinition data)
        {
            Data = data;
            title = string.IsNullOrEmpty(data.id) ? data.name : data.id;

            // Визуально отличаем триггеры от фаз.
            titleContainer.style.backgroundColor = new Color(0.35f, 0.25f, 0.1f);

            InPort = InstantiatePort(Orientation.Horizontal, Direction.Input,
                                     Port.Capacity.Multi, typeof(bool));
            InPort.portName = "выдаётся";
            inputContainer.Add(InPort);

            OutPort = InstantiatePort(Orientation.Horizontal, Direction.Output,
                                      Port.Capacity.Multi, typeof(bool));
            OutPort.portName = "нужен для";
            outputContainer.Add(OutPort);

            var pingBtn = new Button(() => { Selection.activeObject = Data; EditorGUIUtility.PingObject(Data); })
            { text = "Показать" };
            pingBtn.style.fontSize = 10;
            mainContainer.Add(pingBtn);

            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
