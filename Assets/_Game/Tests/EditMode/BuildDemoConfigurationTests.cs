using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
            Assert.That(session.Audio.UiClick, Is.Not.Null);
            Assert.That(session.Audio.Explosion, Is.Not.Null);
            Assert.That(session.Audio.Victory, Is.Not.Null);
            Assert.That(session.Audio.Defeat, Is.Not.Null);
            Assert.That(session.Audio.UiClick.name, Is.EqualTo("click"));
            Assert.That(session.Audio.Explosion.name, Is.EqualTo("explosion"));
            Assert.That(session.Audio.Victory.name, Is.EqualTo("congratulation"));
            Assert.That(session.Audio.Defeat.name, Is.EqualTo("gameover"));
            Assert.That(session.Audio.StartMuted, Is.True);
            var combatVfx = session.Player.GetComponent<CoreGuard.CombatVfxPresenter>();
            Assert.That(combatVfx, Is.Not.Null);
            Assert.That(combatVfx.ExplosionFrames, Is.Not.Null);
            Assert.That(combatVfx.ExplosionFrames.Length, Is.EqualTo(9));
            Assert.That(combatVfx.ExplosionFrames[0].name, Is.EqualTo("explosion_01"));
            Assert.That(session.Defense.Player, Is.Not.Null);
            Assert.That(session.Weapon.Muzzle, Is.Not.Null);
            Assert.That(session.Weapon.ProjectilePrefab, Is.Not.Null);
            Assert.That(session.Weapon.MinePrefab, Is.Not.Null);
            Assert.That(session.Weapon.BulletDamage, Is.EqualTo(25f));
            Assert.That(session.Weapon.RocketDamage, Is.EqualTo(55f));
            Assert.That(session.Weapon.ProjectilePrefab.transform.Find("Rocket body"), Is.Not.Null);
            Assert.That(session.EnemyProjectilePrefab, Is.Not.Null);
            Assert.That(session.Spawner.Prefab, Is.Not.Null);
            var enemyBody = session.Spawner.Prefab.transform.Find("Enemy body");
            Assert.That(enemyBody, Is.Not.Null);
            Assert.That(enemyBody.localScale, Is.EqualTo(Vector3.one * EnemyController.TankVisualScale));
            Assert.That(session.Spawner.Prefab.GetComponent<CircleCollider2D>().radius, Is.EqualTo(EnemyController.TankColliderRadius));
            var enemyTurret = session.Spawner.Prefab.transform.Find("Turret");
            Assert.That(enemyTurret, Is.Not.Null);
            Assert.That(enemyTurret.Find("Barrel"), Is.Not.Null);
            Assert.That(enemyTurret.Find("Muzzle"), Is.Not.Null);
            Assert.That(session.Spawner.Gates.Length, Is.EqualTo(4));
            Assert.That(session.InteractionCycle, Is.Not.Null);
            Assert.That(CountComponentsInScene<CoreGuard.InteractionCycleController>(main.GetRootGameObjects()), Is.EqualTo(1));
            Assert.That(session.InteractionCycle.Session, Is.SameAs(session));
            Assert.That(session.InteractionCycle.X, Is.Not.Null);
            Assert.That(session.InteractionCycle.Y, Is.Not.Null);
            Assert.That(session.InteractionCycle.Z, Is.Not.Null);
            Assert.That(session.Motor.Turret, Is.Not.Null);
            Assert.That(session.Demo.Panel, Is.Not.Null);
            Assert.That(session.gameObject.scene.path, Is.EqualTo(MainScenePath));
            Assert.That(session.Player.gameObject.scene.path, Is.EqualTo(MainScenePath));
            Assert.That(session.Core.gameObject.scene.path, Is.EqualTo(MainScenePath));
            Assert.That(session.Spawner.gameObject.scene.path, Is.EqualTo(MainScenePath));
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(UnrelatedScenePath));
        }

        [Test]
        public void ConfigureProject_RemovesDuplicateInteractionCyclesAndPreservesTheSessionOwnedOne()
        {
            var main = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            InvokeConfigureProject();
            var session = main.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CoreGuard.GameSession>(true)).Single();
            var authored = session.InteractionCycle;
            var duplicateRoot = new GameObject("Duplicate interaction cycle");
            SceneManager.MoveGameObjectToScene(duplicateRoot, main);
            duplicateRoot.AddComponent<CoreGuard.InteractionCycleController>();

            InvokeConfigureProject();

            var cycles = main.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CoreGuard.InteractionCycleController>(true)).ToArray();
            Assert.That(cycles.Length, Is.EqualTo(1));
            Assert.That(session.InteractionCycle, Is.SameAs(authored));
        }

        // Break caught: scene configuration omits shared world bars or fails to apply the supplied red heart-bar art to both protected units.
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
            Assert.That(zone.WarningRing, Is.Not.Null);
            Assert.That(playerBar.transform.localPosition.y, Is.GreaterThan(0f));
            Assert.That(coreBar.transform.localPosition.y, Is.GreaterThan(0f));
            Assert.That(coreBar.transform.localPosition.y + .4f, Is.LessThan(zone.Radius));
            Assert.That(coreBar.Width, Is.GreaterThan(playerBar.Width));
            Assert.That(playerBar.FullColor, Is.EqualTo(Color.red));
            Assert.That(coreBar.FullColor, Is.EqualTo(Color.red));
            Assert.That(playerBar.UsesSpriteArt, Is.True);
            Assert.That(coreBar.UsesSpriteArt, Is.True);
            Assert.That(playerBar.BarSprite, Is.Not.Null);
            Assert.That(playerBar.FillSprite, Is.Not.Null);
            Assert.That(playerBar.IconSprite, Is.Not.Null);
            Assert.That(coreBar.BarSprite, Is.SameAs(playerBar.BarSprite));
            Assert.That(coreBar.FillSprite, Is.SameAs(playerBar.FillSprite));
            Assert.That(coreBar.IconSprite, Is.SameAs(playerBar.IconSprite));
        }

        [Test]
        public void ConfigureProject_CompactHudStaysInsideBothSupportedResolutions()
        {
            var main = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            InvokeConfigureProject();
            var hud = main.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CoreGuard.HudPresenter>(true)).Single();
            Assert.That(hud.transform.Find("Top bar")?.gameObject.activeSelf ?? false, Is.False);
            Assert.That(hud.transform.Find("Bottom bar")?.gameObject.activeSelf ?? false, Is.False);
            Assert.That(hud.StatsText.alignment, Is.EqualTo(TextAnchor.UpperLeft));
            Assert.That(hud.CoreText.alignment, Is.EqualTo(TextAnchor.UpperRight));
            Assert.That(hud.CooldownsText.alignment, Is.EqualTo(TextAnchor.LowerRight));
            Assert.That(hud.CompactHud, Is.True);
            var objective = hud.transform.Find("Objective");
            var controls = hud.transform.Find("Controls");
            var actions = hud.transform.Find("Actions");
            Assert.That(objective == null || !objective.gameObject.activeSelf, Is.True);
            Assert.That(controls == null || !controls.gameObject.activeSelf, Is.True);
            Assert.That(actions == null || !actions.gameObject.activeSelf, Is.True);
            Assert.That(hud.SettingsPanel, Is.Not.Null);
            Assert.That(hud.SettingsButton, Is.Not.Null);
            Assert.That(hud.CloseSettingsButton, Is.Not.Null);
            Assert.That(hud.LoadingPanel, Is.Not.Null);
            Assert.That(hud.LoadingProgress, Is.Not.Null);
            Assert.That(hud.SettingsPanel.activeSelf, Is.False);
            Assert.That(hud.LoadingPanel.activeSelf, Is.False);
            Assert.That(hud.EntryScreen, Is.Not.Null);
            Assert.That(hud.EntryScreen.activeSelf, Is.True);
            Assert.That(hud.StatsText.gameObject.activeSelf, Is.False);
            Assert.That(hud.CoreText.gameObject.activeSelf, Is.False);
            Assert.That(hud.TimerText.gameObject.activeSelf, Is.False);
            Assert.That(hud.WeaponText.gameObject.activeSelf, Is.False);
            Assert.That(hud.CooldownsText.gameObject.activeSelf, Is.False);
            Assert.That(hud.PlayerHealthPanel.activeSelf, Is.False);
            Assert.That(hud.BulletButton.gameObject.activeSelf, Is.False);
            Assert.That(hud.RocketButton.gameObject.activeSelf, Is.False);
            Assert.That(hud.MineButton.gameObject.activeSelf, Is.False);
            Assert.That(hud.ShieldButton.gameObject.activeSelf, Is.False);
            Assert.That(hud.EmpButton.gameObject.activeSelf, Is.False);
            Assert.That(hud.GameplayRoots, Is.Not.Null);
            Assert.That(hud.GameplayRoots.Length, Is.EqualTo(5));
            Assert.That(hud.PlayerHealthPanel, Is.Not.Null);
            Assert.That(hud.PlayerHealthFill, Is.Not.Null);
            Assert.That(hud.PlayerHealthValue, Is.Not.Null);
            Assert.That(hud.PlayerHealthFill.sprite, Is.Not.Null);
            Assert.That(hud.EntryScreen.transform.Find("Menu player barrel"), Is.Not.Null);
            Assert.That(hud.EntryScreen.transform.Find("Menu enemy barrel"), Is.Not.Null);
            var audioView = hud.GetComponent<CoreGuard.AudioToggleView>();
            Assert.That(audioView, Is.Not.Null);
            Assert.That(audioView.SoundOff, Is.Not.Null);
            Assert.That(audioView.SoundOn, Is.Not.Null);
            Assert.That(audioView.MusicOn, Is.Not.Null);
            Assert.That(audioView.MusicOff, Is.Not.Null);
            Assert.That(((RectTransform)audioView.SoundOff.transform).anchoredPosition,
                Is.EqualTo(((RectTransform)audioView.SoundOn.transform).anchoredPosition));
            Assert.That(((RectTransform)audioView.SoundOff.transform).sizeDelta,
                Is.EqualTo(((RectTransform)audioView.SoundOn.transform).sizeDelta));
            Assert.That(((RectTransform)audioView.MusicOn.transform).anchoredPosition,
                Is.EqualTo(((RectTransform)audioView.MusicOff.transform).anchoredPosition));
            Assert.That(((RectTransform)audioView.MusicOn.transform).sizeDelta,
                Is.EqualTo(((RectTransform)audioView.MusicOff.transform).sizeDelta));
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
            Assert.That(inputActions.FindAction("Gameplay/Shield", false).bindings[0].path,
                Is.EqualTo("<Keyboard>/digit0"));
            Assert.That(hud.ResultImage, Is.Not.Null);
            Assert.That(hud.WinSprite, Is.Not.Null);
            Assert.That(hud.LoseSprite, Is.Not.Null);
            Assert.That(hud.WinSprite.name, Is.EqualTo("YOU WIN"));
            Assert.That(hud.LoseSprite.name, Is.EqualTo("YOU LOSE"));
            Assert.That(hud.CountdownImage, Is.Not.Null);
            Assert.That(hud.CountdownZero, Is.Not.Null);
            Assert.That(hud.CountdownOne, Is.Not.Null);
            Assert.That(hud.CountdownTwo, Is.Not.Null);
            Assert.That(hud.CountdownThree, Is.Not.Null);

            var compactRects = new[] { hud.StatsText.rectTransform, hud.CoreText.rectTransform,
                hud.TimerText.rectTransform, hud.WeaponText.rectTransform, hud.CooldownsText.rectTransform,
                hud.FeedbackText.rectTransform };
            foreach (var resolution in new[] { new Vector2(1280, 720), new Vector2(1920, 1080) })
            foreach (var rect in compactRects)
            {
                var minimum = Vector2.Scale(rect.anchorMin, resolution) + rect.anchoredPosition - Vector2.Scale(rect.pivot, rect.sizeDelta);
                var maximum = minimum + rect.sizeDelta;
                Assert.That(minimum.x, Is.GreaterThanOrEqualTo(0), $"{rect.name} leaves {resolution} on the left.");
                Assert.That(minimum.y, Is.GreaterThanOrEqualTo(0), $"{rect.name} leaves {resolution} below.");
                Assert.That(maximum.x, Is.LessThanOrEqualTo(resolution.x), $"{rect.name} leaves {resolution} on the right.");
                Assert.That(maximum.y, Is.LessThanOrEqualTo(resolution.y), $"{rect.name} leaves {resolution} above.");
            }
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
