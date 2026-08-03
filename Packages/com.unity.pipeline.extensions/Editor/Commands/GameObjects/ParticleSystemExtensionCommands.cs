using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Extensions.Editor.Commands.GameObjects
{
    /// <summary>Typed ParticleSystem module access for the modules used by the Unity-MCP parity skills.</summary>
    public static class ParticleSystemExtensionCommands
    {
        [CliCommand("get_particle_system", "Read ParticleSystem runtime state and Main, Emission, Shape, and Noise module settings.", MainThreadRequired = true)]
        public static ParticleSystemDetails GetParticleSystem(
            [CliArg("target", "Reference to a ParticleSystem component or its GameObject.", Required = true)] ObjectRef target,
            [CliArg("include_main", "Include Main module data (default true).", DefaultValue = true)] bool includeMain = true,
            [CliArg("include_emission", "Include Emission module data (default true).", DefaultValue = true)] bool includeEmission = true,
            [CliArg("include_shape", "Include Shape module data (default true).", DefaultValue = true)] bool includeShape = true,
            [CliArg("include_noise", "Include Noise module data (default false).", DefaultValue = false)] bool includeNoise = false)
        {
            return Describe(ResolveParticleSystem(target), includeMain, includeEmission, includeShape, includeNoise);
        }

        [CliCommand("modify_particle_system", "Modify supported Main, Emission, Shape, and Noise ParticleSystem module fields with JSON objects.", MainThreadRequired = true)]
        public static ParticleSystemDetails ModifyParticleSystem(
            [CliArg("target", "Reference to a ParticleSystem component or its GameObject.", Required = true)] ObjectRef target,
            [CliArg("main", "Optional Main fields: duration, loop, startLifetime, startSpeed, startSize, gravityModifier, simulationSpace, maxParticles, playOnAwake.")] JObject main = null,
            [CliArg("emission", "Optional Emission fields: enabled, rateOverTime, rateOverDistance.")] JObject emission = null,
            [CliArg("shape", "Optional Shape fields: enabled, shapeType, radius, angle, arc, radiusThickness.")] JObject shape = null,
            [CliArg("noise", "Optional Noise fields: enabled, strength, frequency, scrollSpeed.")] JObject noise = null,
            [CliArg("dry_run", "Validate and describe the target without applying module changes.")] bool dryRun = false)
        {
            var particleSystem = ResolveParticleSystem(target);
            if (main == null && emission == null && shape == null && noise == null) throw new ArgumentException("At least one module object is required.");
            Validate(main, emission, shape, noise);
            if (dryRun) return Describe(particleSystem, main != null, emission != null, shape != null, noise != null);

            Undo.RecordObject(particleSystem, "Modify Particle System");
            if (main != null) ApplyMain(particleSystem.main, main);
            if (emission != null) ApplyEmission(particleSystem.emission, emission);
            if (shape != null) ApplyShape(particleSystem.shape, shape);
            if (noise != null) ApplyNoise(particleSystem.noise, noise);
            EditorUtility.SetDirty(particleSystem);
            if (particleSystem.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(particleSystem.gameObject.scene);
            return Describe(particleSystem, main != null, emission != null, shape != null, noise != null);
        }

        private static ParticleSystem ResolveParticleSystem(ObjectRef reference)
        {
            if (!ObjectResolver.TryResolve(reference, out var resolved, out var error)) throw new ArgumentException(error);
            var result = resolved as ParticleSystem ?? (resolved as GameObject ?? (resolved as Component)?.gameObject)?.GetComponent<ParticleSystem>();
            return result ?? throw new ArgumentException("target must resolve to a ParticleSystem or a GameObject containing one.");
        }
        private static ParticleSystemDetails Describe(ParticleSystem value, bool includeMain, bool includeEmission, bool includeShape, bool includeNoise)
        {
            var result = new ParticleSystemDetails { Target = ObjectResolver.Describe(value), IsPlaying = value.isPlaying, IsPaused = value.isPaused, IsEmitting = value.isEmitting, IsStopped = value.isStopped, ParticleCount = value.particleCount, Time = value.time };
            if (includeMain) { var m = value.main; result.Main = new ParticleMainDetails { Duration = m.duration, Loop = m.loop, StartLifetime = m.startLifetime.constant, StartSpeed = m.startSpeed.constant, StartSize = m.startSize.constant, GravityModifier = m.gravityModifier.constant, SimulationSpace = m.simulationSpace.ToString(), MaxParticles = m.maxParticles, PlayOnAwake = m.playOnAwake }; }
            if (includeEmission) { var m = value.emission; result.Emission = new ParticleEmissionDetails { Enabled = m.enabled, RateOverTime = m.rateOverTime.constant, RateOverDistance = m.rateOverDistance.constant }; }
            if (includeShape) { var m = value.shape; result.Shape = new ParticleShapeDetails { Enabled = m.enabled, ShapeType = m.shapeType.ToString(), Radius = m.radius, Angle = m.angle, Arc = m.arc, RadiusThickness = m.radiusThickness }; }
            if (includeNoise) { var m = value.noise; result.Noise = new ParticleNoiseDetails { Enabled = m.enabled, Strength = m.strength.constant, Frequency = m.frequency, ScrollSpeed = m.scrollSpeed.constant }; }
            return result;
        }
        private static void Validate(JObject main, JObject emission, JObject shape, JObject noise)
        {
            if (main?["duration"]?.Value<float>() <= 0f) throw new ArgumentException("main.duration must be positive.");
            if (main?["maxParticles"]?.Value<int>() < 0) throw new ArgumentException("main.maxParticles must be non-negative.");
            if (shape?[("radius")]?.Value<float>() < 0f) throw new ArgumentException("shape.radius must be non-negative.");
            if (shape?["angle"]?.Value<float>() < 0f || shape?["angle"]?.Value<float>() > 360f) throw new ArgumentException("shape.angle must be in [0, 360].");
            if (shape?["arc"]?.Value<float>() < 0f || shape?["arc"]?.Value<float>() > 360f) throw new ArgumentException("shape.arc must be in [0, 360].");
            ParseEnum<ParticleSystemSimulationSpace>(main?.Value<string>("simulationSpace"), default, "main.simulationSpace");
            ParseEnum<ParticleSystemShapeType>(shape?.Value<string>("shapeType"), default, "shape.shapeType");
        }
        private static void ApplyMain(ParticleSystem.MainModule module, JObject value)
        {
            if (value["duration"] != null) module.duration = value.Value<float>("duration"); if (value["loop"] != null) module.loop = value.Value<bool>("loop"); if (value["startLifetime"] != null) module.startLifetime = value.Value<float>("startLifetime"); if (value["startSpeed"] != null) module.startSpeed = value.Value<float>("startSpeed"); if (value["startSize"] != null) module.startSize = value.Value<float>("startSize"); if (value["gravityModifier"] != null) module.gravityModifier = value.Value<float>("gravityModifier"); if (value["maxParticles"] != null) module.maxParticles = value.Value<int>("maxParticles"); if (value["playOnAwake"] != null) module.playOnAwake = value.Value<bool>("playOnAwake"); if (value["simulationSpace"] != null) module.simulationSpace = ParseEnum(value.Value<string>("simulationSpace"), module.simulationSpace, "main.simulationSpace");
        }
        private static void ApplyEmission(ParticleSystem.EmissionModule module, JObject value) { if (value["enabled"] != null) module.enabled = value.Value<bool>("enabled"); if (value["rateOverTime"] != null) module.rateOverTime = value.Value<float>("rateOverTime"); if (value["rateOverDistance"] != null) module.rateOverDistance = value.Value<float>("rateOverDistance"); }
        private static void ApplyShape(ParticleSystem.ShapeModule module, JObject value) { if (value["enabled"] != null) module.enabled = value.Value<bool>("enabled"); if (value["radius"] != null) module.radius = value.Value<float>("radius"); if (value["angle"] != null) module.angle = value.Value<float>("angle"); if (value["arc"] != null) module.arc = value.Value<float>("arc"); if (value["radiusThickness"] != null) module.radiusThickness = value.Value<float>("radiusThickness"); if (value["shapeType"] != null) module.shapeType = ParseEnum(value.Value<string>("shapeType"), module.shapeType, "shape.shapeType"); }
        private static void ApplyNoise(ParticleSystem.NoiseModule module, JObject value) { if (value["enabled"] != null) module.enabled = value.Value<bool>("enabled"); if (value["strength"] != null) module.strength = value.Value<float>("strength"); if (value["frequency"] != null) module.frequency = value.Value<float>("frequency"); if (value["scrollSpeed"] != null) module.scrollSpeed = value.Value<float>("scrollSpeed"); }
        private static T ParseEnum<T>(string value, T fallback, string name) where T : struct { if (string.IsNullOrWhiteSpace(value)) return fallback; if (Enum.TryParse(value, true, out T parsed)) return parsed; throw new ArgumentException($"Unknown {name} '{value}'."); }
    }
    [Serializable] public sealed class ParticleSystemDetails { [JsonProperty("target")] public AuthoringResult Target { get; set; } [JsonProperty("isPlaying")] public bool IsPlaying { get; set; } [JsonProperty("isPaused")] public bool IsPaused { get; set; } [JsonProperty("isEmitting")] public bool IsEmitting { get; set; } [JsonProperty("isStopped")] public bool IsStopped { get; set; } [JsonProperty("particleCount")] public int ParticleCount { get; set; } [JsonProperty("time")] public float Time { get; set; } [JsonProperty("main", NullValueHandling = NullValueHandling.Ignore)] public ParticleMainDetails Main { get; set; } [JsonProperty("emission", NullValueHandling = NullValueHandling.Ignore)] public ParticleEmissionDetails Emission { get; set; } [JsonProperty("shape", NullValueHandling = NullValueHandling.Ignore)] public ParticleShapeDetails Shape { get; set; } [JsonProperty("noise", NullValueHandling = NullValueHandling.Ignore)] public ParticleNoiseDetails Noise { get; set; } }
    [Serializable] public sealed class ParticleMainDetails { public float Duration { get; set; } public bool Loop { get; set; } public float StartLifetime { get; set; } public float StartSpeed { get; set; } public float StartSize { get; set; } public float GravityModifier { get; set; } public string SimulationSpace { get; set; } public int MaxParticles { get; set; } public bool PlayOnAwake { get; set; } }
    [Serializable] public sealed class ParticleEmissionDetails { public bool Enabled { get; set; } public float RateOverTime { get; set; } public float RateOverDistance { get; set; } }
    [Serializable] public sealed class ParticleShapeDetails { public bool Enabled { get; set; } public string ShapeType { get; set; } public float Radius { get; set; } public float Angle { get; set; } public float Arc { get; set; } public float RadiusThickness { get; set; } }
    [Serializable] public sealed class ParticleNoiseDetails { public bool Enabled { get; set; } public float Strength { get; set; } public float Frequency { get; set; } public float ScrollSpeed { get; set; } }
}
