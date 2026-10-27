using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Pipeline.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Smoke tests for <see cref="EditorMethodReloadWatcher"/> state machine: Start/Stop flips IsWatching
    /// and restores the auto-refresh pref. Full file-change → reload behavior is covered by the
    /// InPlaceReloadProcessor tests (MethodReloadInPlaceTests); here we tolerate the initial apply's log
    /// since a throwaway temp file has no real [MethodReload] override.
    /// </summary>
    class EditorMethodReloadWatcherTests
    {
        private const string AutoRefreshPref = "kAutoRefresh";          // pre-2021.2 bool
        private const string AutoRefreshModePref = "kAutoRefreshMode";  // 2021.2+ enum (0/1/2)
        private int _savedAutoRefresh;
        private int _savedAutoRefreshMode;
        private string _tempFile;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            if (EditorMethodReloadWatcher.IsWatching) EditorMethodReloadWatcher.StopWatch();
            _savedAutoRefresh = EditorPrefs.GetInt(AutoRefreshPref, 1);
            _savedAutoRefreshMode = EditorPrefs.GetInt(AutoRefreshModePref, 1);
            // Known starting state; mode deliberately non-1 to prove Stop restores the value, not just "on".
            EditorPrefs.SetInt(AutoRefreshPref, 1);
            EditorPrefs.SetInt(AutoRefreshModePref, 2);
            _tempFile = Path.Combine(Path.GetTempPath(), $"IlInterpreterWatchSmoke_{Guid.NewGuid():N}.cs");
            File.WriteAllText(_tempFile, "public class WatchSmokeProbe {}");
            _tempDir = Path.Combine(Path.GetTempPath(), $"IlInterpreterWatchDir_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (EditorMethodReloadWatcher.IsWatching) EditorMethodReloadWatcher.StopWatch();
            EditorPrefs.SetInt(AutoRefreshPref, _savedAutoRefresh);
            EditorPrefs.SetInt(AutoRefreshModePref, _savedAutoRefreshMode);
            try { if (_tempFile != null && File.Exists(_tempFile)) File.Delete(_tempFile); } catch { }
            try { if (_tempDir != null && Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); } catch { }
        }

        // The single-file initial apply on a throwaway file with no [MethodReload] override logs a graceful failure.
        static void ExpectInitialApplyError() =>
            LogAssert.Expect(LogType.Error, new Regex(@"\[MethodReloadWatch\] Reload failed"));

        [Test]
        public void StartWatch_File_SetsStateAndDisablesAutoRefresh()
        {
            ExpectInitialApplyError();
            EditorMethodReloadWatcher.StartWatch(_tempFile, isFolder: false);

            Assert.IsTrue(EditorMethodReloadWatcher.IsWatching, "should be watching after StartWatch");
            Assert.AreEqual(Path.GetFullPath(_tempFile), EditorMethodReloadWatcher.WatchPath);
            Assert.IsFalse(EditorMethodReloadWatcher.IsFolder);
            Assert.AreEqual(0, EditorPrefs.GetInt(AutoRefreshPref, 1), "auto-refresh (legacy key) should be off while watching");
            Assert.AreEqual(0, EditorPrefs.GetInt(AutoRefreshModePref, 1), "auto-refresh mode (2021.2+ key) should be off while watching");
        }

        [Test]
        public void StartWatch_Folder_SetsFolderStateNoInitialApply()
        {
            // Folder mode must NOT bulk-apply on start, so there is no initial-apply error to expect.
            EditorMethodReloadWatcher.StartWatch(_tempDir, isFolder: true);

            Assert.IsTrue(EditorMethodReloadWatcher.IsWatching);
            Assert.IsTrue(EditorMethodReloadWatcher.IsFolder, "should be a folder watch");
            Assert.AreEqual(Path.GetFullPath(_tempDir), EditorMethodReloadWatcher.WatchPath);
        }

        [Test]
        public void StopWatch_ClearsStateAndRestoresAutoRefresh()
        {
            EditorMethodReloadWatcher.StartWatch(_tempDir, isFolder: true);
            EditorMethodReloadWatcher.StopWatch();

            Assert.IsFalse(EditorMethodReloadWatcher.IsWatching, "should not be watching after StopWatch");
            Assert.IsNull(EditorMethodReloadWatcher.WatchPath);
            Assert.AreEqual(1, EditorPrefs.GetInt(AutoRefreshPref, 1), "auto-refresh (legacy key) should be restored on stop");
            Assert.AreEqual(2, EditorPrefs.GetInt(AutoRefreshModePref, 1), "auto-refresh mode should be restored to its prior value (2), not just re-enabled");
        }

        [Test]
        public void StartWatch_MissingPath_DoesNotWatch()
        {
            LogAssert.Expect(LogType.Error, new Regex("not found"));
            EditorMethodReloadWatcher.StartWatch(Path.Combine(Path.GetTempPath(), "does_not_exist_x9.cs"), isFolder: false);
            Assert.IsFalse(EditorMethodReloadWatcher.IsWatching);
        }
    }
}
