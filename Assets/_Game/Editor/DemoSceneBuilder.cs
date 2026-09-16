using System.IO;
using System.Linq;
using CoreGuard;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// All roots are explicitly placed in the supplied scene. Existing named objects
// and assigned gameplay references are retained on repeated configuration.
public static class DemoSceneBuilder
{
    private const string ActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";
    private const string SpritePath = "Assets/_Game/Art/ArenaWhite.asset";
    private const string EnemyPath = "Assets/_Game/Prefabs/Enemy.prefab";
    private const string ProjectilePath = "Assets/_Game/Prefabs/Projectile.prefab";
    private const string MinePath = "Assets/_Game/Prefabs/Mine.prefab";
    private const string PlayerBodyPath = "Assets/_Game/Art/Kenney/Tanks/tankBody_blue_outline.png";
    private const string PlayerBarrelPath = "Assets/_Game/Art/Kenney/Tanks/tankBlue_barrel1_outline.png";
    private const string EnemyBodyPath = "Assets/_Game/Art/Kenney/Tanks/tankBody_red_outline.png";
    private const string EnemyBarrelPath = "Assets/_Game/Art/Kenney/Tanks/tankRed_barrel1_outline.png";
    private const string GroundTilePath = "Assets/_Game/Art/Kenney/Tanks/tileSand1.png";
    private const string CoreBodyPath = "Assets/_Game/Art/Kenney/Tanks/tankBody_darkLarge_outline.png";
    private const string ProjectileSpritePath = "Assets/_Game/Art/Kenney/Tanks/bulletBlue1_outline.png";
    private const string RocketSpritePath = "Assets/_Game/Art/Provided/Combat/rocket_flying.png";
    private const string MineSpritePath = "Assets/_Game/Art/Kenney/Particles/scorch_01.png";
    private const string CoreGlowPath = "Assets/_Game/Art/Kenney/Particles/circle_04.png";
    private const string BulletFlashPath = "Assets/_Game/Art/Kenney/Particles/muzzle_02.png";
    private const string RocketFlashPath = "Assets/_Game/Art/Kenney/Particles/muzzle_01.png";
    private const string MineFlashPath = "Assets/_Game/Art/Kenney/Particles/spark_01.png";
    private const string ShieldFlashPath = "Assets/_Game/Art/Kenney/Particles/circle_04.png";
    private const string EmpFlashPath = "Assets/_Game/Art/Kenney/Particles/magic_01.png";
    private const string GateIconPath = "Assets/_Game/Art/Kenney/Icons/warning.png";
    private const string XIconPath = "Assets/_Game/Art/Kenney/Icons/target.png";
    private const string YIconPath = "Assets/_Game/Art/Kenney/Icons/power.png";
    private const string ZIconPath = "Assets/_Game/Art/Kenney/Icons/plus.png";
    private const string UiPanelPath = "Assets/_Game/Art/Kenney/UI/panel_glass.png";
    private const string UiButtonPath = "Assets/_Game/Art/Kenney/UI/button_rectangle_depth.png";
    private const string UiFontPath = "Assets/_Game/Art/Kenney/UI/Kenney Future.ttf";
    private const string BulletAudioPath = "Assets/_Game/Audio/Kenney/laserSmall_000.ogg";
    private const string RocketAudioPath = "Assets/_Game/Audio/Kenney/laserLarge_000.ogg";
    private const string MineAudioPath = "Assets/_Game/Audio/Kenney/explosionCrunch_000.ogg";
    private const string AlertAudioPath = "Assets/_Game/Audio/Kenney/computerNoise_000.ogg";
    private const string ShieldAudioPath = "Assets/_Game/Audio/Kenney/forceField_000.ogg";
    private const string EmpAudioPath = "Assets/_Game/Audio/Kenney/impactMetal_000.ogg";
    private const string MusicAudioPath = "Assets/_Game/Audio/Provided/music.mp3";
    private const string UiClickAudioPath = "Assets/_Game/Audio/Provided/click.ogg";
    private const string ExplosionAudioPath = "Assets/_Game/Audio/Provided/explosion.wav";
    private const string VictoryAudioPath = "Assets/_Game/Audio/Provided/congratulation.wav";
    private const string DefeatAudioPath = "Assets/_Game/Audio/Provided/gameover.wav";
    private const string WinSpritePath = "Assets/_Game/Art/Provided/Results/YOU WIN.png";
    private const string LoseSpritePath = "Assets/_Game/Art/Provided/Results/YOU LOSE.png";
    private const string StartSpritePath = "Assets/_Game/Art/Provided/Results/start1.png";
    private const string MenuBackgroundPath = "Assets/_Game/Art/Provided/Menu/background_new.jpg";
    private const string MenuLogoPath = "Assets/_Game/Art/Provided/Menu/logo_new.png";
    private const string CountdownZeroPath = "Assets/_Game/Art/Provided/Countdown/zero.png";
    private const string CountdownOnePath = "Assets/_Game/Art/Provided/Countdown/one.png";
    private const string CountdownTwoPath = "Assets/_Game/Art/Provided/Countdown/two.png";
    private const string CountdownThreePath = "Assets/_Game/Art/Provided/Countdown/three.png";
    private const string SettingsIconPath = "Assets/_Game/Art/Provided/Settings/setting.png";
    private const string HealthBarReferencePath = "Assets/_Game/Art/Provided/Combat/health_bar_reference.jpg";
    private const string HealthBarTrackPath = "Assets/_Game/Art/Provided/Combat/health_bar_track.png";
    private const string HealthBarFillPath = "Assets/_Game/Art/Provided/Combat/health_bar_fill.png";
    private const string HealthBarIconPath = "Assets/_Game/Art/Provided/Combat/health_bar_icon.png";
    private static readonly string[] ExplosionFramePaths =
    {
        "Assets/_Game/Art/Provided/Explosion/explosion_01.png",
        "Assets/_Game/Art/Provided/Explosion/explosion_02.png",
        "Assets/_Game/Art/Provided/Explosion/explosion_03.png",
        "Assets/_Game/Art/Provided/Explosion/explosion_04.png",
        "Assets/_Game/Art/Provided/Explosion/explosion_05.png",
        "Assets/_Game/Art/Provided/Explosion/explosion_06.png",
        "Assets/_Game/Art/Provided/Explosion/explosion_07.png",
        "Assets/_Game/Art/Provided/Explosion/explosion_08.png",
        "Assets/_Game/Art/Provided/Explosion/explosion_09.png",
    };
    private static readonly Color Cyan = new Color(.25f, .9f, .85f);
    private static readonly Color Ink = new Color(.055f, .085f, .12f, .97f);

    [InitializeOnLoadMethod]
    private static void EnsureProvidedHealthBarArtOnEditorLoad()
    {
        EnsureHealthBarArt();
    }

