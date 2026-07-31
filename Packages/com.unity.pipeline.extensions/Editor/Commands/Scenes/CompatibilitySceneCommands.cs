using System;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Unity.Pipeline.Extensions.Editor.Commands.Scenes
{
    /// <summary>Scene lifecycle commands that require an explicit data-loss acknowledgement.</summary>
    public static class CompatibilitySceneCommands
    {
        [CliCommand("unload_scene", "Close an additively opened scene. Dirty scenes require save_before_unload=true or confirm=true to discard changes.")]
        public static UnloadSceneResult UnloadScene(
            [CliArg("path", "Path of an already-open scene, relative to the authoring root (Assets/ prefix and .unity optional).", Required = true)] string path,
            [CliArg("save_before_unload", "Save a dirty scene before closing it (default false). ")] bool saveBeforeUnload = false,
            [CliArg("confirm", "Acknowledge discarding a dirty scene when save_before_unload is false.")] bool confirm = false,
            [CliArg("dry_run", "Validate the target and report the intended action without closing the scene.")] bool dryRun = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("'unload_scene' cannot run while the Editor is in or entering Play mode.");

            var normalized = ProjectPaths.Resolve(path, out var error);
            if (normalized == null)
                throw new ArgumentException(error);
            if (!normalized.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                normalized += ".unity";

            var scene = FindOpenScene(normalized);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new ArgumentException($"Scene '{normalized}' is not open and loaded.");
            if (scene == SceneManager.GetActiveScene() && SceneManager.sceneCount <= 1)
                throw new InvalidOperationException("Cannot unload the only open scene.");

            var result = new UnloadSceneResult { Path = normalized, WasDirty = scene.isDirty, Saved = false, Unloaded = false };
            if (dryRun)
                return result;
            if (scene.isDirty && !saveBeforeUnload && !confirm)
                throw new ArgumentException($"Scene '{normalized}' has unsaved changes. Pass save_before_unload=true or confirm=true to discard them.");

            if (scene.isDirty && saveBeforeUnload)
            {
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException($"Failed to save scene '{normalized}' before unloading.");
                result.Saved = true;
            }

            if (!EditorSceneManager.CloseScene(scene, true))
                throw new InvalidOperationException($"Unity refused to close scene '{normalized}'.");
            result.Unloaded = true;
            return result;
        }

        private static Scene FindOpenScene(string normalizedPath)
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (string.Equals(scene.path, normalizedPath, StringComparison.OrdinalIgnoreCase))
                    return scene;
            }
            return default;
        }
    }

    [Serializable]
    public sealed class UnloadSceneResult
    {
        [JsonProperty("path")] public string Path { get; set; }
        [JsonProperty("wasDirty")] public bool WasDirty { get; set; }
        [JsonProperty("saved")] public bool Saved { get; set; }
        [JsonProperty("unloaded")] public bool Unloaded { get; set; }
    }
}
