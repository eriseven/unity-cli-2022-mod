using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Compilation;
using Unity.Pipeline.Editor;
using Unity.Pipeline.Editor.Commands;
using Unity.Pipeline.Editor.Console;
using Unity.Pipeline.Runtime.Commands;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor
{
    class Unity2022Pipeline080Tests
    {
        sealed class BytesScript : IlInterpreter.IScript
        {
            public BytesScript(byte[] bytes) { Il = bytes; }
            public string Name => "Port080Probe";
            public ReadOnlyMemory<byte> Il { get; }
        }

        sealed class Handler
        {
            public int Offset = 40;
            public int Add([CliArg("value", "Value to add")] int value = 2) => Offset + value;
        }

        [Test]
        public void DynamicCommand_InvokesBoundInstanceAndSurvivesDiscoverySwap()
        {
            const string name = "port080_dynamic_probe";
            CommandRegistry.RegisterCommand("global", name, "Port regression probe", new Func<int, int>(new Handler().Add));
            try
            {
                CommandRegistry.SetDiscovery(new TypeCacheCommandDiscovery());
                using var server = new PipelineTestServer();
                var response = server.Execute(name, new { value = 3 });
                Assert.IsTrue(response.IsSuccess, response.Error);
                Assert.AreEqual(43, response.JsonResponse["result"].Value<int>());
                Assert.Throws<InvalidOperationException>(() => CommandRegistry.RegisterCommand("global", name, "duplicate", new Func<int>(() => 0)));
            }
            finally { CommandRegistry.UnregisterCommand("global", name); }
            Assert.IsFalse(CommandRegistry.DiscoverCommands().Any(c => c.Name == name));
        }

        [Test]
        public void Attributes_AreInUnconstrainedAssembly()
        {
            Assert.AreEqual("Unity.Pipeline.Attributes", typeof(CliCommandAttribute).Assembly.GetName().Name);
            Assert.AreEqual(typeof(CliCommandAttribute).Assembly, typeof(CliArgAttribute).Assembly);
            Assert.AreEqual(typeof(CliCommandAttribute).Assembly, typeof(Unity.Pipeline.MethodReload.MethodReloadAttribute).Assembly);
        }

        [TestCase("script recompile_status")]
        [TestCase("build status")]
        [TestCase("lighting bake_status")]
        [TestCase("navmesh bake_status")]
        [TestCase("occlusion bake_status")]
        public void StatusEndpoint_ReturnsJsonObject(string command)
        {
            using var server = new PipelineTestServer();
            var response = server.Execute(command);
            Assert.IsTrue(response.IsSuccess, response.Error);
            Assert.AreEqual(JTokenType.Object, response.JsonResponse["result"].Type);
            Assert.IsNotNull(response.JsonResponse["result"]["status"]);
        }

        [Test]
        public void WaitCondition_ReadsEditorStateAndRejectsUnknownMember()
        {
            var condition = new WaitConditionInput { Member = "UnityEditor.EditorApplication.isPlaying" };
            Assert.AreEqual(false, WaitConditionEvaluator.ReadMember(condition, out var memberType));
            Assert.AreEqual(typeof(bool), memberType);
            Assert.IsTrue(WaitConditionEvaluator.Evaluate("equals", false, new JValue(false), memberType, null, false, out var error));
            Assert.IsNull(error);
            condition.Member = "UnityEditor.EditorApplication.noSuchMember080";
            var exception = Assert.Throws<WaitEvaluationException>(() => WaitConditionEvaluator.ReadMember(condition, out _));
            Assert.IsTrue(exception.IsPermanent);
        }

        [Test]
        public void ConsoleGroundTruth_UsesNativeCountsAndBackfillsLostEntry()
        {
            Assert.IsTrue(EditorConsoleEntries.TryGetCounts(out var before, out _, out _));
            var marker = "port080_backfill_" + Guid.NewGuid().ToString("N");
            LogAssert.Expect(LogType.Error, marker);
            Debug.LogError(marker);
            Assert.IsTrue(EditorConsoleEntries.TryGetCounts(out var after, out _, out _));
            Assert.GreaterOrEqual(after, before + 1);
            Unity.Pipeline.Console.ConsoleLogCapture.Buffer.Clear();
            EditorConsoleGroundTruth.ResetForTests();
            EditorConsoleGroundTruth.SampleForTests();
            var result = ConsoleCommand.Get(level: "error");
            Assert.IsTrue(result.GroundTruth.Seeded);
            Assert.IsTrue(result.Entries.Any(e => e.Seeded && e.Message.Contains(marker)));
            Assert.AreEqual(UnityEditor.EditorUtility.scriptCompilationFailed, result.GroundTruth.CompilationFailed);
        }

        [TestCase("class Probe { public int Add(int n) => n + 2; public static object Run() { System.Func<int,int> add = new Probe().Add; return add(40); } }", 42)]
        [TestCase("class Probe { public static object Run() { return new string(new char[] { 'o', 'k' }); } }", "ok")]
        public void Interpreter080Features_ExecuteWithUnity2022Compiler(string body, object expected)
        {
            var compile = RoslynCompilationService.Compile(new CompilationRequest
            {
                SourceCode = body,
                AssemblyName = "Port080_" + Guid.NewGuid().ToString("N"),
                SkipLoad = true
            });
            Assert.IsTrue(compile.Success, string.Join("; ", compile.Diagnostics.Select(d => d.Message)));
            using var interpreter = new IlInterpreter.Interpreter.ScriptInterpreter(IlInterpreterHostBindings.CreateStandard());
            interpreter.Load(new BytesScript(compile.AssemblyBytes));
            Assert.AreEqual(expected, interpreter.Invoke("Run"));
        }
    }
}
