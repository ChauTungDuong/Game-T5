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
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
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

        [Test]
        public void ConfigureProject_CreatesMissingRequirementsInLoadedMainAndPreservesActiveScene()
        {
            var mainScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            DestroyRequiredRoots(mainScene);
            Assert.That(EditorSceneManager.SaveScene(mainScene), Is.True);

            var unrelatedScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Assert.That(EditorSceneManager.SaveScene(unrelatedScene, UnrelatedScenePath), Is.True);
            unrelatedScene = SceneManager.GetSceneByPath(UnrelatedScenePath);
            Assert.That(unrelatedScene.IsValid() && unrelatedScene.isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(UnrelatedScenePath),
                "The unrelated additive scene must be active before invoking ConfigureProject.");

            InvokeConfigureProject();

            var mainRoots = mainScene.GetRootGameObjects();
            var unrelatedRoots = unrelatedScene.GetRootGameObjects();

            Assert.That(CountComponentsInScene<Camera>(mainRoots), Is.EqualTo(1),
                "A missing camera must be created in Main even when another scene is active.");
            Assert.That(CountComponentsInScene<Canvas>(mainRoots), Is.EqualTo(1),
                "A missing Canvas must be created in Main even when another scene is active.");
            Assert.That(CountComponentsInScene<EventSystem>(mainRoots), Is.EqualTo(1),
                "A missing EventSystem must be created in Main even when another scene is active.");
            Assert.That(CountComponentsInScene<Camera>(unrelatedRoots), Is.EqualTo(0),
                "ConfigureProject must not add a camera to the active unrelated scene.");
            Assert.That(CountComponentsInScene<Canvas>(unrelatedRoots), Is.EqualTo(0),
                "ConfigureProject must not add a Canvas to the active unrelated scene.");
            Assert.That(CountComponentsInScene<EventSystem>(unrelatedRoots), Is.EqualTo(0),
                "ConfigureProject must not add an EventSystem to the active unrelated scene.");
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(UnrelatedScenePath),
                "ConfigureProject must preserve the caller's active scene.");
        }

        private static void DestroyRequiredRoots(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<Camera>(true) != null ||
                    root.GetComponentInChildren<Canvas>(true) != null ||
                    root.GetComponentInChildren<EventSystem>(true) != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void ConfigureProject_BuildsPlayableSceneAndKeepsGameplayRootsInMain()
        {
            var main = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var unrelated = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Assert.That(EditorSceneManager.SaveScene(unrelated, UnrelatedScenePath), Is.True);
            InvokeConfigureProject();
            InvokeConfigureProject();
            var sessions = main.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CoreGuard.GameSession>(true)).ToArray();
            Assert.That(sessions.Length, Is.EqualTo(1), "Main must contain one playable session after repeated configuration.");
            var session = sessions[0];
            Assert.That(session.Player, Is.Not.Null);
            Assert.That(session.Core, Is.Not.Null);
            Assert.That(session.Motor, Is.Not.Null);
            Assert.That(session.Spawner.Prefab, Is.Not.Null);
            Assert.That(session.Spawner.Gates.Length, Is.EqualTo(4));
            Assert.That(session.Motor.Turret, Is.Not.Null);
            Assert.That(session.gameObject.scene.path, Is.EqualTo(MainScenePath));
            Assert.That(session.Player.gameObject.scene.path, Is.EqualTo(MainScenePath));
            Assert.That(session.Core.gameObject.scene.path, Is.EqualTo(MainScenePath));
            Assert.That(session.Spawner.gameObject.scene.path, Is.EqualTo(MainScenePath));
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(UnrelatedScenePath));
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
