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

        [CliCommand("duplicate_gameobject", "Duplicate one or more GameObjects atomically. The default path uses Unity Editor's canonical duplicate command, preserving its naming and selection behaviour. Blocked during Play mode.")]
        public static DuplicateGameObjectsResult DuplicateGameObject(
            [CliArg("sources", "GameObjects to duplicate. All references are resolved before mutation, so an invalid entry leaves the scene unchanged.")] ObjectRef[] sources = null,
            [CliArg("source", "Backward-compatible single-object alias for sources.")] ObjectRef source = null,
            [CliArg("name", "Optional name for the duplicated GameObject.")] string name = null,
            [CliArg("parent", "Optional parent for the duplicate. Omit to keep the source parent.")] ObjectRef parent = null,
            [CliArg("world_position_stays", "When a parent is supplied, preserve the duplicate's world position (default true).")] bool worldPositionStays = true)
        {
            GuardNotPlaying("duplicate_gameobject");
            if (sources != null && sources.Length > 0 && source != null && !source.IsEmpty)
                throw new ArgumentException("Provide either sources or source, not both.");
            if ((sources == null || sources.Length == 0) && (source == null || source.IsEmpty))
                throw new ArgumentException("duplicate_gameobject requires at least one source reference.");

            var requestedSources = sources != null && sources.Length > 0 ? sources : new[] { source };
            // Resolve the complete batch before touching selection or the scene, preserving the
            // original Unity-MCP atomic failure behaviour.
            var sourceGos = requestedSources
                .Select((reference, index) => ResolveGameObject(reference, $"sources[{index}]"))
                .ToArray();

            var hasCustomPlacement = !string.IsNullOrWhiteSpace(name) || (parent != null && !parent.IsEmpty);
            if (hasCustomPlacement && sourceGos.Length != 1)
                throw new ArgumentException("name and parent options are supported only when duplicating one source.");

            if (!hasCustomPlacement)
            {
                Selection.objects = sourceGos;
                Unsupported.DuplicateGameObjectsUsingPasteboard();
                foreach (var duplicate in Selection.gameObjects)
                    EditorSceneManager.MarkSceneDirty(duplicate.scene);

                return new DuplicateGameObjectsResult
                {
                    Result = sourceGos.Select(DescribeCompatibilityReference).ToArray()
                };
            }

            var sourceGo = sourceGos[0];
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
                Selection.activeGameObject = duplicate;
                return new DuplicateGameObjectsResult
                {
                    Result = new[] { DescribeCompatibilityReference(sourceGo) }
                };
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

        private static CompatibilityGameObjectReference DescribeCompatibilityReference(GameObject gameObject)
        {
            return new CompatibilityGameObjectReference
            {
                InstanceId = PipelineUtils.GetObjectId(gameObject),
                Path = GetHierarchyPath(gameObject.transform),
                Name = gameObject.name
            };
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            for (var current = transform; current != null; current = current.parent)
                names.Push(current.name);
            return string.Join("/", names);
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

    [Serializable]
    public sealed class DuplicateGameObjectsResult
    {
        [JsonProperty("result")] public CompatibilityGameObjectReference[] Result { get; set; }
    }

    [Serializable]
    public sealed class CompatibilityGameObjectReference
    {
        [JsonProperty("instanceID")] public int InstanceId { get; set; }
        [JsonProperty("path")] public string Path { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
    }
}
