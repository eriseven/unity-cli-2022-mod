using System.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Extensions.Editor.Commands.GameObjects;
using Unity.Pipeline.Extensions.Editor.Commands.Observability;
using Unity.Pipeline.Extensions.Editor.Commands.Reflection;

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
                "set_navmesh_agent_destination", "capture_isolated_object",
                "find_builtin_assets", "open_prefab_stage", "save_prefab_stage", "close_prefab_stage",
                "profiler_start", "profiler_stop", "profiler_clear_data", "profiler_status",
                "profiler_list_modules", "profiler_enable_module", "profiler_capture_frame",
                "profiler_save_data", "profiler_load_data",
                "find_methods", "get_type_schema", "invoke_method",
                "set_playable_director_timeline", "set_timeline_clip_timing", "move_timeline_clip",
                "remove_timeline_track", "bind_timeline_track", "add_timeline_marker"
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
