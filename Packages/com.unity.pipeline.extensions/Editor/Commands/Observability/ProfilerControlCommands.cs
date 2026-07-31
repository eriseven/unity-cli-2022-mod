using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Commands.Observability;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Pipeline.Extensions.Editor.Commands.Observability
{
    /// <summary>
    /// Small, scriptable controls for Unity's built-in Profiler.  They deliberately do not attempt
    /// to mirror the Profiler window's private module configuration; the enabled-module list is
    /// Pipeline session metadata that lets an agent record its intended inspection focus.
    /// </summary>
    public static class ProfilerControlCommands
    {
        private static readonly HashSet<string> EnabledModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CPU Usage", "GPU Usage", "Rendering", "Memory"
        };

        private static readonly string[] KnownModules =
        {
            "CPU Usage", "GPU Usage", "Rendering", "Memory", "Audio", "Physics", "Physics 2D", "Network", "Video"
        };

        [CliCommand("profiler_start", "Enable Unity's built-in profiler recording for this Editor/Player process.")]
        public static ProfilerControlResult Start()
        {
            Profiler.enabled = true;
            return GetControlResult("started");
        }

        [CliCommand("profiler_stop", "Disable Unity's built-in profiler recording for this Editor/Player process.")]
        public static ProfilerControlResult Stop()
        {
            Profiler.enabled = false;
            return GetControlResult("stopped");
        }

        [CliCommand("profiler_clear_data", "Clear recorded frames from Unity's built-in Profiler. This only affects transient Profiler data.")]
        public static ProfilerControlResult ClearData(
            [CliArg("confirm", "Must be true because this discards the current in-memory Profiler capture.", Required = true)] bool confirm = false)
        {
            if (!confirm)
                throw new ArgumentException("profiler_clear_data requires confirm=true because it discards transient Profiler frames.");

            ProfilerDriver.ClearAllFrames();
            return GetControlResult("cleared");
        }

        [CliCommand("profiler_status", "Get the current built-in Profiler recording state and Pipeline module preferences.")]
        public static ProfilerControlResult Status()
        {
            return GetControlResult("status");
        }

        [CliCommand("profiler_list_modules", "List known Profiler module names and this Pipeline session's enabled-module preferences.")]
        public static ProfilerModulesResult ListModules()
        {
            return new ProfilerModulesResult
            {
                Modules = KnownModules
                    .Select(name => new ProfilerModuleResult { Name = name, Enabled = EnabledModules.Contains(name) })
                    .ToArray(),
                Note = "enabled is Pipeline session metadata; Unity's Profiler window owns its own module UI state."
            };
        }

        [CliCommand("profiler_enable_module", "Set a Pipeline session preference for a Profiler module. It does not change Unity's private Profiler-window layout.")]
        public static ProfilerModulesResult SetModuleEnabled(
            [CliArg("module", "One of the names returned by profiler_list_modules.", Required = true)] string module,
            [CliArg("enabled", "Whether this module is enabled in the Pipeline session preference.", DefaultValue = true)] bool enabled = true)
        {
            var known = KnownModules.FirstOrDefault(name => string.Equals(name, module, StringComparison.OrdinalIgnoreCase));
            if (known == null)
                throw new ArgumentException($"Unknown Profiler module '{module}'. Use profiler_list_modules first.");

            if (enabled)
                EnabledModules.Add(known);
            else
                EnabledModules.Remove(known);

            return ListModules();
        }

        [CliCommand("profiler_capture_frame", "Capture a structured, immediate performance snapshot; it does not serialize a Unity .data Profiler recording.")]
        public static ProfilerFrameSnapshot CaptureFrame()
        {
            return new ProfilerFrameSnapshot
            {
                CapturedAtUtc = DateTime.UtcNow.ToString("O"),
                Recording = Profiler.enabled,
                Render = new RenderStats
                {
                    DrawCalls = UnityStats.drawCalls,
                    Batches = UnityStats.dynamicBatches + UnityStats.staticBatches + UnityStats.instancedBatches,
                    SetPassCalls = UnityStats.setPassCalls,
                    Triangles = UnityStats.triangles,
                    Vertices = UnityStats.vertices
                },
                Memory = new MemoryStats
                {
                    TotalAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(),
                    TotalReservedBytes = Profiler.GetTotalReservedMemoryLong(),
                    MonoUsedBytes = Profiler.GetMonoUsedSizeLong(),
                    MonoHeapBytes = Profiler.GetMonoHeapSizeLong()
                },
                Script = GetScriptStats()
            };
        }

        [CliCommand("profiler_get_memory_stats", "Read Unity allocation and managed-heap counters from the built-in Profiler.")]
        public static MemoryStats GetMemoryStats()
        {
            return CaptureFrame().Memory;
        }

        [CliCommand("profiler_get_render_stats", "Read rendering counters for the most recently rendered Editor frame.")]
        public static RenderStats GetRenderStats()
        {
            return CaptureFrame().Render;
        }

        [CliCommand("profiler_get_script_stats", "Read managed-memory and frame-time counters relevant to script performance.")]
        public static ProfilerScriptStats GetScriptStats()
        {
            return new ProfilerScriptStats
            {
                MonoUsedBytes = Profiler.GetMonoUsedSizeLong(),
                MonoHeapBytes = Profiler.GetMonoHeapSizeLong(),
                GcUsedBytes = GC.GetTotalMemory(false),
                FrameTimeMs = Time.unscaledDeltaTime * 1000f,
                FrameRate = Time.unscaledDeltaTime > 0f ? 1f / Time.unscaledDeltaTime : 0f
            };
        }

        private static ProfilerControlResult GetControlResult(string action)
        {
            return new ProfilerControlResult
            {
                Action = action,
                Recording = Profiler.enabled,
                MaxUsedMemoryBytes = Profiler.maxUsedMemory,
                Supported = Profiler.supported,
                EnabledModules = EnabledModules.OrderBy(name => name, StringComparer.Ordinal).ToArray()
            };
        }
    }

    [Serializable]
    public sealed class ProfilerControlResult
    {
        [JsonProperty("action")] public string Action { get; set; }
        [JsonProperty("recording")] public bool Recording { get; set; }
        [JsonProperty("maxUsedMemoryBytes")] public int MaxUsedMemoryBytes { get; set; }
        [JsonProperty("supported")] public bool Supported { get; set; }
        [JsonProperty("enabledModules")] public string[] EnabledModules { get; set; }
    }

    [Serializable]
    public sealed class ProfilerModulesResult
    {
        [JsonProperty("modules")] public ProfilerModuleResult[] Modules { get; set; }
        [JsonProperty("note")] public string Note { get; set; }
    }

    [Serializable]
    public sealed class ProfilerModuleResult
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("enabled")] public bool Enabled { get; set; }
    }

    [Serializable]
    public sealed class ProfilerFrameSnapshot
    {
        [JsonProperty("capturedAtUtc")] public string CapturedAtUtc { get; set; }
        [JsonProperty("recording")] public bool Recording { get; set; }
        [JsonProperty("render")] public RenderStats Render { get; set; }
        [JsonProperty("memory")] public MemoryStats Memory { get; set; }
        [JsonProperty("script")] public ProfilerScriptStats Script { get; set; }
    }

    [Serializable]
    public sealed class ProfilerScriptStats
    {
        [JsonProperty("monoUsedBytes")] public long MonoUsedBytes { get; set; }
        [JsonProperty("monoHeapBytes")] public long MonoHeapBytes { get; set; }
        [JsonProperty("gcUsedBytes")] public long GcUsedBytes { get; set; }
        [JsonProperty("frameTimeMs")] public float FrameTimeMs { get; set; }
        [JsonProperty("frameRate")] public float FrameRate { get; set; }
    }
}
