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
            Assert.That(session.Weapon, Is.Not.Null);
            Assert.That(session.Defense, Is.Not.Null);
            Assert.That(session.Demo, Is.Not.Null);
            Assert.That(session.Audio, Is.Not.Null);
            Assert.That(session.Audio.GetComponents<AudioSource>().Length, Is.EqualTo(3));
            Assert.That(session.Defense.Player, Is.Not.Null);
            Assert.That(session.Weapon.Muzzle, Is.Not.Null);
            Assert.That(session.Weapon.ProjectilePrefab, Is.Not.Null);
            Assert.That(session.Weapon.MinePrefab, Is.Not.Null);
            Assert.That(session.EnemyProjectilePrefab, Is.Not.Null);
            Assert.That(session.Spawner.Prefab, Is.Not.Null);
            Assert.That(session.Spawner.Gates.Length, Is.EqualTo(4));
            Assert.That(session.Motor.Turret, Is.Not.Null);
            Assert.That(session.Demo.Panel, Is.Not.Null);
            Assert.That(session.gameObject.scene.path, Is.EqualTo(MainScenePath));
            Assert.That(session.Player.gameObject.scene.path, Is.EqualTo(MainScenePath));
            Assert.That(session.Core.gameObject.scene.path, Is.EqualTo(MainScenePath));
            Assert.That(session.Spawner.gameObject.scene.path, Is.EqualTo(MainScenePath));
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(UnrelatedScenePath));
        }

        // Break caught: scene configuration omits shared world bars or gives the Core a player-sized bar that intersects its zone ring.
        [Test]
        public void ConfigureProject_ConfiguresDistinctSharedHealthBarsAbovePlayerAndCore()
        {
            var main = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            InvokeConfigureProject();

            var player = main.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CoreGuard.PlayerStats>(true)).Single();
            var core = main.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CoreGuard.CoreHealth>(true)).Single();
            var playerBar = player.GetComponentInChildren<CoreGuard.WorldHealthBar>(true);
            var coreBar = core.GetComponentInChildren<CoreGuard.WorldHealthBar>(true);
            var zone = core.GetComponentInChildren<CoreGuard.ForbiddenZone>(true);

            Assert.That(playerBar, Is.Not.Null);
            Assert.That(coreBar, Is.Not.Null);
            Assert.That(playerBar.transform.localPosition.y, Is.GreaterThan(0f));
            Assert.That(coreBar.transform.localPosition.y, Is.GreaterThan(0f));
            Assert.That(coreBar.transform.localPosition.y + .4f, Is.LessThan(zone.Radius));
            Assert.That(coreBar.Width, Is.GreaterThan(playerBar.Width));
            Assert.That(playerBar.FullColor, Is.EqualTo(Color.green));
            Assert.That(coreBar.FullColor, Is.EqualTo(Color.cyan));
        }

        // Break caught: a domain/scene reload loses visual references and duplicates the generated bar children.
        [Test]
        public void ConfigureProject_ReopenedSceneReusesEachHealthBarsExistingVisualChildren()
        {
            var main = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            InvokeConfigureProject();
            Assert.That(EditorSceneManager.SaveScene(main), Is.True);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            main = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            InvokeConfigureProject();

            foreach (var bar in main.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CoreGuard.WorldHealthBar>(true)))
            {
                Assert.That(bar.GetComponentsInChildren<LineRenderer>(true).Length, Is.EqualTo(2));
                Assert.That(bar.GetComponentsInChildren<TextMesh>(true).Length, Is.EqualTo(1));
            }
        }

        // Break caught: configuration replaces valid authored health-bar references instead of retaining them.
        [Test]
        public void ConfigureProject_PreservesValidAuthoredPlayerAndCoreHealthBarReferences()
        {
            var main = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var player = main.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CoreGuard.PlayerStats>(true)).Single();
            var core = main.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CoreGuard.CoreHealth>(true)).Single();
            var playerBar = new GameObject("Authored player health bar").AddComponent<CoreGuard.WorldHealthBar>();
            var coreBar = new GameObject("Authored Core health bar").AddComponent<CoreGuard.WorldHealthBar>();
            playerBar.transform.SetParent(player.transform, false);
            coreBar.transform.SetParent(core.transform, false);
            player.HealthBar = playerBar;
            core.HealthBar = coreBar;

            InvokeConfigureProject();
            InvokeConfigureProject();

            Assert.That(player.HealthBar, Is.SameAs(playerBar));
            Assert.That(core.HealthBar, Is.SameAs(coreBar));
        }

        private static int CountComponentsInScene<T>(GameObject[] roots) where T : Component
        {
            return roots.Sum(root => root.GetComponentsInChildren<T>(true).Length);
        }

        [Test]
        public void ConfigureProject_PreservesAuthoredHudReferencesAndPresentation()
        {
            var main = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var hud = main.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CoreGuard.HudPresenter>(true)).Single();
            var alternateStats = new GameObject("Authored stats", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            alternateStats.transform.SetParent(hud.transform, false);
            alternateStats.text = "Authored stats content";
            alternateStats.color = Color.magenta;
            alternateStats.rectTransform.anchoredPosition = new Vector2(37, -14);
            hud.StatsText = alternateStats;
            var alternatePanel = new GameObject("Authored result panel", typeof(RectTransform), typeof(Image));
            alternatePanel.transform.SetParent(hud.transform, false);
            var alternateRetry = new GameObject("Authored retry", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            alternateRetry.transform.SetParent(alternatePanel.transform, false);
            hud.ResultPanel = alternatePanel;
            hud.RetryButton = alternateRetry;
            var resumeRect = (RectTransform)hud.ResumeButton.transform;
            resumeRect.sizeDelta = new Vector2(333, 61);
            hud.ResumeButton.GetComponent<Image>().color = Color.yellow;
            var pauseRect = (RectTransform)hud.PausePanel.transform;
            pauseRect.sizeDelta = new Vector2(601, 351);
            hud.PausePanel.GetComponent<Image>().color = Color.blue;
            hud.StartPanel.SetActive(false);
            hud.PausePanel.SetActive(true);
            alternatePanel.SetActive(true);
            ((RectTransform)hud.transform).anchoredPosition = new Vector2(12, 13);
            InvokeConfigureProject();
            var configuredObjectCount = main.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length);
            InvokeConfigureProject();

            Assert.That(hud.StatsText, Is.SameAs(alternateStats), "An authored stats text reference must not be replaced.");
            Assert.That(hud.RetryButton, Is.SameAs(alternateRetry), "An authored retry button reference must not be replaced.");
            Assert.That(hud.ResultPanel, Is.SameAs(alternatePanel));
            Assert.That(alternateStats.text, Is.EqualTo("Authored stats content"));
            Assert.That(alternateStats.color, Is.EqualTo(Color.magenta));
            Assert.That(alternateStats.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(37, -14)));
            Assert.That(resumeRect.sizeDelta, Is.EqualTo(new Vector2(333, 61)));
            Assert.That(hud.ResumeButton.GetComponent<Image>().color, Is.EqualTo(Color.yellow));
            Assert.That(pauseRect.sizeDelta, Is.EqualTo(new Vector2(601, 351)));
            Assert.That(hud.PausePanel.GetComponent<Image>().color, Is.EqualTo(Color.blue));
            Assert.That(hud.StartPanel.activeSelf, Is.False);
            Assert.That(hud.PausePanel.activeSelf, Is.True);
            Assert.That(alternatePanel.activeSelf, Is.True);
            Assert.That(((RectTransform)hud.transform).anchoredPosition, Is.EqualTo(new Vector2(12, 13)));
            Assert.That(main.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length), Is.EqualTo(configuredObjectCount),
                "Existing valid HUD references must not cause replacement defaults to be created.");
        }

        [Test]
        public void ConfigureProject_RepairsOneDeletedGateAndPreservesValidGateAssignments()
        {
            var main = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var spawner = main.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CoreGuard.EnemySpawner>(true)).Single();
            var customGate = new GameObject("Authored gate").transform;
            customGate.position = new Vector3(6.2f, 1.1f, 0);
            spawner.Gates[0] = customGate;
            var retainedThird = spawner.Gates[2];
            var retainedFourth = spawner.Gates[3];
            retainedThird.position = new Vector3(-6.4f, -.8f, 0);
            UnityEngine.Object.DestroyImmediate(spawner.Gates[1].gameObject);

            InvokeConfigureProject();
            InvokeConfigureProject();

            Assert.That(spawner.Gates[1] != null, Is.True, "A deleted gate in a nonempty array must be repaired.");
            Assert.That(spawner.Gates.Length, Is.EqualTo(4));
            Assert.That(spawner.Gates[0], Is.SameAs(customGate));
            Assert.That(customGate.position, Is.EqualTo(new Vector3(6.2f, 1.1f, 0)));
            Assert.That(spawner.Gates[2], Is.SameAs(retainedThird));
            Assert.That(retainedThird.position, Is.EqualTo(new Vector3(-6.4f, -.8f, 0)));
            Assert.That(spawner.Gates[3], Is.SameAs(retainedFourth));
            Assert.That(spawner.Gates[1].position, Is.EqualTo(new Vector3(0, 3.6f, 0)));
            Assert.That(spawner.Gates[1].gameObject.scene.path, Is.EqualTo(MainScenePath));
            var gatesNamedTwo = main.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Count(t => t.name == "Gate 2");
            Assert.That(gatesNamedTwo, Is.EqualTo(1), "Repeated repair must not duplicate the recreated gate.");
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
