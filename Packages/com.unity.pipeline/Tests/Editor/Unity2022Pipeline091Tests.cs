using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Config;
using Unity.Pipeline.Editor.Commands.Build;
using Unity.Pipeline.MethodReload;
using UnityEditor;
using UnityEditor.Build;

namespace Unity.Pipeline.Tests.Editor
{
    class Unity2022Pipeline091Tests
    {
        [CliCommand("port091.probe.answer", "Dotted command name regression probe")]
        public static int DottedProbe() => 42;

        [CliCommand("Inferred command name regression probe")]
        public static int InferredProbe() => 43;

        [Test]
        public void DottedAndInferredCommands_AreDiscoveredAndInvoked()
        {
            using var server = new PipelineTestServer();
            var dotted = server.Execute("port091.probe answer");
            Assert.IsTrue(dotted.IsSuccess, dotted.Error);
            Assert.AreEqual(42, dotted.JsonResponse["result"].Value<int>());
            var inferred = CommandRegistry.DiscoverCommands().Single(c => c.Method.DeclaringType == GetType() && c.Method.Name == nameof(InferredProbe));
            Assert.AreEqual("unity.pipeline.tests.editor.unity2022pipeline091tests", inferred.Group);
            Assert.AreEqual("inferred_probe", inferred.Name);
            Assert.AreEqual(43, server.Execute(inferred.FullName).JsonResponse["result"].Value<int>());
        }

        [Test]
        public void GroupedDynamicCommand_CanBeRegisteredInvokedAndRemoved()
        {
            CommandRegistry.RegisterCommand("port091.dynamic", "answer", "Probe", new Func<int>(() => 44));
            try
            {
                using var server = new PipelineTestServer();
                Assert.AreEqual(44, server.Execute("port091.dynamic answer").JsonResponse["result"].Value<int>());
            }
            finally { CommandRegistry.UnregisterCommand("port091.dynamic", "answer"); }
            Assert.IsFalse(CommandRegistry.DiscoverCommands().Any(c => c.FullName == "port091.dynamic answer"));
        }

        [Test]
        public void BuildProfiles_ReportUnavailableOnUnity2022()
        {
            var result = JObject.FromObject(BuildProfileCommands.ListPlatforms());
            Assert.AreEqual("feature_unavailable", result["code"].Value<string>());
        }

        [TestCase(BuildTargetGroup.Android)]
        [TestCase(BuildTargetGroup.iOS)]
        public void NonStandaloneBuildTarget_IgnoresStandaloneSubtarget(BuildTargetGroup group)
        {
            Assert.AreEqual(NamedBuildTarget.FromBuildTargetGroup(group),
                PipelineBuildSymbols.TargetFor(group, StandaloneBuildSubtarget.Server));
        }

        [Test]
        public void BuildSymbols_KeepDevelopmentAndReleaseIndependent()
        {
            var symbols = PipelineBuildSymbols.Add("OTHER", PipelineBuildSymbols.Development);
            Assert.IsFalse(PipelineBuildSymbols.Contains(symbols, PipelineBuildSymbols.Release));
            symbols = PipelineBuildSymbols.Add(symbols, PipelineBuildSymbols.Release);
            symbols = PipelineBuildSymbols.Remove(symbols, PipelineBuildSymbols.Development);
            Assert.IsTrue(PipelineBuildSymbols.Contains(symbols, PipelineBuildSymbols.Release));
            Assert.IsFalse(PipelineBuildSymbols.Contains(symbols, PipelineBuildSymbols.Development));
            StringAssert.Contains("OTHER", symbols);
        }

        [Test]
        public void MethodReloadAttributes_KeepUnconstrainedAssemblyBoundary()
        {
            Assert.AreEqual(typeof(CliCommandAttribute).Assembly, typeof(MethodReloadAttribute).Assembly);
            Assert.AreEqual(typeof(MethodReloadAttribute).Assembly, typeof(OnMethodReloadedAttribute).Assembly);
        }

        [Test]
        public void EditorConfig_PreservesDisabledRepushFromCodeReloadSetting()
        {
            var config = UnityEngine.ScriptableObject.CreateInstance<Unity.Pipeline.Editor.EditorPipelineConfig>();
            try
            {
                config.OverwriteFromJson("{\"m_CodeReloadRepushOnConnect\":false}");
                Assert.IsFalse(config.MethodReloadRepushOnConnect);
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }

        [Test]
        public void CompilerPlugins_UseSameBuildGatesAsRuntimeAssembly()
        {
            const string root = "Packages/com.unity.pipeline/Runtime/";
            var constraints = JObject.Parse(File.ReadAllText(root + "Unity.Pipeline.asmdef"))["defineConstraints"].Values<string>().ToArray();
            foreach (var file in Directory.GetFiles(root + "Plugins/CodeAnalysis", "*.dll"))
            {
                var importer = (PluginImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
                CollectionAssert.AreEquivalent(constraints, importer.DefineConstraints, file);
            }
        }

        [Test]
        public void ObsoleteCommand_ReturnsMigrationGuidance()
        {
            using var server = new PipelineTestServer();
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                new System.Text.RegularExpressions.Regex("ExecuteCommandByName: No command named 'get_scene_hierarchy'"));
            var response = server.Execute("get_scene_hierarchy");
            Assert.IsFalse(response.IsSuccess);
            StringAssert.Contains("scene", response.RawResponse);
            StringAssert.Contains("hierarchy", response.RawResponse);
        }
    }
}