    public static void Configure(Scene scene)
    {
        EnsureLayers();
        EnsureHealthBarArt();
        var sprite = EnsureSprite();
        var actions = EnsureActions();
        var camera = Find<Camera>(scene);
        var canvas = Find<Canvas>(scene);
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;

        var world = Root(scene, "Arena");
        Visual(world.transform, "Floor", sprite, Vector2.zero, new Vector2(16, 8.4f), new Color(.045f, .08f, .12f), -10);
        BuildGroundTiles(world.transform, ImportedSprite(GroundTilePath, null));
        Border(world.transform, "North wall", sprite, new Vector2(0, 4.1f), new Vector2(16, .2f));
        Border(world.transform, "South wall", sprite, new Vector2(0, -4.1f), new Vector2(16, .2f));
        Border(world.transform, "East wall", sprite, new Vector2(7.9f, 0), new Vector2(.2f, 8));
        Border(world.transform, "West wall", sprite, new Vector2(-7.9f, 0), new Vector2(.2f, 8));

        var coreObject = Root(scene, "Core");
        coreObject.layer = LayerMask.NameToLayer("Core");
        var core = GetOrAdd<CoreHealth>(coreObject);
        core.GetComponent<CircleCollider2D>().radius = .75f;
        core.GetComponent<CircleCollider2D>().isTrigger = true;
        var coreGlow = ImportedSprite(CoreGlowPath, sprite);
        var coreBodySprite = ImportedSprite(CoreBodyPath, sprite);
        Visual(coreObject.transform, "Core glow", coreGlow, Vector2.zero, new Vector2(1.7f, 1.7f), new Color(.08f, .3f, .32f), 0);
        ApplySprite(coreObject.transform, "Core glow", coreGlow);
        Visual(coreObject.transform, "Core body", coreBodySprite, Vector2.zero, new Vector2(1.45f, 1.45f), Cyan, 1);
        ApplySprite(coreObject.transform, "Core body", coreBodySprite);
        Visual(coreObject.transform, "Core center", sprite, Vector2.zero, new Vector2(.55f, .55f), Ink, 2);
        var coreHealthBar = EnsureWorldHealthBar(coreObject.transform, core.HealthBar, "Core health bar", new Vector3(0f, 1.15f, 0f), 2.2f, .14f, Color.red);
        if (!core.HealthBar) core.HealthBar = coreHealthBar;
        core.RefreshHealthBar();

        var playerObject = Root(scene, "Player");
        playerObject.layer = LayerMask.NameToLayer("Player");
        var stats = GetOrAdd<PlayerStats>(playerObject);
        var newMotor = !playerObject.GetComponent<PlayerMotor>();
        var motor = GetOrAdd<PlayerMotor>(playerObject);
        var playerBody = playerObject.GetComponent<Rigidbody2D>();
        playerBody.gravityScale = 0; playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        GetOrAdd<CircleCollider2D>(playerObject).radius = .6f;
        if (newMotor) playerObject.transform.position = motor.SpawnPosition;
        var playerBodySprite = ImportedSprite(PlayerBodyPath, sprite);
        var playerBarrelSprite = ImportedSprite(PlayerBarrelPath, sprite);
        Visual(playerObject.transform, "Player body", playerBodySprite, Vector2.zero, new Vector2(1.6f, 1.6f), new Color(.3f, .65f, 1), 3);
        ApplySprite(playerObject.transform, "Player body", playerBodySprite);
        var playerHealthBar = EnsureWorldHealthBar(playerObject.transform, stats.HealthBar, "Player health bar", new Vector3(0f, .95f, 0f), 1.25f, .12f, Color.red);
        if (!stats.HealthBar) stats.HealthBar = playerHealthBar;
        stats.RefreshHealthBar();
        var turret = Child(playerObject.transform, "Turret");
        var barrel = Visual(turret.transform, "Barrel", playerBarrelSprite, new Vector2(.68f, 0), new Vector2(1.3f, 1.9f), Color.white, 4);
        ApplySprite(turret.transform, "Barrel", playerBarrelSprite);
        // The Kenney barrel points up by default. Rotate it clockwise so the
        // turret's local +X axis is the same direction as the projectile.
        barrel.transform.localRotation = Quaternion.Euler(0, 0, -90f);
        if (!motor.Turret) motor.Turret = turret.transform;
        var muzzle = Child(turret.transform, "Muzzle");
        muzzle.transform.localPosition = new Vector3(.98f, 0, 0);
        muzzle.transform.localRotation = Quaternion.identity;

        var projectilePrefab = EnsureProjectilePrefab(scene, sprite);
        var minePrefab = EnsureMinePrefab(scene, sprite);
        var session = GetOrAdd<GameSession>(Root(scene, "Game Session"));
        var weapon = GetOrAdd<WeaponController>(playerObject);
        var defense = GetOrAdd<DefenseController>(playerObject);
        var effects = GetOrAdd<StatusEffects>(playerObject);
        var vfx = GetOrAdd<CombatVfxPresenter>(playerObject);
        var audio = GetOrAdd<AudioService>(session.gameObject);
        var demo = GetOrAdd<DemoDirector>(session.gameObject);
        if (!weapon.Session) weapon.Session = session;
        // Keep the first weapon readable: two 25-damage bullets leave a 10 HP
        // creep alive, while one 55-damage rocket is a heavy but non-lethal hit.
        weapon.BulletDamage = 25f;
        weapon.RocketDamage = 55f;
        weapon.RocketSpeed = 11f;
        weapon.RocketCooldown = 1f;
        if (!defense.Session) defense.Session = session;
        if (!defense.Player) defense.Player = stats;
        if (!vfx.Weapon) vfx.Weapon = weapon;
        if (!vfx.Defense) vfx.Defense = defense;
        if (!vfx.Muzzle) vfx.Muzzle = muzzle.transform;
        vfx.BulletFlash = ImportedSprite(BulletFlashPath, null);
        vfx.RocketFlash = ImportedSprite(RocketFlashPath, null);
        vfx.MineFlash = ImportedSprite(MineFlashPath, null);
        vfx.ShieldFlash = ImportedSprite(ShieldFlashPath, null);
        vfx.EmpFlash = ImportedSprite(EmpFlashPath, null);
        vfx.ExplosionFrames = ExplosionFramePaths.Select(path => ImportedSprite(path, null)).ToArray();
        if (!effects.Session) effects.Session = session;
        if (!audio.Session) audio.Session = session;
        if (!audio.Weapon) audio.Weapon = weapon;
        if (!audio.Defense) audio.Defense = defense;
        if (!demo.Session) demo.Session = session;
        audio.BulletFire = PreferImportedClip(BulletAudioPath, audio.BulletFire);
        audio.RocketLaunch = PreferImportedClip(RocketAudioPath, audio.RocketLaunch);
        audio.MineDrop = PreferImportedClip(MineAudioPath, audio.MineDrop);
        audio.AlertBeep = PreferImportedClip(AlertAudioPath, audio.AlertBeep);
        audio.ShieldActivate = PreferImportedClip(ShieldAudioPath, audio.ShieldActivate);
        audio.EmpActivate = PreferImportedClip(EmpAudioPath, audio.EmpActivate);
        audio.MusicLoop = PreferImportedClip(MusicAudioPath, audio.MusicLoop);
        audio.UiClick = PreferImportedClip(UiClickAudioPath, audio.UiClick);
        audio.Explosion = PreferImportedClip(ExplosionAudioPath, audio.Explosion);
        audio.Victory = PreferImportedClip(VictoryAudioPath, audio.Victory);
        audio.Defeat = PreferImportedClip(DefeatAudioPath, audio.Defeat);
        audio.StartMuted = true;
        audio.EnsureSourcesForScene();
        if (!weapon.Muzzle) weapon.Muzzle = muzzle.transform;
        if (!weapon.ProjectilePrefab) weapon.ProjectilePrefab = projectilePrefab;
        if (!weapon.MinePrefab) weapon.MinePrefab = minePrefab;

        var spawner = GetOrAdd<EnemySpawner>(Root(scene, "Enemy Spawner"));
        var configuredEnemyPrefab = EnsureEnemyPrefab(scene, sprite);
        if (!spawner.Prefab || spawner.Prefab.name == "Enemy") spawner.Prefab = configuredEnemyPrefab;
        var positions = new[]
        {
            new Vector2(7.2f, 0f),
            new Vector2(0f, 3.6f),
            new Vector2(-7.2f, 0f),
            new Vector2(0f, -3.6f),
        };
        var retainedGates = spawner.Gates;
        var configuredGates = new Transform[positions.Length];
        for (var i = 0; i < configuredGates.Length; i++)
        {
            if (retainedGates != null && i < retainedGates.Length && retainedGates[i])
            {
                configuredGates[i] = retainedGates[i];
                continue;
            }
            var gate = Child(world.transform, "Gate " + (i + 1));
            gate.transform.localPosition = positions[i];
            var gateSprite = ImportedSprite(GateIconPath, sprite);
            Visual(gate.transform, "Gate marker", gateSprite, Vector2.zero, new Vector2(.25f, .25f), new Color(1, .47f, .35f), 1);
            ApplySprite(gate.transform, "Gate marker", gateSprite);
            configuredGates[i] = gate.transform;
        }
        spawner.Gates = configuredGates;
        if (!session.Player) session.Player = stats;
        if (!session.Core) session.Core = core;
        if (!session.Motor) session.Motor = motor;
        if (!session.Weapon) session.Weapon = weapon;
        if (!session.Defense) session.Defense = defense;
        if (!session.Effects) session.Effects = effects;
        if (!session.Audio) session.Audio = audio;
        if (!session.Demo) session.Demo = demo;
        if (!session.EnemyProjectilePrefab) session.EnemyProjectilePrefab = projectilePrefab;
        if (!session.Spawner) session.Spawner = spawner;
        audio.Bind();
        demo.Bind();
        vfx.Bind();
        if (!motor.Session) motor.Session = session;
        if (!spawner.Session) spawner.Session = session;
        if (!spawner.Core) spawner.Core = core;
        EnsureInteractions(world.transform, session, stats, sprite);
        EnsureInteractionCycle(scene, world.transform, session, stats, core);
        EnsureForbiddenZone(coreObject.transform, session, audio, sprite);
        var input = GetOrAdd<InputReader>(session.gameObject);
        if (!input.Actions) input.Actions = actions;
        if (!input.Session) input.Session = session;
        if (!input.WorldCamera) input.WorldCamera = camera;
        if (!session.Input) session.Input = input;
        var aimCursorRoot = Root(scene, "Aim Cursor");
        var aimCursorRenderer = GetOrAdd<SpriteRenderer>(aimCursorRoot);
        aimCursorRenderer.sprite = ImportedSprite(XIconPath, null);
        aimCursorRenderer.color = new Color(.25f, .9f, .85f, .9f);
        aimCursorRenderer.sortingOrder = 7;
        aimCursorRoot.transform.localScale = Vector3.one * .22f;
        aimCursorRenderer.enabled = false;
        var aimCursor = GetOrAdd<AimCursorPresenter>(aimCursorRoot);
        aimCursor.Session = session;
        aimCursor.Input = input;
        aimCursor.Renderer = aimCursorRenderer;
        var ui = Find<InputSystemUIInputModule>(scene);
        ui.actionsAsset = actions;
        ui.point = ActionReference("UI/Point"); ui.leftClick = ActionReference("UI/Click");
        ui.rightClick = ActionReference("UI/RightClick"); ui.middleClick = ActionReference("UI/MiddleClick");
        ui.scrollWheel = ActionReference("UI/ScrollWheel"); ui.move = ActionReference("UI/Navigate");
        ui.submit = ActionReference("UI/Submit"); ui.cancel = ActionReference("UI/Cancel");
        BuildHud(canvas.transform, session, world, coreObject, playerObject, spawner.gameObject, aimCursorRoot);
    }

