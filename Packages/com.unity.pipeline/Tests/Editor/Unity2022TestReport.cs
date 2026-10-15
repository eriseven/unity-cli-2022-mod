using System.IO;
using NUnit.Framework.Interfaces;
using UnityEditor;
using UnityEngine.TestRunner;

[assembly: TestRunCallback(typeof(Unity.Pipeline.Tests.Editor.Unity2022TestReport))]

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Optional NUnit XML export for validation runs that cross a Unity 2022 domain reload.
    /// The Test Framework re-registers this callback on resume, independently of Pipeline's
    /// in-memory HTTP result collector. Set the SessionState key to an absolute report path.
    /// </summary>
    public sealed class Unity2022TestReport : ITestRunCallback
    {
        public const string OutputPathKey = "Unity.Pipeline.Tests.XmlReportPath";

        public void RunStarted(ITest testsToRun) { }
        public void TestStarted(ITest test) { }
        public void TestFinished(ITestResult result) { }

        public void RunFinished(ITestResult result)
        {
            var path = SessionState.GetString(OutputPathKey, "");
            if (!string.IsNullOrEmpty(path))
                File.WriteAllText(path, result.ToXml(true).OuterXml);
        }
    }
}
