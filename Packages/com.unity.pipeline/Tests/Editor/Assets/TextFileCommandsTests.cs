using NUnit.Framework;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Editor.Commands.Assets;
using UnityEditor;

namespace Unity.Pipeline.Tests.Editor.Assets
{
    /// <summary>
    /// Tests for read_text_file / write_text_file (CLI-191), direct and via the isolated
    /// <see cref="PipelineTestServer"/>. Covers round-tripping content, the overwrite-confirm guard, and
    /// the sandbox guard (out-of-project writes rejected).
    /// </summary>
    class TextFileCommandsTests
    {
        private const string Root = "Assets/__CLI191TextTest";

        [TearDown]
        public void TearDown()
        {
            ProjectPaths.ResetAuthoringRoot();
            if (AssetDatabase.IsValidFolder(Root))
            {
                AssetDatabase.DeleteAsset(Root);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        #region Direct

        [Test]
        public void WriteThenRead_RoundTrips()
        {
            var path = Root + "/Notes/hello.txt";
            const string content = "hello cli-191";

            var write = TextFileCommands.WriteText(path, content);
            Assert.AreEqual(path, write.AssetPath);

            var read = TextFileCommands.ReadText(path);
            Assert.AreEqual(content, read.Contents);
            Assert.AreEqual(System.Text.Encoding.UTF8.GetByteCount(content), read.Bytes);
        }

        [Test]
        public void WriteTextFile_OverwriteWithoutConfirm_Throws()
        {
            var path = Root + "/Notes/dup.txt";
            TextFileCommands.WriteText(path, "first");

            Assert.Throws<System.ArgumentException>(
                () => TextFileCommands.WriteText(path, "second"),
                "Overwriting an existing file without confirm should be rejected");

            var ok = TextFileCommands.WriteText(path, "second", confirm: true);
            Assert.AreEqual(path, ok.AssetPath);
            Assert.AreEqual("second", TextFileCommands.ReadText(path).Contents);
        }

        [Test]
        public void WriteTextFile_OutOfProject_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => TextFileCommands.WriteText("../escape.txt", "x"));
            Assert.Throws<System.ArgumentException>(() => TextFileCommands.WriteText("Assets/../escape.txt", "x"));
        }

        [Test]
        public void ReadTextFile_Missing_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => TextFileCommands.ReadText(Root + "/does-not-exist.txt"));
        }

        #endregion

        #region ViaClient

        [Test]
        public void WriteTextFile_ViaClient_Succeeds()
        {
            using (var server = new PipelineTestServer())
            {
                var path = Root + "/ViaClient/wire.txt";
                var response = server.Execute("asset write_text", new { path, contents = "over the wire" });

                Assert.IsTrue(response.IsSuccess, $"write_text_file should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.AreEqual("over the wire", TextFileCommands.ReadText(path).Contents);
            }
        }

        [Test]
        public void ReadTextFile_ViaClient_Succeeds()
        {
            var path = Root + "/ViaClient/readable.txt";
            TextFileCommands.WriteText(path, "readable content");

            using (var server = new PipelineTestServer())
            {
                var response = server.Execute("asset read_text", new { path });

                Assert.IsTrue(response.IsSuccess, $"read_text_file should succeed: {response.Error}");
                Assert.IsTrue(response.HasValidJson, "Response should have valid JSON");
                Assert.IsTrue(response.JsonResponse.ContainsKey("result"), "Should have result field");
            }
        }

        #endregion
    }
}
