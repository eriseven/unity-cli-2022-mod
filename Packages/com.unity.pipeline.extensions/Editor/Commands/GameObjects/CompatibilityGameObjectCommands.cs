using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Extensions.Editor.Commands.GameObjects
{
    /// <summary>Commands that complement the core GameObject authoring surface.</summary>
    public static class CompatibilityGameObjectCommands
    {
        private const int MaxComponentTypes = 500;

        [CliCommand("duplicate_gameobject", "Duplicate a GameObject, optionally rename or reparent the copy. Undo-able; blocked during Play mode.")]
        public static AuthoringResult DuplicateGameObject(
            [CliArg("source", "Reference to the GameObject to duplicate.", Required = true)] ObjectRef source,
            [CliArg("name", "Optional name for the duplicated GameObject.")] string name = null,
            [CliArg("parent", "Optional parent for the duplicate. Omit to keep the source parent.")] ObjectRef parent = null,
            [CliArg("world_position_stays", "When a parent is supplied, preserve the duplicate's world position (default true).")] bool worldPositionStays = true)
        {
            GuardNotPlaying("duplicate_gameobject");
            var sourceGo = ResolveGameObject(source, "source");
            var parentGo = parent == null || parent.IsEmpty ? null : ResolveGameObject(parent, "parent");
            var sourceLocalPosition = sourceGo.transform.localPosition;
            var sourceLocalRotation = sourceGo.transform.localRotation;
            var sourceLocalScale = sourceGo.transform.localScale;
            var sourceWorldPosition = sourceGo.transform.position;
            var sourceWorldRotation = sourceGo.transform.rotation;

            using (new AuthoringUndoScope("Duplicate GameObject"))
            {
                var duplicate = Object.Instantiate(sourceGo);
                Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate GameObject");

                if (parentGo != null)
                    Undo.SetTransformParent(duplicate.transform, parentGo.transform, "Duplicate GameObject");

                if (parentGo != null)
                {
                    if (worldPositionStays)
                    {
                        duplicate.transform.position = sourceWorldPosition;
                        duplicate.transform.rotation = sourceWorldRotation;
                    }
                    else
                    {
                        duplicate.transform.localPosition = sourceLocalPosition;
                        duplicate.transform.localRotation = sourceLocalRotation;
                        duplicate.transform.localScale = sourceLocalScale;
                    }
                }

                if (!string.IsNullOrWhiteSpace(name))
                    duplicate.name = name;

                EditorSceneManager.MarkSceneDirty(duplicate.scene);
                return ObjectResolver.Describe(duplicate) ?? new AuthoringResult { Type = nameof(GameObject) };
            }
        }

        [CliCommand("list_component_types", "List concrete Unity Component type names available in loaded assemblies (paginated, read-only).")]
        public static ComponentTypeListResult ListComponentTypes(
            [CliArg("search", "Optional case-insensitive substring filter for the full type name.")] string search = null,
            [CliArg("page", "Zero-based result page (default 0). ")] int page = 0,
            [CliArg("page_size", "Items per page (1-500, default 50). ")] int pageSize = 50)
        {
            page = Math.Max(0, page);
            pageSize = Mathf.Clamp(pageSize, 1, MaxComponentTypes);

            var types = new List<string>();
            foreach (var assembly in PipelineUtils.GetLoadedAssemblies())
            {
                Type[] assemblyTypes;
                try { assemblyTypes = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { assemblyTypes = ex.Types.Where(t => t != null).ToArray(); }

                types.AddRange(assemblyTypes
                    .Where(t => t != null && typeof(Component).IsAssignableFrom(t) && !t.IsAbstract && !string.IsNullOrEmpty(t.FullName))
                    .Select(t => t.FullName));
            }

            var filtered = types
                .Distinct(StringComparer.Ordinal)
                .Where(t => string.IsNullOrWhiteSpace(search) || t.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToArray();

            return new ComponentTypeListResult
            {
                Items = filtered.Skip(page * pageSize).Take(pageSize).ToArray(),
                Page = page,
                PageSize = pageSize,
                TotalCount = filtered.Length,
                TotalPages = filtered.Length == 0 ? 0 : (int)Math.Ceiling(filtered.Length / (double)pageSize)
            };
        }

        private static GameObject ResolveGameObject(ObjectRef handle, string parameter)
        {
            if (!ObjectResolver.TryResolve(handle, out var obj, out var error))
                throw new ArgumentException($"Could not resolve {parameter}: {error}");

            var go = obj as GameObject ?? (obj as Component)?.gameObject;
            if (go == null)
                throw new ArgumentException($"{parameter} '{handle}' does not resolve to a GameObject.");
            return go;
        }

        private static void GuardNotPlaying(string command)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException($"'{command}' cannot run while the Editor is in or entering Play mode.");
        }
    }

    [Serializable]
    public sealed class ComponentTypeListResult
    {
        [JsonProperty("items")] public string[] Items { get; set; }
        [JsonProperty("page")] public int Page { get; set; }
        [JsonProperty("pageSize")] public int PageSize { get; set; }
        [JsonProperty("totalCount")] public int TotalCount { get; set; }
        [JsonProperty("totalPages")] public int TotalPages { get; set; }
    }
}
