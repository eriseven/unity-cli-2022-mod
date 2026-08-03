using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Extensions.Editor.Commands.Animation
{
    /// <summary>AnimationClip operations deliberately kept outside the core Pipeline package for Unity-MCP compatibility.</summary>
    public static class AnimationClipExtensionCommands
    {
        [CliCommand("set_animation_clip_metadata", "Set supported AnimationClip metadata: frame rate, loop-time flag, WrapMode, and legacy flag.", MainThreadRequired = true)]
        public static AnimationClipDetails SetAnimationClipMetadata(
            [CliArg("clip", "Reference to an AnimationClip asset.", Required = true)] ObjectRef clip,
            [CliArg("frame_rate", "Optional positive sampling frame rate.")] float? frameRate = null,
            [CliArg("loop", "Optional AnimationClipSettings loop-time value.")] bool? loop = null,
            [CliArg("wrap_mode", "Optional WrapMode: Default, Once, Loop, PingPong, ClampForever.")] string wrapMode = null,
            [CliArg("legacy", "Optional legacy AnimationClip flag.")] bool? legacy = null,
            [CliArg("dry_run", "Validate and describe the requested result without editing the clip.")] bool dryRun = false)
        {
            var (asset, path) = ResolveClip(clip);
            if (frameRate.HasValue && frameRate.Value <= 0f)
                throw new ArgumentException("frame_rate must be positive.");
            var parsedWrapMode = ParseEnum(wrapMode, asset.wrapMode, "wrap_mode");
            var result = Describe(asset, path);
            result.FrameRate = frameRate ?? result.FrameRate;
            result.Loop = loop ?? result.Loop;
            result.WrapMode = parsedWrapMode.ToString();
            result.Legacy = legacy ?? result.Legacy;
            if (dryRun)
                return result;

            if (frameRate.HasValue) asset.frameRate = frameRate.Value;
            if (loop.HasValue)
            {
                var settings = AnimationUtility.GetAnimationClipSettings(asset);
                settings.loopTime = loop.Value;
                AnimationUtility.SetAnimationClipSettings(asset, settings);
            }
            if (!string.IsNullOrWhiteSpace(wrapMode)) asset.wrapMode = parsedWrapMode;
            if (legacy.HasValue) asset.legacy = legacy.Value;
            Save(asset);
            return Describe(asset, path);
        }

        [CliCommand("add_animation_event", "Append an AnimationEvent to an AnimationClip.", MainThreadRequired = true)]
        public static AnimationEventMutationResult AddAnimationEvent(
            [CliArg("clip", "Reference to an AnimationClip asset.", Required = true)] ObjectRef clip,
            [CliArg("time", "Event time in seconds.", Required = true)] float time,
            [CliArg("function_name", "Function name to invoke.", Required = true)] string functionName,
            [CliArg("int_parameter", "Optional integer event parameter.", DefaultValue = 0)] int intParameter = 0,
            [CliArg("float_parameter", "Optional float event parameter.", DefaultValue = 0f)] float floatParameter = 0f,
            [CliArg("string_parameter", "Optional string event parameter.")] string stringParameter = null,
            [CliArg("dry_run", "Validate and describe the event without writing it.")] bool dryRun = false)
        {
            var (asset, path) = ResolveClip(clip);
            if (time < 0f) throw new ArgumentException("time must be >= 0.");
            if (string.IsNullOrWhiteSpace(functionName)) throw new ArgumentException("function_name is required.");
            var events = new List<AnimationEvent>(AnimationUtility.GetAnimationEvents(asset));
            var animationEvent = new AnimationEvent { time = time, functionName = functionName, intParameter = intParameter, floatParameter = floatParameter, stringParameter = stringParameter ?? string.Empty };
            var result = new AnimationEventMutationResult { AssetPath = path, EventCount = events.Count + 1, Event = DescribeEvent(animationEvent) };
            if (dryRun) return result;
            events.Add(animationEvent);
            AnimationUtility.SetAnimationEvents(asset, events.ToArray());
            Save(asset);
            return result;
        }

        [CliCommand("clear_animation_events", "Remove every AnimationEvent from an AnimationClip. Requires confirm=true.", MainThreadRequired = true)]
        public static AnimationEventMutationResult ClearAnimationEvents(
            [CliArg("clip", "Reference to an AnimationClip asset.", Required = true)] ObjectRef clip,
            [CliArg("confirm", "Must be true because all AnimationEvents are removed.", Required = true)] bool confirm = false,
            [CliArg("dry_run", "Validate and report the deletion without writing it.")] bool dryRun = false)
        {
            var (asset, path) = ResolveClip(clip);
            var result = new AnimationEventMutationResult { AssetPath = path, EventCount = 0, RemovedCount = AnimationUtility.GetAnimationEvents(asset).Length };
            if (dryRun) return result;
            if (!confirm) throw new ArgumentException("clear_animation_events requires confirm=true.");
            AnimationUtility.SetAnimationEvents(asset, Array.Empty<AnimationEvent>());
            Save(asset);
            return result;
        }

        [CliCommand("get_animation_clip_details", "Read AnimationClip metadata, float/object-reference bindings, and AnimationEvents.", MainThreadRequired = true)]
        public static AnimationClipDetails GetAnimationClipDetails(
            [CliArg("clip", "Reference to an AnimationClip asset.", Required = true)] ObjectRef clip)
        {
            var (asset, path) = ResolveClip(clip);
            return Describe(asset, path);
        }

        private static AnimationClipDetails Describe(AnimationClip asset, string path)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(asset);
            var result = new AnimationClipDetails { AssetPath = path, FrameRate = asset.frameRate, Length = asset.length, Loop = settings.loopTime, WrapMode = asset.wrapMode.ToString(), Legacy = asset.legacy };
            foreach (var binding in AnimationUtility.GetCurveBindings(asset))
                result.FloatBindings.Add(new AnimationBindingDetails { Path = binding.path, Type = binding.type?.FullName, Property = binding.propertyName, KeyCount = AnimationUtility.GetEditorCurve(asset, binding)?.length ?? 0 });
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(asset))
                result.ObjectReferenceBindings.Add(new AnimationBindingDetails { Path = binding.path, Type = binding.type?.FullName, Property = binding.propertyName, KeyCount = AnimationUtility.GetObjectReferenceCurve(asset, binding)?.Length ?? 0 });
            foreach (var animationEvent in AnimationUtility.GetAnimationEvents(asset)) result.Events.Add(DescribeEvent(animationEvent));
            return result;
        }

        private static AnimationEventDetails DescribeEvent(AnimationEvent value) => new AnimationEventDetails { Time = value.time, FunctionName = value.functionName, IntParameter = value.intParameter, FloatParameter = value.floatParameter, StringParameter = value.stringParameter };
        private static void Save(AnimationClip asset) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssets(); }
        private static T ParseEnum<T>(string value, T fallback, string argument) where T : struct
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            if (Enum.TryParse(value, true, out T parsed)) return parsed;
            throw new ArgumentException($"Unknown {argument} '{value}'.");
        }
        private static (AnimationClip asset, string path) ResolveClip(ObjectRef reference)
        {
            if (reference == null || reference.IsEmpty) throw new ArgumentException("clip is required.");
            if (!ObjectResolver.TryResolve(reference, out var resolved, out var error)) throw new ArgumentException(error);
            if (!(resolved is AnimationClip asset)) throw new ArgumentException("clip must resolve to an AnimationClip asset.");
            var path = AssetDatabase.GetAssetPath(asset);
            var confined = ProjectPaths.Resolve(path, out var confinementError);
            if (confined == null) throw new ArgumentException(confinementError);
            return (asset, confined);
        }
    }

    [Serializable] public sealed class AnimationClipDetails { [JsonProperty("assetPath")] public string AssetPath { get; set; } [JsonProperty("frameRate")] public float FrameRate { get; set; } [JsonProperty("length")] public float Length { get; set; } [JsonProperty("loop")] public bool Loop { get; set; } [JsonProperty("wrapMode")] public string WrapMode { get; set; } [JsonProperty("legacy")] public bool Legacy { get; set; } [JsonProperty("floatBindings")] public List<AnimationBindingDetails> FloatBindings { get; } = new List<AnimationBindingDetails>(); [JsonProperty("objectReferenceBindings")] public List<AnimationBindingDetails> ObjectReferenceBindings { get; } = new List<AnimationBindingDetails>(); [JsonProperty("events")] public List<AnimationEventDetails> Events { get; } = new List<AnimationEventDetails>(); }
    [Serializable] public sealed class AnimationBindingDetails { [JsonProperty("path")] public string Path { get; set; } [JsonProperty("type")] public string Type { get; set; } [JsonProperty("property")] public string Property { get; set; } [JsonProperty("keyCount")] public int KeyCount { get; set; } }
    [Serializable] public sealed class AnimationEventDetails { [JsonProperty("time")] public float Time { get; set; } [JsonProperty("functionName")] public string FunctionName { get; set; } [JsonProperty("intParameter")] public int IntParameter { get; set; } [JsonProperty("floatParameter")] public float FloatParameter { get; set; } [JsonProperty("stringParameter")] public string StringParameter { get; set; } }
    [Serializable] public sealed class AnimationEventMutationResult { [JsonProperty("assetPath")] public string AssetPath { get; set; } [JsonProperty("eventCount")] public int EventCount { get; set; } [JsonProperty("removedCount", NullValueHandling = NullValueHandling.Ignore)] public int? RemovedCount { get; set; } [JsonProperty("event", NullValueHandling = NullValueHandling.Ignore)] public AnimationEventDetails Event { get; set; } }
}
