using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Extensions.Editor.Commands.Navigation
{
    /// <summary>
    /// Actual AI Navigation baking support. This deliberately has a distinct command name because
    /// com.unity.pipeline owns bake_navmesh_surfaces as a v1 placeholder, and the Pipeline registry
    /// does not provide a deterministic extension-overrides-core precedence rule for duplicate names.
    /// </summary>
    public static class NavMeshSurfaceBakeCommands
    {
        [CliCommand("bake_navmesh_surfaces_compat", "Bake or clear AI Navigation NavMeshSurface data. Unlike the legacy bake_navmesh command, this uses each surface's configured collection, modifiers, and build settings.")]
        public static NavMeshSurfaceBakeResult BakeNavMeshSurfaces(
            [CliArg("target", "Optional reference to a NavMeshSurface component or its GameObject. Omit to process every NavMeshSurface in loaded scenes.")] ObjectRef target = null,
            [CliArg("clear", "If true, remove generated NavMeshData instead of building it.")] bool clear = false,
            [CliArg("dry_run", "If true, return the surfaces that would be processed without changing NavMesh data.")] bool dryRun = false)
        {
            var surfaces = ResolveSurfaces(target);
            if (surfaces.Count == 0)
                return new NavMeshSurfaceBakeResult { DryRun = dryRun, Cleared = clear, Surfaces = Array.Empty<NavMeshSurfaceBakeItem>() };

            var results = new List<NavMeshSurfaceBakeItem>(surfaces.Count);
            foreach (var surface in surfaces)
            {
                var hadData = surface.navMeshData != null;
                if (!dryRun)
                {
                    Undo.RegisterCompleteObjectUndo(surface, clear ? "Clear NavMesh Surface" : "Bake NavMesh Surface");
                    if (clear)
                    {
                        surface.RemoveData();
                        surface.navMeshData = null;
                    }
                    else
                    {
                        surface.BuildNavMesh();
                        if (surface.navMeshData != null)
                            EditorUtility.SetDirty(surface.navMeshData);
                    }

                    EditorUtility.SetDirty(surface);
                    if (surface.gameObject.scene.IsValid())
                        EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);
                }

                results.Add(new NavMeshSurfaceBakeItem
                {
                    Surface = ObjectResolver.Describe(surface),
                    GameObject = ObjectResolver.Describe(surface.gameObject),
                    HadNavMeshData = hadData,
                    HasNavMeshData = surface.navMeshData != null
                });
            }

            return new NavMeshSurfaceBakeResult { DryRun = dryRun, Cleared = clear, Surfaces = results.ToArray() };
        }

        static List<NavMeshSurface> ResolveSurfaces(ObjectRef target)
        {
            if (target == null)
            {
                return Object.FindObjectsOfType<NavMeshSurface>(true)
                    .Where(surface => surface != null && surface.gameObject.scene.IsValid() && surface.gameObject.scene.isLoaded)
                    .ToList();
            }

            if (!ObjectResolver.TryResolve(target, out var resolved, out var error))
                throw new ArgumentException(error, nameof(target));

            var surface = resolved as NavMeshSurface
                ?? (resolved as Component)?.GetComponent<NavMeshSurface>()
                ?? (resolved as GameObject)?.GetComponent<NavMeshSurface>();
            if (surface == null)
                throw new ArgumentException("Target must be a NavMeshSurface component or a GameObject that contains one.", nameof(target));

            return new List<NavMeshSurface> { surface };
        }
    }

    [Serializable]
    public sealed class NavMeshSurfaceBakeResult
    {
        public bool DryRun { get; set; }
        public bool Cleared { get; set; }
        public NavMeshSurfaceBakeItem[] Surfaces { get; set; }
    }

    [Serializable]
    public sealed class NavMeshSurfaceBakeItem
    {
        public AuthoringResult Surface { get; set; }
        public AuthoringResult GameObject { get; set; }
        public bool HadNavMeshData { get; set; }
        public bool HasNavMeshData { get; set; }
    }
}