    private static void BuildHud(Transform canvas, GameSession session, params GameObject[] gameplayRoots)
    {
        var existingRoot = canvas.Find("Core Guard HUD");
        var root = Child(canvas, "Core Guard HUD", typeof(RectTransform));
        var rect = (RectTransform)root.transform;
        if (!existingRoot)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        var hud = GetOrAdd<HudPresenter>(root); if (!hud.Session) hud.Session = session;
        hud.CompactHud = true;
        hud.GameplayRoots = gameplayRoots;
        var audioView = GetOrAdd<AudioToggleView>(root);
        if (!audioView.Audio) audioView.Audio = session.Audio;
        // Keep the arena unobstructed. Older scene versions may already contain
        // these large surfaces, so explicitly disable them during reconfigure.
        DisableHudSurface(root.transform, "Top bar");
        DisableHudSurface(root.transform, "Bottom bar");
        var entryScreen = FullScreenImage(root.transform, "Entry screen", ImportedSprite(MenuBackgroundPath, null));
        entryScreen.transform.SetAsFirstSibling();
        hud.EntryScreen = entryScreen;
        SpriteImage(entryScreen.transform, "Menu logo", ImportedSprite(MenuLogoPath, null), new Vector2(0, 210), new Vector2(900, 270));
        SpriteImage(entryScreen.transform, "Menu player", ImportedSprite(PlayerBodyPath, null), new Vector2(-318, 55), new Vector2(158, 158));
        var menuPlayerBarrel = SpriteImage(entryScreen.transform, "Menu player barrel", ImportedSprite(PlayerBarrelPath, null), new Vector2(-318, 55), new Vector2(62, 94));
        menuPlayerBarrel.rectTransform.localRotation = Quaternion.Euler(0, 0, -90f);
        SpriteImage(entryScreen.transform, "Menu enemy", ImportedSprite(EnemyBodyPath, null), new Vector2(318, 55), new Vector2(158, 158));
        var menuEnemyBarrel = SpriteImage(entryScreen.transform, "Menu enemy barrel", ImportedSprite(EnemyBarrelPath, null), new Vector2(318, 55), new Vector2(62, 94));
        menuEnemyBarrel.rectTransform.localRotation = Quaternion.Euler(0, 0, 90f);
        Label(entryScreen.transform, "Menu subtitle", "CORE GUARD  //  DEFEND THE FRONTIER", new Vector2(0, 90), new Vector2(600, 28), 14, new Color(.72f, .88f, .95f));
        MoveChild(root.transform, entryScreen.transform, "Settings panel");
        MoveChild(root.transform, entryScreen.transform, "Start panel");
        var settingsPanel = Panel(entryScreen.transform, "Settings panel", false);
        settingsPanel.SetActive(false);
        Layout((RectTransform)settingsPanel.transform, Vector2.zero, new Vector2(420, 270));
        Label(settingsPanel.transform, "Heading", "SETTINGS", new Vector2(0, 92), new Vector2(360, 42), 28, Cyan);
        Label(settingsPanel.transform, "Audio hint", "AUDIO", new Vector2(0, 52), new Vector2(220, 24), 13, new Color(.67f, .8f, .84f));
        MoveChild(root.transform, settingsPanel.transform, "SoundOff");
        MoveChild(root.transform, settingsPanel.transform, "SoundOn");
        MoveChild(root.transform, settingsPanel.transform, "MusicOn");
        MoveChild(root.transform, settingsPanel.transform, "MusicOff");
        var soundOff = AudioButton(settingsPanel.transform, "SoundOff", "SFX", new Vector2(-72, 8));
        var soundOn = AudioButton(settingsPanel.transform, "SoundOn", "SFX", new Vector2(-72, 8));
        var musicOn = AudioButton(settingsPanel.transform, "MusicOn", "BGM", new Vector2(72, 8));
        var musicOff = AudioButton(settingsPanel.transform, "MusicOff", "BGM", new Vector2(72, 8));
        if (!audioView.SoundOff) audioView.SoundOff = soundOff;
        if (!audioView.SoundOn) audioView.SoundOn = soundOn;
        if (!audioView.MusicOn) audioView.MusicOn = musicOn;
        if (!audioView.MusicOff) audioView.MusicOff = musicOff;
        hud.SettingsPanel = settingsPanel;
        var title = Label(root.transform, "Title", "CORE GUARD", Vector2.zero, new Vector2(240, 38), 27, Cyan);
        Anchor(title.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -14), new Vector2(240, 38));
        title.gameObject.SetActive(false);
        var statsText = Label(root.transform, "Stats", "HP 100/100  •  ARM 50/50  •  C 0", Vector2.zero, new Vector2(390, 30), 15, Color.white);
        Anchor(statsText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -22), new Vector2(390, 30));
        statsText.alignment = TextAnchor.UpperLeft;
        if (!hud.StatsText) hud.StatsText = statsText;
        var healthPanel = Child(root.transform, "Player health display", typeof(RectTransform));
        Anchor((RectTransform)healthPanel.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -64), new Vector2(285, 50));
        var healthTrackSprite = ImportedSprite(HealthBarTrackPath, null);
        var healthTrack = SpriteImage(healthPanel.transform, "Health track", healthTrackSprite, new Vector2(29, 0), new Vector2(212, 40));
        healthTrack.preserveAspect = false;
        healthTrack.color = new Color(.24f, .035f, .045f, .95f);
        healthTrack.raycastTarget = false;
        var healthFill = SpriteImage(healthPanel.transform, "Health fill",
            ImportedSprite(HealthBarFillPath, null) ?? healthTrackSprite,
            new Vector2(29, 0), new Vector2(212, 40));
        healthFill.preserveAspect = false;
        healthFill.color = Color.white;
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = 0;
        healthFill.fillAmount = 1f;
        healthFill.raycastTarget = false;
        SpriteImage(healthPanel.transform, "Health icon", ImportedSprite(HealthBarIconPath, null), new Vector2(-111, 0), new Vector2(50, 50));
        var healthValue = Label(healthPanel.transform, "Health value", "100/100", new Vector2(29, 0), new Vector2(150, 25), 15, Color.white);
        healthValue.raycastTarget = false;
        if (!hud.PlayerHealthPanel) hud.PlayerHealthPanel = healthPanel;
        if (!hud.PlayerHealthFill) hud.PlayerHealthFill = healthFill;
        if (!hud.PlayerHealthValue) hud.PlayerHealthValue = healthValue;
        var coreText = Label(root.transform, "Core health", "CORE 100/100", Vector2.zero, new Vector2(190, 28), 18, Cyan);
        Anchor(coreText.rectTransform, Vector2.one, Vector2.one, new Vector2(-24, -22), new Vector2(190, 28));
        coreText.alignment = TextAnchor.UpperRight;
        if (!hud.CoreText) hud.CoreText = coreText;
        var timerText = Label(root.transform, "Timer", "90.0s", Vector2.zero, new Vector2(120, 26), 18, Color.white);
        Anchor(timerText.rectTransform, Vector2.one, Vector2.one, new Vector2(-24, -52), new Vector2(120, 26));
        timerText.alignment = TextAnchor.UpperRight;
        if (!hud.TimerText) hud.TimerText = timerText;
        var stateText = Label(root.transform, "State", string.Empty, Vector2.zero, new Vector2(1, 1), 1, Color.clear);
        Anchor(stateText.rectTransform, Vector2.one, Vector2.one, new Vector2(-24, -80), new Vector2(1, 1));
        stateText.alignment = TextAnchor.UpperRight;
        stateText.gameObject.SetActive(false);
        if (!hud.StateText) hud.StateText = stateText;
        hud.WinSprite = ImportedSprite(WinSpritePath, hud.WinSprite);
        hud.LoseSprite = ImportedSprite(LoseSpritePath, hud.LoseSprite);
        hud.CountdownZero = ImportedSprite(CountdownZeroPath, hud.CountdownZero);
        hud.CountdownOne = ImportedSprite(CountdownOnePath, hud.CountdownOne);
        hud.CountdownTwo = ImportedSprite(CountdownTwoPath, hud.CountdownTwo);
        hud.CountdownThree = ImportedSprite(CountdownThreePath, hud.CountdownThree);
        var countdownImage = SpriteImage(root.transform, "Start countdown", hud.CountdownThree, Vector2.zero, new Vector2(128, 128));
        if (!hud.CountdownImage) hud.CountdownImage = countdownImage;
        hud.CountdownImage.gameObject.SetActive(false);
        DisableHudSurface(root.transform, "Objective");
        DisableHudSurface(root.transform, "Controls");
        DisableHudSurface(root.transform, "Actions");
        var weaponText = Label(root.transform, "Weapon", "BULLET", Vector2.zero, new Vector2(160, 26), 15, Color.white);
        Anchor(weaponText.rectTransform, Vector2.zero, Vector2.zero, new Vector2(24, 72), new Vector2(160, 26));
        weaponText.alignment = TextAnchor.LowerLeft;
        if (!hud.WeaponText) hud.WeaponText = weaponText;
        var cooldownsText = Label(root.transform, "Cooldowns", "SH READY  •  EMP READY", new Vector2(390, -245), new Vector2(300, 26), 14, new Color(.67f, .8f, .84f));
        if (!hud.CooldownsText) hud.CooldownsText = cooldownsText;
        Anchor(hud.CooldownsText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 72), new Vector2(300, 26));
        hud.CooldownsText.alignment = TextAnchor.LowerRight;
        var feedbackText = Label(root.transform, "Feedback", string.Empty, new Vector2(0, -245), new Vector2(380, 28), 15, new Color(1f, .65f, .25f));
        feedbackText.alignment = TextAnchor.MiddleCenter;
        feedbackText.raycastTarget = false;
        if (!hud.FeedbackText) hud.FeedbackText = feedbackText;
        hud.FeedbackText.gameObject.SetActive(false);
        var bulletButton = ActionButton(root.transform, "Bullet button", "1  BULLET", new Vector2(-315, -271));
        var rocketButton = ActionButton(root.transform, "Rocket button", "2  ROCKET", new Vector2(-185, -271));
        var mineButton = ActionButton(root.transform, "Mine button", "3  MINE", new Vector2(-55, -271));
        var shieldButton = ActionButton(root.transform, "Shield button", "0  SHIELD", new Vector2(105, -271));
        var empButton = ActionButton(root.transform, "EMP button", "E  EMP", new Vector2(235, -271));
        if (!hud.BulletButton) hud.BulletButton = bulletButton;
        if (!hud.RocketButton) hud.RocketButton = rocketButton;
        if (!hud.MineButton) hud.MineButton = mineButton;
        if (!hud.ShieldButton) hud.ShieldButton = shieldButton;
        if (!hud.EmpButton) hud.EmpButton = empButton;
        if (!hud.StartPanel)
        {
            hud.StartPanel = Panel(entryScreen.transform, "Start panel", true);
        }
        else if (hud.StartPanel.transform.parent != entryScreen.transform)
        {
            hud.StartPanel.transform.SetParent(entryScreen.transform, false);
        }
        Layout((RectTransform)hud.StartPanel.transform, new Vector2(0, -150), new Vector2(560, 245));
        Label(hud.StartPanel.transform, "Heading", "DEFEND THE CORE", new Vector2(0, 82), new Vector2(490, 50), 30, Cyan);
        Label(hud.StartPanel.transform, "Brief", "WASD move  •  Mouse aim  •  LMB fire\n1–3 weapons  •  0 shield  •  E EMP", new Vector2(0, 0), new Vector2(490, 90), 17, Color.white);
        if (!hud.StartButton) hud.StartButton = Button(hud.StartPanel.transform, "Start button", "START DEFENSE", new Vector2(0, -103));
        ApplyButtonArtwork(hud.StartButton, ImportedSprite(StartSpritePath, null));
        MoveChild(hud.StartPanel.transform, entryScreen.transform, "Settings");
        var settingsButton = AudioButton(entryScreen.transform, "Settings", "SETTINGS", Vector2.zero);
        Anchor((RectTransform)settingsButton.transform, Vector2.one, Vector2.one, new Vector2(-42, -42), new Vector2(72, 72));
        hud.SettingsButton = settingsButton;
        var closeSettingsButton = Button(settingsPanel.transform, "Close settings button", "BACK", new Vector2(0, -92));
        hud.CloseSettingsButton = closeSettingsButton;
        if (!hud.PausePanel)
        {
            hud.PausePanel = Panel(root.transform, "Pause panel", false);
            Label(hud.PausePanel.transform, "Heading", "PAUSED", new Vector2(0, 55), new Vector2(490, 55), 34, Cyan);
            Label(hud.PausePanel.transform, "Hint", "ESC to resume", new Vector2(0, -5), new Vector2(490, 40), 20, Color.white);
        }
        if (!hud.ResumeButton) hud.ResumeButton = Button(hud.PausePanel.transform, "Resume button", "CONTINUE", new Vector2(0, -103));
        if (!hud.ResultPanel)
        {
            hud.ResultPanel = Panel(root.transform, "Result panel", false);
            Label(hud.ResultPanel.transform, "Hint", "R or TRY AGAIN", new Vector2(0, -5), new Vector2(490, 40), 18, Color.white);
        }
        if (!hud.ResultText) hud.ResultText = Label(hud.ResultPanel.transform, "Heading", "CORE OFFLINE — LOST", new Vector2(0, 55), new Vector2(510, 55), 30, Cyan);
        var resultImage = SpriteImage(hud.ResultPanel.transform, "Result artwork", hud.LoseSprite, new Vector2(0, 116), new Vector2(390, 74));
        if (!hud.ResultImage) hud.ResultImage = resultImage;
        hud.ResultImage.sprite = hud.LoseSprite;
        if (!hud.RetryButton) hud.RetryButton = Button(hud.ResultPanel.transform, "Retry button", "TRY AGAIN", new Vector2(0, -103));
        var loadingPanel = Panel(root.transform, "Loading panel", false);
        loadingPanel.SetActive(false);
        Layout((RectTransform)loadingPanel.transform, Vector2.zero, new Vector2(520, 250));
        Label(loadingPanel.transform, "Heading", "LOADING", new Vector2(0, 62), new Vector2(440, 42), 28, Cyan);
        Label(loadingPanel.transform, "Loading label", "LOADING 0%", new Vector2(0, 18), new Vector2(360, 28), 15, Color.white);
        var loadingTrack = Child(loadingPanel.transform, "Loading track", typeof(RectTransform), typeof(Image));
        Layout((RectTransform)loadingTrack.transform, new Vector2(0, -28), new Vector2(320, 16));
        var trackImage = loadingTrack.GetComponent<Image>();
        trackImage.color = new Color(.08f, .18f, .21f, 1f);
        trackImage.raycastTarget = false;
        var loadingProgress = Child(loadingTrack.transform, "Loading progress", typeof(RectTransform), typeof(Image));
        Layout((RectTransform)loadingProgress.transform, Vector2.zero, new Vector2(320, 16));
        var progressImage = loadingProgress.GetComponent<Image>();
        progressImage.color = Cyan;
        progressImage.type = Image.Type.Filled;
        progressImage.fillMethod = Image.FillMethod.Horizontal;
        progressImage.fillOrigin = 0;
        progressImage.fillAmount = 0f;
        progressImage.raycastTarget = false;
        hud.LoadingPanel = loadingPanel;
        hud.LoadingProgress = progressImage;
        BuildDemoPanel(root.transform, session.Demo);
        HideGameplayHud(hud);
    }

    private static void HideGameplayHud(HudPresenter hud)
    {
        if (!hud) return;
        if (hud.StatsText) hud.StatsText.gameObject.SetActive(false);
        if (hud.CoreText) hud.CoreText.gameObject.SetActive(false);
        if (hud.TimerText) hud.TimerText.gameObject.SetActive(false);
        if (hud.WeaponText) hud.WeaponText.gameObject.SetActive(false);
        if (hud.CooldownsText) hud.CooldownsText.gameObject.SetActive(false);
        if (hud.FeedbackText) hud.FeedbackText.gameObject.SetActive(false);
        if (hud.PlayerHealthPanel) hud.PlayerHealthPanel.SetActive(false);
        if (hud.BulletButton) hud.BulletButton.gameObject.SetActive(false);
        if (hud.RocketButton) hud.RocketButton.gameObject.SetActive(false);
        if (hud.MineButton) hud.MineButton.gameObject.SetActive(false);
        if (hud.ShieldButton) hud.ShieldButton.gameObject.SetActive(false);
        if (hud.EmpButton) hud.EmpButton.gameObject.SetActive(false);
    }

    private static void BuildDemoPanel(Transform parent, DemoDirector demo)
    {
        if (!demo) return;
        var createdPanel = !demo.Panel;
        if (createdPanel)
        {
            demo.Panel = Panel(parent, "Demo panel", false);
            Layout((RectTransform)demo.Panel.transform, Vector2.zero, new Vector2(560, 380));
        }
        if (!demo.ResetButton)
        {
            Label(demo.Panel.transform, "Heading", "DEMO MODE  /  F1 TO CLOSE", new Vector2(0, 135), new Vector2(500, 42), 25, Cyan);
            demo.ResetButton = Button(demo.Panel.transform, "Reset scenario", "RESET SCENARIO", new Vector2(0, 83));
            demo.ZoneEnemyButton = Button(demo.Panel.transform, "Spawn zone enemy", "SPAWN ZONE ENEMY", new Vector2(0, 26));
            demo.ShooterButton = Button(demo.Panel.transform, "Spawn shooter", "SPAWN SHOOTER", new Vector2(0, -31));
            demo.ClusterButton = Button(demo.Panel.transform, "Spawn enemy cluster", "SPAWN ENEMY CLUSTER", new Vector2(0, -88));
            demo.RestoreInteractionsButton = Button(demo.Panel.transform, "Restore X Y Z", "RESTORE X / Y / Z", new Vector2(0, -145));
        }
    }

    private static void EnsureInteractions(Transform parent, GameSession session, PlayerStats player, Sprite sprite)
    {
        CreateInteraction(parent, "X", InteractionKind.X, new Vector2(-5, 2), new Color(1, .25f, .25f), "X  -20 HP / -10 ARMOR", session, player, sprite);
        CreateInteraction(parent, "Y", InteractionKind.Y, new Vector2(3, -2), new Color(.7f, .3f, 1), "Y  SLOW / BREAK SHIELD", session, player, sprite);
        CreateInteraction(parent, "Z", InteractionKind.Z, new Vector2(5, 2), new Color(1, .75f, .15f), "Z  +10 COINS / BOOST", session, player, sprite);
    }

    private static void EnsureInteractionCycle(Scene scene, Transform parent, GameSession session, PlayerStats player, CoreHealth core)
    {
        var allCycles = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<InteractionCycleController>(true))
            .ToArray();
        var cycle = session.InteractionCycle;
        if (cycle && (cycle.gameObject.scene != scene || (cycle.Session && cycle.Session != session))) cycle = null;
        if (!cycle) cycle = session.GetComponent<InteractionCycleController>();
        if (!cycle) cycle = allCycles.FirstOrDefault(candidate => candidate.Session == session);
        if (!cycle) cycle = GetOrAdd<InteractionCycleController>(session.gameObject);

        foreach (var duplicate in allCycles)
            if (duplicate && duplicate != cycle) UnityEngine.Object.DestroyImmediate(duplicate);

        var x = parent.Find("X") ? parent.Find("X").GetComponent<InteractionObject>() : null;
        var y = parent.Find("Y") ? parent.Find("Y").GetComponent<InteractionObject>() : null;
        var z = parent.Find("Z") ? parent.Find("Z").GetComponent<InteractionObject>() : null;
        cycle.BlockedLayers = LayerMask.GetMask("Arena");
        cycle.Configure(session, player, core, x, y, z);
        session.InteractionCycle = cycle;
    }

    private static void CreateInteraction(
        Transform parent,
        string name,
        InteractionKind kind,
        Vector2 position,
        Color color,
        string label,
        GameSession session,
        PlayerStats player,
        Sprite sprite)
    {
        var objectRoot = Child(parent, name);
        objectRoot.transform.localPosition = position;
        var interaction = GetOrAdd<InteractionObject>(objectRoot);
        interaction.Kind = kind;
        if (!interaction.Session) interaction.Session = session;
        if (!interaction.Player) interaction.Player = player;
        var collider = GetOrAdd<CircleCollider2D>(objectRoot);
        collider.isTrigger = true; collider.radius = .45f;
        var markerPath = kind == InteractionKind.X ? XIconPath : kind == InteractionKind.Y ? YIconPath : ZIconPath;
        var markerSprite = ImportedSprite(markerPath, sprite);
        Visual(objectRoot.transform, "Marker", markerSprite, Vector2.zero, new Vector2(.8f, .8f), color, 2);
        ApplySprite(objectRoot.transform, "Marker", markerSprite);
        var textObject = Child(objectRoot.transform, "Label", typeof(TextMesh));
        var text = textObject.GetComponent<TextMesh>();
        text.text = label; text.characterSize = .08f; text.fontSize = 32; text.anchor = TextAnchor.MiddleCenter; text.color = Color.white;
        textObject.transform.localPosition = new Vector3(0, -.7f, 0);
    }

    private static void EnsureForbiddenZone(Transform core, GameSession session, AudioService audio, Sprite sprite)
    {
        var zoneRoot = Child(core, "Forbidden Zone");
        var zone = GetOrAdd<ForbiddenZone>(zoneRoot);
        zone.Session = session;
        zone.Audio = audio;
        zone.Radius = 3f;
        var collider = GetOrAdd<CircleCollider2D>(zoneRoot);
        collider.isTrigger = true;
        collider.radius = zone.Radius;

        var ringObject = Child(zoneRoot.transform, "Zone ring", typeof(LineRenderer));
        var ring = GetOrAdd<LineRenderer>(ringObject);
        ring.loop = true;
        ring.useWorldSpace = false;
        ring.positionCount = 64;
        ring.widthMultiplier = .045f;
        ring.startColor = new Color(1, .18f, .22f, .72f);
        ring.endColor = ring.startColor;
        ring.sortingOrder = -1;
        zone.WarningRing = ring;
        for (var i = 0; i < ring.positionCount; i++)
        {
            var angle = i * Mathf.PI * 2f / ring.positionCount;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * zone.Radius, Mathf.Sin(angle) * zone.Radius, 0));
        }
    }
    private static WorldHealthBar EnsureWorldHealthBar(Transform parent, WorldHealthBar authoredBar, string name, Vector3 position, float width, float height, Color fullColor)
    {
        var bar = authoredBar;
        if (!bar)
        {
            var barObject = Child(parent, name);
            bar = GetOrAdd<WorldHealthBar>(barObject);
        }
        bar.transform.localPosition = position;
        bar.Width = width;
        bar.BarHeight = height;
        bar.FullColor = fullColor;
        bar.BarSprite = ImportedSprite(HealthBarTrackPath, null);
        // A first run can happen before Unity finishes importing the derived
        // fill texture. Falling back to the supplied bar keeps the scene
        // visible; the next configure pass will use the solid red fill asset.
        bar.FillSprite = ImportedSprite(HealthBarFillPath, null) ?? bar.BarSprite;
        bar.IconSprite = ImportedSprite(HealthBarIconPath, null);
        return bar;
    }
    private static GameObject Panel(Transform parent, string name, bool initiallyActive)
    {
        var existing = parent.Find(name);
        if (existing)
        {
            var retainedImage = existing.GetComponent<Image>();
            var retainedSprite = ImportedSprite(UiPanelPath, null);
            if (retainedImage && retainedSprite)
            {
                retainedImage.sprite = retainedSprite;
                retainedImage.type = Image.Type.Simple;
            }
            return existing.gameObject;
        }
        var panel = Child(parent, name, typeof(RectTransform), typeof(Image));
        Layout((RectTransform)panel.transform, Vector2.zero, new Vector2(560, 320));
        var image = panel.GetComponent<Image>();
        image.sprite = ImportedSprite(UiPanelPath, null);
        image.type = Image.Type.Simple;
        image.color = Ink;
        panel.SetActive(initiallyActive);
        return panel;
    }
    private static Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var existing = parent.Find(name);
        if (existing && existing.TryGetComponent<Text>(out var retained))
        {
            Layout((RectTransform)retained.transform, position, size);
            var retainedFont = AssetDatabase.LoadAssetAtPath<Font>(UiFontPath);
            if (retainedFont) retained.font = retainedFont;
            retained.text = value; retained.fontSize = fontSize; retained.color = color;
            retained.alignment = TextAnchor.MiddleCenter; retained.raycastTarget = false;
            return retained;
        }
        var go = Child(parent, name, typeof(RectTransform), typeof(Text));
        Layout((RectTransform)go.transform, position, size);
        var text = go.GetComponent<Text>();
        text.font = AssetDatabase.LoadAssetAtPath<Font>(UiFontPath) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value; text.fontSize = fontSize; text.color = color;
        text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        return text;
    }
    private static GameObject FullScreenImage(Transform parent, string name, Sprite sprite)
    {
        var existing = parent.Find(name);
        var go = existing ? existing.gameObject : Child(parent, name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = GetOrAdd<Image>(go);
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = true;
        go.SetActive(true);
        return go;
    }
    private static Image SpriteImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        var existing = parent.Find(name);
        var go = existing ? existing.gameObject : Child(parent, name, typeof(RectTransform), typeof(Image));
        Layout((RectTransform)go.transform, position, size);
        var image = GetOrAdd<Image>(go);
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }
    private static void EnsureHealthBarArt()
    {
        if (File.Exists(HealthBarTrackPath) && File.Exists(HealthBarFillPath) && File.Exists(HealthBarIconPath)) return;
        if (!File.Exists(HealthBarReferencePath)) return;

        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(source, File.ReadAllBytes(HealthBarReferencePath), false)) return;
            // The supplied 360x360 reference contains an energy row above the
            // red HP row. Crop the red row and key out its checkerboard matte.
            CreateMaskedCrop(source, new RectInt(116, 88, 199, 78), HealthBarTrackPath);
            CreateMaskedCrop(source, new RectInt(116, 88, 199, 78), HealthBarFillPath, new Color(1f, .04f, .09f, 1f));
            CreateMaskedCrop(source, new RectInt(48, 88, 64, 82), HealthBarIconPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        finally { Object.DestroyImmediate(source); }
    }
    private static void CreateMaskedCrop(Texture2D source, RectInt crop, string path, Color? solidColor = null)
    {
        var xMin = Mathf.Clamp(crop.xMin, 0, source.width - 1);
        var yMin = Mathf.Clamp(crop.yMin, 0, source.height - 1);
        var width = Mathf.Clamp(crop.width, 1, source.width - xMin);
        var height = Mathf.Clamp(crop.height, 1, source.height - yMin);
        var output = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color[width * height];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var color = source.GetPixel(xMin + x, yMin + y);
            var neutral = Mathf.Max(Mathf.Abs(color.r - color.g), Mathf.Abs(color.g - color.b), Mathf.Abs(color.r - color.b)) < .08f;
            color.a = neutral && (color.r + color.g + color.b) / 3f > .68f ? 0f : 1f;
            if (solidColor.HasValue && color.a > 0f) color = solidColor.Value;
            pixels[y * width + x] = color;
        }
        output.SetPixels(pixels);
        output.Apply();
        File.WriteAllBytes(path, output.EncodeToPNG());
        Object.DestroyImmediate(output);
    }
    private static void ApplyButtonArtwork(Button button, Sprite sprite)
    {
        if (!button || !sprite) return;
        var image = button.GetComponent<Image>();
        if (image)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }
        var labelObject = button.transform.Find("Label");
        var label = labelObject ? labelObject.GetComponent<Text>() : null;
        if (label) label.enabled = false;
    }
    private static Button Button(Transform parent, string name, string title, Vector2 position)
    {
        var existing = parent.Find(name);
        if (existing && existing.TryGetComponent<Button>(out var retained))
        {
            StyleButton(retained, new Color(.13f, .43f, .46f));
            var retainedLabel = existing.Find("Label");
            if (retainedLabel && retainedLabel.GetComponent<Text>())
            {
                Label(existing, "Label", title, Vector2.zero, new Vector2(280, 48), 21, Color.white);
            }
            return retained;
        }
        var go = Child(parent, name, typeof(RectTransform), typeof(Image), typeof(Button));
        Layout((RectTransform)go.transform, position, new Vector2(290, 52));
        StyleButton(go.GetComponent<Button>(), new Color(.13f, .43f, .46f));
        Label(go.transform, "Label", title, Vector2.zero, new Vector2(280, 48), 21, Color.white);
        return go.GetComponent<Button>();
    }

    private static Button ActionButton(Transform parent, string name, string title, Vector2 position)
    {
        var existing = parent.Find(name);
        var go = existing ? existing.gameObject : Child(parent, name, typeof(RectTransform), typeof(Image), typeof(Button));
        Layout((RectTransform)go.transform, position, new Vector2(112, 42));
        StyleButton(go.GetComponent<Button>(), new Color(.13f, .43f, .46f));
        var label = Label(go.transform, "Label", title, Vector2.zero, new Vector2(106, 38), 13, Color.white);
        Layout((RectTransform)label.transform, Vector2.zero, new Vector2(106, 38));
        return go.GetComponent<Button>();
    }

    private static Button AudioButton(Transform parent, string name, string title, Vector2 position)
    {
        var existing = parent.Find(name);
        var go = existing ? existing.gameObject : Child(parent, name, typeof(RectTransform), typeof(Image), typeof(Button));
        Layout((RectTransform)go.transform, position, new Vector2(56, 56));
        StyleButton(go.GetComponent<Button>(), new Color(.13f, .43f, .46f));
        var iconPath = name == "Settings" ? SettingsIconPath :
            name == "SoundOff" ? "Assets/_Game/Art/Kenney/Icons/audioOff.png" :
            name == "SoundOn" ? "Assets/_Game/Art/Kenney/Icons/audioOn.png" :
            name == "MusicOn" ? "Assets/_Game/Art/Kenney/Icons/musicOn.png" :
            "Assets/_Game/Art/Kenney/Icons/musicOff.png";
        var icon = ImportedSprite(iconPath, null);
        if (icon)
        {
            var iconObject = Child(go.transform, "Icon", typeof(RectTransform), typeof(Image));
            Layout((RectTransform)iconObject.transform, new Vector2(0, 11), new Vector2(22, 22));
            var iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = icon; iconImage.color = Color.white; iconImage.raycastTarget = false;
        }
        var label = Label(go.transform, "Label", title, new Vector2(0, -15), new Vector2(54, 24), 10, Color.white);
        Layout((RectTransform)label.transform, new Vector2(0, -15), new Vector2(54, 24));
        return go.GetComponent<Button>();
    }
    private static GameObject HudSurface(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var surface = Panel(parent, name, true);
        Layout((RectTransform)surface.transform, position, size);
        var image = surface.GetComponent<Image>();
        if (image) image.raycastTarget = false;
        return surface;
    }

    private static void DisableHudSurface(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing) existing.gameObject.SetActive(false);
    }
    private static void MoveChild(Transform from, Transform to, string name)
    {
        var existing = from.Find(name);
        if (existing && existing.parent != to) existing.SetParent(to, false);
    }
    private static void StyleButton(Button button, Color normal)
    {
        var image = button.GetComponent<Image>();
        var buttonSprite = ImportedSprite(UiButtonPath, null);
        if (image && buttonSprite)
        {
            image.sprite = buttonSprite;
            image.type = Image.Type.Simple;
        }
        if (!button.targetGraphic) button.targetGraphic = button.GetComponent<Graphic>();
        var colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = new Color(.2f, .62f, .63f);
        colors.pressedColor = new Color(.08f, .28f, .3f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(normal.r, normal.g, normal.b, .42f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }
    private static void Layout(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
    private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
    private static void Border(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        var wall = Visual(parent, name, sprite, position, size, new Color(.11f, .38f, .45f), 0);
        wall.layer = LayerMask.NameToLayer("Arena"); GetOrAdd<BoxCollider2D>(wall);
    }
    private static void BuildGroundTiles(Transform parent, Sprite tileSprite)
    {
        if (!tileSprite) return;
        const float tileSize = 1.28f;
        const int columns = 13;
        const int rows = 7;
        for (var x = 0; x < columns; x++)
        for (var y = 0; y < rows; y++)
        {
            var position = new Vector2((x - (columns - 1) * .5f) * tileSize, (y - (rows - 1) * .5f) * tileSize);
            var name = $"Ground tile {x}-{y}";
            // tileSand1 is 0.64 world units at 100 PPU. Scale it to the
            // 1.28-unit grid so the arena has no distracting gaps.
            Visual(parent, name, tileSprite, position, new Vector2(2f, 2f), new Color(.19f, .28f, .32f), -9);
            ApplySprite(parent, name, tileSprite);
        }
    }
    private static GameObject Visual(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, Color color, int order)
    {
        var old = parent.Find(name);
        if (old)
        {
            old.localPosition = position;
            old.localScale = new Vector3(size.x, size.y, 1);
            var oldRenderer = old.GetComponent<SpriteRenderer>();
            if (oldRenderer)
            {
                if (sprite) oldRenderer.sprite = sprite;
                oldRenderer.color = color;
                oldRenderer.sortingOrder = order;
            }
            return old.gameObject;
        }
        var go = Child(parent, name, typeof(SpriteRenderer));
        go.transform.localPosition = position; go.transform.localScale = new Vector3(size.x, size.y, 1);
        var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
        return go;
    }
    private static void ApplySprite(Transform parent, string name, Sprite sprite)
    {
        if (!sprite) return;
        var existing = parent.Find(name);
        if (!existing) return;
        var renderer = existing.GetComponent<SpriteRenderer>();
        if (renderer) renderer.sprite = sprite;
    }
    private static T UpdatePrefabVisual<T>(string path, string childName, Sprite sprite, Vector2 scale, Color color, float colliderRadius) where T : Component
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ApplySprite(root.transform, childName, sprite);
            var child = root.transform.Find(childName);
            if (child)
            {
                child.localScale = new Vector3(scale.x, scale.y, 1);
                var renderer = child.GetComponent<SpriteRenderer>();
                if (renderer) renderer.color = color;
            }
            var collider = root.GetComponent<CircleCollider2D>();
            if (collider && colliderRadius > 0)
            {
                collider.radius = colliderRadius;
                collider.isTrigger = true;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return prefab ? prefab.GetComponent<T>() : null;
    }
    private static EnemyController EnsureEnemyPrefab(Scene scene, Sprite sprite)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
        var enemySprite = ImportedSprite(EnemyBodyPath, sprite);
        var enemyBarrel = ImportedSprite(EnemyBarrelPath, null);
        if (existing) return UpdateEnemyPrefab(EnemyPath, enemySprite, enemyBarrel);
        var go = new GameObject("Enemy", typeof(EnemyController)); SceneManager.MoveGameObjectToScene(go, scene);
        go.layer = LayerMask.NameToLayer("Enemy");
        var body = go.GetComponent<Rigidbody2D>(); body.gravityScale = 0; body.bodyType = RigidbodyType2D.Kinematic; body.constraints = RigidbodyConstraints2D.FreezeRotation;
        var collider = go.GetComponent<CircleCollider2D>(); collider.radius = EnemyController.TankColliderRadius; collider.isTrigger = true;
        Visual(go.transform, "Enemy body", enemySprite, Vector2.zero, Vector2.one * EnemyController.TankVisualScale, new Color(1, .36f, .3f), 3);
        EnsureEnemyCannon(go.transform, enemyBarrel);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, EnemyPath);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<EnemyController>();
    }

    private static EnemyController UpdateEnemyPrefab(string path, Sprite bodySprite, Sprite barrelSprite)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ApplySprite(root.transform, "Enemy body", bodySprite);
            var body = root.transform.Find("Enemy body");
            if (body)
            {
                body.localScale = Vector3.one * EnemyController.TankVisualScale;
                var renderer = body.GetComponent<SpriteRenderer>();
                if (renderer) renderer.color = new Color(1, .36f, .3f);
            }
            var collider = root.GetComponent<CircleCollider2D>();
            if (collider)
            {
                collider.radius = EnemyController.TankColliderRadius;
                collider.isTrigger = true;
            }
            EnsureEnemyCannon(root.transform, barrelSprite);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return prefab ? prefab.GetComponent<EnemyController>() : null;
    }

    private static void EnsureEnemyCannon(Transform enemy, Sprite barrelSprite)
    {
        var turret = Child(enemy, "Turret");
        var barrel = Visual(turret.transform, "Barrel", barrelSprite, new Vector2(.68f, 0), new Vector2(1.3f, 1.9f), new Color(1, .32f, .28f), 4);
        ApplySprite(turret.transform, "Barrel", barrelSprite);
        barrel.transform.localRotation = Quaternion.Euler(0, 0, -90f);
        var muzzle = Child(turret.transform, "Muzzle");
        muzzle.transform.localPosition = new Vector3(.98f, 0, 0);
        muzzle.transform.localRotation = Quaternion.identity;
        var controller = enemy.GetComponent<EnemyController>();
        if (controller)
        {
            controller.Turret = turret.transform;
            controller.Muzzle = muzzle.transform;
        }
    }

    private static Projectile EnsureProjectilePrefab(Scene scene, Sprite sprite)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath);
        var bulletSprite = ImportedSprite(ProjectileSpritePath, sprite);
        var rocketSprite = ImportedSprite(RocketSpritePath, bulletSprite);
        if (existing) return UpdateProjectilePrefab(ProjectilePath, bulletSprite, rocketSprite);

        var go = new GameObject("Projectile", typeof(Projectile));
        SceneManager.MoveGameObjectToScene(go, scene);
        var collider = go.GetComponent<CircleCollider2D>();
        collider.radius = .14f;
        Visual(go.transform, "Projectile body", bulletSprite, Vector2.zero, Vector2.one, Color.white, 5);
        var rocketBody = Visual(go.transform, "Rocket body", rocketSprite, Vector2.zero, Vector2.one * .2f, Color.white, 5);
        rocketBody.SetActive(false);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, ProjectilePath);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<Projectile>();
    }

    private static Projectile UpdateProjectilePrefab(string path, Sprite bulletSprite, Sprite rocketSprite)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ApplySprite(root.transform, "Projectile body", bulletSprite);
            var bullet = root.transform.Find("Projectile body");
            if (bullet)
            {
                bullet.localScale = Vector3.one;
                var renderer = bullet.GetComponent<SpriteRenderer>();
                if (renderer) renderer.color = Color.white;
                bullet.gameObject.SetActive(true);
            }
            var rocket = Visual(root.transform, "Rocket body", rocketSprite, Vector2.zero, Vector2.one * .2f, Color.white, 5);
            ApplySprite(root.transform, "Rocket body", rocketSprite);
            rocket.SetActive(false);
            var collider = root.GetComponent<CircleCollider2D>();
            if (collider)
            {
                collider.radius = .14f;
                collider.isTrigger = true;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return prefab ? prefab.GetComponent<Projectile>() : null;
    }

    private static Mine EnsureMinePrefab(Scene scene, Sprite sprite)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(MinePath);
        if (existing) return UpdatePrefabVisual<Mine>(MinePath, "Mine body", ImportedSprite(MineSpritePath, sprite), new Vector2(.75f, .75f), new Color(1, .75f, .2f), .35f);

        var go = new GameObject("Mine", typeof(Mine));
        SceneManager.MoveGameObjectToScene(go, scene);
        var collider = go.GetComponent<CircleCollider2D>();
        collider.radius = .35f;
        Visual(go.transform, "Mine body", ImportedSprite(MineSpritePath, sprite), Vector2.zero, new Vector2(.75f, .75f), new Color(1, .75f, .2f), 4);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, MinePath);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<Mine>();
    }
    private static Sprite ImportedSprite(string path, Sprite fallback)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            importer = AssetImporter.GetAtPath(path) as TextureImporter;
        }
        if (importer && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        var imported = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return imported ? imported : fallback;
    }
    private static AudioClip PreferImportedClip(string path, AudioClip fallback)
    {
        var imported = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        return imported ? imported : fallback;
    }
    private static Sprite EnsureSprite()
    {
        var existing = AssetDatabase.LoadAllAssetsAtPath(SpritePath).OfType<Sprite>().FirstOrDefault();
        if (existing) return existing;
        var texture = new Texture2D(1, 1); texture.name = "Arena white"; texture.SetPixel(0, 0, Color.white); texture.Apply();
        AssetDatabase.CreateAsset(texture, SpritePath);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
        sprite.name = "Arena square"; AssetDatabase.AddObjectToAsset(sprite, texture); AssetDatabase.SaveAssets();
        return sprite;
    }
    private static InputActionAsset EnsureActions()
    {
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
        var editable = InputActionAsset.FromJson(asset.ToJson());
        var map = editable.FindActionMap("Gameplay") ?? editable.AddActionMap("Gameplay");
        if (map.FindAction("Move", false) == null)
        {
            var move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        }
        if (map.FindAction("Aim", false) == null) map.AddAction("Aim", InputActionType.Value, "<Mouse>/position", expectedControlLayout: "Vector2");
        if (map.FindAction("Fire", false) == null) map.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
        if (map.FindAction("Weapon1", false) == null) map.AddAction("Weapon1", InputActionType.Button, "<Keyboard>/digit1");
        if (map.FindAction("Weapon2", false) == null) map.AddAction("Weapon2", InputActionType.Button, "<Keyboard>/digit2");
        if (map.FindAction("Weapon3", false) == null) map.AddAction("Weapon3", InputActionType.Button, "<Keyboard>/digit3");
        if (map.FindAction("Shield", false) == null) map.AddAction("Shield", InputActionType.Button, "<Keyboard>/digit0");
        if (map.FindAction("EMP", false) == null) map.AddAction("EMP", InputActionType.Button, "<Keyboard>/e");
        if (map.FindAction("DemoMode", false) == null) map.AddAction("DemoMode", InputActionType.Button, "<Keyboard>/f1");
        if (map.FindAction("Pause", false) == null) map.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
        if (map.FindAction("Retry", false) == null) map.AddAction("Retry", InputActionType.Button, "<Keyboard>/r");
        File.WriteAllText(ActionsPath, editable.ToJson()); Object.DestroyImmediate(editable);
        AssetDatabase.ImportAsset(ActionsPath, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
    }
    private static InputActionReference ActionReference(string path) => AssetDatabase.LoadAllAssetsAtPath(ActionsPath)
        .OfType<InputActionReference>().First(reference => reference.action.actionMap.name + "/" + reference.action.name == path);
    private static void EnsureLayers()
    {
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        foreach (var name in new[] { "Player", "Enemy", "Core", "Arena" })
        {
            if (LayerMask.NameToLayer(name) >= 0) continue;
            for (var i = 8; i < layers.arraySize; i++)
            {
                var slot = layers.GetArrayElementAtIndex(i); if (!string.IsNullOrEmpty(slot.stringValue)) continue;
                slot.stringValue = name; tags.ApplyModifiedProperties(); break;
            }
        }
    }
    private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).First();
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var existing = go.GetComponent<T>();
        return existing ? existing : go.AddComponent<T>();
    }
    private static GameObject Root(Scene scene, string name)
    {
        var existing = scene.GetRootGameObjects().FirstOrDefault(go => go.name == name); if (existing) return existing;
        var created = new GameObject(name); SceneManager.MoveGameObjectToScene(created, scene); return created;
    }
    private static GameObject Child(Transform parent, string name, params System.Type[] components)
    {
        var existing = parent.Find(name); if (existing) return existing.gameObject;
        var created = new GameObject(name, components); SceneManager.MoveGameObjectToScene(created, parent.gameObject.scene);
        created.transform.SetParent(parent, false); return created;
    }
}
