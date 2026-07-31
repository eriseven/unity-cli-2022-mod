using System;
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
    /// Renders a GameObject in a temporary preview scene. Unlike a layer-masking approach this does
    /// not change the source object's layer, visibility, transform, camera, or scene membership.
    /// </summary>
    public static class IsolatedCaptureCommands
    {
        private const int MaxDimension = 4096;

        [CliCommand("capture_isolated_object", "Render a GameObject by itself in a temporary preview scene. The source scene is not modified; requires a GPU.")]
        public static CaptureResult CaptureIsolatedObject(
            [CliArg("target", "Reference to the GameObject or Component to render.", Required = true)] ObjectRef target,
            [CliArg("width", "Output width in pixels (default 512; capped at 4096).", DefaultValue = 512)] int width = 512,
            [CliArg("height", "Output height in pixels (default 512; capped at 4096).", DefaultValue = 512)] int height = 512,
            [CliArg("transparent_background", "Render with a transparent background instead of opaque black.", DefaultValue = true)] bool transparentBackground = true,
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

            var w = Mathf.Clamp(width, 1, MaxDimension);
            var h = Mathf.Clamp(height, 1, MaxDimension);
            var preview = EditorSceneManager.NewPreviewScene();
            GameObject clone = null;
            GameObject cameraObject = null;
            GameObject lightObject = null;
            try
            {
                clone = Object.Instantiate(source);
                clone.name = source.name + " (Pipeline Capture Preview)";
                SceneManager.MoveGameObjectToScene(clone, preview);

                var bounds = CalculateBounds(clone);
                cameraObject = new GameObject("Pipeline Capture Camera") { hideFlags = HideFlags.HideAndDontSave };
                SceneManager.MoveGameObjectToScene(cameraObject, preview);
                var camera = cameraObject.AddComponent<Camera>();
                ConfigureCamera(camera, bounds, w, h, transparentBackground);

                lightObject = new GameObject("Pipeline Capture Light") { hideFlags = HideFlags.HideAndDontSave };
                SceneManager.MoveGameObjectToScene(lightObject, preview);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.transform.rotation = Quaternion.Euler(40f, -35f, 0f);

                var png = RenderPng(camera, w, h);
                var savedPath = WriteIfRequested(png, savePath);
                var wantsInline = string.IsNullOrEmpty(savePath) || includeInlineImage;
                return new CaptureResult
                {
                    Width = w,
                    Height = h,
                    Encoding = "png",
                    Base64 = wantsInline ? Convert.ToBase64String(png) : null,
                    Bytes = png.Length,
                    Source = "isolated:" + source.name,
                    SavedPath = savedPath
                };
            }
            finally
            {
                if (lightObject != null)
                    Object.DestroyImmediate(lightObject);
                if (cameraObject != null)
                    Object.DestroyImmediate(cameraObject);
                if (clone != null)
                    Object.DestroyImmediate(clone);
                EditorSceneManager.ClosePreviewScene(preview);
            }
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

        private static void ConfigureCamera(Camera camera, Bounds bounds, int width, int height, bool transparent)
        {
            var radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z, 0.5f);
            var fov = 30f;
            var distance = radius / Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f) * 1.35f;
            camera.transform.position = bounds.center + new Vector3(0f, radius * 0.2f, -distance);
            camera.transform.LookAt(bounds.center);
            camera.fieldOfView = fov;
            camera.aspect = width / (float)height;
            camera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2.5f);
            camera.farClipPlane = distance + radius * 2.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = transparent ? new Color(0f, 0f, 0f, 0f) : Color.black;
            camera.allowHDR = false;
            camera.allowMSAA = false;
        }

        private static byte[] RenderPng(Camera camera, int width, int height)
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
                try
                {
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture.Apply();
                    return ImageConversion.EncodeToPNG(texture);
                }
                finally
                {
                    Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
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
}
