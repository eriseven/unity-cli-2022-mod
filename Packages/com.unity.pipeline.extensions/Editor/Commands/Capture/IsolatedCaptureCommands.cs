using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Capture;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Extensions.Editor.Commands.Capture
{
    /// <summary>
    /// Renders a GameObject in a disposable preview scene. The preview contains either the target
    /// alone (isolated) or clones of the target scene's roots (non-isolated), so capture controls
    /// never mutate the source scene, object hierarchy, layers, or lighting.
    /// </summary>
    public static class IsolatedCaptureCommands
    {
        private const int MaxDimension = 4096;

        [CliCommand("capture_isolated_object", "Render a GameObject in an isolated or scene-context preview with configurable views, backgrounds, lights, and composite output. The source scene is not modified; requires a GPU.")]
        public static CaptureResult CaptureIsolatedObject(
            [CliArg("target", "Reference to the GameObject or Component to render.", Required = true)] ObjectRef target,
            [CliArg("width", "Output width in pixels, or the per-view width for composite output (default 512; capped at 4096).", DefaultValue = 512)] int width = 512,
            [CliArg("height", "Output height in pixels, or the per-view height for composite output (default 512; capped at 4096).", DefaultValue = 512)] int height = 512,
            [CliArg("resolution", "Optional square per-view resolution. When supplied it overrides width and height.")] int? resolution = null,
            [CliArg("include_children", "Include target children in the render (default true).", DefaultValue = true)] bool includeChildren = true,
            [CliArg("isolated", "When true, render only the target hierarchy; when false, render clones of the target scene context (default true).", DefaultValue = true)] bool isolated = true,
            [CliArg("background_mode", "SolidColor, Skybox, or Transparent. Omit to retain transparent_background compatibility.")] string backgroundMode = null,
            [CliArg("background_color", "Solid-color background in #RRGGBB or #RRGGBBAA format (default #000000).", DefaultValue = "#000000")] string backgroundColor = "#000000",
            [CliArg("camera_view", "Front, Back, Left, Right, Top, Bottom, or Composite (default Front).", DefaultValue = "Front")] string cameraView = "Front",
            [CliArg("field_of_view", "Vertical field of view in degrees (default 60).", DefaultValue = 60f)] float fieldOfView = 60f,
            [CliArg("near_clip_plane", "Camera near clip plane (default 0.01).", DefaultValue = 0.01f)] float nearClipPlane = 0.01f,
            [CliArg("far_clip_plane", "Camera far clip plane (default 1000).", DefaultValue = 1000f)] float farClipPlane = 1000f,
            [CliArg("padding", "Framing multiplier around the target bounds (default 1.2).", DefaultValue = 1.2f)] float padding = 1.2f,
            [CliArg("lights", "Optional JSON array of light configurations. Omit for a default directional light; [] disables additional lights.")] string lightsJson = null,
            [CliArg("transparent_background", "Legacy background switch used only when background_mode is omitted (default true).", DefaultValue = true)] bool transparentBackground = true,
            [CliArg("save_path", "Optional project-relative PNG path. When set, inline image data is omitted unless include_inline_image=true.")] string savePath = null,
            [CliArg("include_inline_image", "Include base64 PNG when save_path is set.", DefaultValue = false)] bool includeInlineImage = false)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("No GPU available (batchmode/headless); cannot capture.");
            if (!ObjectResolver.TryResolve(target, out var resolved, out var resolveError))
                throw new ArgumentException($"Could not resolve target: {resolveError}");
            var source = resolved as GameObject ?? (resolved as Component)?.gameObject;
            if (source == null)
                throw new ArgumentException("target must resolve to a GameObject or Component.");

            var view = ParseCameraView(cameraView);
            var perViewLimit = view == IsolatedCameraView.Composite ? MaxDimension / 2 : MaxDimension;
            var requestedWidth = resolution ?? width;
            var requestedHeight = resolution ?? height;
            var perViewWidth = Mathf.Clamp(requestedWidth, 1, perViewLimit);
            var perViewHeight = Mathf.Clamp(requestedHeight, 1, perViewLimit);
            var background = ParseBackgroundMode(backgroundMode, transparentBackground);
            var color = ParseColor(backgroundColor, Color.black, "background_color");
            var preview = EditorSceneManager.NewPreviewScene();
            var temporaryObjects = new List<GameObject>();
            try
            {
                var captureRoot = CloneCaptureContext(source, preview, isolated, temporaryObjects);
                if (!includeChildren)
                    DestroyChildren(captureRoot.transform);
                if (isolated)
                    ActivateCloneHierarchy(captureRoot.transform);

                var bounds = CalculateBounds(captureRoot);
                var cameraObject = CreatePreviewObject("Pipeline Capture Camera", preview, temporaryObjects);
                var camera = cameraObject.AddComponent<Camera>();
                camera.allowHDR = false;
                camera.allowMSAA = false;
                CreateLights(preview, lightsJson, temporaryObjects);

                var png = RenderPng(camera, bounds, perViewWidth, perViewHeight, view, background, color,
                    fieldOfView, nearClipPlane, farClipPlane, padding);
                var savedPath = WriteIfRequested(png, savePath);
                var wantsInline = string.IsNullOrEmpty(savePath) || includeInlineImage;
                var composite = view == IsolatedCameraView.Composite;
                return new CaptureResult
                {
                    Width = composite ? perViewWidth * 2 : perViewWidth,
                    Height = composite ? perViewHeight * 2 : perViewHeight,
                    Encoding = "png",
                    Base64 = wantsInline ? Convert.ToBase64String(png) : null,
                    Bytes = png.Length,
                    Source = $"isolated:{source.name}; isolated={isolated}; view={view}",
                    SavedPath = savedPath
                };
            }
            finally
            {
                for (var index = temporaryObjects.Count - 1; index >= 0; index--)
                {
                    if (temporaryObjects[index] != null)
                        Object.DestroyImmediate(temporaryObjects[index]);
                }
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static GameObject CloneCaptureContext(GameObject source, Scene preview, bool isolated,
            ICollection<GameObject> temporaryObjects)
        {
            if (isolated)
            {
                var clone = Object.Instantiate(source);
                clone.name = source.name + " (Pipeline Capture Preview)";
                SceneManager.MoveGameObjectToScene(clone, preview);
                temporaryObjects.Add(clone);
                return clone;
            }

            var sourceRoot = source.transform.root;
            GameObject captureRoot = null;
            foreach (var root in source.scene.GetRootGameObjects())
            {
                var rootClone = Object.Instantiate(root);
                rootClone.name = root.name + " (Pipeline Capture Preview)";
                SceneManager.MoveGameObjectToScene(rootClone, preview);
                temporaryObjects.Add(rootClone);
                if (root == sourceRoot.gameObject)
                    captureRoot = FindEquivalentTransform(source.transform, sourceRoot, rootClone.transform).gameObject;
            }

            return captureRoot ?? throw new InvalidOperationException("Failed to clone the target into the preview scene.");
        }

        private static Transform FindEquivalentTransform(Transform source, Transform sourceRoot, Transform cloneRoot)
        {
            var path = new Stack<int>();
            for (var current = source; current != sourceRoot; current = current.parent)
                path.Push(current.GetSiblingIndex());
            var clone = cloneRoot;
            while (path.Count > 0)
                clone = clone.GetChild(path.Pop());
            return clone;
        }

        private static void DestroyChildren(Transform root)
        {
            for (var index = root.childCount - 1; index >= 0; index--)
                Object.DestroyImmediate(root.GetChild(index).gameObject);
        }

        private static void ActivateCloneHierarchy(Transform root)
        {
            root.gameObject.SetActive(true);
            for (var index = 0; index < root.childCount; index++)
                ActivateCloneHierarchy(root.GetChild(index));
        }

        private static GameObject CreatePreviewObject(string name, Scene preview, ICollection<GameObject> temporaryObjects)
        {
            var gameObject = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(gameObject, preview);
            temporaryObjects.Add(gameObject);
            return gameObject;
        }

        private static void CreateLights(Scene preview, string lightsJson, ICollection<GameObject> temporaryObjects)
        {
            List<IsolatedLightConfig> configurations;
            if (lightsJson == null)
            {
                configurations = new List<IsolatedLightConfig>
                {
                    new IsolatedLightConfig { Type = "Directional", Color = "#FFFFFF", Intensity = 1f, Rotation = new[] { 50f, -30f, 0f } }
                };
            }
            else
            {
                try
                {
                    configurations = JsonConvert.DeserializeObject<List<IsolatedLightConfig>>(lightsJson)
                        ?? new List<IsolatedLightConfig>();
                }
                catch (JsonException exception)
                {
                    throw new ArgumentException($"lights must be a JSON array of light configurations: {exception.Message}", exception);
                }
            }

            foreach (var configuration in configurations)
            {
                if (!Enum.TryParse(configuration.Type ?? "Directional", true, out LightType lightType))
                    throw new ArgumentException($"Unknown light type '{configuration.Type}'.");
                var gameObject = CreatePreviewObject("Pipeline Capture Light", preview, temporaryObjects);
                var light = gameObject.AddComponent<Light>();
                light.type = lightType;
                light.color = ParseColor(configuration.Color, Color.white, "lights[].color");
                light.intensity = configuration.Intensity ?? 1f;
                light.range = configuration.Range ?? 10f;
                light.spotAngle = configuration.SpotAngle ?? 30f;
                light.innerSpotAngle = configuration.InnerSpotAngle ?? 21.8f;
                light.shadows = ParseEnum(configuration.Shadows, LightShadows.None, "lights[].shadows");
                light.shadowStrength = configuration.ShadowStrength ?? 1f;
                light.bounceIntensity = configuration.BounceIntensity ?? 1f;
                light.cullingMask = configuration.CullingMask ?? -1;
                light.cookieSize = configuration.CookieSize ?? 10f;
                light.renderMode = ParseEnum(configuration.RenderMode, LightRenderMode.Auto, "lights[].renderMode");
                if (configuration.ColorTemperature.HasValue)
                {
                    light.useColorTemperature = true;
                    light.colorTemperature = configuration.ColorTemperature.Value;
                }
                gameObject.transform.position = ToVector3(configuration.Position, Vector3.zero);
                gameObject.transform.rotation = Quaternion.Euler(ToVector3(configuration.Rotation, new Vector3(50f, -30f, 0f)));
            }
        }

        private static T ParseEnum<T>(string value, T fallback, string argumentName) where T : struct
        {
            if (string.IsNullOrEmpty(value))
                return fallback;
            if (Enum.TryParse(value, true, out T parsed))
                return parsed;
            throw new ArgumentException($"Unknown {argumentName} value '{value}'.");
        }

        private static Vector3 ToVector3(float[] value, Vector3 fallback)
        {
            if (value == null)
                return fallback;
            if (value.Length != 3)
                throw new ArgumentException("Light position and rotation values must contain exactly three numbers.");
            return new Vector3(value[0], value[1], value[2]);
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.one);

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds.size.sqrMagnitude < 0.000001f ? new Bounds(bounds.center, Vector3.one) : bounds;
        }

        private static byte[] RenderPng(Camera camera, Bounds bounds, int width, int height, IsolatedCameraView view,
            CaptureBackgroundMode background, Color backgroundColor, float fieldOfView, float nearClipPlane,
            float farClipPlane, float padding)
        {
            if (view != IsolatedCameraView.Composite)
                return RenderSinglePng(camera, bounds, width, height, view, background, backgroundColor,
                    fieldOfView, nearClipPlane, farClipPlane, padding);

            var composite = new Texture2D(width * 2, height * 2, TextureFormat.RGBA32, false);
            try
            {
                var views = new[] { IsolatedCameraView.Front, IsolatedCameraView.Right, IsolatedCameraView.Back, IsolatedCameraView.Top };
                for (var index = 0; index < views.Length; index++)
                {
                    ConfigureCamera(camera, bounds, width, height, views[index], background, backgroundColor,
                        fieldOfView, nearClipPlane, farClipPlane, padding);
                    var tile = RenderToTexture(camera, width, height);
                    try
                    {
                        var x = (index % 2) * width;
                        var y = index < 2 ? height : 0;
                        composite.SetPixels(x, y, width, height, tile.GetPixels());
                    }
                    finally
                    {
                        Object.DestroyImmediate(tile);
                    }
                }
                composite.Apply();
                return ImageConversion.EncodeToPNG(composite);
            }
            finally
            {
                Object.DestroyImmediate(composite);
            }
        }

        private static byte[] RenderSinglePng(Camera camera, Bounds bounds, int width, int height, IsolatedCameraView view,
            CaptureBackgroundMode background, Color backgroundColor, float fieldOfView, float nearClipPlane,
            float farClipPlane, float padding)
        {
            ConfigureCamera(camera, bounds, width, height, view, background, backgroundColor, fieldOfView,
                nearClipPlane, farClipPlane, padding);
            var texture = RenderToTexture(camera, width, height);
            try
            {
                return ImageConversion.EncodeToPNG(texture);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static void ConfigureCamera(Camera camera, Bounds bounds, int width, int height, IsolatedCameraView view,
            CaptureBackgroundMode background, Color backgroundColor, float fieldOfView, float nearClipPlane,
            float farClipPlane, float padding)
        {
            var fov = Mathf.Clamp(fieldOfView, 1f, 179f);
            var radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z, 0.5f);
            var distance = radius / Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f) * Mathf.Max(padding, 0.01f);
            var direction = GetViewDirection(view);
            camera.transform.position = bounds.center + direction * distance;
            camera.transform.rotation = Quaternion.LookRotation(-direction,
                Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up);
            camera.fieldOfView = fov;
            camera.aspect = width / (float)height;
            camera.nearClipPlane = Mathf.Max(0.0001f, nearClipPlane);
            camera.farClipPlane = Mathf.Max(camera.nearClipPlane + 0.001f, farClipPlane);
            camera.clearFlags = background == CaptureBackgroundMode.Skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            camera.backgroundColor = background == CaptureBackgroundMode.Transparent
                ? new Color(0f, 0f, 0f, 0f)
                : backgroundColor;
        }

        private static Texture2D RenderToTexture(Camera camera, int width, int height)
        {
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                return texture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static IsolatedCameraView ParseCameraView(string value)
        {
            return ParseEnum(value, IsolatedCameraView.Front, "camera_view");
        }

        private static CaptureBackgroundMode ParseBackgroundMode(string value, bool transparentBackground)
        {
            return string.IsNullOrEmpty(value)
                ? (transparentBackground ? CaptureBackgroundMode.Transparent : CaptureBackgroundMode.SolidColor)
                : ParseEnum(value, CaptureBackgroundMode.SolidColor, "background_mode");
        }

        private static Color ParseColor(string value, Color fallback, string argumentName)
        {
            if (string.IsNullOrEmpty(value))
                return fallback;
            if (ColorUtility.TryParseHtmlString(value, out var color))
                return color;
            throw new ArgumentException($"{argumentName} must be #RRGGBB or #RRGGBBAA, got '{value}'.");
        }

        private static Vector3 GetViewDirection(IsolatedCameraView view)
        {
            switch (view)
            {
                case IsolatedCameraView.Back: return Vector3.forward;
                case IsolatedCameraView.Left: return Vector3.left;
                case IsolatedCameraView.Right: return Vector3.right;
                case IsolatedCameraView.Top: return Vector3.up;
                case IsolatedCameraView.Bottom: return Vector3.down;
                default: return Vector3.back;
            }
        }

        private static string WriteIfRequested(byte[] png, string savePath)
        {
            if (string.IsNullOrEmpty(savePath))
                return null;

            var path = ProjectPaths.Resolve(savePath, out var error);
            if (path == null)
                throw new ArgumentException(error);
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("save_path must end in .png.");

            var absolute = Path.Combine(ProjectPaths.ProjectRoot, path);
            var directory = Path.GetDirectoryName(absolute);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllBytes(absolute, png);
            AssetDatabase.Refresh();
            return path;
        }
    }

    internal enum IsolatedCameraView
    {
        Front,
        Back,
        Left,
        Right,
        Top,
        Bottom,
        Composite
    }

    internal enum CaptureBackgroundMode
    {
        SolidColor,
        Skybox,
        Transparent
    }

    [Serializable]
    internal sealed class IsolatedLightConfig
    {
        public string Type { get; set; }
        public string Color { get; set; }
        public float? Intensity { get; set; }
        public float[] Rotation { get; set; }
        public float[] Position { get; set; }
        public float? Range { get; set; }
        public float? SpotAngle { get; set; }
        public float? InnerSpotAngle { get; set; }
        public string Shadows { get; set; }
        public float? ShadowStrength { get; set; }
        public float? BounceIntensity { get; set; }
        public float? ColorTemperature { get; set; }
        public float? CookieSize { get; set; }
        public int? CullingMask { get; set; }
        public string RenderMode { get; set; }
    }
}
