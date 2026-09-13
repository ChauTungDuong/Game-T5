using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoreGuard.Tests.Editor
{
    public sealed class BuildDemoConfigurationTests
    {
        private const string MainScenePath = "Assets/_Game/Scenes/Main.unity";
        private const string UnrelatedScenePath = "Assets/_Game/Tests/EditMode/ConfigureProjectUnrelated.unity";
        private const string SentinelName = "ConfigureProject Sentinel";

        private byte[] _originalMainSceneBytes;
        private EditorBuildSettingsScene[] _originalBuildScenes;

        [SetUp]
        public void SetUp()
        {
            _originalMainSceneBytes = File.ReadAllBytes(MainScenePath);
            _originalBuildScenes = EditorBuildSettings.scenes;
        }

        [TearDown]
        public void TearDown()
        {
            File.WriteAllBytes(MainScenePath, _originalMainSceneBytes);
            EditorBuildSettings.scenes = _originalBuildScenes;
            AssetDatabase.DeleteAsset(UnrelatedScenePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }

        [Test]
        public void ConfigureProject_PreservesSceneContentAndBuildEntriesWithoutDuplicates()
        {
            var mainScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            new GameObject(SentinelName);
            Assert.That(EditorSceneManager.SaveScene(mainScene), Is.True);

            var unrelatedScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.That(EditorSceneManager.SaveScene(unrelatedScene, UnrelatedScenePath), Is.True);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, false),
                new EditorBuildSettingsScene(UnrelatedScenePath, true),
                new EditorBuildSettingsScene(MainScenePath, true),
            };

            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            InvokeConfigureProject();

            var configuredScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var roots = configuredScene.GetRootGameObjects();
            var configuredBuildScenes = EditorBuildSettings.scenes;
            var mainEntries = configuredBuildScenes.Where(scene => scene.path == MainScenePath).ToArray();

            Assert.That(roots.Any(root => root.name == SentinelName), Is.True,
                "Existing Main scene content must survive reconfiguration.");
            Assert.That(
                configuredBuildScenes.Any(scene => scene.path == UnrelatedScenePath && scene.enabled),
                Is.True,
                "Unrelated valid build-scene entries must survive reconfiguration.");
            Assert.That(mainEntries.Length, Is.EqualTo(1),
                "Main must appear in build settings exactly once.");
            Assert.That(mainEntries.All(scene => scene.enabled), Is.True,
                "The single Main build entry must be enabled.");
            Assert.That(CountComponentsInScene<Camera>(roots), Is.EqualTo(1),
                "Reconfiguration must not duplicate the required camera.");
            Assert.That(CountComponentsInScene<Canvas>(roots), Is.EqualTo(1),
                "Reconfiguration must not duplicate the required Canvas.");
            Assert.That(CountComponentsInScene<EventSystem>(roots), Is.EqualTo(1),
                "Reconfiguration must not duplicate the required EventSystem.");
        }

        private static int CountComponentsInScene<T>(GameObject[] roots) where T : Component
        {
            return roots.Sum(root => root.GetComponentsInChildren<T>(true).Length);
        }

        private static void InvokeConfigureProject()
        {
            var buildDemoType = Type.GetType("BuildDemo, Assembly-CSharp-Editor");
            Assert.That(buildDemoType, Is.Not.Null, "BuildDemo editor type was not loaded.");

            var configureMethod = buildDemoType.GetMethod(
                "ConfigureProject",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(configureMethod, Is.Not.Null, "BuildDemo.ConfigureProject was not found.");

            configureMethod.Invoke(null, null);
        }
    }
}
