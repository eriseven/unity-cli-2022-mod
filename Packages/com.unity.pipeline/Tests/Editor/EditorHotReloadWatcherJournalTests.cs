using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Pipeline.Editor;
using UnityEditor;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Tests for the hot-reload watcher's applied-files journal: the SessionState-backed record of
    /// files whose overrides were applied in-editor, used to re-apply them after the domain reload
    /// that entering play mode triggers (which wipes MethodReloadRegistry's in-memory overrides).
    /// </summary>
    class EditorMethodReloadWatcherJournalTests
    {
        private string m_TempDir;

        [SetUp]
        public void SetUp()
        {
            EditorMethodReloadWatcher.ClearAppliedFilesJournal();
            m_TempDir = FileUtil.GetUniqueTempPathInProject();
            Directory.CreateDirectory(m_TempDir);
        }

        [TearDown]
        public void TearDown()
        {
            EditorMethodReloadWatcher.ClearAppliedFilesJournal();
            if (Directory.Exists(m_TempDir))
                Directory.Delete(m_TempDir, recursive: true);
        }

        private string CreateTempScript(string name)
        {
            var path = Path.GetFullPath(Path.Combine(m_TempDir, name));
            File.WriteAllText(path, "// test");
            return path;
        }

        [Test]
        public void Journal_RoundTripsAppliedFiles()
        {
            var a = CreateTempScript("A.cs");
            var b = CreateTempScript("B.cs");

            EditorMethodReloadWatcher.JournalAppliedFile(a);
            EditorMethodReloadWatcher.JournalAppliedFile(b);

            var journaled = EditorMethodReloadWatcher.GetJournaledAppliedFiles();
            CollectionAssert.AreEquivalent(new[] { a, b }, journaled);
        }

        [Test]
        public void Journal_DeduplicatesRepeatedApplies()
        {
            var a = CreateTempScript("A.cs");

            EditorMethodReloadWatcher.JournalAppliedFile(a);
            EditorMethodReloadWatcher.JournalAppliedFile(a);

            Assert.AreEqual(1, EditorMethodReloadWatcher.GetJournaledAppliedFiles().Length);
        }

        [Test]
        public void ClearJournal_RemovesAllEntries()
        {
            EditorMethodReloadWatcher.JournalAppliedFile(CreateTempScript("A.cs"));

            EditorMethodReloadWatcher.ClearAppliedFilesJournal();

            Assert.IsEmpty(EditorMethodReloadWatcher.GetJournaledAppliedFiles());
        }

        [Test]
        public void ComputeResumeReapplies_NotPlaying_IsEmpty()
        {
            // Outside play mode the domain reload came from a real recompile, so the compiled code
            // already matches disk — nothing must be re-applied.
            var a = CreateTempScript("A.cs");

            var reapplies = EditorMethodReloadWatcher.ComputeResumeReapplies(
                isPlaying: false, journaledFiles: new[] { a });

            Assert.IsEmpty(reapplies);
        }

        [Test]
        public void ComputeResumeReapplies_Playing_ReturnsOnlyExistingFiles()
        {
            var existing = CreateTempScript("A.cs");
            var deleted = CreateTempScript("B.cs");
            File.Delete(deleted);

            var reapplies = EditorMethodReloadWatcher.ComputeResumeReapplies(
                isPlaying: true, journaledFiles: new[] { existing, deleted });

            Assert.AreEqual(new[] { existing }, reapplies.ToArray());
        }
    }
}
