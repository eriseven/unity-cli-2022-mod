using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Animation;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Extensions.Editor.Commands.Animation
{
    /// <summary>Additional Timeline operations implemented through the same optional-package reflection bridge.</summary>
    public static class TimelineExtensionCommands
    {
        private const string Asm = "Unity.Timeline";
        [CliCommand("set_playable_director_timeline", "Assign a TimelineAsset to a PlayableDirector in the current scene. Requires com.unity.timeline.", MainThreadRequired = true)]
        public static object SetPlayableDirectorTimeline(
            [CliArg("director", "Reference to a PlayableDirector component or its GameObject.", Required = true)] ObjectRef director,
            [CliArg("timeline", "Reference to the TimelineAsset to assign.", Required = true)] ObjectRef timeline,
            [CliArg("dry_run", "Validate references without assigning the TimelineAsset.")] bool dryRun = false)
        {
            if (!TimelineGuard.IsInstalled())
                return TimelineGuard.NotInstalledError();

            var (timelineAsset, assetPath, _) = ResolveTimeline(timeline);
            if (!ObjectResolver.TryResolve(director, out var directorObject, out var directorError))
                throw new ArgumentException($"Could not resolve director: {directorError}");
            var playableDirector = directorObject as PlayableDirector ?? (directorObject as GameObject ?? (directorObject as Component)?.gameObject)?.GetComponent<PlayableDirector>();
            if (playableDirector == null)
                throw new ArgumentException("director must resolve to a PlayableDirector component or GameObject containing one.");

            var result = new TimelineDirectorResult
            {
                TimelinePath = assetPath,
                Director = ObjectResolver.Describe(playableDirector)
            };
            if (dryRun)
                return result;

            playableDirector.playableAsset = timelineAsset as PlayableAsset;
            EditorUtility.SetDirty(playableDirector);
            EditorSceneManager.MarkSceneDirty(playableDirector.gameObject.scene);
            return result;
        }

        [CliCommand("set_timeline_clip_timing", "Set a Timeline clip's start and duration by its track name and zero-based clip index. Requires com.unity.timeline.", MainThreadRequired = true)]
        public static object SetTimelineClipTiming(
            [CliArg("timeline", "Reference to the TimelineAsset to edit.", Required = true)] ObjectRef timeline,
            [CliArg("track", "Name of the track containing the clip.", Required = true)] string track,
            [CliArg("clip_index", "Zero-based index of the clip on the named track.", Required = true)] int clipIndex,
            [CliArg("start", "New clip start time in seconds.", Required = true)] double start,
            [CliArg("duration", "New clip duration in seconds.", Required = true)] double duration,
            [CliArg("dry_run", "Validate without saving the TimelineAsset.")] bool dryRun = false)
        {
            if (!TimelineGuard.IsInstalled())
                return TimelineGuard.NotInstalledError();
            if (start < 0d)
                throw new ArgumentException("start must be >= 0.");
            if (duration <= 0d)
                throw new ArgumentException("duration must be > 0.");

            var (asset, assetPath, timelineType) = ResolveTimeline(timeline);
            var clip = ResolveClip(asset, timelineType, track, clipIndex);
            var result = new TimelineClipMutationResult
            {
                AssetPath = assetPath,
                Track = track,
                ClipIndex = clipIndex,
                Start = start,
                Duration = duration
            };
            if (dryRun)
                return result;

            SetClipTiming(clip, start, duration);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return result;
        }

        [CliCommand("move_timeline_clip", "Move a Timeline clip by changing only its start time. Requires com.unity.timeline.", MainThreadRequired = true)]
        public static object MoveTimelineClip(
            [CliArg("timeline", "Reference to the TimelineAsset to edit.", Required = true)] ObjectRef timeline,
            [CliArg("track", "Name of the track containing the clip.", Required = true)] string track,
            [CliArg("clip_index", "Zero-based index of the clip on the named track.", Required = true)] int clipIndex,
            [CliArg("new_start", "New clip start time in seconds.", Required = true)] double newStart,
            [CliArg("dry_run", "Validate without saving the TimelineAsset.")] bool dryRun = false)
        {
            if (!TimelineGuard.IsInstalled())
                return TimelineGuard.NotInstalledError();
            if (newStart < 0d)
                throw new ArgumentException("new_start must be >= 0.");

            var (asset, assetPath, timelineType) = ResolveTimeline(timeline);
            var clip = ResolveClip(asset, timelineType, track, clipIndex);
            var duration = GetDoubleMember(clip, clip.GetType(), "duration");
            var result = new TimelineClipMutationResult
            {
                AssetPath = assetPath,
                Track = track,
                ClipIndex = clipIndex,
                Start = newStart,
                Duration = duration
            };
            if (dryRun)
                return result;

            SetDoubleMember(clip, clip.GetType(), "start", newStart);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return result;
        }

        [CliCommand("remove_timeline_track", "Remove a named track and its clips from a TimelineAsset. Requires confirm=true because this deletes Timeline content.", MainThreadRequired = true)]
        public static object RemoveTimelineTrack(
            [CliArg("timeline", "Reference to the TimelineAsset to edit.", Required = true)] ObjectRef timeline,
            [CliArg("track", "Exact name of the track to remove.", Required = true)] string track,
            [CliArg("confirm", "Must be true because deleting a track removes its clips and markers.", Required = true)] bool confirm = false,
            [CliArg("dry_run", "Validate without removing the track.")] bool dryRun = false)
        {
            if (!TimelineGuard.IsInstalled())
                return TimelineGuard.NotInstalledError();
            if (!confirm)
                throw new ArgumentException("remove_timeline_track requires confirm=true because it deletes Timeline content.");

            var (asset, assetPath, timelineType) = ResolveTimeline(timeline);
            var trackObject = FindTrackByName(asset, timelineType, track);
            if (trackObject == null)
                throw new ArgumentException($"Track '{track}' was not found on the timeline.");
            var result = new TimelineTrackMutationResult { AssetPath = assetPath, Track = track, Action = "removed" };
            if (dryRun)
                return result;

            var deleteTrack = timelineType.GetMethod("DeleteTrack", new[] { GetTrackAssetType() });
            if (deleteTrack == null)
                throw new InvalidOperationException("TimelineAsset.DeleteTrack(TrackAsset) is unavailable in this Timeline package version.");
            deleteTrack.Invoke(asset, new[] { trackObject });
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return result;
        }

        [CliCommand("bind_timeline_track", "Bind a Timeline track to a scene object through a PlayableDirector. Requires com.unity.timeline.", MainThreadRequired = true)]
        public static object BindTimelineTrack(
            [CliArg("timeline", "Reference to the TimelineAsset containing the track.", Required = true)] ObjectRef timeline,
            [CliArg("track", "Exact name of the track to bind.", Required = true)] string track,
            [CliArg("director", "Reference to a PlayableDirector component or its GameObject.", Required = true)] ObjectRef director,
            [CliArg("binding", "Reference to the scene object/component to bind to the track. Pass null only to clear a binding.")] ObjectRef binding = null,
            [CliArg("dry_run", "Validate references without changing the director binding.")] bool dryRun = false)
        {
            if (!TimelineGuard.IsInstalled())
                return TimelineGuard.NotInstalledError();

            var (asset, assetPath, timelineType) = ResolveTimeline(timeline);
            var trackObject = FindTrackByName(asset, timelineType, track) as Object;
            if (trackObject == null)
                throw new ArgumentException($"Track '{track}' was not found on the timeline.");

            if (!ObjectResolver.TryResolve(director, out var directorObject, out var directorError))
                throw new ArgumentException($"Could not resolve director: {directorError}");
            var playableDirector = directorObject as PlayableDirector ?? (directorObject as GameObject ?? (directorObject as Component)?.gameObject)?.GetComponent<PlayableDirector>();
            if (playableDirector == null)
                throw new ArgumentException("director must resolve to a PlayableDirector component or GameObject containing one.");

            Object bindingObject = null;
            if (binding != null && !binding.IsEmpty && !ObjectResolver.TryResolve(binding, out bindingObject, out var bindingError))
                throw new ArgumentException($"Could not resolve binding: {bindingError}");

            var result = new TimelineBindingResult
            {
                AssetPath = assetPath,
                Track = track,
                Director = ObjectResolver.Describe(playableDirector),
                Binding = bindingObject == null ? null : ObjectResolver.Describe(bindingObject)
            };
            if (dryRun)
                return result;

            playableDirector.SetGenericBinding(trackObject, bindingObject);
            EditorUtility.SetDirty(playableDirector);
            EditorSceneManager.MarkSceneDirty(playableDirector.gameObject.scene);
            return result;
        }

        [CliCommand("add_timeline_marker", "Add a marker of a Timeline marker type to a named track. Requires com.unity.timeline; use a fully-qualified marker type when needed.", MainThreadRequired = true)]
        public static object AddTimelineMarker(
            [CliArg("timeline", "Reference to the TimelineAsset to edit.", Required = true)] ObjectRef timeline,
            [CliArg("track", "Exact name of the target marker-capable track.", Required = true)] string track,
            [CliArg("time", "Marker time in seconds.", Required = true)] double time,
            [CliArg("marker_type", "Fully-qualified Timeline marker type. Defaults to UnityEngine.Timeline.Marker.")] string markerType = "UnityEngine.Timeline.Marker",
            [CliArg("dry_run", "Validate the marker type and track without writing.")] bool dryRun = false)
        {
            if (!TimelineGuard.IsInstalled())
                return TimelineGuard.NotInstalledError();
            if (time < 0d)
                throw new ArgumentException("time must be >= 0.");

            var (asset, assetPath, timelineType) = ResolveTimeline(timeline);
            var trackObject = FindTrackByName(asset, timelineType, track);
            if (trackObject == null)
                throw new ArgumentException($"Track '{track}' was not found on the timeline.");

            var marker = Type.GetType($"{markerType}, {Asm}", throwOnError: false);
            if (marker == null)
                throw new ArgumentException($"Marker type '{markerType}' was not found in {Asm}.");
            var markerBase = Type.GetType($"UnityEngine.Timeline.IMarker, {Asm}", throwOnError: false);
            if (markerBase == null || !markerBase.IsAssignableFrom(marker))
                throw new ArgumentException($"'{markerType}' does not implement UnityEngine.Timeline.IMarker.");

            var result = new TimelineMarkerResult { AssetPath = assetPath, Track = track, Time = time, MarkerType = markerType };
            if (dryRun)
                return result;

            var createMarker = trackObject.GetType().GetMethod("CreateMarker", new[] { typeof(Type), typeof(double) });
            if (createMarker == null)
                throw new InvalidOperationException($"Track '{track}' does not support CreateMarker(Type, double).");
            createMarker.Invoke(trackObject, new object[] { marker, time });
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return result;
        }

        private static (Object asset, string path, Type timelineType) ResolveTimeline(ObjectRef timeline)
        {
            if (timeline == null || timeline.IsEmpty)
                throw new ArgumentException("timeline is required.");
            if (!ObjectResolver.TryResolve(timeline, out var resolved, out var error))
                throw new ArgumentException(error);

            var timelineType = TimelineGuard.ResolveTimelineAssetType();
            if (timelineType == null || !timelineType.IsInstanceOfType(resolved))
                throw new ArgumentException($"Reference '{timeline}' does not resolve to a TimelineAsset.");

            var assetPath = AssetDatabase.GetAssetPath(resolved);
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentException($"Reference '{timeline}' does not point to an on-disk TimelineAsset.");

            var confined = ProjectPaths.Resolve(assetPath, out var confineError);
            if (confined == null)
                throw new ArgumentException(
                    $"Timeline '{assetPath}' is outside the authoring root '{ProjectPaths.AuthoringRoot}': {confineError}");

            return (resolved, confined, timelineType);
        }

        private static Type GetTrackAssetType()
        {
            var type = Type.GetType($"UnityEngine.Timeline.TrackAsset, {Asm}", throwOnError: false);
            if (type == null)
                throw new InvalidOperationException("UnityEngine.Timeline.TrackAsset type was not found.");
            return type;
        }

        private static object FindTrackByName(Object timeline, Type timelineType, string name)
        {
            return GetOutputTracks(timeline, timelineType)
                .FirstOrDefault(track => track != null && string.Equals(GetName(track), name, StringComparison.Ordinal));
        }

        private static IEnumerable<object> GetOutputTracks(Object timeline, Type timelineType)
        {
            var method = timelineType.GetMethod("GetOutputTracks", Type.EmptyTypes);
            var enumerable = method?.Invoke(timeline, null) as IEnumerable;
            if (enumerable == null)
                yield break;

            foreach (var item in enumerable)
                yield return item;
        }

        private static IEnumerable<object> GetClips(object track)
        {
            var method = track.GetType().GetMethod("GetClips", Type.EmptyTypes);
            var enumerable = method?.Invoke(track, null) as IEnumerable;
            if (enumerable == null)
                yield break;

            foreach (var item in enumerable)
                yield return item;
        }

        private static string GetName(object value)
        {
            return value.GetType().GetProperty("name", BindingFlags.Public | BindingFlags.Instance)?.GetValue(value) as string;
        }

        private static void SetClipTiming(object clip, double start, double duration)
        {
            SetDoubleMember(clip, clip.GetType(), "start", start);
            SetDoubleMember(clip, clip.GetType(), "duration", duration);
        }

        private static double GetDoubleMember(object value, Type type, string name)
        {
            var member = GetMember(value, type, name);
            return member == null ? 0d : Convert.ToDouble(member);
        }

        private static object GetMember(object value, Type type, string name)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
                return property.GetValue(value);
            return type.GetField(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(value);
        }

        private static void SetDoubleMember(object value, Type type, string name, double number)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                property.SetValue(value, number);
                return;
            }

            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
                field.SetValue(value, number);
        }

        private static object ResolveClip(Object timeline, Type timelineType, string track, int clipIndex)
        {
            if (clipIndex < 0)
                throw new ArgumentException("clip_index must be >= 0.");
            var trackObject = FindTrackByName(timeline, timelineType, track);
            if (trackObject == null)
                throw new ArgumentException($"Track '{track}' was not found on the timeline.");
            var clips = GetClips(trackObject).ToArray();
            if (clipIndex >= clips.Length)
                throw new ArgumentException($"Track '{track}' contains {clips.Length} clip(s); clip_index {clipIndex} is out of range.");
            return clips[clipIndex];
        }
    }

    [Serializable]
    public sealed class TimelineClipMutationResult
    {
        [JsonProperty("assetPath")] public string AssetPath { get; set; }
        [JsonProperty("track")] public string Track { get; set; }
        [JsonProperty("clipIndex")] public int ClipIndex { get; set; }
        [JsonProperty("start")] public double Start { get; set; }
        [JsonProperty("duration")] public double Duration { get; set; }
    }

    [Serializable]
    public sealed class TimelineTrackMutationResult
    {
        [JsonProperty("assetPath")] public string AssetPath { get; set; }
        [JsonProperty("track")] public string Track { get; set; }
        [JsonProperty("action")] public string Action { get; set; }
    }

    [Serializable]
    public sealed class TimelineBindingResult
    {
        [JsonProperty("assetPath")] public string AssetPath { get; set; }
        [JsonProperty("track")] public string Track { get; set; }
        [JsonProperty("director")] public AuthoringResult Director { get; set; }
        [JsonProperty("binding")] public AuthoringResult Binding { get; set; }
    }

    [Serializable]
    public sealed class TimelineDirectorResult
    {
        [JsonProperty("timelinePath")] public string TimelinePath { get; set; }
        [JsonProperty("director")] public AuthoringResult Director { get; set; }
    }

    [Serializable]
    public sealed class TimelineMarkerResult
    {
        [JsonProperty("assetPath")] public string AssetPath { get; set; }
        [JsonProperty("track")] public string Track { get; set; }
        [JsonProperty("time")] public double Time { get; set; }
        [JsonProperty("markerType")] public string MarkerType { get; set; }
    }
}
