using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Pipeline.Compilation;
using Unity.Pipeline.MethodReload;
using Unity.Pipeline.Runtime.Commands;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Integration tests for hot reload CLI commands.
    /// Tests the complete workflow: CLI commands → compiler → registry → runtime behavior.
    /// </summary>
    class MethodReloadCommandTests
    {
        [SetUp]
        public void SetUp()
        {
            // Clear hot reload state
            MethodReloadRegistry.ClearAllForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            MethodReloadRegistry.ClearAllForTesting();
        }

        [Test]
        public void MethodReloadStatus_ReturnsStats()
        {
            // Arrange - register a test method
            var method = typeof(SimpleTestClass).GetMethod("TestMethod");
            MethodReloadRegistry.RegisterReloadableMethod(method, new MethodReloadAttribute());

            // Act
            var result = MethodReloadCommands.Status();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Contains.Substring("Reloadable Methods: 1"));
            Assert.That(result.Message, Contains.Substring("Active Overrides: 0"));
        }

        [Test]
        public void ReloadFile_WithInvalidFilename_ReturnsBadRequest()
        {
            // Act
            var result = MethodReloadCommands.Reload("");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Bad Request"));
            Assert.That(result.ErrorDetails, Contains.Substring("cannot be empty"));
        }

        [Test]
        public void ReloadFile_WithNonExistentFile_ReturnsFileNotFound()
        {
            // Act
            var result = MethodReloadCommands.Reload("NonExistentFile.cs");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("File Not Found"));
            Assert.That(result.ErrorDetails, Contains.Substring("Could not locate source file"));
        }

        [Test]
        public void ReloadFileInPlace_NonExistentFile_ReturnsFileNotFound()
        {
            var result = MethodReloadCommands.Reload("NonExistentInPlaceFile.cs");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("File Not Found"));
        }

        [Test]
        public void MethodReloadResponse_Success_CreatesCorrectResponse()
        {
            // Act
            var response = MethodReloadResponse.CmdSuccess("test-assembly", "Test message", new System.Collections.Generic.List<string> { "method1", "method2" }, 1500);

            // Assert
            Assert.That(response.Success, Is.True);
            Assert.That(response.AssemblyName, Is.EqualTo("test-assembly"));
            Assert.That(response.Message, Is.EqualTo("Test message"));
            Assert.That(response.Items.Count, Is.EqualTo(2));
            Assert.That(response.ExecutionTimeMs, Is.EqualTo(1500));
        }

        [Test]
        public void MethodReloadResponse_Failure_CreatesCorrectResponse()
        {
            // Act
            var response = MethodReloadResponse.CmdFailure("Test Error", "Detailed error message", 2000);

            // Assert
            Assert.That(response.Success, Is.False);
            Assert.That(response.Error, Is.EqualTo("Test Error"));
            Assert.That(response.ErrorDetails, Is.EqualTo("Detailed error message"));
            Assert.That(response.ExecutionTimeMs, Is.EqualTo(2000));
        }

        /// <summary>
        /// Simple test class for hot reload testing.
        /// </summary>
        public class SimpleTestClass
        {
            public int value;

            public void TestMethod()
            {
                value = 42;
            }
        }
    }
}