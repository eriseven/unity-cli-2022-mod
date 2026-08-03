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
            [CliArg("play_on_awake", "Optional PlayableDirector.playOnAwake value. Omit to preserve the current value.")] bool? playOnAwake = null,
            [CliArg("dry_run", "Validate references without assigning the TimelineAsset.")] bool dryRun = false)
        {
            if (!TimelineGuard.IsInstalled())
                return TimelineGuard.NotInstalledError();

            var (timelineAsset, assetPath, _) = ResolveTimeline(timeline);
            if (!ObjectResolver.TryResolve(director, out var directorObject, out var directorError))
                throw new ArgumentException($"Could not resolve director: {directorError}");
            var gameObject = directorObject as GameObject ?? (directorObject as Component)?.gameObject;
            if (gameObject == null)
                throw new ArgumentException("director must resolve to a GameObject or Component.");
            var playableDirector = directorObject as PlayableDirector ?? gameObject.GetComponent<PlayableDirector>();
            var willCreateDirector = playableDirector == null;

            var result = new TimelineDirectorResult
            {
                TimelinePath = assetPath,
                Director = ObjectResolver.Describe(playableDirector != null ? (Object)playableDirector : gameObject),
                GameObject = ObjectResolver.Describe(gameObject),
                PlayOnAwake = playOnAwake ?? playableDirector?.playOnAwake ?? true,
                CreatedDirector = willCreateDirector
            };
            if (dryRun)
                return result;

            if (playableDirector == null)
                playableDirector = Undo.AddComponent<PlayableDirector>(gameObject);
            playableDirector.playableAsset = timelineAsset as PlayableAsset;
            if (playOnAwake.HasValue)
                playableDirector.playOnAwake = playOnAwake.Value;
            EditorUtility.SetDirty(playableDirector);
            EditorSceneManager.MarkSceneDirty(playableDirector.gameObject.scene);
            result.Director = ObjectResolver.Describe(playableDirector);
            result.PlayOnAwake = playableDirector.playOnAwake;
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
            bindingObject = ResolveCompatibleBinding(trackObject, bindingObject);

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

        [CliCommand("get_timeline_details", "Inspect Timeline tracks, clips, source assets, and markers with compatibility-focused detail. Requires com.unity.timeline.", MainThreadRequired = true)]
        public static TimelineDetailsResult GetTimelineDetails(
            [CliArg("timeline", "Reference to the TimelineAsset to inspect.", Required = true)] ObjectRef timeline,
            [CliArg("include_clips", "Include clip metadata on each output track.", DefaultValue = true)] bool includeClips = true,
            [CliArg("include_markers", "Include marker metadata on each output track.", DefaultValue = true)] bool includeMarkers = true)
        {
            if (!TimelineGuard.IsInstalled())
                throw new InvalidOperationException("com.unity.timeline is not installed.");

            var (asset, assetPath, timelineType) = ResolveTimeline(timeline);
            var rootTracks = GetRootTracks(asset, timelineType).ToArray();
            var tracks = GetOutputTracks(asset, timelineType)
                .Where(track => track != null)
                .Select(track => DescribeTrack(track, rootTracks, includeClips, includeMarkers))
                .ToArray();

            return new TimelineDetailsResult
            {
                AssetPath = assetPath,
                FrameRate = GetDoubleProperty(GetMember(asset, timelineType, "editorSettings"), "fps"),
                DurationMode = GetMember(asset, timelineType, "durationMode")?.ToString(),
                Duration = GetDoubleMember(asset, timelineType, "duration"),
                Tracks = tracks
            };
        }

        [CliCommand("add_timeline_clip_compat", "Add an Animation or Audio Timeline clip with compatible start, duration, and display-name controls. Requires com.unity.timeline.", MainThreadRequired = true)]
        public static TimelineClipAddResult AddTimelineClipCompat(
            [CliArg("timeline", "Reference to the TimelineAsset to edit.", Required = true)] ObjectRef timeline,
            [CliArg("track", "Exact name of the target Animation or Audio track.", Required = true)] string track,
            [CliArg("asset", "Optional AnimationClip or AudioClip source asset. Omit to create the track's default clip.")] ObjectRef asset = null,
            [CliArg("start", "Clip start time in seconds. Defaults to 0.", DefaultValue = 0d)] double start = 0d,
            [CliArg("duration", "Optional clip duration in seconds. Zero preserves Timeline's default duration.", DefaultValue = 0d)] double duration = 0d,
            [CliArg("display_name", "Optional Timeline clip display name.")] string displayName = null,
            [CliArg("dry_run", "Validate the references and requested clip type without writing the TimelineAsset.")] bool dryRun = false)
        {
            if (!TimelineGuard.IsInstalled())
                throw new InvalidOperationException("com.unity.timeline is not installed.");
            if (start < 0d)
                throw new ArgumentException("start must be >= 0.");
            if (duration < 0d)
                throw new ArgumentException("duration must be >= 0.");

            var (timelineAsset, assetPath, timelineType) = ResolveTimeline(timeline);
            var trackObject = FindTrackByName(timelineAsset, timelineType, track);
            if (trackObject == null)
                throw new ArgumentException($"Track '{track}' was not found on the timeline.");

            Object sourceAsset = null;
            if (asset != null && !asset.IsEmpty && !ObjectResolver.TryResolve(asset, out sourceAsset, out var sourceError))
                throw new ArgumentException($"Could not resolve asset: {sourceError}");
            if (sourceAsset != null && !(sourceAsset is AnimationClip) && !(sourceAsset is AudioClip))
                throw new ArgumentException("asset must resolve to an AnimationClip or AudioClip.");

            var playableAssetType = sourceAsset == null ? null : GetPlayableAssetType(sourceAsset);
            var result = new TimelineClipAddResult
            {
                AssetPath = assetPath,
                Track = track,
                Start = start,
                Duration = duration,
                DisplayName = displayName,
                AssetType = playableAssetType?.Name,
                SourceAssetPath = sourceAsset == null ? null : AssetDatabase.GetAssetPath(sourceAsset)
            };
            if (dryRun)
                return result;

            var clip = sourceAsset == null
                ? CreateDefaultClip(trackObject)
                : CreateTypedClip(trackObject, playableAssetType, sourceAsset);
            if (clip == null)
                throw new InvalidOperationException($"Track '{track}' did not create a Timeline clip.");

            var clipType = clip.GetType();
            SetDoubleMember(clip, clipType, "start", start);
            if (duration > 0d)
                SetDoubleMember(clip, clipType, "duration", duration);
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                var displayNameProperty = clipType.GetProperty("displayName", BindingFlags.Public | BindingFlags.Instance);
                if (displayNameProperty == null || !displayNameProperty.CanWrite)
                    throw new InvalidOperationException("TimelineClip.displayName is unavailable in this Timeline package version.");
                displayNameProperty.SetValue(clip, displayName);
            }

            EditorUtility.SetDirty(timelineAsset);
            AssetDatabase.SaveAssets();

            var described = DescribeClip(clip, GetClips(trackObject).ToList().IndexOf(clip));
            result.ClipIndex = described.Index;
            result.DisplayName = described.DisplayName;
            result.Start = described.Start;
            result.Duration = described.Duration;
            result.End = described.End;
            result.AssetType = described.AssetType;
            result.SourceAssetPath = described.SourceAssetPath;
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

        private static Object ResolveCompatibleBinding(Object track, Object binding)
        {
            if (binding == null)
                return null;

            var targetType = GetTrackBindingType(track);
            if (targetType == null || targetType == typeof(GameObject) || targetType.IsInstanceOfType(binding))
                return binding;

            var gameObject = binding as GameObject ?? (binding as Component)?.gameObject;
            var component = gameObject?.GetComponent(targetType);
            if (component != null)
                return component;

            throw new ArgumentException(
                $"Track '{GetName(track)}' expects a binding of type '{targetType.FullName}', but '{gameObject?.name ?? binding.name}' has no matching component.");
        }

        private static Type GetTrackBindingType(Object track)
        {
            var outputs = track.GetType().GetProperty("outputs", BindingFlags.Public | BindingFlags.Instance)?.GetValue(track) as IEnumerable;
            if (outputs == null)
                return null;

            foreach (var output in outputs)
            {
                if (output == null)
                    continue;
                var type = output.GetType().GetProperty("outputTargetType", BindingFlags.Public | BindingFlags.Instance)?.GetValue(output) as Type;
                if (type != null)
                    return type;
            }
            return null;
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

        private static IEnumerable<object> GetRootTracks(Object timeline, Type timelineType)
        {
            var method = timelineType.GetMethod("GetRootTracks", Type.EmptyTypes);
            var enumerable = method?.Invoke(timeline, null) as IEnumerable;
            if (enumerable == null)
                yield break;

            foreach (var item in enumerable)
                yield return item;
        }

        private static object CreateDefaultClip(object track)
        {
            var method = track.GetType().GetMethod("CreateDefaultClip", Type.EmptyTypes);
            if (method == null)
                throw new InvalidOperationException($"Track '{GetName(track)}' does not support CreateDefaultClip().");
            return method.Invoke(track, null);
        }

        private static object CreateTypedClip(object track, Type playableAssetType, Object sourceAsset)
        {
            var trackType = track.GetType();
            var createByType = trackType.GetMethod("CreateClip", new[] { typeof(Type) });
            object clip = createByType?.Invoke(track, new object[] { playableAssetType });
            if (clip == null)
            {
                var generic = trackType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(method => method.Name == "CreateClip" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
                if (generic == null)
                    throw new InvalidOperationException($"Track '{GetName(track)}' does not support typed Timeline clips.");
                clip = generic.MakeGenericMethod(playableAssetType).Invoke(track, null);
            }

            var playableAsset = GetMember(clip, clip.GetType(), "asset") as Object;
            var sourceProperty = playableAsset?.GetType().GetProperty("clip", BindingFlags.Public | BindingFlags.Instance);
            if (sourceProperty == null || !sourceProperty.CanWrite)
                throw new InvalidOperationException($"Playable asset '{playableAsset?.GetType().Name}' does not expose a writable clip source.");
            sourceProperty.SetValue(playableAsset, sourceAsset);
            return clip;
        }

        private static Type GetPlayableAssetType(Object sourceAsset)
        {
            var typeName = sourceAsset is AnimationClip
                ? "UnityEngine.Timeline.AnimationPlayableAsset"
                : "UnityEngine.Timeline.AudioPlayableAsset";
            var type = Type.GetType($"{typeName}, {Asm}", throwOnError: false);
            if (type == null)
                throw new InvalidOperationException($"Timeline playable asset type '{typeName}' was not found.");
            return type;
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

        private static TimelineTrackDetails DescribeTrack(object track, object[] rootTracks, bool includeClips, bool includeMarkers)
        {
            var trackType = track.GetType();
            var clips = includeClips
                ? GetClips(track).Select((clip, index) => DescribeClip(clip, index)).ToArray()
                : Array.Empty<TimelineClipDetails>();
            var markers = includeMarkers
                ? GetMarkers(track).Select(DescribeMarker).ToArray()
                : Array.Empty<TimelineMarkerDetails>();

            return new TimelineTrackDetails
            {
                Name = GetName(track),
                TrackType = trackType.Name,
                IsRoot = rootTracks.Any(root => ReferenceEquals(root, track)),
                RootIndex = Array.FindIndex(rootTracks, root => ReferenceEquals(root, track)),
                Muted = GetBooleanMember(track, trackType, "muted"),
                Locked = GetBooleanMember(track, trackType, "locked"),
                ClipCount = includeClips ? clips.Length : GetClips(track).Count(),
                Clips = clips,
                MarkerCount = includeMarkers ? markers.Length : GetMarkers(track).Count(),
                Markers = markers
            };
        }

        private static TimelineClipDetails DescribeClip(object clip, int index)
        {
            var clipType = clip.GetType();
            var playableAsset = GetMember(clip, clipType, "asset") as Object;
            var sourceAsset = GetSourceAsset(playableAsset);
            return new TimelineClipDetails
            {
                Index = index,
                DisplayName = GetMember(clip, clipType, "displayName") as string,
                Start = GetDoubleMember(clip, clipType, "start"),
                Duration = GetDoubleMember(clip, clipType, "duration"),
                End = GetDoubleMember(clip, clipType, "end"),
                BlendInDuration = GetDoubleMember(clip, clipType, "blendInDuration"),
                BlendOutDuration = GetDoubleMember(clip, clipType, "blendOutDuration"),
                AssetType = playableAsset?.GetType().Name,
                SourceAssetPath = sourceAsset == null ? null : AssetDatabase.GetAssetPath(sourceAsset),
                PlayableAssetPath = playableAsset == null ? null : AssetDatabase.GetAssetPath(playableAsset)
            };
        }

        private static Object GetSourceAsset(Object playableAsset)
        {
            if (playableAsset == null)
                return null;

            var property = playableAsset.GetType().GetProperty("clip", BindingFlags.Public | BindingFlags.Instance);
            return property?.GetValue(playableAsset) as Object;
        }

        private static IEnumerable<object> GetMarkers(object track)
        {
            var method = track.GetType().GetMethod("GetMarkers", Type.EmptyTypes);
            var enumerable = method?.Invoke(track, null) as IEnumerable;
            if (enumerable == null)
                yield break;

            foreach (var item in enumerable)
                yield return item;
        }

        private static TimelineMarkerDetails DescribeMarker(object marker)
        {
            var markerType = marker.GetType();
            return new TimelineMarkerDetails
            {
                Name = GetName(marker),
                MarkerType = markerType.Name,
                Time = GetDoubleMember(marker, markerType, "time")
            };
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

        private static double GetDoubleProperty(object value, string name)
        {
            if (value == null)
                return 0d;
            var property = value.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            var member = property?.GetValue(value);
            return member == null ? 0d : Convert.ToDouble(member);
        }

        private static bool GetBooleanMember(object value, Type type, string name)
        {
            var member = GetMember(value, type, name);
            return member != null && Convert.ToBoolean(member);
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
        [JsonProperty("gameObject")] public AuthoringResult GameObject { get; set; }
        [JsonProperty("playOnAwake")] public bool PlayOnAwake { get; set; }
        [JsonProperty("createdDirector")] public bool CreatedDirector { get; set; }
    }

    [Serializable]
    public sealed class TimelineMarkerResult
    {
        [JsonProperty("assetPath")] public string AssetPath { get; set; }
        [JsonProperty("track")] public string Track { get; set; }
        [JsonProperty("time")] public double Time { get; set; }
        [JsonProperty("markerType")] public string MarkerType { get; set; }
    }

    [Serializable]
    public sealed class TimelineDetailsResult
    {
        [JsonProperty("assetPath")] public string AssetPath { get; set; }
        [JsonProperty("frameRate")] public double FrameRate { get; set; }
        [JsonProperty("durationMode")] public string DurationMode { get; set; }
        [JsonProperty("duration")] public double Duration { get; set; }
        [JsonProperty("tracks")] public TimelineTrackDetails[] Tracks { get; set; }
    }

    [Serializable]
    public sealed class TimelineClipAddResult
    {
        [JsonProperty("assetPath")] public string AssetPath { get; set; }
        [JsonProperty("track")] public string Track { get; set; }
        [JsonProperty("clipIndex")] public int ClipIndex { get; set; }
        [JsonProperty("displayName")] public string DisplayName { get; set; }
        [JsonProperty("start")] public double Start { get; set; }
        [JsonProperty("duration")] public double Duration { get; set; }
        [JsonProperty("end")] public double End { get; set; }
        [JsonProperty("assetType")] public string AssetType { get; set; }
        [JsonProperty("sourceAssetPath")] public string SourceAssetPath { get; set; }
    }

    [Serializable]
    public sealed class TimelineTrackDetails
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("trackType")] public string TrackType { get; set; }
        [JsonProperty("isRoot")] public bool IsRoot { get; set; }
        [JsonProperty("rootIndex")] public int RootIndex { get; set; }
        [JsonProperty("muted")] public bool Muted { get; set; }
        [JsonProperty("locked")] public bool Locked { get; set; }
        [JsonProperty("clipCount")] public int ClipCount { get; set; }
        [JsonProperty("clips")] public TimelineClipDetails[] Clips { get; set; }
        [JsonProperty("markerCount")] public int MarkerCount { get; set; }
        [JsonProperty("markers")] public TimelineMarkerDetails[] Markers { get; set; }
    }

    [Serializable]
    public sealed class TimelineClipDetails
    {
        [JsonProperty("index")] public int Index { get; set; }
        [JsonProperty("displayName")] public string DisplayName { get; set; }
        [JsonProperty("start")] public double Start { get; set; }
        [JsonProperty("duration")] public double Duration { get; set; }
        [JsonProperty("end")] public double End { get; set; }
        [JsonProperty("blendInDuration")] public double BlendInDuration { get; set; }
        [JsonProperty("blendOutDuration")] public double BlendOutDuration { get; set; }
        [JsonProperty("assetType")] public string AssetType { get; set; }
        [JsonProperty("sourceAssetPath")] public string SourceAssetPath { get; set; }
        [JsonProperty("playableAssetPath")] public string PlayableAssetPath { get; set; }
    }

    [Serializable]
    public sealed class TimelineMarkerDetails
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("markerType")] public string MarkerType { get; set; }
        [JsonProperty("time")] public double Time { get; set; }
    }
}
