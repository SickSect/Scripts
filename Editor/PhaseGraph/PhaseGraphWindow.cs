using Core.Story.Phases;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PhaseGraphEditor
{
    /// <summary>
    /// Окно графа фаз: варианты колонками по шагам, триггеры слева.
    /// Всё редактируется прямо в нодах, ассеты создаются кнопками.
    /// Открыть: Window → Phase Graph.
    /// </summary>
    public class PhaseGraphWindow : EditorWindow
    {
        private PhaseGraphView _graphView;
        private PhaseGraph _graph;
        private ObjectField _assetField;
        private IntegerField _newStepField;
        private TextField _newVariantField;

        [MenuItem("Window/Phase Graph")]
        public static void Open()
        {
            var window = GetWindow<PhaseGraphWindow>();
            window.titleContent = new GUIContent("Phase Graph");
            window.minSize = new Vector2(900, 600);
        }

        public static void Open(PhaseGraph graph)
        {
            Open();
            GetWindow<PhaseGraphWindow>().LoadGraph(graph);
        }

        private void CreateGUI()
        {
            BuildToolbar();
            BuildGraph();
            TryAutoLoad();
        }

        private void BuildToolbar()
        {
            var toolbar = new Toolbar();

            _assetField = new ObjectField("Граф фаз")
            {
                objectType = typeof(PhaseGraph),
                allowSceneObjects = false,
                value = _graph
            };
            _assetField.RegisterValueChangedCallback(e => LoadGraph(e.newValue as PhaseGraph));
            toolbar.Add(_assetField);

            toolbar.Add(new ToolbarButton(() => _graphView?.Refresh()) { text = "Обновить" });

            // Создание нового варианта фазы.
            _newStepField = new IntegerField("Шаг") { value = 1 };
            _newStepField.style.width = 90;
            toolbar.Add(_newStepField);

            _newVariantField = new TextField("Вариант") { value = "A" };
            _newVariantField.style.width = 120;
            toolbar.Add(_newVariantField);

            toolbar.Add(new ToolbarButton(() =>
            {
                if (_graph == null) { Debug.LogWarning("[PhaseGraph] не выбран граф"); return; }
                _graphView.CreateVariant(_newStepField.value, _newVariantField.value);
            })
            { text = "+ Вариант фазы" });

            var hint = new Label("  Слева — триггеры. Колонки — шаги. Ассеты создаются кнопками в нодах.");
            hint.style.color = new Color(0.7f, 0.7f, 0.7f);
            hint.style.unityTextAlign = TextAnchor.MiddleLeft;
            toolbar.Add(hint);

            rootVisualElement.Add(toolbar);
        }

        private void BuildGraph()
        {
            _graphView = new PhaseGraphView();
            rootVisualElement.Add(_graphView);
        }

        private void TryAutoLoad()
        {
            if (_graph != null) { LoadGraph(_graph); return; }
            var found = Resources.Load<PhaseGraph>("Core/PhaseGraph");
            if (found != null) LoadGraph(found);
        }

        private void LoadGraph(PhaseGraph graph)
        {
            _graph = graph;
            if (_assetField != null && _assetField.value != graph)
                _assetField.SetValueWithoutNotify(graph);
            _graphView?.Load(graph);
        }
    }
}
