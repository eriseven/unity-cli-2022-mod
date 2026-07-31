using System;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Extensions.Editor.Commands.Prefabs
{
    /// <summary>
    /// Commands for the interactive Prefab Stage.  For unattended declarative changes prefer
    /// <c>save_prefab_contents</c>; these commands are useful when a subsequent Pipeline command
    /// needs to operate in the currently open stage.
    /// </summary>
    public static class PrefabStageCommands
    {
        [CliCommand("open_prefab_stage", "Open a prefab asset or prefab instance in Unity's Prefab Stage. Asset edits made there affect every instance after saving.")]
        public static PrefabStageResult Open(
            [CliArg("prefab", "Reference to a prefab asset or scene prefab instance.", Required = true)] ObjectRef prefab)
        {
            var gameObject = ResolveGameObject(prefab);
            var assetPath = GetPrefabAssetPath(gameObject);
            var current = PrefabStageUtility.GetCurrentPrefabStage();
            if (current != null && string.Equals(current.assetPath, assetPath, StringComparison.OrdinalIgnoreCase))
                return DescribeStage(current, "already_open");

            var stage = AssetDatabase.Contains(gameObject)
                ? PrefabStageUtility.OpenPrefab(assetPath)
                : PrefabStageUtility.OpenPrefab(assetPath, gameObject);
            if (stage == null)
                throw new InvalidOperationException($"Failed to open Prefab Stage for '{assetPath}'.");

            return DescribeStage(stage, "opened");
        }

        [CliCommand("save_prefab_stage", "Save the currently open Prefab Stage back to its prefab asset without closing it.")]
        public static PrefabStageResult Save()
        {
            var stage = RequireCurrentStage();
            var root = stage.prefabContentsRoot;
            if (root == null)
                throw new InvalidOperationException("The current Prefab Stage has no prefab contents root.");

            PrefabUtility.SaveAsPrefabAsset(root, stage.assetPath, out var success);
            if (!success)
                throw new InvalidOperationException($"Failed to save Prefab Stage asset '{stage.assetPath}'.");

            stage.ClearDirtiness();
            AssetDatabase.SaveAssets();
            return DescribeStage(stage, "saved");
        }

        [CliCommand("close_prefab_stage", "Close the current Prefab Stage. Saving is the default; discarding unsaved stage edits requires confirm=true.")]
        public static PrefabStageResult Close(
            [CliArg("save", "Save the prefab asset before returning to the previous stage (default true).", DefaultValue = true)] bool save = true,
            [CliArg("confirm", "Required only when save=false because unsaved Prefab Stage changes will be discarded.")] bool confirm = false)
        {
            var stage = RequireCurrentStage();
            var result = DescribeStage(stage, save ? "closed_saved" : "closed_discarded");

            if (save)
                Save();
            else if (!confirm)
                throw new ArgumentException("close_prefab_stage with save=false requires confirm=true because it discards unsaved prefab edits.");

            stage.ClearDirtiness();
            StageUtility.GoBackToPreviousStage();
            return result;
        }

        private static PrefabStage RequireCurrentStage()
        {
            return PrefabStageUtility.GetCurrentPrefabStage()
                ?? throw new InvalidOperationException("No Prefab Stage is currently open. Call open_prefab_stage first.");
        }

        private static GameObject ResolveGameObject(ObjectRef reference)
        {
            if (!ObjectResolver.TryResolve(reference, out var obj, out var error))
                throw new ArgumentException($"Could not resolve prefab: {error}");

            var gameObject = obj as GameObject ?? (obj as Component)?.gameObject;
            if (gameObject == null)
                throw new ArgumentException("prefab must resolve to a GameObject or Component.");
            return gameObject;
        }

        private static string GetPrefabAssetPath(GameObject gameObject)
        {
            var path = AssetDatabase.GetAssetPath(gameObject);
            if (string.IsNullOrEmpty(path))
                path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException($"GameObject '{gameObject.name}' is not a prefab asset or a prefab instance.");

            var confined = ProjectPaths.Resolve(path, out var pathError);
            if (confined == null || !string.Equals(confined, path, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Prefab asset '{path}' is outside the configured authoring root: {pathError}");
            return path;
        }

        private static PrefabStageResult DescribeStage(PrefabStage stage, string action)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(stage.assetPath);
            return new PrefabStageResult
            {
                Action = action,
                AssetPath = stage.assetPath,
                IsDirty = stage.prefabContentsRoot != null && stage.prefabContentsRoot.scene.isDirty,
                Prefab = ObjectResolver.Describe(asset)
            };
        }
    }

    [Serializable]
    public sealed class PrefabStageResult
    {
        public string Action { get; set; }
        public string AssetPath { get; set; }
        public bool IsDirty { get; set; }
        public AuthoringResult Prefab { get; set; }
    }
}
