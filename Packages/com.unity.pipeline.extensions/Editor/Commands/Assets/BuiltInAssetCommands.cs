using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Extensions.Editor.Commands.Assets
{
    /// <summary>Read-only access to assets stored in Unity's editor built-in resources archive.</summary>
    public static class BuiltInAssetCommands
    {
        private const string BuiltInResourcesPath = "Resources/unity_builtin_extra";
        private static Object[] cachedAssets;

        [CliCommand("find_builtin_assets", "Search Unity Editor built-in resources by optional name fragment and type. Built-in assets do not have project GUIDs.")]
        public static BuiltInAssetSearchResult Find(
            [CliArg("name", "Optional case-insensitive name fragment. Space, underscore, hyphen, and period separate search words.")] string name = null,
            [CliArg("type", "Optional fully-qualified or unambiguous short Unity object type, e.g. UnityEngine.Texture2D.")] string type = null,
            [CliArg("max_results", "Maximum results to return (1-200, default 10).", DefaultValue = 10)] int maxResults = 10)
        {
            if (maxResults < 1 || maxResults > 200)
                throw new ArgumentException("max_results must be between 1 and 200.");
            var requiredType = string.IsNullOrWhiteSpace(type) ? null : ResolveType(type);
            if (requiredType != null && !typeof(Object).IsAssignableFrom(requiredType))
                throw new ArgumentException($"type '{type}' does not derive from UnityEngine.Object.");

            var words = string.IsNullOrWhiteSpace(name)
                ? Array.Empty<string>()
                : name.Split(new[] { ' ', '_', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
            var results = GetAssets()
                .Where(asset => asset != null && !string.IsNullOrEmpty(asset.name))
                .Where(asset => requiredType == null || requiredType.IsInstanceOfType(asset))
                .Select(asset => new { Asset = asset, Priority = MatchPriority(asset.name, name, words) })
                .Where(item => item.Priority >= 0)
                .OrderBy(item => item.Priority)
                .ThenBy(item => item.Asset.name, StringComparer.OrdinalIgnoreCase)
                .Take(maxResults)
                .Select(item => Describe(item.Asset))
                .ToArray();

            return new BuiltInAssetSearchResult
            {
                ResourcePath = BuiltInResourcesPath,
                Results = results,
                Truncated = results.Length == maxResults
            };
        }

        private static Object[] GetAssets()
        {
            if (cachedAssets != null)
                return cachedAssets;
            cachedAssets = AssetDatabase.LoadAllAssetsAtPath(BuiltInResourcesPath) ?? Array.Empty<Object>();
            return cachedAssets;
        }

        private static int MatchPriority(string assetName, string query, string[] words)
        {
            if (string.IsNullOrWhiteSpace(query))
                return 0;
            if (string.Equals(assetName, query, StringComparison.OrdinalIgnoreCase))
                return 0;
            if (assetName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return 1;
            var matchedWords = words.Count(word => assetName.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
            return matchedWords == words.Length ? 2 : matchedWords > 0 ? 3 : -1;
        }

        private static BuiltInAssetResult Describe(Object asset)
        {
            var description = ObjectResolver.Describe(asset);
            return new BuiltInAssetResult
            {
                Name = asset.name,
                Type = asset.GetType().FullName,
                AssetPath = AssetDatabase.GetAssetPath(asset),
                Reference = description
            };
        }

        private static Type ResolveType(string typeName)
        {
            var direct = Type.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (direct != null)
                return direct;

            var matches = new List<Type>();
            foreach (var assembly in PipelineUtils.GetLoadedAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (System.Reflection.ReflectionTypeLoadException ex) { types = ex.Types.Where(candidate => candidate != null).ToArray(); }
                matches.AddRange(types.Where(candidate =>
                    string.Equals(candidate.FullName, typeName, StringComparison.Ordinal) ||
                    string.Equals(candidate.Name, typeName, StringComparison.Ordinal)));
            }

            matches = matches.Distinct().ToList();
            if (matches.Count == 1)
                return matches[0];
            if (matches.Count == 0)
                throw new ArgumentException($"Could not resolve type '{typeName}'. Use a fully-qualified type name.");
            throw new ArgumentException($"Type name '{typeName}' is ambiguous. Use a fully-qualified type name.");
        }
    }

    [Serializable]
    public sealed class BuiltInAssetSearchResult
    {
        [JsonProperty("resourcePath")] public string ResourcePath { get; set; }
        [JsonProperty("results")] public BuiltInAssetResult[] Results { get; set; }
        [JsonProperty("truncated")] public bool Truncated { get; set; }
    }

    [Serializable]
    public sealed class BuiltInAssetResult
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("assetPath")] public string AssetPath { get; set; }
        [JsonProperty("reference")] public AuthoringResult Reference { get; set; }
    }
}
