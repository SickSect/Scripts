using Core.Story.Phases;
using UnityEditor;
using UnityEngine;

namespace PhaseGraphEditor
{
    /// <summary>
    /// Пункт контекстного меню: открыть выбранный PhaseGraph-ассет в окне обзора.
    /// (Правый клик по ассету → Open Phase Graph)
    /// </summary>
    public static class PhaseGraphAssetOpener
    {
        [MenuItem("Assets/Open Phase Graph", true)]
        private static bool Validate() => Selection.activeObject is PhaseGraph;

        [MenuItem("Assets/Open Phase Graph")]
        private static void OpenSelected()
        {
            if (Selection.activeObject is PhaseGraph g)
                PhaseGraphWindow.Open(g);
        }
    }
}
