using NUnit.Framework;
using Unity.Pipeline.Editor;
using UnityEditor;

namespace Unity.Pipeline.Tests.Editor
{
    class RuntimePipelineConfigEditorTests
    {
        [TestCase(false, false, MessageType.Info, "Runtime Pipeline lets a Player build be remotely controlled",
            TestName = "GetStatusMessage_DisabledInBuilds_ReturnsFeatureInfo")]
        [TestCase(true, false, MessageType.Warning, "development and profiled builds",
            TestName = "GetStatusMessage_EnabledWithDevelopmentBuildOn_ReturnsWarning")]
        [TestCase(false, true, MessageType.Warning, "release builds",
            TestName = "GetStatusMessage_EnabledWithDevelopmentBuildOff_ReturnsExcludedInfo")]
        public void GetStatusMessage_ReturnsExpectedMessage(
            bool enableInBuilds, bool developmentBuildCurrentlyOn, MessageType expectedType, string expectedSubstring)
        {
            var status = RuntimePipelineConfigEditor.GetStatusMessage(enableInBuilds, developmentBuildCurrentlyOn);

            Assert.AreEqual(expectedType, status.Type);
            StringAssert.Contains(expectedSubstring, status.Message);
        }
    }
}
