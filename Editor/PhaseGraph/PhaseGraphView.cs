using System.Collections.Generic;
using System.Linq;
using Core.Flags;
using Core.Story.Phases;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace PhaseGraphEditor
{
    /// <summary>
    /// Редактируемый холст графа фаз: варианты колонками по шагам, триггеры слева,
    /// связи «триггер → условие фазы» и «фаза → выдаваемый триггер».
    ///
    /// Ассеты (варианты, спавны, условия, триггеры) создаются прямо из нод.
    /// </summary>
    public class PhaseGraphView : GraphView
    {
        private readonly List<PhaseVariantNode> _phaseNodes = new();
        private readonly Dictionary<TriggerDefinition, TriggerNode> _triggerNodes = new();
        private PhaseGraph _graph;

        private const float ColumnWidth = 400f;
        private const float RowHeight = 520f;
        private const float TriggerColumnX = -300f;

        public PhaseGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            style.flexGrow = 1;
        }

        public void Load(PhaseGraph graph)
        {
            _graph = graph;
            ClearGraph();
            if (graph == null) return;

            BuildPhaseNodes(graph);
            BuildTriggerNodes();
            BuildConnections();
        }

        /// <summary>Перерисовать граф целиком (после создания/удаления ассетов).</summary>
        public void Refresh() => Load(_graph);

        private void ClearGraph()
        {
            _phaseNodes.Clear();
            _triggerNodes.Clear();
            graphElements.ForEach(RemoveElement);
        }

        // ---------- фазы колонками по шагам ----------

        private void BuildPhaseNodes(PhaseGraph graph)
        {
            var byStep = new Dictionary<int, List<PhaseVariant>>();
            foreach (var v in graph.variants)
            {
                if (v == null) continue;
                if (!byStep.TryGetValue(v.step, out var list))
                {
                    list = new List<PhaseVariant>();
                    byStep[v.step] = list;
                }
                list.Add(v);
            }

            foreach (var kv in byStep.OrderBy(k => k.Key))
            {
                int step = kv.Key;
                var variants = kv.Value;

                for (int row = 0; row < variants.Count; row++)
                {
                    var node = new PhaseVariantNode(variants[row], graph);
                    node.OnNeedRebuild = _ => Refresh();
                    node.SetPosition(new Rect((step - 1) * ColumnWidth, row * RowHeight, 340, 400));
                    AddElement(node);
                    _phaseNodes.Add(node);
                }
            }
        }

        // ---------- триггеры ----------

        private void BuildTriggerNodes()
        {
            var used = new List<TriggerDefinition>();

            foreach (var pn in _phaseNodes)
            {
                foreach (var t in pn.GetConditionTriggers())
                    if (t != null && !used.Contains(t)) used.Add(t);

                var give = pn.Data.onActivateTrigger;
                if (give != null && !used.Contains(give)) used.Add(give);
            }

            for (int i = 0; i < used.Count; i++)
            {
                var node = new TriggerNode(used[i]);
                node.SetPosition(new Rect(TriggerColumnX, i * 130f, 200, 100));
                AddElement(node);
                _triggerNodes[used[i]] = node;
            }
        }

        // ---------- связи ----------

        private void BuildConnections()
        {
            foreach (var pn in _phaseNodes)
            {
                foreach (var t in pn.GetConditionTriggers())
                {
                    if (t == null || !_triggerNodes.TryGetValue(t, out var tn)) continue;
                    AddElement(tn.OutPort.ConnectTo(pn.TriggerInPort));
                }

                var give = pn.Data.onActivateTrigger;
                if (give != null && _triggerNodes.TryGetValue(give, out var giveNode))
                    AddElement(pn.TriggerOutPort.ConnectTo(giveNode.InPort));
            }
        }

        // ---------- создание варианта ----------

        public void CreateVariant(int step, string variantId)
        {
            if (_graph == null) return;
            PhaseAssetFactory.CreateVariant(_graph, step, variantId);
            Refresh();
        }

        // Связи вручную не тянем — они выводятся из условий.
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
            => new List<Port>();
    }
}
