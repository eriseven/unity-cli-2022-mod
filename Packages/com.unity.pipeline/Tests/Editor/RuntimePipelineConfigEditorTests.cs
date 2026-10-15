using NUnit.Framework;
using Unity.Pipeline.Editor;
using UnityEditor;

namespace Unity.Pipeline.Tests.Editor
{
    class RuntimePipelineConfigEditorTests
    {
        [TestCase(false, true, MessageType.Info, "Runtime Pipeline lets a Player build be remotely controlled",
            TestName = "GetStatusMessage_DisabledInBuilds_ReturnsFeatureInfo")]
        [TestCase(true, true, MessageType.Warning, "Only ship this in development",
            TestName = "GetStatusMessage_EnabledWithDevelopmentBuildOn_ReturnsWarning")]
        [TestCase(true, false, MessageType.Info, "excluded from non-development",
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
