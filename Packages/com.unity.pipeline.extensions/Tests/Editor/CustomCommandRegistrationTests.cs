using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Extensions.Editor.Commands.GameObjects;
using Unity.Pipeline.Extensions.Editor.Commands.Observability;
using Unity.Pipeline.Extensions.Editor.Commands.Reflection;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;

namespace Unity.Pipeline.Extensions.Tests.Editor
{
    /// <summary>
    /// Contract tests for the compatibility extension's Pipeline commands. Behavioural tests for
    /// scene/UI/GPU operations remain in their focused
    /// fixtures; these checks make accidental command-discovery regressions immediately visible.
    /// </summary>
    public class CustomCommandRegistrationTests
    {
        [SetUp]
        public void SetUp()
        {
            CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());
        }

        [Test]
        public void CompatibilityCommands_AreDiscovered()
        {
            var discovered = CommandRegistry.DiscoverCommands().Select(command => command.Name).ToArray();
            var expected = new[]
            {
                "duplicate_gameobject", "list_component_types", "unload_scene",
                "set_navmesh_agent_destination", "bake_navmesh_surfaces_compat", "capture_isolated_object",
                "find_builtin_assets", "open_prefab_stage", "create_prefab_stage_gameobject", "save_prefab_stage", "close_prefab_stage",
                "profiler_start", "profiler_stop", "profiler_clear_data", "profiler_status",
                "profiler_list_modules", "profiler_enable_module", "profiler_capture_frame",
                "profiler_save_data", "profiler_load_data",
                "find_methods", "get_type_schema", "get_json_schema", "invoke_method",
                "set_playable_director_timeline", "set_timeline_clip_timing", "move_timeline_clip",
                "remove_timeline_track", "bind_timeline_track", "add_timeline_marker", "get_timeline_details",
                "add_timeline_clip_compat",
                "set_animation_clip_metadata", "add_animation_event", "clear_animation_events", "get_animation_clip_details",
                "get_particle_system", "modify_particle_system"
            };

            CollectionAssert.IsSubsetOf(expected, discovered);
        }

        [Test]
        public void ListComponentTypes_ContainsTransform()
        {
            var result = CompatibilityGameObjectCommands.ListComponentTypes(search: "UnityEngine.Transform", page: 0, pageSize: 10);

            CollectionAssert.Contains(result.Items, "UnityEngine.Transform");
            Assert.Greater(result.TotalCount, 0);
        }

        [Test]
        public void ReflectionMetadata_DescribesKnownType()
        {
            var methods = ReflectionCommands.FindMethods("UnityEngine.Vector3", "Normalize");
            var schema = ReflectionCommands.GetTypeSchema("UnityEngine.Vector3");

            Assert.IsNotEmpty(methods.Methods);
            Assert.AreEqual("UnityEngine.Vector3", schema.Type);
            Assert.IsNotEmpty(schema.Properties);
        }

        [Test]
        public void JsonSchema_UsesCompatibilityContractForVector3()
        {
            var response = ReflectionCommands.GetJsonSchema("UnityEngine.Vector3");
            var schema = JObject.Parse(response.Result);

            Assert.AreEqual("object", schema.Value<string>("type"));
            CollectionAssert.AreEquivalent(
                new[] { "x", "y", "z" },
                ((JObject)schema["properties"]).Properties().Select(property => property.Name));
            CollectionAssert.AreEquivalent(new[] { "x", "y", "z" }, schema["required"].Values<string>());
            Assert.IsFalse(schema.Value<bool>("additionalProperties"));
        }

        [Test]
        public void ReflectionInvocation_SerializesUnityValueTypeReturnsWithoutLoops()
        {
            var invocation = ReflectionCommands.InvokeMethod(
                method: "Normalize",
                type: "UnityEngine.Vector3",
                argumentsJson: "[{\"x\":3,\"y\":0,\"z\":4}]",
                confirm: true);

            Assert.IsTrue(invocation.Invoked);
            var vector = invocation.ReturnValue as JObject;
            Assert.IsNotNull(vector);
            Assert.That(vector.Value<float>("x"), Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(vector.Value<float>("y"), Is.Zero);
            Assert.That(vector.Value<float>("z"), Is.EqualTo(0.8f).Within(0.0001f));
        }

        [Test]
        public void DuplicateGameObject_BatchUsesUnityNamingAndReturnsSources()
        {
            var selectionBefore = Selection.objects;
            var sourceA = new GameObject("PipelineParityBatchA");
            var sourceB = new GameObject("PipelineParityBatchB");
            try
            {
                var result = CompatibilityGameObjectCommands.DuplicateGameObject(
                    sources: new[]
                    {
                        new ObjectRef { InstanceId = PipelineUtils.GetObjectId(sourceA) },
                        new ObjectRef { InstanceId = PipelineUtils.GetObjectId(sourceB) }
                    });

                CollectionAssert.AreEquivalent(
                    new[] { "PipelineParityBatchA", "PipelineParityBatchB" },
                    result.Result.Select(reference => reference.Name));
                Assert.IsNotNull(GameObject.Find("PipelineParityBatchA (1)"));
                Assert.IsNotNull(GameObject.Find("PipelineParityBatchB (1)"));
                CollectionAssert.AreEquivalent(
                    new[] { "PipelineParityBatchA (1)", "PipelineParityBatchB (1)" },
                    Selection.gameObjects.Select(gameObject => gameObject.name));

                Assert.Throws<System.ArgumentException>(() => CompatibilityGameObjectCommands.DuplicateGameObject(
                    sources: new[]
                    {
                        new ObjectRef { InstanceId = PipelineUtils.GetObjectId(sourceA) },
                        new ObjectRef { HierarchyPath = "/MissingForAtomicity" }
                    }));
                Assert.IsNull(GameObject.Find("PipelineParityBatchA (2)"));
            }
            finally
            {
                foreach (var gameObject in Object.FindObjectsOfType<GameObject>()
                    .Where(gameObject => gameObject.name.StartsWith("PipelineParityBatch")))
                    Object.DestroyImmediate(gameObject);
                Selection.objects = selectionBefore;
            }
        }

        [Test]
        public void ProfilerSnapshot_IsStructured()
        {
            var snapshot = ProfilerControlCommands.CaptureFrame();

            Assert.IsNotNull(snapshot.Render);
            Assert.IsNotNull(snapshot.Memory);
            Assert.IsNotNull(snapshot.Script);
            Assert.IsNotEmpty(snapshot.CapturedAtUtc);
        }
    }
}
