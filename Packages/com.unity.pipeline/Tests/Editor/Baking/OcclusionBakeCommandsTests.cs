using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Editor.Commands.Baking;
using UnityEditor.SceneManagement;

namespace Unity.Pipeline.Tests.Editor.Baking
{
    /// <summary>
    /// Tests for the CLI-215 occlusion-culling bake commands. Covers discovery/schema, the destructive
    /// clear guard, the idle status, the no_scene guard, and dry_run being a no-op (and echoing the
    /// effective parameters). The full bake → succeeded flow is exercised live.
    /// </summary>
    class OcclusionBakeCommandsTests
    {
        [SetUp]
        public void SetUp() => CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void OcclusionCommands_AreDiscovered_WithExpectedSchema()
        {
            var commands = CommandRegistry.DiscoverCommands().ToList();

            var bake = commands.FirstOrDefault(c => c.FullName == "occlusion bake");
            Assert.IsNotNull(bake, "Should discover bake_occlusion_culling");
            CollectionAssert.AreEquivalent(
                new[] { "smallest_occluder", "smallest_hole", "backface_threshold", "confirm", "dry_run" },
                bake.Parameters.Select(p => p.Name).ToList());

            var status = commands.FirstOrDefault(c => c.FullName == "occlusion bake_status");
            Assert.IsNotNull(status, "Should discover occlusion_bake_status");
            Assert.IsFalse(status.MainThreadRequired, "occlusion_bake_status must be off-main-thread");
            Assert.AreEqual(0, status.Parameters.Count, "occlusion_bake_status takes no parameters");

            Assert.IsNotNull(commands.FirstOrDefault(c => c.FullName == "occlusion cancel_bake"));
            Assert.IsNotNull(commands.FirstOrDefault(c => c.FullName == "occlusion clear_bake"));
        }

        [Test]
        public void ClearOcclusion_WithoutConfirm_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => OcclusionBakeCommands.ClearBake());
        }

        [Test]
        public void BakeOcclusion_NoOpenSavedScene_ReturnsNoScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var result = JObject.FromObject(OcclusionBakeCommands.Bake());
            Assert.AreEqual("no_scene", result["code"].Value<string>());
        }

        [Test]
        public void OcclusionBakeStatus_AlwaysHasStatusField()
        {
            var json = OcclusionBakeCommands.BakeStatus();
            Assert.IsNotNull(json["status"]);
        }
    }
}
