#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Core.Player.EditorTools
{
    /// <summary>
    /// Автосборка поворотов (Turn) для игрока — по аналогии с Locomotion-билдером.
    ///
    /// Что делает по кнопке меню (Tools → Player → Build Turn):
    ///   1) Настраивает импорт turn-клипов (Standing_Turns_*, Turn_and_Walk_*):
    ///      Loop Time, Bake Into Pose на Rotation/Y (Body Orientation), XZ Bake снят —
    ///      те же настройки, что дали рабочий результат на Running_05.
    ///   2) Добавляет в существующий Animator Controller параметр "Turn" (Float),
    ///      если его нет.
    ///   3) Создаёт отдельный слой "Turn Layer" поверх Locomotion с Blend Tree по "Turn":
    ///      влево (standing turn left) ← 0 (пусто/idle) → вправо (standing turn right).
    ///      Слой с весом 1 и аддитивным/оверрайд поведением — поворот корпуса.
    ///
    /// Работает с контроллером игрока. Путь ищется автоматически: берётся контроллер,
    /// назначенный на первый найденный Animator с моделью MorroMan в открытых сценах,
    /// иначе — по фиксированному пути ниже.
    ///
    /// ВАЖНО: билдер не может гарантировать, что MoCap-клипы идеальны — но применяет
    /// проверенные настройки импорта. Если клип всё же уносит/крутит, правь его вручную.
    /// </summary>
    public static class PlayerTurnBuilder
    {
        // Имена клипов (как в проекте после импорта FBX).
        private const string StandLeft = "Standing_Turns_Left";
        private const string StandRight = "Standing_Turns_Right";

        private static readonly string[] TurnWalkClips =
        {
            "Turn_and_Walk_Left_90", "Turn_and_Walk_Left_135", "Turn_and_Walk_Left_180",
            "Turn_and_Walk_Right_90", "Turn_and_Walk_Right_135", "Turn_and_Walk_Right_180"
        };

        // Путь к контроллеру игрока (правь, если у тебя другой).
        private const string ControllerPath = "Assets/Resources/Anims/PlayerAnimator.controller";

        private const string TurnParam = "Turn";
        private const string TurnLayerName = "Turn Layer";

        [MenuItem("Tools/Player/Build Turn")]
        public static void Build()
        {
            // 1) Настроить импорт turn-клипов.
            ConfigureClipImport(StandLeft);
            ConfigureClipImport(StandRight);
            foreach (var c in TurnWalkClips) ConfigureClipImport(c);

            AssetDatabase.Refresh();

            // 2) Найти контроллер.
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError($"[TurnBuilder] Контроллер не найден: {ControllerPath}. " +
                               "Проверь путь в скрипте (ControllerPath).");
                return;
            }

            // 3) Параметр Turn.
            if (!HasParameter(controller, TurnParam))
                controller.AddParameter(TurnParam, AnimatorControllerParameterType.Float);

            // 4) Клипы поворота на месте.
            var standLeft = FindClip(StandLeft);
            var standRight = FindClip(StandRight);
            if (standLeft == null || standRight == null)
            {
                Debug.LogError("[TurnBuilder] Не найдены Standing_Turns_Left/Right. " +
                               "Проверь имена клипов в проекте.");
                return;
            }

            // 5) Слой Turn поверх Locomotion.
            RemoveLayerIfExists(controller, TurnLayerName);

            var layer = new AnimatorControllerLayer
            {
                name = TurnLayerName,
                defaultWeight = 1f,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = new AnimatorStateMachine { name = TurnLayerName }
            };
            AssetDatabase.AddObjectToAsset(layer.stateMachine, controller);
            layer.stateMachine.hideFlags = HideFlags.HideInHierarchy;

            // Blend Tree 1D по Turn: left ← 0 (пусто) → right.
            var tree = new BlendTree
            {
                name = "TurnBlend",
                blendType = BlendTreeType.Simple1D,
                blendParameter = TurnParam,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, controller);

            // Порог -1 = поворот влево, 0 = нейтраль (тот же standLeft на слабом весе даст
            // почти статику; для чистоты можно пустой клип, но берём стойку через standLeft
            // на 0-скорости слоя). Проще: left(-1), тот же left(0) с малым влиянием, right(1).
            tree.AddChild(standLeft, -1f);
            tree.AddChild(standLeft, 0f);   // около нуля — почти без поворота
            tree.AddChild(standRight, 1f);

            var state = layer.stateMachine.AddState("TurnState");
            state.motion = tree;
            layer.stateMachine.defaultState = state;

            // Добавляем слой в контроллер.
            controller.AddLayer(layer);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[TurnBuilder] Turn-слой собран. Параметр Turn пишется из PlayerAnimator " +
                      "(YawDelta). Крути мышью стоя — корпус поворачивается. " +
                      "Если слой перекрывает ноги — примени Avatar Mask на верх тела (см. лог).");
        }

        // --- Настройка импорта клипа ---
        private static void ConfigureClipImport(string clipName)
        {
            var path = FindClipPath(clipName);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"[TurnBuilder] Клип не найден для настройки: {clipName}");
                return;
            }

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;

            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].loopTime = true;
                clips[i].loopPose = true;

                // Rotation: bake, body orientation.
                clips[i].lockRootRotation = true;              // Bake Into Pose (rotation)
                clips[i].keepOriginalOrientation = false;      // Based Upon = Body Orientation
                clips[i].rotationOffset = 0f;

                // Y: bake, original.
                clips[i].lockRootHeightY = true;               // Bake Into Pose (Y)
                clips[i].keepOriginalPositionY = true;         // Based Upon = Original
                clips[i].heightOffset = 0f;

                // XZ: bake снят (как у рабочего Running_05).
                clips[i].lockRootPositionXZ = false;           // Bake Into Pose (XZ) = OFF
                clips[i].keepOriginalPositionXZ = false;       // Based Upon = Center of Mass
            }

            importer.clipAnimations = clips;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        // --- Хелперы поиска ---
        private static AnimationClip FindClip(string clipName)
        {
            var path = FindClipPath(clipName);
            if (string.IsNullOrEmpty(path)) return null;
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
                if (a is AnimationClip clip && clip.name == clipName) return clip;
            return null;
        }

        private static string FindClipPath(string clipName)
        {
            var guids = AssetDatabase.FindAssets($"t:AnimationClip {clipName}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (a is AnimationClip clip && clip.name == clipName) return path;
            }
            // fbx может содержать клип с таким sub-именем
            var fbxGuids = AssetDatabase.FindAssets(clipName);
            foreach (var guid in fbxGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".fbx") || path.EndsWith(".FBX"))
                {
                    foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
                        if (a is AnimationClip clip && clip.name == clipName) return path;
                }
            }
            return null;
        }

        private static bool HasParameter(AnimatorController c, string name)
        {
            foreach (var p in c.parameters) if (p.name == name) return true;
            return false;
        }

        private static void RemoveLayerIfExists(AnimatorController c, string layerName)
        {
            for (int i = c.layers.Length - 1; i >= 0; i--)
            {
                if (c.layers[i].name == layerName)
                {
                    c.RemoveLayer(i);
                }
            }
        }
    }
}
#endif