using System.IO;
using Core.Flags;
using Core.Story;
using Core.Story.Phases;
using UnityEditor;
using UnityEngine;

namespace PhaseGraphEditor
{
    /// <summary>
    /// Создание ассетов прямо из графа фаз: варианты, спавны, условия, триггеры.
    /// Кладёт файлы рядом с PhaseGraph — в подпапки по типу, чтобы не искать вручную.
    /// </summary>
    public static class PhaseAssetFactory
    {
        /// <summary>Папка, где лежит граф (базовая для всех создаваемых ассетов).</summary>
        private static string BaseFolder(PhaseGraph graph)
        {
            string path = AssetDatabase.GetAssetPath(graph);
            return string.IsNullOrEmpty(path) ? "Assets" : Path.GetDirectoryName(path).Replace('\\', '/');
        }

        private static string EnsureSubFolder(PhaseGraph graph, string sub)
        {
            string root = BaseFolder(graph);
            string full = $"{root}/{sub}";
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(root, sub);
            return full;
        }

        private static T Create<T>(PhaseGraph graph, string sub, string name) where T : ScriptableObject
        {
            string folder = EnsureSubFolder(graph, sub);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.asset");

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        // ---------- фабрики ----------

        public static PhaseVariant CreateVariant(PhaseGraph graph, int step, string variantId)
        {
            var v = Create<PhaseVariant>(graph, "Variants", $"Phase_{step}{variantId}");
            v.step = step;
            v.variantId = variantId;
            EditorUtility.SetDirty(v);

            graph.variants.Add(v);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            return v;
        }

        public static TriggerCondition CreateCondition(PhaseGraph graph, string name = "Condition")
            => Create<TriggerCondition>(graph, "Conditions", name);

        public static TriggerDefinition CreateTrigger(PhaseGraph graph, string id = "new_trigger")
        {
            var t = Create<TriggerDefinition>(graph, "Triggers", $"Trigger_{id}");
            t.id = id;
            EditorUtility.SetDirty(t);
            AssetDatabase.SaveAssets();
            return t;
        }

        public static T CreateSpawn<T>(PhaseGraph graph, string name) where T : PhaseSpawn
            => Create<T>(graph, "Spawns", name);

        /// <summary>Удалить ассет с диска (для кнопок «удалить» в графе).</summary>
        public static void DeleteAsset(Object asset)
        {
            if (asset == null) return;
            string path = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(path)) AssetDatabase.DeleteAsset(path);
        }
    }
}
