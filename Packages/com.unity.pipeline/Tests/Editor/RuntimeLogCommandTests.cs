using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Pipeline.Runtime.Commands;
using Unity.Pipeline.Tests;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the log command (RuntimeLogCommand), exercised directly and via PipelineClient.
    /// </summary>
    class RuntimeLogCommandTests
    {
        #region Direct

        [Test]
        public void LogMessage_InfoLevel_LogsAndReturns()
        {
            var result = RuntimeLogCommand.Write("info msg", "info");
            Assert.That(result, Does.Contain("Logged info message"));
            LogAssert.Expect(LogType.Log, new Regex(".*info msg.*"));
        }

        [Test]
        public void LogMessage_WarningLevel_LogsAndReturns()
        {
            var result = RuntimeLogCommand.Write("warn msg", "warning");
            Assert.That(result, Does.Contain("Logged warning message"));
            LogAssert.Expect(LogType.Warning, new Regex(".*warn msg.*"));
        }

        [Test]
        public void LogMessage_ErrorLevel_LogsAndReturns()
        {
            var result = RuntimeLogCommand.Write("err msg", "error");
            Assert.That(result, Does.Contain("Logged error message"));
            LogAssert.Expect(LogType.Error, new Regex(".*err msg.*"));
        }

        [Test]
        public void LogMessage_InvalidLevel_DefaultsToInfo()
        {
            var result = RuntimeLogCommand.Write("default msg", "bogus");
            Assert.That(result, Does.Contain("Logged message with default level"));
            LogAssert.Expect(LogType.Warning, new Regex(".*Unknown log level.*"));
            LogAssert.Expect(LogType.Log, new Regex(".*default msg.*"));
        }

        [Test]
        public void LogMessage_EmptyMessage_ReturnsError()
        {
            Assert.AreEqual("Error: Message cannot be empty", RuntimeLogCommand.Write("", "info"));
        }

        #endregion

        #region ViaClient

        [Test]
        public void Log_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("console write", new { message = "http log msg", level = "info" });
                Assert.IsTrue(response.IsSuccess, $"log should succeed: {response.Error}");
                LogAssert.Expect(LogType.Log, new Regex(".*http log msg.*"));
            }
        }

        #endregion
    }
}
