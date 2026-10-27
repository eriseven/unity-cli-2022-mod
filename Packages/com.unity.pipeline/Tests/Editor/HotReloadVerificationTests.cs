using System;
using System.Reflection;
using NUnit.Framework;
using Unity.Pipeline.MethodReload;
using UnityEngine;

namespace Unity.Pipeline.Tests.Editor
{
    /// <summary>
    /// Simple verification tests for hot reload infrastructure.
    /// These tests verify the core functionality works correctly in isolation.
    /// </summary>
    class MethodReloadVerificationTests
    {
        [SetUp]
        public void SetUp()
        {
            // Ensure clean state for each test
            MethodReloadRegistry.ClearAllForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            MethodReloadRegistry.ClearAllForTesting();
        }

        [Test]
        public void MethodReloadRegistry_EmptyState_HasZeroStats()
        {
            // Act
            var stats = MethodReloadRegistry.GetStats();

            // Assert
            Assert.That(stats.ReloadableMethodCount, Is.EqualTo(0));
            Assert.That(stats.ActiveOverrideCount, Is.EqualTo(0));
            Assert.That(stats.LoadedTypeCount, Is.EqualTo(0));
        }

        [Test]
        public void MethodReloadRegistry_RegisterSingleMethod_CorrectCount()
        {
            // Arrange
            var testType = typeof(SimpleTestClass);
            var testMethod = testType.GetMethod("TestMethod");
            var attribute = new MethodReloadAttribute();

            // Act
            MethodReloadRegistry.RegisterReloadableMethod(testMethod, attribute);

            // Assert
            var stats = MethodReloadRegistry.GetStats();
            Assert.That(stats.ReloadableMethodCount, Is.EqualTo(1));
            Assert.That(stats.ReloadableMethodIds.Count, Is.EqualTo(1));
            Assert.That(stats.ReloadableMethodIds.Contains("SimpleTestClass.TestMethod"));
        }

        [Test]
        public void MethodReloadRegistry_RegisterMultipleMethods_CorrectCount()
        {
            // Arrange
            var testType = typeof(SimpleTestClass);
            var method1 = testType.GetMethod("TestMethod");
            var method2 = testType.GetMethod("AnotherTestMethod");

            // Act
            MethodReloadRegistry.RegisterReloadableMethod(method1, new MethodReloadAttribute());
            MethodReloadRegistry.RegisterReloadableMethod(method2, new MethodReloadAttribute { Id = "CustomId" });

            // Assert
            var stats = MethodReloadRegistry.GetStats();
            Assert.That(stats.ReloadableMethodCount, Is.EqualTo(2));
            Assert.That(stats.ReloadableMethodIds.Count, Is.EqualTo(2));
            Assert.That(stats.ReloadableMethodIds.Contains("SimpleTestClass.TestMethod"));
            Assert.That(stats.ReloadableMethodIds.Contains("CustomId"));
        }

        [Test]
        public void MethodReloadRegistry_TryInvokeWithoutOverride_ReturnsFalse()
        {
            // Arrange
            var testType = typeof(SimpleTestClass);
            var testMethod = testType.GetMethod("TestMethod");
            MethodReloadRegistry.RegisterReloadableMethod(testMethod, new MethodReloadAttribute());

            var testInstance = new SimpleTestClass();

            // Act
            var result = MethodReloadRegistry.TryInvokeMethodReload("SimpleTestClass.TestMethod", testInstance);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void MethodReloadRegistry_ClearForTesting_RemovesAllState()
        {
            // Arrange - add some state
            var testType = typeof(SimpleTestClass);
            var testMethod = testType.GetMethod("TestMethod");
            MethodReloadRegistry.RegisterReloadableMethod(testMethod, new MethodReloadAttribute());

            // Verify state exists
            var statsBefore = MethodReloadRegistry.GetStats();
            Assert.That(statsBefore.ReloadableMethodCount, Is.EqualTo(1));

            // Act
            MethodReloadRegistry.ClearAllForTesting();

            // Assert
            var statsAfter = MethodReloadRegistry.GetStats();
            Assert.That(statsAfter.ReloadableMethodCount, Is.EqualTo(0));
            Assert.That(statsAfter.ActiveOverrideCount, Is.EqualTo(0));
            Assert.That(statsAfter.LoadedTypeCount, Is.EqualTo(0));
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

            public int AnotherTestMethod(int input)
            {
                return input * 2;
            }
        }
    }
}