using Core.Story.Dialogue;
using UnityEditor;
using UnityEngine;

namespace DialogueGraphEditor
{
    /// <summary>
    /// Пункт контекстного меню: открыть выбранный Dialogue-ассет в нодовом редакторе.
    /// (Правый клик по ассету → Open Dialogue Graph)
    /// </summary>
    public static class DialogueAssetOpener
    {
        [MenuItem("Assets/Open Dialogue Graph", true)]
        private static bool Validate() => Selection.activeObject is Dialogue;

        [MenuItem("Assets/Open Dialogue Graph")]
        private static void OpenSelected()
        {
            if (Selection.activeObject is Dialogue d)
                DialogueGraphWindow.Open(d);
        }
    }
}
