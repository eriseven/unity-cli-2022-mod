using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Editor.Commands.Baking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Unity.Pipeline.Tests.Editor.Baking
{
    /// <summary>
    /// Tests for the CLI-215 lighting bake commands. These cover the parts that are deterministic
    /// without waiting on a real (minutes-long) lightmap bake: command discovery/schema, the
    /// settings get/set round-trip, the destructive clear guard, the idle status, the no_scene guard,
    /// and dry_run being a no-op. The full bake → completed acceptance flow is exercised live against a
    /// running Editor (it is too slow/platform-dependent for the unit suite).
    /// </summary>
    class LightingBakeCommandsTests
    {
        [SetUp]
        public void SetUp() => CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());

        [TearDown]
        public void TearDown()
        {
            // Leave the editor with a fresh empty (untitled) scene so other tests are unaffected.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void LightingCommands_AreDiscovered_WithExpectedSchema()
        {
            var commands = CommandRegistry.DiscoverCommands().ToList();

            var bake = commands.FirstOrDefault(c => c.FullName == "lighting bake");
            Assert.IsNotNull(bake, "Should discover bake_lighting");
            CollectionAssert.AreEquivalent(new[] { "confirm", "dry_run" },
                bake.Parameters.Select(p => p.Name).ToList());

            var status = commands.FirstOrDefault(c => c.FullName == "lighting bake_status");
            Assert.IsNotNull(status, "Should discover lighting_bake_status");
            Assert.IsFalse(status.MainThreadRequired,
                "lighting_bake_status must be off-main-thread so it answers while a bake holds the main thread");
            Assert.AreEqual(0, status.Parameters.Count, "lighting_bake_status takes no parameters");

            Assert.IsNotNull(commands.FirstOrDefault(c => c.FullName == "lighting cancel_bake"));
            Assert.IsNotNull(commands.FirstOrDefault(c => c.FullName == "lighting clear_bake"));
            Assert.IsNotNull(commands.FirstOrDefault(c => c.FullName == "lighting get_settings"));
            Assert.IsNotNull(commands.FirstOrDefault(c => c.FullName == "lighting set_settings"));
        }

        [Test]
        public void GetLightingSettings_ReturnsCoreFields()
        {
            var settings = LightingBakeCommands.GetSettings();
            if (!settings.Available)
            {
                // No LightingSettings are exposed when no scene is open (e.g. batchmode test
                // projects start with an untitled empty scene). Skip rather than fail.
                Assert.Pass("No active LightingSettings — skipping (no scene open in test project).");
                return;
            }
            Assert.IsNotNull(settings.Lightmapper, "lightmapper should be reported");

            // directionalMode must be one of the two spec values, never a raw flags-enum string.
            Assert.That(settings.DirectionalMode, Is.EqualTo("NonDirectional").Or.EqualTo("Directional"),
                "directionalMode must be 'NonDirectional' or 'Directional'");

            // lightmapCompression must be the LightmapCompression enum name, not a bool.
            var validCompressions = new[] { "None", "LowQuality", "NormalQuality", "HighQuality" };
            CollectionAssert.Contains(validCompressions, settings.LightmapCompression,
                "lightmapCompression must be one of the LightmapCompression enum names");
        }

        [Test]
        public void SetLightingSettings_DirectionalMode_RoundTrips()
        {
            var before = LightingBakeCommands.GetSettings();
            if (!before.Available)
            {
                Assert.Pass("No active LightingSettings — skipping directionalMode round-trip test.");
                return;
            }

            var target = before.DirectionalMode == "NonDirectional" ? "Directional" : "NonDirectional";
            try
            {
                var result = JObject.FromObject(LightingBakeCommands.SetSettings(
                    new JObject { ["directionalMode"] = target }));
                CollectionAssert.Contains(result["applied"].Select(t => t.Value<string>()).ToList(),
                    "directionalMode", "directionalMode should be in applied[]");

                var after = LightingBakeCommands.GetSettings();
                Assert.That(after.DirectionalMode, Is.EqualTo("NonDirectional").Or.EqualTo("Directional"),
                    "directionalMode must remain a spec-valid string after set");
            }
            finally
            {
                LightingBakeCommands.SetSettings(new JObject { ["directionalMode"] = before.DirectionalMode });
            }
        }

        [Test]
        public void SetLightingSettings_RoundTrips_BouncesAndResolution()
        {
            var before = LightingBakeCommands.GetSettings();
            var newBounces = before.Bounces == 3 ? 2 : 3;
            var newResolution = before.LightmapResolution >= 40f ? 20f : 50f;

            try
            {
                var settings = new JObject
                {
                    ["bounces"] = newBounces,
                    ["lightmapResolution"] = newResolution
                };
                LightingBakeCommands.SetSettings(settings);

                var after = LightingBakeCommands.GetSettings();
                Assert.AreEqual(newBounces, after.Bounces, "bounces should round-trip");
                Assert.AreEqual(newResolution, after.LightmapResolution, 0.001f, "lightmapResolution should round-trip");
            }
            finally
            {
                // Restore the prior values so the suite leaves no residue in the host project's
                // lighting settings (the active settings may be a shared scene-default or asset).
                LightingBakeCommands.SetSettings(new JObject
                {
                    ["bounces"] = before.Bounces,
                    ["lightmapResolution"] = before.LightmapResolution
                });
            }
        }

        [Test]
        public void SetLightingSettings_ReportsUnknownKeys()
        {
            var settings = new JObject
            {
                ["bounces"] = 2,
                ["totallyNotARealKey"] = 5
            };
            var result = JObject.FromObject(LightingBakeCommands.SetSettings(settings));

            var applied = result["applied"].Select(t => t.Value<string>()).ToList();
            var unknown = result["unknown"].Select(t => t.Value<string>()).ToList();
            CollectionAssert.Contains(applied, "bounces");
            CollectionAssert.Contains(unknown, "totallyNotARealKey");
        }

        [Test]
        public void SetLightingSettings_DryRun_DoesNotChangeAnything()
        {
            var before = LightingBakeCommands.GetSettings();
            var changed = before.Bounces == 1 ? 4 : 1;

            LightingBakeCommands.SetSettings(new JObject { ["bounces"] = changed }, dryRun: true);

            var after = LightingBakeCommands.GetSettings();
            Assert.AreEqual(before.Bounces, after.Bounces, "dry_run must not mutate settings");
        }

        [Test]
        public void ClearBakedLighting_WithoutConfirm_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => LightingBakeCommands.ClearBake());
        }

        [Test]
        public void ClearBakedLighting_DryRun_DoesNotThrow_AndReportsWouldClear()
        {
            var result = JObject.FromObject(LightingBakeCommands.ClearBake(confirm: true, dryRun: true));
            Assert.AreEqual("dry_run", result["status"].Value<string>());
            Assert.IsTrue(result["wouldClear"].Value<bool>());
        }

        [Test]
        public void BakeLighting_NoOpenSavedScene_ReturnsNoScene()
        {
            // A fresh untitled scene has an empty path, so it is not a bakeable (saved) scene.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.IsTrue(string.IsNullOrEmpty(SceneManager.GetActiveScene().path), "test precondition: untitled scene");

            var result = JObject.FromObject(LightingBakeCommands.Bake());
            Assert.AreEqual("no_scene", result["code"].Value<string>());
        }

        [Test]
        public void LightingBakeStatus_Idle_WhenNothingTriggered_OrAfterStatusFile()
        {
            // The status reflects either a clean idle or the result of a prior (cancelled/completed)
            // bake from another test; in all cases it must be valid JSON carrying a "status" field.
            var json = LightingBakeCommands.BakeStatus();
            Assert.IsNotNull(json["status"], "lighting_bake_status should always include a 'status' field");
        }
    }
}
