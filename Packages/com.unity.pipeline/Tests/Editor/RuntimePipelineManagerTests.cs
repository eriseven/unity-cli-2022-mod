using System.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    public class RuntimePipelineManagerTests
    {
#pragma warning disable CS0618
        [Test]
        public void LegacyComponent_DoesNotCreateOrStopDriverOrChangeDiscovery()
        {
            var driver = RuntimePipelineBootstrap.Instance;
            var server = PipelineServerStartup.Server;
            var discovery = CommandRegistry.DiscoverCommands().First(c => c.FullName == "editor status");
            var go = new GameObject("LegacyPipelineCompatibilityTest");
            try
            {
                var manager = go.AddComponent<RuntimePipelineManager>();

                manager.autoStart = true;
                Assert.AreSame(driver, RuntimePipelineBootstrap.Instance);
                Assert.AreEqual(driver != null && driver.IsServerRunning, manager.IsServerRunning);
                Assert.AreSame(server, PipelineServerStartup.Server);
                Assert.AreSame(discovery, CommandRegistry.DiscoverCommands().First(c => c.FullName == "editor status"));
            }
            finally { Object.DestroyImmediate(go); }
            Assert.AreSame(driver, RuntimePipelineBootstrap.Instance);
            Assert.AreSame(server, PipelineServerStartup.Server);
        }

        [Test]
        public void LegacyComponent_PreservesSerializedMigrationValues()
        {
            var go = new GameObject("LegacyPipelineSerializationTest");
            try
            {
                var manager = go.AddComponent<RuntimePipelineManager>();
                JsonUtility.FromJsonOverwrite("{\"enableInBuilds\":true,\"port\":7912,\"requestTimeoutMs\":42000,\"maxWorkItemsPerFrame\":7}", manager);
                Assert.IsTrue(manager.enableInBuilds);
                Assert.AreEqual(7912, manager.port);
                Assert.AreEqual(42000, manager.requestTimeoutMs);
                Assert.AreEqual(7, manager.maxWorkItemsPerFrame);
            }
            finally { Object.DestroyImmediate(go); }
        }
#pragma warning restore CS0618
    }
}
