using Core.Story.Dialogue;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueGraphEditor
{
    /// <summary>
    /// Окно нодового редактора диалогов.
    /// Узлы двух типов: реплика (Speech) и вариант игрока (Choice).
    /// Связи между ними = переходы.
    /// </summary>
    public class DialogueGraphWindow : EditorWindow
    {
        private DialogueGraphView _graphView;
        private Dialogue _dialogue;
        private ObjectField _assetField;
        private TextField _startNodeField;
        private ObjectField _completionField;

        [MenuItem("Window/Dialogue Graph")]
        public static void Open()
        {
            var window = GetWindow<DialogueGraphWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
            window.minSize = new Vector2(900, 600);
        }

        public static void Open(Dialogue dialogue)
        {
            Open();
            GetWindow<DialogueGraphWindow>().LoadDialogue(dialogue);
        }

        private void CreateGUI()
        {
            BuildToolbar();
            BuildGraph();
        }

        private void BuildToolbar()
        {
            var toolbar = new Toolbar();

            _assetField = new ObjectField("Диалог")
            {
                objectType = typeof(Dialogue),
                allowSceneObjects = false,
                value = _dialogue
            };
            _assetField.RegisterValueChangedCallback(e => LoadDialogue(e.newValue as Dialogue));
            toolbar.Add(_assetField);

            toolbar.Add(new ToolbarButton(() =>
            {
                _graphView?.Save();
                Debug.Log("[DialogueGraph] сохранено");
            })
            { text = "Сохранить" });

            toolbar.Add(new ToolbarButton(() =>
            {
                if (_dialogue == null) return;
                _graphView.CreateNode(DialogueNode.NodeKind.Speech, new Vector2(200, 200));
            })
            { text = "+ Реплика" });

            toolbar.Add(new ToolbarButton(() =>
            {
                if (_dialogue == null) return;
                _graphView.CreateNode(DialogueNode.NodeKind.Choice, new Vector2(400, 200));
            })
            { text = "+ Ответ" });

            toolbar.Add(new ToolbarButton(() => _graphView?.DeleteSelectedNodes())
            { text = "Удалить выделенные" });

            _startNodeField = new TextField("Старт (id)")
            { value = _dialogue != null ? _dialogue.startNodeId : "" };
            _startNodeField.style.width = 170;
            _startNodeField.RegisterValueChangedCallback(e =>
            {
                if (_dialogue != null) _dialogue.startNodeId = e.newValue;
            });
            toolbar.Add(_startNodeField);

            _completionField = new ObjectField("Завершение (опц.)")
            {
                objectType = typeof(Core.Flags.TriggerDefinition),
                allowSceneObjects = false,
                value = _dialogue != null ? _dialogue.completionTrigger : null
            };
            _completionField.style.width = 240;
            _completionField.RegisterValueChangedCallback(e =>
            {
                if (_dialogue != null)
                    _dialogue.completionTrigger = e.newValue as Core.Flags.TriggerDefinition;
            });
            toolbar.Add(_completionField);

            rootVisualElement.Add(toolbar);
        }

        private void BuildGraph()
        {
            _graphView = new DialogueGraphView();
            rootVisualElement.Add(_graphView);
            if (_dialogue != null) _graphView.Load(_dialogue);
        }

        private void LoadDialogue(Dialogue dialogue)
        {
            _dialogue = dialogue;
            if (_assetField != null && _assetField.value != dialogue)
                _assetField.SetValueWithoutNotify(dialogue);
            if (_startNodeField != null)
                _startNodeField.SetValueWithoutNotify(dialogue != null ? dialogue.startNodeId : "");
            if (_completionField != null)
                _completionField.SetValueWithoutNotify(dialogue != null ? dialogue.completionTrigger : null);

            _graphView?.Load(dialogue);
        }
    }
}
