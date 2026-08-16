using System.Collections.Generic;
using System.Linq;
using Core.Story.Dialogue;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueGraphEditor
{
    /// <summary>
    /// Холст графа диалога: узлы (реплики и выборы), связи = переходы (nextIds).
    /// Загружает Dialogue-ассет и сохраняет структуру обратно.
    /// </summary>
    public class DialogueGraphView : GraphView
    {
        private Dialogue _dialogue;
        private readonly List<DialogueGraphNode> _nodes = new();

        public DialogueGraphView()
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

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(p =>
                p != startPort &&
                p.node != startPort.node &&
                p.direction != startPort.direction).ToList();
        }

        // ---------- загрузка ----------

        public void Load(Dialogue dialogue)
        {
            _dialogue = dialogue;
            ClearGraph();
            if (dialogue == null) return;

            foreach (var data in dialogue.nodes)
            {
                if (data == null) continue;
                var node = new DialogueGraphNode(data, dialogue);
                node.OnNeedRebuild = RebuildNode;
                AddElement(node);
                _nodes.Add(node);
            }

            // Связи по nextIds.
            foreach (var node in _nodes)
            {
                foreach (var targetId in node.Data.nextIds)
                {
                    var target = _nodes.FirstOrDefault(n => n.Data.id == targetId);
                    if (target == null) continue;
                    AddElement(node.OutputPort.ConnectTo(target.InputPort));
                }
            }
        }

        private void ClearGraph()
        {
            _nodes.Clear();
            graphElements.ForEach(RemoveElement);
        }

        // ---------- сохранение ----------

        public void Save()
        {
            if (_dialogue == null) return;

            foreach (var node in _nodes)
            {
                node.SavePosition();
                node.Data.nextIds.Clear();
            }

            // Связи → nextIds.
            foreach (var edge in graphElements.OfType<Edge>())
            {
                var from = edge.output?.node as DialogueGraphNode;
                var to = edge.input?.node as DialogueGraphNode;
                if (from == null || to == null) continue;
                if (!from.Data.nextIds.Contains(to.Data.id))
                    from.Data.nextIds.Add(to.Data.id);
            }

            _dialogue.nodes = _nodes.Select(n => n.Data).ToList();

            EditorUtility.SetDirty(_dialogue);
            AssetDatabase.SaveAssets();
        }

        // ---------- создание узлов ----------

        public void CreateNode(DialogueNode.NodeKind kind, Vector2 position)
        {
            string prefix = kind == DialogueNode.NodeKind.Choice ? "choice" : "speech";
            var data = new DialogueNode
            {
                id = $"{prefix}_{System.DateTime.Now.Ticks % 10000}",
                kind = kind,
                graphPosition = position
            };
            var node = new DialogueGraphNode(data, _dialogue);
            node.OnNeedRebuild = RebuildNode;
            AddElement(node);
            _nodes.Add(node);
        }

        /// <summary>Пересоздать ноду (после создания ассета — чтобы поля обновились).</summary>
        public void RebuildNode(DialogueGraphNode node)
        {
            var data = node.Data;
            data.graphPosition = node.GetPosition().position;

            // Запоминаем связи этой ноды, чтобы восстановить.
            var outgoing = new List<string>(data.nextIds);
            var incoming = _nodes
                .Where(n => n != node && n.Data.nextIds.Contains(data.id))
                .Select(n => n.Data.id).ToList();

            var deadEdges = graphElements.OfType<Edge>()
                .Where(e => e.output?.node == node || e.input?.node == node).ToList();
            foreach (var e in deadEdges)
            {
                e.output?.Disconnect(e);
                e.input?.Disconnect(e);
                RemoveElement(e);
            }

            _nodes.Remove(node);
            RemoveElement(node);

            var fresh = new DialogueGraphNode(data, _dialogue);
            fresh.OnNeedRebuild = RebuildNode;
            AddElement(fresh);
            _nodes.Add(fresh);

            // Восстанавливаем связи.
            foreach (var targetId in outgoing)
            {
                var target = _nodes.FirstOrDefault(n => n.Data.id == targetId);
                if (target != null) AddElement(fresh.OutputPort.ConnectTo(target.InputPort));
            }
            foreach (var sourceId in incoming)
            {
                var source = _nodes.FirstOrDefault(n => n.Data.id == sourceId);
                if (source != null) AddElement(source.OutputPort.ConnectTo(fresh.InputPort));
            }
        }

        /// <summary>Удалить выделенные узлы (вместе со связями).</summary>
        public void DeleteSelectedNodes()
        {
            var selectedNodes = selection.OfType<DialogueGraphNode>().ToList();
            foreach (var node in selectedNodes)
            {
                var deadEdges = graphElements.OfType<Edge>()
                    .Where(e => e.output?.node == node || e.input?.node == node).ToList();
                foreach (var e in deadEdges)
                {
                    e.output?.Disconnect(e);
                    e.input?.Disconnect(e);
                    RemoveElement(e);
                }
                _nodes.Remove(node);
                RemoveElement(node);
            }
        }
    }
}
