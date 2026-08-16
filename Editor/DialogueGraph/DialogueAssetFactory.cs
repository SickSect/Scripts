using System.IO;
using Core.Flags;
using Core.Story;
using Core.Story.Dialogue;
using UnityEditor;
using UnityEngine;

namespace DialogueGraphEditor
{
    /// <summary>
    /// Создание ассетов прямо из графа диалога: условия и триггеры.
    /// Файлы кладутся рядом с диалогом, в подпапки по типу.
    /// </summary>
    public static class DialogueAssetFactory
    {
        private static string BaseFolder(Dialogue dialogue)
        {
            string path = AssetDatabase.GetAssetPath(dialogue);
            return string.IsNullOrEmpty(path) ? "Assets" : Path.GetDirectoryName(path).Replace('\\', '/');
        }

        private static string EnsureSubFolder(Dialogue dialogue, string sub)
        {
            string root = BaseFolder(dialogue);
            string full = $"{root}/{sub}";
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(root, sub);
            return full;
        }

        private static T Create<T>(Dialogue dialogue, string sub, string name) where T : ScriptableObject
        {
            string folder = EnsureSubFolder(dialogue, sub);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.asset");

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static TriggerDefinition CreateTrigger(Dialogue dialogue, string id)
        {
            var t = Create<TriggerDefinition>(dialogue, "Triggers", $"Trigger_{id}");
            t.id = id;
            EditorUtility.SetDirty(t);
            AssetDatabase.SaveAssets();
            return t;
        }

        public static TriggerCondition CreateCondition(Dialogue dialogue, string name)
            => Create<TriggerCondition>(dialogue, "Conditions", $"Cond_{name}");

        /// <summary>
        /// Условие «этой ветки ещё не было»: НЕ выдан указанный триггер.
        /// Оставлено для совместимости; для накопления блокировок используй AddBlockTrigger.
        /// </summary>
        public static TriggerCondition CreateNotTriggerCondition(Dialogue dialogue,
                                                                 TriggerDefinition trigger,
                                                                 string name)
        {
            var cond = CreateCondition(dialogue, name);
            cond.mode = TriggerCondition.Mode.All;
            cond.requirements.Add(new TriggerCondition.Requirement
            {
                trigger = trigger,
                negate = true
            });
            EditorUtility.SetDirty(cond);
            AssetDatabase.SaveAssets();
            return cond;
        }

        /// <summary>
        /// Добавить блокировку «НЕ этот триггер» к условию (создаёт условие, если его нет).
        ///
        /// mode = All + negate у каждого требования: ветка ВИДНА, пока НИ ОДНОГО из
        /// перечисленных триггеров не выдано. Появился любой — ветка скрывается.
        /// Повторный вызов с новым триггером накапливает блокировки.
        /// </summary>
        public static TriggerCondition AddBlockTrigger(Dialogue dialogue,
                                                       TriggerCondition existing,
                                                       TriggerDefinition trigger,
                                                       string name)
        {
            if (trigger == null) return existing;

            var cond = existing != null ? existing : CreateCondition(dialogue, name);
            cond.mode = TriggerCondition.Mode.All;

            bool already = cond.requirements.Exists(r => r.trigger == trigger && r.negate);
            if (!already)
            {
                cond.requirements.Add(new TriggerCondition.Requirement
                {
                    trigger = trigger,
                    negate = true
                });
            }

            EditorUtility.SetDirty(cond);
            AssetDatabase.SaveAssets();
            return cond;
        }
    }
}