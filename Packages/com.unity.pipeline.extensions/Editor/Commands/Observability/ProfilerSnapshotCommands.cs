using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using UnityEditor;

namespace Unity.Pipeline.Extensions.Editor.Commands.Observability
{
    /// <summary>Persist the structured profiler snapshots produced by <see cref="ProfilerControlCommands"/>.</summary>
    public static class ProfilerSnapshotCommands
    {
        private const long MaxLoadBytes = 10L * 1024L * 1024L;

        [CliCommand("profiler_save_data", "Save a structured Pipeline Profiler snapshot as JSON under the authoring root. This is not Unity's binary .data profiler recording format.")]
        public static ProfilerSnapshotFileResult Save(
            [CliArg("path", "Project-relative .json path under the authoring root, e.g. Profiling/run-001.json.", Required = true)] string path,
            [CliArg("confirm", "Required only when overwriting an existing snapshot file.")] bool confirm = false,
            [CliArg("dry_run", "Validate the path and overwrite requirement without writing a file.")] bool dryRun = false)
        {
            var resolved = ResolveSnapshotPath(path);
            var exists = File.Exists(ToAbsolute(resolved));
            if (exists && !confirm)
                throw new ArgumentException($"Snapshot '{resolved}' already exists. Pass confirm=true to overwrite it.");
            if (dryRun)
                return new ProfilerSnapshotFileResult { Path = resolved, Exists = exists, Saved = false };

            var snapshot = new
            {
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                status = ProfilerControlCommands.Status(),
                memory = ProfilerControlCommands.GetMemoryStats(),
                rendering = ProfilerControlCommands.GetRenderStats(),
                script = ProfilerControlCommands.GetScriptStats(),
                frame = ProfilerControlCommands.CaptureFrame()
            };

            var absolute = ToAbsolute(resolved);
            var directory = Path.GetDirectoryName(absolute);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(absolute, JsonConvert.SerializeObject(snapshot, Formatting.Indented));
            AssetDatabase.Refresh();
            return new ProfilerSnapshotFileResult { Path = resolved, Exists = exists, Saved = true };
        }

        [CliCommand("profiler_load_data", "Load and parse a structured Pipeline Profiler snapshot JSON file from the authoring root. Files over 10 MiB are rejected.")]
        public static object Load(
            [CliArg("path", "Project-relative .json snapshot path under the authoring root.", Required = true)] string path)
        {
            var resolved = ResolveSnapshotPath(path);
            var absolute = ToAbsolute(resolved);
            if (!File.Exists(absolute))
                throw new ArgumentException($"Profiler snapshot '{resolved}' does not exist.");

            var info = new FileInfo(absolute);
            if (info.Length > MaxLoadBytes)
                throw new ArgumentException($"Profiler snapshot '{resolved}' is {info.Length} bytes, above the 10 MiB limit.");
            try
            {
                return JToken.Parse(File.ReadAllText(absolute));
            }
            catch (JsonReaderException exception)
            {
                throw new ArgumentException($"Profiler snapshot '{resolved}' is not valid JSON: {exception.Message}");
            }
        }

        private static string ResolveSnapshotPath(string path)
        {
            var resolved = ProjectPaths.Resolve(path, out var error);
            if (resolved == null)
                throw new ArgumentException(error);
            if (!resolved.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Profiler snapshot path must end in .json.");
            return resolved;
        }

        private static string ToAbsolute(string projectRelative) => Path.Combine(ProjectPaths.ProjectRoot, projectRelative);
    }

    [Serializable]
    public sealed class ProfilerSnapshotFileResult
    {
        [JsonProperty("path")] public string Path { get; set; }
        [JsonProperty("exists")] public bool Exists { get; set; }
        [JsonProperty("saved")] public bool Saved { get; set; }
    }
}
